using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TalesFromTheUnderbrush
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private GameManager _gameManager;
        private SpriteBatch _spriteBatch;

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
            _gameManager = new GameManager(_graphics);
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            GlobalSettings.Initialize(Content, GraphicsDevice);

            _gameManager.Initialize(GraphicsDevice,_spriteBatch);
            // Создание менеджера игры

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