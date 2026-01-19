using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace TalesFromTheUnderbrush.src.UI.Camera
{
    public class OrthographicCamera2_5D : CameraBase
    {
        // === ПРИВАТНЫЕ ПОЛЯ ===
        private float _zoom = 1.0f;
        private float _minZoom = 0.5f;
        private float _maxZoom = 3.0f;

        // Изометрические параметры
        private readonly Vector2 _isoAngle = new Vector2(0.5f, 0.25f);
        private readonly Vector2 _tileBaseSize = new Vector2(64, 32);

        // Для плавного движения
        private Vector3 _velocity = Vector3.Zero;

        // === ПУБЛИЧНЫЕ СВОЙСТВА (только чтение) ===
        public float Zoom => _zoom;
        public int ViewportTilesX { get; private set; }
        public int ViewportTilesY { get; private set; }

        // === КОНСТРУКТОР ===
        public OrthographicCamera2_5D(int viewportWidth, int viewportHeight)
            : base(viewportWidth, viewportHeight)
        {
            // Используем защищенные сеттеры родителя
            SetPosition(new Vector3(0, 0, 500));
            SetTarget(Vector3.Zero);

            // Специфичная инициализация
            //Initialize2_5DMatrices();
            CalculateViewportBounds();

            // Настройка скорости для этой камеры
            MoveSpeed = 10.0f; // Используем protected setter
            ZoomSpeed = 0.15f;
        }

        // === ЗАЩИЩЕННЫЕ МЕТОДЫ ДЛЯ ВНУТРЕННЕГО ИСПОЛЬЗОВАНИЯ ===

        /// <summary>Внутренний метод для установки зума</summary>
        private void SetZoomInternal(float zoomLevel)
        {
            float newZoom = MathHelper.Clamp(zoomLevel, _minZoom, _maxZoom);

            if (Math.Abs(newZoom - _zoom) < 0.001f) return;

            _zoom = newZoom;

            // Пересчитываем матрицы
            Update2_5DViewMatrix();
            CalculateViewportBounds();

            OnZoomChanged();
        }

        // === ПЕРЕОПРЕДЕЛЕНИЕ БАЗОВЫХ МЕТОДОВ ===

        protected override void InitializeMatrices()
        {
            // 2.5D специфичная проекция
            SetProjectionMatrix(Matrix.CreateOrthographicOffCenter(
                0, ViewportWidth,
                ViewportHeight, 0,
                -1000, 1000));

            Update2_5DViewMatrix();
        }

        protected override void UpdateViewMatrix()
        {
            // Используем 2.5D реализацию
            Update2_5DViewMatrix();
        }

        private void Update2_5DViewMatrix()
        {
            // Изометрическое преобразование позиции камеры
            Vector3 pos = base.Position; // Используем public getter

            float isoX = (pos.X - pos.Y) * _tileBaseSize.X * _zoom * _isoAngle.X;
            float isoY = (pos.X + pos.Y) * 0.5f * _tileBaseSize.Y * _zoom * _isoAngle.Y
                       - pos.Z * _tileBaseSize.Y * _zoom;

            // Создаем матрицу вида
            Matrix viewMatrix = Matrix.CreateLookAt(
                new Vector3(isoX, isoY, pos.Z),
                new Vector3(isoX, isoY, 0),
                Vector3.Up);

            // Изометрический поворот
            float angle = MathHelper.ToRadians(30f);
            viewMatrix *= Matrix.CreateRotationX(angle) *
                         Matrix.CreateRotationY(MathHelper.ToRadians(45f));

            // Используем защищенный сеттер из родителя
            SetViewMatrix(viewMatrix);
        }

        // === ПУБЛИЧНЫЕ МЕТОДЫ ДЛЯ ВНЕШНЕГО ИСПОЛЬЗОВАНИЯ ===

        /// <summary>Установить зум (публичный интерфейс)</summary>
        public void SetZoom(float zoomLevel)=>SetZoomInternal(zoomLevel);

        /// <summary>Увеличить зум</summary>
        public void ZoomIn(float amount = 0.1f)=>SetZoomInternal(_zoom + amount);

        /// <summary>Уменьшить зум</summary>
        public void ZoomOut(float amount = 0.1f)=>SetZoomInternal(_zoom - amount);
        
        /// <summary>Получить видимую область в тайлах</summary>
        public Rectangle GetVisibleTileBounds()
        {
            Vector3 pos = base.Position;

            return new Rectangle(
                (int)(pos.X - ViewportTilesX / 2),
                (int)(pos.Y - ViewportTilesY / 2),
                ViewportTilesX,
                ViewportTilesY
            );
        }

        // === IUpdatable реализация ===
        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Обработка ввода
            HandleInput(deltaTime);

            // Плавное движение
            if (UseSmoothing && _velocity.LengthSquared() > 0.001f)
            {
                // Используем публичный метод Move из родителя
                Move(_velocity * deltaTime);
                _velocity = Vector3.Lerp(_velocity, Vector3.Zero, deltaTime / Smoothness);
            }

            // Обновление матриц
            Update2_5DViewMatrix();
        }

        private void HandleInput(float deltaTime)
        {
            KeyboardState keyboard = Keyboard.GetState();

            Vector3 input = Vector3.Zero;

            if (keyboard.IsKeyDown(Keys.W)) input.Y -= 1;
            if (keyboard.IsKeyDown(Keys.S)) input.Y += 1;
            if (keyboard.IsKeyDown(Keys.A)) input.X -= 1;
            if (keyboard.IsKeyDown(Keys.D)) input.X += 1;
            if (keyboard.IsKeyDown(Keys.Q)) input.Z += 1;
            if (keyboard.IsKeyDown(Keys.E)) input.Z -= 1;

            if (input != Vector3.Zero)
            {
                input.Normalize();
                _velocity = input * base.MoveSpeed * _zoom;
            }

            // Зум через колесико мыши
            MouseState mouse = Mouse.GetState();
            int scroll = mouse.ScrollWheelValue;
            if (mouse.ScrollWheelValue != 0)
            {
                float zoomDelta = scroll > 0 ? ZoomSpeed : -ZoomSpeed;
                SetZoomInternal(_zoom + zoomDelta);
            }
        }

        // === Преобразование координат ===
        public override Vector2 WorldToScreen(Vector3 worldPosition)
        {
            Vector3 position = Position;

            float screenX = (worldPosition.X - worldPosition.Y) *
                           (_tileBaseSize.X * _zoom * _isoAngle.X);
            float screenY = (worldPosition.X + worldPosition.Y) *
                           (_tileBaseSize.Y * _zoom * _isoAngle.Y * 0.5f)
                           - worldPosition.Z * (_tileBaseSize.Y * _zoom);

            float camX = (position.X - position.Y) * (_tileBaseSize.X * _zoom * _isoAngle.X);
            float camY = (position.X + position.Y) * (_tileBaseSize.Y * _zoom * _isoAngle.Y * 0.5f)
                       - position.Z * (_tileBaseSize.Y * _zoom);

            Vector2 center = new Vector2(ViewportWidth / 2, ViewportHeight / 2);

            return new Vector2(
                center.X + (screenX - camX),
                center.Y + (screenY - camY)
            );
        }

        public override Vector3 ScreenToWorld(Vector2 screenPosition, float worldZ = 0)
        {
            Vector2 center = new Vector2(ViewportWidth / 2, ViewportHeight / 2);
            Vector2 offset = screenPosition - center;

            float inverseScale = 1.0f / (_tileBaseSize.X * _zoom * _isoAngle.X);

            Vector3 pos = Position;

            float worldX = pos.X + (offset.X + offset.Y) * inverseScale;
            float worldY = pos.Y + (offset.Y - offset.X) * inverseScale;

            return new Vector3(worldX, worldY, worldZ);
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===
        private void CalculateViewportBounds()
        {
            float effectiveTileWidth = _tileBaseSize.X * _zoom;
            float effectiveTileHeight = _tileBaseSize.Y * _zoom;

            ViewportTilesX = (int)(ViewportWidth / (effectiveTileWidth * _isoAngle.X)) + 2;
            ViewportTilesY = (int)(ViewportHeight / (effectiveTileHeight * _isoAngle.Y)) + 2;
        }

        private void OnZoomChanged()
        {
            // Можно добавить логику при изменении зума
        }

        // === ToString для отладки ===
        public override string ToString()
        {
            return $"OrthographicCamera2_5D [Pos:{Position.X:F0},{Position.Y:F0},{Position.Z:F0} Zoom:{_zoom:F2}x]";
        }
    }
}
