using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.IO;
using TalesFromTheUnderbrush.src;
using TalesFromTheUnderbrush.src.UI.Camera;

namespace TalesFromTheUnderbrush
{
    public static class GlobalSettings
    {
        public static string GameTitle { get; private set; } = "Bug Life Simulator";
        public static int TargetFPS { get; private set; } = 60;
        public static bool FullScreen { get; private set; } = false;
        public static int ScreenWidth { get; private set; } = 1280;
        public static int ScreenHeight { get; private set; } = 720;

        // === РЕЖИМЫ ОТЛАДКИ ===
        public static bool DebugMode { get; set; } = true;
        public static bool ShowFPS { get; set; } = true;
        public static bool ShowMouseCoordinates { get; set; } = true;
        public static bool ShowObjectDebugInfo { get; set; } = true;
        public static bool ShowCollisionBounds { get; set; } = false;
        public static bool ShowTileDebug { get; set; } = false;
        public static bool ShowSpatialGrid { get; set; } = false;
        public static bool ShowCameraInfo { get; set; } = true;
        public static bool ShowWorldInfo { get; set; } = true;
        public static bool ShowPathfinding { get; set; } = false;
        public static bool ShowAIStates { get; set; } = false;
        public static bool GodMode { get; set; } = false;
        public static bool FastForward { get; set; } = false;
        public static bool SkipRendering { get; set; } = false;
        public static bool LogEverything { get; set; } = false;

        // === ПРОИЗВОДИТЕЛЬНОСТЬ ===
        public static int EntityLimit { get; set; } = 1000;
        public static int FPSLimit { get; set; } = 0;
        public static int SpatialGridChunkSize { get; set; } = 64;
        // Синхронизировано с GameSetting.WorldChunkSize
        public static int TileGridChunkSize { get; set; } = GameSetting.WorldChunkSize;

        // === ГЕЙМПЛЕЙ (Тесты) ===
        public static bool InfiniteResources { get; set; } = false;
        public static bool InstantBuild { get; set; } = false;
        public static bool NoEnemies { get; set; } = false;
        public static bool NoNeeds { get; set; } = false;
        public static int StartingLevel { get; set; } = 1;

        // === МЕТОДЫ УПРАВЛЕНИЯ ===
        public static void EnableAllDebug()
        {
            DebugMode = true; ShowFPS = true; ShowMouseCoordinates = true; ShowObjectDebugInfo = true;
            ShowCollisionBounds = true; ShowTileDebug = true; ShowSpatialGrid = true; ShowCameraInfo = true;
            ShowWorldInfo = true; ShowPathfinding = true; ShowAIStates = true; LogEverything = true;
        }

        public static void DisableAllDebug()
        {
            DebugMode = false; ShowFPS = false; ShowMouseCoordinates = false; ShowObjectDebugInfo = false;
            ShowCollisionBounds = false; ShowTileDebug = false; ShowSpatialGrid = false; ShowCameraInfo = false;
            ShowWorldInfo = false; ShowPathfinding = false; ShowAIStates = false; LogEverything = false;
        }

        public static void ToggleDebugMode()
        {
            DebugMode = !DebugMode;
            Console.WriteLine($"[GlobalSettings] DebugMode = {DebugMode}");
        }

        public static void ToggleDebugSetting(string settingName)
        {
            switch (settingName.ToLower())
            {
                case "fps": ShowFPS = !ShowFPS; break;
                case "collision": ShowCollisionBounds = !ShowCollisionBounds; break;
                case "tiles": ShowTileDebug = !ShowTileDebug; break;
                case "grid": ShowSpatialGrid = !ShowSpatialGrid; break;
                case "camera": ShowCameraInfo = !ShowCameraInfo; break;
                case "world": ShowWorldInfo = !ShowWorldInfo; break;
                case "pathfinding": ShowPathfinding = !ShowPathfinding; break;
                case "ai": ShowAIStates = !ShowAIStates; break;
                default: Console.WriteLine($"Unknown debug setting: {settingName}"); break;
            }
        }

