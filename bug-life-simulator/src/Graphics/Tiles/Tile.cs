using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Базовый класс тайла - ОТДЕЛЬНО от Entity
    /// Оптимизирован для статичных объектов мира
    /// Теперь реализует IDrawable для унифицированной отрисовки
    /// </summary>
    public abstract class Tile : DrawableBase, IDisposable, IRequiresSpriteBatch
    {
        // === ID и тип ===
        public ulong Id { get; }
        public TileType Type { get; protected set; }
        public string TileSet { get; protected set; } = "default";

        // === Позиция в гриде ===
        public Point GridPosition { get; private set; }
        public int Layer { get; private set; }

        // Вычисляемые мировые координаты
        public Vector2 WorldPosition => new Vector2(
            GridPosition.X * TileSize.Width + TileSize.Width / 2,
            GridPosition.Y * TileSize.Height + TileSize.Height / 2
        );

        public float WorldHeight => Layer * TileSize.Height;

        // === Графические данные ===
        public Rectangle SourceRect { get; private set; }
        public Color TintColor { get; private set; } = Color.White;
        public float Rotation { get; private set; }

        // === Свойства для геймплея ===
        public bool IsWalkable { get; private set; } = true;
        public bool IsTransparent { get; private set; } = false;
        public bool IsSolid { get; private set; } = true;
        public bool IsBuildable { get; private set; } = true;
        public bool IsDestructible { get; private set; } = false;

        public int Durability { get; private set; } = 100;
        public int MaxDurability { get; private set; } = 100;

        // === Свойства из Tiled ===
        public Dictionary<string, string> Properties { get; } = new();

        // === Анимация ===
        public bool IsAnimated => _animationFrames != null && _animationFrames.Count > 1;
        protected SpriteBatch CurrentSpriteBatch { get; private set; }
        private List<Rectangle> _animationFrames;
        private List<float> _animationDurations;
        private int _currentFrame;
        private float _frameTimer;

        // === Соседи (для оптимизации рендеринга) ===
        public Tile[] Neighbors { get; private set; } = new Tile[6]; // 4 стороны + верх + низ

        // === События ===
        public event Action<Tile> OnDestroyed;
        public event Action<Tile> OnDamaged;
        public event Action<Tile> OnChanged;

        // === Статические размеры (одинаковые для всех тайлов) ===
        public static Size TileSize { get; set; } = new Size(64, 64);

        // === РЕАЛИЗАЦИЯ IDRAWABLE ===
        public float DrawOrder
        {
            get => _drawOrder;
            set
            {
                if (_drawOrder != value)
                {
                    _drawOrder = value;
                    DrawOrderChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public void SetSpriteBatch(SpriteBatch spriteBatch)
        {
            CurrentSpriteBatch = spriteBatch;
        }

        // Переопределяем Draw для использования SpriteBatch
        public override void Draw(GameTime gameTime)
        {
            if (!Visible || CurrentSpriteBatch == null) return;
            Draw(gameTime, CurrentSpriteBatch);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!Visible || spriteBatch == null) return;
            DrawTile(spriteBatch, gameTime);
        }

        protected abstract void DrawTile(SpriteBatch spriteBatch, GameTime gameTime);

        // === Публичные методы для изменения свойств ===

        /// <summary>
        /// Установить цвет тайла
        /// </summary>
        public void SetTintColor(Color color)
        {
            TintColor = color;
            OnChanged?.Invoke(this);
        }

        /// <summary>
        /// Установить поворот тайла
        /// </summary>
        public void SetRotation(float rotation)
        {
            Rotation = rotation;
            OnChanged?.Invoke(this);
        }

        /// <summary>
        /// Установить прямоугольник источника текстуры
        /// </summary>
        public void SetSourceRect(Rectangle rect)
        {
            SourceRect = rect;
            OnChanged?.Invoke(this);
        }

        /// <summary>
        /// Установить свойство проходимости
        /// </summary>
        public void SetWalkable(bool walkable)
        {
            if (IsWalkable != walkable)
            {
                IsWalkable = walkable;
                OnChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Установить свойство прозрачности
        /// </summary>
        public void SetTransparent(bool transparent)
        {
            if (IsTransparent != transparent)
            {
                IsTransparent = transparent;
                OnChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Установить свойство твердости
        /// </summary>
        public void SetSolid(bool solid)
        {
            if (IsSolid != solid)
            {
                IsSolid = solid;
                OnChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Установить возможность строительства
        /// </summary>
        public void SetBuildable(bool buildable)
        {
            if (IsBuildable != buildable)
            {
                IsBuildable = buildable;
                OnChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Установить разрушаемость
        /// </summary>
        public void SetDestructible(bool destructible)
        {
            if (IsDestructible != destructible)
            {
                IsDestructible = destructible;
                OnChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Установить прочность
        /// </summary>
        public void SetDurability(int durability)
        {
            if (durability < 0) durability = 0;

            if (Durability != durability)
            {
                Durability = durability;
                OnChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Установить максимальную прочность
        /// </summary>
        public void SetMaxDurability(int maxDurability)
        {
            if (maxDurability < 1) maxDurability = 1;

            if (MaxDurability != maxDurability)
            {
                MaxDurability = maxDurability;

                // Корректируем текущую прочность, если нужно
                if (Durability > MaxDurability)
                {
                    Durability = MaxDurability;
                }

                OnChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Восстановить прочность до максимума
        /// </summary>
        public void RestoreDurability()
        {
            if (Durability != MaxDurability)
            {
                Durability = MaxDurability;
                OnChanged?.Invoke(this);
            }
        }

        /// <summary>
        /// Нанести урон тайлу
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (damage <= 0 || !IsDestructible) return;

            int newDurability = Durability - damage;
            SetDurability(newDurability);

            if (newDurability <= 0)
            {
                Destroy();
            }
            else
            {
                OnDamaged?.Invoke(this);
            }
        }

        // Protected методы для наследников
        protected void SetTintColorInternal(Color color)
        {
            TintColor = color;
            OnChanged?.Invoke(this);
        }

        protected void SetWalkableInternal(bool walkable)
        {
            IsWalkable = walkable;
            OnChanged?.Invoke(this);
        }

        protected void SetTransparentInternal(bool transparent)
        {
            IsTransparent = transparent;
            OnChanged?.Invoke(this);
        }

        protected void SetSolidInternal(bool solid)
        {
            IsSolid = solid;
            OnChanged?.Invoke(this);
        }

        protected void SetBuildableInternal(bool buildable)
        {
            IsBuildable = buildable;
            OnChanged?.Invoke(this);
        }

        protected void SetDestructibleInternal(bool destructible)
        {
            IsDestructible = destructible;
            OnChanged?.Invoke(this);
        }

        private float _drawOrder = 0;

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
        private bool _visible = true;

        public event EventHandler DrawOrderChanged;
        public event EventHandler VisibleChanged;

        // === Конструктор ===
        protected Tile(Point gridPosition, int layer)
        {
            Id = TileIdGenerator.Next();
            GridPosition = gridPosition;
            Layer = layer;

            // Автоматически вычисляем DrawOrder на основе положения
            DrawOrder = layer * 1000 + gridPosition.Y * 10 + gridPosition.X;
        }

        // === Публичные методы ===

        public void SetPosition(Point gridPosition, int layer)
        {
            GridPosition = gridPosition;
            Layer = layer;
            OnChanged?.Invoke(this);

            // Обновляем DrawOrder при изменении позиции
            DrawOrder = layer * 1000 + gridPosition.Y * 10 + gridPosition.X;
        }

        public void SetProperties(Dictionary<string, string> properties)
        {
            Properties.Clear();
            foreach (var kvp in properties)
            {
                Properties[kvp.Key] = kvp.Value;
            }

            // Автоматически применяем свойства
            ApplyProperties();
        }

        public void SetAnimation(List<Rectangle> frames, List<float> frameDurations)
        {
            if (frames == null || frames.Count == 0)
                return;

            _animationFrames = frames;
            _animationDurations = frameDurations ?? Enumerable.Repeat(0.1f, frames.Count).ToList();

            _currentFrame = 0;
            _frameTimer = 0;
            SourceRect = frames[0];
        }

        public void SetNeighbors(Tile north, Tile south, Tile east, Tile west, Tile above, Tile below)
        {
            Neighbors[0] = north;
            Neighbors[1] = south;
            Neighbors[2] = east;
            Neighbors[3] = west;
            Neighbors[4] = above;
            Neighbors[5] = below;
        }

        // === Обновление ===

        public virtual void Update(GameTime gameTime)
        {
            UpdateAnimation(gameTime);
        }

        private void UpdateAnimation(GameTime gameTime)
        {
            if (!IsAnimated)
                return;

            _frameTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            while (_frameTimer >= _animationDurations[_currentFrame])
            {
                _frameTimer -= _animationDurations[_currentFrame];
                _currentFrame = (_currentFrame + 1) % _animationFrames.Count;
                SourceRect = _animationFrames[_currentFrame];
            }
        }



        // === Взаимодействие ===

        public virtual bool ApplyDamage(int damage)
        {
            if (!IsDestructible)
                return false;

            Durability -= damage;
            OnDamaged?.Invoke(this);

            if (Durability <= 0)
            {
                Destroy();
                return true;
            }

            return false;
        }

        public virtual void Destroy()
        {
            Visible = false;
            OnDestroyed?.Invoke(this);
            Dispose();
        }

        public virtual bool CanPlaceOnTop()
        {
            return IsSolid && IsWalkable && !IsAnimated;
        }

        // === Утилиты ===

        public string GetProperty(string key, string defaultValue = "")
        {
            return Properties.TryGetValue(key, out string value) ? value : defaultValue;
        }

        public T GetProperty<T>(string key, T defaultValue = default)
        {
            if (Properties.TryGetValue(key, out string value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch { }
            }
            return defaultValue;
        }

        public bool HasProperty(string key)
        {
            return Properties.ContainsKey(key);
        }

        protected virtual void ApplyProperties()
        {
            // Применяем стандартные свойства из Tiled через сеттеры
            SetWalkable(GetProperty("walkable", "true") == "true");
            SetTransparent(GetProperty("transparent", "false") == "true");
            SetSolid(GetProperty("solid", "true") == "true");
            SetBuildable(GetProperty("buildable", "true") == "true");
            SetDestructible(GetProperty("destructible", "false") == "true");

            if (int.TryParse(GetProperty("durability", ""), out int durability))
            {
                SetMaxDurability(durability);
                RestoreDurability();
            }

            // Применяем видимость
            Visible = GetProperty("visible", "true") == "true";

            // Применяем цвет, если указан
            string colorHex = GetProperty("color", "");
            if (!string.IsNullOrEmpty(colorHex) && colorHex.StartsWith("#"))
            {
                try
                {
                    System.Drawing.Color color = System.Drawing.ColorTranslator.FromHtml(colorHex);
                    SetTintColor(new Color(color.R, color.G, color.B, color.A));
                }
                catch { }
            }
        }

        // === Очистка ===

        public virtual void Dispose()
        {
            OnDestroyed = null;
            OnDamaged = null;
            OnChanged = null;

            DrawOrderChanged = null;
            VisibleChanged = null;

            _animationFrames?.Clear();
            _animationDurations?.Clear();
            Properties.Clear();

            for (int i = 0; i < Neighbors.Length; i++)
                Neighbors[i] = null;

            CurrentSpriteBatch = null;
        }

        public override string ToString()
        {
            return $"{Type} at ({GridPosition.X}, {GridPosition.Y}, {Layer}) [Visible: {Visible}, DrawOrder: {DrawOrder}]";
        }
    }

    // === Вспомогательные типы ===

    public enum TileType
    {
        Empty,
        Grass,
        Stone,
        Water,
        Sand,
        Dirt,
        Wood,
        Brick,
        Glass,
        Metal,
        Crystal,
        Lava,
        Ice,
        Snow,
        Fungus,
        Special
    }

    public struct Size
    {
        public int Width;
        public int Height;

        public Size(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}