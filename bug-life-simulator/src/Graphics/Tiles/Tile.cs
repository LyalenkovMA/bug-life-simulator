using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using TalesFromTheUnderbrush.src.Graphics;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Базовый класс тайла — ОТДЕЛЬНО от Entity.
    /// Оптимизирован для статичных объектов мира.
    /// Хранит ТОЛЬКО игровые координаты (GridPosition + Layer).
    /// Отрисовка происходит через World.Draw() с переданной экранной позицией.
    /// </summary>
    public abstract class Tile : IDisposable, IRenderable
    {
        // === ID и тип ===
        private static ulong _nextId = 1;
        public ulong Id { get; }
        public TileType Type => _type;
        

        // === ИГРОВЫЕ КООРДИНАТЫ (только логика, не рендеринг!) ===
        public Point GridPosition { get; private set; }
        public int Layer { get; private set; }

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
        
        protected void SetType(TileType type) => _type = type;
        
        private TileType _type;
        private List<Rectangle> _animationFrames;
        private List<float> _animationDurations;
        private int _currentFrame;
        private float _frameTimer;

        // === Соседи (для оптимизации рендеринга) ===
        public Tile[] Neighbors { get; private set; } = new Tile[6];

        // === События ===
        public event Action<Tile> OnDestroyed;
        public event Action<Tile> OnDamaged;
        public event Action<Tile> OnChanged;

        protected virtual void OnChangedEvent()
        {
            OnChanged?.Invoke(this);
        }

        // === IRenderable ===
        private float _drawOrder;
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

        // === Статические размеры (из GameSetting) ===
        public static Size TileSize => new Size(
            (int)GameSetting.WorldTileWidth,    // 128
            (int)GameSetting.WorldTileHeight    // 64
        );

        public virtual Vector2 TopFaceSize => new Vector2(
            GameSetting.WorldTileWidth,
            GameSetting.WorldTileHeight
        );

        public virtual float Height => 32f;

        // === Конструктор ===
        protected Tile(Point gridPosition, int layer)
        {
            Id = _nextId++;
            GridPosition = gridPosition;
            Layer = layer;
            _visible = true;

            // Автоматически вычисляем DrawOrder на основе положения
            UpdateDrawOrder();
        }

        // === IRenderable.Draw — основной метод отрисовки ===

        /// <summary>
        /// Дополнительный метод отрисовки с SpriteBatch.
        /// </summary>
        public virtual void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!Visible || spriteBatch == null)
                return;

            // Вычисляем позицию через GlobalSettings
            Vector2 screenPos = GlobalSettings.GetIsometricGridPosition(
                new Vector2(GridPosition.X, GridPosition.Y),
                Layer
            );

            float drawDepth = GlobalSettings.GetIsometricDrawDepth(
                new Vector2(GridPosition.X, GridPosition.Y),
                Layer
            );

            Draw(spriteBatch, screenPos, drawDepth, 1.0f);
        }

        /// <summary>
        /// Отрисовка тайла с ПЕРЕДАННОЙ экранной позицией.
        /// Вызывается из World.Draw() после вычисления позиции через камеру.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch для отрисовки</param>
        /// <param name="screenPosition">Экранная позиция (центр верхней грани, вычислен через камеру)</param>
        /// <param name="drawDepth">Глубина для сортировки (0.0–0.9999)</param>
        /// <param name="zoom">Множитель зума (для масштабирования спрайта)</param>
        public virtual void Draw(SpriteBatch spriteBatch, Vector2 screenPosition,
                                 float drawDepth, float zoom = 1.0f)
        {
            if (!Visible || spriteBatch == null)
                return;

            // 1. Получаем источник текстуры (динамический размер из атласа)
            Rectangle sourceRect = GetSourceRectangle();

            // 2. === АВТОМАТИЧЕСКОЕ ЦЕНТРИРОВАНИЕ ===
            // screenPosition — это центр ВЕРХНЕЙ ГРАНИ тайла (ромб 128×64)
            // Спрайт в атласе может включать боковые грани, поэтому нужно сместить его
            Vector2 drawOffset = new Vector2(-sourceRect.Width / 2f,  // Центр спрайта по X
                -sourceRect.Height + GameSetting.WorldTileHalfHeight);// Низ верхней грани по Y

            // 3. Вычисляем позицию отрисовки
            Vector2 drawPosition = screenPosition + drawOffset;

            // 4. Отрисовка с ЗУМОМ
            spriteBatch.Draw(
                texture: GetTexture(),
                position: drawPosition,
                sourceRectangle: sourceRect,
                color: TintColor,
                rotation: Rotation,
                origin: Vector2.Zero,
                scale: zoom,              // ← ПРИМЕНЯЕМ ЗУМ!
                effects: SpriteEffects.None,
                layerDepth: drawDepth);
        }

       
        /// <summary>
        /// Получить текстуру тайла (реализуется в наследниках).
        /// </summary>
        protected abstract Texture2D GetTexture();

        /// <summary>
        /// Получить источник текстуры (может быть переопределён в наследниках).
        /// </summary>
        protected virtual Rectangle GetSourceRectangle() => SourceRect;

        // === Публичные методы для изменения свойств ===

        public void SetTintColor(Color color)
        {
            TintColor = color;
            OnChanged?.Invoke(this);
        }

        public void SetRotation(float rotation)
        {
            Rotation = rotation;
            OnChanged?.Invoke(this);
        }

        public void SetSourceRect(Rectangle rect)
        {
            SourceRect = rect;
            OnChanged?.Invoke(this);
        }

        public void SetWalkable(bool walkable)
        {
            if (IsWalkable != walkable)
            {
                IsWalkable = walkable;
                OnChanged?.Invoke(this);
            }
        }

        public void SetTransparent(bool transparent)
        {
            if (IsTransparent != transparent)
            {
                IsTransparent = transparent;
                OnChanged?.Invoke(this);
            }
        }

        public void SetSolid(bool solid)
        {
            if (IsSolid != solid)
            {
                IsSolid = solid;
                OnChanged?.Invoke(this);
            }
        }

        public void SetBuildable(bool buildable)
        {
            if (IsBuildable != buildable)
            {
                IsBuildable = buildable;
                OnChanged?.Invoke(this);
            }
        }

        public void SetDestructible(bool destructible)
        {
            if (IsDestructible != destructible)
            {
                IsDestructible = destructible;
                OnChanged?.Invoke(this);
            }
        }

        public void SetDurability(int durability)
        {
            if (durability < 0) durability = 0;
            if (Durability != durability)
            {
                Durability = durability;
                OnChanged?.Invoke(this);
            }
        }

        public void SetMaxDurability(int maxDurability)
        {
            if (maxDurability < 1) 
                maxDurability = 1;

            if (MaxDurability != maxDurability)
            {
                MaxDurability = maxDurability;

                if (Durability > MaxDurability)
                    Durability = MaxDurability;

                OnChanged?.Invoke(this);
            }
        }

        public void RestoreDurability()
        {
            if (Durability != MaxDurability)
            {
                Durability = MaxDurability;
                OnChanged?.Invoke(this);
            }
        }

        public void TakeDamage(int damage)
        {
            if (damage <= 0 || !IsDestructible) return;

            int newDurability = Durability - damage;
            SetDurability(newDurability);

            if (newDurability <= 0)
                Destroy();
            else
                OnDamaged?.Invoke(this);
        }

        // === Protected методы для наследников ===
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

        // === Позиция (публичный метод) ===
        public void SetPosition(Point gridPosition, int layer)
        {
            GridPosition = gridPosition;
            Layer = layer;
            OnChanged?.Invoke(this);
            UpdateDrawOrder();
        }

        // === Свойства из Tiled ===
        public void SetProperties(Dictionary<string, string> properties)
        {
            Properties.Clear();
            foreach (var kvp in properties)
                Properties[kvp.Key] = kvp.Value;
            ApplyProperties();
        }

        // === Анимация ===
        public void SetAnimation(List<Rectangle> frames, List<float> frameDurations)
        {
            if (frames == null || frames.Count == 0) return;

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
            if (!IsAnimated) return;

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
            if (!IsDestructible) return false;

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
            SetWalkable(GetProperty("walkable", "true") == "true");
            SetTransparent(GetProperty("transparent", "false") == "false");
            SetSolid(GetProperty("solid", "true") == "true");
            SetBuildable(GetProperty("buildable", "true") == "true");
            SetDestructible(GetProperty("destructible", "false") == "false");

            if (int.TryParse(GetProperty("durability", ""), out int durability))
            {
                SetMaxDurability(durability);
                RestoreDurability();
            }

            Visible = GetProperty("visible", "true") == "true";

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

        // === Обновление порядка отрисовки ===
        protected virtual void UpdateDrawOrder()
        {
            // Формула: (X + Y) * 100 + Z * 50
            DrawOrder = (GridPosition.X + GridPosition.Y) * 100 + Layer * 50;
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
        Empty, Grass, Stone, Water, Sand, Dirt,
        Wood, Brick, Glass, Metal, Crystal, Lava,
        Ice, Snow, Fungus, Special
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