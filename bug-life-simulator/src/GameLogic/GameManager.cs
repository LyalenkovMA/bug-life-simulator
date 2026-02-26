using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TalesFromTheUnderbrush.src;
using TalesFromTheUnderbrush.src.GameLogic;
using TalesFromTheUnderbrush.src.UI.Camera;
using TalesFromTheUnderbrush.tests;
using Color = Microsoft.Xna.Framework.Color;

namespace TalesFromTheUnderbrush
{
    public enum GameStateType
    {
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Settings
    }

    public class GameManager : IDisposable
    {
        public Camera2_5D Camera { get; private set; }
        public TestCamera TestCamera { get; private set; }

        private KeyboardState _prevKeyboardState;
        private MouseState _prevMouseState;
        private GameStateType _currentState;
        private readonly Dictionary<GameStateType, IGameState> _states;
        private World _world;
        private SpriteBatch _spriteBatch;
        private GraphicsDevice _graphicsDevice;
        private GraphicsDeviceManager _graphics;
        private readonly GameAssetManager _assetManager; // ← Создаётся внутри

        // Для загрузки текстур тайлов
        private Texture2D _grassAtlas;
        private Rectangle _grassSourceRect;
        private Rectangle _dirtSourceRect;

        // КОНСТРУКТОР: принимаем ContentManager
        public GameManager(GraphicsDeviceManager graphics, ContentManager contentManager)
        {
            _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
            _assetManager = new GameAssetManager(contentManager ?? throw new ArgumentNullException(nameof(contentManager)));
            _states = new Dictionary<GameStateType, IGameState>();
            _currentState = GameStateType.MainMenu;
            InitializeStates();
        }

        public void Initialize(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;

            // Создаём мир ПОСЛЕ инициализации графики
            _world = new World("TestWorld", 30,30);

            // Инициализируем камеру с правильными размерами
            TestCamera = new TestCamera(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight
            );

            Console.WriteLine("[GameManager] Инициализация завершена");
        }

        private void InitializeStates()
        {
            _states[GameStateType.MainMenu] = new TestState();
            // Добавьте остальные состояния по мере готовности
        }

        public void LoadContent()
        {
            // Загружаем атлас (путь должен соответствовать структуре Content/)
            _grassAtlas = _assetManager.Load<Texture2D>("Tilesets/GrassTiles");

            // Определяем области в атласе (пример для атласа 512x256 с тайлами 256x128)
            _grassSourceRect = new Rectangle(0, 0, 256, (int)GameSetting.WorldTileWidth);   // Трава
            _dirtSourceRect = new Rectangle(256, 0, 256, (int)GameSetting.WorldTileWidth);  // Грязь

            // Инициализируем тайлы в мире
            _world?.InitializeTiles(_grassAtlas, _grassSourceRect, _dirtSourceRect);

            // 2. Загрузка контента для всех состояний
            foreach (IGameState state in _states.Values)
                state.LoadContent();
        }

        public void Update(GameTime gameTime)
        {
            KeyboardState currentKeyboard = Keyboard.GetState();
            MouseState currentMouse = Mouse.GetState();

            // 1. Обновление текущего состояния игры
            if (_states.TryGetValue(_currentState, out var currentState))
            {
                currentState.Update(gameTime);

                // Проверка смены состояния
                var nextState = currentState.GetNextState();
                if (nextState.HasValue && nextState.Value != _currentState)
                    ChangeState(nextState.Value);
            }

            // 2. УПРАВЛЕНИЕ КАМЕРОЙ
            HandleCameraInput(currentKeyboard, currentMouse, gameTime);

            // 3. Обновление мира
            _world?.Update(gameTime);

            // 4. Обновление камеры
            TestCamera?.Update(gameTime);

            // 5. Отладочные команды
            HandleDebugInput(currentKeyboard);

            // Сохраняем состояние для следующего кадра
            _prevKeyboardState = currentKeyboard;
            _prevMouseState = currentMouse;
        }

