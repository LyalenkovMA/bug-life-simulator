using System;
using System.Drawing;
using Microsoft.Xna.Framework;

namespace TalesFromTheUnderbrush
{
    /// <summary>
    /// Интерфейс для объектов с коллизиями
    /// </summary>
    public interface ICollidable
    {
        /// <summary>
        /// Форма коллизии
        /// </summary>
        CollisionShape CollisionShape { get; }

        /// <summary>
        /// Слой коллизии (битовая маска)
        /// </summary>
        CollisionLayer CollisionLayer { get; }

        /// <summary>
        /// Слои, с которыми происходит столкновение
        /// </summary>
        CollisionLayer CollidesWith { get; }

        /// <summary>
        /// Получить границы коллизии в мировых координатах
        /// </summary>
        CollisionBounds GetCollisionBounds();

        /// <summary>
        /// Проверить коллизию с другим объектом
        /// </summary>
        bool CheckCollision(ICollidable other);

        /// <summary>
        /// Обработка коллизии
        /// </summary>
        void OnCollision(CollisionInfo collision);

        /// <summary>
        /// Игнорировать коллизии (для триггеров)
        /// </summary>
        bool IsTrigger { get; }

        /// <summary>
        /// Проходимый (можно проходить сквозь)
        /// </summary>
        bool IsPassable { get; }
    }

    /// <summary>
    /// Форма коллизии
    /// </summary>
    public enum CollisionShape
    {
        None,       // Нет коллизии
        Rectangle,  // Прямоугольник (AABB)
        Circle,     // Круг
        Box,        // 3D коробка (с учётом высоты)
        Polygon,    // Произвольный полигон
        Point       // Точка
    }

    /// <summary>
    /// Слои коллизий (битовая маска)
    /// </summary>
    [Flags]
    public enum CollisionLayer
    {
        None = 0,
        Terrain = 1 << 0,   // Земля, вода, непроходимые тайлы
        Character = 1 << 1,   // Персонажи, NPC
        Item = 1 << 2,   // Предметы на земле
        Projectile = 1 << 3,   // Снаряды, пули
        Trigger = 1 << 4,   // Триггеры, зоны
        Decoration = 1 << 5,   // Декорации (проходимые)
        Building = 1 << 6,   // Постройки
        Flying = 1 << 7,   // Летающие объекты
        Water = 1 << 8,   // Водные объекты
        Climbable = 1 << 9,   // Поверхности для скалолазания

        // Группы для удобства
        All = ~0,
        Solid = Terrain | Building,
        Dynamic = Character | Item | Projectile,
        Interactable = Character | Item | Trigger
    }

    /// <summary>
    /// Информация о коллизии
    /// </summary>
    public struct CollisionInfo
    {
        public ICollidable Other;
        public Vector2 Penetration;     // Вектор проникновения (на сколько нужно отодвинуть)
        public Vector2 Normal;          // Нормаль столкновения
        public float Depth;             // Глубина проникновения
        public Vector2 Point;           // Точка столкновения
        public bool IsTrigger;          // Это триггерное столкновение

        public static CollisionInfo Empty => new()
        {
            Other = null,
            Penetration = Vector2.Zero,
            Normal = Vector2.Zero,
            Depth = 0,
            Point = Vector2.Zero,
            IsTrigger = false
        };

        public bool IsValid => Other != null && Depth > 0;
    }

    /// <summary>
    /// Границы коллизии
    /// </summary>
    public struct CollisionBounds
    {
        public CollisionShape Shape;

        // Для Rectangle
        public RectangleF Rectangle;

        // Для Circle
        public Vector2 Center;
        public float Radius;

        // Для Box (с учётом высоты)
        public Vector3 Min;
        public Vector3 Max;

        // Для Polygon
        public Vector2[] Vertices;

        public static CollisionBounds CreateRectangle(RectangleF rect)
        {
            return new CollisionBounds
            {
                Shape = CollisionShape.Rectangle,
                Rectangle = rect
            };
        }

        public static CollisionBounds CreateCircle(Vector2 center, float radius)
        {
            return new CollisionBounds
            {
                Shape = CollisionShape.Circle,
                Center = center,
                Radius = radius
            };
        }

        public static CollisionBounds CreateBox(Vector3 position, Vector3 size)
        {
            return new CollisionBounds
            {
                Shape = CollisionShape.Box,
                Min = position - size / 2,
                Max = position + size / 2
            };
        }

        public RectangleF GetBoundingRectangle()
        {
            return Shape switch
            {
                CollisionShape.Rectangle => Rectangle,
                CollisionShape.Circle => new RectangleF(
                    Center.X - Radius,
                    Center.Y - Radius,
                    Radius * 2,
                    Radius * 2
                ),
                CollisionShape.Box => new RectangleF(
                    Min.X,
                    Min.Y,
                    Max.X - Min.X,
                    Max.Y - Min.Y
                ),
                _ => RectangleF.Empty
            };
        }
    }
}
