namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Направления в логической сетке (не экранные!).
    /// В изометрии 2:1:
    /// - Top = уменьшение Y (вверх по сетке)
    /// - Bottom = увеличение Y (вниз по сетке)
    /// - Right = увеличение X (вправо по сетке)
    /// - Left = уменьшение X (влево по сетке)
    /// </summary>
    public enum GridDirection
    {
        Top,    // Вверх (Y - 1)
        Right,  // Вправо (X + 1)
        Bottom, // Вниз (Y + 1)
        Left    // Влево (X - 1)
    }
}
