namespace SaveFetch
{
    /// <summary>User-editable settings, persisted by SMAPI as config.json in the mod folder.</summary>
    public class ModConfig
    {
        /// <summary>Root URL of the website. The real URL is set in the deployed config.json,
        /// not here — this default is compiled into the DLL and visible in the public repo.</summary>
        public string BaseUrl { get; set; } = "https://example.test";

        /// <summary>Fixed port for the login callback listener; 0 picks any free port.</summary>
        public int CallbackPort { get; set; } = 0;
    }
}
