using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Управление гридом тайлов (отдельно от World)
    /// Оптимизирован для быстрого доступа к тайлам
    /// </summary>
    public class TileGrid : IDisposable, IDrawable, IRequiresSpriteBatch
    {
        // === Размеры грида ===
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int Layers { get; private set; }
        public int Depth { get; private set; }

        public int ChunksWidth => _chunks.GetLength(0);
        public int ChunksHeight => _chunks.GetLength(1);

        private SpriteBatch _currentSpriteBatch;
        public float DrawOrder { get; set; } = 0; // Чанки отсортируют себя внутри
        public bool Visible { get; set; } = true;
        public event EventHandler DrawOrderChanged;
        public event EventHandler VisibleChanged;

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

                    _chunks[x, y] = new TileChunk(new Point(x, y), chunkSize);
                }
            }

            Console.WriteLine($"[TileGrid] Создана сетка {width}x{height}x{depth}, чанков: {chunksX}x{chunksY}");
        }

        // === Публичные методы для работы с тайлами ===

        /// <summary>
        /// Создать и добавить тайл в указанную позицию
        /// </summary>
        public T CreateTile<T>(int x, int y, int z = 0) where T : Tile, new()
        {
            if (!IsInBounds(x, y, z))
                return null;

            var tile = new T();
            if (SetTile(x, y, z, tile))
            {
                return tile;
            }
            return null;
        }

        /// <summary>
        /// Создать тайл с инициализацией
        /// </summary>
        public Tile CreateTile(int x, int y, int z, TileType type, Color? color = null)
        {
            if (!IsInBounds(x, y, z))
                return null;

            Tile tile = type switch
            {
                TileType.Grass => new GrassTile(new Point(x, y), z),
                //TileType.Dirt => new DirtTile(new Point(x, y), z),
                //TileType.Stone => new StoneTile(new Point(x, y), z),
                //TileType.Water => new WaterTile(new Point(x, y), z),
                //_ => new BasicTile(new Point(x, y), z)
            };

            if (color.HasValue)
            {
                tile.SetTintColor(color.Value);
            }

            if (SetTile(x, y, z, tile))
            {
                return tile;
            }
            return null;
        }

        /// <summary>
        /// Удалить тайл по координатам
        /// </summary>
        public bool RemoveTile(int x, int y, int z)
        {
            return SetTile(x, y, z, null);
        }

        /// <summary>
        /// Получить все тайлы в области
        /// </summary>
        public List<Tile> GetTilesInArea(Rectangle area)
        {
            var tiles = new List<Tile>();

            // Определяем границы в координатах сетки
            int startX = Math.Max(0, area.X / Tile.TileSize.Width);
            int endX = Math.Min(Width - 1, (area.Right) / Tile.TileSize.Width);
            int startY = Math.Max(0, area.Y / Tile.TileSize.Height);
            int endY = Math.Min(Height - 1, (area.Bottom) / Tile.TileSize.Height);

            for (int x = startX; x <= endX; x++)
            {
                for (int y = startY; y <= endY; y++)
                {
                    for (int z = 0; z < Depth; z++)
                    {
                        var tile = GetTile(x, y, z);
                        if (tile != null)
                        {
                            tiles.Add(tile);
                        }
                    }
                }
            }

            return tiles;
        }

        /// <summary>
        /// Обновить все тайлы (вызывается из World.Update)
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // Можно оптимизировать, обновляя только видимые тайлы
            foreach (var tile in GetVisibleTiles())
            {
                tile?.Update(gameTime);
            }

            // Обновляем состояние чанков
            foreach (var chunk in _chunks)
            {
                // Если чанк реализует IUpdatable
                if (chunk is IUpdatable updatableChunk)
                {
                    updatableChunk.Update(gameTime);
                }
            }
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

        public void Dispose()
        {
            Clear(); // Очищает все чанки
                     // Дополнительно можно очистить _spatialGrid, если в нём есть метод Dispose
            if (_spatialGrid is IDisposable disposableGrid)
            {
                disposableGrid.Dispose();
            }
            GridChanged = null; // Отписываем всех от события
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
            // Было: return new Point(Width / _chunkSize, Height / _chunkSize); // Неверно!
            // Стало:
            return new Point(gridPos.X / _chunkSize, gridPos.Y / _chunkSize);
        }

        //    public Tile GetTopTile(int x, int y)
        //    {
        //        if (!IsInBounds(x, y))
        //            return null;

        //        int height = _heightMap[x, y];
        //        return GetTile(x, y, height);
        //    }

        /// <summary>
        /// Установить или удалить тайл по координатам. Главный метод для изменения сетки.
        /// </summary>
        public bool SetTile(int x, int y, int z, Tile tile)
        {
            if (!IsInBounds(x, y, z))
                return false;

            // Получаем старый тайл
            Tile oldTile = _tiles[x, y, z];
            TileChunk chunk = GetChunkAtWorldPos(x, y);

            if (oldTile != null)
            {
                // Удаляем старый тайл
                RemoveTileInternal(oldTile, x, y, z);
            }

            // Устанавливаем новый
            _tiles[x, y, z] = tile;

            if (tile != null)
            {
                // Устанавливаем позицию через внутренний метод
                tile.SetPositionInternal(new Point(x, y), z);

                // Добавляем в системы
                AddTileInternal(tile, x, y, z);
            }

            // Обновляем чанк
            if (chunk != null)
            {
                int localX = x % _chunkSize;
                int localY = y % _chunkSize;
                chunk.SetTile(localX, localY, z, tile);
            }

            GridChanged?.Invoke(this);
            return true;
        }

        private void AddTileInternal(Tile tile, int x, int y, int z)
        {
            // Добавляем в SpatialGrid
            var bounds = new RectangleF(
                tile.WorldPosition.X - Tile.TileSize.Width / 2,
                tile.WorldPosition.Y - Tile.TileSize.Height / 2,
                Tile.TileSize.Width,
                Tile.TileSize.Height
            );
            _spatialGrid.Add(tile, bounds);

            TotalTiles++;
            TileAdded?.Invoke(tile);

            // Обновляем карту высот
            if (z >= _heightMap[x, y])
            {
                _heightMap[x, y] = z;
            }
        }

        private void RemoveTileInternal(Tile tile, int x, int y, int z)
        {
            // Удаляем из SpatialGrid
            _spatialGrid.Remove(tile);

            TotalTiles--;
            TileRemoved?.Invoke(tile);
            tile.Dispose();

            // Обновляем карту высот если нужно
            if (z == _heightMap[x, y])
            {
                // Ищем новый верхний тайл
                for (int layer = z - 1; layer >= 0; layer--)
                {
                    if (_tiles[x, y, layer] != null)
                    {
                        _heightMap[x, y] = layer;
                        break;
                    }
                }
            }
        }

        [Obsolete("Используйте Draw(GameTime gameTime) или Draw(GameTime gameTime, SpriteBatch spriteBatch)")]
        public void Draw(SpriteBatch spriteBatch)
        {
            // Для временной совместимости просто вызываем новую версию
            // GameTime можно передать null или заглушку, если это не влияет на рендеринг тайлов
            Draw(null, spriteBatch);
        }

        // === ОТРИСОВКА ===
        public void Draw(GameTime gameTime)
        {
            if (!Visible || _currentSpriteBatch == null) return;

            // Используем перегрузку с SpriteBatch
            Draw(gameTime, _currentSpriteBatch);
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!Visible) return;

            // Устанавливаем SpriteBatch для всех видимых чанков
            // и делегируем отрисовку им
            // В реальном сценарии здесь должна быть проверка видимости области (culling)
            foreach (TileChunk chunk in _chunks)
            {
                if (chunk != null && chunk.Visible)
                {
                    // Передаём SpriteBatch в чанк для отрисовки его тайлов
                    if (chunk is IRequiresSpriteBatch chunkWithBatch)
                    {
                        chunkWithBatch.SetSpriteBatch(spriteBatch);
                    }
                    chunk.Draw(gameTime); // Чанк реализует IDrawable
                }
            }
        }

        public void SetSpriteBatch(SpriteBatch spriteBatch)
        {
            _currentSpriteBatch = spriteBatch;
        }

        /// <summary>
        /// Вспомогательный метод для поиска верхнего непустого слоя в столбце.
        /// </summary>
        private int FindTopTileLayer(int x, int y)
        {
            for (int layer = Depth - 1; layer >= 0; layer--)
            {
                if (_tiles[x, y, layer] != null)
                    return layer;
            }
            return -1; // Столбец пуст
        }

        //    // === ToString ===
        //    public override string ToString()
        //    {
        //        return $"TileGrid {Width}x{Height}x{Depth} (Chunks: {ChunksWidth}x{ChunksHeight})";
        //    }

    }
}
