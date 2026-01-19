using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using TalesFromTheUnderbrush.src.Graphics.Tiles;


// TileGrid.cs - ДОБАВИТЬ ЭТИ МЕТОДЫ
namespace TalesFromTheUnderbrush.src.Core.Tiles
{
    public class WorldState : IPersistable
    {
        // === ВНУТРЕННИЙ ENUM ===
        public enum StateType
        {
            Normal,     // Обычное состояние
            Paused,     // Пауза
            Danger,     // Опасность (враги рядом)
            Crisis,     // Кризис (катастрофа)
            Peaceful,   // Мирное время
            Night,      // Ночь
            Day         // День
        }

        // === СВОЙСТВА ===
        public StateType CurrentState { get; private set; }
        public float TimeScale { get; private set; } = 1.0f;
        public DateTime WorldTime { get; private set; }
        public int DayNumber { get; private set; } = 1;

        // Модификаторы состояния
        public float MovementModifier { get; private set; } = 1.0f;
        public float CombatModifier { get; private set; } = 1.0f;
        public float ResourceModifier { get; private set; } = 1.0f;

        public string PersistentId => throw new NotImplementedException();

        public string PersistentType => throw new NotImplementedException();

        public bool ShouldSave => throw new NotImplementedException();

        // === КОНСТРУКТОРЫ ===
        public WorldState()
        {
            CurrentState = StateType.Normal;
            WorldTime = new DateTime(1, 1, 1, 12, 0, 0); // Полдень первого дня
            UpdateModifiers();
        }

        public WorldState(PersistenceData data)
        {
            Load(data);
        }

        public event Action<IPersistable> OnBeforeSave;
        public event Action<IPersistable> OnAfterLoad;

        // === МЕТОДЫ УПРАВЛЕНИЯ ===

        /// <summary>
        /// Установить состояние мира
        /// </summary>
        public void SetState(StateType state, float timeScale = 1.0f)
        {
            CurrentState = state;
            TimeScale = MathHelper.Clamp(timeScale, 0.0f, 10.0f);
            UpdateModifiers();
        }

        /// <summary>
        /// Обновить время мира
        /// </summary>
        public void UpdateTime(GameTime gameTime)
        {
            float deltaSeconds = (float)gameTime.ElapsedGameTime.TotalSeconds * TimeScale;
            WorldTime = WorldTime.AddSeconds(deltaSeconds);

            // Проверяем смену дня/ночи
            UpdateDayNightCycle();
        }

        /// <summary>
        /// Получить строковое представление
        /// </summary>
        public override string ToString()
        {
            return $"{CurrentState} (Day {DayNumber}, {WorldTime:HH:mm})";
        }

        // === ПРИВАТНЫЕ МЕТОДЫ ===
        private void UpdateModifiers()
        {
            switch (CurrentState)
            {
                case StateType.Normal:
                    MovementModifier = 1.0f;
                    CombatModifier = 1.0f;
                    ResourceModifier = 1.0f;
                    break;

                case StateType.Paused:
                    MovementModifier = 0.0f;
                    CombatModifier = 0.0f;
                    ResourceModifier = 0.0f;
                    break;

                case StateType.Danger:
                    MovementModifier = 1.2f;
                    CombatModifier = 1.3f;
                    ResourceModifier = 0.8f;
                    break;

                case StateType.Crisis:
                    MovementModifier = 1.5f;
                    CombatModifier = 1.8f;
                    ResourceModifier = 0.5f;
                    break;

                case StateType.Peaceful:
                    MovementModifier = 0.8f;
                    CombatModifier = 0.5f;
                    ResourceModifier = 1.2f;
                    break;

                case StateType.Night:
                    MovementModifier = 0.7f;
                    CombatModifier = 1.1f;
                    ResourceModifier = 0.9f;
                    break;

                case StateType.Day:
                    MovementModifier = 1.1f;
                    CombatModifier = 0.9f;
                    ResourceModifier = 1.1f;
                    break;
            }
        }

        private void UpdateDayNightCycle()
        {
            // Ночь с 20:00 до 6:00
            bool isNight = WorldTime.Hour >= 20 || WorldTime.Hour < 6;

            if (isNight && CurrentState != StateType.Night)
            {
                SetState(StateType.Night, TimeScale);
            }
            else if (!isNight && CurrentState == StateType.Night)
            {
                SetState(StateType.Day, TimeScale);
            }

            // Увеличиваем день при смене даты
            if (WorldTime.Day != DayNumber)
            {
                DayNumber = WorldTime.Day;
            }
        }

        // === IPersistable РЕАЛИЗАЦИЯ ===
        public PersistenceData Save()
        {
            var data = new PersistenceData();
            data.SetValue("CurrentState", CurrentState.ToString());
            data.SetValue("TimeScale", TimeScale);
            data.SetValue("WorldTime", WorldTime.Ticks);
            data.SetValue("DayNumber", DayNumber);
            return data;
        }

        public void Load(PersistenceData data)
        {
            if (Enum.TryParse(data.GetValue<string>("CurrentState"), out StateType state))
            {
                CurrentState = state;
            }

            TimeScale = data.GetValue<float>("TimeScale", 1.0f);

            long ticks = data.GetValue<long>("WorldTime");
            WorldTime = new DateTime(ticks);

            DayNumber = data.GetValue<int>("DayNumber", 1);

            UpdateModifiers();
        }
    }
}