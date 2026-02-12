using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using TalesFromTheUnderbrush.src.Graphics.Tiles;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Тайл травы. Чистый data-holder без доступа к AssetManager или камере.
    /// Текстура и параметры передаются извне при создании.
    /// </summary>
    public class GrassTile : Tile
    {
        // === ДАННЫЕ ДЛЯ РЕНДЕРИНГА ===
        private readonly Texture2D _texture;
        private readonly Rectangle _sourceRect;

        // === ДАННЫЕ ДЛЯ ГЕЙМПЛЕЯ ===
        /// <summary>
        /// Бонус к скорости перемещения (1.0 = базовый, 1.15 = +15%)
        /// </summary>
        public readonly float MovementBonus;

        // === КОНСТРУКТОР ===
        /// <summary>
        /// Создаёт тайл травы с заданными параметрами.
        /// </summary>
        /// <param name="gridPosition">Позиция в сетке (X, Y)</param>
        /// <param name="layer">Уровень высоты по Z</param>
        /// <param name="texture">Текстура/атлас из AssetManager</param>
        /// <param name="sourceRect">Область в атласе для этого тайла</param>
        /// <param name="movementBonus">Множитель скорости перемещения</param>
        /// <param name="isWalkable">Проходимость клетки</param>
        public GrassTile(Point gridPosition, int layer, Texture2D texture, Rectangle sourceRect, float movementBonus = 1.0f, bool isWalkable = true)
            : base(gridPosition, layer)
        {
            _texture = texture ?? throw new ArgumentNullException(nameof(texture));
            _sourceRect = sourceRect;
            MovementBonus = movementBonus;
            Type = TileType.Grass;
            SetWalkableInternal(isWalkable);
            SetTintColorInternal(Color.White);
        }

        // === ОТРИСОВКА (временно 2D для отладки) ===
        /// <summary>
        /// Временная 2D-отрисовка для первого прототипа.
        /// Позже заменится на изометрическую через внешний рендерер.
        /// </summary>
        protected override void DrawTile(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!Visible || spriteBatch == null || _texture == null)
                return;

            // Используем текущее свойство WorldPosition (прямоугольная сетка)
            // ПОЗЖЕ: заменим на изометрию через камеру в TileGrid.Draw()
            Vector2 drawPosition = new Vector2(
                WorldPosition.X - GameSetting.WorldTileHalfWidth,
                WorldPosition.Y - GameSetting.WorldTileHeight / 2f
            );

            // Нормализуем глубину для SpriteBatch (0.0–1.0)
            float drawDepth = (GridPosition.X + GridPosition.Y) * 100 + Layer * 50;
            drawDepth = MathHelper.Clamp(drawDepth / 10000f, 0f, 0.9999f);

            spriteBatch.Draw(
                texture: _texture,
                position: drawPosition,
                sourceRectangle: _sourceRect,
                color: TintColor,
                rotation: Rotation,
                origin: Vector2.Zero,
                scale: 1.0f,
                effects: SpriteEffects.None,
                layerDepth: drawDepth
            );
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===
        /// <summary>
        /// Может ли персонаж взобраться на этот тайл?
        /// </summary>
        public bool CanClimbFrom(int fromLayer) => Math.Abs(Layer - fromLayer) <= 1;
    }
}