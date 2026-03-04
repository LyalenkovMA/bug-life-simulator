using System;
using Point = Microsoft.Xna.Framework.Point;


namespace TalesFromTheUnderbrush.src
{
    /// <summary>
    /// Пользовательский RectangleF для устранения конфликта между библиотеками.
    /// Использует float координаты для точности в изометрической проекции.
    /// </summary>
    public struct GameRectangleF : IEquatable<GameRectangleF>
    {
        // === ПОЛЯ ===
        public float X;
        public float Y;
        public float Width;
        public float Height;

        // === КОНСТРУКТОРЫ ===
        public GameRectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public GameRectangleF(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        // === ВЫЧИСЛЯЕМЫЕ СВОЙСТВА ===
        public float Left => X;
        public float Right => X + Width;
        public float Top => Y;
        public float Bottom => Y + Height;

        public float CenterX => X + Width / 2f;
        public float CenterY => Y + Height / 2f;

        public bool IsEmpty => Width <= 0 || Height <= 0;

        // === СТАТИЧЕСКИЕ МЕТОДЫ ===
        public static GameRectangleF Empty => new GameRectangleF(0, 0, 0, 0);

        // === ПРОВЕРКА ПЕРЕСЕЧЕНИЙ ===
        public bool Intersects(GameRectangleF other)
        {
            return Left < other.Right &&
                   Right > other.Left &&
                   Top < other.Bottom &&
                   Bottom > other.Top;
        }

        public static bool Intersects(GameRectangleF a, GameRectangleF b)
        {
            return a.Intersects(b);
        }

        // === ПРОВЕРКА СОДЕРЖИМОГО ===
        public bool Contains(float x, float y)
        {
            return x >= Left && x < Right && y >= Top && y < Bottom;
        }

        public bool Contains(GameRectangleF other)
        {
            return other.Left >= Left &&
                   other.Right <= Right &&
                   other.Top >= Top &&
                   other.Bottom <= Bottom;
        }

        public bool Contains(Point point)
        {
            return Contains(point.X, point.Y);
        }

        // === СМЕЩЕНИЕ ===
        public void Offset(float offsetX, float offsetY)
        {
            X += offsetX;
            Y += offsetY;
        }

        public void Offset(Point offset)
        {
            X += offset.X;
            Y += offset.Y;
        }

        // === ИНФЛЯЦИЯ (расширение/сжатие) ===
        public void Inflate(float width, float height)
        {
            X -= width / 2f;
            Y -= height / 2f;
            Width += width;
            Height += height;
        }

        // === СРАВНЕНИЕ ===
        public override bool Equals(object obj)
        {
            return obj is GameRectangleF other && Equals(other);
        }

        public bool Equals(GameRectangleF other)
        {
            return X == other.X && Y == other.Y &&
                   Width == other.Width && Height == other.Height;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }

        public static bool operator ==(GameRectangleF left, GameRectangleF right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GameRectangleF left, GameRectangleF right)
        {
            return !left.Equals(right);
        }

        // === СТРОКОВОЕ ПРЕДСТАВЛЕНИЕ ===
        public override string ToString()
        {
            return $"RectangleF [X={X:F1}, Y={Y:F1}, Width={Width:F1}, Height={Height:F1}]";
        }

        // === ПРЕОБРАЗОВАНИЯ ===
        public Microsoft.Xna.Framework.Rectangle ToRectangle()
        {
            return new Microsoft.Xna.Framework.Rectangle(
                (int)X, (int)Y, (int)Width, (int)Height);
        }

        public static GameRectangleF FromRectangle(Microsoft.Xna.Framework.Rectangle rect)
        {
            return new GameRectangleF(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }
}