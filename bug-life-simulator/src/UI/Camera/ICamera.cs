using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Drawing;
using TalesFromTheUnderbrush.src.Graphics;
using MonoGameIDrawable = Microsoft.Xna.Framework.IDrawable;
// === ЯВНЫЕ АЛИАСЫ — КРИТИЧНО! ===
using MonoGameIUpdateable = Microsoft.Xna.Framework.IUpdateable;

namespace TalesFromTheUnderbrush.src.UI.Camera
{
    /// <summary>
    /// Интерфейс камеры для изометрического мира.
    /// Наследуется от MonoGame IUpdateable и IDrawable для интеграции с игровым циклом.
    /// </summary>
    public interface ICamera : IUpdattGameEntity, IRenderable
    {
        // === СВОЙСТВА (только чтение) ===
        new Vector3 Position { get; }
        Vector3 Target { get; }
        Matrix ViewMatrix { get; }
        Matrix ProjectionMatrix { get; }
        Matrix ViewProjectionMatrix { get; }
        int ViewportWidth { get; }
        int ViewportHeight { get; }

        // === ГРАНИЦЫ КАМЕРЫ ===
        new RectangleF Bounds { get; }

        // === МЕТОДЫ УПРАВЛЕНИЯ ===
        void Move(Vector3 offset);
        void LookAt(Vector3 target);
        void SetViewport(int width, int height);

        // === ПРОЕКЦИЯ (критично для изометрии) ===
        Vector2 WorldToScreen(Vector3 worldPosition);
        Vector3 ScreenToWorld(Vector2 screenPosition, float worldZ = 0);

        // === УТИЛИТЫ ===
        bool IsInView(Vector3 worldPosition);
        Matrix GetViewMatrix();
    }
}