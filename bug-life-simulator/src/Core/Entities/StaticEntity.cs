using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;

namespace TalesFromTheUnderbrush.src.Core.Entities
{
    /// <summary>
    /// Статичная сущность (тайл, декорация)
    /// </summary>
    public abstract class StaticEntity : GameEntity, ICollidable, IPersistable
    {
        // === ICollidable ===
        public virtual CollisionShape CollisionShape => CollisionShape.Box;
        public virtual CollisionLayer CollisionLayer => CollisionLayer.Terrain;

        public abstract CollisionLayer CollidesWith { get; }
        public abstract bool IsTrigger { get; }
        public abstract bool IsPassable { get; }
        public abstract string PersistentId { get; }
        public abstract string PersistentType { get; }
        public abstract bool ShouldSave { get; }

        public abstract event Action<IPersistable> OnBeforeSave;
        public abstract event Action<IPersistable> OnAfterLoad;

        public virtual bool CheckCollision(ICollidable other)
        {
            // Простая AABB проверка
            RectangleF myBounds = GetBounds();
            RectangleF otherBounds = other is Entity entity ? entity.GetBounds() : default;

            return myBounds.Contains(otherBounds);
        }

        public virtual void OnCollision(ICollidable other, Vector2 penetration)
        {
            // Статичные объекты обычно не реагируют на коллизии
        }

        // === IPersistable ===
        //public virtual EntityData Save()
        //{
        //    return new EntityData(this)
        //    {
        //        Properties = new Dictionary<string, string>()
        //    };
        //}

        //public virtual void Load(EntityData data)
        //{
        //    SetPosition(data.Position);
        //    SetHeight(data.Height);
        //    SetName(data.Name);
        //}

        // === Конструктор ===
        protected StaticEntity(string name = null) : base(name)
        {
            SetUpdateOrder(100); // Низкий приоритет обновления
        }

        // === Реализация GameEntity ===
        public override void Update(GameTime gameTime)
        {
            // Статичные объекты обычно не обновляются
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!Visible || !IsActive)
                return;

            // Базовая реализация будет в наследниках
        }

        public abstract CollisionBounds GetCollisionBounds();
        public abstract void OnCollision(CollisionInfo collision);

        PersistenceData IPersistable.Save()
        {
            throw new NotImplementedException();
        }

        public abstract void Load(PersistenceData data);
    }
}
