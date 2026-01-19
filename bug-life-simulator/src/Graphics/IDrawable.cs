using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;


namespace TalesFromTheUnderbrush.src.Graphics
{
    /// <summary>
    /// Интерфейс для объектов, которые рисуются
    /// </summary>
    public interface IDrawable
    {
        /// <summary>
        /// Глубина отрисовки (меньше = раньше/ниже)
        /// </summary>
        float DrawDepth { get; }

        /// <summary>
        /// Видимость
        /// </summary>
        bool Visible { get; }

        /// <summary>
        /// Отрисовка
        /// </summary>
        void Draw(GameTime gameTime, SpriteBatch spriteBatch);

        /// <summary>
        /// Установить глубину отрисовки
        /// </summary>
        void SetDrawDepth(float depth);

        /// <summary>
        /// Установить видимость
        /// </summary>
        void SetVisible(bool visible);

        /// <summary>
        /// Событие изменения глубины
        /// </summary>
        event EventHandler DrawDepthChanged;

        /// <summary>
        /// Событие изменения видимости
        /// </summary>
        event EventHandler VisibleChanged;
    }
}
