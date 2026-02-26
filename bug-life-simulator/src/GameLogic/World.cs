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
using IRenderable = TalesFromTheUnderbrush.src.Graphics.IRenderable;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TalesFromTheUnderbrush.src.GameLogic
{
    /// <summary>
    /// Мир игры — центральный оркестратор рендеринга и обновления.
    /// Отвечает за глобальную сортировку тайлов и сущностей для изометрической проекции.
    /// Чанки и тайлы — чистые контейнеры данных без логики отрисовки.
    /// </summary>
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
        public int VisibleEntityCount => _entities.Values.Count(e => e.Visible);

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
            int worldWidthPixels = width * Tile.TileSize.Width;
            int worldHeightPixels = height * Tile.TileSize.Height;
            int cellSize = Math.Max(Tile.TileSize.Width, Tile.TileSize.Height);

            _spatialGrid = new SpatialGrid<Entity>(worldWidthPixels, worldHeightPixels, cellSize);
            _tileGrid = new TileGrid(width, height); // Глубина 32 для вертикального перемещения
        }

        /// <summary>
        /// Инициализирует тестовые тайлы ПОСЛЕ загрузки контента.
        /// Вызывать из GameManager.LoadContent() после загрузки атласа.
        /// </summary>
        public void InitializeTiles(Texture2D atlas, Rectangle grassRect, Rectangle dirtRect)
        {
            int width = _tileGrid.Width;
            int height = _tileGrid.Height;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = (x + y) % 2 == 0
                        ? new GrassTile(new Point(x, y), 0, atlas, grassRect, true)
                        : new GrassTile(new Point(x, y), 0, atlas, dirtRect, true);

                    _tileGrid.SetTile(x, y, 0, tile);
                }
            }

            Console.WriteLine($"[World] Инициализировано {width * height} тайлов");
        }

        // === Управление сущностями ===
        public void AddEntity(Entity entity)
        {
            if (entity == null || _entities.ContainsKey(entity.Id)) return;

            RectangleF bounds = entity.GetBounds();
            _spatialGrid.Add(entity, new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height));
            _entities[entity.Id] = entity;
            entity.World = this;
            EntityAdded?.Invoke(entity);
            Console.WriteLine($"[World] Добавлена сущность: {entity.Name} (ID: {entity.Id})");
        }

        public bool RemoveEntity(ulong entityId) => RemoveEntity(_entities.GetValueOrDefault(entityId));
        public bool RemoveEntity(Entity entity)
        {
            if (entity == null || !_entities.ContainsKey(entity.Id)) return false;
            _spatialGrid.Remove(entity);
            _entities.Remove(entity.Id);
            entity.World = null;
            EntityRemoved?.Invoke(entity);
            Console.WriteLine($"[World] Удалена сущность: {entity.Name} (ID: {entity.Id})");
            return true;
        }

        public Entity GetEntityById(ulong id) => _entities.TryGetValue(id, out Entity? entity) ? entity : null;

        // === Поиск сущностей ===
        public List<Entity> GetEntitiesInArea(Rectangle area) =>
            _spatialGrid.Query(new RectangleF(area.X, area.Y, area.Width, area.Height)).Cast<Entity>().ToList();

        public List<Entity> GetEntitiesInArea(RectangleF area) =>
            _spatialGrid.Query(area).Cast<Entity>().ToList();

        // === Работа с тайлами ===
        public Tile GetTileAt(int x, int y, int z = 0) => _tileGrid?.GetTile(x, y, z);
        public void SetTileAt(int x, int y, int z, Tile tile) => _tileGrid?.SetTile(x, y, z, tile);
        public bool IsTileWalkable(int x, int y, int z = 0) => _tileGrid?.IsWalkable(x, y) ?? false;

        // === Обновление ===
        public void Update(GameTime gameTime)
        {
            GameTimeWorld = gameTime;
            _tileGrid?.Update(gameTime);

            foreach (Entity entity in _entities.Values.Where(e => e.IsActive).ToList())
            {
                try
                {
                    entity.Update(gameTime);
                    if (entity.ShouldBeRemoved) RemoveEntity(entity);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[World] Ошибка при обновлении сущности {entity.Name}: {ex.Message}");
                }
            }

            WorldUpdated?.Invoke(this);
        }

        // === ОСНОВНОЙ МЕТОД ОТРИСОВКИ ===
        public void Draw(SpriteBatch spriteBatch, ICamera camera = null)
        {
            if (spriteBatch == null || _tileGrid == null) return;

            if (camera != null)
            {
                DrawWithCamera(spriteBatch, camera);
            }
            else
            {
                // Режим отладки: отрисовка всего мира (только для небольших тестовых карт!)
                DrawFullWorldDebug(spriteBatch);
            }
        }

        // === Отрисовка с камерой (основной режим) ===
        private void DrawWithCamera(SpriteBatch spriteBatch, ICamera camera)
        {
            // 1. Определяем видимую область в мировых координатах
            Rectangle visibleBounds = new Rectangle(
                (int)camera.Bounds.X,
                (int)camera.Bounds.Y,
                (int)camera.Bounds.Width,
                (int)camera.Bounds.Height
            );

            // 2. Собираем ВСЕ видимые тайлы из ВИДИМЫХ чанков
            List<Tile> visibleTiles = new List<Tile>();
            List<TileChunk> visibleChunks = _tileGrid.GetChunksInArea(visibleBounds);

            foreach (TileChunk chunk in visibleChunks)
            {
                if (chunk == null) continue;
                // GetAllTiles() возвращает все тайлы чанка; фильтруем видимые
                IEnumerable<Tile> tiles = chunk.GetAllTiles().Where(t => t != null && t.Visible);
                visibleTiles.AddRange(tiles);
            }

            // 3. ГЛОБАЛЬНАЯ СОРТИРОВКА ПО ГЛУБИНЕ (КРИТИЧНО ДЛЯ ИЗОМЕТРИИ!)
            // Формула: (X + Y) * 100 + Z * 50 — гарантирует правильное наложение МЕЖДУ чанками
            IEnumerable<Tile> sortedTiles = visibleTiles.OrderBy(tile =>
                (tile.GridPosition.X + tile.GridPosition.Y) * 100 + tile.Layer * 50);

            // 4. ОТРИСОВКА КАЖДОГО ТАЙЛА
            foreach (Tile tile in sortedTiles)
            {
                // 1. Вычисляем экранную позицию через камеру (ИЗОМЕТРИЯ!)
                Vector2 screenPos = camera.WorldToScreen(
                    new Vector3(tile.GridPosition.X, tile.GridPosition.Y, tile.Layer)
                );

                // 2. Нормализуем глубину для SpriteBatch (0.0–0.9999)
                float drawDepth = (tile.GridPosition.X + tile.GridPosition.Y) * 100 + tile.Layer * 50;
                drawDepth = MathHelper.Clamp(drawDepth / 10000f, 0f, 0.9999f);

                // 3. Вызываем отрисовку с ПЕРЕДАННОЙ позицией
                tile.DrawAtPosition(spriteBatch, screenPos, drawDepth);
            }
            // 5. ОТРИСОВКА СУЩНОСТЕЙ В ВИДИМОЙ ОБЛАСТИ
            //List<Entity> visibleEntities = GetEntitiesInArea(visibleBounds)
            //    .Where(e => e.Visible && e is IDrawable)
            //    .OrderBy(e => (e.Position.X + e.Position.Y) * 100 + e.Position.Z * 50); // Примерная сортировка

            //foreach (Entity entity in visibleEntities)
            //{
            //    if (entity is IRequiresSpriteBatch requiresBatch)
            //        requiresBatch.SetSpriteBatch(spriteBatch);
            //    entity.Draw(GameTimeWorld);
            //}

            //// 6. ОТЛАДОЧНАЯ ИНФОРМАЦИЯ
            //if (GlobalSettings.DebugMode)
            //    DrawDebugInfo(spriteBatch, visibleBounds, visibleTiles.Count, visibleEntities.Count());
        }

        // === Отрисовка всего мира (только для отладки небольших карт!) ===
        private void DrawFullWorldDebug(SpriteBatch spriteBatch)
        {
            IEnumerable<Tile> allTiles = _tileGrid.GetAllTiles().Where(t => t != null && t.Visible);
            IOrderedEnumerable<Tile> sortedTiles = allTiles.OrderBy(tile =>
                (tile.GridPosition.X + tile.GridPosition.Y) * 100 + tile.Layer * 50);

            foreach (Tile tile in sortedTiles)
                tile.Draw(GameTimeWorld, spriteBatch);

            foreach (Entity entity in _entities.Values.Where(e => e.Visible && e is IRenderable))
            {
                if (entity is IRequiresSpriteBatch requiresBatch)
                    requiresBatch.SetSpriteBatch(spriteBatch);
                entity.Draw(GameTimeWorld);
            }

            if (GlobalSettings.DebugMode)
                DrawDebugInfo(spriteBatch, new Rectangle(0, 0, 800, 600), allTiles.Count(), _entities.Values.Count(e => e.Visible));
        }

        // === Отладочная информация ===
        private void DrawDebugInfo(SpriteBatch spriteBatch, Rectangle visibleBounds, int tileCount, int entityCount)
        {
            string debugText = $"World: {Name} | " +
                             $"Tiles: {tileCount} | " +
                             $"Entities: {entityCount} | " +
                             $"View: [{visibleBounds.X},{visibleBounds.Y}] | " +
                             $"Time: {GameTimeWorld?.TotalGameTime.TotalSeconds:F1}s";
            Console.WriteLine($"[DEBUG] {debugText}");
            // В будущем: отрисовка текста через SpriteFont
        }

        // === Очистка ===
        public void Dispose()
        {
            foreach (Entity entity in _entities.Values.ToList())
            {
                RemoveEntity(entity);
                entity.Dispose();
            }
            _entities.Clear();
            _tileGrid?.Dispose();
            _tileGrid = null;
            _spatialGrid = null;
            Console.WriteLine($"[World] Мир '{Name}' очищен");
        }

        public override string ToString() => $"World '{Name}' ({EntityCount} entities)";
    }
}