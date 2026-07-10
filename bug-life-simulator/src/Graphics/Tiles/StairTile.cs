using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Лестница (0.5 высоты). Разрешает вертикальный переход между слоями.
    /// </summary>
    public class StairTile : Tile
    {
        private readonly Texture2D _texture;
        private readonly Rectangle _sourceRect;

        public GridDirection Direction { get; }
        public override float Elevation => 0.5f; // 🔥 Лестница = 0.5 высоты

        // 🔥 КРИТИЧНО: Лестница разрешает смену Z-слоя
        public override bool AllowsZTransition => true;

        public StairTile(Point gridPosition, int layer, Texture2D texture, Rectangle sourceRect, GridDirection direction)
            : base(gridPosition, layer)
        {
            _texture = texture;
            _sourceRect = sourceRect;
            Direction = direction;
            SetType(TileType.Wood);
            SetWalkable(true);
            SetSolid(true);

            // Целевой слой зависит от направления
            // Если лестница ведёт "вверх" (Top), то TargetLayer = Layer + 1
            // Если "вниз" (Bottom), то TargetLayer = Layer - 1
            TargetLayer = (Direction == GridDirection.Top) ? layer + 1 : layer - 1;
        }

        protected override Texture2D GetTexture() => _texture;
        protected override Rectangle GetSourceRectangle() => _sourceRect;
    }
}