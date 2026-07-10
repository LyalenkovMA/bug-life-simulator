using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using TalesFromTheUnderbrush.src;
using Point = Microsoft.Xna.Framework.Point;
using RectangleF = TalesFromTheUnderbrush.src.GameRectangleF;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Управление гридом тайлов — контейнер данных для комнаты.
    /// Автоматически чанкует данные. Поддерживает 3D-сетку (X, Y, Z).
    /// </summary>
    public class TileGrid : IDisposable
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Depth { get; private set; }
        public int ChunkSize { get; private set; }

        private readonly TileChunk[,] _chunks;
        public int ChunksWidth => _chunks.GetLength(0);
        public int ChunksHeight => _chunks.GetLength(1);

        public event Action<Tile> TileAdded;
        public event Action<Tile> TileRemoved;
        public event Action<TileGrid> GridChanged;

        public int TotalTiles { get; private set; }

        public TileGrid(int width, int height, int depth = 3)
        {
            if (width <= 0 || height <= 0 || depth <= 0)
                throw new ArgumentException("Dimensions must be positive");

            Width = width;
            Height = height;
            Depth = depth;
            ChunkSize = GameSetting.WorldChunkSize;

            int chunksX = (int)Math.Ceiling((float)width / ChunkSize);
            int chunksY = (int)Math.Ceiling((float)height / ChunkSize);

            _chunks = new TileChunk[chunksX, chunksY];

            for (int x = 0; x < chunksX; x++)
            {
                for (int y = 0; y < chunksY; y++)
                {
                    int chunkW = Math.Min(ChunkSize, width - x * ChunkSize);
                    int chunkH = Math.Min(ChunkSize, height - y * ChunkSize);
                    _chunks[x, y] = new TileChunk(new Point(x, y), chunkW, chunkH, depth);
                }
            }

            Console.WriteLine($"[TileGrid] Создана сетка {width}x{height}x{depth}, чанков: {chunksX}x{chunksY}");
        }

        // === УПРАВЛЕНИЕ ТАЙЛАМИ ===
        /// <summary>
        /// Установить тайл по мировым (глобальным) координатам
        /// </summary>
        public bool SetTile(int x, int y, int z, Tile tile)
        {
            if (!IsInBounds(x, y, z)) return false;

            TileChunk chunk = GetChunkAtWorldPos(x, y);
            if (chunk == null) return false;

            // Преобразуем мировые координаты в локальные координаты внутри чанка
            int localX = x % ChunkSize;
            int localY = y % ChunkSize;

            // Сохраняем старый тайл для событий и очистки памяти
            Tile oldTile = chunk.GetTile(localX, localY, z);
            if (oldTile != null)
            {
                TileRemoved?.Invoke(oldTile);
                oldTile.Dispose();
                TotalTiles--;
            }

            // Устанавливаем новый тайл в чанк
            chunk.SetTile(localX, localY, z, tile);

            if (tile != null)
            {
                // 🔥 КРИТИЧЕСКИ ВАЖНО: Принудительно задаём тайлу его глобальные координаты!
                // Без этого тайл будет "думать", что он находится в (0,0), и отрисовка сломается.
                tile.SetPosition(new Point(x, y), z);
                TileAdded?.Invoke(tile);
                TotalTiles++;
            }

            GridChanged?.Invoke(this);
            return true;
        }

        /// <summary>
        /// Получить тайл по мировым координатам
        /// </summary>
        public Tile GetTile(int x, int y, int z)
        {
            if (!IsInBounds(x, y, z)) return null;

            TileChunk chunk = GetChunkAtWorldPos(x, y);
            if (chunk == null) return null;

            return chunk.GetTile(x % ChunkSize, y % ChunkSize, z);
        }

        public bool RemoveTile(int x, int y, int z) => SetTile(x, y, z, null);

        // === РАБОТА С ЧАНКАМИ ===
        public TileChunk GetChunkAtWorldPos(int worldX, int worldY)
        {
            if (worldX < 0 || worldY < 0 || worldX >= Width || worldY >= Height)
                return null;

            int cx = worldX / ChunkSize;
            int cy = worldY / ChunkSize;

            if (cx >= 0 && cx < ChunksWidth && cy >= 0 && cy < ChunksHeight)
                return _chunks[cx, cy];

            return null;
        }

        public List<TileChunk> GetChunksInArea(RectangleF area)
        {
            var chunks = new List<TileChunk>();

            int startX = Math.Max(0, (int)(area.X / ChunkSize));
            int startY = Math.Max(0, (int)(area.Y / ChunkSize));
            int endX = Math.Min(ChunksWidth - 1, (int)((area.X + area.Width) / ChunkSize));
            int endY = Math.Min(ChunksHeight - 1, (int)((area.Y + area.Height) / ChunkSize));

            for (int cx = startX; cx <= endX; cx++)
            {
                for (int cy = startY; cy <= endY; cy++)
                {
                    if (_chunks[cx, cy] != null)
                        chunks.Add(_chunks[cx, cy]);
                }
            }

            return chunks;
        }

        public IEnumerable<Tile> GetAllTiles()
        {
            foreach (TileChunk chunk in _chunks)
            {
                if (chunk != null)
                {
                    foreach (Tile tile in chunk.GetAllTiles())
                        yield return tile;
                }
            }
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===
        public bool IsInBounds(int x, int y, int z = 0)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height && z >= 0 && z < Depth;
        }

        /// <summary>
        /// Получить верхний тайл в столбце (максимальный Z с тайлом)
        /// </summary>
        public Tile GetTopTile(int x, int y)
        {
            if (!IsInBounds(x, y)) return null;

            for (int z = Depth - 1; z >= 0; z--)
            {
                Tile tile = GetTile(x, y, z);
                if (tile != null) return tile;
            }

            return null;
        }

        /// <summary>
        /// Проверка проходимости на КОНКРЕТНОМ слое (Layer).
        /// Если слой не указан, проверяется верхний тайл (как раньше).
        /// </summary>
        public bool IsWalkable(int x, int y, int targetLayer = -1)
        {
            if (!IsInBounds(x, y, targetLayer >= 0 ? targetLayer : 0)) return false;

            if (targetLayer >= 0)
            {
                // Явная проверка слоя
                Tile tile = GetTile(x, y, targetLayer);
                return tile != null && tile.IsWalkable;
            }
            else
            {
                // Обратная совместимость: ищем верхний проходимый тайл
                for (int z = Depth - 1; z >= 0; z--)
                {
                    Tile tile = GetTile(x, y, z);
                    if (tile != null && tile.IsWalkable) return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Можно ли построить на клетке (проверка опоры)
        /// </summary>
        public bool IsBuildable(int x, int y, int layer)
        {
            if (!IsInBounds(x, y, layer)) return false;
            if (GetTile(x, y, layer) != null) return false; // Клетка занята

            // Проверка опоры для слоёв выше 0
            if (layer > 0)
            {
                Tile below = GetTile(x, y, layer - 1);
                return below != null && below.IsSolid;
            }

            return true;
        }

        // === ОБНОВЛЕНИЕ (ЛОГИКА, НЕ ОТРИСОВКА!) ===
        public void Update(GameTime gameTime)
        {
            // .ToList() предотвращает исключение при изменении коллекции во время итерации
            foreach (Tile tile in GetAllTiles().ToList())
                tile?.Update(gameTime);
        }

        // === ОЧИСТКА ===
        public void Clear()
        {
            foreach (TileChunk chunk in _chunks)
                chunk?.Clear();

            TotalTiles = 0;
            Console.WriteLine("[TileGrid] Сетка очищена");
        }

        public void Dispose()
        {
            Clear();
            TileAdded = null;
            TileRemoved = null;
            GridChanged = null;
        }
    }
}