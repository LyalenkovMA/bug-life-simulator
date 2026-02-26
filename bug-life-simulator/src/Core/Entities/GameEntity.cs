using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TalesFromTheUnderbrush.src.Graphics;

namespace TalesFromTheUnderbrush.src.Core.Entities
{
    /// <summary>
    /// Базовый класс для всех сущностей в игре.
    /// Реализует IUpdattGameEntity и IRenderable для единой архитектуры.
    /// </summary>
    public abstract class GameEntity : Entity, IUpdattGameEntity, IRenderable
    {
        // === IUpdattGameEntity ===
        private int _updateOrder = 0;
        public int UpdateOrder
        {
            get => _updateOrder;
            set
            {
                if (_updateOrder != value)
                {
                    _updateOrder = value;
                    UpdateOrderChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler UpdateOrderChanged;

        public void SetUpdateOrder(int order) => UpdateOrder = order;

        // === IRenderable ===
        private float _drawOrder = 0.5f;
        public float DrawOrder
        {
            get => _drawOrder;
            set
            {
                if (Math.Abs(_drawOrder - value) > float.Epsilon)
                {
                    _drawOrder = value;
                    DrawOrderChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler DrawOrderChanged;
        public event EventHandler VisibleChanged;

        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    VisibleChanged?.Invoke(this, EventArgs.Empty);
                    base.SetVisible(value); // Синхронизация с базовым классом
                }
            }
        }
        private bool _visible = true;

        public void SetVisible(bool visible) => Visible = visible;

        // === Конструктор ===
        protected GameEntity(string name = null) : base(name)
        {
            // Автоматически вычисляем глубину отрисовки на основе высоты
            OnHeightChanged += (entity, oldH, newH) => UpdateDrawDepth();
        }

        // === Утилиты ===
        protected virtual void UpdateDrawDepth()
        {
            // Базовый расчет глубины: чем выше объект, тем позже рисуется
            DrawOrder = 0.5f + (GetWorldHeight() * 0.05f);
        }

        // === IRenderable.Draw — переопределяется в наследниках ===
        
        // === IUpdattGameEntity.Update — переопределяется в наследниках ===
        public override void Update(GameTime gameTime)
        {
            // Базовая логика обновления
            UpdateDrawDepth();
        }
    }
}