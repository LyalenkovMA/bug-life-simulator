using Microsoft.Xna.Framework;

namespace TalesFromTheUnderbrush
{
    public static class GameSetting
    {
        // === ТАЙЛЫ И СЕТКА (2:1 ИЗОМЕТРИЯ) ===
        public const float WorldTileWidth = 128f;
        public const float WorldTileHeight = 64f;
        public const float WorldTileHalfWidth = WorldTileWidth / 2f;
        public const float WorldTileHalfHeight = WorldTileHeight / 2f;
        public const int WorldChunkHeight = 4;

        public const int WorldChunkSize = 64;      // Размер чанка в тайлах
        public const int VisualLayerStep = 32;     // Визуальное смещение по Z (только для сортировки, не для физики!)

        // === ПЕРСОНАЖ (Визуальные габариты) ===
        public const int CharacterVisualWidth = 80;   // ~1/2 от ширины тайла
        public const int CharacterVisualHeight = 160; // 2.5 тайла
        // Точка привязки спрайта: низ по центру (относительно сетки)
        public static readonly Vector2 CharacterSpriteOrigin = new Vector2(CharacterVisualWidth / 2f, CharacterVisualHeight);

        // === КАМЕРА ===
        public const float CameraMoveSpeed = 5.0f;
        public const float CameraZoomSpeed = 0.1f;
        public const float CameraMinZoom = 0.5f;
        public const float CameraMaxZoom = 2.0f;
        public const float CameraDefaultZoom = 1.0f;

        // === ГЕЙМПЛЕЙ И МЕХАНИКИ ===
        public const float CharacterMoveSpeed = 3.0f;       // Базовая скорость шага (тайлов/сек)
        public const float CharacterRunMultiplier = 1.5f;
        public const float CharacterInteractionRange = 2.0f;

        // Потребности (Маслоу)
        public const float NeedsHungerRate = 0.1f;
        public const float NeedsEnergyDrainRate = 0.05f;
        public const float NeedsMoodChangeRate = 0.02f;

        // === UI ===
        public const float UIScale = 1.0f;
        public static readonly Color UITextColor = Color.White;
        public static readonly Color UIHighlightColor = Color.Gold;

        // === ОТЛАДОЧНЫЕ ПЕРЕКЛЮЧАТЕЛИ (Временные) ===
        public const bool TemporaryGodMode = false;
        public const bool TemporaryUnlimitedResources = false;

        // === УТИЛИТЫ ===
        public static readonly Vector2 TileSize = new Vector2(WorldTileWidth, WorldTileHeight);
        public static Vector2 TileToWorld(Vector2 tilePosition) => tilePosition * TileSize;
        public static Vector2 WorldToTile(Vector2 worldPosition) => worldPosition / TileSize;
    }
}