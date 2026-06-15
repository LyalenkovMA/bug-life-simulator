using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TalesFromTheUnderbrush.src.GameLogic;
using TalesFromTheUnderbrush.src.Graphics;
using TalesFromTheUnderbrush.src.UI.Camera;
using RectangleF = TalesFromTheUnderbrush.src.GameRectangleF;
using Point = Microsoft.Xna.Framework.Point;

namespace TalesFromTheUnderbrush.src.Core.Entities
{
    /// <summary>
    /// Базовый класс для ВСЕХ сущностей в игре.
    /// Реализует IRenderable для единой архитектуры отрисовки.
    /// Отрисовка происходит через World.Draw() с переданной экранной позицией.
    /// Использует сеточные координаты (GridPosition + Layer) как Tile.
    /// </summary>
    public abstract class Entity : IDisposable, IRenderable
    {
        // === ID и имя ===
        private static ulong _nextId = 1;
        public ulong Id { get; }
        public string Name { get; private set; }
        public string Tag { get;private set; } = string.Empty;

        public int CurrentLayer { get; private set; } = 0;

        // === IRenderable ===
        private float _drawOrder = 0.5f;
        public float DrawOrder
        {
            get => _drawOrder;
            set
            {
                if (Math.Abs(_drawOrder - value) > float.Epsilon)
                {
                    _drawOrder = value;
                    DrawOrderChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private bool _visible = true;
        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    VisibleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler DrawOrderChanged;
        public event EventHandler VisibleChanged;

        // === Для обратной совместимости ===
        public virtual bool IsActive { get; set; } = true;
        public bool IsVisible => Visible;
        public virtual bool ShouldBeRemoved { get; protected set; } = false;

        // === Ссылка на мир ===
        public World World { get; internal set; }

        // === ИГРОВЫЕ КООРДИНАТЫ (2D сетка + высота, как у Tile) ===
        private Point _gridPosition;
        public Point GridPosition
        {
            get => _gridPosition;
            set
            {
                if (_gridPosition != value)
                {
                    Point oldPos = _gridPosition;
                    _gridPosition = value;
                    OnPositionChanged?.Invoke(this, oldPos, value);
                    UpdateDrawOrder();
                }
            }
        }

        /// <summary>
        /// Синхронизирует слой персонажа с тайлом под ногами.
        /// Вызывается после завершения движения.
        /// </summary>
        public void SyncLayerWithGround()
        {
            if (World?.CurrentRoom?.GetTileGrid() == null) return;

            // Ищем верхний проходимый тайл на текущей позиции
            for (int z = GameSetting.WorldChunkHeight - 1; z >= 0; z--)
            {
                var tile = World.CurrentRoom.GetTileGrid().GetTile(GridPosition.X, GridPosition.Y, z);
                if (tile != null && tile.IsWalkable)
                {
                    CurrentLayer = z;
                    return;
                }
            }
            CurrentLayer = 0; // Откат к земле, если ничего не найдено
        }

        private int _layer = 0;
        public int Layer
        {
            get => _layer;
            set
            {
                if (_layer != value)
                {
                    int oldLayer = _layer;
                    _layer = Math.Max(0, value);
                    OnLayerChanged?.Invoke(this, oldLayer, value);
                    UpdateDrawOrder();
                }
            }
        }

        // === Размеры сущности (в клетках сетки) ===
        public const float BaseWidth  = 1f;
        public const float BaseHeight = 1f;

        public float Width { get; private set; } = BaseWidth;

        public float Height { get; private set; } = BaseHeight;

        public float Depth => _depth;

        private float _depth;

        // === Размеры спрайта (для отрисовки, в пикселях) ===
        protected const int BaseSpriteWidth = 64;
        protected const int BaseSpriteHeight = 128;

        protected int SpriteWidth { get; private set; } = BaseSpriteWidth;
        protected int SpriteHeight { get; private set; } = BaseSpriteHeight;

        // === Состояние ===
        public bool IsPersistent { get; private set; } = true;
        public bool IsDisposed { get; private set; }

        // === Иерархия ===
        public Entity Parent { get; private set; }
        public List<Entity> Children { get; } = new();

        // === События ===
        public event Action<Entity> OnDisposed;
        public event Action<Entity, Point, Point> OnPositionChanged;
        public event Action<Entity, int, int> OnLayerChanged;
        public event Action<Entity> OnAddedToWorld;
        public event Action<Entity> OnRemovedFromWorld;

        // === Конструктор ===
        protected Entity(float depth, string name = null)
        {
            Id = _nextId++;
            Name = name ?? $"Entity_{Id}";
            _depth = depth;

            // Автоматически вычисляем глубину отрисовки на основе позиции
            UpdateDrawOrder();

            OnPositionChanged += (entity, oldPos, newPos) => UpdateDrawOrder();
            OnLayerChanged += (entity, oldLayer, newLayer) => UpdateDrawOrder();
        }

        // === IRenderable.Draw — ОСНОВНОЙ МЕТОД (с позицией) ===

        /// <summary>
        /// Основной метод отрисовки (из интерфейса IRenderable).
        /// Вызывается из World.Draw() с переданной экранной позицией.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch для отрисовки</param>
        /// <param name="screenPosition">Экранная позиция (центр верхней грани, вычислен через камеру)</param>
        /// <param name="drawDepth">Глубина для сортировки SpriteBatch (0.0–0.9999)</param>
        /// <param name="zoom">Множитель зума для масштабирования спрайта</param>
        public virtual void Draw(
            SpriteBatch spriteBatch,
            Vector2 screenPosition,
            float drawDepth,
            float zoom = 1.0f)
        {
            if (!Visible || spriteBatch == null)
                return;

            // Вызываем абстрактный метод для конкретной реализации
            DrawEntity(spriteBatch, screenPosition, drawDepth, zoom);
        }

        /// <summary>
        /// Дополнительный метод отрисовки (для совместимости).
        /// Используется когда позиция вычисляется внутри объекта (отладка).
        /// </summary>
        public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!Visible || spriteBatch == null)
                return;

            // Вычисляем позицию через камеру (если есть мир)
            if (World != null && World.Camera != null)
            {
                Vector2 screenPos = World.Camera.WorldToScreen(
                    new Vector3(GridPosition.X, GridPosition.Y, Layer)
                );

                float drawDepth = (GridPosition.X + GridPosition.Y) * 100 + Layer * 50;
                drawDepth = MathHelper.Clamp(drawDepth / 10000f, 0f, 0.9999f);

                Draw(spriteBatch, screenPos, drawDepth, 1.0f);
            }
        }

        // === АБСТРАКТНЫЙ МЕТОД для наследников (ОБЯЗАТЕЛЬНО к реализации) ===

        /// <summary>
        /// Абстрактный метод отрисовки сущности.
        /// Все наследники ОБЯЗАНЫ реализовать этот метод.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch для отрисовки</param>
        /// <param name="screenPosition">Экранная позиция (центр верхней грани тайла, на котором стоит сущность)</param>
        /// <param name="drawDepth">Глубина для сортировки SpriteBatch (0.0–0.9999)</param>
        /// <param name="zoom">Множитель зума для масштабирования спрайта</param>
        protected abstract void DrawEntity(
            SpriteBatch spriteBatch,
            Vector2 screenPosition,
            float drawDepth,
            float zoom);

        // === IUpdattGameEntity.Update — переопределяется в наследниках ===
        public abstract void Update(GameTime gameTime);

        // === ОБЯЗАТЕЛЬНАЯ инициализация (переопределяется в наследниках) ===
        public abstract void Initialize();

        // === Базовые методы ===

        // === Публичные методы для изменения свойств ===
        public void SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;
            Name = name;
        }

