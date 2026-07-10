using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Скос (0.5 высоты). Плавный подъём/спуск в одном из 4 направлений.
    /// </summary>
    public class SlopeTile : Tile
    {
        private readonly Texture2D _texture;
        private readonly Rectangle _sourceRect;

        public GridDirection Direction { get; }
        public override float Elevation => 0.5f; // 🔥 Скос = 0.5 высоты

        public SlopeTile(Point gridPosition, int layer, Texture2D texture, Rectangle sourceRect, GridDirection direction)
            : base(gridPosition, layer)
        {
            _texture = texture;
            _sourceRect = sourceRect;
            Direction = direction;
            SetType(TileType.Stone);
            SetWalkable(true);
            SetSolid(true);
        }

        protected override Texture2D GetTexture() => _texture;
        protected override Rectangle GetSourceRectangle() => _sourceRect;

        /// <summary>
        /// Переопределяем смещение для визуального эффекта склона.
        /// В зависимости от направления, тайл визуально "наклоняется".
        /// </summary>
        protected override Vector2 CalculateTileOffset(Rectangle sourceRect, float zoom)
        {
            Vector2 baseOffset = base.CalculateTileOffset(sourceRect, zoom);

            // В изометрии скос визуально смещается в зависимости от направления
            float slopeOffset = 0f;
            switch (Direction)
            {
                case GridDirection.Top:
                    slopeOffset = -sourceRect.Height * 0.25f; // Смещение вверх
                    break;
                case GridDirection.Bottom:
                    slopeOffset = sourceRect.Height * 0.25f;  // Смещение вниз
                    break;
                case GridDirection.Left:
                case GridDirection.Right:
                    // Для горизонтальных направлений можно добавить горизонтальное смещение
                    slopeOffset = 0f;
                    break;
            }

            return baseOffset + new Vector2(0, slopeOffset * zoom);
        }
    }
}