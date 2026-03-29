using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TerrainBuildFixTools
{
    [MenuItem("Tools/Terrain/Report Build Terrain Tree State")]
    public static void ReportBuildTerrainTreeState()
    {
        ProcessBuildScenes(fixSceneData: false);
    }

    [MenuItem("Tools/Terrain/Fix Build Terrain Trees")]
    public static void FixBuildTerrainTrees()
    {
        ProcessBuildScenes(fixSceneData: true);
    }

    // Batchmode entry point.
    public static void FixBuildTerrainTreesBatchMode()
    {
        FixBuildTerrainTrees();
    }

    private static void ProcessBuildScenes(bool fixSceneData)
    {
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        if (buildScenes == null || buildScenes.Length == 0)
        {
            Debug.LogWarning("TerrainBuildFixTools: No scenes are enabled in Build Settings.");
            return;
        }

        string activeScenePath = EditorSceneManager.GetActiveScene().path;
        HashSet<TerrainData> touchedTerrainData = new HashSet<TerrainData>();
        int processedSceneCount = 0;
        int converterCount = 0;
        int fixedTerrainCount = 0;

        for (int i = 0; i < buildScenes.Length; i++)
        {
            EditorBuildSettingsScene buildScene = buildScenes[i];
            if (buildScene == null || !buildScene.enabled || string.IsNullOrWhiteSpace(buildScene.path))
            {
                continue;
            }

            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
            var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            processedSceneCount++;

            TerrainTreeToPrefabConverter[] converters = FindSceneConverters();
            if (converters == null || converters.Length == 0)
            {
                Debug.Log($"TerrainBuildFixTools: Scene '{scene.path}' has no TerrainTreeToPrefabConverter components.");
                RestoreSceneSetup(previousSetup, activeScenePath);
                continue;
            }

            bool sceneDirty = false;
            for (int converterIndex = 0; converterIndex < converters.Length; converterIndex++)
            {
                TerrainTreeToPrefabConverter converter = converters[converterIndex];
                if (converter == null)
                {
                    continue;
                }

                converterCount++;
                Terrain targetTerrain = ResolveTargetTerrain(converter);
                if (targetTerrain == null || targetTerrain.terrainData == null)
                {
                    Debug.LogWarning($"TerrainBuildFixTools: Converter '{converter.name}' in '{scene.path}' has no valid target terrain.");
                    continue;
                }

                TerrainData terrainData = targetTerrain.terrainData;
                TreePrototype[] prototypes = terrainData.treePrototypes;
                TreeInstance[] instances = terrainData.treeInstances;
                LogTerrainState(scene.path, targetTerrain, terrainData, prototypes, instances);

                if (!fixSceneData)
                {
                    continue;
                }

                if (touchedTerrainData.Contains(terrainData))
                {
                    continue;
                }

                converter.ConvertPaintedTreesToPrefabs();
                terrainData.treeInstances = System.Array.Empty<TreeInstance>();
                terrainData.treePrototypes = System.Array.Empty<TreePrototype>();
                EditorUtility.SetDirty(terrainData);
                touchedTerrainData.Add(terrainData);
                fixedTerrainCount++;
                sceneDirty = true;

                Debug.Log(
                    $"TerrainBuildFixTools: Cleared terrain tree data for '{targetTerrain.name}' " +
                    $"({AssetDatabase.GetAssetPath(terrainData)}).");
            }

            if (fixSceneData && sceneDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            RestoreSceneSetup(previousSetup, activeScenePath);
        }

        if (fixSceneData && touchedTerrainData.Count > 0)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        Debug.Log(
            $"TerrainBuildFixTools: Completed. Scenes={processedSceneCount}, Converters={converterCount}, " +
            $"FixedTerrains={fixedTerrainCount}, Mode={(fixSceneData ? "Fix" : "Report")}.");
    }

    private static TerrainTreeToPrefabConverter[] FindSceneConverters()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<TerrainTreeToPrefabConverter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<TerrainTreeToPrefabConverter>(true);
#endif
    }

    private static Terrain ResolveTargetTerrain(TerrainTreeToPrefabConverter converter)
    {
        if (converter == null)
        {
            return null;
        }

        SerializedObject serializedObject = new SerializedObject(converter);
        SerializedProperty targetTerrainProperty = serializedObject.FindProperty("targetTerrain");
        Terrain targetTerrain = targetTerrainProperty != null
            ? targetTerrainProperty.objectReferenceValue as Terrain
            : null;

        if (targetTerrain != null)
        {
            return targetTerrain;
        }

        return converter.GetComponent<Terrain>();
    }

    private static void LogTerrainState(
        string scenePath,
        Terrain terrain,
        TerrainData terrainData,
        TreePrototype[] prototypes,
        TreeInstance[] instances)
    {
        string terrainDataPath = AssetDatabase.GetAssetPath(terrainData);
        int prototypeCount = prototypes != null ? prototypes.Length : 0;
        int instanceCount = instances != null ? instances.Length : 0;

        Debug.Log(
            $"TerrainBuildFixTools: Scene='{scenePath}', Terrain='{terrain.name}', TerrainData='{terrainDataPath}', " +
            $"Prototypes={prototypeCount}, Instances={instanceCount}.");

        if (prototypeCount == 0)
        {
            return;
        }

        int[] instanceCounts = CountInstancesByPrototype(instances, prototypeCount);
        for (int i = 0; i < prototypeCount; i++)
        {
            TreePrototype prototype = prototypes[i];
            GameObject prefab = prototype != null ? prototype.prefab : null;
            string prefabPath = prefab != null ? AssetDatabase.GetAssetPath(prefab) : "<missing>";
            string prefabName = prefab != null ? prefab.name : "<missing>";
            Debug.Log(
                $"TerrainBuildFixTools:   Prototype[{i}] Name='{prefabName}', Instances={instanceCounts[i]}, Path='{prefabPath}'.");
        }
    }

    private static int[] CountInstancesByPrototype(TreeInstance[] instances, int prototypeCount)
    {
        int[] counts = new int[prototypeCount];
        if (instances == null)
        {
            return counts;
        }

        for (int i = 0; i < instances.Length; i++)
        {
            int prototypeIndex = instances[i].prototypeIndex;
            if (prototypeIndex < 0 || prototypeIndex >= counts.Length)
            {
                continue;
            }

            counts[prototypeIndex]++;
        }

        return counts;
    }

    private static void RestoreSceneSetup(SceneSetup[] previousSetup, string fallbackScenePath)
    {
        if (previousSetup != null && previousSetup.Length > 0)
        {
            EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            return;
        }

        if (!string.IsNullOrWhiteSpace(fallbackScenePath))
        {
            EditorSceneManager.OpenScene(fallbackScenePath, OpenSceneMode.Single);
        }
    }
}
