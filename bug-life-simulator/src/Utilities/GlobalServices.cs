using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TalesFromTheUnderbrush.src.GameLogic;

namespace TalesFromTheUnderbrush
{
    // Контейнер для глобальных сервисов
    public static class GlobalServices
    {
        public static AssetManager Assets { get; private set; }
        public static RenderManager Renderer { get; private set; }
        //public static InputManager Input { get; private set; }

        public static void Initialize(ContentManager content, GraphicsDevice graphicsDevice)
        {
            Assets = new AssetManager(content);
            Renderer = new RenderManager(graphicsDevice);
           // Input = new InputManager();
        }

        public static void Dispose()
        {
            Renderer?.Dispose();
            Assets?.Dispose();
        }
    }
}
