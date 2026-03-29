using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TerrainStoneProxySetupTools
{
    private const string ProxyFolder = "Assets/TerrainObjects/Stones/TerrainProxies";

    private sealed class StoneProxySetup
    {
        public string name;
        public string realPrefabPath;
        public string proxyPrefabPath;
        public GameObject realPrefab;
        public GameObject proxyPrefab;
    }

    [MenuItem("Tools/Terrain/Setup Stone Proxy Painting")]
    public static void SetupStoneProxyPainting()
    {
        List<StoneProxySetup> setups = CreateOrRefreshStoneProxyPrefabs();
        if (setups.Count == 0)
        {
            Debug.LogWarning("TerrainStoneProxySetupTools: No stone prefabs found to configure.");
            return;
        }

        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        if (buildScenes == null || buildScenes.Length == 0)
        {
            Debug.LogWarning("TerrainStoneProxySetupTools: No enabled build scenes found.");
            return;
        }

        int updatedTerrainAssets = 0;
        int replacedPrototypeSlots = 0;
        int configuredScenes = 0;

        for (int i = 0; i < buildScenes.Length; i++)
        {
            EditorBuildSettingsScene buildScene = buildScenes[i];
            if (buildScene == null || !buildScene.enabled || string.IsNullOrWhiteSpace(buildScene.path))
            {
                continue;
            }

            var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            bool sceneDirty = false;

            HashSet<TerrainData> updatedInScene = new HashSet<TerrainData>();
#if UNITY_2023_1_OR_NEWER
            Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            Terrain[] terrains = Object.FindObjectsOfType<Terrain>(true);
#endif

            for (int terrainIndex = 0; terrainIndex < terrains.Length; terrainIndex++)
            {
                Terrain terrain = terrains[terrainIndex];
                if (terrain == null || terrain.terrainData == null)
                {
                    continue;
                }

                if (ReplaceStoneTerrainPrototypes(terrain.terrainData, setups, out int replacedInTerrain))
                {
                    replacedPrototypeSlots += replacedInTerrain;
                    if (updatedInScene.Add(terrain.terrainData))
                    {
                        updatedTerrainAssets++;
                    }
                }
            }

            if (EnsureSceneSpawnerConfigured(setups))
            {
                configuredScenes++;
                sceneDirty = true;
            }

            if (sceneDirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"TerrainStoneProxySetupTools: Completed. Proxies={setups.Count}, " +
            $"TerrainAssetsUpdated={updatedTerrainAssets}, ReplacedPrototypeSlots={replacedPrototypeSlots}, " +
            $"ScenesConfigured={configuredScenes}.");
    }

    public static void SetupStoneProxyPaintingBatchMode()
    {
        SetupStoneProxyPainting();
    }

    private static List<StoneProxySetup> CreateOrRefreshStoneProxyPrefabs()
    {
        EnsureFolderExists(ProxyFolder);

        string[] realPrefabPaths =
        {
            "Assets/TerrainObjects/Stones/Prefabs/Stone1.prefab",
            "Assets/TerrainObjects/Stones/Prefabs/Stone2.prefab",
            "Assets/TerrainObjects/Stones/Prefabs/Stone3.prefab",
            "Assets/TerrainObjects/Stones/Prefabs/Stone4.prefab",
            "Assets/TerrainObjects/Stones/Prefabs/Stone5.prefab"
        };

        List<StoneProxySetup> setups = new List<StoneProxySetup>(realPrefabPaths.Length);
        for (int i = 0; i < realPrefabPaths.Length; i++)
        {
            string realPrefabPath = realPrefabPaths[i];
            GameObject realPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(realPrefabPath);
            if (realPrefab == null)
            {
                continue;
            }

            string stoneName = Path.GetFileNameWithoutExtension(realPrefabPath);
            string proxyPrefabPath = $"{ProxyFolder}/{stoneName}_TerrainProxy.prefab";

            CreateOrRefreshProxyPrefab(realPrefabPath, proxyPrefabPath);

            GameObject proxyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(proxyPrefabPath);
            if (proxyPrefab == null)
            {
                continue;
            }

            setups.Add(new StoneProxySetup
            {
                name = stoneName,
                realPrefabPath = realPrefabPath,
                proxyPrefabPath = proxyPrefabPath,
                realPrefab = realPrefab,
                proxyPrefab = proxyPrefab
            });
        }

        return setups;
    }

    private static void CreateOrRefreshProxyPrefab(string sourcePrefabPath, string proxyPrefabPath)
    {
        GameObject sourceRoot = PrefabUtility.LoadPrefabContents(sourcePrefabPath);
        if (sourceRoot == null)
        {
            return;
        }

        GameObject proxyRoot = new GameObject(Path.GetFileNameWithoutExtension(proxyPrefabPath));
        proxyRoot.layer = 0;
        proxyRoot.tag = "Untagged";
        proxyRoot.transform.localPosition = Vector3.zero;
        proxyRoot.transform.localRotation = Quaternion.identity;
        proxyRoot.transform.localScale = sourceRoot.transform.localScale;

        try
        {
            CopyRenderableComponents(sourceRoot, proxyRoot);

            int childCount = sourceRoot.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform sourceChild = sourceRoot.transform.GetChild(i);
                if (sourceChild == null || ShouldSkipSourceNode(sourceChild))
                {
                    continue;
                }

                if (!HasRenderableDescendant(sourceChild))
                {
                    continue;
                }

                CopyRenderableHierarchy(sourceChild, proxyRoot.transform);
            }

            PrefabUtility.SaveAsPrefabAsset(proxyRoot, proxyPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(proxyRoot);
            PrefabUtility.UnloadPrefabContents(sourceRoot);
        }
    }

    private static void CopyRenderableHierarchy(Transform source, Transform destinationParent)
    {
        GameObject destination = new GameObject(source.name);
        destination.layer = source.gameObject.layer;
        destination.tag = "Untagged";
        destination.transform.SetParent(destinationParent, false);
        destination.transform.localPosition = source.localPosition;
        destination.transform.localRotation = source.localRotation;
        destination.transform.localScale = source.localScale;

        CopyRenderableComponents(source.gameObject, destination);

        int childCount = source.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = source.GetChild(i);
            if (child == null || ShouldSkipSourceNode(child))
            {
                continue;
            }

            if (!HasRenderableDescendant(child))
            {
                continue;
            }

            CopyRenderableHierarchy(child, destination.transform);
        }
    }

    private static void CopyRenderableComponents(GameObject source, GameObject destination)
    {
        MeshFilter sourceFilter = source.GetComponent<MeshFilter>();
        if (sourceFilter != null)
        {
            MeshFilter destinationFilter = destination.AddComponent<MeshFilter>();
            EditorUtility.CopySerialized(sourceFilter, destinationFilter);
        }

        MeshRenderer sourceRenderer = source.GetComponent<MeshRenderer>();
        if (sourceRenderer != null)
        {
            MeshRenderer destinationRenderer = destination.AddComponent<MeshRenderer>();
            EditorUtility.CopySerialized(sourceRenderer, destinationRenderer);
        }
    }

    private static bool HasRenderableDescendant(Transform source)
    {
        if (source == null || ShouldSkipSourceNode(source))
        {
            return false;
        }

        if (source.GetComponent<MeshRenderer>() != null)
        {
            return true;
        }

        int childCount = source.childCount;
        for (int i = 0; i < childCount; i++)
        {
            if (HasRenderableDescendant(source.GetChild(i)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldSkipSourceNode(Transform source)
    {
        return source != null && source.name.IndexOf("Icosphere", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool ReplaceStoneTerrainPrototypes(TerrainData terrainData, List<StoneProxySetup> setups, out int replacedCount)
    {
        replacedCount = 0;
        if (terrainData == null)
        {
            return false;
        }

        TreePrototype[] prototypes = terrainData.treePrototypes;
        if (prototypes == null || prototypes.Length == 0)
        {
            return false;
        }

        bool changed = false;
        for (int i = 0; i < prototypes.Length; i++)
        {
            TreePrototype prototype = prototypes[i];
            GameObject currentPrefab = prototype != null ? prototype.prefab : null;
            if (currentPrefab == null)
            {
                continue;
            }

            for (int setupIndex = 0; setupIndex < setups.Count; setupIndex++)
            {
                StoneProxySetup setup = setups[setupIndex];
                if (setup == null || setup.realPrefab == null || setup.proxyPrefab == null)
                {
                    continue;
                }

                if (currentPrefab != setup.realPrefab)
                {
                    continue;
                }

                prototype.prefab = setup.proxyPrefab;
                prototypes[i] = prototype;
                replacedCount++;
                changed = true;
                break;
            }
        }

        if (!changed)
        {
            return false;
        }

        terrainData.treePrototypes = prototypes;
        EditorUtility.SetDirty(terrainData);
        return true;
    }

    private static bool EnsureSceneSpawnerConfigured(List<StoneProxySetup> setups)
    {
        GameObject spawnerObject = GameObject.Find("TerrainStoneProxySpawner");
        bool changed = false;

        if (spawnerObject == null)
        {
            spawnerObject = new GameObject("TerrainStoneProxySpawner");
            changed = true;
        }

        TerrainTreeProxySpawner spawner = spawnerObject.GetComponent<TerrainTreeProxySpawner>();
        if (spawner == null)
        {
            spawner = spawnerObject.AddComponent<TerrainTreeProxySpawner>();
            changed = true;
        }

        SerializedObject serializedSpawner = new SerializedObject(spawner);
        changed |= SetBool(serializedSpawner, "spawnOnStart", true);
        changed |= SetBool(serializedSpawner, "removePaintedProxyInstances", true);
        changed |= SetBool(serializedSpawner, "cloneTerrainDataDuringPlayInEditor", true);
        changed |= SetBool(serializedSpawner, "logConversionSummary", true);
        changed |= SetString(serializedSpawner, "spawnParentName", "ConvertedTerrainTrees");
        changed |= SetBool(serializedSpawner, "createSpawnParentIfMissing", true);
        changed |= SetBool(serializedSpawner, "snapSpawnedObjectsToTerrain", true);
        changed |= SetBool(serializedSpawner, "alignSpawnedObjectsToTerrainNormal", false);
        changed |= SetFloat(serializedSpawner, "terrainSurfaceOffset", 0f);

        GameObject convertedRoot = GameObject.Find("ConvertedTerrainTrees");
        changed |= SetObjectReference(serializedSpawner, "spawnParent", convertedRoot != null ? convertedRoot.transform : null);

        SerializedProperty mappingsProperty = serializedSpawner.FindProperty("mappings");
        if (mappingsProperty != null && mappingsProperty.isArray)
        {
            if (mappingsProperty.arraySize != setups.Count)
            {
                mappingsProperty.arraySize = setups.Count;
                changed = true;
            }

            for (int i = 0; i < setups.Count; i++)
            {
                StoneProxySetup setup = setups[i];
                SerializedProperty mappingProperty = mappingsProperty.GetArrayElementAtIndex(i);
                SerializedProperty proxyProperty = mappingProperty.FindPropertyRelative("terrainProxyPrefab");
                SerializedProperty runtimeProperty = mappingProperty.FindPropertyRelative("runtimePrefab");

                if (proxyProperty != null && proxyProperty.objectReferenceValue != setup.proxyPrefab)
                {
                    proxyProperty.objectReferenceValue = setup.proxyPrefab;
                    changed = true;
                }

                if (runtimeProperty != null && runtimeProperty.objectReferenceValue != setup.realPrefab)
                {
                    runtimeProperty.objectReferenceValue = setup.realPrefab;
                    changed = true;
                }
            }
        }

        serializedSpawner.ApplyModifiedPropertiesWithoutUndo();

        if (changed)
        {
            EditorUtility.SetDirty(spawnerObject);
            EditorUtility.SetDirty(spawner);
        }

        return changed;
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parentPath = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        if (!string.IsNullOrWhiteSpace(parentPath) && !AssetDatabase.IsValidFolder(parentPath))
        {
            EnsureFolderExists(parentPath);
        }

        string folderName = Path.GetFileName(folderPath);
        AssetDatabase.CreateFolder(parentPath, folderName);
    }

    private static bool SetBool(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.boolValue == value)
        {
            return false;
        }

        property.boolValue = value;
        return true;
    }

    private static bool SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.stringValue == value)
        {
            return false;
        }

        property.stringValue = value;
        return true;
    }

    private static bool SetFloat(SerializedObject serializedObject, string propertyName, float value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || Mathf.Approximately(property.floatValue, value))
        {
            return false;
        }

        property.floatValue = value;
        return true;
    }

    private static bool SetObjectReference(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == value)
        {
            return false;
        }

        property.objectReferenceValue = value;
        return true;
    }
}
