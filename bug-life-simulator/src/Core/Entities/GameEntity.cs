using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TalesFromTheUnderbrush.src.GameLogic;
using TalesFromTheUnderbrush.src.Graphics;
using TalesFromTheUnderbrush.src.UI.Camera;
using RectangleF = TalesFromTheUnderbrush.src.GameRectangleF;
using Point = Microsoft.Xna.Framework.Point;
using System.Collections.Generic;

namespace TalesFromTheUnderbrush.src.Core.Entities
{
    /// <summary>
    /// Конкретный класс для игровых сущностей (персонажи, NPC, враги).
    /// Наследуется от Entity и добавляет игровую логику.
    /// </summary>
    public abstract class GameEntity : Entity
    {
        // === ИГРОВАЯ СТАТИСТИКА ===
        public int Health { get; private set; } = 100;
        public int MaxHealth { get; private set; } = 100;
        public int Energy { get; private set; } = 100;
        public int MaxEnergy { get; private set; } = 100;

        // === ДВИЖЕНИЕ ===
        public float MoveSpeed { get; private set; } = 3.0f;
        public bool IsMoving { get; private set; } = false;
        public Point TargetGridPosition { get; private set; }

        // === ВЗАИМОДЕЙСТВИЕ ===
        public float InteractionRange { get; private set; } = 2.0f;
        public Entity CurrentInteractionTarget { get; private set; }

        // === СОСТОЯНИЕ ===
        public EntityState CurrentState { get; private set; } = EntityState.Idle;
        public float StateTimer { get; private set; } = 0f;

        // === КОНСТРУКТОР ===
        protected GameEntity(float depth, string name = null)
            : base(depth, name)
        {
            // Инициализация базовых характеристик
            SetSpriteSize(BaseSpriteWidth, BaseSpriteHeight);
            SetSize(BaseWidth, BaseHeight);
        }

        // === ОБНОВЛЕНИЕ СОСТОЯНИЯ ===
        public override void Update(GameTime gameTime)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Обновление таймера состояния
            StateTimer += delta;

            // Обновление энергии
            UpdateEnergy(delta);

            // Логика текущего состояния
            UpdateState(delta);

            // Проверка целей взаимодействия
            UpdateInteractionTarget();
        }

        // === ОБНОВЛЕНИЕ ЭНЕРГИИ ===
        protected virtual void UpdateEnergy(float delta)
        {
            if (IsMoving)
            {
                Energy = Math.Max(0, Energy - (int)(delta * 5));
            }
            else
            {
                Energy = Math.Min(MaxEnergy, Energy + (int)(delta * 2));
            }
        }

        // === ЛОГИКА СОСТОЯНИЯ ===
        protected virtual void UpdateState(float delta)
        {
            switch (CurrentState)
            {
                case EntityState.Idle:
                    UpdateIdleState(delta);
                    break;
                case EntityState.Moving:
                    UpdateMovingState(delta);
                    break;
                case EntityState.Interacting:
                    UpdateInteractingState(delta);
                    break;
                case EntityState.Combat:
                    UpdateCombatState(delta);
                    break;
            }
        }

        // === СОСТОЯНИЕ: ПОКОЙ ===
        protected virtual void UpdateIdleState(float delta)
        {
            IsMoving = false;
        }

        // === СОСТОЯНИЕ: ДВИЖЕНИЕ ===
        protected virtual void UpdateMovingState(float delta)
        {
            if (GridPosition == TargetGridPosition)
            {
                CurrentState = EntityState.Idle;
                IsMoving = false;
            }
            else
            {
                IsMoving = true;
                // Движение к цели (реализуется в наследниках)
            }
        }

        // === СОСТОЯНИЕ: ВЗАИМОДЕЙСТВИЕ ===
        protected virtual void UpdateInteractingState(float delta)
        {
            if (CurrentInteractionTarget == null ||
                GetDistanceTo(CurrentInteractionTarget) > InteractionRange)
            {
                CurrentState = EntityState.Idle;
                CurrentInteractionTarget = null;
            }
        }

