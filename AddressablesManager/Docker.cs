using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using UnityEngine.Pool;

namespace UnityEngine.AddressableAssets
{
    public static partial class AddressablesManager
    {
        private static readonly Dictionary<Scene, HashSet<string>> _sceneToAddress = new();
        private static readonly HashSet<string> _dockedAssetToGameObject = new();
        private static bool _isInitialized;

        #region  SceneDocker
        /// <summary>
        /// Load Asset Async and dock address to scene
        /// Asset will be unloaded when scene is unloaded
        /// </summary>
        /// <param name="assetKey"></param>
        /// <param name="sceneToDock"> can't be default. address will leak and can't be auto unload</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static UniTask<OperationResult<T>> LoadAssetAsync<T>(string assetKey, Scene sceneToDock) where T : Object
        {
            if (sceneToDock != default(Scene) && GuardKey(assetKey, out var key))
                DockToScene(key, sceneToDock);
            return LoadAssetAsync<T>(assetKey);
        }
        
        /// <summary>
        /// Load Asset Async and dock address to scene
        /// Asset will be unloaded when scene is unloaded
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="sceneToDock">can't be default. address will leak and can't be auto unload</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static UniTask<OperationResult<T>> LoadAssetAsync<T>(AssetReferenceT<T> reference, Scene sceneToDock) where T : Object
        {
            if (sceneToDock != default(Scene) && GuardKey(reference, out var key))
                DockToScene(key, sceneToDock);
            return LoadAssetAsync<T>(reference);
        }
        
        public static UniTask<OperationResult<GameObject>> InstantiateAsync(
            string assetKey,
            Scene sceneToDock,
            Transform parent = null,
            bool inWorldSpace = false,
            bool trackHandle = true) 
        {
            if (sceneToDock != default(Scene) && GuardKey(assetKey, out var key))
                DockToScene(key, sceneToDock);
            return InstantiateAsync(assetKey, parent, inWorldSpace, trackHandle);
        }
        
        public static UniTask<OperationResult<GameObject>> InstantiateAsync(
            AssetReference reference, 
            Scene sceneToDock,
            Transform parent = null,
            bool inWorldSpace = false)
        {
            if (sceneToDock != default(Scene) && GuardKey(reference, out var key))
                DockToScene(key, sceneToDock);
            return InstantiateAsync(reference, parent, inWorldSpace);
        }

        
        private static void DockToScene( string key, Scene scene)
        {
            SubscribeSceneEvents();
            if (!_sceneToAddress.TryGetValue(scene, out var addressList))
            {
                addressList = CollectionPool<HashSet<string>, string>.Get();
                addressList.Add(key);
                _sceneToAddress.Add(scene, addressList);
            }
            if (!addressList.Contains(key))
                addressList.Add(key);
        }

        #endregion

        #region GameObjectDocker
        /// <summary>
        /// Load Asset Async and dock address to scene
        /// Asset will be unloaded when GameObject is destroyed
        /// </summary>
        /// <param name="assetKey"></param>
        /// <param name="gameObjectToDock">can't be null. address will leak and can't be auto unload</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static UniTask<OperationResult<T>> LoadAssetAsync<T>(string assetKey, GameObject gameObjectToDock) where T : Object
        {
            if (gameObjectToDock != null && GuardKey(assetKey, out var key))
                DockToGameObject(key, gameObjectToDock);
            return LoadAssetAsync<T>(assetKey);
        }
        
        /// <summary>
        /// Load Asset Async and dock address to scene
        /// Asset will be unloaded when GameObject is destroyed
        /// </summary>
        /// <param name="reference"></param>
        /// <param name="gameObjectToDock">can't be null. address will leak and can't be auto unload</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static UniTask<OperationResult<T>> LoadAssetAsync<T>( AssetReferenceT<T> reference, GameObject gameObjectToDock) where T : Object
        {
            if (gameObjectToDock != null && GuardKey(reference, out var key))
                DockToGameObject(key, gameObjectToDock);
            return LoadAssetAsync<T>(reference);
        }

        private static void DockToGameObject(string key, GameObject gameObjectDocker)
        {
            if (!_dockedAssetToGameObject.Contains(key))
            {
                _dockedAssetToGameObject.Add(key);
                gameObjectDocker.OnDestroyTrigger(() =>
                {
                    _dockedAssetToGameObject.Remove(key);
                    ReleaseAsset(key);
                });
            }
            else
            {
                Debug.LogWarning(
                    $"Not able to dock asset {key} to {gameObjectDocker.name} because it is already docked to another GameObject");
            }
        }

        #endregion
        
        public static void OnSceneUnloaded(Scene sceneUnload)
        {
            if (_sceneToAddress.TryGetValue(sceneUnload, out var addressList))
            {
                foreach (var address in addressList)
                    ReleaseAsset(address);
                _sceneToAddress.Remove(sceneUnload);
                CollectionPool<HashSet<string>, string>.Release(addressList);
                Resources.UnloadUnusedAssets();
            }
        }

        private static void SubscribeSceneEvents()
        {
            if (_isInitialized)
                return;
            _isInitialized = true;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        static partial void OnAppQuit()
        {
            Clear();
            _sceneToAddress.Clear();
            _dockedAssetToGameObject.Clear();
            _isInitialized = false;
        }
    }
}