using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using TalesFromTheUnderbrush.src.Graphics;
using IDrawable = TalesFromTheUnderbrush.src.Graphics.IDrawable;

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

        // === Конструктор ===
        protected GameEntity(string name = null) : base(name)
        {
            // Автоматически вычисляем глубину отрисовки на основе высоты
            this.OnHeightChanged += (entity, oldH, newH) => UpdateDrawDepth();
        }

        // === Реализация интерфейсов ===
        public abstract void Update(GameTime gameTime);

        // У GameEntity своя специфическая отрисовка с SpriteBatch
        public override void Draw(GameTime gameTime)
        {
            // Базовая реализация - ничего не делает
            // Наследники должны переопределить Draw(GameTime, SpriteBatch)
        }

        // Это основной метод для GameEntity
        public abstract void Draw(GameTime gameTime, SpriteBatch spriteBatch);

        // === Утилиты ===
        protected virtual void UpdateDrawDepth()
        {
            // Базовый расчет глубины: чем выше объект, тем позже рисуется
            SetDrawDepth(0.5f + (GetWorldHeight() * 0.05f));
        }

        // Методы для управления DrawOrder (наследуются от Entity)
        public void SetDrawDepth(float depth)
        {
            // Используем protected setter из Entity
            DrawOrder = depth;
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;
            base.SetVisible(visible);
        }
    }
}