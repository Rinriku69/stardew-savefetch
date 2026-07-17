using System;
using System.Threading.Tasks;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SaveFetch
{
    public class ModEntry : Mod
    {
        private ModConfig config = null!;
        private TokenStore tokens = null!;
        private AvatarStateStore avatarState = null!;
        private AuthService auth = null!;
        private ApiClient api = null!;
        private string lastUploadResult = "(no upload yet)";

        public override void Entry(IModHelper helper)
        {
            this.config = helper.ReadConfig<ModConfig>();
            this.tokens = new TokenStore(helper.Data);
            this.avatarState = new AvatarStateStore(helper.Data);
            this.auth = new AuthService(this.Monitor, this.config, this.tokens);
            this.api = new ApiClient(this.config.SaveUrl, this.config.RefreshUrl);

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

            // build/render on the game thread (reads live game state, and rendering the sprite
            // touches the GraphicsDevice), upload on a background task so a slow server can't
            // freeze the game
            SavePayload payload = SavePayload.Build(this.ModManifest.Version.ToString());

            string appearanceHash = AppearanceSnapshot.ComputeHash(Game1.player);
            byte[]? avatarPng = appearanceHash != this.avatarState.LastSentAppearanceHash
                ? PlayerSpriteRenderer.RenderIdlePortrait(Game1.player)
                : null;

            Task.Run(() => this.UploadAsync(payload, avatarPng, appearanceHash));
        }

        private async Task UploadAsync(SavePayload payload, byte[]? avatarPng, string appearanceHash)
        {
            AuthData? auth = this.tokens.Get();
            if (auth == null)
                return;

            var (saveResult, saveDetail, accessToken) = await this.UploadWithRefreshAsync(
                token => this.api.UploadSaveAsync(payload, token), auth.AccessToken);

            this.lastUploadResult = $"{saveResult} ({saveDetail})";

            switch (saveResult)
            {
                case UploadResult.Success:
                    this.Monitor.Log($"Save uploaded ({payload.Season} {payload.Day}, year {payload.Year}).", LogLevel.Info);
                    break;
                case UploadResult.Unauthorized:
                    this.Monitor.Log("Save upload rejected: your login expired. Run `savefetch_login` to log in again.", LogLevel.Warn);
                    break;
                default:
                    this.Monitor.Log($"Save upload failed: {saveDetail}", LogLevel.Warn);
                    break;
            }

            if (avatarPng == null)
                return;

            // reuse whatever token the save upload ended up with, so a token refreshed above
            // isn't refreshed a second time for this second request
            var (avatarResult, avatarDetail, _) = await this.UploadWithRefreshAsync(
                token => this.api.UploadAvatarAsync(avatarPng, this.config.AvatarUrl, token), accessToken);

            switch (avatarResult)
            {
                case UploadResult.Success:
                    this.avatarState.SetLastSentAppearanceHash(appearanceHash);
                    this.Monitor.Log("Avatar uploaded.", LogLevel.Info);
                    break;
                case UploadResult.Unauthorized:
                    this.Monitor.Log("Avatar upload rejected: your login expired. Run `savefetch_login` to log in again.", LogLevel.Warn);
                    break;
                default:
                    this.Monitor.Log($"Avatar upload failed: {avatarDetail}", LogLevel.Warn);
                    break;
            }
        }

        /// <summary>Runs <paramref name="upload"/> with the current access token, refreshing it
        /// once and retrying if the server rejects it. Shared by both upload calls in
        /// <see cref="UploadAsync"/> so the refresh dance (access tokens are short-lived, so a
        /// 401 usually means "expired", not "wrong user") isn't duplicated per endpoint.</summary>
        private async Task<(UploadResult Result, string Detail, string AccessToken)> UploadWithRefreshAsync(
            Func<string, Task<(UploadResult Result, string Detail)>> upload, string accessToken)
        {
            var (result, detail) = await upload(accessToken);
            if (result != UploadResult.Unauthorized)
                return (result, detail, accessToken);

            this.Monitor.Log("Access token rejected; trying to refresh it.", LogLevel.Trace);
            string? refreshed = await this.api.RefreshTokenAsync(accessToken);
            if (refreshed == null)
                return (result, detail, accessToken);

            this.tokens.UpdateAccessToken(refreshed);
            this.Monitor.Log("Login refreshed.", LogLevel.Trace);

            (result, detail) = await upload(refreshed);
            return (result, detail, refreshed);
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

            this.Monitor.Log($"Login URL: {this.config.LoginUrl}", LogLevel.Info);
            this.Monitor.Log($"Save URL: {this.config.SaveUrl}", LogLevel.Info);
            this.Monitor.Log($"Last upload: {this.lastUploadResult}", LogLevel.Info);
        }

        private void OnLogoutCommand(string command, string[] args)
        {
            this.tokens.Clear();
            this.Monitor.Log("Logged out. Saves will no longer be uploaded.", LogLevel.Info);
        }
    }
}