        public void SetTag(string tag) => Tag = tag ?? string.Empty;

        public void SetGridPosition(Point position) => GridPosition = position;
        public void SetGridPosition(int x, int y) => SetGridPosition(new Point(x, y));

        public void SetLayer(int layer) => Layer = layer;

        public void SetSize(float width, float height = 1f)
        {
            if (width <= 0 || height <= 0)
                throw new ArgumentException("Dimensions must be positive");
            Width = width;
            Height = height;
        }

        public void SetSize(float size) => SetSize(size, size);

        public void SetSpriteSize(int width, int height)
        {
            SpriteWidth = width;
            SpriteHeight = height;
        }

        public void SetActive(bool active)
        {
            if (IsActive != active)
                IsActive = active;
        }

        public void SetPersistent(bool persistent) => IsPersistent = persistent;

        // === Иерархия ===
        public void AddChild(Entity child)
        {
            if (child == null || child == this || child.IsDisposed)
                return;

            if (child.Parent != null)
                child.Parent.RemoveChild(child);

            Children.Add(child);
            child.Parent = this;
            child.GridPosition = new Point(
                child.GridPosition.X - GridPosition.X,
                child.GridPosition.Y - GridPosition.Y
            );
        }

        public void RemoveChild(Entity child)
        {
            if (child != null && Children.Remove(child))
            {
                child.Parent = null;
                child.GridPosition = new Point(
                    child.GridPosition.X + GridPosition.X,
                    child.GridPosition.Y + GridPosition.Y
                );
            }
        }

        // === Удаление ===
        public virtual void MarkForRemoval()
        {
            ShouldBeRemoved = true;
            IsActive = false;
            Visible = false;
        }

