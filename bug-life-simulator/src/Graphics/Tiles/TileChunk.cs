using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Point = Microsoft.Xna.Framework.Point;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Чанк тайлов — чистый 3D-контейнер данных.
    /// Не отвечает за отрисовку. Только хранение и предоставление тайлов миру.
    /// </summary>
    public class TileChunk : IDisposable
    {
        // === ПОЗИЦИЯ И РАЗМЕРЫ ===
        public Point Position { get; }          // Позиция чанка в мировой сетке (в чанках)
        public int Width { get; }               // Ширина чанка в тайлах (X)
        public int Height { get; }              // Высота чанка в тайлах (Y)
        public int Depth { get; }               // Глубина чанка в тайлах (Z)

        // === ФЛАГИ ===
        public bool IsDirty { get; set; } = true; // Флаг изменения (для будущей оптимизации кэширования)

        // === ХРАНЕНИЕ ТАЙЛОВ ===
        private readonly Tile[,,] _tiles; // 3D-массив: [локальный X, локальный Y, локальный Z]

        // === КОНСТРУКТОР ===
        /// <summary>
        /// Создаёт чанк заданного размера.
        /// </summary>
        /// <param name="position">Позиция чанка в мировой сетке (в чанках)</param>
        /// <param name="width">Ширина в тайлах (рекомендуется 64)</param>
        /// <param name="height">Высота в тайлах (рекомендуется 64)</param>
        /// <param name="depth">Глубина в тайлах (рекомендуется 32)</param>
        public TileChunk(Point position, int width, int height, int depth)
        {
            Position = position;
            Width = width;
            Height = height;
            Depth = depth;
            _tiles = new Tile[width, height, depth];
        }

        // === УСТАНОВКА/ПОЛУЧЕНИЕ ТАЙЛОВ ===
        /// <summary>
        /// Устанавливает тайл в чанк по локальным координатам.
        /// </summary>
        public void SetTile(int x, int y, int z, Tile tile)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
                return;

            // Освобождаем старый тайл, если он был
            _tiles[x, y, z]?.Dispose();
            _tiles[x, y, z] = tile;
            IsDirty = true;
        }

        /// <summary>
        /// Получает тайл по локальным координатам.
        /// </summary>
        public Tile GetTile(int x, int y, int z)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
                return null;

            return _tiles[x, y, z];
        }

        /// <summary>
        /// Удаляет тайл по локальным координатам.
        /// </summary>
        public void RemoveTile(int x, int y, int z)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
                return;

            _tiles[x, y, z]?.Dispose();
            _tiles[x, y, z] = null;
            IsDirty = true;
        }

        // === ПЕРЕБОР ТАЙЛОВ ===
        /// <summary>
        /// Возвращает ВСЕ видимые тайлы чанка для отрисовки миром.
        /// </summary>
        public IEnumerable<Tile> GetAllVisibleTiles()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int z = 0; z < Depth; z++)
                    {
                        var tile = _tiles[x, y, z];
                        if (tile != null && tile.Visible)
                            yield return tile;
                    }
                }
            }
        }

        /// <summary>
        /// Возвращает ВСЕ тайлы чанка (включая невидимые) для обновления/логики.
        /// </summary>
        public IEnumerable<Tile> GetAllTiles()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int z = 0; z < Depth; z++)
                    {
                        if (_tiles[x, y, z] != null)
                            yield return _tiles[x, y, z];
                    }
                }
            }
        }

        // === ОЧИСТКА ===
        /// <summary>
        /// Очищает чанк и освобождает все тайлы.
        /// </summary>
        public void Clear()
        {
            foreach (Tile tile in GetAllTiles())
                tile.Dispose();

            Array.Clear(_tiles, 0, _tiles.Length);
            IsDirty = true;
        }

        public void Dispose() => Clear();
    }
}