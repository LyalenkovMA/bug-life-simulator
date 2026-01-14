using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.Xna.Framework;
using Point = Microsoft.Xna.Framework.Point;

namespace TalesFromTheUnderbrush.src
{
    /// <summary>
    /// Пространственная сетка для быстрого поиска объектов в области
    /// </summary>
    public class SpatialGrid<T> where T : class
    {
        private readonly float _cellSize;
        private readonly int _cellsX;
        private readonly int _cellsY;
        private readonly List<T>[,] _grid;
        private readonly Dictionary<T, List<Point>> _objectCells = new();

        public int Count { get; private set; }

        public SpatialGrid(float width, float height, float cellSize)
        {
            if (cellSize <= 0)
                throw new ArgumentException("Cell size must be positive");

            _cellSize = cellSize;
            _cellsX = (int)Math.Ceiling(width / cellSize);
            _cellsY = (int)Math.Ceiling(height / cellSize);

            _grid = new List<T>[_cellsX, _cellsY];

            for (int y = 0; y < _cellsY; y++)
            {
                for (int x = 0; x < _cellsX; x++)
                {
                    _grid[x, y] = new List<T>();
                }
            }
        }

        /// <summary>
        /// Добавить объект с заданными границами
        /// </summary>
        public void Add(T obj, RectangleF bounds)
        {
            if (obj == null || _objectCells.ContainsKey(obj))
                return;

            var cells = GetCellsForBounds(bounds);
            _objectCells[obj] = cells;

            foreach (var cell in cells)
            {
                if (IsValidCell(cell))
                {
                    _grid[cell.X, cell.Y].Add(obj);
                }
            }

            Count++;
        }

        /// <summary>
        /// Удалить объект
        /// </summary>
        public bool Remove(T obj)
        {
            if (obj == null || !_objectCells.TryGetValue(obj, out var cells))
                return false;

            foreach (Point cell in cells)
            {
                if (IsValidCell(cell))
                {
                    _grid[cell.X, cell.Y].Remove(obj);
                }
            }

            _objectCells.Remove(obj);
            Count--;

            return true;
        }

        /// <summary>
        /// Обновить позицию объекта
        /// </summary>
        public bool Update(T obj, RectangleF newBounds)
        {
            if (obj == null || !_objectCells.ContainsKey(obj))
                return false;

            Remove(obj);
            Add(obj, newBounds);

            return true;
        }

        /// <summary>
        /// Получить объекты в области
        /// </summary>
        public List<T> Query(RectangleF area)
        {
            var result = new HashSet<T>();
            var cells = GetCellsForBounds(area);

            foreach (var cell in cells)
            {
                if (IsValidCell(cell))
                {
                    foreach (var obj in _grid[cell.X, cell.Y])
                    {
                        // Проверяем, что объект действительно в области
                        // (так как один объект может быть в нескольких ячейках)
                        result.Add(obj);
                    }
                }
            }

            return result.ToList();
        }

        /// <summary>
        /// Получить все объекты
        /// </summary>
        public List<T> GetAll()
        {
            return _objectCells.Keys.ToList();
        }

        /// <summary>
        /// Очистить сетку
        /// </summary>
        public void Clear()
        {
            for (int y = 0; y < _cellsY; y++)
            {
                for (int x = 0; x < _cellsX; x++)
                {
                    _grid[x, y].Clear();
                }
            }

            _objectCells.Clear();
            Count = 0;
        }

        private List<Point> GetCellsForBounds(RectangleF bounds)
        {
            int startX = Math.Max(0, (int)(bounds.Left / _cellSize));
            int endX = Math.Min(_cellsX - 1, (int)(bounds.Right / _cellSize));
            int startY = Math.Max(0, (int)(bounds.Top / _cellSize));
            int endY = Math.Min(_cellsY - 1, (int)(bounds.Bottom / _cellSize));

            List<Point> cells = new List<Point>();

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    cells.Add(new Point(x, y));
                }
            }

            return cells;
        }

        private bool IsValidCell(Point cell)
        {
            return cell.X >= 0 && cell.X < _cellsX &&
                   cell.Y >= 0 && cell.Y < _cellsY;
        }
    }
}
