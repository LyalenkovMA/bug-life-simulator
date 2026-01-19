using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TalesFromTheUnderbrush.src.Core.Entities;
using TalesFromTheUnderbrush.src.Graphics.Tiles;
using TalesFromTheUnderbrush.src.UI.Camera;
using Color = Microsoft.Xna.Framework.Color;
using Point = Microsoft.Xna.Framework.Point;

namespace TalesFromTheUnderbrush.src.GameLogic
{
    /// <summary>
    /// Основной класс мира, управляющий тайлами и сущностями
    /// </summary>
    public class World : IUpdatable, IDrawable, IPersistable
    {
        // === ПОЛЯ ===
        private readonly TileGrid _tileGrid;
        private readonly SpatialGrid<Entity> _spatialGrid;
        private readonly List<GameEntity> _gameEntities;
        private readonly List<StaticEntity> _staticEntities;

        private readonly Random _random;
        private WorldState _worldState;

        public event EventHandler UpdateOrderChanged;
        public event EventHandler DrawDepthChanged;
        public event EventHandler VisibleChanged;
        public event Action<IPersistable> OnBeforeSave;
        public event Action<IPersistable> OnAfterLoad;

        // === СВОЙСТВА ===
        public string Name { get; private set; }
        public int Width => _tileGrid?.Width ?? 0;
        public int Height => _tileGrid?.Height ?? 0;
        public int Depth => _tileGrid?.Depth ?? 0;

        public TileGrid TileGrid => _tileGrid;
        public SpatialGrid<Entity> SpatialGrid => _spatialGrid;
        public WorldState State => _worldState;

        public int UpdateOrder => throw new NotImplementedException();

        public float DrawDepth => throw new NotImplementedException();

        public bool Visible => throw new NotImplementedException();

        public string PersistentId => throw new NotImplementedException();

        public string PersistentType => throw new NotImplementedException();

        public bool ShouldSave => throw new NotImplementedException();

        // === КОНСТРУКТОРЫ ===

        /// <summary>
        /// Создать новый пустой мир
        /// </summary>
        public World(string name, int width, int height, int depth = 1)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("World name cannot be empty");

            if (width <= 0 || height <= 0 || depth <= 0)
                throw new ArgumentException("World dimensions must be positive");

            Name = name;
            _tileGrid = new TileGrid(width, height, depth);
            _spatialGrid = new SpatialGrid<Entity>(width, height, 64); // Чанки по 64x64
            _gameEntities = new List<GameEntity>();
            _staticEntities = new List<StaticEntity>();
            _random = new Random();
            _worldState = WorldState.Normal;

            Console.WriteLine($"[World] Создан мир '{name}' размером {width}x{height}x{depth}");
        }

        /// <summary>
        /// Создать мир из данных сохранения
        /// </summary>
        public World(PersistenceData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Загружаем из данных сохранения
            Name = data.GetValue<string>("Name");
            int width = data.GetValue<int>("Width");
            int height = data.GetValue<int>("Height");
            int depth = data.GetValue<int>("Depth", 1);

            _tileGrid = new TileGrid(width, height, depth);
            _spatialGrid = new SpatialGrid<Entity>(width, height, 64);
            _gameEntities = new List<GameEntity>();
            _staticEntities = new List<StaticEntity>();
            _random = new Random();
            _worldState = data.GetValue<WorldState>("WorldState", WorldState.Normal);

            // Загружаем тайлы
            LoadTilesFromData(data);

            Console.WriteLine($"[World] Загружен мир '{Name}' из сохранения");
        }

        // === ОСНОВНЫЕ МЕТОДЫ ===

        /// <summary>
        /// Обновление состояния мира
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (_worldState == WorldState.Paused)
                return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Обновляем все игровые сущности
            for (int i = _gameEntities.Count - 1; i >= 0; i--)
            {
                var entity = _gameEntities[i];

                if (entity.IsActive)
                {
                    entity.Update(gameTime);

                    // Проверяем, не нужно ли удалить сущность
                    if (entity.ShouldBeRemoved)
                    {
                        RemoveGameEntity(entity);
                        i--; // Корректируем индекс после удаления
                    }
                }
            }

            // Обновляем статические сущности (реже)
            if (gameTime.TotalGameTime.Milliseconds % 100 == 0) // Каждые 100 мс
            {
                foreach (var entity in _staticEntities)
                {
                    if (entity.IsActive)
                    {
                        entity.Update(gameTime);
                    }
                }
            }

