using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TalesFromTheUnderbrush.src.Graphics.Tiles;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Тайл травы. Хранит текстуру и источник из атласа.
    /// НЕ переопределяет отрисовку — использует базовый Tile.DrawAtPosition().
    /// Реализует только получение текстуры для базового класса.
    /// </summary>
    public class GrassTile : Tile
    {
        // === ДАННЫЕ ДЛЯ РЕНДЕРИНГА (хранятся локально) ===
        private readonly Texture2D _texture;      // Атлас текстур
        private readonly Rectangle _sourceRect;   // Область в атласе

        // === ИГРОВАЯ ЛОГИКА ===

        /// <summary>
        /// Тип биома/зоны.
        /// </summary>
        public BiomeType Biome { get; private set; }

        /// <summary>
        /// Бонус к перемещению (1.0 = нормально, 0.5 = медленно, 1.5 = быстро).
        /// </summary>
        public float MovementModifier { get; private set; } = 1.0f;

        /// <summary>
        /// Дополнительная защита для персонажа на этом тайле.
        /// </summary>
        public float DefenseBonus { get; private set; } = 0f;

        // === КОНСТРУКТОР ===

        /// <summary>
        /// Создаёт тайл травы с полной информацией для отрисовки.
        /// </summary>
        public GrassTile(
            Point gridPosition,
            int layer,
            Texture2D texture,
            Rectangle sourceRect,
            BiomeType biome = BiomeType.Grassland,
            float movementModifier = 1.0f,
            bool isWalkable = true)
            : base(gridPosition, layer)
        {
            _texture = texture ?? throw new System.ArgumentNullException(nameof(texture));
            _sourceRect = sourceRect;

            // === Устанавливаем тип через базовый класс ===
            SetType(TileType.Grass);

            Biome = biome;
            MovementModifier = movementModifier;

            // === Инициализация свойств через protected методы ===
            SetWalkableInternal(isWalkable);
            SetTintColorInternal(Color.White);
            SetSolidInternal(true);

            // === Применяем свойства биома ===
            ApplyBiomeProperties();
        }

        // === РЕАЛИЗАЦИЯ АБСТРАКТНЫХ МЕТОДОВ БАЗОВОГО КЛАССА ===

        /// <summary>
        /// Получить текстуру тайла (обязательная реализация для Tile).
        /// </summary>
        protected override Texture2D GetTexture() => _texture;

        /// <summary>
        /// Получить источник текстуры (переопределение для Tile).
        /// </summary>
        protected override Rectangle GetSourceRectangle() => _sourceRect;

        // === ИГРОВАЯ ЛОГИКА ===

        /// <summary>
        /// Применить свойства биома (вызывается из конструктора).
        /// </summary>
        private void ApplyBiomeProperties()
        {
            switch (Biome)
            {
                case BiomeType.Grassland:
                    MovementModifier = 1.0f;
                    DefenseBonus = 0f;
                    break;

                case BiomeType.Forest:
                    MovementModifier = 0.7f;  // Медленнее в лесу
                    DefenseBonus = 0.2f;       // Бонус защиты в лесу
                    break;

                case BiomeType.Swamp:
                    MovementModifier = 0.5f;  // Очень медленно в болоте
                    DefenseBonus = 0f;
                    break;

                case BiomeType.Road:
                    MovementModifier = 1.5f;  // Быстрее по дороге
                    DefenseBonus = 0f;
                    break;

                case BiomeType.Mountain:
                    MovementModifier = 0.3f;  // Очень медленно в горах
                    DefenseBonus = 0.3f;       // Бонус защиты в горах
                    break;

                case BiomeType.Water:
                    MovementModifier = 0.0f;  // Нельзя ходить по воде
                    SetWalkableInternal(false);
                    break;

                default:
                    MovementModifier = 1.0f;
                    DefenseBonus = 0f;
                    break;
            }
        }

        /// <summary>
        /// Получить стоимость перемещения на этот тайл.
        /// </summary>
        public float GetMovementCost()
        {
            if (!IsWalkable) return float.MaxValue;
            return 1.0f / MovementModifier;
        }

        /// <summary>
        /// Применить бонус к персонажу (вызывается когда персонаж стоит на тайле).
        /// </summary>
        public void ApplyBonusToCharacter(ref float movementSpeed, ref float defense)
        {
            if (!IsWalkable) return;

            movementSpeed *= MovementModifier;
            defense += DefenseBonus;
        }

        /// <summary>
        /// Сменить биом тайла (динамическое изменение мира).
        /// </summary>
        public void ChangeBiome(BiomeType newBiome)
        {
            if (Biome != newBiome)
            {
                Biome = newBiome;
                ApplyBiomeProperties();
                OnChangedEvent();
            }
        }

        // === ОТРИСОВКА ===
        // ❌ НЕ переопределяем DrawAtPosition() — используем базовый из Tile.cs
        // ✅ Базовый класс уже реализует:
        //    - DrawAtPosition(spriteBatch, screenPos, drawDepth, zoom)
        //    - Автоматическое центрирование через sourceRect
        //    - Применение зума к scale

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

        public override string ToString()
        {
            return $"{Type} [{Biome}] at ({GridPosition.X}, {GridPosition.Y}, {Layer})  " +
                   $"[Move: {MovementModifier:F2}, Defense: {DefenseBonus:F2}]";
        }
    }

    // === ТИПЫ БИОМОВ ===

    public enum BiomeType
    {
        Grassland,    // Обычная земля
        Forest,       // Лес
        Swamp,        // Болото
        Road,         // Дорога
        Mountain,     // Горы
        Water,        // Вода
        Desert,       // Пустыня
        Snow,         // Снег
        Lava,         // Лава
        Special       // Особые зоны
    }
}