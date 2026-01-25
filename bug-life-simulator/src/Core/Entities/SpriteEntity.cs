using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using TalesFromTheUnderbrush.src.Graphics;

namespace TalesFromTheUnderbrush.src.Core.Entities
{
    /// <summary>
    /// Базовая сущность с поддержкой спрайтов
    /// </summary>
    public abstract class SpriteEntity : Entity, IRequiresSpriteBatch
    {
        protected Texture2D Texture { get; private set; }
        protected SpriteBatch CurrentSpriteBatch { get; private set; }

        public Color TintColor { get; set; } = Color.White;
        public Vector2 Origin { get; set; } = Vector2.Zero;
        public SpriteEffects SpriteEffects { get; set; } = SpriteEffects.None;

        public SpriteEntity(string name = null) : base(name)
        {
        }

        public void SetTexture(Texture2D texture)
        {
            Texture = texture;
            if (texture != null)
            {
                Origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            }
        }

        // Реализация IRequiresSpriteBatch
        public void SetSpriteBatch(SpriteBatch spriteBatch)
        {
            CurrentSpriteBatch = spriteBatch;
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible || CurrentSpriteBatch == null || Texture == null)
                return;

            Draw(gameTime, CurrentSpriteBatch);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!Visible || Texture == null) return;

            Vector2 worldPos = GetWorldPosition();
            float worldHeight = GetWorldHeight();

            // Рассчитываем позицию с учетом высоты (2.5D)
            Vector2 drawPosition = new Vector2(
                worldPos.X,
                worldPos.Y - worldHeight * 0.5f
            );

            spriteBatch.Draw(
                Texture,
                drawPosition,
                null,
                TintColor,
                0f,
                Origin,
                1f,
                SpriteEffects,
                DrawOrder / 1000f // Нормализуем для LayerDepth
            );
        }
    }
}