        // === УПРАВЛЕНИЕ КАМЕРОЙ ===
        private void HandleCameraInput(KeyboardState keyboard, MouseState mouse, GameTime gameTime)
        {
            if (TestCamera == null) return;

            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float moveSpeed = GameSetting.CameraMoveSpeed * 200f * delta;

            // WASD для перемещения
            Vector2 moveDir = Vector2.Zero;
            if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A)) moveDir.X -= 1;
            if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D)) moveDir.X += 1;
            if (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W)) moveDir.Y -= 1;
            if (keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S)) moveDir.Y += 1;

            if (moveDir != Vector2.Zero)
            {
                moveDir.Normalize();
                TestCamera.Move(new Vector3(moveDir.X * moveSpeed, moveDir.Y * moveSpeed, 0));
            }

            // Колесо мыши для зума
            int scrollDelta = mouse.ScrollWheelValue - _prevMouseState.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                float zoomDelta = scrollDelta > 0 ? GameSetting.CameraZoomSpeed : -GameSetting.CameraZoomSpeed;
                // TestCamera.Zoom(zoomDelta); // Раскомментировать при реализации
            }

            // ПКМ для перетаскивания
            if (mouse.RightButton == ButtonState.Pressed && _prevMouseState.RightButton == ButtonState.Pressed)
            {
                int dx = mouse.X - _prevMouseState.X;
                int dy = mouse.Y - _prevMouseState.Y;
                if (dx != 0 || dy != 0)
                {
                    TestCamera.Move(new Vector3(-dx * 2, -dy * 2, 0));
                }
            }
        }

        // === ОТРИСОВКА ===
        public void Draw(GameTime gameTime)
        {
            if (_graphicsDevice == null || _spriteBatch == null || _world == null)
                return;

            // 1. Очистка экрана
            _graphicsDevice.Clear(Color.CornflowerBlue);

            // 2. Начало отрисовки
            _spriteBatch.Begin(
                SpriteSortMode.BackToFront,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null, null, null,
                TestCamera?.GetViewMatrix() ?? Matrix.Identity
            );

            // 3. Отрисовка мира с камерой
            _world.Draw(_spriteBatch, TestCamera);

            // 4. Завершение отрисовки
            _spriteBatch.End();
        }

        // === УПРАВЛЕНИЕ СОСТОЯНИЯМИ ===
        public void ChangeState(GameStateType newState)
        {
            if (_states.TryGetValue(_currentState, out var oldState))
                oldState.OnExit();

            _currentState = newState;

            if (_states.TryGetValue(_currentState, out var newStateObj))
                newStateObj.OnEnter();

            Console.WriteLine($"[GameManager] Состояние изменено на {_currentState}");
        }

        // === ОТЛАДОЧНЫЕ КОМАНДЫ ===
        private void HandleDebugInput(KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.F1) && _prevKeyboardState.IsKeyUp(Keys.F1))
                GlobalSettings.ToggleDebugMode();

            if (keyboard.IsKeyDown(Keys.F5) && _prevKeyboardState.IsKeyUp(Keys.F5))
            {
                if (TestCamera != null)
                {
                    Console.WriteLine($"[CAMERA] Pos: ({TestCamera.Position.X:F1}, {TestCamera.Position.Y:F1})");
                }
            }

            if (keyboard.IsKeyDown(Keys.F10) && _prevKeyboardState.IsKeyUp(Keys.F10))
            {
                Console.WriteLine("[GameManager] Перезагрузка мира...");
                _world?.Dispose();
                _world = new World("TestWorld", 30, 30);
            }
        }

        // === ОЧИСТКА РЕСУРСОВ ===
        public void Dispose()
        {
            foreach (var state in _states.Values)
            {
                if (state is IDisposable disposable)
                    disposable.Dispose();
            }

            _world?.Dispose();
            _grassAtlas?.Dispose();
            _assetManager?.Dispose();

            Console.WriteLine("[GameManager] Все ресурсы освобождены");
        }
    }
}