using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Полублок (0.5 высоты). Ступенька, низкий бордюр, выступ.
    /// </summary>
    public class HalfBlockTile : Tile
    {
        private readonly Texture2D _texture;
        private readonly Rectangle _sourceRect;

        public override float Elevation => 0.5f; // 🔥 Полублок = 0.5 высоты

        public HalfBlockTile(Point gridPosition, int layer, Texture2D texture, Rectangle sourceRect)
            : base(gridPosition, layer)
        {
            _texture = texture;
            _sourceRect = sourceRect;
            SetType(TileType.Stone);
            SetWalkable(true);
            SetSolid(true);
        }

        protected override Texture2D GetTexture() => _texture;
        protected override Rectangle GetSourceRectangle() => _sourceRect;
    }
}