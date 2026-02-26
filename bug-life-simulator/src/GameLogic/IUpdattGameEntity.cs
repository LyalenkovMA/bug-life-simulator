using Microsoft.Xna.Framework;
using System;

namespace TalesFromTheUnderbrush
{
    /// <summary>
    /// Интерфейс для обновляемых объектов в игровом цикле.
    /// </summary>
    public interface IUpdattGameEntity
    {
        /// <summary>
        /// Приоритет обновления (меньше = раньше)
        /// </summary>
        int UpdateOrder { get; set; }

        /// <summary>
        /// Обновление состояния
        /// </summary>
        void Update(GameTime gameTime);

        /// <summary>
        /// Установить приоритет обновления
        /// </summary>
        void SetUpdateOrder(int order);

        /// <summary>
        /// Установить видимость
        /// </summary>
        void SetVisible(bool visible);

        /// <summary>
        /// Событие изменения UpdateOrder
        /// </summary>
        event EventHandler UpdateOrderChanged;
    }
}