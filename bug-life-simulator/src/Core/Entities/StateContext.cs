using Microsoft.Xna.Framework;

namespace TalesFromTheUnderbrush.src.Core.Entities
{
    /// <summary>
    /// Фазы единого жизненного цикла состояния.
    /// </summary>
    public enum CharacterState { Idle, Preparing, Executing, Interrupted }

    /// <summary>
    /// Типы действий, инициируемых контроллером.
    /// </summary>
    public enum ActionType { None, Move, Interact, Rest, Attack }

    /// <summary>
    /// Параметры текущего состояния. Заполняются при запросе действия.
    /// </summary>
    public struct StateContext
    {
        public ActionType Action;
        public Point TargetGrid;
        public float TelegraphDuration; // Время визуального сигнала
        public float ExecutionDuration; // Время исполнения
    }
}