        // === Коллизии ===
        public virtual RectangleF GetCollisionBounds()
        {
            return new RectangleF(
                (int)(GridPosition.X - BaseWidth / 2),
                (int)(GridPosition.Y - Depth / 2),
                (int)BaseWidth,
                (int)Depth
            );
        }

        public virtual bool CheckCollision(Entity other)
        {
            if (other == null) return false;
            RectangleF bounds1 = GetCollisionBounds();
            RectangleF bounds2 = other.GetCollisionBounds();
            return bounds1.Intersects(bounds2);
        }

        // === Утилиты ===
        public Point GetWorldGridPosition()
        {
            if (Parent == null) return GridPosition;
            return new Point(
                Parent.GetWorldGridPosition().X + GridPosition.X,
                Parent.GetWorldGridPosition().Y + GridPosition.Y
            );
        }

        public int GetWorldLayer()
        {
            if (Parent == null) return Layer;
            return Parent.GetWorldLayer() + Layer;
        }

        public Vector3 GetWorldPosition3D()
        {
            Point worldPos = GetWorldGridPosition();
            return new Vector3(worldPos.X, worldPos.Y, GetWorldLayer());
        }

        public RectangleF GetBounds()
        {
            Point worldPos = GetWorldGridPosition();
            return new RectangleF(
                (int)(worldPos.X - BaseWidth / 2),
                (int)(worldPos.Y - Depth / 2),
                (int)BaseWidth,
                (int)Depth
            );
        }

        // === Перемещение ===
        public void Move(Point delta) => SetGridPosition(
            new Point(GridPosition.X + delta.X, GridPosition.Y + delta.Y)
        );

        public void Move(int deltaX, int deltaY) => Move(new Point(deltaX, deltaY));

        public void MoveToLayer(int targetLayer, int speed = 1)
        {
            if (speed <= 0)
                throw new ArgumentException("Speed must be positive");
            int newLayer = (int)MathHelper.Lerp(Layer, targetLayer, speed);
            SetLayer(newLayer);
        }

        // === Обновление порядка отрисовки ===
        protected virtual void UpdateDrawOrder()
        {
            Point worldPos = GetWorldGridPosition();
            int worldLayer = GetWorldLayer();

            // Формула: (X + Y) * 100 + Z * 50 — согласовано с Tile
            DrawOrder = (worldPos.X + worldPos.Y) * 100 + worldLayer * 50;
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ ОТРИСОВКИ ===

        /// <summary>
        /// Вычисляет смещение для центрирования спрайта сущности.
        /// В отличие от тайлов, сущности центрируются по НИЖНЕЙ грани (где "ноги").
        /// </summary>
        /// <param name="spriteWidth">Ширина спрайта в пикселях</param>
        /// <param name="spriteHeight">Высота спрайта в пикселях</param>
        /// <returns>Смещение для центрирования относительно позиции "ног"</returns>
        protected Vector2 CalculateEntityOffset(int spriteWidth, int spriteHeight)
        {
            // ✅ ПРАВИЛЬНОЕ ЦЕНТРИРОВАНИЕ ДЛЯ СУЩНОСТЕЙ:
            // screenPosition — это позиция "ног" сущности на земле (центр верхней грани тайла)
            // Спрайт должен быть центрирован по X и поднят по Y на свою высоту

            return new Vector2(
                -spriteWidth / 2f,      // Центр спрайта по X
                -spriteHeight           // Поднять спрайт на полную высоту (ноги на земле)
            );
        }

        /// <summary>
        /// Вычисляет смещение для центрирования спрайта сущности с учётом зума.
        /// </summary>
        protected Vector2 CalculateEntityOffset(int spriteWidth, int spriteHeight, float zoom)
        {
            Vector2 offset = CalculateEntityOffset(spriteWidth, spriteHeight);
            return offset * zoom;
        }

        // === Очистка ===
        public virtual void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;
            IsActive = false;
            Visible = false;

            // Очищаем детей
            foreach (Entity child in Children.ToArray())
                child.Dispose();
            Children.Clear();

            // Отписываемся от родителя
            if (Parent != null)
            {
                Parent.RemoveChild(this);
                Parent = null;
            }

            // Отписываемся от событий World
            OnAddedToWorld = null;
            OnRemovedFromWorld = null;

            // Уведомляем подписчиков
            OnDisposed?.Invoke(this);

            // Очищаем все обработчики событий
            OnDisposed = null;
            OnPositionChanged = null;
            OnLayerChanged = null;
            DrawOrderChanged = null;
            VisibleChanged = null;
        }

        // === Для отладки ===
        public override string ToString()
        {
            Point worldPos = GetWorldGridPosition();
            return $"{GetType().Name} '{Name}' ({worldPos.X}, {worldPos.Y}, {GetWorldLayer()}) " +
                   $"[Visible: {Visible}, DrawOrder: {DrawOrder:F3}]";
        }
    }
}