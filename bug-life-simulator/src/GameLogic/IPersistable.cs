using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace TalesFromTheUnderbrush
{
    /// <summary>
    /// Интерфейс для объектов, которые можно сохранять и загружать
    /// </summary>
    public interface IPersistable
    {
        /// <summary>
        /// Уникальный идентификатор для сохранения
        /// </summary>
        string PersistentId { get; }

        /// <summary>
        /// Тип объекта для десериализации
        /// </summary>
        string PersistentType { get; }

        /// <summary>
        /// Нужно ли сохранять этот объект
        /// </summary>
        bool ShouldSave { get; }

        /// <summary>
        /// Сохранить состояние объекта
        /// </summary>
        PersistenceData Save();

        /// <summary>
        /// Загрузить состояние объекта
        /// </summary>
        void Load(PersistenceData data);

        /// <summary>
        /// Событие перед сохранением
        /// </summary>
        event Action<IPersistable> OnBeforeSave;

        /// <summary>
        /// Событие после загрузки
        /// </summary>
        event Action<IPersistable> OnAfterLoad;
    }

    

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

    /// <summary>
    /// Конвертеры для Vector2, Vector3, Color и других структур XNA
    /// </summary>
    public static class XnaConverters
    {
        public static Dictionary<string, object> SerializeVector2(Vector2 vector)
        {
            return new Dictionary<string, object>
            {
                ["x"] = vector.X,
                ["y"] = vector.Y
            };
        }

        public static Vector2 DeserializeVector2(Dictionary<string, object> data)
        {
            return new Vector2(
                Convert.ToSingle(data["x"]),
                Convert.ToSingle(data["y"])
            );
        }

        public static Dictionary<string, object> SerializeVector3(Vector3 vector)
        {
            return new Dictionary<string, object>
            {
                ["x"] = vector.X,
                ["y"] = vector.Y,
                ["z"] = vector.Z
            };
        }

        public static Vector3 DeserializeVector3(Dictionary<string, object> data)
        {
            return new Vector3(
                Convert.ToSingle(data["x"]),
                Convert.ToSingle(data["y"]),
                Convert.ToSingle(data["z"])
            );
        }

        public static Dictionary<string, object> SerializeColor(Color color)
        {
            return new Dictionary<string, object>
            {
                ["r"] = color.R,
                ["g"] = color.G,
                ["b"] = color.B,
                ["a"] = color.A
            };
        }

        public static Color DeserializeColor(Dictionary<string, object> data)
        {
            return new Color(
                Convert.ToByte(data["r"]),
                Convert.ToByte(data["g"]),
                Convert.ToByte(data["b"]),
                Convert.ToByte(data["a"])
            );
        }

        public static Dictionary<string, object> SerializeRectangle(Rectangle rect)
        {
            return new Dictionary<string, object>
            {
                ["x"] = rect.X,
                ["y"] = rect.Y,
                ["width"] = rect.Width,
                ["height"] = rect.Height
            };
        }

        public static Rectangle DeserializeRectangle(Dictionary<string, object> data)
        {
            return new Rectangle(
                Convert.ToInt32(data["x"]),
                Convert.ToInt32(data["y"]),
                Convert.ToInt32(data["width"]),
                Convert.ToInt32(data["height"])
            );
        }
    }
}
