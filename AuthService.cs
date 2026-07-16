using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using StardewModdingAPI;

namespace SaveFetch
{
    /// <summary>
    /// Browser-based login (OAuth loopback redirect pattern, like `gh auth login`):
    /// open the website's login page in the default browser with a one-time state value,
    /// and catch the token the site redirects back to a temporary localhost listener.
    /// </summary>
    public class AuthService
    {
        private static readonly TimeSpan LoginTimeout = TimeSpan.FromMinutes(2);

        private readonly IMonitor monitor;
        private readonly ModConfig config;
        private readonly TokenStore tokens;
        private int loginInProgress; 

        public AuthService(IMonitor monitor, ModConfig config, TokenStore tokens)
        {
            this.monitor = monitor;
            this.config = config;
            this.tokens = tokens;
        }

        public async Task BeginLoginAsync()
        {
            if (Interlocked.Exchange(ref this.loginInProgress, 1) == 1)
            {
                this.monitor.Log("A login is already in progress — finish it in your browser first.", LogLevel.Warn);
                return;
            }

            HttpListener? listener = null;
            try
            {
                string state = GenerateState();
                int port = this.config.CallbackPort > 0 ? this.config.CallbackPort : GetFreePort();

                listener = new HttpListener();
                listener.Prefixes.Add($"http://127.0.0.1:{port}/callback/");
                listener.Start();

                string separator = this.config.LoginUrl.Contains('?') ? "&" : "?";
                string loginUrl = $"{this.config.LoginUrl.TrimEnd('/')}{separator}port={port}&state={Uri.EscapeDataString(state)}";
                this.monitor.Log("Opening your browser to log in...", LogLevel.Info);
                this.monitor.Log($"If it doesn't open, visit: {loginUrl}", LogLevel.Info);
                Process.Start(new ProcessStartInfo(loginUrl) { UseShellExecute = true });

                var contextTask = listener.GetContextAsync();
                var completed = await Task.WhenAny(contextTask, Task.Delay(LoginTimeout));
                if (completed != contextTask)
                {
                    this.monitor.Log("Login timed out after 2 minutes. Run `savefetch_login` to try again.", LogLevel.Warn);
                    return;
                }

                HttpListenerContext context = contextTask.Result;
                var query = HttpUtility.ParseQueryString(context.Request.Url?.Query ?? "");
                string? token = query["token"];
                string username = query["username"] ?? "(unknown)";
                string? returnedState = query["state"];

                if (returnedState != state || string.IsNullOrEmpty(token))
                {
                    await RespondAsync(context, "<h2>❌ Login failed</h2><p>Invalid callback. Return to the game and try again.</p>");
                    this.monitor.Log("Login failed: callback was missing the token or had a mismatched state value.", LogLevel.Error);
                    return;
                }

                this.tokens.Set(token, username);
                await RespondAsync(context, $"<h2>✅ Logged in as {HttpUtility.HtmlEncode(username)}</h2><p>You can close this tab and return to the game.</p>");
                this.monitor.Log($"Logged in as {username}. Saves will now be uploaded automatically.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                this.monitor.Log($"Login failed: {ex.Message}", LogLevel.Error);
                this.monitor.Log(ex.ToString(), LogLevel.Trace);
            }
            finally
            {
                listener?.Close();
                Interlocked.Exchange(ref this.loginInProgress, 0);
            }
        }


        private static string GenerateState()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToHexString(bytes);
        }

        private static int GetFreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        private static async Task RespondAsync(HttpListenerContext context, string bodyHtml)
        {
            string html = $"<!doctype html><html><head><meta charset=\"utf-8\"><title>SaveFetch</title></head><body style=\"font-family:sans-serif;text-align:center;margin-top:4rem\">{bodyHtml}</body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.Close();
        }
    }
}
