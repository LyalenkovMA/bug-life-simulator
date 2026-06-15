namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Тип геометрической формы тайла.
    /// </summary>
    public enum TileGeometryType
    {
        FullBlock,  // Полный блок (стандартный пол/стена)
        HalfBlock,  // Полублок (ступенька, низкий бордюр)
        Slope,      // Скос (плавный подъем/спуск)
        Stairs      // Лестница (явный переход между уровнями)
    }
}
