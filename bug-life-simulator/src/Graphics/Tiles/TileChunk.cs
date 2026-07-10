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
        public TileChunk(Point position, int width, int height, int depth)
        {
            Position = position;
            Width = width;
            Height = height;
            Depth = depth;
            _tiles = new Tile[width, height, depth];
        }

        // === УСТАНОВКА/ПОЛУЧЕНИЕ ТАЙЛОВ ===
        public void SetTile(int x, int y, int z, Tile tile)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
                return;

            _tiles[x, y, z]?.Dispose();
            _tiles[x, y, z] = tile;
            IsDirty = true;
        }

        public Tile GetTile(int x, int y, int z)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
                return null;

            return _tiles[x, y, z];
        }

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
        /// Порядок: снизу вверх (Z=0 → Z=Depth-1) для корректной изометрии.
        /// </summary>
        public IEnumerable<Tile> GetAllVisibleTiles()
        {
            for (int z = 0; z < Depth; z++)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        Tile tile = _tiles[x, y, z];
                        if (tile != null && tile.Visible)
                            yield return tile;
                    }
                }
            }
        }

        public IEnumerable<Tile> GetAllTiles()
        {
            for (int z = 0; z < Depth; z++)
            {
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        if (_tiles[x, y, z] != null)
                            yield return _tiles[x, y, z];
                    }
                }
            }
        }

        // === ОЧИСТКА ===
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