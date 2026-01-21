using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TalesFromTheUnderbrush.src.Core.Entities;
using TalesFromTheUnderbrush.src.Core.Tiles;
using TalesFromTheUnderbrush.src.Graphics.Tiles;
using TalesFromTheUnderbrush.src.UI.Camera;
using Color = Microsoft.Xna.Framework.Color;
using IDrawable = TalesFromTheUnderbrush.src.Graphics.IDrawable;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TalesFromTheUnderbrush.src.GameLogic
{
    /// <summary>
    /// Основной класс мира, управляющий тайлами и сущностями
    /// </summary>
    public class World : IUpdatable, IDrawable, IPersistable
    {
        // === ПОЛЯ ===
        private readonly TileGrid _tileGrid;
        private readonly SpatialGrid<Entity> _spatialGrid;
        private readonly List<GameEntity> _gameEntities;
        private readonly List<StaticEntity> _staticEntities;

        private readonly Random _random;
        private WorldState _worldState;

        public event EventHandler UpdateOrderChanged;
        public event EventHandler DrawDepthChanged;
        public event EventHandler VisibleChanged;
        public GameTime GameTimeWorld;
        public event Action<IPersistable> OnBeforeSave;
        public event Action<IPersistable> OnAfterLoad;

        // === СВОЙСТВА ===
        public string Name { get; private set; }
        public int Width => _tileGrid?.Width ?? 0;
        public int Height => _tileGrid?.Height ?? 0;
        public int Depth => _tileGrid?.Depth ?? 0;

        public TileGrid TileGrid => _tileGrid;
        public SpatialGrid<Entity> SpatialGrid => _spatialGrid;
        public WorldState State => _worldState;

        public int UpdateOrder => throw new NotImplementedException();

        public float DrawDepth => throw new NotImplementedException();

        public bool Visible => throw new NotImplementedException();

        public string PersistentId => throw new NotImplementedException();

        public string PersistentType => throw new NotImplementedException();

        public bool ShouldSave => throw new NotImplementedException();

        // === КОНСТРУКТОРЫ ===

        /// <summary>
        /// Создать новый пустой мир
        /// </summary>
        public World(string name, int width, int height, int depth = 1)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("World name cannot be empty");

            if (width <= 0 || height <= 0 || depth <= 0)
                throw new ArgumentException("World dimensions must be positive");

            Name = name;
            _tileGrid = new TileGrid(width, height, depth);
            _spatialGrid = new SpatialGrid<Entity>(width, height, 64); // Чанки по 64x64
            _gameEntities = new List<GameEntity>();
            _staticEntities = new List<StaticEntity>();
            _random = new Random();
            _worldState = new WorldState();
            GameTimeWorld = new GameTime();

            Console.WriteLine($"[World] Создан мир '{name}' размером {width}x{height}x{depth}");
        }

        /// <summary>
        /// Создать мир из данных сохранения
        /// </summary>
        public World(PersistenceData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Загружаем из данных сохранения
            Name = data.GetValue<string>("Name");
            int width = data.GetValue<int>("Width");
            int height = data.GetValue<int>("Height");
            int depth = data.GetValue<int>("Depth", 1);

            _tileGrid = new TileGrid(width, height, depth);
            _spatialGrid = new SpatialGrid<Entity>(width, height, 64);
            _gameEntities = new List<GameEntity>();
            _staticEntities = new List<StaticEntity>();
            _random = new Random();
            _worldState = new WorldState();

            // Загружаем тайлы
            LoadTilesFromData(data);

            Console.WriteLine($"[World] Загружен мир '{Name}' из сохранения");
        }

        // === ОСНОВНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Обновление состояния мира
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (_worldState.CurrentState == WorldState.StateType.Paused)
                return;

            // Обновляем время мира
            _worldState.UpdateTime(gameTime);

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Применяем модификаторы из состояния мира
            float effectiveDeltaTime = deltaTime * _worldState.TimeScale;

            // Обновляем все игровые сущности
            for (int i = _gameEntities.Count - 1; i >= 0; i--)
            {
                var entity = _gameEntities[i];

                if (entity.IsActive)
                {
                    entity.Update(gameTime);

                    // Проверяем, не нужно ли удалить сущность
                    if (entity.ShouldBeRemoved)
                    {
                        RemoveGameEntity(entity);
                        i--; // Корректируем индекс после удаления
                    }
                }
            }

            // Обновляем статические сущности (реже)
            if (gameTime.TotalGameTime.Milliseconds % 100 == 0) // Каждые 100 мс
            {
                foreach (var entity in _staticEntities)
                {
                    if (entity.IsActive)
                    {
                        entity.Update(gameTime);
                    }
                }
            }

            // Обновляем SpatialGrid (позиции сущностей)
            UpdateSpatialGrid();
        }

        /// <summary>
        /// Отрисовка мира
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, ICamera camera)
        {
            if (camera == null)
            {
                Draw(spriteBatch);
                return;
            }

            // Получаем видимую область через камеру
            Rectangle visibleBounds = GetVisibleBounds(camera);

            // Отрисовываем только видимые чанки
            List<TileChunk> visibleChunks = _tileGrid?.GetChunksInArea(visibleBounds) ?? new List<TileChunk>();

            foreach (TileChunk chunk in visibleChunks)
            {
                chunk.Draw(spriteBatch);
            }

            // Отрисовываем сущности в видимой области
            List<Entity> visibleEntities = GetEntitiesInArea(visibleBounds);
            foreach (Entity entity in visibleEntities)
            {
                if (entity.IsVisible)
                {
                    entity.Draw(GameTimeWorld);
                }
            }

            // Отрисовка отладочной информации
            if (GlobalSettings.DebugMode)
            {
                DrawDebugInfo(spriteBatch, visibleBounds, visibleChunks.Count, visibleEntities.Count);
            }
        }

        /// <summary>
        /// Отрисовка мира БЕЗ камеры (простая версия)
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Отрисовка тайлов (всех)
            _tileGrid?.Draw(spriteBatch);

            // Отрисовка всех игровых сущностей
            foreach (GameEntity entity in _gameEntities)
            {
                if (entity.IsVisible)
                {
                    entity.Draw(GameTimeWorld);
                }
            }

            // Отрисовка всех статических сущностей
            foreach (StaticEntity entity in _staticEntities)
            {
                if (entity.IsVisible)
                {
                    entity.Draw(GameTimeWorld);
                }
            }

            // Отладочная информация (если включен режим отладки)
            if (GlobalSettings.DebugMode)
            {
                DrawDebugOverlay(spriteBatch);
            }
        }

        // === РАБОТА С СУЩНОСТЯМИ ===

        /// <summary>
        /// Добавить игровую сущность в мир
        /// </summary>
        public void AddGameEntity(GameEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _gameEntities.Add(entity);
            _spatialGrid.Add(entity);

            //entity = this; // Связываем сущность с миром

            Console.WriteLine($"[World] Добавлена игровая сущность: {entity.Name}");
        }

        /// <summary>
        /// Добавить статическую сущность в мир
        /// </summary>
        public void AddStaticEntity(StaticEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _staticEntities.Add(entity);
            _spatialGrid.Add(entity);

            entity.World = this;

            Console.WriteLine($"[World] Добавлена статическая сущность: {entity.Name}");
        }

        /// <summary>
        /// Удалить игровую сущность из мира
        /// </summary>
        public bool RemoveGameEntity(GameEntity entity)
        {
            if (entity == null) return false;

            bool removed = _gameEntities.Remove(entity);
            if (removed)
            {
                _spatialGrid.Remove(entity);
                entity.World = null; // Разрываем связь

                Console.WriteLine($"[World] Удалена игровая сущность: {entity.Name}");
            }

            return removed;
        }

        /// <summary>
        /// Удалить статическую сущность из мира
        /// </summary>
        public bool RemoveStaticEntity(StaticEntity entity)
        {
            if (entity == null) return false;

            bool removed = _staticEntities.Remove(entity);
            if (removed)
            {
                _spatialGrid.Remove(entity);
                entity.World = null;

                Console.WriteLine($"[World] Удалена статическая сущность: {entity.Name}");
            }

            return removed;
        }

        /// <summary>
        /// Получить сущность по ID
        /// </summary>
        public Entity GetEntityById(Guid id)
        {
            // Ищем в игровых сущностях
            var gameEntity = _gameEntities.FirstOrDefault(e => e.Id == id);
            if (gameEntity != null) return gameEntity;

            // Ищем в статических сущностях
            var staticEntity = _staticEntities.FirstOrDefault(e => e.Id == id);
            return staticEntity;
        }

        /// <summary>
        /// Получить все сущности в области
        /// </summary>
        public List<Entity> GetEntitiesInArea(Rectangle area)
        {
            return _spatialGrid.GetEntitiesInArea(area).ToList();
        }

        /// <summary>
        /// Получить все сущности определённого типа
        /// </summary>
        public List<T> GetEntitiesOfType<T>() where T : Entity
        {
            var result = new List<T>();

            result.AddRange(_gameEntities.OfType<T>());
            result.AddRange(_staticEntities.OfType<T>());

            return result;
        }

        // === РАБОТА С ТАЙЛАМИ ===

        /// <summary>
        /// Получить тайл по координатам
        /// </summary>
        public Tile GetTileAt(int x, int y, int z = 0)
        {
            return _tileGrid?.GetTile(x, y, z);
        }

        /// <summary>
        /// Установить тайл по координатам
        /// </summary>
        public void SetTileAt(int x, int y, int z, Tile tile)
        {
            _tileGrid?.SetTile(x, y, z, tile);
        }

        /// <summary>
        /// Проверить, можно ли пройти в клетку
        /// </summary>
        public bool IsTileWalkable(int x, int y, int z = 0)
        {
            var tile = GetTileAt(x, y, z);
            return tile?.IsWalkable ?? false;
        }

        /// <summary>
        /// Получить все тайлы в области
        /// </summary>
        public List<Tile> GetTilesInArea(Rectangle area)
        {
            return _tileGrid?.GetTilesInArea(area) ?? new List<Tile>();
        }

        // === СОХРАНЕНИЕ/ЗАГРУЗКА ===

        /// <summary>
        /// Сохранить состояние мира
        /// </summary>
        public PersistenceData Save()
        {
            PersistenceData data = new PersistenceData();

            // Основные свойства
            data.SetValue("Name", Name);
            data.SetValue("Width", Width);
            data.SetValue("Height", Height);
            data.SetValue("Depth", Depth);
            data.SetValue("WorldState", _worldState);

            SaveWorldStateToData(data);

            // Сохраняем тайлы
            SaveTilesToData(data);

            // Сохраняем сущности
            SaveEntitiesToData(data);

            Console.WriteLine($"[World] Мир '{Name}' сохранён");
            return data;
        }

        /// <summary>
        /// Получить модификаторы из состояния мира
        /// </summary>
        public (float movement, float combat, float resources) GetWorldModifiers()
        {
            return (_worldState.MovementModifier,
                    _worldState.CombatModifier,
                    _worldState.ResourceModifier);
        }

        /// <summary>
        /// Проверить, сейчас ночь
        /// </summary>
        public bool IsNightTime()
        {
            return _worldState.CurrentState == WorldState.StateType.Night;
        }

        /// <summary>
        /// Получить текущее время мира
        /// </summary>
        public string GetWorldTimeString()
        {
            return _worldState.WorldTime.ToString("HH:mm");
        }

        /// <summary>
        /// Загрузить состояние мира
        /// </summary>
        public void Load(PersistenceData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Name = data.GetValue<string>("Name");
            _worldState = data.GetValue<WorldState>("WorldState", WorldState.Normal);

            // Загружаем тайлы
            LoadTilesFromData(data);

            // Загружаем сущности
            LoadEntitiesFromData(data);

            // Обновляем SpatialGrid
            UpdateSpatialGrid();

            // Загружаем состояние мира
            LoadWorldStateFromData(data);

            Console.WriteLine($"[World] Мир '{Name}' загружен");
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

        private void UpdateSpatialGrid()
        {
            _spatialGrid.Clear();

            // Добавляем все игровые сущности
            foreach (var entity in _gameEntities)
            {
                if (entity.IsActive)
                {
                    _spatialGrid.Add(entity);
                }
            }

            // Добавляем все статические сущности
            foreach (var entity in _staticEntities)
            {
                if (entity.IsActive)
                {
                    _spatialGrid.Add(entity);
                }
            }
        }

        private void DrawDebugInfo(SpriteBatch spriteBatch, Rectangle visibleBounds,
                                 int visibleChunks, int visibleEntities)
        {
            var font = AssetManager.Instance.GetFont("DebugFont");
            if (font == null) return;

            Vector2 position = new Vector2(10, 10);

            string info = $"World: {Name}\n" +
                         $"State: {_worldState}\n" +
                         $"Visible: {visibleBounds.X},{visibleBounds.Y} ({visibleBounds.Width}x{visibleBounds.Height})\n" +
                         $"Chunks: {visibleChunks}, Entities: {visibleEntities}\n" +
                         $"Total Entities: {_gameEntities.Count + _staticEntities.Count}";

            // Фон для текста
            var textSize = font.MeasureString(info);
            spriteBatch.DrawRectangle(
                new Rectangle((int)position.X - 2, (int)position.Y - 2,
                             (int)textSize.X + 4, (int)textSize.Y + 4),
                Color.Black * 0.5f);

            // Текст
            spriteBatch.DrawString(font, info, position, Color.White);

            // Рисуем границу видимой области
            if (camera is OrthographicCamera2_5D camera2_5D)
            {
                Vector2 topLeft = camera2_5D.WorldToScreen(new Vector3(visibleBounds.Left, visibleBounds.Top, 0));
                Vector2 bottomRight = camera2_5D.WorldToScreen(new Vector3(visibleBounds.Right, visibleBounds.Bottom, 0));

                Rectangle screenBounds = new Rectangle(
                    (int)topLeft.X, (int)topLeft.Y,
                    (int)(bottomRight.X - topLeft.X),
                    (int)(bottomRight.Y - topLeft.Y));

                spriteBatch.DrawRectangle(screenBounds, Color.Yellow * 0.3f, 2);
            }
        }

        /// <summary>
        /// Получить видимую область из камеры
        /// </summary>
        private Rectangle GetVisibleAreaFromCamera(ICamera camera)
        {
            // Проверяем тип камеры для оптимального получения области
            if (camera is OrthographicCamera2_5D camera2_5D)
            {
                return camera2_5D.GetVisibleTileBounds();
            }

            // Универсальный способ для любой камеры
            // Преобразуем углы экрана в мировые координаты
            Vector2 screenTopLeft = Vector2.Zero;
            Vector2 screenBottomRight = Vector2.Zero;
            //Vector2 screenBottomRight = new Vector2(
            //    _graphics.PreferredBackBufferWidth,
            //    _graphics.PreferredBackBufferHeight);

            Vector3 worldTopLeft = camera.ScreenToWorld(screenTopLeft, 0);
            Vector3 worldBottomRight = camera.ScreenToWorld(screenBottomRight, 0);

            return new Rectangle(
                (int)Math.Floor(worldTopLeft.X),
                (int)Math.Floor(worldTopLeft.Y),
                (int)Math.Ceiling(worldBottomRight.X - worldTopLeft.X) + 2,
                (int)Math.Ceiling(worldBottomRight.Y - worldTopLeft.Y) + 2
            );
        }

        /// <summary>
        /// Отрисовка только видимых тайлов
        /// </summary>
        private void DrawVisibleTiles(SpriteBatch spriteBatch, ICamera camera, Rectangle visibleArea)
        {
            if (_tileGrid == null) return;

            // Получаем только чанки в видимой области
            var visibleChunks = _tileGrid.GetChunksInArea(visibleArea);

            foreach (var chunk in visibleChunks)
            {
                if (chunk == null) continue;

                // Получаем тайлы из чанка, которые в видимой области
                var tilesInChunk = chunk.GetTilesInArea(visibleArea);

                foreach (var tile in tilesInChunk)
                {
                    if (tile == null || !tile.IsVisible) continue;

                    // Преобразуем мировые координаты в экранные
                    Vector2 screenPosition = camera.WorldToScreen(tile.WorldPosition);

                    // Вычисляем глубину для сортировки
                    float depth = CalculateTileDepth(tile.WorldPosition);

                    // Отрисовываем тайл
                    spriteBatch.Draw(
                        tile.Texture,
                        screenPosition,
                        tile.SourceRectangle,
                        tile.TintColor,
                        0f,
                        Vector2.Zero,
                        Vector2.One,
                        SpriteEffects.None,
                        depth
                    );

                    // Отрисовка дебаг-информации для тайла
                    if (GlobalSettings.ShowTileDebug)
                    {
                        DrawTileDebug(spriteBatch, tile, screenPosition);
                    }
                }
            }
        }

        /// <summary>
        /// Отрисовка только видимых сущностей
        /// </summary>
        private void DrawVisibleEntities(SpriteBatch spriteBatch, ICamera camera, Rectangle visibleArea)
        {
            // Получаем сущности в видимой области через SpatialGrid
            var visibleEntities = _spatialGrid.GetEntitiesInArea(visibleArea);

            // Сортируем сущности по глубине для корректного отображения
            var sortedEntities = visibleEntities
                .Where(e => e.IsVisible)
                .OrderBy(e => CalculateEntityDepth(e))
                .ToList();

            foreach (var entity in sortedEntities)
            {
                // Для Entity без собственной отрисовки используем базовую
                if (entity is IDrawable drawableEntity)
                {
                    drawableEntity.Draw(GameTimeWorld ,spriteBatch);
                }
                else
                {
                    // Базовая отрисовка для Entity
                    DrawEntityBasic(spriteBatch, camera, entity);
                }
            }
        }

        /// <summary>
        /// Базовая отрисовка сущности (если нет собственной реализации)
        /// </summary>
        private void DrawEntityBasic(SpriteBatch spriteBatch, ICamera camera, Entity entity)
        {
            if (entity.Texture == null) return;

            Vector2 screenPosition = camera.WorldToScreen(entity.Position);
            float depth = CalculateEntityDepth(entity);

            spriteBatch.Draw(
                entity.Texture,
                screenPosition,
                null,
                Color.White,
                0f,
                new Vector2(entity.Texture.Width / 2, entity.Texture.Height / 2),
                Vector2.One,
                SpriteEffects.None,
                depth
            );
        }

        /// <summary>
        /// Вычисление глубины для тайла (Z-order для 2.5D)
        /// </summary>
        private float CalculateTileDepth(Vector3 worldPosition)
        {
            // Формула для изометрической 2.5D: чем дальше/ниже, тем меньше глубина
            // Normalize to 0-1 range
            return 1.0f - ((worldPosition.Y + worldPosition.X) * 0.001f + worldPosition.Z * 0.0001f);
        }

        /// <summary>
        /// Вычисление глубины для сущности
        /// </summary>
        private float CalculateEntityDepth(Entity entity)
        {
            // Сущности отрисовываются поверх тайлов на той же высоте
            return 1.0f - ((entity.Position.Y + entity.Position.X) * 0.001f +
                          entity.Position.Z * 0.0001f) - 0.00001f;
        }

        // === ОТЛАДОЧНАЯ ОТРИСОВКА ===

        /// <summary>
        /// Общая отладочная информация (без камеры)
        /// </summary>
        private void DrawDebugOverlay(SpriteBatch spriteBatch)
        {
            var font = AssetManager.Instance?.GetFont("DebugFont");
            if (font == null) return;

            string debugText = $"World: {Name}\n" +
                              $"State: {_worldState}\n" +
                              $"Entities: {_gameEntities.Count + _staticEntities.Count}\n" +
                              $"Time: {GetWorldTimeString()}";

            spriteBatch.DrawString(font, debugText, new Vector2(10, 10), Color.White);
        }

        /// <summary>
        /// Отладочная информация с камерой
        /// </summary>
        private void DrawCameraDebugInfo(SpriteBatch spriteBatch, ICamera camera, Rectangle visibleArea)
        {
            var font = AssetManager.Instance?.GetFont("DebugFont");
            if (font == null) return;

            // Собираем только включенную отладочную информацию
            List<string> debugLines = new List<string>();

            if (GlobalSettings.ShowWorldInfo)
            {
                debugLines.Add($"World: {Name}");
                debugLines.Add($"State: {_worldState}");
                debugLines.Add($"Entities: {_gameEntities.Count + _staticEntities.Count}");
                debugLines.Add($"Time: {GetWorldTimeString()}");
            }

            if (GlobalSettings.ShowCameraInfo && camera != null)
            {
                debugLines.Add($"Camera: {camera.Position.X:F0},{camera.Position.Y:F0},{camera.Position.Z:F0}");
                debugLines.Add($"Visible: {visibleArea.Width}x{visibleArea.Height}");
                debugLines.Add($"Entities in view: {_spatialGrid?.GetEntitiesInArea(visibleArea).Count ?? 0}");
            }

            if (GlobalSettings.ShowTileDebug && _tileGrid != null)
            {
                debugLines.Add($"Tiles: {_tileGrid.Width}x{_tileGrid.Height}x{_tileGrid.Depth}");
                debugLines.Add($"Chunks: {_tileGrid.ChunksWidth}x{_tileGrid.ChunksHeight}");
            }

            if (debugLines.Count == 0) return;

            string debugText = string.Join("\n", debugLines);

            // Фон для текста
            var textSize = font.MeasureString(debugText);
            var backgroundRect = new Rectangle(5, 5, (int)textSize.X + 10, (int)textSize.Y + 10);

            spriteBatch.DrawRectangle(backgroundRect, Color.Black * 0.7f);
            spriteBatch.DrawString(font, debugText, new Vector2(10, 10), Color.White);

            // Визуализация если включено
            if (GlobalSettings.ShowSpatialGrid)
            {
                DrawSpatialGridDebug(spriteBatch, camera);
            }

            if (GlobalSettings.ShowTileDebug)
            {
                DrawTileGridDebug(spriteBatch, camera, visibleArea);
            }
        }

        /// <summary>
        /// Отрисовка границ видимой области
        /// </summary>
        private void DrawVisibleAreaBounds(SpriteBatch spriteBatch, ICamera camera, Rectangle visibleArea)
        {
            // Преобразуем углы области в экранные координаты
            Vector2[] corners = new Vector2[]
            {
                camera.WorldToScreen(new Vector3(visibleArea.Left, visibleArea.Top, 0)),
                camera.WorldToScreen(new Vector3(visibleArea.Right, visibleArea.Top, 0)),
                camera.WorldToScreen(new Vector3(visibleArea.Right, visibleArea.Bottom, 0)),
                camera.WorldToScreen(new Vector3(visibleArea.Left, visibleArea.Bottom, 0))
            };

            // Рисуем линии границы
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 start = corners[i];
                Vector2 end = corners[(i + 1) % corners.Length];

                DrawLine(spriteBatch, start, end, Color.Yellow * 0.5f, 2);
            }
        }

        /// <summary>
        /// Отладочная отрисовка тайла
        /// </summary>
        private void DrawTileDebug(SpriteBatch spriteBatch, Tile tile, Vector2 screenPosition)
        {
            // Рамка вокруг тайла
            Rectangle tileRect = new Rectangle(
                (int)screenPosition.X,
                (int)screenPosition.Y,
                tile.Texture?.Width ?? 64,
                tile.Texture?.Height ?? 64);

            spriteBatch.DrawRectangle(tileRect, Color.Red * 0.3f, 1);

            // Координаты тайла
            var font = AssetManager.Instance?.GetSmallFont();
            if (font != null)
            {
                string coordText = $"{tile.GridPosition.X},{tile.GridPosition.Y},{tile.Height}";
                spriteBatch.DrawString(font, coordText,
                    new Vector2(screenPosition.X + 2, screenPosition.Y + 2),
                    Color.White * 0.8f);
            }
        }

        /// <summary>
        /// Вспомогательный метод для рисования линии
        /// </summary>
        private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness = 1f)
        {
            float length = Vector2.Distance(start, end);
            float angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);

            // Используем белую текстуру 1x1
            var pixel = AssetManager.Instance?.GetPixelTexture();
            if (pixel == null) return;

            spriteBatch.Draw(pixel,
                start,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(length, thickness),
                SpriteEffects.None,
                0f);
        }

        // === ДОПОЛНИТЕЛЬНЫЙ МЕТОД ДЛЯ GameManager ===

        /// <summary>
        /// Отрисовка мира с предустановленными настройками (для GameManager)
        /// </summary>
        public void DrawWithCamera(SpriteBatch spriteBatch, ICamera camera, bool enableDebug = false)
        {
            // Сохраняем текущее состояние отладки
            bool oldDebugMode = GlobalSettings.DebugMode;

            if (enableDebug)
            {
                GlobalSettings.DebugMode = true;
            }

            // Вызываем соответствующую перегрузку
            if (camera != null)
            {
                Draw(spriteBatch, camera);
            }
            else
            {
                Draw(spriteBatch);
            }

            // Восстанавливаем состояние отладки
            if (enableDebug)
            {
                GlobalSettings.DebugMode = oldDebugMode;
            }
        }
    }

        private Rectangle GetVisibleBounds(ICamera camera)
        {
            if (camera is OrthographicCamera2_5D camera2_5D)
            {
                // Используем метод из камеры 2.5D
                return camera2_5D.GetVisibleTileBounds();
            }

            // Резервный вариант для других камер
            Vector3 camPos = camera.Position;
            int visibleRange = 15; // Тайлов в каждую сторону

            // Учитываем зум камеры
            if (camera is CameraBase cameraBase)
            {
                // Можно получить зум через рефлексию или добавить свойство в ICamera
                visibleRange = (int)(visibleRange / cameraBase.Zoom);
            }

            return new Rectangle(
                (int)camPos.X - visibleRange,
                (int)camPos.Y - visibleRange,
                visibleRange * 2,
                visibleRange * 2
            );
        }

        private void SaveTilesToData(PersistenceData data)
        {
            if (_tileGrid == null) return;

            var tilesData = new List<PersistenceData>();

            for (int z = 0; z < Depth; z++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        var tile = _tileGrid.GetTile(x, y, z);
                        if (tile != null)
                        {
                            tilesData.Add(tile.Save());
                        }
                    }
                }
            }

            data.SetValue("Tiles", tilesData);
        }

        private void LoadTilesFromData(PersistenceData data)
        {
            if (_tileGrid == null || !data.HasValue("Tiles")) return;

            List<PersistenceData> tilesData = data.GetValue<List<PersistenceData>>("Tiles");
            foreach (PersistenceData tileData in tilesData)
            {
                Tile tile = new Tile(tileData);
                _tileGrid.SetTile(tile.GridPosition.X, tile.GridPosition.Y, tile.Height, tile);
            }
        }

        private void SaveEntitiesToData(PersistenceData data)
        {
            var entitiesData = new List<PersistenceData>();

            // Сохраняем игровые сущности
            foreach (var entity in _gameEntities)
            {
                entitiesData.Add(entity.Save());
            }

            // Сохраняем статические сущности
            foreach (var entity in _staticEntities)
            {
                entitiesData.Add(entity.Save());
            }

            data.SetValue("Entities", entitiesData);
        }

        private void LoadEntitiesFromData(PersistenceData data)
        {
            if (!data.HasValue("Entities")) return;

            _gameEntities.Clear();
            _staticEntities.Clear();

            var entitiesData = data.GetValue<List<PersistenceData>>("Entities");
            foreach (var entityData in entitiesData)
            {
                string entityType = entityData.GetValue<string>("EntityType");

                switch (entityType)
                {
                    case "GameEntity":
                        var gameEntity = new GameEntity(entityData);
                        AddGameEntity(gameEntity);
                        break;

                    case "StaticEntity":
                        var staticEntity = new StaticEntity(entityData);
                        AddStaticEntity(staticEntity);
                        break;

                    default:
                        Console.WriteLine($"[World] Неизвестный тип сущности: {entityType}");
                        break;
                }
            }
        }

        // === УТИЛИТЫ ===

        /// <summary>
        /// Изменить состояние мира
        /// </summary>
        public void SetWorldState(WorldState.StateType state, float timeScale = 1.0f)
        {
            _worldState.SetState(state, timeScale);
            Console.WriteLine($"[World] Состояние мира изменено: {_worldState}");
        }

        /// <summary>
        /// Очистить мир
        /// </summary>
        public void Clear()
        {
            _gameEntities.Clear();
            _staticEntities.Clear();
            _spatialGrid.Clear();
            _tileGrid?.Clear();

            Console.WriteLine($"[World] Мир '{Name}' очищен");
        }

        /// <summary>
        /// Для отладки
        /// </summary>
        public override string ToString()
        {
            return $"World '{Name}' ({Width}x{Height}x{Depth}), Entities: {_gameEntities.Count + _staticEntities.Count}";
        }

        public void SetUpdateOrder(int order)
        {
            throw new NotImplementedException();
        }

        public void SetVisible(bool visible)
        {
            throw new NotImplementedException();
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            throw new NotImplementedException();
        }

        public void SetDrawDepth(float depth)
        {
            throw new NotImplementedException();
        }

        private void SaveWorldStateToData(PersistenceData data)
        {
            data.SetValue("WorldState", _worldState.Save());
        }

        private void LoadWorldStateFromData(PersistenceData data)
        {
            if (data.HasValue("WorldState"))
            {
                var stateData = data.GetValue<PersistenceData>("WorldState");
                _worldState.Load(stateData);
            }
        }
    }
}
