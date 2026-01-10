namespace TalesFromTheUnderbrush
{
    internal static class GlobalSettingsHelpers
    {

        public static T LoadAsset<T>(string assetPath) where T : class
        {
            if (loadedAssets.ContainsKey(assetPath))
                return loadedAssets[assetPath] as T;

            T asset = Content.Load<T>(assetPath);
            loadedAssets[assetPath] = asset;

            return asset;
        }

        public static T LoadAsset<T>(string assetPath) where T : class
        {
            if (loadedAssets.ContainsKey(assetPath))
                return loadedAssets[assetPath] as T;

            T asset = Content.Load<T>(assetPath); // <- Content может быть null!
            loadedAssets[assetPath] = asset;
            return asset;
        }
    }
}