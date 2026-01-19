using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace TalesFromTheUnderbrush.src
{
    public class AssetManager : IDisposable
    {
        private readonly ContentManager _content;
        private readonly Dictionary<Type, Dictionary<string, object>> _assets;

        public AssetManager(ContentManager content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _assets = new Dictionary<Type, Dictionary<string, object>>();
        }

        // Вспомогательный метод для безопасного получения словаря по типу
        private Dictionary<string, object> GetTypeDictionary<T>() where T : class
        {
            var type = typeof(T);

            if (!_assets.TryGetValue(type, out var typeDict))
            {
                typeDict = new Dictionary<string, object>();
                _assets[type] = typeDict;
            }

            return typeDict;
        }

        // Основной метод загрузки с кэшированием
        public T Load<T>(string assetPath) where T : class
        {
            if (string.IsNullOrEmpty(assetPath))
                throw new ArgumentException("Asset path cannot be null or empty", nameof(assetPath));

            Dictionary<string, object> typeDict = GetTypeDictionary<T>();

            // Проверяем кэш
            if (typeDict.TryGetValue(assetPath, out var cachedAsset))
            {
                return (T)cachedAsset;
            }

            // Загружаем из ContentManager
            T asset = _content.Load<T>(assetPath);

            // Кэшируем
            typeDict[assetPath] = asset;

            return asset;
        }

        // Удаление из кэша
        public void Unload<T>(string assetPath) where T : class
        {
            Dictionary<string, object> typeDict = GetTypeDictionary<T>();
            typeDict.Remove(assetPath);
        }

        // Удаление всех ассетов определённого типа
        public void UnloadAll<T>() where T : class
        {
            var type = typeof(T);
            _assets.Remove(type);
        }

        // Полная очистка всех ассетов
        public void Clear()
        {
            foreach (var typeDict in _assets.Values)
            {
                foreach (var asset in typeDict.Values)
                {
                    if (asset is IDisposable disposable)
                        disposable.Dispose();
                }
                typeDict.Clear();
            }
            _assets.Clear();
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
