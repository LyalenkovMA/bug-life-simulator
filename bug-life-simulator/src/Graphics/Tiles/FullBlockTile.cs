using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Полный блок (1.0 высоты). Стандартный пол, стена, платформа.
    /// </summary>
    public class FullBlockTile : Tile
    {
        private readonly Texture2D _texture;
        private readonly Rectangle _sourceRect;

        public override float Elevation => 1.0f; // 🔥 Полный блок = 1.0 высоты

        public FullBlockTile(Point gridPosition, int layer, Texture2D texture, Rectangle sourceRect, bool isWalkable = true)
            : base(gridPosition, layer)
        {
            _texture = texture;
            _sourceRect = sourceRect;
            SetType(TileType.Stone);
            SetWalkable(isWalkable);
            SetSolid(true);
        }

        protected override Texture2D GetTexture() => _texture;
        protected override Rectangle GetSourceRectangle() => _sourceRect;
    }
}