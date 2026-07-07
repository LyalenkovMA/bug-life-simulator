using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    public class FullBlockTile : Tile
    {
        public override float Elevation { get; set; }

        private readonly Texture2D _texture;
        private readonly Rectangle _sourceRect;

        public FullBlockTile(Point gridPosition, int layer, Texture2D texture, Rectangle sourceRect, bool isWalkable = true)
            : base(gridPosition, layer)
        {
            _texture = texture;
            _sourceRect = sourceRect;
            Elevation = 1.0f;
            SetType(TileType.Stone); // Или другой подходящий тип
            SetWalkable(isWalkable);
            SetSolid(true);
        }

        protected override Texture2D GetTexture() => _texture;
        protected override Rectangle GetSourceRectangle() => _sourceRect;
    }
}