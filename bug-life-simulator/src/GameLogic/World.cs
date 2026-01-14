using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TalesFromTheUnderbrush.src.Core.Entities;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;

namespace TalesFromTheUnderbrush.src.GameLogic
{
    /// <summary>
    /// Игровой мир - содержит грид тайлов и управляет сущностями
    /// </summary>
    public class World : IDisposable
    {
        // === Размеры мира ===
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int MaxLayers { get; private set; }

        public float TileWidth { get; private set; }
        public float TileHeight { get; private set; }

        // === Грид тайлов [x, y, z] ===
        private Entity[,,] _tileGrid;

        // === Карта высот (быстрый доступ к верхнему тайлу) ===
        private int[,] _heightMap;

        // === Пространственное разделение (для оптимизации) ===
        private readonly SpatialGrid<Entity> _spatialGrid;

        // === Чанковая система ===
        private const int CHUNK_SIZE = 16;
        private readonly Dictionary<Point, WorldChunk> _chunks;

        // === События ===
        public event Action<Entity> EntityAdded;
        public event Action<Entity> EntityRemoved;
        public event Action<World> WorldChanged;

        // === Статистика ===
        public int TotalEntities => _spatialGrid.Count;
        public int TotalTiles { get; private set; }

        // === Конструктор ===
        public World(int width, int height, int maxLayers = 10, float tileWidth = 1f, float tileHeight = 1f)
        {
            if (width <= 0 || height <= 0 || maxLayers <= 0)
                throw new ArgumentException("World dimensions must be positive");

            Width = width;
            Height = height;
            MaxLayers = maxLayers;
            TileWidth = tileWidth;
            TileHeight = tileHeight;

            // Инициализируем грид
            _tileGrid = new Entity[width, height, maxLayers];
            _heightMap = new int[width, height];

            // Инициализируем spatial grid
            float cellSize = Math.Max(tileWidth, tileHeight) * 2;
            _spatialGrid = new SpatialGrid<Entity>(width * tileWidth, height * tileHeight, cellSize);

            // Инициализируем чанки
            _chunks = new Dictionary<Point, WorldChunk>();
            InitializeChunks();
        }

        // === Инициализация чанков ===
        private void InitializeChunks()
        {
            int chunksX = (Width + CHUNK_SIZE - 1) / CHUNK_SIZE;
            int chunksY = (Height + CHUNK_SIZE - 1) / CHUNK_SIZE;

            for (int y = 0; y < chunksY; y++)
            {
                for (int x = 0; x < chunksX; x++)
                {
                    var chunkPos = new Point(x, y);
                    _chunks[chunkPos] = new WorldChunk(chunkPos, CHUNK_SIZE);
                }
            }
        }

        // === Работа с тайлами ===

        /// <summary>
        /// Получить тайл по координатам
        /// </summary>
        public Entity GetTile(int x, int y, int z)
        {
            if (!IsInBounds(x, y, z))
                return null;

            return _tileGrid[x, y, z];
        }

        /// <summary>
        /// Получить верхний тайл в клетке
        /// </summary>
        public Entity GetTopTile(int x, int y)
        {
            if (!IsInBounds(x, y))
                return null;

            int height = _heightMap[x, y];
            return GetTile(x, y, height);
        }

