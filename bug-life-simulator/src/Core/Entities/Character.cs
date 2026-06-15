using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TalesFromTheUnderbrush.src.Core.Entities.Controllers;
using System;

namespace TalesFromTheUnderbrush.src.Core.Entities
{
    public class Character : Entity
    {
        // === АДМИНИСТРАТОР СОСТОЯНИЙ ===
        public CharacterState CurrentState { get; private set; } = CharacterState.Idle;
        public ICharacterController Controller { get; private set; }
        private StateContext _context;
        private float _stateTimer;

        /// <summary>
        /// Событие смены состояния. Сюда позже подключится слой визуальных маркеров.
        /// </summary>
        public event Action<CharacterState, StateContext> OnStateChanged;

        public Character(string name, ICharacterController controller) : base(1.0f, name)
        {
            Controller = controller;
            Controller.Attach(this);
            SetSpriteSize(64, 128); // Базовые габариты
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // 1. Контроллер принимает решение или читает ввод
            Controller.Update(gameTime);

            // 2. Администратор обновляет машину состояний
            UpdateStateMachine(dt);

            // 3. Базовая логика сущности (синхронизация визуала, коллизии)
            Update(gameTime);
        }

        // === УПРАВЛЕНИЕ ЖИЗНЕННЫМ ЦИКЛОМ ===

        /// <summary>
        /// Контроллер вызывает этот метод для запроса действия.
        /// </summary>
        public bool RequestAction(ActionType action, Point target, float telegraphTime, float execTime)
        {
            if (CurrentState != CharacterState.Idle) return false; // Занят

            _context = new StateContext
            {
                Action = action,
                TargetGrid = target,
                TelegraphDuration = telegraphTime,
                ExecutionDuration = execTime
            };

            TransitionToState(CharacterState.Preparing);
            return true;
        }

        private void UpdateStateMachine(float dt)
        {
            switch (CurrentState)
            {
                case CharacterState.Idle:
                    break;

                case CharacterState.Preparing:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f)
                    {
                        TransitionToState(CharacterState.Executing);
                    }
                    break;

                case CharacterState.Executing:
                    _stateTimer -= dt;
                    if (_stateTimer <= 0f)
                    {
                        CompleteAction();
                    }
                    break;

                case CharacterState.Interrupted:
                    // Сброс и возврат в Idle
                    TransitionToState(CharacterState.Idle);
                    break;
            }
        }

        private void TransitionToState(CharacterState newState)
        {
            CurrentState = newState;
            _stateTimer = newState switch
            {
                CharacterState.Preparing => _context.TelegraphDuration,
                CharacterState.Executing => _context.ExecutionDuration,
                _ => 0f
            };

            // Уведомляем систему о смене фазы (здесь позже сработают маркеры)
            OnStateChanged?.Invoke(CurrentState, _context);

            // Запуск логики исполнения
            if (newState == CharacterState.Executing)
            {
                ExecuteAction();
            }
        }

        private void ExecuteAction()
        {
            switch (_context.Action)
            {
                case ActionType.Move:
                    // Запускаем плавное движение, уже реализованное в Entity
                    break;
                case ActionType.Attack:
                    // PlayAnimation("Attack");
                    break;
            }
        }

        private void CompleteAction()
        {
            // Фиксируем логическую позицию после завершения движения
            if (_context.Action == ActionType.Move)
            {
                GridPosition = _context.TargetGrid;
            }

            TransitionToState(CharacterState.Idle);
        }

        /// <summary>
        /// Вызывается при получении урона или смене приоритетов.
        /// </summary>
        public void Interrupt()
        {
            if (CurrentState == CharacterState.Idle) return;
            // Отменяем движение, если оно запущено
             TransitionToState(CharacterState.Interrupted);
        }

        // === ОТРИСОВКА (Заглушка) ===
        protected override void DrawEntity(SpriteBatch spriteBatch, Vector2 screenPosition, float drawDepth, float zoom)
        {
            Rectangle rect = new Rectangle(
                (int)(screenPosition.X - 32 * zoom),
                (int)(screenPosition.Y - 128 * zoom),
                (int)(64 * zoom),
                (int)(128 * zoom)
            );
            // Цвет меняется в зависимости от фазы (для отладки)
            Color debugColor = CurrentState switch
            {
                CharacterState.Idle => Color.Gray,
                CharacterState.Preparing => Color.Yellow,
                CharacterState.Executing => Color.Green,
                _ => Color.Red
            };
            //spriteBatch.Draw(Texture2D.White, rect, debugColor * 0.7f);
        }

        public override void Initialize()
        {
            throw new NotImplementedException();
        }
    }
}