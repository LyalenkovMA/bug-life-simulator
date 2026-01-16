using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace TalesFromTheUnderbrush.src.GameLogic
{
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
