using System;
using StardewModdingAPI;

namespace SaveFetch
{
    /// <summary>The persisted login state.</summary>
    public class AuthData
    {
        public string AccessToken { get; set; } = "";
        public string Username { get; set; } = "";
        public DateTime LoggedInAtUtc { get; set; }
    }

    /// <summary>
    /// Persists the access token via SMAPI's global data API, which writes to the game's
    /// .smapi/mod-data folder — unlike config.json this isn't user-facing or shared.
    /// </summary>
    public class TokenStore
    {
        private const string Key = "auth";
        private readonly IDataHelper data;
        private AuthData? cached;

        public TokenStore(IDataHelper data)
        {
            this.data = data;
            this.cached = data.ReadGlobalData<AuthData>(Key);
        }

        /// <summary>The current login state, or null if not logged in.</summary>
        public AuthData? Get() => this.cached;

        public bool IsLoggedIn => !string.IsNullOrEmpty(this.cached?.AccessToken);

        public void Set(string accessToken, string username)
        {
            this.cached = new AuthData
            {
                AccessToken = accessToken,
                Username = username,
                LoggedInAtUtc = DateTime.UtcNow
            };
            this.data.WriteGlobalData(Key, this.cached);
        }

        public void UpdateAccessToken(string accessToken)
        {
            if (this.cached == null)
                return;

            this.cached.AccessToken = accessToken;
            this.data.WriteGlobalData(Key, this.cached);
        }

        public void Clear()
        {
            this.cached = null;
            this.data.WriteGlobalData<AuthData>(Key, null);
        }
    }
}
