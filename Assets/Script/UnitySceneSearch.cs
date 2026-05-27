using System.Collections.Generic; using UnityEngine;

public static class UnitySceneSearch {
    private sealed class CacheEntry {
        public Object[] objects;
        public float expiresAt; }

    private static readonly Dictionary<string, CacheEntry> Cache = new Dictionary<string, CacheEntry>();

    public static T FindFirst<T>(bool includeInactive = true) where T : Object {
#if UNITY_2023_1_OR_NEWER
        FindObjectsInactive inactiveMode = includeInactive
            ? FindObjectsInactive.Include
            : FindObjectsInactive.Exclude;
        return Object.FindAnyObjectByType<T>(inactiveMode);
#else
        return Object.FindObjectOfType<T>(includeInactive);
#endif
    }

    public static T FindFirstCached<T>(float lifetimeSeconds, bool includeInactive = true) where T : Object {
        T[] objects = FindAllCached<T>(lifetimeSeconds, includeInactive);
        for (int i = 0; i < objects.Length; i++) { if (objects[i] != null) { return objects[i]; } }

        return null; }

    public static T[] FindAll<T>(bool includeInactive = true) where T : Object {
#if UNITY_2023_1_OR_NEWER
        FindObjectsInactive inactiveMode = includeInactive
            ? FindObjectsInactive.Include
            : FindObjectsInactive.Exclude;
        return Object.FindObjectsByType<T>(inactiveMode);
#else
        return Object.FindObjectsOfType<T>(includeInactive);
#endif
    }

    public static T[] FindAllCached<T>(float lifetimeSeconds, bool includeInactive = true) where T : Object {
        if (lifetimeSeconds <= 0f) { return FindAll<T>(includeInactive); }

        string key = typeof(T).FullName + "|" + includeInactive;
        float now = Application.isPlaying ? Time.unscaledTime : 0f;
        if (Cache.TryGetValue(key, out CacheEntry entry) &&
            now < entry.expiresAt &&
            entry.objects is T[] cachedObjects) { return cachedObjects; }

        T[] objects = FindAll<T>(includeInactive);
        Cache[key] = new CacheEntry {
            objects = objects,
            expiresAt = now + Mathf.Max(0.01f, lifetimeSeconds)
        };
        return objects; }

    public static void ClearCache() { Cache.Clear(); }
}
