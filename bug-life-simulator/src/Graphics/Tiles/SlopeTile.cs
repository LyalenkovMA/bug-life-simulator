using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    public class SlopeTile : Tile
    {
        public override float Elevation { get; set; }

        private readonly Texture2D _texture;
        private readonly Rectangle _sourceRect;
        public GridDirection Direction { get; }

        public SlopeTile(Point gridPosition, int layer, Texture2D texture, Rectangle sourceRect, GridDirection direction)
            : base(gridPosition, layer)
        {
            _texture = texture;
            _sourceRect = sourceRect;
            Direction = direction;
            Elevation = 0.5f;
            SetType(TileType.Stone);
            SetWalkable(true);
            SetSolid(true);
        }

        protected override Texture2D GetTexture() => _texture;
        protected override Rectangle GetSourceRectangle() => _sourceRect;

        protected override Vector2 CalculateTileOffset(Rectangle sourceRect, float zoom)
        {
            Vector2 baseOffset = CalculateTileOffset(sourceRect, zoom);

            // В изометрии скос визуально смещается в зависимости от направления
            // Например, скос "вверх" (Top) рисует наклон от задней части к передней
            float slopeOffset = 0f;
            switch (Direction)
            {
                case GridDirection.Top:
                    slopeOffset = -sourceRect.Height * 0.25f; // Смещение вверх
                    break;
                case GridDirection.Bottom:
                    slopeOffset = sourceRect.Height * 0.25f; // Смещение вниз
                    break;
                    // Left/Right требуют горизонтального смещения
            }

            return baseOffset + new Vector2(0, slopeOffset * zoom);
        }

        // Скос визуально может требовать особого смещения, но базовый Tile.Draw 
        // уже хорошо справляется с изометрией.
    }
}