using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Управление гридом тайлов — ЧИСТЫЙ КОНТЕЙНЕР ДАННЫХ.
    /// Не отвечает за отрисовку. Только хранение, доступ и обновление тайлов через чанки.
    /// </summary>
    public class TileGrid : IDisposable
    {
        // === РАЗМЕРЫ ГРИДА ===
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Depth { get; private set; }
        public int ChunkSize { get; private set; }

        // === ЧАНКИ (2D-массив чанков, каждый чанк — 3D-контейнер) ===
        private readonly TileChunk[,] _chunks;
        public int ChunksWidth => _chunks.GetLength(0);
        public int ChunksHeight => _chunks.GetLength(1);

        // === СОБЫТИЯ ===
        public event Action<Tile> TileAdded;
        public event Action<Tile> TileRemoved;
        public event Action<TileGrid> GridChanged;

        // === СТАТИСТИКА ===
        public int TotalTiles { get; private set; }

        // === КОНСТРУКТОР ===
        public TileGrid(int width, int height)
        {
            if (width <= 0 || height <= 0 || GameSetting.WorldChunkSize <= 0 || GameSetting.WorldChunkSize <= 0)
                throw new ArgumentException("Dimensions must be positive");

            Width = width;
            Height = height;
            Depth = GameSetting.WorldChunkSize;
            ChunkSize = GameSetting.WorldChunkSize;

            // Рассчитываем количество чанков
            int chunksX = (int)Math.Ceiling((float)width / GameSetting.WorldChunkSize);
            int chunksY = (int)Math.Ceiling((float)height / GameSetting.WorldChunkSize);
            _chunks = new TileChunk[chunksX, chunksY];

            // Инициализируем 3D-чанки
            for (int x = 0; x < chunksX; x++)
            {
                for (int y = 0; y < chunksY; y++)
                {
                    int chunkWidth = Math.Min(GameSetting.WorldChunkSize, width - x * GameSetting.WorldChunkSize);
                    int chunkHeight = Math.Min(GameSetting.WorldChunkSize, height - y * GameSetting.WorldChunkSize);
                    // Глубина чанка = глубина всего грида (для простоты)
                    _chunks[x, y] = new TileChunk(new Point(x, y), chunkWidth, chunkHeight, GameSetting.WorldChunkSize);
                }
            }

            Console.WriteLine($"[TileGrid] Создана сетка {width}x{height}x{GameSetting.WorldChunkSize}, чанков: {chunksX}x{chunksY}");
        }

        // === УПРАВЛЕНИЕ ТАЙЛАМИ ===
        /// <summary>
        /// Установить тайл по мировым координатам
        /// </summary>
        public bool SetTile(int x, int y, int z, Tile tile)
        {
            if (!IsInBounds(x, y, z)) return false;

            TileChunk chunk = GetChunkAtWorldPos(x, y);
            if (chunk == null) return false;

            // Преобразуем мировые координаты в локальные чанка
            int localX = x % ChunkSize;
            int localY = y % ChunkSize;

            // Сохраняем старый тайл для событий
            Tile oldTile = chunk.GetTile(localX, localY, z);
            if (oldTile != null)
            {
                TileRemoved?.Invoke(oldTile);
                oldTile.Dispose();
                TotalTiles--;
            }

            // Устанавливаем новый тайл
            chunk.SetTile(localX, localY, z, tile);
            if (tile != null) 
                tile.SetPosition(new Point(x, y), z);

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

            int localX = x % ChunkSize;
            int localY = y % ChunkSize;
            return chunk.GetTile(localX, localY, z);
        }

        /// <summary>
        /// Удалить тайл по координатам
        /// </summary>
        public bool RemoveTile(int x, int y, int z)
        {
            return SetTile(x, y, z, null);
        }

        // === РАБОТА С ЧАНКАМИ ===
        /// <summary>
        /// Получить чанк по мировым координатам
        /// </summary>
        public TileChunk GetChunkAtWorldPos(int worldX, int worldY)
        {
            if (worldX < 0 || worldY < 0 || worldX >= Width || worldY >= Height)
                return null;

            int chunkX = worldX / ChunkSize;
            int chunkY = worldY / ChunkSize;

            if (chunkX >= 0 && chunkX < ChunksWidth && chunkY >= 0 && chunkY < ChunksHeight)
                return _chunks[chunkX, chunkY];

            return null;
        }

        /// <summary>
        /// Получить все чанки в прямоугольной области (в мировых координатах)
        /// </summary>
        public List<TileChunk> GetChunksInArea(GameRectangleF area)
        {
            List<TileChunk> chunks = new List<TileChunk>();

            int startChunkX = (int)Math.Max(0, area.X / ChunkSize);
            int startChunkY = (int)Math.Max(0, area.Y / ChunkSize);
            int endChunkX = (int)Math.Min(ChunksWidth - 1, (area.X + area.Width) / ChunkSize);
            int endChunkY = (int)Math.Min(ChunksHeight - 1, (area.Y + area.Height) / ChunkSize);

            for (int cx = startChunkX; cx <= endChunkX; cx++)
            {
                for (int cy = startChunkY; cy <= endChunkY; cy++)
                {
                    if (_chunks[cx, cy] != null)
                        chunks.Add(_chunks[cx, cy]);
                }
            }
            return chunks;
        }

        /// <summary>
        /// Получить ВСЕ тайлы из ВСЕХ чанков (для обновления логики)
        /// </summary>
        public IEnumerable<Tile> GetAllTiles()
        {
            foreach (TileChunk chunk in _chunks)
            {
                if (chunk != null)
                {
                    foreach (Tile tile in chunk.GetAllTiles())
                    {
                        yield return tile;
                    }
                }
            }
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===
        /// <summary>
        /// Проверить границы грида
        /// </summary>
        public bool IsInBounds(int x, int y, int z = 0)
        {
            return x >= 0 && x < Width &&
                   y >= 0 && y < Height &&
                   z >= 0 && z < Depth;
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
        /// <summary>
        /// Обновить все тайлы (вызывается из World.Update)
        /// </summary>
        public void Update(GameTime gameTime)
        {
            foreach (Tile tile in GetAllTiles().ToList()) // .ToList() создаёт копию
                tile?.Update(gameTime);
        }

        // === ОЧИСТКА ===
        public void Clear()
        {
            foreach (TileChunk chunk in _chunks)
            {
                chunk?.Clear();
            }
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
