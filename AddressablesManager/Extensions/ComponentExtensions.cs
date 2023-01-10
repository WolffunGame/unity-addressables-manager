namespace UnityEngine.AddressableAssets
{
    internal static class ComponentExtensions
    {
        public static void OnDestroyTrigger(this GameObject gameObject, System.Action callBack)
        {
            var comp = GetOrAddComponent<DestroyTriggerComp>();
            comp.OnGameObjectDestroy += callBack;
        }

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
#if UNITY_2019_2_OR_NEWER
            if (!gameObject.TryGetComponent<T>(out var component))
                component = gameObject.AddComponent<T>();

#else
            var component = gameObject.GetComponent<T>();
            if (component == null)
                component = gameObject.AddComponent<T>();
#endif

            return component;
        }
    }
}