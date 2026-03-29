using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class TerrainPerformanceFixTools
{
    // Keeps visible tree prefabs active while clamping terrain rendering and disabling converted clutter.
    [MenuItem("Tools/Terrain/Fix Terrain Conversion Performance")]
    public static void FixTerrainConversionPerformance()
    {
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        if (buildScenes == null || buildScenes.Length == 0)
        {
            Debug.LogWarning("TerrainPerformanceFixTools: No enabled build scenes found.");
            return;
        }

        int processedScenes = 0;
        int adjustedConverters = 0;
        int optimizedRoots = 0;
        int deactivatedChildren = 0;
        int renderersAdjusted = 0;
        int terrainsAdjusted = 0;
        int lightsAdjusted = 0;
        int runtimeOptimizersConfigured = 0;

        for (int i = 0; i < buildScenes.Length; i++)
        {
            EditorBuildSettingsScene buildScene = buildScenes[i];
            if (buildScene == null || !buildScene.enabled || string.IsNullOrWhiteSpace(buildScene.path))
            {
                continue;
            }

            var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            processedScenes++;
            bool sceneDirty = false;

#if UNITY_2023_1_OR_NEWER
            TerrainTreeToPrefabConverter[] converters = Object.FindObjectsByType<TerrainTreeToPrefabConverter>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            TerrainTreeToPrefabConverter[] converters = Object.FindObjectsOfType<TerrainTreeToPrefabConverter>(true);
#endif

            for (int converterIndex = 0; converterIndex < converters.Length; converterIndex++)
            {
                TerrainTreeToPrefabConverter converter = converters[converterIndex];
                if (converter == null)
                {
                    continue;
                }

                SerializedObject serializedConverter = new SerializedObject(converter);
                SetBool(serializedConverter, "convertPaintedTreesOnStart", false);
                SetBool(serializedConverter, "convertOnlyCuttableTrees", true);
                SetBool(serializedConverter, "convertDetailMeshes", false);
                SetBool(serializedConverter, "debugDetailConversion", false);
                serializedConverter.ApplyModifiedPropertiesWithoutUndo();

                converter.enabled = false;
                EditorUtility.SetDirty(converter);
                adjustedConverters++;
                sceneDirty = true;

                SerializedProperty parentProperty = serializedConverter.FindProperty("parentForSpawnedTrees");
                Transform parent = parentProperty != null ? parentProperty.objectReferenceValue as Transform : null;
                if (parent != null)
                {
                    if (!parent.gameObject.activeSelf)
                    {
                        parent.gameObject.SetActive(true);
                        EditorUtility.SetDirty(parent.gameObject);
                        sceneDirty = true;
                    }

                    int rootDeactivatedChildren = OptimizeConvertedRoot(parent);
                    if (rootDeactivatedChildren > 0)
                    {
                        deactivatedChildren += rootDeactivatedChildren;
                        optimizedRoots++;
                        sceneDirty = true;
                    }

                    int rootRenderersAdjusted = OptimizeConvertedRootRenderers(parent);
                    if (rootRenderersAdjusted > 0)
                    {
                        renderersAdjusted += rootRenderersAdjusted;
                        sceneDirty = true;
                    }
                }
            }

            int sceneTerrainsAdjusted = OptimizeTerrainsInScene();
            terrainsAdjusted += sceneTerrainsAdjusted;
            if (sceneTerrainsAdjusted > 0)
            {
                sceneDirty = true;
            }

            int sceneLightsAdjusted = OptimizeLightsInScene();
            lightsAdjusted += sceneLightsAdjusted;
            if (sceneLightsAdjusted > 0)
            {
                sceneDirty = true;
            }

            if (EnsureRuntimeOptimizerConfigured())
            {
                runtimeOptimizersConfigured++;
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
            $"TerrainPerformanceFixTools: Completed. Scenes={processedScenes}, " +
            $"ConvertersAdjusted={adjustedConverters}, OptimizedRoots={optimizedRoots}, " +
            $"DeactivatedChildren={deactivatedChildren}, RenderersAdjusted={renderersAdjusted}, " +
            $"TerrainsAdjusted={terrainsAdjusted}, LightsAdjusted={lightsAdjusted}, " +
            $"RuntimeOptimizersConfigured={runtimeOptimizersConfigured}.");
    }

    public static void FixTerrainConversionPerformanceBatchMode()
    {
        FixTerrainConversionPerformance();
    }

    private static int OptimizeConvertedRoot(Transform root)
    {
        if (root == null)
        {
            return 0;
        }

        int changed = 0;
        int childCount = root.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null)
            {
                continue;
            }

            GameObject childObject = child.gameObject;
            bool shouldStayActive = ShouldKeepConvertedChild(childObject);
            if (childObject.activeSelf != shouldStayActive)
            {
                childObject.SetActive(shouldStayActive);
                EditorUtility.SetDirty(childObject);
                if (!shouldStayActive)
                {
                    changed++;
                }
            }
        }

        return changed;
    }

    private static bool ShouldKeepConvertedChild(GameObject candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        if (candidate.GetComponentInChildren<CutTree>(true) != null)
        {
            return true;
        }

        string normalizedName = candidate.name.ToLowerInvariant();
        if (normalizedName.Contains("tree") || normalizedName.Contains("bamboo"))
        {
            return true;
        }

        if (candidate.GetComponentInChildren<MineStone>(true) != null)
        {
            return false;
        }

        if (candidate.GetComponentInChildren<StoneColliderScript>(true) != null)
        {
            return false;
        }

        if (candidate.GetComponentInChildren<InventoryItem>(true) != null)
        {
            return false;
        }

        return false;
    }

    private static int OptimizeTerrainsInScene()
    {
#if UNITY_2023_1_OR_NEWER
        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        Terrain[] terrains = Object.FindObjectsOfType<Terrain>(true);
#endif

        int adjusted = 0;
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null)
            {
                continue;
            }

            bool changed = false;
            changed |= SetFloatIfDifferent(() => terrain.treeDistance, value => terrain.treeDistance = value, 1200f);
            changed |= SetFloatIfDifferent(() => terrain.treeBillboardDistance, value => terrain.treeBillboardDistance = value, 80f);
            changed |= SetIntIfDifferent(() => terrain.treeMaximumFullLODCount, value => terrain.treeMaximumFullLODCount = value, 40);
            changed |= SetFloatIfDifferent(() => terrain.detailObjectDistance, value => terrain.detailObjectDistance = value, 35f);
            changed |= SetFloatIfDifferent(() => terrain.detailObjectDensity, value => terrain.detailObjectDensity = value, 0.35f);
            changed |= SetFloatIfDifferent(() => terrain.heightmapPixelError, value => terrain.heightmapPixelError = value, 10f);
            changed |= SetBoolIfDifferent(() => terrain.drawInstanced, value => terrain.drawInstanced = value, true);

            if (changed)
            {
                EditorUtility.SetDirty(terrain);
                adjusted++;
            }
        }

        return adjusted;
    }

    private static int OptimizeConvertedRootRenderers(Transform root)
    {
        if (root == null)
        {
            return 0;
        }

        int adjusted = 0;
        int childCount = root.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null || !child.gameObject.activeSelf)
            {
                continue;
            }

            MeshRenderer[] renderers = child.GetComponentsInChildren<MeshRenderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                MeshRenderer renderer = renderers[rendererIndex];
                if (renderer == null)
                {
                    continue;
                }

                bool changed = false;
                if (renderer.shadowCastingMode != ShadowCastingMode.Off)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    changed = true;
                }

                if (renderer.receiveShadows)
                {
                    renderer.receiveShadows = false;
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(renderer);
                    adjusted++;
                }
            }
        }

        return adjusted;
    }

    private static int OptimizeLightsInScene()
    {
#if UNITY_2023_1_OR_NEWER
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        Light[] lights = Object.FindObjectsOfType<Light>(true);
#endif

        int adjusted = 0;
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null)
            {
                continue;
            }

            bool changed = false;
            if (light.type == LightType.Directional)
            {
                if (light.shadows != LightShadows.Hard)
                {
                    light.shadows = LightShadows.Hard;
                    changed = true;
                }

                if (light.shadowStrength > 0.55f)
                {
                    light.shadowStrength = 0.55f;
                    changed = true;
                }
            }
            else
            {
                if (light.shadows != LightShadows.None)
                {
                    light.shadows = LightShadows.None;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(light);
                adjusted++;
            }
        }

        return adjusted;
    }

    private static bool EnsureRuntimeOptimizerConfigured()
    {
        GameObject optimizerObject = GameObject.Find("SceneRuntimeOptimizer");
        bool changed = false;

        if (optimizerObject == null)
        {
            optimizerObject = new GameObject("SceneRuntimeOptimizer");
            changed = true;
        }

        ResHandler optimizer = optimizerObject.GetComponent<ResHandler>();
        if (optimizer == null)
        {
            optimizer = optimizerObject.AddComponent<ResHandler>();
            changed = true;
        }

        SerializedObject serializedOptimizer = new SerializedObject(optimizer);
        changed |= SetBool(serializedOptimizer, "forceResolutionOnStart", false);
        changed |= SetBool(serializedOptimizer, "useHighestRefreshRate", true);
        changed |= SetInt(serializedOptimizer, "targetWidth", 1920);
        changed |= SetInt(serializedOptimizer, "targetHeight", 1080);
        changed |= SetInt(serializedOptimizer, "targetRefreshRate", 0);
        changed |= SetEnum(serializedOptimizer, "fullscreenMode", (int)FullScreenMode.ExclusiveFullScreen);

        Transform[] managedRoots = FindManagedRoots();
        changed |= SetObjectArray(serializedOptimizer, "managedRoots", managedRoots);
        changed |= SetBool(serializedOptimizer, "includeInactiveChildren", true);
        changed |= SetBool(serializedOptimizer, "useGameObjectCulling", true);
        changed |= SetBool(serializedOptimizer, "useRendererCullingFallback", false);
        changed |= SetFloat(serializedOptimizer, "treeRenderDistance", 160f);
        changed |= SetFloat(serializedOptimizer, "treeShadowDistance", 55f);
        changed |= SetFloat(serializedOptimizer, "cullingUpdateInterval", 0.12f);
        changed |= SetInt(serializedOptimizer, "maxObjectsProcessedPerTick", 1200);
        changed |= SetInt(serializedOptimizer, "maxRenderersProcessedPerTick", 800);
        changed |= SetInt(serializedOptimizer, "initialCullBatchSize", 3000);

        changed |= SetBool(serializedOptimizer, "enforceDistanceCaps", true);
        changed |= SetFloat(serializedOptimizer, "hardMaxRenderDistance", 220f);
        changed |= SetFloat(serializedOptimizer, "hardMaxShadowDistance", 90f);
        changed |= SetBool(serializedOptimizer, "clampCameraFarClip", true);
        changed |= SetFloat(serializedOptimizer, "cameraFarClipDistance", 300f);

        changed |= SetBool(serializedOptimizer, "applyGlobalQualityClamps", true);
        changed |= SetBool(serializedOptimizer, "applyGlobalShadowDistance", true);
        changed |= SetFloat(serializedOptimizer, "globalShadowDistance", 30f);
        changed |= SetFloat(serializedOptimizer, "qualityLodBias", 0.8f);
        changed |= SetFloat(serializedOptimizer, "terrainDetailDensityScale", 0.35f);
        changed |= SetFloat(serializedOptimizer, "terrainDetailDistance", 35f);
        changed |= SetFloat(serializedOptimizer, "terrainTreeDistance", 1200f);
        changed |= SetFloat(serializedOptimizer, "terrainBillboardStart", 60f);

        changed |= SetBool(serializedOptimizer, "optimizeRealtimeLights", true);
        changed |= SetFloat(serializedOptimizer, "lightsUpdateInterval", 0.2f);
        changed |= SetFloat(serializedOptimizer, "nonDirectionalLightDistance", 40f);
        changed |= SetFloat(serializedOptimizer, "nonDirectionalShadowDistance", 12f);
        changed |= SetInt(serializedOptimizer, "maxShadowedNonDirectionalLights", 0);
        changed |= SetBool(serializedOptimizer, "disableShadowsOnDisabledLights", true);

        changed |= SetBool(serializedOptimizer, "adaptiveDistanceByFps", false);
        changed |= SetFloat(serializedOptimizer, "lowFpsThreshold", 40f);
        changed |= SetFloat(serializedOptimizer, "highFpsThreshold", 80f);
        changed |= SetFloat(serializedOptimizer, "adaptiveCheckInterval", 1f);
        changed |= SetFloat(serializedOptimizer, "adaptiveStep", 20f);
        changed |= SetFloat(serializedOptimizer, "adaptiveMinRenderDistance", 80f);
        changed |= SetFloat(serializedOptimizer, "adaptiveMaxRenderDistance", 220f);
        changed |= SetFloat(serializedOptimizer, "adaptiveMinShadowDistance", 35f);
        changed |= SetFloat(serializedOptimizer, "adaptiveMaxShadowDistance", 100f);

        serializedOptimizer.ApplyModifiedPropertiesWithoutUndo();

        if (changed)
        {
            EditorUtility.SetDirty(optimizerObject);
            EditorUtility.SetDirty(optimizer);
        }

        return changed;
    }

    private static Transform[] FindManagedRoots()
    {
        System.Collections.Generic.List<Transform> found = new System.Collections.Generic.List<Transform>(4);
        TryAddManagedRoot(found, "ConvertedTerrainTrees");
        TryAddManagedRoot(found, "Trees&stones");
        TryAddManagedRoot(found, "Trees");
        TryAddManagedRoot(found, "Lamps");
        return found.ToArray();
    }

    private static void TryAddManagedRoot(System.Collections.Generic.List<Transform> found, string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null)
        {
            found.Add(target.transform);
        }
    }

    private static bool SetFloatIfDifferent(System.Func<float> getter, System.Action<float> setter, float value)
    {
        if (Mathf.Approximately(getter(), value))
        {
            return false;
        }

        setter(value);
        return true;
    }

    private static bool SetIntIfDifferent(System.Func<int> getter, System.Action<int> setter, int value)
    {
        if (getter() == value)
        {
            return false;
        }

        setter(value);
        return true;
    }

    private static bool SetBoolIfDifferent(System.Func<bool> getter, System.Action<bool> setter, bool value)
    {
        if (getter() == value)
        {
            return false;
        }

        setter(value);
        return true;
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

    private static bool SetInt(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.intValue == value)
        {
            return false;
        }

        property.intValue = value;
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

    private static bool SetEnum(SerializedObject serializedObject, string propertyName, int value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || property.enumValueIndex == value)
        {
            return false;
        }

        property.enumValueIndex = value;
        return true;
    }

    private static bool SetObjectArray(SerializedObject serializedObject, string propertyName, Object[] values)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property == null || !property.isArray)
        {
            return false;
        }

        bool changed = property.arraySize != values.Length;
        if (property.arraySize != values.Length)
        {
            property.arraySize = values.Length;
        }

        for (int i = 0; i < values.Length; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            if (element.objectReferenceValue != values[i])
            {
                element.objectReferenceValue = values[i];
                changed = true;
            }
        }

        return changed;
    }
}
