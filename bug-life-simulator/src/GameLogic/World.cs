using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TalesFromTheUnderbrush.src.Core.Entities;
using TalesFromTheUnderbrush.src.Graphics;
using TalesFromTheUnderbrush.src.Graphics.Tiles;
using TalesFromTheUnderbrush.src.UI.Camera;
using Color = Microsoft.Xna.Framework.Color;
using IDrawable = TalesFromTheUnderbrush.src.Graphics.IDrawable;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TalesFromTheUnderbrush.src.GameLogic
{
    public class World : IDisposable
    {
        // === Основные свойства ===
        public string Name { get; private set; }
        public GameTime GameTimeWorld { get; private set; }

        // === Системы ===
        private TileGrid _tileGrid;
        private SpatialGrid<Entity> _spatialGrid;
        private readonly Dictionary<ulong, Entity> _entities = new();

        // === Статистика ===
        public int EntityCount => _entities.Count;
        public int ActiveEntityCount => _entities.Values.Count(e => e.IsActive);
        public int VisibleEntityCount => _entities.Values.Count(e => e.IsVisible);

        // === События ===
        public event Action<Entity> EntityAdded;
        public event Action<Entity> EntityRemoved;
        public event Action<World> WorldUpdated;

        // === Конструктор ===
        public World(string name, int width = 100, int height = 100)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("World name cannot be empty");

            Name = name;

            // Инициализируем SpatialGrid с учетом размеров тайлов
            int worldWidthPixels = width * Tile.TileSize.Width;
            int worldHeightPixels = height * Tile.TileSize.Height;
            int cellSize = Math.Max(Tile.TileSize.Width, Tile.TileSize.Height);

            _spatialGrid = new SpatialGrid<Entity>(worldWidthPixels, worldHeightPixels, cellSize);

            // Создаем TileGrid
            _tileGrid = new TileGrid(width, height, 1, 16); // 1 слой, размер чанка 16

            // Создаем несколько тестовых тайлов для визуализации
            InitializeTestTiles(width, height);

            Console.WriteLine($"[World] Создан мир '{name}' размером {width}x{height}");
        }

        // === Инициализация тестовых тайлов ===
        private void InitializeTestTiles(int width, int height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    TileType type = (x + y) % 2 == 0 ? TileType.Grass : TileType.Dirt;
                    Color color = type == TileType.Grass ? Color.Green : Color.SaddleBrown;

                    // Создаем через TileGrid
                    _tileGrid.CreateTile(x, y, 0, type, color);
                }
            }
        }

        // Временный метод для создания базового тайла
        private Tile CreateBasicTile(int x, int y, int layer, TileType type)
        {

            // Создаем конкретный тип тайла
            Tile tile = type switch
            {
                TileType.Grass => new GrassTile(new Point(x, y), layer),
            //    TileType.Dirt => new DirtTile(new Point(x, y), layer), // Нужно создать
            //    TileType.Water => new WaterTile(new Point(x, y), layer), // Нужно создать
            //    TileType.Stone => new StoneTile(new Point(x, y), layer), // Нужно создать
            //    _ => new GrassTile(new Point(x, y), layer) // По умолчанию
            };

            return tile;
        }

        // === Управление сущностями ===
        public void AddEntity(Entity entity)
        {
            if (entity == null || _entities.ContainsKey(entity.Id))
                return;

            // Вычисляем границы для SpatialGrid
            var bounds = entity.GetBounds();
            var rectangleF = new System.Drawing.RectangleF(
                bounds.X, bounds.Y, bounds.Width, bounds.Height
            );

            // Добавляем в системы
            _spatialGrid.Add(entity, rectangleF);
            _entities[entity.Id] = entity;
            entity.World = this;

            // Уведомляем
            EntityAdded?.Invoke(entity);

            Console.WriteLine($"[World] Добавлена сущность: {entity.Name} (ID: {entity.Id})");
        }

        public bool RemoveEntity(ulong entityId)
        {
            if (!_entities.TryGetValue(entityId, out var entity))
                return false;

            return RemoveEntity(entity);
        }

        public bool RemoveEntity(Entity entity)
        {
            if (entity == null || !_entities.ContainsKey(entity.Id))
                return false;

            // Удаляем из систем
            _spatialGrid.Remove(entity);
            _entities.Remove(entity.Id);
            entity.World = null;

            // Уведомляем
            EntityRemoved?.Invoke(entity);

            Console.WriteLine($"[World] Удалена сущность: {entity.Name} (ID: {entity.Id})");
            return true;
        }

        public Entity GetEntityById(ulong id)
        {
            _entities.TryGetValue(id, out var entity);
            return entity;
        }

        // === Поиск сущностей ===
        public List<Entity> GetEntitiesInArea(Rectangle area)
        {
            // Преобразуем Rectangle в RectangleF для SpatialGrid
            var rectF = new RectangleF(area.X, area.Y, area.Width, area.Height);
            return _spatialGrid.Query(rectF).Cast<Entity>().ToList();
        }

        public List<Entity> GetEntitiesInArea(System.Drawing.RectangleF area)
        {
            return _spatialGrid.Query(area).Cast<Entity>().ToList();
        }

        // === Работа с тайлами ===
        public Tile GetTileAt(int x, int y, int z = 0)
        {
            return _tileGrid?.GetTile(x, y, z);
        }

        public void SetTileAt(int x, int y, int z, Tile tile)
        {
            // Все операции через TileGrid
            _tileGrid?.SetTile(x, y, z, tile);
        }

        public bool IsTileWalkable(int x, int y, int z = 0)
        { // Используем TileGrid
            return _tileGrid?.IsWalkable(x, y) ?? false;
        }

        // === Обновление ===
        public void Update(GameTime gameTime)
        {
            GameTimeWorld = gameTime;

            // Обновляем тайлы через TileGrid
            _tileGrid.Update(gameTime);

            // Обновляем все активные сущности
            List<Entity> activeEntities = _entities.Values.Where(e => e.IsActive).ToList();
            foreach (Entity entity in activeEntities)
            {
                try
                {
                    entity.Update(gameTime);

                    // Проверяем, нужно ли удалить сущность
                    if (entity.ShouldBeRemoved)
                    {
                        RemoveEntity(entity);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[World] Ошибка при обновлении сущности {entity.Name}: {ex.Message}");
                }
            }

            // Обновляем тайлы
            _tileGrid?.Update(gameTime);

            // Уведомляем подписчиков
            WorldUpdated?.Invoke(this);
        }

        // === Отрисовка ===
        public void Draw(SpriteBatch spriteBatch, ICamera camera = null)
        {
            if (spriteBatch == null) return;

            if (camera == null)
            {
                // Отрисовка без камеры (весь мир)
                Draw(spriteBatch);
                return;
            }

            // Получаем видимую область через камеру
            Rectangle visibleBounds = GetVisibleBounds(camera);

            // 1. Отрисовываем тайлы в видимой области
            if (_tileGrid != null)
            {
                // Получаем видимые чанки
                List<TileChunk> visibleChunks = _tileGrid.GetChunksInArea(visibleBounds);

                // Устанавливаем SpriteBatch для чанков и отрисовываем
                foreach (TileChunk chunk in visibleChunks)
                {
                    if (chunk is IDrawable drawableChunk)
                    {
                        // Для TileChunk нужен особый подход, так как он требует SpriteBatch
                        if (chunk is TileChunk tileChunk)
                        {
                            tileChunk.SetSpriteBatch(spriteBatch);
                        }

                        if (drawableChunk.Visible)
                        {
                            drawableChunk.Draw(GameTimeWorld);
                        }
                    }
                }
            }

            // 2. Отрисовываем сущности в видимой области
            // Устанавливаем SpriteBatch для всех видимых сущностей
            var visibleEntities = GetEntitiesInArea(visibleBounds);

            foreach (var entity in visibleEntities)
            {
                if (entity is IRequiresSpriteBatch requiresBatch)
                {
                    requiresBatch.SetSpriteBatch(spriteBatch);
                }

                if (entity.Visible)
                {
                    entity.Draw(GameTimeWorld);
                }
            }

            // 3. Отладочная информация
            if (GlobalSettings.DebugMode)
            {
                DrawDebugInfo(spriteBatch, visibleBounds);
            }
        }

        // Перегруженный метод для отрисовки без камеры
        public void Draw(SpriteBatch spriteBatch)
        {
            if (spriteBatch == null) return;

            // Отрисовываем все тайлы
            _tileGrid?.Draw(spriteBatch);

            // Отрисовываем все сущности
            foreach (var entity in _entities.Values.Where(e => e.IsVisible))
            {
                entity.Draw(GameTimeWorld);
            }
        }

        // === Вспомогательные методы ===
        private Rectangle GetVisibleBounds(ICamera camera)
        {
            if (camera == null)
                return new Rectangle(0, 0, 800, 600); // Дефолтные размеры

            var cameraBounds = camera.Bounds;
            return new Rectangle(
                (int)cameraBounds.X,
                (int)cameraBounds.Y,
                (int)cameraBounds.Width,
                (int)cameraBounds.Height
            );
        }

        private void DrawDebugInfo(SpriteBatch spriteBatch, Rectangle visibleBounds)
        {
            // Простая отладочная информация
            string debugText = $"World: {Name}\n" +
                             $"Entities: {EntityCount} (Active: {ActiveEntityCount}, Visible: {VisibleEntityCount})\n" +
                             $"View: [{visibleBounds.X}, {visibleBounds.Y}] - [{visibleBounds.Right}, {visibleBounds.Bottom}]\n" +
                             $"Time: {GameTimeWorld?.TotalGameTime.TotalSeconds:F1}s";

            // Здесь можно добавить отрисовку текста
            // Для этого нужен SpriteFont, пока просто выводим в консоль
            Console.WriteLine($"[DEBUG] {debugText.Replace("\n", " | ")}");
        }

        // === Очистка ===
        public void Dispose()
        {
            // Удаляем все сущности
            foreach (Entity entity in _entities.Values.ToList())
            {
                RemoveEntity(entity);
                entity.Dispose();
            }
            _entities.Clear();

            // Очищаем тайлы
            _tileGrid?.Dispose();
            _tileGrid = null;

            // Очищаем SpatialGrid
            _spatialGrid = null;

            Console.WriteLine($"[World] Мир '{Name}' очищен");
        }

        public override string ToString()
        {
            return $"World '{Name}' ({EntityCount} entities)";
        }
    }
}