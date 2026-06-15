using System;
using Microsoft.Xna.Framework;

namespace TalesFromTheUnderbrush.src.GameLogic
{
    /// <summary>
    /// Реализация 2D шума Перлина для процедурной генерации.
    /// </summary>
    public static class PerlinNoise
    {
        private static readonly int[] _permutation = new int[512];

        // Статический конструктор для инициализации таблицы перестановок
        static PerlinNoise()
        {
            // Для воспроизводимости можно использовать фиксированный Seed
            // Но здесь используем простую инициализацию.
            // Для полноценного мира лучше передавать Seed в методы.
            Random random = new Random(42); // Фиксированный сид для стабильного мира
            int[] p = new int[256];
            for (int i = 0; i < 256; i++) p[i] = i;

            // Перемешивание
            for (int i = 255; i > 0; i--)
            {
                int n = random.Next(i + 1);
                int temp = p[i]; p[i] = p[n]; p[n] = temp;
            }

            for (int i = 0; i < 512; i++)
                _permutation[i] = p[i & 255];
        }

        public static float Get(float x, float y, int seed = 0)
        {
            // Добавляем сид к координатам, если нужен уникальный мир
            // В данной простой реализации сид учитывается косвенно через таблицу

            int X = (int)Math.Floor(x) & 255, Y = (int)Math.Floor(y) & 255;

            x -= (int)Math.Floor(x);
            y -= (int)Math.Floor(y);

            float u = Fade(x), v = Fade(y);

            int a = _permutation[X] + Y, aa = _permutation[a], ab = _permutation[a + 1];
            int b = _permutation[X + 1] + Y, ba = _permutation[b], bb = _permutation[b + 1];

            return Lerp(v, Lerp(u, Grad(_permutation[aa], x, y), Grad(_permutation[ba], x - 1, y)),
                               Lerp(u, Grad(_permutation[ab], x, y - 1), Grad(_permutation[bb], x - 1, y - 1)));
        }

        private static float Fade(float t) { return t * t * t * (t * (t * 6 - 15) + 10); }
        private static float Lerp(float t, float a, float b) { return a + t * (b - a); }
        private static float Grad(int hash, float x, float y)
        {
            int h = hash & 15;
            float u = h < 8 ? x : y;
            float v = h < 4 ? y : h == 12 || h == 14 ? x : 0;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
        }
    }
}