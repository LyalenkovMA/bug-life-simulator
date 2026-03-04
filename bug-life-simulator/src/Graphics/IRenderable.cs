using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using RectangleF = TalesFromTheUnderbrush.src.GameRectangleF;

namespace TalesFromTheUnderbrush.src.Graphics
{
    /// <summary>
    /// Интерфейс для всех отрисовываемых объектов в игре.
    /// Работает с переданной экранной позицией (не вычисляет сам).
    /// </summary>
    public interface IRenderable
    {
        // === СВОЙСТВА ===

        /// <summary>
        /// Порядок отрисовки (меньшее значение = рисуется раньше).
        /// Используется для глобальной сортировки в World.Draw().
        /// </summary>
        float DrawOrder { get; set; }

        /// <summary>
        /// Видимость объекта.
        /// </summary>
        bool Visible { get; set; }

        // === СОБЫТИЯ ===

        /// <summary>
        /// Событие изменения порядка отрисовки.
        /// </summary>
        event EventHandler DrawOrderChanged;

        /// <summary>
        /// Событие изменения видимости.
        /// </summary>
        event EventHandler VisibleChanged;

        // === МЕТОДЫ ОТРИСОВКИ ===

        /// <summary>
        /// Основной метод отрисовки.
        /// Вызывается из World.Draw() с переданной экранной позицией.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch для отрисовки</param>
        /// <param name="screenPosition">Экранная позиция (центр верхней грани, вычислен через GlobalSettings/Camera)</param>
        /// <param name="drawDepth">Глубина для сортировки SpriteBatch (0.0–0.9999)</param>
        /// <param name="zoom">Множитель зума для масштабирования спрайта</param>
        void Draw(SpriteBatch spriteBatch, Vector2 screenPosition, float drawDepth, float zoom = 1.0f);

        /// <summary>
        /// Дополнительный метод отрисовки (для совместимости).
        /// Используется когда позиция вычисляется внутри объекта (отладка).
        /// </summary>
        /// <param name="gameTime">Игровое время</param>
        /// <param name="spriteBatch">SpriteBatch для отрисовки</param>
        void Draw(GameTime gameTime, SpriteBatch spriteBatch);
    }

    // === РАСШИРЕНИЯ ИНТЕРФЕЙСА ===

    /// <summary>
    /// Для объектов, которым нужен доступ к игровому времени.
    /// </summary>
    public interface IRenderableWithTime : IRenderable
    {
        /// <summary>
        /// Обновление состояния перед отрисовкой.
        /// </summary>
        void Update(GameTime gameTime);
    }

    /// <summary>
    /// Для объектов с известными границами (для culling).
    /// </summary>
    public interface IHasBounds : IRenderable
    {
        /// <summary>
        /// Получить границы объекта в мировых координатах.
        /// </summary>
        RectangleF GetBounds();

        /// <summary>
        /// Проверить, находится ли объект в области видимости.
        /// </summary>
        bool IsInView(RectangleF viewBounds);
    }

    /// <summary>
    /// Для объектов с изменяемым порядком отрисовки.
    /// </summary>
    public interface IHasDrawOrder : IRenderable
    {
        /// <summary>
        /// Установить порядок отрисовки с вызовом события.
        /// </summary>
        void SetDrawOrder(float order);
    }

    /// <summary>
    /// Для объектов с изменяемой видимостью.
    /// </summary>
    public interface IHasVisibility : IRenderable
    {
        /// <summary>
        /// Установить видимость с вызовом события.
        /// </summary>
        void SetVisible(bool visible);
    }

    // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

    /// <summary>
    /// Расширения для работы с IRenderable.
    /// </summary>
    public static class RenderableExtensions
    {
        /// <summary>
        /// Сравнение по порядку отрисовки (для сортировки).
        /// </summary>
        public static int CompareByDrawOrder(IRenderable a, IRenderable b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            return a.DrawOrder.CompareTo(b.DrawOrder);
        }

        /// <summary>
        /// Проверить, находится ли объект в области видимости камеры.
        /// </summary>
        public static bool IsInCameraView(this IRenderable renderable, RectangleF cameraBounds)
        {
            if (!renderable.Visible) return false;

            if (renderable is IHasBounds hasBounds)
            {
                return cameraBounds.Intersects(hasBounds.GetBounds());
            }

            return true; // Если нет границ, считаем видимым
        }

        /// <summary>
        /// Установить видимость с вызовом события.
        /// </summary>
        public static void SetVisible(this IRenderable renderable, bool visible)
        {
            if (renderable is IHasVisibility hasVisibility)
            {
                hasVisibility.SetVisible(visible);
            }
            else if (renderable.Visible != visible)
            {
                renderable.Visible = visible;
            }
        }

        /// <summary>
        /// Установить порядок отрисовки с вызовом события.
        /// </summary>
        public static void SetDrawOrder(this IRenderable renderable, float order)
        {
            if (renderable is IHasDrawOrder hasDrawOrder)
            {
                hasDrawOrder.SetDrawOrder(order);
            }
            else if (Math.Abs(renderable.DrawOrder - order) > float.Epsilon)
            {
                renderable.DrawOrder = order;
            }
        }
    }
}