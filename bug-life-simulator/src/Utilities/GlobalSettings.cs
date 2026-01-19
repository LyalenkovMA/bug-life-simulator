using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.IO;
using TalesFromTheUnderbrush.src;
using TalesFromTheUnderbrush.src.Graphics;


namespace TalesFromTheUnderbrush
{
    public static class GlobalSettings
    {
        public static string GameTitle { get; private set; } = "Bug Life Simulator";
        public static int TargetFPS { get; private set; } = 60;
        public static bool FullScreen { get; private set; } = false;
        public static int ScreenWidth { get; private set; } = 1280;
        public static int ScreenHeight { get; private set; } = 720;

        // Сервисы (инициализируются один раз в начале)
        public static AssetManager Assets { get; private set; }
        public static RenderManager Renderer { get; private set; }

        /// <summary>Глобальный режим отладки</summary>
        public static bool DebugMode { get; set; } = true; // По умолчанию включен для разработки

        /// <summary>Показывать FPS</summary>
        public static bool ShowFPS { get; set; } = true;

        /// <summary>Показывать координаты мыши</summary>
        public static bool ShowMouseCoordinates { get; set; } = true;

        /// <summary>Показывать отладочную информацию об объектах</summary>
        public static bool ShowObjectDebugInfo { get; set; } = true;

        /// <summary>Показывать границы коллизий</summary>
        public static bool ShowCollisionBounds { get; set; } = false;

        /// <summary>Показывать границы тайлов</summary>
        public static bool ShowTileDebug { get; set; } = false;

        /// <summary>Показывать SpatialGrid (чанки)</summary>
        public static bool ShowSpatialGrid { get; set; } = false;

        /// <summary>Показывать информацию о камере</summary>
        public static bool ShowCameraInfo { get; set; } = true;

        /// <summary>Показывать информацию о мире</summary>
        public static bool ShowWorldInfo { get; set; } = true;

        /// <summary>Показывать путь поиска (pathfinding)</summary>
        public static bool ShowPathfinding { get; set; } = false;

        /// <summary>Показывать AI состояния</summary>
        public static bool ShowAIStates { get; set; } = false;

        /// <summary>Включить "бог-режим" (неуязвимость и т.д.)</summary>
        public static bool GodMode { get; set; } = false;

        /// <summary>Быстрое выполнение (ускоренная симуляция)</summary>
        public static bool FastForward { get; set; } = false;

        /// <summary>Пропускать рендеринг (только логика)</summary>
        public static bool SkipRendering { get; set; } = false;

        /// <summary>Логировать все события</summary>
        public static bool LogEverything { get; set; } = false;

        // === НАСТРОЙКИ ПРОИЗВОДИТЕЛЬНОСТИ ===

        /// <summary>Лимит сущностей на экране</summary>
        public static int EntityLimit { get; set; } = 1000;

        /// <summary>Лимит FPS (0 = без ограничений)</summary>
        public static int FPSLimit { get; set; } = 0;

        /// <summary>Размер чанков в SpatialGrid</summary>
        public static int SpatialGridChunkSize { get; set; } = 64;

        /// <summary>Размер чанков в TileGrid</summary>
        public static int TileGridChunkSize { get; set; } = 16;

        // === НАСТРОЙКИ ГЕЙМПЛЕЯ (для тестирования) ===

        /// <summary>Бесконечные ресурсы</summary>
        public static bool InfiniteResources { get; set; } = false;

        /// <summary>Мгновенное строительство</summary>
        public static bool InstantBuild { get; set; } = false;

        /// <summary>Отключить врагов</summary>
        public static bool NoEnemies { get; set; } = false;

        /// <summary>Отключить потребности (голод, сон и т.д.)</summary>
        public static bool NoNeeds { get; set; } = false;

        /// <summary>Начальный уровень игрока</summary>
        public static int StartingLevel { get; set; } = 1;

        // === МЕТОДЫ УПРАВЛЕНИЯ ===