        /// <summary>
        /// Установить тайл
        /// </summary>
        public bool SetTile(int x, int y, int z, Entity tile)
        {
            if (!IsInBounds(x, y, z))
                return false;

            // Проверяем, что это действительно тайл
            if (tile != null && !IsTileEntity(tile))
                return false;

            // Удаляем старый тайл если есть
            var oldTile = _tileGrid[x, y, z];
            if (oldTile != null)
            {
                RemoveEntity(oldTile);
            }

            // Устанавливаем новый тайл
            _tileGrid[x, y, z] = tile;

            if (tile != null)
            {
                // Устанавливаем позицию тайла
                tile.SetPosition(x, y);
                tile.SetHeight(z);

                // Добавляем в системы
                AddEntityInternal(tile);

                // Обновляем карту высот
                if (z >= _heightMap[x, y])
                {
                    _heightMap[x, y] = z;
                }

                TotalTiles++;
            }

            // Помечаем чанк как изменённый
            MarkChunkDirty(x, y);

            WorldChanged?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Создать и добавить тайл
        /// </summary>
        public Entity CreateTile(int x, int y, int z, Func<Vector2, float, Entity> tileCreator)
        {
            if (!IsInBounds(x, y, z))
                return null;

            var tile = tileCreator?.Invoke(new Vector2(x, y), z);
            if (tile == null)
                return null;

            if (SetTile(x, y, z, tile))
                return tile;

            return null;
        }

        /// <summary>
        /// Удалить тайл
        /// </summary>
        public bool RemoveTile(int x, int y, int z)
        {
            return SetTile(x, y, z, null);
        }

        // === Работа с сущностями ===

        /// <summary>
        /// Добавить сущность в мир
        /// </summary>
        public bool AddEntity(Entity entity)
        {
            if (entity == null || entity.IsDisposed)
                return false;

            if (entity.World != null && entity.World != this)
            {
                entity.World.RemoveEntity(entity);
            }

            // Проверяем позицию
            var position = entity.Position;
            if (!IsInBounds(position.X, position.Y))
                return false;

            // Если это тайл - используем специальный метод
            if (IsTileEntity(entity))
            {
                int x = (int)position.X;
                int y = (int)position.Y;
                int z = (int)entity.Height;

                return SetTile(x, y, z, entity);
            }

            // Обычная сущность
            AddEntityInternal(entity);
            return true;
        }

        private void AddEntityInternal(Entity entity)
        {
            entity.World = this;

            // Добавляем в spatial grid
            var bounds = entity.GetBounds();
            _spatialGrid.Add(entity, bounds);

            // Добавляем в чанк
            var chunkPos = GetChunkPosition(entity.Position);
            if (_chunks.TryGetValue(chunkPos, out var chunk))
            {
                chunk.AddEntity(entity);
            }

            // Событие добавления
            EntityAdded?.Invoke(entity);
            entity.OnAddedToWorld?.Invoke(entity);
        }

        /// <summary>
        /// Удалить сущность из мира
        /// </summary>
        public bool RemoveEntity(Entity entity)
        {
            if (entity == null || entity.World != this)
                return false;

            // Если это тайл - удаляем из грида
            if (IsTileEntity(entity))
            {
                var position = entity.Position;
                int x = (int)position.X;
                int y = (int)position.Y;
                int z = (int)entity.Height;

                if (GetTile(x, y, z) == entity)
                {
                    _tileGrid[x, y, z] = null;
                    TotalTiles--;

                    // Пересчитываем карту высот
                    if (z == _heightMap[x, y])
                    {
                        RecalculateHeightAt(x, y);
                    }
                }
            }

            // Удаляем из spatial grid
            _spatialGrid.Remove(entity);

            // Удаляем из чанка
            var chunkPos = GetChunkPosition(entity.Position);
            if (_chunks.TryGetValue(chunkPos, out var chunk))
            {
                chunk.RemoveEntity(entity);
            }

            entity.World = null;

            // Событие удаления
            EntityRemoved?.Invoke(entity);
            entity.OnRemovedFromWorld?.Invoke(entity);

            return true;
        }

        /// <summary>
        /// Получить сущность по ID
        /// </summary>
        public Entity GetEntity(ulong id)
        {
            // TODO: Можно добавить Dictionary для быстрого поиска по ID
            return _spatialGrid.FirstOrDefault(e => e.Id == id);
        }

        // === Поиск сущностей ===

        /// <summary>
        /// Получить все сущности в области
        /// </summary>
        public List<Entity> GetEntitiesInArea(RectangleF area, Predicate<Entity> filter = null)
        {
            var entities = _spatialGrid.Query(area);

            if (filter != null)
            {
                entities.RemoveAll(e => !filter(e));
            }

            return entities;
        }

        /// <summary>
        /// Получить сущности рядом с точкой
        /// </summary>
        public List<Entity> GetEntitiesNear(Vector2 position, float radius, Predicate<Entity> filter = null)
        {
            var area = new RectangleF(
                position.X - radius,
                position.Y - radius,
                radius * 2,
                radius * 2
            );

            var candidates = GetEntitiesInArea(area, filter);

            // Уточняем по расстоянию
            candidates.RemoveAll(e =>
                Vector2.Distance(position, e.Position) > radius
            );

            return candidates;
        }

        /// <summary>
        /// Получить тайлы в области
        /// </summary>
        public List<Entity> GetTilesInArea(RectangleF area, int minLayer = 0, int maxLayer = int.MaxValue)
        {
            var result = new List<Entity>();

            int startX = Math.Max(0, (int)area.Left);
            int endX = Math.Min(Width - 1, (int)area.Right);
            int startY = Math.Max(0, (int)area.Top);
            int endY = Math.Min(Height - 1, (int)area.Bottom);

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    int maxZ = Math.Min(_heightMap[x, y], maxLayer);

                    for (int z = minLayer; z <= maxZ; z++)
                    {
                        var tile = _tileGrid[x, y, z];
                        if (tile != null)
                        {
                            result.Add(tile);
                        }
                    }
                }
            }

