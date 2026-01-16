using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace TalesFromTheUnderbrush
{
    public interface IUpdatable
    {
        /// <summary>
        /// Приоритет обновления (меньше = раньше)
        /// </summary>
        int UpdateOrder { get; }

        /// <summary>
        /// Обновление состояния
        /// </summary>
        void Update(GameTime gameTime);

        /// <summary>
        /// Установить приоритет обновления
        /// </summary>
        void SetUpdateOrder(int order);
        void SetVisible(bool visible);

        /// <summary>
        /// Событие изменения UpdateOrder
        /// </summary>
        event EventHandler UpdateOrderChanged;
    }
}
