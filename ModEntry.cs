using System.Threading.Tasks;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace SaveFetch
{
    public class ModEntry : Mod
    {
        private ModConfig config = null!;
        private TokenStore tokens = null!;
        private AuthService auth = null!;
        private ApiClient api = null!;
        private string lastUploadResult = "(no upload yet)";

        public override void Entry(IModHelper helper)
        {
            this.config = helper.ReadConfig<ModConfig>();
            this.tokens = new TokenStore(helper.Data);
            this.auth = new AuthService(this.Monitor, this.config, this.tokens);
            this.api = new ApiClient(this.config.BaseUrl);

            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.Saved += this.OnSaved;

            helper.ConsoleCommands.Add("savefetch_login", "Log in to the SaveFetch website via your browser.", this.OnLoginCommand);
            helper.ConsoleCommands.Add("savefetch_status", "Show SaveFetch login state and last upload result.", this.OnStatusCommand);
            helper.ConsoleCommands.Add("savefetch_logout", "Log out and stop uploading saves.", this.OnLogoutCommand);
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            if (this.tokens.IsLoggedIn)
                this.Monitor.Log($"Logged in as {this.tokens.Get()!.Username}. Saves will be uploaded automatically.", LogLevel.Info);
            else
                this.Monitor.Log("Not logged in — run `savefetch_login` in this console to enable save uploads.", LogLevel.Warn);
        }

        private void OnSaved(object? sender, SavedEventArgs e)
        {
            if (!this.tokens.IsLoggedIn)
                return;

            // build on the game thread (reads live game state), upload on a background task
            // so a slow server can't freeze the game
            SavePayload payload = SavePayload.Build(this.ModManifest.Version.ToString());
            string token = this.tokens.Get()!.AccessToken;

            Task.Run(async () =>
            {
                var (result, detail) = await this.api.UploadSaveAsync(payload, token);
                this.lastUploadResult = $"{result} ({detail})";

                switch (result)
                {
                    case UploadResult.Success:
                        this.Monitor.Log($"Save uploaded ({payload.Season} {payload.Day}, year {payload.Year}).", LogLevel.Info);
                        break;
                    case UploadResult.Unauthorized:
                        this.Monitor.Log("Save upload rejected: your login expired. Run `savefetch_login` to log in again.", LogLevel.Warn);
                        break;
                    default:
                        this.Monitor.Log($"Save upload failed: {detail}", LogLevel.Warn);
                        break;
                }
            });
        }

        private void OnLoginCommand(string command, string[] args)
        {
            _ = this.auth.BeginLoginAsync();
        }

        private void OnStatusCommand(string command, string[] args)
        {
            if (this.tokens.IsLoggedIn)
            {
                AuthData data = this.tokens.Get()!;
                this.Monitor.Log($"Logged in as {data.Username} (since {data.LoggedInAtUtc:u}).", LogLevel.Info);
            }
            else
            {
                this.Monitor.Log("Not logged in. Run `savefetch_login` to log in.", LogLevel.Info);
            }

            this.Monitor.Log($"Server: {this.config.BaseUrl}", LogLevel.Info);
            this.Monitor.Log($"Last upload: {this.lastUploadResult}", LogLevel.Info);
        }

        private void OnLogoutCommand(string command, string[] args)
        {
            this.tokens.Clear();
            this.Monitor.Log("Logged out. Saves will no longer be uploaded.", LogLevel.Info);
        }
    }
}
