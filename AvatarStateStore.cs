using StardewModdingAPI;

namespace SaveFetch
{
    /// <summary>The last appearance hash successfully uploaded, so a repeat save with an
    /// unchanged outfit doesn't re-render and re-upload an identical image.</summary>
    public class AvatarState
    {
        public string LastSentAppearanceHash { get; set; } = "";
    }

    /// <summary>
    /// Persists AvatarState via SMAPI's global data API — same mechanism as TokenStore, but a
    /// separate key/class because this is an upload-dedup concern, not an auth concern.
    /// </summary>
    public class AvatarStateStore
    {
        private const string Key = "avatar-state";
        private readonly IDataHelper data;
        private AvatarState cached;

        public AvatarStateStore(IDataHelper data)
        {
            this.data = data;
            this.cached = data.ReadGlobalData<AvatarState>(Key) ?? new AvatarState();
        }

        public string LastSentAppearanceHash => this.cached.LastSentAppearanceHash;

        public void SetLastSentAppearanceHash(string hash)
        {
            this.cached = new AvatarState { LastSentAppearanceHash = hash };
            this.data.WriteGlobalData(Key, this.cached);
        }
    }
}
