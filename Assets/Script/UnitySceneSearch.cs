using UnityEngine;

public static class UnitySceneSearch {
    public static T FindFirst<T>(bool includeInactive = true) where T : Object {
#if UNITY_2023_1_OR_NEWER
        FindObjectsInactive inactiveMode = includeInactive
            ? FindObjectsInactive.Include
            : FindObjectsInactive.Exclude;
        return Object.FindFirstObjectByType<T>(inactiveMode);
#else
        return Object.FindObjectOfType<T>(includeInactive);
#endif
    }

    public static T[] FindAll<T>(bool includeInactive = true) where T : Object {
#if UNITY_2023_1_OR_NEWER
        FindObjectsInactive inactiveMode = includeInactive
            ? FindObjectsInactive.Include
            : FindObjectsInactive.Exclude;
        return Object.FindObjectsByType<T>(inactiveMode, FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<T>(includeInactive);
#endif
    }
}