            return result;
        }

        // === Проверка проходимости ===

        /// <summary>
        /// Можно ли пройти в клетку
        /// </summary>
        public bool IsWalkable(int x, int y)
        {
            var tile = GetTopTile(x, y);
            return tile != null && IsWalkableTile(tile);
        }

        /// <summary>
        /// Можно ли построить в клетке
        /// </summary>
        public bool IsBuildable(int x, int y, int z)
        {
            if (!IsInBounds(x, y, z))
                return false;

            // Проверяем, что клетка пуста
            if (GetTile(x, y, z) != null)
                return false;

            // Проверяем опору (если не земля)
            if (z > 0)
            {
                var below = GetTile(x, y, z - 1);
                if (below == null || !IsSolidTile(below))
                    return false;
            }

            return true;
        }

        // === Утилиты ===

        private bool IsTileEntity(Entity entity)
        {
            // TODO: Можно добавить атрибут или интерфейс ITileEntity
            return entity is StaticEntity; // Временное решение
        }

        private bool IsWalkableTile(Entity tile)
        {
            // TODO: Определить по свойствам тайла
            return true;
        }

        private bool IsSolidTile(Entity tile)
        {
            // TODO: Определить по свойствам тайла
            return tile != null;
        }

        /// <summary>
        /// Проверить границы мира
        /// </summary>
        public bool IsInBounds(float x, float y, int z = 0)
        {
            return x >= 0 && x < Width &&
                   y >= 0 && y < Height &&
                   z >= 0 && z < MaxLayers;
        }

        /// <summary>
        /// Преобразовать мировые координаты в грид-координаты
        /// </summary>
        public Point WorldToGrid(Vector2 worldPos)
        {
            return new Point(
                (int)Math.Floor(worldPos.X / TileWidth),
                (int)Math.Floor(worldPos.Y / TileHeight)
            );
        }

        /// <summary>
        /// Преобразовать грид-координаты в мировые
        /// </summary>
        public Vector2 GridToWorld(int gridX, int gridY)
        {
            return new Vector2(
                gridX * TileWidth + TileWidth / 2,
                gridY * TileHeight + TileHeight / 2
            );
        }

