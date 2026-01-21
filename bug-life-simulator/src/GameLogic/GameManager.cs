using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using TalesFromTheUnderbrush;
using TalesFromTheUnderbrush.src.GameLogic;
using TalesFromTheUnderbrush.src.Graphics;
using TalesFromTheUnderbrush.tests;

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

        private GameStateType _currentState;
        private readonly Dictionary<GameStateType, IGameState> _states;
        private World _world;
        private SpriteBatch _spriteBatch;
        private GraphicsDevice _graphicsDevice;

        // Разные состояния игры
        //private MainMenuState _mainMenu;
        //private PlayingState _playingState;
        //private PauseState _pauseState;

        public GameManager()
        {
            _states = new Dictionary<GameStateType, IGameState>();
            _currentState = GameStateType.MainMenu;
           
            _world = new World("Test",30,30);

            InitializeStates();
        }

        private GraphicsDeviceManager _graphics;

        private void InitializeStates()
        {
            _states[GameStateType.MainMenu] = new TestState();
            //_mainMenu = new MainMenuState();
            //_playingState = new PlayingState();
            //_pauseState = new PauseState();

            //_states[GameState.MainMenu] = _mainMenu;
            //_states[GameState.Playing] = _playingState;
            //_states[GameState.Paused] = _pauseState;

            Camera = new Camera2_5D(
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );
        }

        public void LoadContent()
        {
            // Загрузка контента для всех состояний
            foreach (var state in _states.Values)
            {
                state.LoadContent();
            }
        }

        public void Update(GameTime gameTime)
        {
            // Обновление текущего состояния
            if (_states.TryGetValue(_currentState, out var currentState))
            {
                currentState.Update(gameTime);

                HandleDebugInput();

                // Проверка на смену состояния
                var nextState = currentState.GetNextState();
                if (nextState.HasValue && nextState.Value != _currentState)
                {
                    ChangeState(nextState.Value);
                }
            }
        }

        // В GameManager.Draw():
        protected void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // Используем World с камерой
            _world?.DrawWithCamera(_spriteBatch, ActiveCamera, GlobalSettings.DebugMode);

            // Отдельно рисуем UI
            //DrawUI();

            //Draw(gameTime);
        }

        public void ChangeState(GameStateType newState)
        {
            // Уведомление о выходе из старого состояния
            if (_states.TryGetValue(_currentState, out var oldState))
            {
                oldState.OnExit();
            }

            _currentState = newState;

            // Уведомление о входе в новое состояние
            if (_states.TryGetValue(_currentState, out var newStateObj))
            {
                newStateObj.OnEnter();
            }
        }

        public void Dispose()
        {
            foreach (var state in _states.Values)
            {
                if (state is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        // В GameManager.Update() добавить:
        private void HandleDebugInput()
        {
            KeyboardState keyboard = Keyboard.GetState();

            // F1 - переключить режим отладки
            if (keyboard.IsKeyDown(Keys.F1) && keyboard.IsKeyUp(Keys.F1))
            {
                GlobalSettings.ToggleDebugMode();
            }

            // F2 - показать/скрыть FPS
            if (keyboard.IsKeyDown(Keys.F2) && keyboard.IsKeyUp(Keys.F2))
            {
                GlobalSettings.ToggleDebugSetting("fps");
            }

            // F3 - показать/скрыть тайлы
            if (keyboard.IsKeyDown(Keys.F3) && _prevKeyboardState.IsKeyUp(Keys.F3))
            {
                GlobalSettings.ToggleDebugSetting("tiles");
            }

            // F4 - показать/скрыть SpatialGrid
            if (keyboard.IsKeyDown(Keys.F4) && _prevKeyboardState.IsKeyUp(Keys.F4))
            {
                GlobalSettings.ToggleDebugSetting("grid");
            }

            // F5 - показать/скрыть информацию о камере
            if (keyboard.IsKeyDown(Keys.F5) && _prevKeyboardState.IsKeyUp(Keys.F5))
            {
                GlobalSettings.ToggleDebugSetting("camera");
            }

            // F6 - бог-режим
            if (keyboard.IsKeyDown(Keys.F6) && _prevKeyboardState.IsKeyUp(Keys.F6))
            {
                GlobalSettings.GodMode = !GlobalSettings.GodMode;
                Console.WriteLine($"[GameManager] GodMode = {GlobalSettings.GodMode}");
            }

            // F12 - сохранить скриншот дебаг-информации
            if (keyboard.IsKeyDown(Keys.F12) && _prevKeyboardState.IsKeyUp(Keys.F12))
            {
                SaveDebugScreenshot();
            }
        }
    }
}