            // Обновляем SpatialGrid (позиции сущностей)
            UpdateSpatialGrid();
        }

        /// <summary>
        /// Отрисовка мира
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Отрисовка тайлов (TileGrid сам управляет отрисовкой)
            _tileGrid?.Draw(spriteBatch);

            // Отрисовка сущностей
            foreach (var entity in _gameEntities)
            {
                if (entity.IsVisible)
                {
                    entity.Draw(spriteBatch);
                }
            }

            foreach (var entity in _staticEntities)
            {
                if (entity.IsVisible)
                {
                    entity.Draw(spriteBatch);
                }
            }
        }

        /// <summary>
        /// Отрисовка мира с учётом камеры (оптимизированная версия)
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, ICamera camera)
        {
            if (camera == null)
            {
                Draw(spriteBatch);
                return;
            }

            // Получаем видимую область через камеру
            var visibleBounds = GetVisibleBounds(camera);

            // Отрисовываем только видимые тайлы
            if (_tileGrid != null)
            {
                var visibleTiles = _tileGrid.GetTilesInArea(visibleBounds);
                foreach (var tile in visibleTiles)
                {
                    if (tile.IsVisible)
                    {
                        tile.Draw(spriteBatch);
                    }
                }
            }

            // Отрисовываем только видимые сущности
            var visibleEntities = _spatialGrid.GetEntitiesInArea(visibleBounds);
            foreach (var entity in visibleEntities)
            {
                if (entity.IsVisible)
                {
                    entity.Draw(spriteBatch);
                }
            }
        }

        // === РАБОТА С СУЩНОСТЯМИ ===

        /// <summary>
        /// Добавить игровую сущность в мир
        /// </summary>
        public void AddGameEntity(GameEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _gameEntities.Add(entity);
            _spatialGrid.Add(entity);

            entity.World = this; // Связываем сущность с миром

            Console.WriteLine($"[World] Добавлена игровая сущность: {entity.Name}");
        }

        /// <summary>
        /// Добавить статическую сущность в мир
        /// </summary>
        public void AddStaticEntity(StaticEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            _staticEntities.Add(entity);
            _spatialGrid.Add(entity);

            entity.World = this;

            Console.WriteLine($"[World] Добавлена статическая сущность: {entity.Name}");
        }

        /// <summary>
        /// Удалить игровую сущность из мира
        /// </summary>
        public bool RemoveGameEntity(GameEntity entity)
        {
            if (entity == null) return false;

            bool removed = _gameEntities.Remove(entity);
            if (removed)
            {
                _spatialGrid.Remove(entity);
                entity.World = null; // Разрываем связь

                Console.WriteLine($"[World] Удалена игровая сущность: {entity.Name}");
            }

            return removed;
        }

        /// <summary>
        /// Удалить статическую сущность из мира
        /// </summary>
        public bool RemoveStaticEntity(StaticEntity entity)
        {
            if (entity == null) return false;

            bool removed = _staticEntities.Remove(entity);
            if (removed)
            {
                _spatialGrid.Remove(entity);
                entity.World = null;

                Console.WriteLine($"[World] Удалена статическая сущность: {entity.Name}");
            }

            return removed;
        }

        /// <summary>
        /// Получить сущность по ID
        /// </summary>
        public Entity GetEntityById(Guid id)
        {
            // Ищем в игровых сущностях
            var gameEntity = _gameEntities.FirstOrDefault(e => e.Id == id);
            if (gameEntity != null) return gameEntity;

            // Ищем в статических сущностях
            var staticEntity = _staticEntities.FirstOrDefault(e => e.Id == id);
            return staticEntity;
        }

        /// <summary>
        /// Получить все сущности в области
        /// </summary>
        public List<Entity> GetEntitiesInArea(Rectangle area)
        {
            return _spatialGrid.GetEntitiesInArea(area).ToList();
        }

        /// <summary>
        /// Получить все сущности определённого типа
        /// </summary>
        public List<T> GetEntitiesOfType<T>() where T : Entity
        {
            var result = new List<T>();

            result.AddRange(_gameEntities.OfType<T>());
            result.AddRange(_staticEntities.OfType<T>());

            return result;
        }

        // === РАБОТА С ТАЙЛАМИ ===

        /// <summary>
        /// Получить тайл по координатам
        /// </summary>
        public Tile GetTileAt(int x, int y, int z = 0)
        {
            return _tileGrid?.GetTile(x, y, z);
        }

        /// <summary>
        /// Установить тайл по координатам
        /// </summary>
        public void SetTileAt(int x, int y, int z, Tile tile)
        {
            _tileGrid?.SetTile(x, y, z, tile);
        }

        /// <summary>
        /// Проверить, можно ли пройти в клетку
        /// </summary>
        public bool IsTileWalkable(int x, int y, int z = 0)
        {
            var tile = GetTileAt(x, y, z);
            return tile?.IsWalkable ?? false;
        }

        /// <summary>
        /// Получить все тайлы в области
        /// </summary>
        public List<Tile> GetTilesInArea(Rectangle area)
        {
            return _tileGrid?.GetTilesInArea(area) ?? new List<Tile>();
        }

        // === СОХРАНЕНИЕ/ЗАГРУЗКА ===

        /// <summary>
        /// Сохранить состояние мира
        /// </summary>
        public PersistenceData Save()
        {
            var data = new PersistenceData("World");

            // Основные свойства
            data.SetValue("Name", Name);
            data.SetValue("Width", Width);
            data.SetValue("Height", Height);
            data.SetValue("Depth", Depth);
            data.SetValue("WorldState", _worldState);

            // Сохраняем тайлы
            SaveTilesToData(data);

            // Сохраняем сущности
            SaveEntitiesToData(data);

            Console.WriteLine($"[World] Мир '{Name}' сохранён");
            return data;
        }

        /// <summary>
        /// Загрузить состояние мира
        /// </summary>
        public void Load(PersistenceData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Name = data.GetValue<string>("Name");
            _worldState = data.GetValue<WorldState>("WorldState", WorldState.Normal);

            // Загружаем тайлы
            LoadTilesFromData(data);

            // Загружаем сущности
            LoadEntitiesFromData(data);

            // Обновляем SpatialGrid
            UpdateSpatialGrid();

            Console.WriteLine($"[World] Мир '{Name}' загружен");
        }

        // === ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ===

        private void UpdateSpatialGrid()
        {
            _spatialGrid.Clear();

            // Добавляем все игровые сущности
            foreach (var entity in _gameEntities)
            {
                if (entity.IsActive)
                {
                    _spatialGrid.Add(entity);
                }
            }

            // Добавляем все статические сущности
            foreach (var entity in _staticEntities)
            {
                if (entity.IsActive)
                {
                    _spatialGrid.Add(entity);
                }
            }
        }

        private Rectangle GetVisibleBounds(ICamera camera)
        {
            if (camera is OrthographicCamera2_5D camera2_5D)
            {
                return camera2_5D.GetVisibleTileBounds();
            }

            // По умолчанию возвращаем область вокруг камеры
            Vector3 camPos = camera.Position;
            int visibleRange = 20; // 20 тайлов во все стороны

            return new Rectangle(
                (int)camPos.X - visibleRange,
                (int)camPos.Y - visibleRange,
                visibleRange * 2,
                visibleRange * 2
            );
        }

        private void SaveTilesToData(PersistenceData data)
        {
            if (_tileGrid == null) return;

            var tilesData = new List<PersistenceData>();

            for (int z = 0; z < Depth; z++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        var tile = _tileGrid.GetTile(x, y, z);
                        if (tile != null)
                        {
                            tilesData.Add(tile.Save());
                        }
                    }
                }
            }

            data.SetValue("Tiles", tilesData);
        }

        private void LoadTilesFromData(PersistenceData data)
        {
            if (_tileGrid == null || !data.HasValue("Tiles")) return;

            var tilesData = data.GetValue<List<PersistenceData>>("Tiles");
            foreach (var tileData in tilesData)
            {
                var tile = new Tile(tileData);
                _tileGrid.SetTile(tile.GridPosition.X, tile.GridPosition.Y, tile.Height, tile);
            }
        }

        private void SaveEntitiesToData(PersistenceData data)
        {
            var entitiesData = new List<PersistenceData>();

            // Сохраняем игровые сущности
            foreach (var entity in _gameEntities)
            {
                entitiesData.Add(entity.Save());
            }

            // Сохраняем статические сущности
            foreach (var entity in _staticEntities)
            {
                entitiesData.Add(entity.Save());
            }

            data.SetValue("Entities", entitiesData);
        }

        private void LoadEntitiesFromData(PersistenceData data)
        {
            if (!data.HasValue("Entities")) return;

            _gameEntities.Clear();
            _staticEntities.Clear();

            var entitiesData = data.GetValue<List<PersistenceData>>("Entities");
            foreach (var entityData in entitiesData)
            {
                string entityType = entityData.GetValue<string>("EntityType");

                switch (entityType)
                {
                    case "GameEntity":
                        var gameEntity = new GameEntity(entityData);
                        AddGameEntity(gameEntity);
                        break;

                    case "StaticEntity":
                        var staticEntity = new StaticEntity(entityData);
                        AddStaticEntity(staticEntity);
                        break;

                    default:
                        Console.WriteLine($"[World] Неизвестный тип сущности: {entityType}");
                        break;
                }
            }
        }

        // === УТИЛИТЫ ===

        /// <summary>
        /// Изменить состояние мира
        /// </summary>
        public void SetWorldState(WorldState state)
        {
            _worldState = state;
            Console.WriteLine($"[World] Состояние мира изменено на: {state}");
        }

        /// <summary>
        /// Очистить мир
        /// </summary>
        public void Clear()
        {
            _gameEntities.Clear();
            _staticEntities.Clear();
            _spatialGrid.Clear();
            _tileGrid?.Clear();

            Console.WriteLine($"[World] Мир '{Name}' очищен");
        }

        /// <summary>
        /// Для отладки
        /// </summary>
        public override string ToString()
        {
            return $"World '{Name}' ({Width}x{Height}x{Depth}), Entities: {_gameEntities.Count + _staticEntities.Count}";
        }

        public void SetUpdateOrder(int order)
        {
            throw new NotImplementedException();
        }

        public void SetVisible(bool visible)
        {
            throw new NotImplementedException();
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            throw new NotImplementedException();
        }

        public void SetDrawDepth(float depth)
        {
            throw new NotImplementedException();
        }
    }
}
