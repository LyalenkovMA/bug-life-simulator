using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    public class GrassTile : Tile
    {
        private Texture2D _texture;

        public GrassTile(Point gridPosition, int layer) : base(gridPosition, layer)
        {
            Type = TileType.Grass;
            SetWalkableInternal(true);
            SetTintColor(Color.Green);
        }

        public void SetTexture(Texture2D texture)
        {
            _texture = texture;
        }

        protected override void DrawTile(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (_texture != null)
            {
                // Отрисовка с текстурой
                spriteBatch.Draw(_texture,
                    new Rectangle(
                        (int)WorldPosition.X - TileSize.Width / 2,
                        (int)WorldPosition.Y - TileSize.Height / 2,
                        TileSize.Width,
                        TileSize.Height
                    ),
                    SourceRect,
                    TintColor,
                    Rotation,
                    Vector2.Zero,
                    SpriteEffects.None,
                    DrawOrder / 10000f // Нормализуем для LayerDepth
                );
            }
            else
            {
                // Запасной вариант: цветной квадрат
                var pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                pixel.SetData(new[] { TintColor });

                spriteBatch.Draw(pixel,
                    new Rectangle(
                        (int)WorldPosition.X - TileSize.Width / 2,
                        (int)WorldPosition.Y - TileSize.Height / 2,
                        TileSize.Width,
                        TileSize.Height
                    ),
                    TintColor
                );

                pixel.Dispose();
            }
        }
    }
}