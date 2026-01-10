using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TalesFromTheUnderbrush
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private GameManager _gameManager;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Настройка графики
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.SynchronizeWithVerticalRetrace = true; // VSync включен
        }

        protected override void Initialize()
        {
            // Инициализация глобальных сервисов (через GlobalSettings)
            GlobalSettings.Initialize(Content, GraphicsDevice);

            // Создание менеджера игры
            _gameManager = new GameManager();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Загрузка контента через GameManager
            _gameManager.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            // Выход по Escape
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Обновление игры через GameManager
            _gameManager.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Отрисовка через GameManager
            _gameManager.Draw(gameTime);

            base.Draw(gameTime);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _gameManager?.Dispose();
                GlobalSettings.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}