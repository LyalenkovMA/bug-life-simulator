using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using TalesFromTheUnderbrush.src.UI.Camera;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TalesFromTheUnderbrush.src.Core.Entities
{
    /// <summary>
    /// Минимальный базовый класс для ВСЕХ объектов в игре
    /// Содержит только самое необходимое
    /// </summary>
    public abstract class Entity : IDisposable
    {
        // === ID и имя ===
        private static ulong _nextId = 1;

        public ulong Id { get; }
        public string Name { get; private set; }
        public string Tag { get; private set; } = string.Empty;

        public virtual bool IsActive { get; set; } = true;
        public virtual bool IsVisible { get; set; } = true;
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
                    _height = Math.Max(0, value); // Высота не может быть отрицательной
                    OnHeightChanged?.Invoke(this, oldHeight, value);
                }
            }
        }

        // === Размеры ===
        public float Width { get; private set; } = 1f;
        public float Depth { get; private set; } = 1f;

        // === Состояние ===
        public bool IsPersistent { get; private set; } = true; // Сохраняется между сессиями
        public bool IsDisposed { get; private set; }

        // === Ссылки ===
        public World World { get; internal set; }
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
            if (IsVisible != visible)
            {
                IsVisible = visible;
            }
        }

        public void SetPersistent(bool persistent)
        {
            IsPersistent = persistent;
        }

        // === Базовые методы (опциональные) ===
        public abstract void Initialize();
        public abstract void Update(GameTime gameTime);
        public abstract void Draw(GameTime gameTime);

        // === Иерархия ===
        public void AddChild(Entity child)
        {
            if (child == null || child == this || child.IsDisposed)
                return;

            if (child.Parent != null)
                child.Parent.RemoveChild(child);

            Children.Add(child);
            child.Parent = this;

            // Пересчитываем позицию относительно родителя
            child.Position -= Position;
        }

        /// <summary>
        /// Пометить сущность для удаления
        /// </summary>
        public virtual void MarkForRemoval()
        {
            ShouldBeRemoved = true;
            IsActive = false;
            IsVisible = false;
        }

        public virtual Rectangle GetCollisionBounds()
        {
            // Базовая реализация - размер 1x1 тайла
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

        public void RemoveChild(Entity child)
        {
            if (child != null && Children.Remove(child))
            {
                child.Parent = null;
                child.Position += Position;
            }
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

        public bool IsInView(CameraBase camera)
        {
            RectangleF bounds = GetBounds();
            return camera.Bound.Intersects(bounds);
        }

        // === Перемещение с delta ===
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

        // === Очистка ===
        public virtual void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;
            SetActive(false);
            SetVisible(false);

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

            // Уведомляем подписчиков
            OnDisposed?.Invoke(this);

            // Очищаем все обработчики событий
            OnDisposed = null;
            OnPositionChanged = null;
            OnHeightChanged = null;
            OnAddedToWorld = null;
            OnRemovedFromWorld = null;
        }

        // === Для отладки ===
        public override string ToString()
        {
            Vector2 worldPos = GetWorldPosition();
            return $"{GetType().Name} '{Name}' ({worldPos.X:F1}, {worldPos.Y:F1}, {GetWorldHeight():F1})";
        }
    }
}