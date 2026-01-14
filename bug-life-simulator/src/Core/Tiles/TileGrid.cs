using System;
using System.Collections.Generic;
using System.Drawing;
using Microsoft.Xna.Framework;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TalesFromTheUnderbrush.src.Core.Tiles
{
    /// <summary>
    /// Управление гридом тайлов (отдельно от World)
    /// Оптимизирован для быстрого доступа к тайлам
    /// </summary>
    public class TileGrid : IDisposable
    {
        // === Размеры грида ===
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Layers { get; private set; }

        // === Грид тайлов ===
        private Tile[,,] _tiles;

        // === Карта высот ===
        private int[,] _heightMap;

        // === Пространственное разделение для тайлов ===
        private readonly SpatialGrid<Tile> _spatialGrid;

        // === Чанки для оптимизации рендеринга ===
        private const int CHUNK_SIZE = 16;
        private readonly Dictionary<Point, TileChunk> _chunks;

        // === События ===
        public event Action<Tile> TileAdded;
        public event Action<Tile> TileRemoved;
        public event Action<TileGrid> GridChanged;

        // === Статистика ===
        public int TotalTiles { get; private set; }

        // === Конструктор ===
        public TileGrid(int width, int height, int layers = 10)
        {
            if (width <= 0 || height <= 0 || layers <= 0)
                throw new ArgumentException("Grid dimensions must be positive");

            Width = width;
            Height = height;
            Layers = layers;

            _tiles = new Tile[width, height, layers];
            _heightMap = new int[width, height];

            // Spatial grid для быстрого поиска тайлов
            float worldWidth = width * Tile.TileSize.Width;
            float worldHeight = height * Tile.TileSize.Height;
            float cellSize = Math.Max(Tile.TileSize.Width, Tile.TileSize.Height) * 2;
            _spatialGrid = new SpatialGrid<Tile>(worldWidth, worldHeight, cellSize);

            // Инициализируем чанки
            _chunks = new Dictionary<Point, TileChunk>();
            InitializeChunks();
        }

        // === Основные операции ===

        public Tile GetTile(int x, int y, int layer)
        {
            if (!IsInBounds(x, y, layer))
                return null;

            return _tiles[x, y, layer];
        }

        public Tile GetTopTile(int x, int y)
        {
            if (!IsInBounds(x, y))
                return null;

            int height = _heightMap[x, y];
            return GetTile(x, y, height);
        }

        public bool SetTile(int x, int y, int layer, Tile tile)
        {
            if (!IsInBounds(x, y, layer))
                return false;

            // Удаляем старый тайл
            var oldTile = _tiles[x, y, layer];
            if (oldTile != null)
            {
                RemoveTileInternal(oldTile);
            }

            // Устанавливаем новый тайл
            _tiles[x, y, layer] = tile;

            if (tile != null)
            {
                // Устанавливаем позицию тайла
                tile.SetPosition(new Point(x, y), layer);

                // Добавляем в системы
                AddTileInternal(tile);

                // Обновляем карту высот
                if (layer >= _heightMap[x, y])
                {
                    _heightMap[x, y] = layer;
                }

                // Устанавливаем соседей
                UpdateTileNeighbors(tile);

                TotalTiles++;
            }

            // Помечаем чанк как изменённый
            MarkChunkDirty(x, y);

            GridChanged?.Invoke(this);
            return true;
        }

        public bool RemoveTile(int x, int y, int layer)
        {
            return SetTile(x, y, layer, null);
        }

        // === Поиск тайлов ===

        public List<Tile> GetTilesInArea(RectangleF area, int minLayer = 0, int maxLayer = int.MaxValue)
        {
            var result = new List<Tile>();

            // Конвертируем мировые координаты в грид
            int startX = Math.Max(0, (int)(area.Left / Tile.TileSize.Width));
            int endX = Math.Min(Width - 1, (int)(area.Right / Tile.TileSize.Width));
            int startY = Math.Max(0, (int)(area.Top / Tile.TileSize.Height));
            int endY = Math.Min(Height - 1, (int)(area.Bottom / Tile.TileSize.Height));

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    int maxZ = Math.Min(_heightMap[x, y], maxLayer);

                    for (int z = minLayer; z <= maxZ; z++)
                    {
                        var tile = _tiles[x, y, z];
                        if (tile != null)
                        {
                            result.Add(tile);
                        }
                    }
                }
            }

            return result;
        }

        public List<Tile> GetVisibleTiles(RectangleF viewport, Camera camera)
        {
            // Можно оптимизировать через spatial grid или чанки
            var tiles = _spatialGrid.Query(viewport);

            // Сортируем по глубине для правильного рендеринга
            tiles.Sort((a, b) =>
                (a.GridPosition.Y + a.GridPosition.X + a.Layer * 10)
                .CompareTo(b.GridPosition.Y + b.GridPosition.X + b.Layer * 10)
            );

            return tiles;
        }

        // === Проверки ===

        public bool IsWalkable(int x, int y)
        {
            var tile = GetTopTile(x, y);
            return tile != null && tile.IsWalkable;
        }

        public bool IsBuildable(int x, int y, int layer)
        {
            if (!IsInBounds(x, y, layer))
                return false;

            // Проверяем, что клетка пуста
            if (GetTile(x, y, layer) != null)
                return false;

            // Проверяем опору (если не на земле)
            if (layer > 0)
            {
                var below = GetTile(x, y, layer - 1);
                if (below == null || !below.IsSolid)
                    return false;
            }

            return true;
        }

        public bool HasSupport(int x, int y, int layer)
        {
            if (layer == 0)
                return true; // Земля всегда имеет поддержку

            var below = GetTile(x, y, layer - 1);
            return below != null && below.IsSolid;
        }

        // === Утилиты ===

        public bool IsInBounds(int x, int y, int layer = 0)
        {
            return x >= 0 && x < Width &&
                   y >= 0 && y < Height &&
                   layer >= 0 && layer < Layers;
        }

        public Point WorldToGrid(Vector2 worldPos)
        {
            return new Point(
                (int)Math.Floor(worldPos.X / Tile.TileSize.Width),
                (int)Math.Floor(worldPos.Y / Tile.TileSize.Height)
            );
        }

        public Vector2 GridToWorld(int gridX, int gridY)
        {
            return new Vector2(
                gridX * Tile.TileSize.Width + Tile.TileSize.Width / 2,
                gridY * Tile.TileSize.Height + Tile.TileSize.Height / 2
            );
        }

        // === Приватные методы ===

        private void AddTileInternal(Tile tile)
        {
            // Добавляем в spatial grid
            var bounds = new RectangleF(
                tile.WorldPosition.X - Tile.TileSize.Width / 2,
                tile.WorldPosition.Y - Tile.TileSize.Height / 2,
                Tile.TileSize.Width,
                Tile.TileSize.Height
            );
            _spatialGrid.Add(tile, bounds);

            // Добавляем в чанк
            var chunkPos = GetChunkPosition(tile.GridPosition);
            if (_chunks.TryGetValue(chunkPos, out var chunk))
            {
                chunk.AddTile(tile);
            }

            TileAdded?.Invoke(tile);
        }

        private void RemoveTileInternal(Tile tile)
        {
            // Удаляем из spatial grid
            _spatialGrid.Remove(tile);

            // Удаляем из чанка
            var chunkPos = GetChunkPosition(tile.GridPosition);
            if (_chunks.TryGetValue(chunkPos, out var chunk))
            {
                chunk.RemoveTile(tile);
            }

            // Освобождаем соседей
            for (int i = 0; i < tile.Neighbors.Length; i++)
            {
                if (tile.Neighbors[i] != null)
                {
                    // Находим обратную связь и очищаем
                    var neighbor = tile.Neighbors[i];
                    for (int j = 0; j < neighbor.Neighbors.Length; j++)
                    {
                        if (neighbor.Neighbors[j] == tile)
                        {
                            neighbor.Neighbors[j] = null;
                            break;
                        }
                    }
                }
            }

            tile.Dispose();
            TotalTiles--;

            TileRemoved?.Invoke(tile);
        }

        private void UpdateTileNeighbors(Tile tile)
        {
            int x = tile.GridPosition.X;
            int y = tile.GridPosition.Y;
            int z = tile.Layer;

            tile.SetNeighbors(
                north: GetTile(x, y - 1, z),
                south: GetTile(x, y + 1, z),
                east: GetTile(x + 1, y, z),
                west: GetTile(x - 1, y, z),
                above: GetTile(x, y, z + 1),
                below: GetTile(x, y, z - 1)
            );

            // Обновляем обратные ссылки у соседей
            UpdateNeighborReference(tile.Neighbors[0], tile, 1); // Север -> Юг
            UpdateNeighborReference(tile.Neighbors[1], tile, 0); // Юг -> Север
            UpdateNeighborReference(tile.Neighbors[2], tile, 3); // Восток -> Запад
            UpdateNeighborReference(tile.Neighbors[3], tile, 2); // Запад -> Восток
            UpdateNeighborReference(tile.Neighbors[4], tile, 5); // Верх -> Низ
            UpdateNeighborReference(tile.Neighbors[5], tile, 4); // Низ -> Верх
        }

        private void UpdateNeighborReference(Tile neighbor, Tile tile, int direction)
        {
            if (neighbor != null && direction >= 0 && direction < neighbor.Neighbors.Length)
            {
                neighbor.Neighbors[direction] = tile;
            }
        }

        private void InitializeChunks()
        {
            int chunksX = (Width + CHUNK_SIZE - 1) / CHUNK_SIZE;
            int chunksY = (Height + CHUNK_SIZE - 1) / CHUNK_SIZE;

            for (int y = 0; y < chunksY; y++)
            {
                for (int x = 0; x < chunksX; x++)
                {
                    var chunkPos = new Point(x, y);
                    _chunks[chunkPos] = new TileChunk(chunkPos, CHUNK_SIZE);
                }
            }
        }

        private Point GetChunkPosition(Point gridPos)
        {
            return new Point(gridPos.X / CHUNK_SIZE, gridPos.Y / CHUNK_SIZE);
        }

        private void MarkChunkDirty(int gridX, int gridY)
        {
            var chunkPos = GetChunkPosition(new Point(gridX, gridY));
            if (_chunks.TryGetValue(chunkPos, out var chunk))
            {
                chunk.IsDirty = true;
            }
        }

        // === Очистка ===

        public void Clear()
        {
            // Удаляем все тайлы
            for (int z = 0; z < Layers; z++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        var tile = _tiles[x, y, z];
                        if (tile != null)
                        {
                            RemoveTileInternal(tile);
                            _tiles[x, y, z] = null;
                        }
                    }
                }
            }

            // Сбрасываем карту высот
            Array.Clear(_heightMap, 0, _heightMap.Length);

            // Сбрасываем чанки
            foreach (var chunk in _chunks.Values)
            {
                chunk.Clear();
            }

            GridChanged?.Invoke(this);
        }

        public void Dispose()
        {
            Clear();
            _spatialGrid.Clear();
            _chunks.Clear();
        }
    }

    /// <summary>
    /// Чанк тайлов для оптимизации рендеринга
    /// </summary>
    public class TileChunk
    {
        public Point Position { get; }
        public int Size { get; }
        public bool IsDirty { get; set; } = true;
        public bool IsVisible { get; set; } = true;

        private readonly List<Tile> _tiles = new();

        public TileChunk(Point position, int size)
        {
            Position = position;
            Size = size;
        }

        public void AddTile(Tile tile)
        {
            if (!_tiles.Contains(tile))
            {
                _tiles.Add(tile);
                IsDirty = true;
            }
        }

        public void RemoveTile(Tile tile)
        {
            if (_tiles.Remove(tile))
            {
                IsDirty = true;
            }
        }

        public void Clear()
        {
            _tiles.Clear();
            IsDirty = true;
        }

        public List<Tile> GetTiles()
        {
            return new List<Tile>(_tiles);
        }

        public Rectangle GetBounds()
        {
            int worldX = Position.X * Size * Tile.TileSize.Width;
            int worldY = Position.Y * Size * Tile.TileSize.Height;
            int worldWidth = Size * Tile.TileSize.Width;
            int worldHeight = Size * Tile.TileSize.Height;

            return new Rectangle(worldX, worldY, worldWidth, worldHeight);
        }
    }
}
