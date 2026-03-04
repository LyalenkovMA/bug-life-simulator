using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TalesFromTheUnderbrush.src.Core.Entities;
using TalesFromTheUnderbrush.src.Graphics;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using RectangleF = Microsoft.Xna.Framework.Rectangle;

namespace TalesFromTheUnderbrush.src.Core.Entities
{
    /// <summary>
    /// Базовый класс для сущностей со спрайтовой графикой.
    /// Наследуется от Entity и реализует отрисовку через World.Draw().
    /// Позиция вычисляется внешним рендерером (World), не зависит от камеры.
    /// </summary>
    public abstract class SpriteEntity : Entity
    {
        // === ДАННЫЕ ДЛЯ РЕНДЕРИНГА ===
        private readonly Texture2D _texture;
        private Rectangle _sourceRect;
        private Color _tintColor = Color.White;
        private float _rotation = 0f;
        private SpriteEffects _spriteEffects = SpriteEffects.None;

        // === АНИМАЦИЯ ===
        private bool _isAnimated;
        private Rectangle[] _animationFrames;
        private float[] _frameDurations;
        private int _currentFrame;
        private float _frameTimer;
        private bool _loopAnimation = true;

        // === КОНСТРУКТОР ===
        /// <summary>
        /// Создаёт спрайтовую сущность.
        /// </summary>
        /// <param name="name">Имя сущности</param>
        /// <param name="texture">Текстура/атлас</param>
        /// <param name="sourceRect">Область в атласе</param>
        /// <param name="gridPosition">Позиция в сетке (X, Y)</param>
        /// <param name="layer">Высота по Z (слой)</param>
        /// <param name="depth">Глубина сущности (для коллизий)</param>
        protected SpriteEntity(
            string name,
            Texture2D texture,
            Rectangle sourceRect,
            Point gridPosition,
            int layer = 0,
            float depth = 1f)
            : base(depth, name)
        {
            _texture = texture ?? throw new ArgumentNullException(nameof(texture));
            _sourceRect = sourceRect;

            // Устанавливаем размеры спрайта для центрирования
            SetSpriteSize(sourceRect.Width, sourceRect.Height);

            // Устанавливаем позицию
            SetGridPosition(gridPosition);
            SetLayer(layer);

            // Автоматический расчёт глубины отрисовки
            UpdateDrawOrder();
        }

        // === СВОЙСТВА ===
        public Texture2D Texture => _texture;

        public Rectangle SourceRect
        {
            get => _sourceRect;
            set => _sourceRect = value;
        }

        public Color TintColor
        {
            get => _tintColor;
            set => _tintColor = value;
        }

        public float Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }

        public SpriteEffects SpriteEffects
        {
            get => _spriteEffects;
            set => _spriteEffects = value;
        }

        public bool IsAnimated => _isAnimated;

        public bool IsAnimationFinished => !_loopAnimation && _currentFrame == _animationFrames?.Length - 1;

        // === ОТРИСОВКА (реализация IRenderable) ===

        /// <summary>
        /// Реализация отрисовки сущности с переданной экранной позицией.
        /// Вызывается из World.Draw() после вычисления позиции через камеру.
        /// </summary>
        protected override void DrawEntity(
            SpriteBatch spriteBatch,
            Vector2 screenPosition,
            float drawDepth,
            float zoom)
        {
            if (!Visible || !IsActive || spriteBatch == null || _texture == null)
                return;

            // 1. Вычисляем смещение для центрирования (по "ногам" сущности)
            Vector2 drawOffset = CalculateEntityOffset(SpriteWidth, SpriteHeight, zoom);

            // 2. Вычисляем позицию отрисовки
            Vector2 drawPosition = screenPosition + drawOffset;

            // 3. Отрисовка спрайта
            spriteBatch.Draw(
                texture: _texture,
                position: drawPosition,
                sourceRectangle: _sourceRect,
                color: _tintColor,
                rotation: _rotation,
                origin: Vector2.Zero,
                scale: zoom,
                effects: _spriteEffects,
                layerDepth: drawDepth
            );
        }

