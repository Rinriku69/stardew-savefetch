using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;

namespace SaveFetch
{
    /// <summary>
    /// The curated save summary sent to the server. Built from live game state right after
    /// a save completes (GameLoop.Saved), so it matches exactly what was written to disk.
    /// </summary>
    public class SavePayload
    {
        public ulong SaveId { get; set; }
        public string FarmerName { get; set; } = "";
        public string FarmName { get; set; } = "";

        /// <summary>Identifies the farmer within the save. The server already knows which website
        /// account sent this (the JWT's subject); this is the in-game identity, which the farmer
        /// name can't safely stand in for.</summary>
        public long UniqueMultiplayerID { get; set; }

        /// <summary>Whether this client hosts the farm. Marks the row the server should trust for
        /// world-level values (money is a shared wallet, so every client reports the same figure).</summary>
        public bool IsHost { get; set; }

        public string GameVersion { get; set; } = "";
        public string ModVersion { get; set; } = "";
        public DateTime SentAtUtc { get; set; }

        public int Day { get; set; }
        public string Season { get; set; } = "";
        public int Year { get; set; }
        public uint DaysPlayed { get; set; }
        public ulong PlaytimeMs { get; set; }

        public long Money { get; set; }
        public ulong TotalMoneyEarned { get; set; }

        public Dictionary<string, int> Skills { get; set; } = new();
        public Dictionary<string, uint> Stats { get; set; } = new();

        public static SavePayload Build(string modVersion)
        {
            Farmer player = Game1.player;

            return new SavePayload
            {
                SaveId = Game1.uniqueIDForThisGame,
                FarmerName = player.Name,
                FarmName = player.farmName.Value,
                UniqueMultiplayerID = player.UniqueMultiplayerID,
                IsHost = Context.IsMainPlayer,
                GameVersion = Game1.version,
                ModVersion = modVersion,
                SentAtUtc = DateTime.UtcNow,

                Day = Game1.dayOfMonth,
                Season = Game1.currentSeason,
                Year = Game1.year,
                DaysPlayed = Game1.stats.DaysPlayed,
                PlaytimeMs = player.millisecondsPlayed,

                Money = player.Money,
                TotalMoneyEarned = player.totalMoneyEarned,

                Skills = new Dictionary<string, int>
                {
                    ["farming"] = player.farmingLevel.Value,
                    ["mining"] = player.miningLevel.Value,
                    ["foraging"] = player.foragingLevel.Value,
                    ["fishing"] = player.fishingLevel.Value,
                    ["combat"] = player.combatLevel.Value
                },

                Stats = new Dictionary<string, uint>
                {
                    ["itemsCrafted"] = Game1.stats.ItemsCrafted,
                    ["itemsCooked"] = Game1.stats.ItemsCooked,
                    ["fishCaught"] = Game1.stats.FishCaught,
                    ["monstersKilled"] = Game1.stats.MonstersKilled
                }
            };
        }
    }
}
