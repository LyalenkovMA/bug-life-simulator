using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using TalesFromTheUnderbrush.src.GameLogic;
using TalesFromTheUnderbrush.src.Graphics;
using TalesFromTheUnderbrush.src.UI.Camera;
using IDrawable = TalesFromTheUnderbrush.src.Graphics.IDrawable;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TalesFromTheUnderbrush.src.Core.Entities
{
    /// <summary>
    /// Минимальный базовый класс для ВСЕХ объектов в игре
    /// Содержит только самое необходимое
    /// Теперь полностью реализует IDrawable
    /// </summary>
    public abstract class Entity : IDisposable, IDrawable
    {
        // === ID и имя ===
        private static ulong _nextId = 1;

        public ulong Id { get; }
        public string Name { get; private set; }
        public string Tag { get; private set; } = string.Empty;

        // === Реализация IDrawable ===
        private float _drawOrder;
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

        private bool _visible = true;
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

        // === Для обратной совместимости ===
        public virtual bool IsActive { get; set; } = true;
        public bool IsVisible
        {
            get => Visible;
            set => Visible = value;
        }

        public virtual bool ShouldBeRemoved { get; protected set; } = false;

        // Ссылка на мир (будет устанавливаться World при добавлении)
        public World World { get; set; }

        // === Позиция (2D координаты + высота) ===
        private Vector2 _position;
        public Vector2 Position
        {
            get => _position;
            private set
            {
                if (_position != value)
                {
                    Vector2 oldPos = _position;
                    _position = value;
                    OnPositionChanged?.Invoke(this, oldPos, value);
                    UpdateDrawOrder();
                }
            }
        }

        private float _height;

        public float Height
        {
            get => _height;
            private set
            {
                if (_height != value)
                {
                    float oldHeight = _height;
                    _height = Math.Max(0, value);
                    OnHeightChanged?.Invoke(this, oldHeight, value);
                    UpdateDrawOrder();
                }
            }
        }

        // === Размеры ===
        public float Width { get; private set; } = 1f;
        public float Depth { get; private set; } = 1f;

        // === Состояние ===
        public bool IsPersistent { get; private set; } = true;
        public bool IsDisposed { get; private set; }

        // === Ссылки ===
        public World GameWorld { get; internal set; }
        public Entity Parent { get; private set; }
        public List<Entity> Children { get; } = new();

        // === События ===
        public event Action<Entity> OnDisposed;
        public event Action<Entity, Vector2, Vector2> OnPositionChanged;
        public event Action<Entity, float, float> OnHeightChanged;
        public event Action<Entity> OnAddedToWorld;
        public event Action<Entity> OnRemovedFromWorld;

        // === Конструктор ===
        protected Entity(string name = null)
        {
            Id = _nextId++;
            Name = name ?? $"Entity_{Id}";

            DrawOrder = Id / 1000000f;

            OnPositionChanged += (entity, oldPos, newPos) => UpdateDrawOrder();
            OnHeightChanged += (entity, oldHeight, newHeight) => UpdateDrawOrder();
        }

        // === РЕАЛИЗАЦИЯ IDRAWABLE (ПОЛНАЯ) ===

        /// <summary>
        /// Основной метод отрисовки (из интерфейса IDrawable)
        /// </summary>
        public virtual void Draw(GameTime gameTime)
        {
            // Базовая реализация - ничего не делает
            // Наследники должны переопределить
        }

        /// <summary>
        /// Дополнительный метод отрисовки с SpriteBatch (из интерфейса IDrawable)
        /// </summary>
        public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // По умолчанию просто вызываем основной метод
            // Наследники могут переопределить для использования SpriteBatch
            Draw(gameTime);
        }

        // === Базовые методы ===
        public virtual void Initialize()
        {
            // Базовая реализация пустая
        }

        public virtual void Update(GameTime gameTime)
        {
            // Базовая реализация пустая
        }

        // === Публичные методы для изменения свойств ===
        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;

            Name = name;
        }

        public void SetTag(string tag) => Tag = tag ?? string.Empty;

        public void SetPosition(Vector2 position) => Position = position;

        public void SetPosition(float x, float y) => SetPosition(new Vector2(x, y));

        public void SetHeight(float height) => Height = height;

        public void SetSize(float width, float depth)
        {
            if (width <= 0 || depth <= 0)
                throw new ArgumentException("Width and depth must be positive");

            Width = width;
            Depth = depth;
        }

        public void SetSize(float size)
        {
            SetSize(size, size);
        }

        public void SetActive(bool active)
        {
            if (IsActive != active)
            {
                IsActive = active;
            }
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;
        }

        public void SetPersistent(bool persistent)
        {
            IsPersistent = persistent;
        }

        // === Иерархия ===
        public void AddChild(Entity child)
        {
            if (child == null || child == this || child.IsDisposed)
                return;

            if (child.Parent != null)
                child.Parent.RemoveChild(child);

            Children.Add(child);
            child.Parent = this;
            child.Position -= Position;
        }

        public void RemoveChild(Entity child)
        {
            if (child != null && Children.Remove(child))
            {
                child.Parent = null;
                child.Position += Position;
            }
        }

        /// <summary>
        /// Пометить сущность для удаления
        /// </summary>
        public virtual void MarkForRemoval()
        {
            ShouldBeRemoved = true;
            IsActive = false;
            Visible = false;
        }

        // === Коллизии ===
        public virtual Rectangle GetCollisionBounds()
        {
            return new Rectangle(
                (int)Position.X,
                (int)Position.Y,
                1, 1
            );
        }

        public virtual bool CheckCollision(Entity other)
        {
            if (other == null) return false;

            var bounds1 = GetCollisionBounds();
            var bounds2 = other.GetCollisionBounds();

            return bounds1.Intersects(bounds2);
        }

        // === Утилиты ===
        public Vector2 GetWorldPosition()
        {
            if (Parent == null)
                return Position;

            return Parent.GetWorldPosition() + Position;
        }

        public float GetWorldHeight()
        {
            if (Parent == null)
                return Height;

            return Parent.GetWorldHeight() + Height;
        }

        public Vector3 GetWorldPosition3D()
        {
            Vector2 pos2D = GetWorldPosition();
            return new Vector3(pos2D.X, pos2D.Y, GetWorldHeight());
        }

        public RectangleF GetBounds()
        {
            Vector2 worldPos = GetWorldPosition();
            return new RectangleF(
                worldPos.X - Width / 2,
                worldPos.Y - Depth / 2,
                Width,
                Depth
            );
        }

        public bool IsInView(ICamera camera)
        {
            if (camera == null) return true;

            RectangleF bounds = GetBounds();
            return camera.Bounds.Intersects(bounds);
        }

        // === Перемещение ===
        public void Move(Vector2 delta)
        {
            SetPosition(Position + delta);
        }

        public void Move(float deltaX, float deltaY)
        {
            Move(new Vector2(deltaX, deltaY));
        }

        public void MoveToHeight(float targetHeight, float speed = 1f)
        {
            if (speed <= 0)
                throw new ArgumentException("Speed must be positive");

            float newHeight = MathHelper.Lerp(Height, targetHeight, speed);
            SetHeight(newHeight);
        }

        // === Обновление порядка отрисовки ===
        protected virtual void UpdateDrawOrder()
        {
            Vector2 worldPos = GetWorldPosition();
            DrawOrder = 0.5f + (GetWorldHeight() * 0.05f) + (worldPos.Y * 0.0001f);
        }

        // === Очистка ===
        public virtual void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            IsActive = false;
            Visible = false;

            // Очищаем детей
            foreach (Entity child in Children.ToArray())
                child.Dispose();
            Children.Clear();

            // Отписываемся от родителя
            if (Parent != null)
            {
                Parent.RemoveChild(this);
                Parent = null;
            }

            // Отписываемся от событий World
            OnAddedToWorld = null;
            OnRemovedFromWorld = null;

            // Уведомляем подписчиков
            OnDisposed?.Invoke(this);

            // Очищаем все обработчики событий
            OnDisposed = null;
            OnPositionChanged = null;
            OnHeightChanged = null;

            // Очищаем события IDrawable
            DrawOrderChanged = null;
            VisibleChanged = null;
        }

        // === Для отладки ===
        public override string ToString()
        {
            Vector2 worldPos = GetWorldPosition();
            return $"{GetType().Name} '{Name}' ({worldPos.X:F1}, {worldPos.Y:F1}, {GetWorldHeight():F1}) [Visible: {Visible}, DrawOrder: {DrawOrder:F3}]";
        }
    }
}