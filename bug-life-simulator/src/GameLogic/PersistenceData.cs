using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace TalesFromTheUnderbrush
{
    /// <summary>
    /// Данные для сохранения
    /// </summary>
    public class PersistenceData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; } = 1;

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonProperty("data")]
        public Dictionary<string, object> Data { get; set; } = new();

        [JsonProperty("children")]
        public List<PersistenceData> Children { get; set; } = new();

        public PersistenceData() { }

        public PersistenceData(string id, string type)
        {
            Id = id;
            Type = type;
        }

        public T GetValue<T>(string key, T defaultValue = default)
        {
            if (Data.TryGetValue(key, out object value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public void SetValue<T>(string key, T value)
        {
            Data[key] = value;
        }

        public bool HasValue(string key)
        {
            return Data.ContainsKey(key);
        }

        public void RemoveValue(string key)
        {
            Data.Remove(key);
        }
    }
}
