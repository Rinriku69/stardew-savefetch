using System;
using System.Security.Cryptography;
using System.Text;
using StardewValley;

namespace SaveFetch
{
    /// <summary>
    /// Fingerprints the parts of a Farmer's appearance that change what their sprite looks like,
    /// so ModEntry can skip re-rendering/re-uploading the avatar when nothing actually changed.
    /// </summary>
    public static class AppearanceSnapshot
    {
        public static string ComputeHash(Farmer player)
        {
            string raw = string.Join('|',
                player.skin.Value,
                player.hair.Value,
                player.hairstyleColor.Value.PackedValue,
                player.shirt.Value,
                player.shirtItem.Value?.ItemId ?? "",
                player.pants.Value,
                player.pantsItem.Value?.ItemId ?? "",
                player.pantsColor.Value.PackedValue,
                player.accessory.Value,
                player.hat.Value?.ItemId ?? "",
                player.Gender
            );

            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash)[..16];
        }
    }
}
