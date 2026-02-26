using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TalesFromTheUnderbrush.src.Core.Entities;
using TalesFromTheUnderbrush.src.Graphics;

namespace TalesFromTheUnderbrush.src.Core.Entities
{
    /// <summary>
    /// Базовый класс для сущностей со спрайтовой графикой.
    /// Не зависит от камеры — позиция вычисляется внешним рендерером (World).
    /// </summary>
    public abstract class SpriteEntity : StaticEntity
    {
        // === ДАННЫЕ ДЛЯ РЕНДЕРИНГА ===
        private readonly Texture2D _texture;
        private Rectangle _sourceRect;
        private Vector2 _origin;
        private SpriteEffects _spriteEffects;

        // === АНИМАЦИЯ ===
        private bool _isAnimated;
        private Rectangle[] _animationFrames;
        private float[] _frameDurations;
        private int _currentFrame;
        private float _frameTimer;

        // === КОНСТРУКТОР ===
        /// <summary>
        /// Создаёт спрайтовую сущность.
        /// </summary>
        /// <param name="name">Имя сущности</param>
        /// <param name="texture">Текстура/атлас</param>
        /// <param name="sourceRect">Область в атласе</param>
        /// <param name="position">Позиция в мире (X, Y)</param>
        /// <param name="height">Высота по Z</param>
        protected SpriteEntity(
            string name,
            Texture2D texture,
            Rectangle sourceRect,
            Vector2 position,
            float height = 0f)
            : base(name)
        {
            _texture = texture ?? throw new ArgumentNullException(nameof(texture));
            _sourceRect = sourceRect;
            _origin = new Vector2(sourceRect.Width / 2f, sourceRect.Height); // Нижняя центральная точка
            _spriteEffects = SpriteEffects.None;

            SetPosition(position);
            SetHeight(height);

            // Автоматический расчёт глубины отрисовки
            UpdateDrawDepth();
        }

        // === СВОЙСТВА ===
        public Texture2D Texture => _texture;

        public Rectangle SourceRect
        {
            get => _sourceRect;
            set => _sourceRect = value;
        }

        public Vector2 Origin
        {
            get => _origin;
            set => _origin = value;
        }

        public SpriteEffects SpriteEffects
        {
            get => _spriteEffects;
            set => _spriteEffects = value;
        }

        public bool IsAnimated => _isAnimated;

        // === ОТРИСОВКА (основной метод IRenderable) ===
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (!Visible || !IsActive || spriteBatch == null || _texture == null)
                return;

            // Вычисляем глубину для сортировки (как в тайлах)
            float drawDepth = CalculateDrawDepth();
            drawDepth = MathHelper.Clamp(drawDepth, 0f, 0.9999f);

            // Вычисляем экранную позицию (будет переопределено в World.Draw для изометрии)
            Vector2 screenPosition = GetScreenPosition();

            // Отрисовка спрайта
            spriteBatch.Draw(
                texture: _texture,
                position: screenPosition,
                sourceRectangle: _sourceRect,
                color: Color.White,
                rotation: 0f,
                origin: _origin,
                scale: 1.0f,
                effects: _spriteEffects,
                layerDepth: drawDepth
            );
        }

        // === БАЗОВАЯ ОТРИСОВКА (без SpriteBatch) ===
        public override void Draw(GameTime gameTime)
        {
            // Пустая реализация — спрайты требуют SpriteBatch
        }

        // === ВЫЧИСЛЕНИЕ ЭКРАННОЙ ПОЗИЦИИ ===
        /// <summary>
        /// Вычисляет экранную позицию для отрисовки.
        /// Переопределяется в World.Draw для изометрической проекции.
        /// </summary>
        protected virtual Vector2 GetScreenPosition()
        {
            // Временно: 2D-позиция для отладки
            // В изометрии: World вычислит через камеру.WorldToScreen()
            return new Vector2(Position.X, Position.Y);
        }

        // === ВЫЧИСЛЕНИЕ ГЛУБИНЫ ===
        protected virtual float CalculateDrawDepth()
        {
            // Формула как у тайлов: (X + Y) * 100 + Z * 50
            return (Position.X + Position.Y) * 100 + Height * 50;
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
            _sourceRect = frames[0];
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
                _currentFrame = (_currentFrame + 1) % _animationFrames.Length;
                _sourceRect = _animationFrames[_currentFrame];
            }
        }

        // === ОБНОВЛЕНИЕ ===
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Обновляем анимацию
            if (_isAnimated)
                UpdateAnimation(gameTime);

            // Обновляем глубину отрисовки при изменении позиции
            UpdateDrawDepth();
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
            // Можно добавить поле _tintColor и использовать в Draw()
        }

        // === КОЛЛИЗИИ ===
        public override CollisionBounds GetCollisionBounds()
        {
            //Простая AABB коллизия на основе позиции и размера спрайта
            return new CollisionBounds();
        }

        // === СЕРИАЛИЗАЦИЯ ===
        public override void Load(PersistenceData data)
        {
            //base.Load(data);

            //// Загрузка специфичных данных спрайта
            //if (data.Properties.TryGetValue("SourceRect", out string rectStr))
            //{
            //    // Парсинг Rectangle из строки
            //}
        }

        // === ОТЛАДОЧНАЯ ИНФОРМАЦИЯ ===
        public override string ToString()
        {
            return $"SpriteEntity '{Name}' at ({Position.X:F1}, {Position.Y:F1}, {Height:F1}) " +
                   $"[Visible: {Visible}, DrawOrder: {DrawOrder:F3}, Animated: {_isAnimated}]";
        }
    }
}