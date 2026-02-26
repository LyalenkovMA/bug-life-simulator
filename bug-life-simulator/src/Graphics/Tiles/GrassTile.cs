using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TalesFromTheUnderbrush.src.Graphics.Tiles;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Тайл травы. Хранит текстуру и источник из атласа.
    /// Не зависит от AssetManager или камеры — данные передаются при создании.
    /// </summary>
    public class GrassTile : Tile
    {
        // === ДАННЫЕ ДЛЯ РЕНДЕРИНГА (хранятся локально) ===
        private readonly Texture2D _texture;      // Текстура/атлас
        private readonly Rectangle _sourceRect;   // Область в атласе (кусочек)

        // === КОНСТРУКТОР (обязательные параметры) ===
        /// <summary>
        /// Создаёт тайл травы с полной информацией для отрисовки.
        /// </summary>
        /// <param name="gridPosition">Позиция в сетке (X, Y)</param>
        /// <param name="layer">Уровень высоты по Z</param>
        /// <param name="texture">Текстура/атлас (загружена через AssetManager)</param>
        /// <param name="sourceRect">Область в атласе для этого тайла (кусочек 256×128 → 128×64)</param>
        /// <param name="isWalkable">Проходимость клетки</param>
        public GrassTile(
            Point gridPosition,
            int layer,
            Texture2D texture,
            Rectangle sourceRect,
            bool isWalkable = true)
            : base(gridPosition, layer)
        {
            _texture = texture ?? throw new System.ArgumentNullException(nameof(texture));
            _sourceRect = sourceRect; // Сохраняем ЛОКАЛЬНО, не через базовое свойство

            Type = TileType.Grass;
            SetWalkableInternal(isWalkable);
            SetTintColorInternal(Color.White);
            SetSolidInternal(true);
        }

        // === РЕАЛИЗАЦИЯ АБСТРАКТНЫХ МЕТОДОВ БАЗОВОГО КЛАССА ===
        protected override Texture2D GetTexture() => _texture;
        protected override Rectangle GetSourceRectangle() => _sourceRect;

        // === ОТРИСОВКА (старый метод для совместимости) ===
        protected override void DrawTile(SpriteBatch spriteBatch, GameTime gameTime)
        {
            // Временно: используем WorldPosition для отладки
            // Позже: World.Draw() будет вызывать DrawAtPosition() напрямую
            Vector2 tempPosition = new Vector2(
                WorldPosition.X - GameSetting.WorldTileHalfWidth,
                WorldPosition.Y - GameSetting.WorldTileHeight / 2f
            );
            float tempDepth = (GridPosition.X + GridPosition.Y) * 100 + Layer * 50;
            tempDepth = MathHelper.Clamp(tempDepth / 10000f, 0f, 0.9999f);

            DrawAtPosition(spriteBatch, tempPosition, tempDepth);
        }       
    }
}