        // === СОСТОЯНИЕ: БОЙ ===
        protected virtual void UpdateCombatState(float delta)
        {
            // Логика боя (реализуется в наследниках)
        }

        // === ДВИЖЕНИЕ ===
        public virtual void MoveTo(Point targetGridPosition)
        {
            TargetGridPosition = targetGridPosition;
            CurrentState = EntityState.Moving;
            StateTimer = 0f;
        }

        public virtual void MoveTo(int x, int y) => MoveTo(new Point(x, y));

        public virtual bool CanMoveTo(Point targetGridPosition)
        {
            if (World == null) return false;

            // Проверка границ мира
            // Проверка проходимости тайла
            // Проверка коллизий с другими сущностями
            return true;
        }

        // === ВЗАИМОДЕЙСТВИЕ ===
        public virtual void InteractWith(Entity target)
        {
            if (target == null) return;

            float distance = GetDistanceTo(target);
            if (distance <= InteractionRange)
            {
                CurrentInteractionTarget = target;
                CurrentState = EntityState.Interacting;
                StateTimer = 0f;
                OnInteractionStarted?.Invoke(this, target);
            }
        }

        public virtual void StopInteraction()
        {
            CurrentInteractionTarget = null;
            CurrentState = EntityState.Idle;
            OnInteractionEnded?.Invoke(this);
        }

        // === УТИЛИТЫ ===
        public float GetDistanceTo(Entity other)
        {
            if (other == null) return float.MaxValue;

            Point myPos = GetWorldGridPosition();
            Point otherPos = other.GetWorldGridPosition();

            float dx = myPos.X - otherPos.X;
            float dy = myPos.Y - otherPos.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public float GetDistanceTo(Point gridPosition)
        {
            Point myPos = GetWorldGridPosition();
            float dx = myPos.X - gridPosition.X;
            float dy = myPos.Y - gridPosition.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        // === ПРОВЕРКА ЦЕЛЕЙ ВЗАИМОДЕЙСТВИЯ ===
        protected virtual void UpdateInteractionTarget()
        {
            if (World == null) return;

            // Найти ближайшую сущность в радиусе взаимодействия
            List<Entity> nearbyEntities = World.GetEntitiesInArea(
                new RectangleF(
                    (int)(GridPosition.X - InteractionRange),
                    (int)(GridPosition.Y - InteractionRange),
                    (int)(InteractionRange * 2),
                    (int)(InteractionRange * 2))
                    );

            // Логика выбора цели (реализуется в наследниках)
        }

        // === УРОН И ЗДОРОВЬЕ ===
        public virtual void TakeDamage(int damage)
        {
            Health = Math.Max(0, Health - damage);
            OnDamaged?.Invoke(this, damage);

            if (Health <= 0)
            {
                OnDestroyed?.Invoke(this);
                MarkForRemoval();
            }
        }

        public virtual void Heal(int amount)
        {
            Health = Math.Min(MaxHealth, Health + amount);
            OnHealed?.Invoke(this, amount);
        }

        // === СОБЫТИЯ ===
        public event Action<GameEntity, Entity> OnInteractionStarted;
        public event Action<GameEntity> OnInteractionEnded;
        public event Action<GameEntity, int> OnDamaged;
        public event Action<GameEntity, int> OnHealed;
        public event Action<GameEntity> OnDestroyed;

        // === ОТРИСОВКА (переопределяется в конкретных сущностях) ===
        // Наследники реализуют DrawEntity() для конкретной отрисовки

        // === ДЛЯ ОТЛАДКИ ===
        public override string ToString()
        {
            Point worldPos = GetWorldGridPosition();
            return $"{GetType().Name} '{Name}' ({worldPos.X}, {worldPos.Y}, {GetWorldLayer()}) " +
                   $"[HP: {Health}/{MaxHealth}, State: {CurrentState}]";
        }
    }

    // === СОСТОЯНИЯ СУЩНОСТИ ===
    public enum EntityState
    {
        Idle,
        Moving,
        Interacting,
        Combat,
        Dead
    }
}