using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SharpDX.MediaFoundation;

namespace TalesFromTheUnderbrush.src.UI.Camera
{
    public class Camera2_5D : CameraBase
    {
        private float _zoom = 1.0f;
        private float _minZoom = 0.5f;
        private float _maxZoom = 3.0f;

        private Vector2 _offset;

        public float Zoom
        {
            get => _zoom;
            set => _zoom = MathHelper.Clamp(value, _minZoom, _maxZoom);
        }

        public Camera2_5D(int viewportWidth, int viewportHeight)
            : base(viewportWidth, viewportHeight)
        {
            _offset = new Vector2(0, 0);
            // Камера для 2.5D игр
        }

        public override void Update(GameTime gameTime)
        {
            // Обработка ввода для перемещения камеры
            KeyboardState keyboard = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();

            // Пример: перемещение стрелками
            Vector3 move = Vector3.Zero;
            float moveSpeed = MoveSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (keyboard.IsKeyDown(Keys.Left)) move.X -= moveSpeed;
            if (keyboard.IsKeyDown(Keys.Right)) move.X += moveSpeed;
            if (keyboard.IsKeyDown(Keys.Up)) move.Y -= moveSpeed;
            if (keyboard.IsKeyDown(Keys.Down)) move.Y += moveSpeed;

            if (move != Vector3.Zero) Move(move);
            
            // Зум колесиком мыши
            if (mouse.ScrollWheelValue != _lastScrollValue)
            {
                float zoomDelta = (mouse.ScrollWheelValue - _lastScrollValue) * 0.001f;
                Zoom += zoomDelta;
                _lastScrollValue = mouse.ScrollWheelValue;
            }
        }

        private int _lastScrollValue = 0;

        public override Vector2 WorldToScreen(Vector3 worldPos)
        {
            float tileWidth = GameSetting.WorldTileWidth;
            float tileHeight = GameSetting.WorldTileHeight;
            float verticalStep = GameSetting.WorldChunkHeight; // Высота "ступеньки" по Z

            float screenX = (worldPos.X - worldPos.Y) * (tileWidth / 2);
            float screenY = (worldPos.X + worldPos.Y) * (tileHeight / 2) - worldPos.Z * verticalStep;

            return new Vector2(screenX, screenY) + _offset; // + смещение камеры
        }



        public override Vector3 ScreenToWorld(Vector2 screenPosition, float worldZ = 0)
        {
            // Обратное преобразование
            Vector2 centeredPos = (screenPosition - new Vector2(ViewportWidth / 2f, ViewportHeight / 2f)) * Zoom;
            // Упрощенная реализация - для полной нужна матрица обратного преобразования
            return new Vector3(centeredPos.X + Position.X, centeredPos.Y + Position.Y, worldZ);
        }

        protected override void UpdateViewMatrix()
        {
            // Кастомная матрица вида для 2.5D с учетом зума
            Matrix viewMatrix = Matrix.CreateLookAt(
                Position,
                Target,
                Vector3.Up);

            // Применяем зум
            viewMatrix *= Matrix.CreateScale(Zoom, Zoom, 1);

            SetViewMatrix(viewMatrix);
        }
    }
}