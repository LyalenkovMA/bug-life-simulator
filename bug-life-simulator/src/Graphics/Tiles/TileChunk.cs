using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D9;
using System;
using System.Collections.Generic;
using System.Linq;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TalesFromTheUnderbrush.src.Graphics.Tiles
{
    /// <summary>
    /// Чанк тайлов для оптимизации рендеринга
    /// </summary>
    public class TileChunk : IDisposable, IDrawable
    {
        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    VisibleChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        public float DrawOrder
        {
            get => _drawOrder;
            set
            {
                if (_drawOrder != value)
                {
                    _drawOrder = value;
                    DrawOrderChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool IsVisible
        {
            get => Visible;
            set => Visible = value;
        }

        public event EventHandler DrawOrderChanged;
        public event EventHandler VisibleChanged;
        public event EventHandler DrawDepthChanged;

        public Point Position { get; private set; }
        public int Size { get; private set; }
        public bool IsDirty { get; set; } = true;
        private SpriteBatch _spriteBatch;
        private bool _visible = true;
        private float _drawOrder = 0;

        private readonly List<Tile> _tiles = new();

        // Если их нет, добавьте в начало класса:
        private int _width, _height, _depth;
        // Для хранения SpriteBatch (нужен для интерфейсного метода Draw)
        private SpriteBatch _currentSpriteBatch;


        public TileChunk(Point position, int size)
        {
            Position = position;
            Size = size;
            _width = size;
            _height = size;
            _depth = 1; // Или значение из конфигурации

            // Инициализируем DrawOrder на основе позиции
            DrawOrder = position.Y * 1000 + position.X;
        }

        public int Width => _width;
        public int Height => _height;
        public int Depth => _depth;

        // === Метод для установки SpriteBatch ===
        public void SetSpriteBatch(SpriteBatch spriteBatch)
        {
            _currentSpriteBatch = spriteBatch;
        }

        public float DrawDepth => throw new NotImplementedException();

        /// <summary>
        /// Получить все тайлы в области (прямоугольник в мировых координатах)
        /// </summary>
        public List<Tile> GetTilesInArea(Rectangle area)
        {
            var result = new List<Tile>();

            // Границы чанка в мировых координатах
            int chunkWorldX = Position.X * Size * Tile.TileSize.Width;
            int chunkWorldY = Position.Y * Size * Tile.TileSize.Height;
            int chunkWorldWidth = Size * Tile.TileSize.Width;
            int chunkWorldHeight = Size * Tile.TileSize.Height;

            Rectangle chunkRect = new Rectangle(chunkWorldX, chunkWorldY, chunkWorldWidth, chunkWorldHeight);

            // Если область не пересекается с чанком - возвращаем пустой список
            if (!area.Intersects(chunkRect))
                return result;

            // Перебираем все тайлы чанка
            foreach (Tile tile in _tiles)
            {
                // Проверяем, попадает ли тайл в область
                Rectangle tileRect = new Rectangle(
                    (int)tile.WorldPosition.X - Tile.TileSize.Width / 2,
                    (int)tile.WorldPosition.Y - Tile.TileSize.Height / 2,
                    Tile.TileSize.Width,
                    Tile.TileSize.Height
                );

                if (area.Intersects(tileRect))
                {
                    result.Add(tile);
                }
            }

            return result;
        }

        /// <summary>
        /// Получить все тайлы чанка
        /// </summary>
        public IEnumerable<Tile> GetAllTiles()
        {
            //for (int x = 0; x < Width; x++)
            //{
            //    for (int y = 0; y < Height; y++)
            //    {
            //        for (int z = 0; z < Depth; z++)
            //        {
            //            var tile = GetTile(x, y, z);
            //            if (tile != null)
            //                yield return tile;
            //        }
            //    }
            //}
            return _tiles;

        }

        public Tile GetTile(int x, int y, int z)
        {
            // Ваша существующая реализация
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Depth)
                return null;

            int globalX = Position.X * Size + x;
            int globalY = Position.Y * Size + y;

            return _tiles.FirstOrDefault(t =>
                t.GridPosition.X == globalX &&
                t.GridPosition.Y == globalY &&
                t.Layer == z);
        }

        public void AddTile(Tile tile)
        {
            if (tile != null && !_tiles.Contains(tile))
            {
                _tiles.Add(tile);
                IsDirty = true;
            }
        }

        public void RemoveTile(int x, int y, int z)
        {
            Tile tile = GetTile(x, y, z);
            if (tile != null && _tiles.Remove(tile))
            {
                tile.Dispose();
                IsDirty = true;
            }
        }

        public void Clear()
        {
            foreach (Tile tile in _tiles)
            {
                tile.Dispose();
            }
            _tiles.Clear();
            IsDirty = true;
        }

        public List<Tile> GetTiles()
        {
            return new List<Tile>(_tiles);
        }

        public void DrawChunk(SpriteBatch spriteBatch)
        {
            _spriteBatch = spriteBatch;
            // существующая логика отрисовки
            if (!Visible) return;

            var sortedTiles = _tiles.OrderBy(t => t.Layer).ThenBy(t => t.GridPosition.Y);
            foreach (var tile in sortedTiles)
            {
                // отрисовка тайлов
            }
        }

        public void Draw(GameTime gameTime)
        {
            // Вызываем существующий метод отрисовки
            // если у нас есть SpriteBatch
            if (_currentSpriteBatch != null && Visible)
            {
                Draw(_currentSpriteBatch);  // Вызываем старый метод
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible || spriteBatch == null)
                return;

            // Сортируем тайлы для правильного порядка отрисовки
            var sortedTiles = _tiles
                .Where(t => t != null && t.Visible)
                .OrderBy(t => t.Layer)
                .ThenBy(t => t.GridPosition.Y)
                .ThenBy(t => t.GridPosition.X);

            foreach (Tile tile in sortedTiles)
            {
                // Временная отрисовка - простой прямоугольник
                // ЗАМЕНИТЕ ЭТО на реальную отрисовку текстур
                Rectangle tileRect = new Rectangle(
                    (int)tile.WorldPosition.X - Tile.TileSize.Width / 2,
                    (int)tile.WorldPosition.Y - Tile.TileSize.Height / 2,
                    Tile.TileSize.Width,
                    Tile.TileSize.Height
                );

                // Создаем временную текстуру для отрисовки
                Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                pixel.SetData(new[] { Color.White });

                // Рисуем прямоугольник (для теста)
                spriteBatch.Draw(pixel, tileRect, Color.Gray * 0.5f);

                // Рисуем рамку
                DrawRectangle(spriteBatch, tileRect, Color.DarkGray, 1);

                pixel.Dispose();
            }
        }

        public Rectangle GetBounds()
        {
            int worldX = Position.X * Size * Tile.TileSize.Width;
            int worldY = Position.Y * Size * Tile.TileSize.Height;
            int worldWidth = Size * Tile.TileSize.Width;
            int worldHeight = Size * Tile.TileSize.Height;

            return new Rectangle(worldX, worldY, worldWidth, worldHeight);
        }

        /// <summary>
        /// Установить видимость чанка
        /// </summary>
        public void SetVisible(bool visible)=>IsVisible = visible;

        public void Dispose()=> Clear();

        internal void SetTile(int x, int y, int z, Tile tile)
        {
            // Ваша существующая реализация
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Depth)
                return;

            RemoveTile(x, y, z);

            if (tile != null)
            {
                int globalX = Position.X * Size + x;
                int globalY = Position.Y * Size + y;
                tile.SetPosition(new Point(globalX, globalY), z);
                _tiles.Add(tile);
                IsDirty = true;
            }
        }

        public void SetDrawDepth(float depth)
        {
            throw new NotImplementedException();
        }

        // === Вспомогательный метод для отрисовки прямоугольника ===
        private void DrawRectangle(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            // Верхняя линия
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            // Нижняя линия
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            // Левая линия
            spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            // Правая линия
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);

            pixel.Dispose();
        }
    }
}
