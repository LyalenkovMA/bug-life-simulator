using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    public class HalfBlockTile : Tile
    {
        private readonly Texture2D _texture;
        private readonly Rectangle _sourceRect;

        public HalfBlockTile(Point gridPosition, int layer, Texture2D texture, Rectangle sourceRect)
            : base(gridPosition, layer,0.5f)
        {
            _texture = texture;
            _sourceRect = sourceRect;
            SetType(TileType.Stone);
            SetWalkable(true); // Обычно по полублокам можно ходить
            SetSolid(true);
        }

        protected override Texture2D GetTexture() => _texture;
        protected override Rectangle GetSourceRectangle() => _sourceRect;

        // Опционально: можно переопределить CalculateTileOffset, чтобы визуально 
        // полублок был ниже полного блока, если это не делается через Z-слой.
    }
}