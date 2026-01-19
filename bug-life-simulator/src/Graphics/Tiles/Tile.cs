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
    /// </summary>
    public abstract class Tile : IDisposable
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
        public Rectangle SourceRect { get; protected set; }
        public Color Color { get;private set; } = Color.White;
        public float Rotation { get;private set; }

        // === Свойства для геймплея ===
        public bool IsWalkable { get; protected set; } = true;
        public bool IsTransparent { get; protected set; } = false;
        public bool IsSolid { get; protected set; } = true;
        public bool IsBuildable { get; protected set; } = true;
        public bool IsDestructible { get; protected set; } = false;

        public int Durability { get; protected set; } = 100;
        public int MaxDurability { get; protected set; } = 100;

        // === Свойства из Tiled ===
        public Dictionary<string, string> Properties { get; } = new();

        // === Анимация ===
        public bool IsAnimated => _animationFrames != null && _animationFrames.Count > 1;
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

        // === Конструктор ===
        protected Tile(Point gridPosition, int layer)
        {
            Id = TileIdGenerator.Next();
            GridPosition = gridPosition;
            Layer = layer;
        }

        // === Публичные методы ===

        public void SetPosition(Point gridPosition, int layer)
        {
            GridPosition = gridPosition;
            Layer = layer;
            OnChanged?.Invoke(this);
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
            // Применяем стандартные свойства из Tiled
            IsWalkable = GetProperty("walkable", "true") == "true";
            IsTransparent = GetProperty("transparent", "false") == "true";
            IsSolid = GetProperty("solid", "true") == "true";
            IsBuildable = GetProperty("buildable", "true") == "true";
            IsDestructible = GetProperty("destructible", "false") == "true";

            if (int.TryParse(GetProperty("durability", ""), out int durability))
            {
                Durability = durability;
                MaxDurability = durability;
            }
        }

        // === Очистка ===

        public virtual void Dispose()
        {
            OnDestroyed = null;
            OnDamaged = null;
            OnChanged = null;

            _animationFrames?.Clear();
            _animationDurations?.Clear();
            Properties.Clear();

            for (int i = 0; i < Neighbors.Length; i++)
                Neighbors[i] = null;
        }

        public override string ToString()
        {
            return $"{Type} at ({GridPosition.X}, {GridPosition.Y}, {Layer})";
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