        /// <summary>Включить все опции отладки</summary>
        public static void EnableAllDebug()
        {
            DebugMode = true;
            ShowFPS = true;
            ShowMouseCoordinates = true;
            ShowObjectDebugInfo = true;
            ShowCollisionBounds = true;
            ShowTileDebug = true;
            ShowSpatialGrid = true;
            ShowCameraInfo = true;
            ShowWorldInfo = true;
            ShowPathfinding = true;
            ShowAIStates = true;
            LogEverything = true;
        }

        /// <summary>Выключить все опции отладки</summary>
        public static void DisableAllDebug()
        {
            DebugMode = false;
            ShowFPS = false;
            ShowMouseCoordinates = false;
            ShowObjectDebugInfo = false;
            ShowCollisionBounds = false;
            ShowTileDebug = false;
            ShowSpatialGrid = false;
            ShowCameraInfo = false;
            ShowWorldInfo = false;
            ShowPathfinding = false;
            ShowAIStates = false;
            LogEverything = false;
        }

        /// <summary>Переключить режим отладки</summary>
        public static void ToggleDebugMode()
        {
            DebugMode = !DebugMode;
            Console.WriteLine($"[GlobalSettings] DebugMode = {DebugMode}");
        }

        /// <summary>Переключить конкретную настройку отладки</summary>
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

            Console.WriteLine($"[GlobalSettings] {settingName} = {GetDebugSettingValue(settingName)}");
        }

        /// <summary>Получить значение настройки отладки</summary>
        public static bool GetDebugSettingValue(string settingName)
        {
            return settingName.ToLower() switch
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
        }

        /// <summary>Сохранить настройки в файл</summary>
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

                Console.WriteLine($"[GlobalSettings] Настройки сохранены в {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GlobalSettings] Ошибка сохранения: {ex.Message}");
            }
        }

        /// <summary>Загрузить настройки из файла</summary>
        public static void LoadFromFile(string filePath = "settings.json")
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"[GlobalSettings] Файл настроек не найден: {filePath}");
                    return;
                }

                string json = File.ReadAllText(filePath);
                var settings = JsonConvert.DeserializeObject<dynamic>(json);

                if (settings != null)
                {
                    // Загружаем настройки отладки
                    DebugMode = settings.DebugMode ?? DebugMode;
                    ShowFPS = settings.ShowFPS ?? ShowFPS;
                    // ... загрузить все остальные свойства

                    Console.WriteLine($"[GlobalSettings] Настройки загружены из {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GlobalSettings] Ошибка загрузки: {ex.Message}");
            }
        }

        /// <summary>Сбросить настройки к значениям по умолчанию</summary>
        public static void ResetToDefaults()
        {
            DebugMode = true;
            ShowFPS = true;
            ShowMouseCoordinates = true;
            ShowObjectDebugInfo = true;
            ShowCollisionBounds = false;
            ShowTileDebug = false;
            ShowSpatialGrid = false;
            ShowCameraInfo = true;
            ShowWorldInfo = true;
            ShowPathfinding = false;
            ShowAIStates = false;
            GodMode = false;
            FastForward = false;
            SkipRendering = false;
            LogEverything = false;
            EntityLimit = 1000;
            FPSLimit = 0;
            SpatialGridChunkSize = 64;
            TileGridChunkSize = 16;
            InfiniteResources = false;
            InstantBuild = false;
            NoEnemies = false;
            NoNeeds = false;
            StartingLevel = 1;

            Console.WriteLine("[GlobalSettings] Настройки сброшены к значениям по умолчанию");
        }

        /// <summary>Получить строку с текущими настройками отладки</summary>
        public static string GetDebugInfoString()
        {
            return $"Debug: {(DebugMode ? "ON" : "OFF")}\n" +
                   $"FPS: {(ShowFPS ? "SHOW" : "HIDE")}\n" +
                   $"Camera: {(ShowCameraInfo ? "SHOW" : "HIDE")}\n" +
                   $"World: {(ShowWorldInfo ? "SHOW" : "HIDE")}\n" +
                   $"Tiles: {(ShowTileDebug ? "SHOW" : "HIDE")}\n" +
                   $"Grid: {(ShowSpatialGrid ? "SHOW" : "HIDE")}";
        }


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