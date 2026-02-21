using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Drawing;
using TalesFromTheUnderbrush.src.Graphics;

namespace TalesFromTheUnderbrush.src.UI.Camera
{
    /// <summary>
    /// Тестовая камера для отладки изометрического мира.
    /// Свободное перемещение (WASD), зум (колесо мыши), перетаскивание (ПКМ).
    /// Наследуется от CameraBase для единой архитектуры.
    /// </summary>
    public class TestCamera : CameraBase
    {
        // === НАСТРОЙКИ УПРАВЛЕНИЯ ===
        public float MoveSpeed { get; set; } = 400f;      // Пикселей в секунду
        public float ZoomSpeed { get; set; } = 0.1f;       // Шаг зума за клик колеса
        public float MinZoom { get; set; } = 0.5f;         // Минимальный зум
        public float MaxZoom { get; set; } = 2.0f;         // Максимальный зум
        public float DragSensitivity { get; set; } = 1.0f; // Чувствительность перетаскивания

        // === СОСТОЯНИЕ МЫШИ (для отслеживания изменений) ===
        private MouseState _prevMouseState;
        private bool _isDragging = false;
        private Vector2 _dragStartPos;
        private Vector3 _dragStartCameraPos;

        // === КОНСТРУКТОР ===
        public TestCamera(int viewportWidth, int viewportHeight)
            : base(viewportWidth, viewportHeight)
        {
            // Инициализация настроек из GameSetting
            MoveSpeed = GameSetting.CameraMoveSpeed * 100f;
            ZoomSpeed = GameSetting.CameraZoomSpeed;
            MinZoom = GameSetting.CameraMinZoom;
            MaxZoom = GameSetting.CameraMaxZoom;

            // Начальная позиция: центр вьюпорта
            SetPosition(new Vector3(viewportWidth / 2f, viewportHeight / 2f, 500f));
            SetTarget(new Vector3(viewportWidth / 2f, viewportHeight / 2f, 0f));

            Console.WriteLine($"[TestCamera] Создана камера {viewportWidth}x{viewportHeight}");
        }

        // === ОБНОВЛЕНИЕ (вызывается из GameManager.Update) ===
        public override void Update(GameTime gameTime)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyboard = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();

            // 1. WASD-перемещение
            HandleKeyboardMovement(keyboard, delta);

            // 2. Зум колесом мыши
            HandleMouseZoom(mouse);

            // 3. Перетаскивание ПКМ
            HandleMouseDrag(mouse);

            // Сохраняем состояние для следующего кадра
            _prevMouseState = mouse;

            // Обновляем матрицы через базовый класс
            UpdateViewMatrix();
        }

        // === ОБРАБОТКА КЛАВИАТУРЫ (WASD) ===
        private void HandleKeyboardMovement(KeyboardState keyboard, float delta)
        {
            Vector2 moveDir = Vector2.Zero;

            if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
                moveDir.Y -= 1;
            if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
                moveDir.Y += 1;
            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
                moveDir.X -= 1;
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
                moveDir.X += 1;

            if (moveDir != Vector2.Zero)
            {
                moveDir.Normalize();
                Vector3 moveOffset = new Vector3(moveDir.X * MoveSpeed * delta, moveDir.Y * MoveSpeed * delta, 0);
                Move(moveOffset);
            }
        }

        // === ОБРАБОТКА ЗУМА (КОЛЕСО МЫШИ) ===
        private void HandleMouseZoom(MouseState mouse)
        {
            int scrollDelta = mouse.ScrollWheelValue - _prevMouseState.ScrollWheelValue;

            if (scrollDelta != 0)
            {
                float zoomDelta = scrollDelta > 0 ? ZoomSpeed : -ZoomSpeed;

                // Получаем текущий зум из матрицы проекции
                float currentZoom = 1.0f; // По умолчанию
                // В CameraBase нет прямого доступа к ZoomLevel, поэтому используем Position.Z как индикатор
                // Или можно добавить свойство в CameraBase

                // Для простоты: меняем Position.Z (высота камеры)
                float newZ = Position.Z - scrollDelta * 0.5f;
                newZ = MathHelper.Clamp(newZ, 100f, 1000f); // Ограничиваем высоту

                SetPosition(new Vector3(Position.X, Position.Y, newZ));
            }
        }

