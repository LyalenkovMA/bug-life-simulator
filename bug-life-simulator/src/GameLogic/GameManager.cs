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

    /// <summary>
    /// Главный менеджер игры — оркестрирует все системы.
    /// Отвечает за: графику, камеру, мир, состояния игры, ввод.
    /// </summary>
    public class GameManager : IDisposable
    {
        // === КАМЕРЫ ===
        public Camera2_5D Camera { get; private set; }
        public TestCamera TestCamera { get; private set; }

        // === СОСТОЯНИЯ ===
        private KeyboardState _prevKeyboardState;
        private MouseState _prevMouseState;
        private GameStateType _currentState;
        private readonly Dictionary<GameStateType, IGameState> _states;

        // === МИР ===
        private World _world;

        // === ГРАФИКА ===
        private SpriteBatch _spriteBatch;
        private GraphicsDevice _graphicsDevice;
        private GraphicsDeviceManager _graphics;

        // === РЕСУРСЫ ===
        private readonly GameAssetManager _assetManager;
        private Texture2D _grassAtlas;

        // === КОНСТРУКТОР ===
        public GameManager(GraphicsDeviceManager graphics, ContentManager contentManager)
        {
            _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
            _assetManager = new GameAssetManager(contentManager ?? throw new ArgumentNullException(nameof(contentManager)));
            _states = new Dictionary<GameStateType, IGameState>();
            _currentState = GameStateType.MainMenu;

            InitializeStates();
        }

        // === ИНИЦИАЛИЗАЦИЯ ===
        public void Initialize(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;

            // === НАСТРОЙКА ГРАФИКИ ===
            _graphics.IsFullScreen = GlobalSettings.FullScreen;

            if (GlobalSettings.FullScreen)
            {
                // Полноэкранный режим: нативное разрешение монитора
                _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
            else
            {
                // Оконный режим: стандартное разрешение
                _graphics.PreferredBackBufferWidth = GlobalSettings.ScreenWidth;   // 1280
                _graphics.PreferredBackBufferHeight = GlobalSettings.ScreenHeight; // 720
            }

            _graphics.ApplyChanges();

            // === СОЗДАНИЕ МИРА ===
            _world = new World("TestWorld", 30, 30);

            // === СОЗДАНИЕ КАМЕРЫ ===
            TestCamera = new TestCamera(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight
            );

            Console.WriteLine($"[GameManager] Инициализация завершена | " +
                             $"Fullscreen: {GlobalSettings.FullScreen} | " +
                             $"Resolution: {_graphics.PreferredBackBufferWidth}x{_graphics.PreferredBackBufferHeight}");
        }

        // === ИНИЦИАЛИЗАЦИЯ СОСТОЯНИЙ ===
        private void InitializeStates()
        {
            _states[GameStateType.MainMenu] = new TestState();
            // Добавьте остальные состояния по мере готовности
        }

        // === ЗАГРУЗКА КОНТЕНТА ===
        public void LoadContent()
        {
            // === ЗАГРУЗКА АТЛАСА ===
            _grassAtlas = _assetManager.Load<Texture2D>("Tilesets/GrassTiles");

            // === ДИНАМИЧЕСКИЙ РАСЧЁТ РАЗМЕРОВ ТАЙЛОВ ИЗ АТЛАСА ===
            const int TilesPerRow = 5;  // Известно из вашего атласа (512px / 5 = ~102px)
            const int TileRows = 2;     // Известно из вашего атласа (256px / 2 = 128px)

            int tileArtWidth = _grassAtlas.Width / TilesPerRow;
            int tileArtHeight = _grassAtlas.Height / TileRows;

            Console.WriteLine($"[GameManager] Атлас: {_grassAtlas.Width}x{_grassAtlas.Height}");
            Console.WriteLine($"[GameManager] Тайл в атласе: {tileArtWidth}x{tileArtHeight}");

            // === ИНИЦИАЛИЗАЦИЯ МИРА ===
            _world?.InitializeTiles(_grassAtlas, TilesPerRow, TileRows);

            // === ЗАГРУЗКА СОСТОЯНИЙ ===
            foreach (IGameState state in _states.Values)
                state.LoadContent();

            Console.WriteLine("[GameManager] Контент загружен");
        }

        // === ОБНОВЛЕНИЕ ===
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

            // 5. Отладочные команды (полноэкранный режим и т.д.)
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
                TestCamera.ZoomIn(zoomDelta); // ← Используем метод из CameraBase
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
            // ✅ ВАЖНО: Matrix.Identity — камера не применяется через матрицу!
            // Камера применяется вручную через WorldToScreen() в World.Draw()
            _spriteBatch.Begin(
                SpriteSortMode.BackToFront,      // Сортировка по depth для изометрии
                BlendState.AlphaBlend,           // Прозрачность
                SamplerState.PointClamp,         // Пиксельная графика без сглаживания
                null, null, null,
                Matrix.Identity                  // ← БЕЗ матрицы камеры!
            );

            // 3. Отрисовка мира с камерой
            _world.Draw(_spriteBatch, TestCamera);

            // 4. Завершение отрисовки
            _spriteBatch.End();

            // 5. Отладочная информация (опционально)
            if (GlobalSettings.DebugMode && GlobalSettings.ShowCameraInfo)
            {
                DrawDebugOverlay(gameTime);
            }
        }

        // === ОТЛАДОЧНЫЙ СЛОЙ ===
        private void DrawDebugOverlay(GameTime gameTime)
        {
            // В будущем: отрисовка FPS, позиции камеры, информации о тайлах
            // Сейчас только вывод в консоль по F5
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
            // === ПОЛНОЭКРАННЫЙ РЕЖИМ (F11) ===
            if (keyboard.IsKeyDown(Keys.F11) && _prevKeyboardState.IsKeyUp(Keys.F11))
            {
                ToggleFullScreen();
                return;
            }

            // === Alt+Enter для fullscreen (альтернатива) ===
            if (keyboard.IsKeyDown(Keys.Enter) && keyboard.IsKeyDown(Keys.LeftAlt))
            {
                if (_prevKeyboardState.IsKeyUp(Keys.Enter))
                {
                    ToggleFullScreen();
                    return;
                }
            }

            // === Режим отладки (F1) ===
            if (keyboard.IsKeyDown(Keys.F1) && _prevKeyboardState.IsKeyUp(Keys.F1))
            {
                GlobalSettings.ToggleDebugMode();
            }

            // === Информация о камере (F5) ===
            if (keyboard.IsKeyDown(Keys.F5) && _prevKeyboardState.IsKeyUp(Keys.F5))
            {
                if (TestCamera != null)
                {
                    Console.WriteLine($"[CAMERA] Pos: ({TestCamera.Position.X:F1}, {TestCamera.Position.Y:F1}, {TestCamera.Position.Z:F1})");
                    Console.WriteLine($"[CAMERA] Zoom: {TestCamera.Zoom:F2}");
                    Console.WriteLine($"[CAMERA] Viewport: {TestCamera.ViewportWidth}x{TestCamera.ViewportHeight}");
                }
            }

            // === Перезагрузка мира (F10) ===
            if (keyboard.IsKeyDown(Keys.F10) && _prevKeyboardState.IsKeyUp(Keys.F10))
            {
                Console.WriteLine("[GameManager] Перезагрузка мира...");
                _world?.Dispose();
                _world = new World("TestWorld", 30, 30);

                // Переинициализируем тайлы
                if (_grassAtlas != null)
                {
                    const int TilesPerRow = 5;
                    const int TileRows = 2;
                    _world.InitializeTiles(_grassAtlas, TilesPerRow, TileRows);
                }
            }

            //// === Сброс камеры (F12) ===
            //if (keyboard.IsKeyDown(Keys.F12) && _prevKeyboardState.IsKeyUp(Keys.F12))
            //{
            //    if (TestCamera != null)
            //    {
            //        TestCamera.SetPosition(new Vector3(0, 0, 500f));
            //        TestCamera.SetTarget(new Vector3(0, 0, 0f));
            //        Console.WriteLine("[GameManager] Камера сброшена");
            //    }
            //}
        }

        // === ПЕРЕКЛЮЧЕНИЕ ПОЛНОЭКРАННОГО РЕЖИМА ===
        private void ToggleFullScreen()
        {
            bool isFullScreen = !GlobalSettings.FullScreen;

            _graphics.IsFullScreen = GlobalSettings.FullScreen;

            if (isFullScreen)
            {
                // Оконный режим
                _graphics.PreferredBackBufferWidth = GlobalSettings.ScreenWidth;
                _graphics.PreferredBackBufferHeight = GlobalSettings.ScreenHeight;
            }
            else
            {
                // Полноэкранный режим
                _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }

            _graphics.ApplyChanges();

            // Обновляем камеру с новыми размерами вьюпорта
            TestCamera?.SetViewport(
                _graphics.PreferredBackBufferWidth,
                _graphics.PreferredBackBufferHeight
            );

            Console.WriteLine($"[GameManager] Fullscreen: {GlobalSettings.FullScreen} | " +
                             $"Resolution: {_graphics.PreferredBackBufferWidth}x{_graphics.PreferredBackBufferHeight}");
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