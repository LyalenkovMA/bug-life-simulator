using Microsoft.Xna.Framework;
using System.Drawing;
using TalesFromTheUnderbrush.src.Graphics;
using IDrawable = Microsoft.Xna.Framework.IDrawable;

namespace TalesFromTheUnderbrush.src.UI.Camera
{
    public interface ICamera : IUpdatable, IDrawable
    {
        // ТОЛЬКО getters - никаких сеттеров!
        Vector3 Position { get; }
        Vector3 Target { get; }
        Matrix ViewMatrix { get; }
        Matrix ProjectionMatrix { get; }
        Matrix ViewProjectionMatrix { get; }

        new RectangleF Bounds { get; } // "new" потому что уже есть в IDrawable

        // Контролируемые операции
        void Move(Vector3 offset);
        void LookAt(Vector3 target);
        void SetViewport(int width, int height);

        // Утилиты
        Vector2 WorldToScreen(Vector3 worldPosition);
        Vector3 ScreenToWorld(Vector2 screenPosition, float worldZ = 0);
    }
}