        // === ОБРАБОТКА ПЕРЕТАСКИВАНИЯ (ПКМ) ===
        private void HandleMouseDrag(MouseState mouse)
        {
            // Начало перетаскивания
            if (mouse.RightButton == ButtonState.Pressed && !_isDragging)
            {
                _isDragging = true;
                _dragStartPos = new Vector2(mouse.X, mouse.Y);
                _dragStartCameraPos = Position;
            }
            // Перетаскивание
            else if (mouse.RightButton == ButtonState.Pressed && _isDragging)
            {
                float dx = mouse.X - _dragStartPos.X;
                float dy = mouse.Y - _dragStartPos.Y;

                Vector3 newPos = new Vector3(
                    _dragStartCameraPos.X - dx * DragSensitivity,
                    _dragStartCameraPos.Y - dy * DragSensitivity,
                    _dragStartCameraPos.Z
                );

                SetPosition(newPos);
                SetTarget(new Vector3(newPos.X, newPos.Y, 0));
            }
            // Конец перетаскивания
            else if (mouse.RightButton == ButtonState.Released && _isDragging)
            {
                _isDragging = false;
            }
        }

        // === ИЗОМЕТРИЧЕСКАЯ ПРОЕКЦИЯ: Мир → Экран ===
        public override Vector2 WorldToScreen(Vector3 worldPosition)
        {
            // 1. Изометрическая формула для 128×64 тайлов
            float screenX = (worldPosition.X - worldPosition.Y) * GameSetting.WorldTileHalfWidth;
            float screenY = (worldPosition.X + worldPosition.Y) * GameSetting.WorldTileHalfHeight;

            // 2. Учёт высоты Z (чем выше Z, тем выше на экране)
            screenY -= worldPosition.Z * GameSetting.IsometricLayerHeight;

            // 3. Применяем позицию камеры (смещение вьюпорта)
            screenX += ViewportWidth / 2f - Position.X;
            screenY += ViewportHeight / 4f - Position.Y;

            return new Vector2(screenX, screenY);
        }

        // === ОБРАТНАЯ ПРОЕКЦИЯ: Экран → Мир (для кликов мыши) ===
        public override Vector3 ScreenToWorld(Vector2 screenPosition, float worldZ = 0)
        {
            // 1. Учитываем смещение камеры
            float adjustedX = screenPosition.X - ViewportWidth / 2f + Position.X;
            float adjustedY = screenPosition.Y - ViewportHeight / 4f + Position.Y;

            // 2. Обратная изометрическая формула
            float worldX = (adjustedX / GameSetting.WorldTileHalfWidth + adjustedY / GameSetting.WorldTileHalfHeight) / 2;
            float worldY = (adjustedY / GameSetting.WorldTileHalfHeight - adjustedX / GameSetting.WorldTileHalfWidth) / 2;

            return new Vector3(worldX, worldY, worldZ);
        }

        // === ПОЛУЧЕНИЕ МАТРИЦЫ ДЛЯ SPRITEBATCH.BEGIN() ===
        /// <summary>
        /// Возвращает матрицу трансформации для SpriteBatch.Begin().
        /// Для изометрии используем простую матрицу сдвига и масштаба.
        /// </summary>
        public Matrix GetViewMatrix()
        {
            // Простая матрица для 2D-изометрии
            return Matrix.CreateTranslation(-Position.X, -Position.Y, 0);
        }

        // === ПЕРЕОПРЕДЕЛЕНИЕ БАЗОВЫХ МЕТОДОВ ===
        protected override void InitializeMatrices()
        {
            // Для изометрии не используем стандартную ортографическую проекцию
            // Вместо этого работаем с ручной проекцией в WorldToScreen/ScreenToWorld
            SetProjectionMatrix(Matrix.Identity);
            SetViewMatrix(Matrix.Identity);
        }

        protected override void UpdateViewMatrix()
        {
            // Для изометрии обновляем только позицию, матрицы не критичны
            // Основная проекция происходит в WorldToScreen()
            base.UpdateViewMatrix();
        }

        // === ОТЛАДОЧНАЯ ИНФОРМАЦИЯ ===
        public override string ToString()
        {
            return $"TestCamera [Pos: ({Position.X:F1}, {Position.Y:F1}, {Position.Z:F1}) | Viewport: {ViewportWidth}x{ViewportHeight}]";
        }
    }
}
