using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace TalesFromTheUnderbrush
{
    public class RenderManager : IDisposable
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly SpriteBatch _spriteBatch;

        // Очередь отрисовки
        private readonly List<RenderCommand> _renderQueue = new();

        public RenderManager(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            _spriteBatch = new SpriteBatch(graphicsDevice);
        }

        // Структура команды отрисовки
        private struct RenderCommand
        {
            public Texture2D Texture;
            public Vector2 Position;
            public Rectangle? SourceRectangle;
            public Color Color;
            public float Rotation;
            public Vector2 Origin;
            public Vector2 Scale;
            public SpriteEffects Effects;
            public float LayerDepth;
        }

        // Публичный метод для планирования отрисовки
        public void ScheduleDraw(Texture2D texture, Vector2 position,
                                 Rectangle? sourceRectangle = null, Color? color = null,
                                 float rotation = 0f, Vector2? origin = null,
                                 Vector2? scale = null, SpriteEffects effects = SpriteEffects.None,
                                 float layerDepth = 0f)
        {
            _renderQueue.Add(new RenderCommand
            {
                Texture = texture,
                Position = position,
                SourceRectangle = sourceRectangle,
                Color = color ?? Color.White,
                Rotation = rotation,
                Origin = origin ?? Vector2.Zero,
                Scale = scale ?? Vector2.One,
                Effects = effects,
                LayerDepth = layerDepth
            });
        }

        // Сортировка и отрисовка всей очереди
        public void Flush()
        {
            if (_renderQueue.Count == 0)
                return;

            // Сортируем по глубине (от заднего плана к переднему)
            _renderQueue.Sort((a, b) => a.LayerDepth.CompareTo(b.LayerDepth));

            _spriteBatch.Begin(
                sortMode: SpriteSortMode.Deferred,
                blendState: BlendState.AlphaBlend,
                samplerState: SamplerState.PointClamp,
                depthStencilState: DepthStencilState.None,
                rasterizerState: RasterizerState.CullNone);

            foreach (var command in _renderQueue)
            {
                _spriteBatch.Draw(
                    command.Texture,
                    command.Position,
                    command.SourceRectangle,
                    command.Color,
                    command.Rotation,
                    command.Origin,
                    command.Scale,
                    command.Effects,
                    command.LayerDepth);
            }

            _spriteBatch.End();
            _renderQueue.Clear();
        }

        // Очистка очереди без отрисовки
        public void ClearQueue()
        {
            _renderQueue.Clear();
        }

        public void Dispose()
        {
            _spriteBatch?.Dispose();
        }
    }
}
