using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using RectangleF = TalesFromTheUnderbrush.src.GameRectangleF;

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

        // === ZOOM (КРИТИЧНО!) ===
        protected float _zoom = 1.0f;
        protected float _minZoom = 0.5f;
        protected float _maxZoom = 2.0f;

        // === IUpdattGameEntity / IRenderable ===
        private int _updateOrder = 0;
        private float _drawOrder = 0.5f;
        private bool _visible = true;

        // === СОБЫТИЯ ===
        public event EventHandler UpdateOrderChanged;
        public event EventHandler DrawOrderChanged;
        public event EventHandler VisibleChanged;

        // === ПУБЛИЧНЫЕ СВОЙСТВА ===
        public Vector3 Position => _position;
        public Vector3 Target => _target;
        public Matrix ViewMatrix => _viewMatrix;
        public Matrix ProjectionMatrix => _projectionMatrix;
        public Matrix ViewProjectionMatrix => _viewProjectionMatrix;
        public int ViewportWidth => _viewportWidth;
        public int ViewportHeight => _viewportHeight;

        // === ZOOM СВОЙСТВА (ПУБЛИЧНЫЙ ДОСТУП!) ===
        public float Zoom => _zoom;
        public float MinZoom => _minZoom;
        public float MaxZoom => _maxZoom;

        // === IUpdattGameEntity Свойства ===
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

        // === IRenderable Свойства ===
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

        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    VisibleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // === ГРАНИЦЫ КАМЕРЫ ===
        public RectangleF Bounds
        {
            get
            {
                Vector3 topLeftWorld = ScreenToWorld(Vector2.Zero, 0);
                Vector3 bottomRightWorld = ScreenToWorld(new Vector2(_viewportWidth, _viewportHeight), 0);
                return new RectangleF(
                    (int)topLeftWorld.X,
                    (int)topLeftWorld.Y,
                    (int)(bottomRightWorld.X - topLeftWorld.X),
                    (int)(bottomRightWorld.Y - topLeftWorld.Y)
                );
            }
        }

        // === НАСТРОЙКИ ДЛЯ НАСЛЕДНИКОВ ===
        protected float MoveSpeed { get; set; } = 5.0f;
        protected float ZoomSpeed { get; set; } = 0.1f;

        // === КОНСТРУКТОР ===
        protected CameraBase(int viewportWidth, int viewportHeight)
        {
            if (viewportWidth <= 0 || viewportHeight <= 0)
                throw new ArgumentException("Viewport dimensions must be positive");

            _viewportWidth = viewportWidth;
            _viewportHeight = viewportHeight;
            _zoom = 1.0f;
            _minZoom = GameSetting.CameraMinZoom;
            _maxZoom = GameSetting.CameraMaxZoom;

            InitializeMatrices();
        }

        // === ИНИЦИАЛИЗАЦИЯ МАТРИЦ ===
        protected virtual void InitializeMatrices()
        {

            //_projectionMatrix = Matrix.Identity;
            //_viewMatrix = Matrix.Identity;
            //_viewProjectionMatrix = Matrix.Identity;

            //SetPosition(new Vector3(_viewportWidth / 2f, _viewportHeight / 2f, 500f));
            //SetTarget(new Vector3(_viewportWidth / 2f, _viewportHeight / 2f, 0f));
            SetProjectionMatrix(Matrix.Identity);  // ✅ Переопределяет!
            SetViewMatrix(Matrix.Identity);
        }

        // === ОБНОВЛЕНИЕ МАТРИЦ ===
        protected virtual void UpdateViewMatrix()
        {
            _viewMatrix = Matrix.CreateTranslation(-_position.X, -_position.Y, 0) *
                         Matrix.CreateScale(_zoom, _zoom, 1);
            _viewProjectionMatrix = _viewMatrix * _projectionMatrix;
        }

        // === ЗАЩИЩЁННЫЕ СЕТТЕРЫ ===
        protected void SetPosition(Vector3 position, bool updateView = true)
        {
            if (_position == position) return;
            _position = position;
            if (updateView) UpdateViewMatrix();
        }

        protected void SetTarget(Vector3 target, bool updateView = true)
        {
            if (_target == target) return;
            _target = target;
            if (updateView) UpdateViewMatrix();
        }

        // === ZOOM МЕТОДЫ (КРИТИЧНО!) ===
        protected void SetZoom(float zoom, bool updateView = true)
        {
            float newZoom = MathHelper.Clamp(zoom, _minZoom, _maxZoom);
            if (Math.Abs(_zoom - newZoom) < float.Epsilon) return;

            _zoom = newZoom;
            if (updateView) UpdateViewMatrix();
        }

        public void ZoomIn(float delta)
        {
            SetZoom(_zoom + delta);
        }

        public void ZoomOut(float delta)
        {
            SetZoom(_zoom - delta);
        }

        protected void SetProjectionMatrix(Matrix matrix)
        {
            if (_projectionMatrix == matrix) return;
            _projectionMatrix = matrix;
            _viewProjectionMatrix = _viewMatrix * _projectionMatrix;
        }

        protected void SetViewMatrix(Matrix matrix)
        {
            if (_viewMatrix == matrix) return;
            _viewMatrix = matrix;
            _viewProjectionMatrix = _viewMatrix * _projectionMatrix;
        }

        // === АБСТРАКТНЫЕ МЕТОДЫ ===
        public abstract void Update(GameTime gameTime);
        public abstract Vector2 WorldToScreen(Vector3 worldPosition);
        public abstract Vector3 ScreenToWorld(Vector2 screenPosition, float worldZ = 0);

        // === ПУБЛИЧНЫЕ МЕТОДЫ ICamera ===
        public virtual void Move(Vector3 offset)
        {
            if (offset == Vector3.Zero) return;
            SetPosition(_position + offset);
            SetTarget(_target + offset);
        }

        public virtual void LookAt(Vector3 target) => SetTarget(target);

        public virtual void SetViewport(int width, int height)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Viewport dimensions must be positive");

            _viewportWidth = width;
            _viewportHeight = height;
            UpdateViewMatrix();
        }

        public virtual void Teleport(Vector3 position)
        {
            Vector3 offset = position - _position;
            SetPosition(position);
            SetTarget(_target + offset);
        }

        // === УТИЛИТЫ ===
        public bool IsInView(Vector3 worldPosition)
        {
            Vector2 screenPos = WorldToScreen(worldPosition);
            return screenPos.X >= 0 && screenPos.X <= _viewportWidth &&
                   screenPos.Y >= 0 && screenPos.Y <= _viewportHeight;
        }

        public virtual Matrix GetViewMatrix()
        {
            return Matrix.CreateTranslation(-_position.X, -_position.Y, 0) *
                   Matrix.CreateScale(_zoom, _zoom, 1);
        }

        // === IUpdattGameEntity / IRenderable Методы ===
        public void SetUpdateOrder(int order) => UpdateOrder = order;
        public void SetVisible(bool visible) => Visible = visible;

        public virtual void Draw(GameTime gameTime) { }
        public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch) => Draw(gameTime);

        // === ОТЛАДОЧНАЯ ИНФОРМАЦИЯ ===
        public override string ToString()
        {
            return $"Camera [Pos: ({_position.X:F1}, {_position.Y:F1}, {_position.Z:F1}) | " +
                   $"Zoom: {_zoom:F2} | Viewport: {_viewportWidth}x{_viewportHeight}]";
        }

        // === IDisposable ===
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                UpdateOrderChanged = null;
                DrawOrderChanged = null;
                VisibleChanged = null;
            }
        }
    }
}