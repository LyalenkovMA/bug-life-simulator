using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Drawing;
using TalesFromTheUnderbrush.src.UI.Camera;

namespace TalesFromTheUnderbrush.src.Graphics
{
    /// <summary>
    /// Интерфейс для всех отрисовываемых объектов в игре
    /// Унифицирует систему отрисовки через GameManager и World
    /// </summary>
    public interface IDrawable
    {
        /// <summary>
        /// Порядок отрисовки (меньшее значение - рисуется раньше)
        /// </summary>
        float DrawOrder { get; }

        /// <summary>
        /// Видимость объекта
        /// </summary>
        bool Visible { get; }

        /// <summary>
        /// Событие изменения порядка отрисовки
        /// </summary>
        event EventHandler DrawOrderChanged;

        /// <summary>
        /// Событие изменения видимости
        /// </summary>
        event EventHandler VisibleChanged;

        /// <summary>
        /// Основной метод отрисовки
        /// </summary>
        /// <param name="gameTime">Игровое время</param>
        void Draw(GameTime gameTime);

        /// <summary>
        /// Дополнительный метод отрисовки с SpriteBatch
        /// Для объектов, которым нужен прямой доступ к SpriteBatch
        /// </summary>
        /// <param name="gameTime">Игровое время</param>
        /// <param name="spriteBatch">Контекст отрисовки</param>
        void Draw(GameTime gameTime, SpriteBatch spriteBatch);
    }

    /// <summary>
    /// Расширение интерфейса для объектов, требующих камеру
    /// </summary>
    public interface IDrawableWithCamera : IDrawable
    {
        /// <summary>
        /// Отрисовка с учетом камеры
        /// </summary>
        void Draw(GameTime gameTime, SpriteBatch spriteBatch, ICamera camera);
    }

    /// <summary>
    /// Расширение для объектов с состоянием отрисовки
    /// </summary>
    public interface IDrawableWithState : IDrawable
    {
        /// <summary>
        /// Состояние отрисовки (для оптимизации)
        /// </summary>
        DrawState DrawState { get; }

        /// <summary>
        /// Обновить состояние отрисовки
        /// </summary>
        void UpdateDrawState(ICamera camera);
    }

    /// <summary>
    /// Состояние отрисовки объекта
    /// </summary>
    public enum DrawState
    {
        /// <summary>
        /// Объект вне видимой области
        /// </summary>
        OutOfView,

        /// <summary>
        /// Объект частично виден
        /// </summary>
        PartiallyVisible,

        /// <summary>
        /// Объект полностью виден
        /// </summary>
        FullyVisible,

        /// <summary>
        /// Объект затенен/закрыт другими объектами
        /// </summary>
        Occluded,

        /// <summary>
        /// Объект требует обновления
        /// </summary>
        Dirty
    }

    /// <summary>
    /// Вспомогательный класс для работы с IDrawable
    /// </summary>
    public static class DrawableExtensions
    {
        /// <summary>
        /// Сравнение по порядку отрисовки
        /// </summary>
        public static int CompareByDrawOrder(IDrawable a, IDrawable b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            return a.DrawOrder.CompareTo(b.DrawOrder);
        }

        /// <summary>
        /// Установить порядок отрисовки с вызовом события
        /// </summary>
        public static void SetDrawOrder(this IDrawable drawable, float order)
        {
            if (drawable is IHasDrawOrder hasDrawOrder)
            {
                hasDrawOrder.SetDrawOrder(order);
            }
        }

        /// <summary>
        /// Установить видимость с вызовом события
        /// </summary>
        public static void SetVisible(this IDrawable drawable, bool visible)
        {
            if (drawable is IHasVisibility hasVisibility)
            {
                hasVisibility.SetVisible(visible);
            }
        }

        /// <summary>
        /// Проверить, находится ли объект в области видимости камеры
        /// </summary>
        public static bool IsInCameraView(this IDrawable drawable, ICamera camera)
        {
            if (camera == null || !drawable.Visible) return false;

            if (drawable is IHasBounds hasBounds)
            {
                return camera.Bounds.Equals(hasBounds.GetBounds());
            }

            return true; // Если нет информации о границах, считаем видимым
        }
    }

    /// <summary>
    /// Интерфейс для объектов с изменяемым порядком отрисовки
    /// </summary>
    public interface IHasDrawOrder : IDrawable
    {
        /// <summary>
        /// Установить порядок отрисовки
        /// </summary>
        void SetDrawOrder(float order);
    }

    /// <summary>
    /// Интерфейс для объектов с изменяемой видимостью
    /// </summary>
    public interface IHasVisibility : IDrawable
    {
        /// <summary>
        /// Установить видимость
        /// </summary>
        void SetVisible(bool visible);
    }

    /// <summary>
    /// Интерфейс для объектов с известными границами
    /// </summary>
    public interface IHasBounds : IDrawable
    {
        /// <summary>
        /// Получить границы объекта
        /// </summary>
        RectangleF GetBounds();
    }

    /// <summary>
    /// Интерфейс для объектов, которым нужен SpriteBatch
    /// </summary>
    public interface IRequiresSpriteBatch : IDrawable
    {
        /// <summary>
        /// Установить SpriteBatch для отрисовки
        /// </summary>
        void SetSpriteBatch(SpriteBatch spriteBatch);
    }
}