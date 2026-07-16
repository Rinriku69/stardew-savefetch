namespace SaveFetch
{
    /// <summary>User-editable settings, persisted by SMAPI as config.json in the mod folder.</summary>
    public class ModConfig
    {
        /// <summary>Full URL of the website's mod-login page (the browser is opened here with
        /// port/state appended). The real URL is set in the deployed config.json, not here —
        /// these defaults are compiled into the DLL and visible in the public repo.</summary>
        public string LoginUrl { get; set; } = "https://example.test/mod-auth";

        /// <summary>Full URL of the API endpoint that receives the save payload (POST, Bearer auth).</summary>
        public string SaveUrl { get; set; } = "https://example.test/api/saves";

        /// <summary>Full URL of the JWT refresh endpoint. The expired token is POSTed here as a
        /// Bearer header when an upload returns 401; the response supplies a replacement.</summary>
        public string RefreshUrl { get; set; } = "https://example.test/api/auth/jwt/refresh";

        /// <summary>Fixed port for the login callback listener; 0 picks any free port.</summary>
        public int CallbackPort { get; set; } = 0;
    }
}
