using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TalesFromTheUnderbrush;

namespace TalesFromTheUnderbrush.src.Graphics
{
    /// <summary>
    /// 2.5D изометрическая камера с поддержкой вертикального движения (высоты)
    /// </summary>
    public class Camera
    {
        // === ОСНОВНЫЕ СВОЙСТВА КАМЕРЫ ===

        /// <summary>Позиция камеры в мировых координатах (X, Y, Z)</summary>
        public Vector3 Position { get; private set; }

        /// <summary>Целевая позиция для плавного движения</summary>
        private Vector3 _targetPosition;

        /// <summary>Скорость перемещения камеры</summary>
        public float MoveSpeed { get;private set; } = 5.0f;

        /// <summary>Скорость плавного интерполяции (damping)</summary>
        public float Smoothness { get;private set; } = 0.85f;

        /// <summary>Масштаб (зум) камеры</summary>
        public float Zoom { get; private set; }

        /// <summary>Минимальный зум</summary>
        public float MinZoom { get;private set; } = 0.5f;

        /// <summary>Максимальный зум</summary>
        public float MaxZoom { get;private set; } = 3.0f;

        /// <summary>Скорость изменения зума</summary>
        public float ZoomSpeed { get; set; } = 0.1f;

        /// <summary>Ширина области видимости в тайлах</summary>
        public int ViewportWidthInTiles { get; private set; }

        /// <summary>Высота области видимости в тайлах</summary>
        public int ViewportHeightInTiles { get; private set; }

        /// <summary>Размер тайла в пикселях (до изометрической проекции)</summary>
        public Vector2 TileBaseSize { get;private set; } = new Vector2(64, 32); // Ширина=64, Высота=32

        /// <summary>Границы камеры в мировых координатах</summary>
        public Rectangle Bounds { get; private set; }

        // === МАТРИЦЫ ПРЕОБРАЗОВАНИЯ ===

        /// <summary>Матрица вида (view matrix)</summary>
        public Matrix ViewMatrix { get; private set; }

        /// <summary>Матрица проекции (projection matrix)</summary>
        public Matrix ProjectionMatrix { get; private set; }

        /// <summary>Объединенная матрица вида и проекции</summary>
        public Matrix ViewProjectionMatrix { get; private set; }

        /// <summary>Углы изометрической проекции (2:1 стандартная изометрия)</summary>
        private readonly Vector2 _isometricAngle = new Vector2(0.5f, 0.25f);

        // === СВОЙСТВА ДЛЯ УПРАВЛЕНИЯ ===

        /// <summary>Клавиши управления камерой</summary>
        public Keys[] MoveKeys { get; set; } = new Keys[]
        {
            Keys.W, Keys.S, Keys.A, Keys.D,
            Keys.Q, Keys.E,  // Q/E для вертикального движения
            Keys.R, Keys.F   // R/F для изменения высоты над уровнем моря
        };

        /// <summary>Включено ли плавное движение</summary>
        public bool UseSmoothing { get;private set; } = true;

        /// <summary>Блокировка вращения камеры (всегда изометрическая)</summary>
        public bool LockRotation { get;private set; } = true;

        /// <summary>Фиксированный угол изометрии в радианах</summary>
        public float FixedAngle { get;private set; } = MathHelper.ToRadians(30f);

        // === СОСТОЯНИЕ ВВОДА ===
        private KeyboardState _currentKeyboardState;
        private KeyboardState _previousKeyboardState;
        private MouseState _currentMouseState;
        private MouseState _previousMouseState;
        private Vector2 _previousMousePosition;

        /// <summary>
        /// Инициализация камеры
        /// </summary>
        /// <param name="viewportWidth">Ширина области вывода</param>
        /// <param name="viewportHeight">Высота области вывода</param>
        /// <param name="initialPosition">Начальная позиция камеры</param>
        public Camera(int viewportWidth, int viewportHeight, Vector3 initialPosition)
        {
            // Начальная позиция
            Position = initialPosition;
            _targetPosition = initialPosition;

            // Начальный зум
            Zoom = 1.0f;

            // Рассчитываем видимую область в тайлах
            UpdateViewportBounds(viewportWidth, viewportHeight);

            // Инициализируем матрицы
            UpdateMatrices(viewportWidth, viewportHeight);

            // Устанавливаем границы (пока без ограничений)
            Bounds = new Rectangle(-10000, -10000, 20000, 20000);
        }

