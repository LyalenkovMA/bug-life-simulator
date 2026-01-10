using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TalesFromTheUnderbrush.src.GameLogic;

namespace TalesFromTheUnderbrush
{
    public static class GlobalSettings
    {
        // Сервисы (инициализируются один раз в начале)
        public static AssetManager Assets { get; private set; }
        public static RenderManager Renderer { get; private set; }

        // Константы и утилиты
        public const float BaseTileHeight = 64f;
        public const float HeightMultiplier = 32f;

        // Инициализация (вызывается из Game1.Initialize())
        public static void Initialize(ContentManager content, GraphicsDevice graphicsDevice)
        {
            Assets = new AssetManager(content);
            Renderer = new RenderManager(graphicsDevice);
        }

        // Методы для удобства (опционально)
        public static Texture2D LoadTexture(string path) => Assets.Load<Texture2D>(path);
        public static SpriteFont LoadFont(string path) => Assets.Load<SpriteFont>(path);

        // Изометрические преобразования
        public static Vector2 ToIso(Vector3 worldPos)
        {
            float screenX = (worldPos.X - worldPos.Y) * GameSetting.WorldTileHalfWidth;
            float screenY = (worldPos.X + worldPos.Y) * GameSetting.WorldTileHalfHeight
                          - ZToScreenOffset(worldPos.Z);
            return new Vector2(screenX, screenY);
        }

        public static float ZToScreenOffset(float z) => z * HeightMultiplier;
        public static float CalculateDepth(float worldY, float z) => (worldY * 0.01f) + (z * 0.001f);

        // Очистка ресурсов
        public static void Dispose()
        {
            Renderer?.Dispose();
            Assets?.Dispose();
        }
    }
}