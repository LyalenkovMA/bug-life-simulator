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
        public float DrawOrder { get;private set; }
        public bool Visible { get;private set; } = true;
        public event EventHandler DrawOrderChanged;
        public event EventHandler VisibleChanged;
        public event EventHandler DrawDepthChanged;

        public Point Position { get; private set; }
        public int Size { get; private set; }
        public bool IsDirty { get; set; } = true;
        public bool IsVisible { get; private set; } = true;

        private SpriteBatch _spriteBatch;

        private readonly List<Tile> _tiles = new();

        // Если их нет, добавьте в начало класса:
        private int _width, _height, _depth;


        public TileChunk(Point position, int size)
        {
            Position = position;
            Size = size;
            _width = size;
            _height = size;
            _depth = 1; // Или значение из конфигурации
        }

        public int Width => _width;
        public int Height => _height;
        public int Depth => _depth;

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
            // Проверяем границы
            if (x < 0 || x >= Size || y < 0 || y >= Size || z < 0 || z >= Depth)
                return null;

            // Ищем тайл с нужными координатами
            // Глобальные координаты = Position * Size + локальные координаты
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

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsVisible) return;

            // Сортируем тайлы по слою для правильного отображения
            var sortedTiles = _tiles.OrderBy(t => t.Layer).ThenBy(t => t.GridPosition.Y);

            foreach (Tile tile in sortedTiles)
            {
                // Временная заглушка для отрисовки
                // Позже нужно будет добавить текстуры
                if (tile != null)
                {
                    // Для теста можно нарисовать простой прямоугольник
                    // или добавить базовую отрисовку
                }
            }

            // После отрисовки сбрасываем флаг изменений
            IsDirty = false;
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

        internal void SetTile(int localX, int localY, int z, Tile tile)
        {
            if (localX < 0 || localX >= Size || localY < 0 || localY >= Size || z < 0 || z >= Depth)
                return;

            // Удаляем старый тайл на этой позиции
            RemoveTile(localX, localY, z);

            if (tile != null)
            {
                // Устанавливаем позицию тайла
                int globalX = Position.X * Size + localX;
                int globalY = Position.Y * Size + localY;
                tile.SetPosition(new Point(globalX, globalY), z);

                // Добавляем в список
                _tiles.Add(tile);
                IsDirty = true; // Помечаем чанк как измененный
            }
        }

        public void Draw(Microsoft.Xna.Framework.GameTime gameTime, SpriteBatch spriteBatch)
        {
            throw new NotImplementedException();
        }

        public void SetDrawDepth(float depth)
        {
            throw new NotImplementedException();
        }

        public void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            throw new NotImplementedException();
        }
    }
}