        /// <summary>
        /// Обновление видимых границ на основе размера окна
        /// </summary>
        public void UpdateViewportBounds(int viewportWidth, int viewportHeight)
        {
            // Конвертируем пиксели в тайлы с учетом изометрии и зума
            float tileWidth = TileBaseSize.X * Zoom;
            float tileHeight = TileBaseSize.Y * Zoom;

            // В изометрии видимая область рассчитывается особым образом
            ViewportWidthInTiles = (int)(viewportWidth / (tileWidth * _isometricAngle.X)) + 2;
            ViewportHeightInTiles = (int)(viewportHeight / (tileHeight * _isometricAngle.Y)) + 2;
        }

        /// <summary>
        /// Обновление матриц проекции и вида
        /// </summary>
        private void UpdateMatrices(int viewportWidth, int viewportHeight)
        {
            // Матрица проекции (ортографическая для 2.5D)
            ProjectionMatrix = Matrix.CreateOrthographicOffCenter(
                0, viewportWidth, viewportHeight, 0, -1000, 1000);

            // Матрица вида с изометрической проекцией
            UpdateViewMatrix();

            // Объединенная матрица
            ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
        }

        /// <summary>
        /// Обновление матрицы вида с изометрической проекцией
        /// </summary>
        private void UpdateViewMatrix()
        {
            // Изометрическое преобразование
            float isoX = Position.X - Position.Y;
            float isoY = (Position.X + Position.Y) * 0.5f - Position.Z;

            // Масштабирование с учетом зума
            isoX *= TileBaseSize.X * Zoom * _isometricAngle.X;
            isoY *= TileBaseSize.Y * Zoom * _isometricAngle.Y;

            // Создаем матрицу вида
            ViewMatrix = Matrix.CreateLookAt(
                new Vector3(isoX, isoY, 500),  // Позиция камеры в 3D пространстве
                new Vector3(isoX, isoY, 0),    // Точка, на которую смотрим
                Vector3.Up);                   // Вектор "вверх"

            // Добавляем изометрический поворот
            if (LockRotation)
            {
                ViewMatrix *= Matrix.CreateRotationX(FixedAngle) *
                             Matrix.CreateRotationY(MathHelper.ToRadians(45));
            }
        }

        /// <summary>
        /// Преобразование мировых координат в экранные
        /// </summary>
        public Vector2 WorldToScreen(Vector3 worldPosition)
        {
            // Изометрическое преобразование
            float screenX = (worldPosition.X - worldPosition.Y) *
                           (TileBaseSize.X * Zoom * _isometricAngle.X);
            float screenY = (worldPosition.X + worldPosition.Y) *
                           (TileBaseSize.Y * Zoom * _isometricAngle.Y * 0.5f) -
                           worldPosition.Z * (TileBaseSize.Y * Zoom);

            // Учитываем позицию камеры
            float cameraX = (Position.X - Position.Y) *
                           (TileBaseSize.X * Zoom * _isometricAngle.X);
            float cameraY = (Position.X + Position.Y) *
                           (TileBaseSize.Y * Zoom * _isometricAngle.Y * 0.5f) -
                           Position.Z * (TileBaseSize.Y * Zoom);

            // Центрируем на камере и добавляем смещение на половину экрана
            Vector2 viewportCenter = new Vector2(
                Game1.ViewportWidth / 2,
                Game1.ViewportHeight / 2);

            return new Vector2(
                viewportCenter.X + (screenX - cameraX),
                viewportCenter.Y + (screenY - cameraY)
            );
        }

