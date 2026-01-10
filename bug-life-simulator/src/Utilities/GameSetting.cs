using Microsoft.Xna.Framework;

namespace TalesFromTheUnderbrush
{
    public static class GameSetting
    {
        public const float WorldTileWidth = 128f;
        public const float WorldTileHeight = 64f;
        public const float WorldTileHalfWidth = WorldTileWidth / 2f;
        public const float WorldTileHalfHeight = WorldTileHeight / 2f;
        public const int WorldChunkSize = 16;
        public const float CharacterWorldGravity = 9.8f;

        public const float CharacterMoveSpeed = 3.0f;
        public const float CharacterRunMultiplier = 1.5f;
        public const float CharacterInteractionRange = 2.0f;

        public const float CameraMoveSpeed = 5.0f;
        public const float CameraZoomSpeed = 0.1f;
        public const float CameraMinZoom = 0.5f;
        public const float CameraMaxZoom = 2.0f;
        public const float CameraDefaultZoom = 1.0f;

        public const float IsometricIsoAngle = 30f;
        public const float IsometricLayerHeight = 32f;
        public const int IsometricMaxRenderLayers = 10;

        public const float UIScale = 1.0f;
        public static readonly Color UITextColor = Color.White;
        public static readonly Color UIHighlightColor = Color.Gold;

        public const bool TemporaryGodMode = false;
        public const bool TemporaryUnlimitedResources = false;

        public const float NeedsHungerRate = 0.1f;
        public const float NeedsEnergyDrainRate = 0.05f;
        public const float NeedsMoodChangeRate = 0.02f;

        public static readonly Vector2 TileSize = new Vector2(WorldTileWidth, WorldTileHeight);

        public static Vector2 TileToWorld(Vector2 tilePosition) => tilePosition * TileSize;

        public static Vector2 WorldToTile(Vector2 worldPosition) => worldPosition / TileSize;
    }
}
