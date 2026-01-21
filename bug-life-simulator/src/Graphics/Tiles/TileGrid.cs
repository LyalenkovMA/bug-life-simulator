using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using TalesFromTheUnderbrush.src.GameLogic;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Управление гридом тайлов (отдельно от World)
    /// Оптимизирован для быстрого доступа к тайлам
    /// </summary>
    public class TileGrid
    {
        // === Размеры грида ===
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Layers { get; private set; }
        public int Depth { get; private set; }

        public int ChunksWidth => _chunks.GetLength(0);
        public int ChunksHeight => _chunks.GetLength(1);

        // === Грид тайлов ===
        private Tile[,,] _tiles;

        // === Карта высот ===
        private int[,] _heightMap;

        // === Пространственное разделение для тайлов ===
        private readonly SpatialGrid<Tile> _spatialGrid;

        // === Чанки для оптимизации рендеринга ===
        private const int CHUNK_SIZE = 16;
        private readonly TileChunk[,] _chunks;
        private readonly int _chunkSize;


        // === События ===
        public event Action<Tile> TileAdded;
        public event Action<Tile> TileRemoved;
        public event Action<TileGrid> GridChanged;

        // === Статистика ===
        public int TotalTiles { get; private set; }

        // === Конструктор ===
        public TileGrid(int width, int height, int depth = 1, int chunkSize = 16)
        {
            if (width <= 0 || height <= 0 || depth <= 0 || chunkSize <= 0)
                throw new ArgumentException("Dimensions must be positive");

            Width = width;
            Height = height;
            Depth = depth;
            _chunkSize = chunkSize;

            // Инициализируем массивы
            _tiles = new Tile[width, height, depth];
            _heightMap = new int[width, height];
            _spatialGrid = new SpatialGrid<Tile>(width * Tile.TileSize.Width,
                                                 height * Tile.TileSize.Height,
                                                 Tile.TileSize.Width);

            // Рассчитываем количество чанков
            int chunksX = (int)Math.Ceiling((float)width / chunkSize);
            int chunksY = (int)Math.Ceiling((float)height / chunkSize);

            _chunks = new TileChunk[chunksX, chunksY];

            // Инициализируем чанки
            for (int x = 0; x < chunksX; x++)
            {
                for (int y = 0; y < chunksY; y++)
                {
                    // Рассчитываем размер чанка (крайние могут быть меньше)
                    int chunkWidth = Math.Min(chunkSize, width - x * chunkSize);
                    int chunkHeight = Math.Min(chunkSize, height - y * chunkSize);

                    _chunks[x, y] = new TileChunk(new Point(x,y), chunkSize);
                }
            }

            Console.WriteLine($"[TileGrid] Создана сетка {width}x{height}x{depth}, чанков: {chunksX}x{chunksY}");
        }

        /// <summary>
        /// Получить все чанки в указанной области
        /// </summary>
        public List<TileChunk> GetChunksInArea(Rectangle area)
        {
            var chunks = new List<TileChunk>();

            int startChunkX = Math.Max(0, area.X / _chunkSize);
            int startChunkY = Math.Max(0, area.Y / _chunkSize);
            int endChunkX = Math.Min(ChunksWidth - 1, (area.X + area.Width) / _chunkSize);
            int endChunkY = Math.Min(ChunksHeight - 1, (area.Y + area.Height) / _chunkSize);

            for (int chunkX = startChunkX; chunkX <= endChunkX; chunkX++)
            {
                for (int chunkY = startChunkY; chunkY <= endChunkY; chunkY++)
                {
                    var chunk = _chunks[chunkX, chunkY];
                    if (chunk != null)
                    {
                        chunks.Add(chunk);
                    }
                }
            }

            return chunks;
        }

        /// <summary>
        /// Получить чанк по мировым координатам
        /// </summary>
        public TileChunk GetChunkAtWorldPos(int worldX, int worldY)
        {
            if (worldX < 0 || worldY < 0 || worldX >= Width || worldY >= Height)
                return null;

            int chunkX = worldX / _chunkSize;
            int chunkY = worldY / _chunkSize;

            if (chunkX >= 0 && chunkX < ChunksWidth && chunkY >= 0 && chunkY < ChunksHeight)
            {
                return _chunks[chunkX, chunkY];
            }

            return null;
        }

        /// <summary>
        /// Очистить все тайлы
        /// </summary>
        public void Clear()
        {
            for (int x = 0; x < ChunksWidth; x++)
            {
                for (int y = 0; y < ChunksHeight; y++)
                {
                    _chunks[x, y]?.Clear();
                }
            }

            Console.WriteLine($"[TileGrid] Сетка очищена");
        }

        /// <summary>
        /// Получить все видимые тайлы (для отрисовки)
        /// </summary>
        public IEnumerable<Tile> GetVisibleTiles()
        {
            foreach (var chunk in _chunks)
            {
                if (chunk != null)
                {
                    foreach (Tile tile in chunk.GetAllTiles())
                    {
                        if (tile?.IsVisible == true)
                        {
                            yield return tile;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Получить тайл по координатам
        /// </summary>
        public Tile GetTile(int x, int y, int z = 0)
        {
            TileChunk chunk = GetChunkAtWorldPos(x, y);
            if (chunk == null) return null;

            // Локальные координаты внутри чанка
            // chunk.Position - это позиция чанка в "чанковых" координатах
            int localX = x - (chunk.Position.X * _chunkSize);
            int localY = y - (chunk.Position.Y * _chunkSize);

            return chunk.GetTile(localX, localY, z);
        }

        // === Основные операции (исправленные) ===

        /// <summary>
        /// Проверить, находится ли координата в границах сетки
        /// </summary>
        public bool IsInBounds(int x, int y, int layer = 0)
        {
            return x >= 0 && x < Width &&
                   y >= 0 && y < Height &&
                   layer >= 0 && layer < Depth;
        }

        /// <summary>
        /// Получить тайл по мировым координатам (публичная версия)
        /// </summary>
        public Tile GetTileAt(int x, int y, int z = 0)
        {
            return GetTile(x, y, z);
        }

        /// <summary>
        /// Получить верхний тайл в столбце
        /// </summary>
        public Tile GetTopTile(int x, int y)
        {
            if (!IsInBounds(x, y))
                return null;

            // Ищем самый верхний непустой тайл в столбце
            for (int z = Depth - 1; z >= 0; z--)
            {
                var tile = GetTile(x, y, z);
                if (tile != null)
                    return tile;
            }
            return null;
        }

        /// <summary>
        /// Проверить, можно ли пройти по клетке
        /// </summary>
        public bool IsWalkable(int x, int y)
        {
            var tile = GetTopTile(x, y);
            return tile != null && tile.IsWalkable;
        }

        /// <summary>
        /// Можно ли построить на клетке
        /// </summary>
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

        /// <summary>
        /// Есть ли поддержка под тайлом
        /// </summary>
        public bool HasSupport(int x, int y, int layer)
        {
            if (layer == 0)
                return true; // Земля всегда имеет поддержку

            Tile below = GetTile(x, y, layer - 1);
            return below != null && below.IsSolid;
        }

        /// <summary>
        /// Конвертировать мировые координаты в координаты сетки
        /// </summary>
        public Point WorldToGrid(Vector2 worldPos)
        {
            return new Point(
                (int)Math.Floor(worldPos.X / Tile.TileSize.Width),
                (int)Math.Floor(worldPos.Y / Tile.TileSize.Height)
            );
        }

        /// <summary>
        /// Конвертировать координаты сетки в мировые
        /// </summary>
        public Vector2 GridToWorld(int gridX, int gridY)
        {
            return new Vector2(
                gridX * Tile.TileSize.Width + Tile.TileSize.Width / 2,
                gridY * Tile.TileSize.Height + Tile.TileSize.Height / 2
            );
        }

        /// <summary>
        /// Получить позицию чанка по координатам грида
        /// </summary>
        private Point GetChunkPosition(Point gridPos)
        {
            return new Point(Width / _chunkSize, Height / _chunkSize);
        }

        //    public Tile GetTopTile(int x, int y)
        //    {
        //        if (!IsInBounds(x, y))
        //            return null;

        //        int height = _heightMap[x, y];
        //        return GetTile(x, y, height);
        //    }

        //    /// <summary>
        //    /// Установить тайл по координатам
        //    /// </summary>
            public void SetTile(int x, int y, int z, Tile tile)
            {
                var chunk = GetChunkAtWorldPos(x, y);
                if (chunk == null) return;

                int localX = x % _chunkSize;
                int localY = y % _chunkSize;

                chunk.SetTile(localX, localY, z, tile);

                // Обновляем мировые координаты тайла
                if (tile != null)
                {
                    tile.SetWorldPosition(new Vector3(x, y, z));
                }
            }

            // === ОТРИСОВКА ===
            public void Draw(SpriteBatch spriteBatch)
            {
                foreach (var chunk in _chunks)
                {
                    chunk?.Draw(spriteBatch);
                }
            }

        //    // === ToString ===
        //    public override string ToString()
        //    {
        //        return $"TileGrid {Width}x{Height}x{Depth} (Chunks: {ChunksWidth}x{ChunksHeight})";
        //    }
        //}
    }
}
