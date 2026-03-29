using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ConvertedTerrainSnapTools
{
    [MenuItem("Tools/Terrain/Snap Converted Terrain Resources")]
    public static void SnapAllInOpenScenes()
    {
        SetMineStoneEmbedDepthAndSnapInternal(null);
    }

    [MenuItem("Tools/Terrain/Set Converted Stone Embed Depth To 4 And Snap")]
    public static void SetMineStoneEmbedDepthToFourAndSnap()
    {
        SetMineStoneEmbedDepthAndSnapInternal(4f);
    }

    public static void SetMineStoneEmbedDepthAndSnap(float embedDepth)
    {
        SetMineStoneEmbedDepthAndSnapInternal(embedDepth);
    }

    private static void SetMineStoneEmbedDepthAndSnapInternal(float? embedDepthOverride)
    {
        if (Application.isPlaying)
        {
            Debug.LogWarning("ConvertedTerrainSnapTools: Stop Play Mode before snapping converted terrain resources.");
            return;
        }

        TerrainTreeToPrefabConverter[] converters = Object.FindObjectsByType<TerrainTreeToPrefabConverter>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        TerrainTreeProxySpawner[] proxySpawners = Object.FindObjectsByType<TerrainTreeProxySpawner>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        int snappedConverters = 0;
        for (int i = 0; i < converters.Length; i++)
        {
            TerrainTreeToPrefabConverter converter = converters[i];
            if (converter == null)
            {
                continue;
            }

            if (embedDepthOverride.HasValue)
            {
                SerializedObject serializedConverter = new SerializedObject(converter);
                SerializedProperty embedDepthProperty = serializedConverter.FindProperty("mineStoneEmbedDepth");
                if (embedDepthProperty != null)
                {
                    embedDepthProperty.floatValue = embedDepthOverride.Value;
                    serializedConverter.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            converter.SnapExistingConvertedObjectsToTerrain();
            EditorUtility.SetDirty(converter);
            if (converter.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(converter.gameObject.scene);
            }

            snappedConverters++;
        }

        int updatedProxySpawners = 0;
        for (int i = 0; i < proxySpawners.Length; i++)
        {
            TerrainTreeProxySpawner proxySpawner = proxySpawners[i];
            if (proxySpawner == null || !embedDepthOverride.HasValue)
            {
                continue;
            }

            SerializedObject serializedSpawner = new SerializedObject(proxySpawner);
            SerializedProperty embedDepthProperty = serializedSpawner.FindProperty("mineStoneEmbedDepth");
            if (embedDepthProperty == null)
            {
                continue;
            }

            embedDepthProperty.floatValue = embedDepthOverride.Value;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(proxySpawner);
            if (proxySpawner.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(proxySpawner.gameObject.scene);
            }

            updatedProxySpawners++;
        }

        if (snappedConverters > 0 || updatedProxySpawners > 0)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        string depthSummary = embedDepthOverride.HasValue
            ? $" EmbedDepth={embedDepthOverride.Value}."
            : string.Empty;
        Debug.Log(
            $"ConvertedTerrainSnapTools: Snapped converted terrain resources for {snappedConverters} converters. " +
            $"Updated proxy spawners={updatedProxySpawners}.{depthSummary}");
    }
}