        /// <summary>
        /// Преобразование экранных координат в мировые (2.5D)
        /// </summary>
        public Vector3 ScreenToWorld(Vector2 screenPosition, float worldZ = 0)
        {
            // Центр экрана
            Vector2 viewportCenter = new Vector2(
                Game1.ViewportWidth / 2,
                Game1.ViewportHeight / 2);

            // Смещение относительно центра
            Vector2 offset = screenPosition - viewportCenter;

            // Обратное изометрическое преобразование
            float inverseScale = 1.0f / (TileBaseSize.X * Zoom * _isometricAngle.X);

            // Рассчитываем мировые координаты
            float worldX = Position.X + (offset.X + offset.Y) * inverseScale;
            float worldY = Position.Y + (offset.Y - offset.X) * inverseScale;

            return new Vector3(worldX, worldY, worldZ);
        }

        /// <summary>
        /// Обновление состояния камеры
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // Сохраняем предыдущее состояние ввода
            _previousKeyboardState = _currentKeyboardState;
            _previousMouseState = _currentMouseState;
            _previousMousePosition = new Vector2(
                _currentMouseState.X, _currentMouseState.Y);

            // Получаем текущее состояние
            _currentKeyboardState = Keyboard.GetState();
            _currentMouseState = Mouse.GetState();

            // Обработка ввода
            HandleInput(gameTime);

            // Плавное движение к целевой позиции
            if (UseSmoothing)
            {
                Position = Vector3.Lerp(Position, _targetPosition, 1.0f - Smoothness);

                // Проверка на достижение цели (для оптимизации)
                if (Vector3.DistanceSquared(Position, _targetPosition) < 0.001f)
                {
                    Position = _targetPosition;
                }
            }
            else
            {
                Position = _targetPosition;
            }

            // Ограничение позиции камеры границами
            ClampToBounds();

            // Обновление матриц
            UpdateViewMatrix();
            ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
        }

        /// <summary>
        /// Обработка пользовательского ввода
        /// </summary>
        private void HandleInput(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Vector3 moveDirection = Vector3.Zero;

            // === ДВИЖЕНИЕ КАМЕРЫ ===

            // Горизонтальное движение (X, Y)
            if (_currentKeyboardState.IsKeyDown(MoveKeys[0])) moveDirection.Y -= 1; // W - север
            if (_currentKeyboardState.IsKeyDown(MoveKeys[1])) moveDirection.Y += 1; // S - юг
            if (_currentKeyboardState.IsKeyDown(MoveKeys[2])) moveDirection.X -= 1; // A - запад
            if (_currentKeyboardState.IsKeyDown(MoveKeys[3])) moveDirection.X += 1; // D - восток

            // Вертикальное движение (Z - высота над уровнем моря)
            if (_currentKeyboardState.IsKeyDown(MoveKeys[4])) moveDirection.Z += 1; // Q - вверх
            if (_currentKeyboardState.IsKeyDown(MoveKeys[5])) moveDirection.Z -= 1; // E - вниз

            // Изменение высоты "над уровнем моря" (отдельная ось для обзора)
            if (_currentKeyboardState.IsKeyDown(MoveKeys[6])) Position += Vector3.Up * 0.5f; // R
            if (_currentKeyboardState.IsKeyDown(MoveKeys[7])) Position += Vector3.Down * 0.5f; // F

            // Нормализуем вектор движения, если есть ввод
            if (moveDirection != Vector3.Zero)
            {
                moveDirection.Normalize();
                _targetPosition += moveDirection * MoveSpeed * deltaTime / Zoom;
            }

            // === УПРАВЛЕНИЕ ЗУМОМ ===

            // Колесико мыши
            int scrollDelta = _currentMouseState.ScrollWheelValue - _previousMouseState.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                float zoomFactor = scrollDelta > 0 ? 1 + ZoomSpeed : 1 - ZoomSpeed;
                SetZoom(Zoom * zoomFactor);
            }

            // Клавиши для зума
            if (_currentKeyboardState.IsKeyDown(Keys.OemPlus)) SetZoom(Zoom * (1 + ZoomSpeed * deltaTime));
            if (_currentKeyboardState.IsKeyDown(Keys.OemMinus)) SetZoom(Zoom * (1 - ZoomSpeed * deltaTime));

