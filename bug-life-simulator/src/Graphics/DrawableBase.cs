using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TalesFromTheUnderbrush.src.Graphics
{
    /// <summary>
    /// Базовая реализация IDrawable с поддержкой событий
    /// </summary>
    public abstract class DrawableBase : IDrawable, IHasDrawOrder, IHasVisibility
    {
        private float _drawOrder;
        private bool _visible = true;

        public float DrawOrder
        {
            get => _drawOrder;
            protected set
            {
                if (Math.Abs(_drawOrder - value) > 0.001f)
                {
                    _drawOrder = value;
                    DrawOrderChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool Visible
        {
            get => _visible;
            protected set
            {
                if (_visible != value)
                {
                    _visible = value;
                    VisibleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler DrawOrderChanged;
        public event EventHandler VisibleChanged;

        public virtual void Draw(GameTime gameTime)
        {
            // Базовая реализация
        }

        public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Базовая реализация с SpriteBatch
            Draw(gameTime);
        }

        public void SetDrawOrder(float order)
        {
            DrawOrder = order;
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;
        }
    }
}
