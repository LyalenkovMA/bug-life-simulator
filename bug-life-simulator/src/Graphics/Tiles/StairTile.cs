using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    public class StairTile : Tile
    {
        private readonly Texture2D _texture;
        private readonly Rectangle _sourceRect;
        public GridDirection Direction { get; }

        public StairTile(Point gridPosition, int layer, Texture2D texture, Rectangle sourceRect, GridDirection direction)
            : base(gridPosition, layer, 0.5f)
        {
            _texture = texture;
            _sourceRect = sourceRect;
            Direction = direction;
            SetType(TileType.Wood); // Или Stone
            SetWalkable(true);
            SetSolid(true);

            // ВАЖНО: Здесь можно добавить специальный флаг или свойство, 
            // которое скажет контроллеру движения, что этот тайл позволяет сменить Z-слой.
            // Например, если у вас есть свойство в базовом Tile:
            // this.AllowsZTransition = true; 
        }

        protected override Texture2D GetTexture() => _texture;
        protected override Rectangle GetSourceRectangle() => _sourceRect;
    }
}