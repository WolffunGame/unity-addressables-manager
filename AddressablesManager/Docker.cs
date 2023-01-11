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

        public static UniTask<OperationResult<T>> DockTo<T>(this UniTask<OperationResult<T>> operation, string assetKey,
            Scene sceneDocker) where T : Object
        {
            if (GuardKey(assetKey, out var key))
            {
                SubscribeSceneEvents();
                if (!_sceneToAddress.TryGetValue(sceneDocker, out var addressList))
                {
                    addressList = CollectionPool<HashSet<string>, string>.Get();
                    addressList.Add(key);
                    _sceneToAddress.Add(sceneDocker, addressList);
                }
                if (!addressList.Contains(key))
                    addressList.Add(key);
            }
            
            return operation;
        }

        public static UniTask<OperationResult<T>> DockTo<T>(this UniTask<OperationResult<T>> operation,
            AssetReferenceT<T> reference, Scene sceneDocker) where T : Object
        {
            if (GuardKey(reference, out var key))
                return DockTo(operation, key, sceneDocker);
            return operation;
        }

        public static UniTask<OperationResult<T>> DockTo<T>(this UniTask<OperationResult<T>> operation, string assetKey,
            GameObject gameObject) where T : Object
        {
            if (GuardKey(assetKey, out var key))
            {
                if (!_dockedAssetToGameObject.Contains(assetKey))
                {
                    _dockedAssetToGameObject.Add(key);
                    gameObject.OnDestroyTrigger(() =>
                    {
                        _dockedAssetToGameObject.Remove(key);
                        ReleaseAsset(key);
                    });
                }
                else
                {
                    Debug.LogWarning(
                        $"Not able to dock asset {key} to {gameObject.name} because it is already docked to another GameObject");
                }
            }
          
            return operation;
        }

        public static UniTask<OperationResult<T>> DockTo<T>(this UniTask<OperationResult<T>> operation,
            AssetReferenceT<T> reference, GameObject gameObject) where T : Object
        {
            if (GuardKey(reference, out var key))
                return DockTo(operation, key, gameObject);
            return operation;
        }

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
            Application.quitting += OnApplicationQuit;
        }

        private static void OnApplicationQuit()
        {
            _isInitialized = false;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            Application.quitting -= OnApplicationQuit;
        }
    }
}