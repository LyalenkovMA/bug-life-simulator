using System.Collections.Generic;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TalesFromTheUnderbrush.src.Core.Tiles
{
    /// <summary>
    /// Чанк тайлов для оптимизации рендеринга
    /// </summary>
    public class TileChunk
    {
        public Point Position { get; private set; }
        public int Size { get; private set; }
        public bool IsDirty { get; set; } = true;
        public bool IsVisible { get; private set; } = true;

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
