using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TalesFromTheUnderbrush.src.GameLogic;
using TalesFromTheUnderbrush.src.UI.Camera;

namespace TalesFromTheUnderbrush.src.UI
{
    public class DebugOverlay : IDrawable
    {
        private SpriteFont _font;
        private ICamera _camera;
        private World _world;
        private float _fps;
        private int _frameCount;
        private TimeSpan _elapsedTime = TimeSpan.Zero;

        public event EventHandler DrawDepthChanged;
        public event EventHandler VisibleChanged;

        public float DrawDepth => throw new NotImplementedException();

        public bool Visible => throw new NotImplementedException();

        public void Initialize(SpriteFont font, ICamera camera, World world)
        {
            _font = font;
            _camera = camera;
            _world = world;
        }

        public void Update(GameTime gameTime)
        {
            _elapsedTime += gameTime.ElapsedGameTime;

            if (_elapsedTime > TimeSpan.FromSeconds(1))
            {
                _fps = _frameCount;
                _frameCount = 0;
                _elapsedTime -= TimeSpan.FromSeconds(1);
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!GlobalSettings.DebugMode) return;

            List<string> lines = new List<string>();

            // FPS (если включено)
            if (GlobalSettings.ShowFPS)
            {
                lines.Add($"FPS: {_fps}");
            }

            // Координаты мыши (если включено)
            if (GlobalSettings.ShowMouseCoordinates)
            {
                MouseState mouse = Mouse.GetState();
                Vector2 mousePos = new Vector2(mouse.X, mouse.Y);

                if (_camera != null)
                {
                    Vector3 worldPos = _camera.ScreenToWorld(mousePos);
                    lines.Add($"Mouse: {mousePos.X:F0},{mousePos.Y:F0}");
                    lines.Add($"World: {worldPos.X:F1},{worldPos.Y:F1},{worldPos.Z:F1}");
                }
            }

            // Информация о камере (если включено)
            if (GlobalSettings.ShowCameraInfo && _camera != null)
            {
                lines.Add($"Camera: {_camera.Position.X:F0},{_camera.Position.Y:F0},{_camera.Position.Z:F0}");

                if (_camera is OrthographicCamera2_5D cam2_5D)
                {
                    lines.Add($"Zoom: {cam2_5D.Zoom:F2}x");
                }
            }

            // Информация о мире (если включено)
            if (GlobalSettings.ShowWorldInfo && _world != null)
            {
                lines.Add($"World: {_world.Name}");
                lines.Add($"Time: {_world.GetWorldTimeString()}");
                lines.Add($"State: {_world.State}");
            }

            // Рисуем все строки
            Vector2 position = new Vector2(10, 10);
            float lineHeight = _font.LineSpacing;

            // Фон
            if (lines.Count > 0)
            {
                float width = lines.Max(l => _font.MeasureString(l).X) + 20;
                float height = lines.Count * lineHeight + 10;

                spriteBatch.DrawRectangle(
                    new Rectangle((int)position.X - 5, (int)position.Y - 5,
                                 (int)width, (int)height),
                    Color.Black * 0.5f);
            }

            // Текст
            foreach (string line in lines)
            {
                spriteBatch.DrawString(_font, line, position, Color.White);
                position.Y += lineHeight;
            }
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            throw new NotImplementedException();
        }

        public void SetDrawDepth(float depth)
        {
            throw new NotImplementedException();
        }

        public void SetVisible(bool visible)
        {
            throw new NotImplementedException();
        }
    }
}
