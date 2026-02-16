using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TalesFromTheUnderbrush.src;
using TalesFromTheUnderbrush.src.GameLogic;
using TalesFromTheUnderbrush.src.Graphics;
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

        private KeyboardState _prevKeyboardState;
        private MouseState _prevMouseState;
        private GameStateType _currentState;
        private readonly Dictionary<GameStateType, IGameState> _states;
        private World _world;
        private SpriteBatch _spriteBatch;
        private GraphicsDevice _graphicsDevice;
        private GraphicsDeviceManager _graphics;
        private ContentManager _contentManager;
        private AssetManager _assetManager;

        // Для загрузки текстур тайлов
        private Texture2D _grassAtlas;
        private Rectangle _grassSourceRect;
        private Rectangle _dirtSourceRect;

        public GameManager(GraphicsDeviceManager graphics, ContentManager contentManager)
        {
            _graphics = graphics;
            _states = new Dictionary<GameStateType, IGameState>();
            _currentState = GameStateType.MainMenu;
            _contentManager = contentManager;
            _assetManager = new AssetManager(contentManager);

            // УБРАНО: создание _world здесь — будет в Initialize
            InitializeStates();
        }

        public void Initialize(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;

            // Создаём мир ПОСЛЕ инициализации графики
            _world = new World("TestWorld", 30, 30);

            // Инициализируем камеру с правильными размерами
            Camera = new Camera2_5D(
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
            // 1. ЗАГРУЗКА АТЛАСА ТАЙЛОВ (временно закомментировано до реализации InitializeTiles в World)
            try
            {
                // Загружаем атлас (путь должен соответствовать структуре Content/)
                _grassAtlas = _assetManager.Load<Texture2D>("Tilesets/GrassTiles");


                // Определяем области в атласе (пример для атласа 512x256 с тайлами 256x128)
                _grassSourceRect = new Rectangle(0, 0, 256, 128);   // Трава в левом верхнем углу
                _dirtSourceRect = new Rectangle(256, 0, 256, 128);  // Грязь в правом верхнем углу

                Console.WriteLine("[GameManager] Атлас тайлов загружен успешно");

                // ВРЕМЕННО: закомментировано до реализации InitializeTiles в World
                _world?.InitializeTiles(_grassAtlas, _grassSourceRect, _dirtSourceRect);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GameManager] Ошибка загрузки атласа: {ex.Message}");
                Console.WriteLine("Продолжаем с тестовыми цветными тайлами");
            }

            // 2. Загрузка контента для всех состояний
            foreach (var state in _states.Values)
            {
                try
                {
                    state.LoadContent();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GameManager] Ошибка загрузки состояния {state}: {ex.Message}");
                }
            }
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

            // 2. УПРАВЛЕНИЕ КАМЕРОЙ (добавлено!)
            HandleCameraInput(currentKeyboard, currentMouse, gameTime);

            // 3. Обновление мира
            _world?.Update(gameTime);

            // 4. Отладочные команды
            HandleDebugInput(currentKeyboard);

            // Сохраняем состояние для следующего кадра
            _prevKeyboardState = currentKeyboard;
            _prevMouseState = currentMouse;
        }

        // === УПРАВЛЕНИЕ КАМЕРОЙ ===
        private void HandleCameraInput(KeyboardState keyboard, MouseState mouse, GameTime gameTime)
        {
            if (Camera == null) return;

            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float moveSpeed = GameSetting.CameraMoveSpeed * 200f * delta; // Масштабируем для плавности

            // Стрелки / WASD для перемещения
            Vector2 moveDir = Vector2.Zero;
            if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.A)) moveDir.X -= 1;
            if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.D)) moveDir.X += 1;
            if (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.W)) moveDir.Y -= 1;
            if (keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.S)) moveDir.Y += 1;

            if (moveDir != Vector2.Zero)
            {
                moveDir.Normalize();
                //Camera.Move(moveDir * moveSpeed);
            }

            // Колесо мыши для зума
            int scrollDelta = mouse.ScrollWheelValue - _prevMouseState.ScrollWheelValue;
            if (scrollDelta != 0)
            {
                float zoomDelta = scrollDelta > 0 ? GameSetting.CameraZoomSpeed : -GameSetting.CameraZoomSpeed;
                //Camera.Zoom(zoomDelta);
            }

            // ПКМ для перетаскивания камеры
            if (mouse.RightButton == ButtonState.Pressed && _prevMouseState.RightButton == ButtonState.Released)
            {
                // Начало перетаскивания (можно добавить флаг в Camera)
            }
            else if (mouse.RightButton == ButtonState.Pressed && _prevMouseState.RightButton == ButtonState.Pressed)
            {
                // Перемещение камеры при удержании ПКМ
                int dx = mouse.X - _prevMouseState.X;
                int dy = mouse.Y - _prevMouseState.Y;
                if (dx != 0 || dy != 0)
                {
                    //Camera.Move(new Vector2(-dx * 2, -dy * 2)); // Коэффициент для чувствительности
                }
            }
        }

        // === ОТРИСОВКА ===
        public void Draw(GameTime gameTime)
        {
            if (_graphicsDevice == null || _spriteBatch == null || _world == null)
                return;

            // 1. Очистка экрана (добавлено!)
            _graphicsDevice.Clear(Color.CornflowerBlue); // Временный цвет фона

            // 2. Начало отрисовки
            _spriteBatch.Begin(
                SpriteSortMode.BackToFront,
                BlendState.AlphaBlend,
                SamplerState.PointClamp, // Для пиксель-арта без размытия
                null, null, null
                //Camera?.GetViewMatrix() ?? Matrix.Identity
            );

            // 3. Отрисовка мира С КАМЕРОЙ (исправлено!)
            _world.Draw(_spriteBatch, Camera);

            // 4. Отрисовка UI (временно)
            // DrawUI(_spriteBatch, gameTime);

            // 5. Завершение отрисовки
            _spriteBatch.End();
        }

        // === УПРАВЛЕНИЕ СОСТОЯНИЯМИ ===
        public void ChangeState(GameStateType newState)
        {
            // Выход из текущего состояния
            if (_states.TryGetValue(_currentState, out var oldState))
                oldState.OnExit();

            _currentState = newState;

            // Вход в новое состояние
            if (_states.TryGetValue(_currentState, out var newStateObj))
                newStateObj.OnEnter();

            Console.WriteLine($"[GameManager] Состояние изменено на {_currentState}");
        }

        // === ОТЛАДОЧНЫЕ КОМАНДЫ ===
        private void HandleDebugInput(KeyboardState keyboard)
        {
            // F1: режим отладки
            if (keyboard.IsKeyDown(Keys.F1) && _prevKeyboardState.IsKeyUp(Keys.F1))
                GlobalSettings.ToggleDebugMode();

            // F2: FPS
            if (keyboard.IsKeyDown(Keys.F2) && _prevKeyboardState.IsKeyUp(Keys.F2))
                GlobalSettings.ToggleDebugSetting("fps");

            // F3: отображение тайлов
            if (keyboard.IsKeyDown(Keys.F3) && _prevKeyboardState.IsKeyUp(Keys.F3))
                GlobalSettings.ToggleDebugSetting("tiles");

            // F4: SpatialGrid
            if (keyboard.IsKeyDown(Keys.F4) && _prevKeyboardState.IsKeyUp(Keys.F4))
                GlobalSettings.ToggleDebugSetting("grid");

            // F5: информация о камере
            if (keyboard.IsKeyDown(Keys.F5) && _prevKeyboardState.IsKeyUp(Keys.F5))
            {
                GlobalSettings.ToggleDebugSetting("camera");
            }

            // F6: бог-режим
            if (keyboard.IsKeyDown(Keys.F6) && _prevKeyboardState.IsKeyUp(Keys.F6))
            {
                GlobalSettings.GodMode = !GlobalSettings.GodMode;
                Console.WriteLine($"[GameManager] GodMode = {GlobalSettings.GodMode}");
            }

            // F10: перезагрузка мира (для тестов)
            if (keyboard.IsKeyDown(Keys.F10) && _prevKeyboardState.IsKeyUp(Keys.F10))
            {
                Console.WriteLine("[GameManager] Перезагрузка мира...");
                _world?.Dispose();
                _world = new World("TestWorld", 30, 30);
                // Временно: повторная загрузка атласа не требуется, так как тайлы без текстур
            }
        }

        // === ОЧИСТКА РЕСУРСОВ ===
        public void Dispose()
        {
            // Освобождение состояний
            foreach (var state in _states.Values)
            {
                if (state is IDisposable disposable)
                    disposable.Dispose();
            }

            // Освобождение мира
            _world?.Dispose();

            // Освобождение текстур (если загружены)
            _grassAtlas?.Dispose();
            _assetManager?.Dispose(); // Очистит кэш и освободит текстуры

            Console.WriteLine("[GameManager] Все ресурсы освобождены");
        }
    }
}