using Newtonsoft.Json;
using System;
using System.Collections.Generic;


namespace TalesFromTheUnderbrush.src.GameLogic
{
    /// <summary>
    /// Состояние мира для сохранения
    /// </summary>
    public class WorldState
    {
        [JsonProperty("worldName")]
        public string WorldName { get; set; }

        [JsonProperty("worldVersion")]
        public int WorldVersion { get; set; } = 1;

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonProperty("entities")]
        public List<PersistenceData> Entities { get; set; } = new();

        [JsonProperty("tiles")]
        public List<PersistenceData> Tiles { get; set; } = new();

        [JsonProperty("playerData")]
        public PersistenceData PlayerData { get; set; }

        [JsonProperty("metadata")]
        public Dictionary<string, object> Metadata { get; set; } = new();

        public WorldState(string worldName)
        {
            WorldName = worldName;
        }
    }
}
