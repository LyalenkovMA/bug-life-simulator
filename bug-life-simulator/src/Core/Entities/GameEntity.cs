using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace TalesFromTheUnderbrush.src.Core.Entities
{
    public abstract class GameEntity : Entity, IUpdatable, IDrawable
    {
        // === IUpdatable ===
        private int _updateOrder = 0;
        public int UpdateOrder
        {
            get => _updateOrder;
            private set
            {
                if (_updateOrder != value)
                {
                    _updateOrder = value;
                    UpdateOrderChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler UpdateOrderChanged;

        public void SetUpdateOrder(int order)
        {
            UpdateOrder = order;
        }

        // === IDrawable ===
        private float _drawDepth = 0;
        public float DrawDepth
        {
            get => _drawDepth;
            private set
            {
                if (_drawDepth != value)
                {
                    _drawDepth = value;
                    DrawDepthChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool _visible = true;
        public bool Visible
        {
            get => _visible;
            private set
            {
                if (_visible != value)
                {
                    _visible = value;
                    VisibleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler DrawDepthChanged;
        public event EventHandler VisibleChanged;

        public void SetDrawDepth(float depth)
        {
            DrawDepth = depth;
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;
            base.SetVisible(visible); // Вызываем базовый метод
        }

        // === Конструктор ===
        protected GameEntity(string name = null) : base(name)
        {
            // Автоматически вычисляем глубину отрисовки на основе высоты
            this.OnHeightChanged += (entity, oldH, newH) => UpdateDrawDepth();
        }

        // === Реализация интерфейсов ===
        public abstract void Update(GameTime gameTime);

        public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);

        // === Утилиты ===
        protected virtual void UpdateDrawDepth()
        {
            // Базовый расчет глубины: чем выше объект, тем позже рисуется
            SetDrawDepth(0.5f + (GetWorldHeight() * 0.05f));
        }
    }
}