        private Point GetChunkPosition(Vector2 worldPos)
        {
            int chunkX = (int)(worldPos.X / (CHUNK_SIZE * TileWidth));
            int chunkY = (int)(worldPos.Y / (CHUNK_SIZE * TileHeight));
            return new Point(chunkX, chunkY);
        }

        private void MarkChunkDirty(int gridX, int gridY)
        {
            int chunkX = gridX / CHUNK_SIZE;
            int chunkY = gridY / CHUNK_SIZE;
            var chunkPos = new Point(chunkX, chunkY);

            if (_chunks.TryGetValue(chunkPos, out var chunk))
            {
                chunk.IsDirty = true;
            }
        }

        private void RecalculateHeightAt(int x, int y)
        {
            for (int z = MaxLayers - 1; z >= 0; z--)
            {
                if (_tileGrid[x, y, z] != null)
                {
                    _heightMap[x, y] = z;
                    return;
                }
            }

            _heightMap[x, y] = 0;
        }

        // === Генерация тестового мира ===

        public void GenerateTestWorld()
        {
            Random random = new Random();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    // Случайная высота (холмы)
                    float noise = (float)PerlinNoise(x * 0.1f, y * 0.1f, random);
                    int height = (int)(3 + noise * 2);

                    // Основание (камень)
                    for (int z = 0; z < height - 1; z++)
                    {
                        CreateTile(x, y, z, (pos, h) =>
                            new TestTile(pos, h) { TileColor = Color.DarkGray }
                        );
                    }

                    // Верхний слой (трава/земля)
                    CreateTile(x, y, height - 1, (pos, h) =>
                    {
                        var tile = new TestTile(pos, h);
                        tile.TileColor = random.NextDouble() > 0.3 ? Color.Green : Color.SandyBrown;
                        return tile;
                    });

                    // Иногда добавляем дерево
                    if (random.NextDouble() > 0.9 && height > 2)
                    {
                        CreateTile(x, y, height, (pos, h) =>
                            new TestTile(pos, h) { TileColor = Color.SaddleBrown, SetSize(0.3f, 0.3f) }
                        );
                    }
                }
            }

            WorldChanged?.Invoke(this);
        }

        private float PerlinNoise(float x, float y, Random random)
        {
            // Упрощённый шум для теста
            return (float)(Math.Sin(x * 0.3f) * Math.Cos(y * 0.3f) * 0.5f + 0.5f);
        }

        // === Очистка ===

        public void Clear()
        {
            // Удаляем все сущности
            var entities = _spatialGrid.ToList();
            foreach (var entity in entities)
            {
                RemoveEntity(entity);
            }

            // Очищаем грид
            Array.Clear(_tileGrid, 0, _tileGrid.Length);
            Array.Clear(_heightMap, 0, _heightMap.Length);

            TotalTiles = 0;

            // Сбрасываем чанки
            foreach (var chunk in _chunks.Values)
            {
                chunk.Clear();
            }

            WorldChanged?.Invoke(this);
        }

        public void Dispose()
        {
            Clear();
            _spatialGrid.Clear();
            _chunks.Clear();
        }
    }

    /// <summary>
    /// Чанк мира для оптимизации
    /// </summary>
    public class WorldChunk
    {
        public Point Position { get; }
        public int Size { get; }
        public bool IsDirty { get; set; }
        public bool IsVisible { get; set; } = true;

        private readonly List<Entity> _entities = new();

        public WorldChunk(Point position, int size)
        {
            Position = position;
            Size = size;
        }

        public void AddEntity(Entity entity)
        {
            if (!_entities.Contains(entity))
            {
                _entities.Add(entity);
                IsDirty = true;
            }
        }

        public void RemoveEntity(Entity entity)
        {
            if (_entities.Remove(entity))
            {
                IsDirty = true;
            }
        }

        public void Clear()
        {
            _entities.Clear();
            IsDirty = true;
        }

        public List<Entity> GetEntities()
        {
            return new List<Entity>(_entities);
        }
    }
}
