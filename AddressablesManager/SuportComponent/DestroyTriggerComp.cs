using System;

namespace UnityEngine.AddressableAssets
{
    public class DestroyTriggerComp : MonoBehaviour
    {
        internal Action OnGameObjectDestroy;

        private void OnDestroy() 
        {
            OnGameObjectDestroy?.Invoke();
            OnGameObjectDestroy = null;
        } 
    }
}