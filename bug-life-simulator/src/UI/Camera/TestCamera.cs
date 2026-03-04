using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace TalesFromTheUnderbrush.src.UI.Camera
{
    public class TestCamera : CameraBase
    {
        // === НАСТРОЙКИ УПРАВЛЕНИЯ ===
        public new float MoveSpeed { get; set; } = 400f;
        public new float ZoomSpeed { get; set; } = 0.1f;
        public float DragSensitivity { get; set; } = 1.0f;

        // === СОСТОЯНИЕ МЫШИ ===
        private MouseState _prevMouseState;
        private bool _isDragging = false;
        private Vector2 _dragStartPos;
        private Vector3 _dragStartCameraPos;

        // === КОНСТРУКТОР ===
        public TestCamera(int viewportWidth, int viewportHeight)
            : base(viewportWidth, viewportHeight)
        {
            MoveSpeed = GameSetting.CameraMoveSpeed * 100f;
            ZoomSpeed = GameSetting.CameraZoomSpeed;

            // Используем защищённые поля базового класса
            _minZoom = GameSetting.CameraMinZoom;
            _maxZoom = GameSetting.CameraMaxZoom;

            // === ИСПРАВЛЕНО: Начальная позиция = 0, 0, 500 ===
            SetPosition(new Vector3(0, 0, 500f));  // ← ИЗМЕНЕНО (было viewportWidth/2)
            SetTarget(new Vector3(0, 0, 0f));      // ← ИЗМЕНЕНО

            Console.WriteLine($"[TestCamera] Создана камера {viewportWidth}x{viewportHeight}");
        }

        // === ОБНОВЛЕНИЕ ===
        public override void Update(GameTime gameTime)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            KeyboardState keyboard = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();

            HandleKeyboardMovement(keyboard, delta);
            HandleMouseZoom(mouse);
            HandleMouseDrag(mouse);

            _prevMouseState = mouse;
            UpdateViewMatrix();
        }

        // === WASD ПЕРЕМЕЩЕНИЕ ===
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
                Vector3 moveOffset = new Vector3(
                    moveDir.X * MoveSpeed * delta,
                    moveDir.Y * MoveSpeed * delta,
                    0
                );
                Move(moveOffset);
            }
        }

        // === ЗУМ КОЛЕСОМ ===
        private void HandleMouseZoom(MouseState mouse)
        {
            int scrollDelta = mouse.ScrollWheelValue - _prevMouseState.ScrollWheelValue;

            if (scrollDelta != 0)
            {
                float zoomDelta = scrollDelta > 0 ? ZoomSpeed : -ZoomSpeed;
                ZoomIn(zoomDelta);
            }
        }

        // === ПЕРЕТАСКИВАНИЕ ПКМ ===
        private void HandleMouseDrag(MouseState mouse)
        {
            if (mouse.RightButton == ButtonState.Pressed && !_isDragging)
            {
                _isDragging = true;
                _dragStartPos = new Vector2(mouse.X, mouse.Y);
                _dragStartCameraPos = Position;
            }
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
            else if (mouse.RightButton == ButtonState.Released && _isDragging)
            {
                _isDragging = false;
            }
        }

        // === ИЗОМЕТРИЧЕСКАЯ ПРОЕКЦИЯ ===
        public override Vector2 WorldToScreen(Vector3 worldPosition)
        {
            // 1. === ИСПРАВЛЕНО: Сначала применяем камеру в МИРОВОМ пространстве ===
            float worldX = worldPosition.X - Position.X;
            float worldY = worldPosition.Y - Position.Y;
            float worldZ = worldPosition.Z;

            // 2. Изометрическая формула для 128×64 тайлов
            float screenX = (worldX - worldY) * GameSetting.WorldTileHalfWidth;
            float screenY = (worldX + worldY) * GameSetting.WorldTileHalfHeight;
            screenY -= worldZ * GameSetting.IsometricLayerHeight;

            // 3. Зум и центрирование вьюпорта
            screenX = screenX * Zoom + ViewportWidth / 2f;
            screenY = screenY * Zoom + ViewportHeight / 4f;

            return new Vector2(screenX, screenY);
        }

        // === ОБРАТНАЯ ПРОЕКЦИЯ ===
        // === ОБРАТНАЯ ПРОЕКЦИЯ ===
        public override Vector3 ScreenToWorld(Vector2 screenPosition, float worldZ = 0)
        {
            // 1. Обратный порядок: сначала убираем зум и вьюпорт
            float adjustedX = (screenPosition.X - ViewportWidth / 2f) / Zoom;
            float adjustedY = (screenPosition.Y - ViewportHeight / 4f) / Zoom;

            // 2. Обратная изометрическая формула
            float worldX = (adjustedX / GameSetting.WorldTileHalfWidth +
                           adjustedY / GameSetting.WorldTileHalfHeight) / 2;
            float worldY = (adjustedY / GameSetting.WorldTileHalfHeight -
                           adjustedX / GameSetting.WorldTileHalfWidth) / 2;

            // 3. Добавляем позицию камеры
            worldX += Position.X;
            worldY += Position.Y;

            return new Vector3(worldX, worldY, worldZ);
        }

        // === МАТРИЦА ДЛЯ SPRITEBATCH ===
        public override Matrix GetViewMatrix()
        {
            return Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
                   Matrix.CreateScale(Zoom, Zoom, 1);
        }

        // === ПЕРЕОПРЕДЕЛЕНИЕ БАЗОВЫХ МЕТОДОВ ===
        protected override void InitializeMatrices()
        {
            SetProjectionMatrix(Matrix.Identity);
            SetViewMatrix(Matrix.Identity);
        }

        protected override void UpdateViewMatrix()
        {
            base.UpdateViewMatrix();
        }

        // === ОТЛАДОЧНАЯ ИНФОРМАЦИЯ ===
        public override string ToString()
        {
            return $"TestCamera [Pos: ({Position.X:F1}, {Position.Y:F1}, {Position.Z:F1}) | " +
                   $"Zoom: {Zoom:F2} | Viewport: {ViewportWidth}x{ViewportHeight}]";
        }
    }
}