            // Сброс зума
            if (_currentKeyboardState.IsKeyDown(Keys.Home) &&
                !_previousKeyboardState.IsKeyDown(Keys.Home))
            {
                SetZoom(1.0f);
            }

            // === ПЕРЕТАСКИВАНИЕ КАМЕРЫ МЫШЬЮ ===

            if (_currentMouseState.MiddleButton == ButtonState.Pressed)
            {
                Vector2 currentMousePos = new Vector2(_currentMouseState.X, _currentMouseState.Y);
                Vector2 delta = (currentMousePos - _previousMousePosition) * 0.5f / Zoom;

                _targetPosition.X -= delta.X;
                _targetPosition.Y -= delta.Y;
            }
        }

        /// <summary>
        /// Установка нового значения зума с ограничениями
        /// </summary>
        public void SetZoom(float newZoom)
        {
            Zoom = MathHelper.Clamp(newZoom, MinZoom, MaxZoom);

            // При изменении зума нужно обновить границы видимости
            UpdateViewportBounds(Game1.ViewportWidth, Game1.ViewportHeight);
        }

        /// <summary>
        /// Установка позиции камеры напрямую
        /// </summary>
        public void SetPosition(Vector3 newPosition)
        {
            Position = newPosition;
            _targetPosition = newPosition;
            ClampToBounds();
        }

        /// <summary>
        /// Установка позиции камеры с плавным переходом
        /// </summary>
        public void SetTargetPosition(Vector3 newTargetPosition)
        {
            _targetPosition = newTargetPosition;
            ClampToBounds();
        }

        /// <summary>
        /// Ограничение камеры установленными границами
        /// </summary>
        private void ClampToBounds()
        {
            _targetPosition.X = MathHelper.Clamp(_targetPosition.X,
                Bounds.Left, Bounds.Right);
            _targetPosition.Y = MathHelper.Clamp(_targetPosition.Y,
                Bounds.Top, Bounds.Bottom);
            _targetPosition.Z = MathHelper.Clamp(_targetPosition.Z,
                -100, 100); // Ограничение по высоте
        }

        /// <summary>
        /// Центрирование камеры на объекте
        /// </summary>
        public void CenterOn(Vector3 worldPosition)
        {
            SetTargetPosition(worldPosition);
        }

        /// <summary>
        /// Проверка, виден ли тайл в области видимости камеры
        /// </summary>
        public bool IsTileVisible(Vector3 tilePosition)
        {
            // Простая проверка по границам видимости
            float leftBound = Position.X - ViewportWidthInTiles / 2;
            float rightBound = Position.X + ViewportWidthInTiles / 2;
            float topBound = Position.Y - ViewportHeightInTiles / 2;
            float bottomBound = Position.Y + ViewportHeightInTiles / 2;

            return tilePosition.X >= leftBound && tilePosition.X <= rightBound &&
                   tilePosition.Y >= topBound && tilePosition.Y <= bottomBound;
        }

        /// <summary>
        /// Получение прямоугольника видимой области в мировых координатах
        /// </summary>
        public Rectangle GetVisibleWorldBounds()
        {
            int left = (int)(Position.X - ViewportWidthInTiles / 2);
            int top = (int)(Position.Y - ViewportHeightInTiles / 2);

            return new Rectangle(
                left,
                top,
                ViewportWidthInTiles,
                ViewportHeightInTiles);
        }

        /// <summary>
        /// Сброс камеры к начальному состоянию
        /// </summary>
        public void Reset()
        {
            Position = Vector3.Zero;
            _targetPosition = Vector3.Zero;
            Zoom = 1.0f;
        }

        /// <summary>
        /// Получение строкового представления состояния камеры
        /// </summary>
        public override string ToString()
        {
            return $"Camera: Pos=({Position.X:F1},{Position.Y:F1},{Position.Z:F1}) Zoom={Zoom:F2} " +
                   $"Viewport={ViewportWidthInTiles}x{ViewportHeightInTiles} tiles";
        }
    }
}
