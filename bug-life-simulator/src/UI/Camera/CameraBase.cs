using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Drawing;
using TalesFromTheUnderbrush.src.Graphics;

namespace TalesFromTheUnderbrush.src.UI.Camera
{
    public abstract class CameraBase : ICamera
    {
        // === ПРИВАТНЫЕ ПОЛЯ ===
        private Vector3 _position;
        private Vector3 _target;
        private Matrix _viewMatrix;
        private Matrix _projectionMatrix;
        private Matrix _viewProjectionMatrix;
        private int _viewportWidth;
        private int _viewportHeight;

        // === РЕАЛИЗАЦИЯ IUpdatable и IDrawable ===
        private int _updateOrder = 0;
        private float _drawDepth = 0.5f; // Средняя глубина
        private bool _visible = true; // Камеры обычно невидимы, но свойство должно быть

        public int UpdateOrder
        {
            get => _updateOrder;
            protected set
            {
                if (_updateOrder != value)
                {
                    _updateOrder = value;
                    UpdateOrderChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public float DrawDepth
        {
            get => _drawDepth;
            protected set
            {
                if (Math.Abs(_drawDepth - value) > float.Epsilon)
                {
                    _drawDepth = value;
                    DrawDepthChanged?.Invoke(this, EventArgs.Empty);
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

        public RectangleF Bounds
        {
            get
            {
                // Для ортографической камеры
                Vector3 topLeftWorld = ScreenToWorld(Vector2.Zero, 0);
                Vector3 bottomRightWorld = ScreenToWorld(new Vector2(_viewportWidth, _viewportHeight), 0);
                return new RectangleF(
                    topLeftWorld.X,
                    topLeftWorld.Y,
                    bottomRightWorld.X - topLeftWorld.X,
                    bottomRightWorld.Y - topLeftWorld.Y
                );
            }
        }

        public event EventHandler UpdateOrderChanged;
        public event EventHandler DrawDepthChanged;
        public event EventHandler VisibleChanged;
        public event EventHandler<EventArgs> DrawOrderChanged;

        // === ПУБЛИЧНЫЕ СВОЙСТВА ICamera ===
        public Vector3 Position => _position;
        public Vector3 Target => _target;
        public Matrix ViewMatrix => _viewMatrix;
        public Matrix ProjectionMatrix => _projectionMatrix;
        public Matrix ViewProjectionMatrix => _viewProjectionMatrix;
        public int ViewportWidth => _viewportWidth;
        public int ViewportHeight => _viewportHeight;

        // === ЗАЩИЩЕННЫЕ СЕТТЕРЫ ===
        protected void SetPosition(Vector3 position, bool updateView = true)
        {
            if (_position == position) return;

            _position = position;

            if (updateView)
            {
                UpdateViewMatrix();
                UpdateViewProjection();
            }

            OnPositionChanged();
        }

        protected void SetTarget(Vector3 target, bool updateView = true)
        {
            if (_target == target) return;

            _target = target;

            if (updateView)
            {
                UpdateViewMatrix();
                UpdateViewProjection();
            }

            OnTargetChanged();
        }

        protected void SetProjectionMatrix(Matrix matrix)
        {
            if (_projectionMatrix == matrix) return;

            _projectionMatrix = matrix;
            UpdateViewProjection();
            OnProjectionChanged();
        }

        protected void SetViewMatrix(Matrix matrix)
        {
            if (_viewMatrix == matrix) return;

            _viewMatrix = matrix;
            UpdateViewProjection();
            OnViewChanged();
        }

        private void UpdateViewProjection()
        {
            _viewProjectionMatrix = _viewMatrix * _projectionMatrix;
            OnViewProjectionChanged();
        }

        // === СВОЙСТВА ДЛЯ НАСЛЕДНИКОВ ===
        protected float MoveSpeed { get; set; } = 5.0f;
        protected float ZoomSpeed { get; set; } = 0.1f;
        protected float RotationSpeed { get; set; } = 0.005f;
        protected bool UseSmoothing { get; set; } = true;
        protected float Smoothness { get; set; } = 0.85f;

        public int DrawOrder => throw new NotImplementedException();

        // === КОНСТРУКТОР ===
        protected CameraBase(int viewportWidth, int viewportHeight)
        {
            if (viewportWidth <= 0 || viewportHeight <= 0)
                throw new ArgumentException("Viewport dimensions must be positive");

            _viewportWidth = viewportWidth;
            _viewportHeight = viewportHeight;

            InitializeMatrices();
        }

        event EventHandler<EventArgs> Microsoft.Xna.Framework.IDrawable.VisibleChanged
        {
            add
            {
                throw new NotImplementedException();
            }

            remove
            {
                throw new NotImplementedException();
            }
        }

        // === АБСТРАКТНЫЕ МЕТОДЫ (требуют реализации в наследниках) ===
        public abstract void Update(GameTime gameTime);
        public abstract Vector2 WorldToScreen(Vector3 worldPosition);
        public abstract Vector3 ScreenToWorld(Vector2 screenPosition, float worldZ = 0);

        // === ВИРТУАЛЬНЫЕ МЕТОДЫ ===
        protected virtual void InitializeMatrices()
        {
            // Базовая ортографическая проекция для 2D/2.5D
            SetProjectionMatrix(Matrix.CreateOrthographicOffCenter(
                0, _viewportWidth,
                _viewportHeight, 0,
                -1000, 1000));

            SetViewMatrix(Matrix.Identity);
            SetPosition(new Vector3(_viewportWidth / 2f, _viewportHeight / 2f, 100)); // Сверху
            SetTarget(new Vector3(_viewportWidth / 2f, _viewportHeight / 2f, 0)); // Смотрим вниз
        }

        protected virtual void UpdateViewMatrix()
        {
            // Базовая реализация - камера смотрит на цель
            var viewMatrix = Matrix.CreateLookAt(
                _position,
                _target,
                Vector3.Up);

            SetViewMatrix(viewMatrix);
        }

        // === ПУБЛИЧНЫЕ МЕТОДЫ ICamera ===
        public virtual void SetViewport(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Viewport dimensions must be positive");

            _viewportWidth = width;
            _viewportHeight = height;

            // Пересчитываем проекционную матрицу
            SetProjectionMatrix(Matrix.CreateOrthographicOffCenter(
                0, width, height, 0, -1000, 1000));
        }

        public virtual void LookAt(Vector3 target)
        {
            SetTarget(target);
        }

        public virtual void Move(Vector3 offset)
        {
            if (offset == Vector3.Zero) return;

            SetPosition(_position + offset);
            SetTarget(_target + offset);
        }

        public virtual void Teleport(Vector3 position)
        {
            Vector3 offset = position - _position;
            SetPosition(position);
            SetTarget(_target + offset);
        }

        // === РЕАЛИЗАЦИЯ IUpdatable и IDrawable МЕТОДОВ ===
        public void SetUpdateOrder(int order)
        {
            UpdateOrder = order;
        }

        public void SetDrawDepth(float depth)
        {
            DrawDepth = depth;
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;
        }

        // IDrawable.Draw(GameTime) - камера не рисуется, но метод должен быть
        public virtual void Draw(GameTime gameTime)
        {
            // Камера не рисует себя по умолчанию
            // Можно переопределить для отладочной визуализации
        }

        // IDrawable.Draw(GameTime, SpriteBatch) - для совместимости
        public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            Draw(gameTime); // Вызываем базовую версию
        }

        // === СОБЫТИЯ ДЛЯ НАСЛЕДНИКОВ ===
        protected virtual void OnPositionChanged() { }
        protected virtual void OnTargetChanged() { }
        protected virtual void OnViewChanged() { }
        protected virtual void OnProjectionChanged() { }
        protected virtual void OnViewProjectionChanged() { }

        // === УТИЛИТЫ ===
        public Vector2 GetScreenPosition(Vector3 worldPosition)
        {
            // Преобразуем мировые координаты в проекционные
            Vector3.Transform(ref worldPosition, ref _viewProjectionMatrix, out Vector3 result);

            // Преобразуем в экранные координаты
            return new Vector2(
                (result.X + 1) * 0.5f * _viewportWidth,
                (1 - result.Y) * 0.5f * _viewportHeight
            );
        }

        public bool IsInView(Vector3 worldPosition)
        {
            Vector2 screenPos = GetScreenPosition(worldPosition);
            return screenPos.X >= 0 && screenPos.X <= _viewportWidth &&
                   screenPos.Y >= 0 && screenPos.Y <= _viewportHeight;
        }

        // === ДЛЯ ОТЛАДКИ ===
        public override string ToString()
        {
            return $"Camera [Pos: {_position}, Target: {_target}, Viewport: {_viewportWidth}x{_viewportHeight}]";
        }
    }
}