        public static bool GetDebugSettingValue(string settingName) => settingName.ToLower() switch
        {
            "fps" => ShowFPS,
            "collision" => ShowCollisionBounds,
            "tiles" => ShowTileDebug,
            "grid" => ShowSpatialGrid,
            "camera" => ShowCameraInfo,
            "world" => ShowWorldInfo,
            "pathfinding" => ShowPathfinding,
            "ai" => ShowAIStates,
            _ => false
        };

        public static void SaveToFile(string filePath = "settings.json")
        {
            try
            {
                var settings = new
                {
                    DebugMode,
                    ShowFPS,
                    ShowMouseCoordinates,
                    ShowObjectDebugInfo,
                    ShowCollisionBounds,
                    ShowTileDebug,
                    ShowSpatialGrid,
                    ShowCameraInfo,
                    ShowWorldInfo,
                    ShowPathfinding,
                    ShowAIStates,
                    GodMode,
                    FastForward,
                    SkipRendering,
                    LogEverything,
                    EntityLimit,
                    FPSLimit,
                    SpatialGridChunkSize,
                    TileGridChunkSize,
                    InfiniteResources,
                    InstantBuild,
                    NoEnemies,
                    NoNeeds,
                    StartingLevel
                };
                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex) { Console.WriteLine($"[GlobalSettings] Ошибка сохранения: {ex.Message}"); }
        }

        public static void LoadFromFile(string filePath = "settings.json")
        {
            try
            {
                if (!File.Exists(filePath)) return;
                string json = File.ReadAllText(filePath);
                var settings = JsonConvert.DeserializeObject<dynamic>(json);
                if (settings != null)
                {
                    DebugMode = settings.DebugMode ?? DebugMode;
                    ShowFPS = settings.ShowFPS ?? ShowFPS;
                    // ... можно добавить загрузку остальных полей
                }
            }
            catch (Exception ex) { Console.WriteLine($"[GlobalSettings] Ошибка загрузки: {ex.Message}"); }
        }

        public static void ResetToDefaults()
        {
            EnableAllDebug(); DebugMode = false; // По умолчанию дебаг выкл, кроме FPS
            ShowFPS = true; ShowCameraInfo = true; ShowWorldInfo = true;
            GodMode = false; FastForward = false; SkipRendering = false; LogEverything = false;
            EntityLimit = 1000; FPSLimit = 0;
            SpatialGridChunkSize = 64; TileGridChunkSize = GameSetting.WorldChunkSize;
            InfiniteResources = false; InstantBuild = false; NoEnemies = false; NoNeeds = false; StartingLevel = 1;
        }

        // === ИЗОМЕТРИЧЕСКИЕ УТИЛИТЫ (2:1) ===
        public static Vector2 GetIsometricGridPosition(Vector2 grid, int layer = 0)
        {
            float screenX = (grid.X - grid.Y) * GameSetting.WorldTileHalfWidth;
            float screenY = (grid.X + grid.Y) * GameSetting.WorldTileHalfHeight - (layer * GameSetting.VisualLayerStep);
            return new Vector2(screenX, screenY);
        }

        public static float GetIsometricDrawDepth(Vector2 grid, int layer)
        {
            float depth = (grid.X + grid.Y) * 1000 + layer * 5;
            return MathHelper.Clamp(depth / 10000f, 0f, 0.9999f);
        }

        public static Point ScreenToIsometricGrid(Vector2 screenPos, ICamera camera = null)
        {
            // Если камера передана, нужно компенсировать её смещение. 
            // В текущей архитектуре камера возвращает мировые координаты, поэтому прямое преобразование безопасно.
            float worldX = (screenPos.X / GameSetting.WorldTileHalfWidth + screenPos.Y / GameSetting.WorldTileHalfHeight) / 2f;
            float worldY = (screenPos.Y / GameSetting.WorldTileHalfHeight - screenPos.X / GameSetting.WorldTileHalfWidth) / 2f;
            return new Point((int)worldX, (int)worldY);
        }
    }
}