        // === АНИМАЦИЯ ===
        /// <summary>
        /// Устанавливает анимацию спрайта.
        /// </summary>
        public void SetAnimation(Rectangle[] frames, float[] frameDurations, bool loop = true)
        {
            if (frames == null || frames.Length == 0)
            {
                _isAnimated = false;
                return;
            }

            _animationFrames = frames;
            _frameDurations = frameDurations ?? new float[frames.Length];
            _currentFrame = 0;
            _frameTimer = 0f;
            _isAnimated = true;
            _loopAnimation = loop;
            _sourceRect = frames[0];

            // Обновляем размеры спрайта для первого кадра
            SetSpriteSize(frames[0].Width, frames[0].Height);
        }

        /// <summary>
        /// Обновляет анимацию (вызывается в Update).
        /// </summary>
        protected void UpdateAnimation(GameTime gameTime)
        {
            if (!_isAnimated || _animationFrames == null)
                return;

            _frameTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_frameTimer >= _frameDurations[_currentFrame])
            {
                _frameTimer = 0f;
                _currentFrame++;

                // Проверка конца анимации
                if (_currentFrame >= _animationFrames.Length)
                {
                    if (_loopAnimation)
                    {
                        _currentFrame = 0;
                    }
                    else
                    {
                        _currentFrame = _animationFrames.Length - 1;
                    }
                }

                _sourceRect = _animationFrames[_currentFrame];
            }
        }

        /// <summary>
        /// Сбросить анимацию к первому кадру.
        /// </summary>
        public void ResetAnimation()
        {
            _currentFrame = 0;
            _frameTimer = 0f;
            if (_animationFrames != null && _animationFrames.Length > 0)
            {
                _sourceRect = _animationFrames[0];
            }
        }

        // === ОБНОВЛЕНИЕ ===
        public override void Update(GameTime gameTime)
        {
            // Обновляем анимацию
            if (_isAnimated)
            {
                UpdateAnimation(gameTime);
            }

            // Обновляем глубину отрисовки при изменении позиции
            UpdateDrawOrder();
        }

        // === ИНИЦИАЛИЗАЦИЯ (обязательная реализация) ===
        public override void Initialize()
        {
            // Базовая инициализация
            IsActive = true;
            Visible = true;

            // Наследники могут переопределить для дополнительной инициализации
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===
        /// <summary>
        /// Устанавливает направление спрайта (для отражения по горизонтали).
        /// </summary>
        public void SetFacingDirection(float directionX)
        {
            if (directionX < 0)
                _spriteEffects = SpriteEffects.FlipHorizontally;
            else if (directionX > 0)
                _spriteEffects = SpriteEffects.None;
        }

        /// <summary>
        /// Устанавливает цвет тонирования спрайта.
        /// </summary>
        public void SetTintColor(Color color)
        {
            _tintColor = color;
        }

        /// <summary>
        /// Устанавливает поворот спрайта.
        /// </summary>
        public void SetRotation(float rotation)
        {
            _rotation = rotation;
        }

        // === КОЛЛИЗИИ ===
        public override GameRectangleF GetCollisionBounds()
        {
            Point worldPos = GetWorldGridPosition();
            return new GameRectangleF(
                (int)(worldPos.X - BaseWidth / 2),
                (int)(worldPos.Y - Depth / 2),
                (int)BaseWidth,
                (int)Depth
            );
        }

        // === ОТЛАДОЧНАЯ ИНФОРМАЦИЯ ===
        public override string ToString()
        {
            Point worldPos = GetWorldGridPosition();
            return $"SpriteEntity '{Name}' at ({worldPos.X}, {worldPos.Y}, {GetWorldLayer()}) " +
                   $"[Visible: {Visible}, DrawOrder: {DrawOrder:F3}, Animated: {_isAnimated}]";
        }
    }
}