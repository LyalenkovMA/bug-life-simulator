using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TalesFromTheUnderbrush.src.Core.Entities;
using TalesFromTheUnderbrush.src.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using RectangleF = TalesFromTheUnderbrush.src.GameRectangleF;
using Point = Microsoft.Xna.Framework.Point;

namespace TalesFromTheUnderbrush.src.Core.Entities
{
    /// <summary>
    /// Статичная сущность (декорация, здание, ресурс).
    /// Наследуется от Entity и добавляет коллизии + сохранение.
    /// НЕ двигается самостоятельно (в отличие от MobileEntity).
    /// </summary>
    public abstract class StaticEntity : Entity, ICollidable, IPersistable
    {
        // === ICollidable ===
        public virtual CollisionShape CollisionShape => CollisionShape.Box;
        public virtual CollisionLayer CollisionLayer => CollisionLayer.Terrain;
        public abstract CollisionLayer CollidesWith { get; }
        public abstract bool IsTrigger { get; }
        public abstract bool IsPassable { get; }

        // === IPersistable ===
        public abstract string PersistentId { get; }
        public abstract string PersistentType { get; }
        public abstract bool ShouldSave { get; }

        public event Action<IPersistable> OnBeforeSave;
        public event Action<IPersistable> OnAfterLoad;

        // === КОНСТРУКТОР ===
        protected StaticEntity(float depth, string name = null)
            : base(depth, name)
        {
            // Статичные сущности имеют низкий приоритет обновления
            UpdateDrawOrder();
        }

        // === IRenderable.Draw — переопределяем для статичных сущностей ===

        /// <summary>
        /// Основной метод отрисовки (из Entity → IRenderable).
        /// Вызывается из World.Draw() с переданной экранной позицией.
        /// </summary>
        public override void Draw(
            SpriteBatch spriteBatch,
            Vector2 screenPosition,
            float drawDepth,
            float zoom = 1.0f)
        {
            if (!Visible || !IsActive || spriteBatch == null)
                return;

            // Вызываем абстрактный метод для конкретной реализации
            DrawStaticEntity(spriteBatch, screenPosition, drawDepth, zoom);
        }

        /// <summary>
        /// Метод для переопределения в наследниках.
        /// Реализует конкретную отрисовку статичной сущности.
        /// </summary>
        protected abstract void DrawStaticEntity(
            SpriteBatch spriteBatch,
            Vector2 screenPosition,
            float drawDepth,
            float zoom);

        // === ICollidable — упрощённая реализация ===

        public virtual bool CheckCollision(ICollidable other)
        {
            if (other == null || IsPassable) return false;

            RectangleF myBounds = GetCollisionBounds();
            RectangleF otherBounds = other is Entity entity
                ? entity.GetCollisionBounds()
                : default;

            return myBounds.Intersects(otherBounds);
        }

        public virtual void OnCollision(ICollidable other, Vector2 penetration)
        {
            // Статичные объекты обычно не реагируют на коллизии
            // Наследники могут переопределить
        }

        public abstract RectangleF GetCollisionBounds();
        public abstract void OnCollision(CollisionInfo collision);

        // === IPersistable — базовая реализация ===

        public virtual PersistenceData Save()
        {
            OnBeforeSave?.Invoke(this);

            var data = new PersistenceData
            {
                Id = PersistentId,
                Type = PersistentType,
                Position = new Point(GridPosition.X, GridPosition.Y),
                Layer = Layer,
                Properties = new Dictionary<string, string>()
            };

            // Сохраняем дополнительные свойства
            SaveAdditionalData(data.Properties);

            return data;
        }

        public virtual void Load(PersistenceData data)
        {
            if (data == null) return;

            SetGridPosition(data.Position);
            SetLayer(data.Layer);

            // Загружаем дополнительные свойства
            if (data.Properties != null)
                LoadAdditionalData(data.Properties);

            OnAfterLoad?.Invoke(this);
        }

        // === Методы для расширения в наследниках ===

        /// <summary>
        /// Сохранить дополнительные данные (переопределяется в наследниках).
        /// </summary>
        protected virtual void SaveAdditionalData(Dictionary<string, string> properties)
        {
            // Наследники добавляют свои данные
        }

        /// <summary>
        /// Загрузить дополнительные данные (переопределяется в наследниках).
        /// </summary>
        protected virtual void LoadAdditionalData(Dictionary<string, string> properties)
        {
            // Наследники загружают свои данные
        }

        // === ОБЯЗАТЕЛЬНЫЕ абстрактные методы (из Entity) ===

        /// <summary>
        /// Обязательная инициализация (требование Entity.cs).
        /// </summary>
        public abstract override void Initialize();

        /// <summary>
        /// Обязательное обновление (требование Entity.cs).
        /// </summary>
        public abstract override void Update(GameTime gameTime);

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Проверить, находится ли сущность в видимой области.
        /// </summary>
        public bool IsInView(RectangleF viewBounds)
        {
            RectangleF bounds = GetCollisionBounds();
            return viewBounds.Intersects(bounds);
        }

        /// <summary>
        /// Установить проходимость (для динамического изменения).
        /// </summary>
        public virtual void SetPassable(bool passable)
        {
            // Наследники могут реализовать
        }

        // === ОТЛАДОЧНАЯ ИНФОРМАЦИЯ ===

        public override string ToString()
        {
            Point worldPos = GetWorldGridPosition();
            return $"StaticEntity '{Name}' ({worldPos.X}, {worldPos.Y}, {GetWorldLayer()}) " +
                   $"[Visible: {Visible}, DrawOrder: {DrawOrder:F3}, Passable: {IsPassable}]";
        }
    }

    // === ВСПОМОГАТЕЛЬНЫЕ ТИПЫ ===

    public enum CollisionShape
    {
        Box,
        Circle,
        Polygon
    }

    public enum CollisionLayer
    {
        Default,
        Terrain,
        Entity,
        Trigger,
        Projectile
    }

    public class CollisionInfo
    {
        public ICollidable Other { get; set; }
        public Vector2 Penetration { get; set; }
        public float Impulse { get; set; }
    }

    public class PersistenceData
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public Point Position { get; set; }
        public int Layer { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public float Rotation { get; set; }
        public float Scale { get; set; } = 1.0f;
    }

    // === ИНТЕРФЕЙСЫ ===

    public interface ICollidable
    {
        CollisionShape CollisionShape { get; }
        CollisionLayer CollisionLayer { get; }
        CollisionLayer CollidesWith { get; }
        bool IsTrigger { get; }
        bool IsPassable { get; }

        bool CheckCollision(ICollidable other);
        void OnCollision(ICollidable other, Vector2 penetration);
        RectangleF GetCollisionBounds();
        void OnCollision(CollisionInfo collision);
    }

    public interface IPersistable
    {
        string PersistentId { get; }
        string PersistentType { get; }
        bool ShouldSave { get; }

        event Action<IPersistable> OnBeforeSave;
        event Action<IPersistable> OnAfterLoad;

        PersistenceData Save();
        void Load(PersistenceData data);
    }
}