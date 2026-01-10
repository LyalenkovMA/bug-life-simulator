using Microsoft.Xna.Framework;
using TalesFromTheUnderbrush;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

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
        private GameStateType _currentState;
        private readonly Dictionary<GameStateType, IGameState> _states;

        // Разные состояния игры
        //private MainMenuState _mainMenu;
        //private PlayingState _playingState;
        //private PauseState _pauseState;

        public GameManager()
        {
            _states = new Dictionary<GameStateType, IGameState>();
            _currentState = GameStateType.MainMenu;

            InitializeStates();
        }

        private void InitializeStates()
        {
            //_mainMenu = new MainMenuState();
            //_playingState = new PlayingState();
            //_pauseState = new PauseState();

            //_states[GameState.MainMenu] = _mainMenu;
            //_states[GameState.Playing] = _playingState;
            //_states[GameState.Paused] = _pauseState;
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

                // Проверка на смену состояния
                var nextState = currentState.GetNextState();
                if (nextState.HasValue && nextState.Value != _currentState)
                {
                    ChangeState(nextState.Value);
                }
            }
        }

        public void Draw(GameTime gameTime)
        {
            // Отрисовка текущего состояния
            if (_states.TryGetValue(_currentState, out var currentState))
            {
                currentState.Draw(gameTime);
            }

            // Финализация отрисовки кадра
            GlobalSettings.Renderer.Flush();
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
    }
}
