using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Controls Terrain Tree To Prefab Converter behavior.
public class TerrainTreeToPrefabConverter : MonoBehaviour
{
    [Header("Converted Resource References")]
    public InventoryAddHandler inventoryAddHandler;
   
    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private Transform parentForSpawnedTrees;
    [Header("Placement")]
    [SerializeField] private bool snapConvertedObjectsToTerrain = true;
    [SerializeField] private bool alignConvertedObjectsToTerrainNormal = false;
    [SerializeField] private float terrainSurfaceOffset = 0f;
    [SerializeField, Min(0f)] private float mineStoneEmbedDepth = 0.2f;
    [Header("Prototype Rotation")]
    [SerializeField] private bool includeBranch01WhenFilteringResources = true;
    [SerializeField] private bool randomizeBranch01Yaw = true;
    [SerializeField] private string branch01PrototypeName = "Branch_01";
    [SerializeField] private int branch01RotationSeed = 9143;
    [Header("Auto Conversion")]
    [SerializeField] private bool convertPaintedTreesOnStart = true;
    [Tooltip("If enabled, converter will only convert resource prefabs (CutTree/MineStone).")]
    [SerializeField] private bool convertOnlyCuttableTrees = true;
    [SerializeField] private bool includeMineableStones = true;
    [SerializeField] private bool includePickableItems = true;
    [SerializeField] private bool clearConvertedTerrainTrees = true;
    [Header("Reverse Conversion")]
    [Tooltip("When restoring prefabs back to terrain trees, include existing terrain trees instead of replacing them.")]
    [SerializeField] private bool appendToExistingTerrainTrees = true;
    [Tooltip("Use all descendants under parentForSpawnedTrees. Disable to use only direct children.")]
    [SerializeField] private bool restoreFromAllDescendants = false;
    [SerializeField] private bool includeInactiveForRestore = true;
    [SerializeField] private bool removeRestoredGameObjects = true;
    [SerializeField] private bool clearParentAfterRestore = false;
    [Header("Detail Mesh Conversion")]
    [SerializeField] private bool convertDetailMeshes = false;
    [Tooltip("Only convert detail prototypes that match these prefabs.")]
    [SerializeField] private List<GameObject> detailPrefabsToConvert = new List<GameObject>();
    [SerializeField] private bool clearConvertedDetailMeshes = true;
    [SerializeField] private bool debugDetailConversion = false;
    [SerializeField] private int maxDetailInstancesTotal = 5000;
    [SerializeField] private int maxDetailInstancesPerCell = 1;
    [SerializeField] private int detailPlacementSeed = 1337;
    [SerializeField] private bool deterministicDetailPlacement = true;
    [SerializeField, Range(0f, 1f)] private float detailCellJitter = 0.6f;

    private static InventoryAddHandler cachedInventoryAddHandler;

    private void Start()
    {
        if (!Application.isPlaying || !convertPaintedTreesOnStart)
        {
            return;
        }

        ConvertPaintedTreesToPrefabs();
    }

    [ContextMenu("Convert Painted Trees To Prefabs")]
    // Handle Convert Painted Trees To Prefabs.
    public void ConvertPaintedTreesToPrefabs()
    {
        if (targetTerrain == null)
        {
            targetTerrain = GetComponent<Terrain>();
        }

        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            Debug.LogWarning("TerrainTreeToPrefabConverter: Missing Terrain reference.", this);
            return;
        }

        if (parentForSpawnedTrees == null)
        {
            GameObject root = new GameObject("ConvertedTerrainTrees");
            root.transform.SetParent(targetTerrain.transform, false);
            parentForSpawnedTrees = root.transform;
        }

        TerrainData terrainData = targetTerrain.terrainData;
        TreePrototype[] prototypes = terrainData.treePrototypes;
        TreeInstance[] treeInstances = terrainData.treeInstances;

        bool hasTreeInstances = treeInstances != null && treeInstances.Length > 0;
        if (!hasTreeInstances)
        {
            Debug.Log("TerrainTreeToPrefabConverter: No painted terrain trees to convert.", this);
        }

        List<TreeInstance> remainingInstances = hasTreeInstances
            ? new List<TreeInstance>(treeInstances.Length)
            : new List<TreeInstance>();
        int convertedCount = 0;

        for (int i = 0; i < (hasTreeInstances ? treeInstances.Length : 0); i++)
        {
            TreeInstance instance = treeInstances[i];

            if (instance.prototypeIndex < 0 || instance.prototypeIndex >= prototypes.Length)
            {
                remainingInstances.Add(instance);
                continue;
            }

            GameObject prototypePrefab = prototypes[instance.prototypeIndex].prefab;
            if (prototypePrefab == null)
            {
                remainingInstances.Add(instance);
                continue;
            }

            if (convertOnlyCuttableTrees)
            {
                bool hasCutTree = prototypePrefab.GetComponentInChildren<CutTree>(true) != null;
                bool hasMineStone = includeMineableStones &&
                    (prototypePrefab.GetComponentInChildren<MineStone>(true) != null ||
                     prototypePrefab.GetComponentInChildren<StoneColliderScript>(true) != null);
                bool hasPickableItem = includePickableItems &&
                    prototypePrefab.GetComponentInChildren<InventoryItem>(true) != null;
                bool isAllowedBranch01 = includeBranch01WhenFilteringResources &&
                    MatchesPrototypeName(prototypePrefab, branch01PrototypeName);

                if (!hasCutTree && !hasMineStone && !hasPickableItem && !isAllowedBranch01)
                {
                    remainingInstances.Add(instance);
                    continue;
                }
            }

            Vector3 worldPosition = targetTerrain.transform.position + Vector3.Scale(instance.position, terrainData.size);
            Quaternion worldRotation = GetConvertedTreeRotation(instance, prototypePrefab, i);

            GameObject spawnedTree = Instantiate(prototypePrefab, worldPosition, worldRotation, parentForSpawnedTrees);
            AssignConvertedObjectReferences(spawnedTree);

            Vector3 baseScale = spawnedTree.transform.localScale;
            spawnedTree.transform.localScale = new Vector3(
                baseScale.x * instance.widthScale,
                baseScale.y * instance.heightScale,
                baseScale.z * instance.widthScale
            );
            ApplyTerrainPlacement(spawnedTree.transform, targetTerrain);

            convertedCount++;

            if (!clearConvertedTerrainTrees)
            {
                remainingInstances.Add(instance);
            }
        }

        if (clearConvertedTerrainTrees && hasTreeInstances)
        {
            terrainData.treeInstances = remainingInstances.ToArray();
        }

        if (hasTreeInstances)
        {
            Debug.Log($"TerrainTreeToPrefabConverter: Converted {convertedCount} tree instances.", this);
        }

        if (convertDetailMeshes)
        {
            ConvertDetailMeshesToPrefabs(targetTerrain, parentForSpawnedTrees);
        }
    }

    [ContextMenu("Convert Prefabs Back To Terrain Trees")]
    // Handle Convert Prefabs Back To Terrain Trees.
    public void ConvertPrefabsBackToTerrainTrees()
    {
        if (targetTerrain == null)
        {
            targetTerrain = GetComponent<Terrain>();
        }

        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            Debug.LogWarning("TerrainTreeToPrefabConverter: Missing Terrain reference for restore.", this);
            return;
        }

        if (parentForSpawnedTrees == null)
        {
            Debug.LogWarning("TerrainTreeToPrefabConverter: parentForSpawnedTrees is not set for restore.", this);
            return;
        }

        TerrainData terrainData = targetTerrain.terrainData;
        TreePrototype[] prototypes = terrainData.treePrototypes;
        if (prototypes == null || prototypes.Length == 0)
        {
            Debug.LogWarning("TerrainTreeToPrefabConverter: Terrain has no tree prototypes.", this);
            return;
        }

        Dictionary<string, int> prototypeByName = BuildPrototypeNameMap(prototypes);
        List<TreeInstance> result = appendToExistingTerrainTrees
            ? new List<TreeInstance>(terrainData.treeInstances)
            : new List<TreeInstance>();

        List<GameObject> candidates = CollectRestoreCandidates(parentForSpawnedTrees, includeInactiveForRestore, restoreFromAllDescendants);
        int restoredCount = 0;
        int skippedCount = 0;
        int outsideTerrainCount = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            GameObject candidate = candidates[i];
            if (candidate == null)
            {
                skippedCount++;
                continue;
            }

            int prototypeIndex = ResolvePrototypeIndexForRestoredObject(candidate, prototypes, prototypeByName);
            if (prototypeIndex < 0)
            {
                skippedCount++;
                continue;
            }

            if (!TryCreateTreeInstanceFromObject(candidate.transform, targetTerrain, prototypes, prototypeIndex, out TreeInstance treeInstance))
            {
                outsideTerrainCount++;
                continue;
            }

            result.Add(treeInstance);
            restoredCount++;

            if (removeRestoredGameObjects)
            {
                if (Application.isPlaying)
                {
                    Destroy(candidate);
                }
                else
                {
                    DestroyImmediate(candidate);
                }
            }
        }

        terrainData.treeInstances = result.ToArray();

        if (removeRestoredGameObjects && clearParentAfterRestore && parentForSpawnedTrees != null)
        {
            int childCount = parentForSpawnedTrees.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Transform child = parentForSpawnedTrees.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        Debug.Log(
            $"TerrainTreeToPrefabConverter: Restored {restoredCount} prefab objects back to terrain trees. " +
            $"Skipped={skippedCount}, OutsideTerrain={outsideTerrainCount}, TotalTerrainTrees={result.Count}.",
            this);
    }

    [ContextMenu("Snap Existing Converted Objects To Terrain")]
    // Handle Snap Existing Converted Objects To Terrain.
    public void SnapExistingConvertedObjectsToTerrain()
    {
        if (targetTerrain == null)
        {
            targetTerrain = GetComponent<Terrain>();
        }

        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            Debug.LogWarning("TerrainTreeToPrefabConverter: Missing Terrain reference for snap.", this);
            return;
        }

        if (parentForSpawnedTrees == null)
        {
            Debug.LogWarning("TerrainTreeToPrefabConverter: parentForSpawnedTrees is not set for snap.", this);
            return;
        }

        List<GameObject> candidates = CollectRestoreCandidates(parentForSpawnedTrees, true, false);
        int snappedCount = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            GameObject candidate = candidates[i];
            if (candidate == null)
            {
                continue;
            }

            ApplyTerrainPlacement(candidate.transform, targetTerrain);
            snappedCount++;
        }

        Debug.Log($"TerrainTreeToPrefabConverter: Snapped {snappedCount} converted objects to terrain.", this);
    }

    private void ConvertDetailMeshesToPrefabs(Terrain terrain, Transform parent)
    {
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogWarning("TerrainTreeToPrefabConverter: Missing Terrain reference for detail conversion.", this);
            return;
        }

        bool hasExplicitDetailPrefabFilter = detailPrefabsToConvert != null && detailPrefabsToConvert.Count > 0;
        bool canAutoConvertPickableDetails = includePickableItems;
        if (!hasExplicitDetailPrefabFilter && !canAutoConvertPickableDetails)
        {
            Debug.LogWarning("TerrainTreeToPrefabConverter: No detail prefabs assigned for conversion and pickable detail auto-convert is disabled.", this);
            return;
        }

        TerrainData data = terrain.terrainData;
        DetailPrototype[] detailPrototypes = data.detailPrototypes;
        if (detailPrototypes == null || detailPrototypes.Length == 0)
        {
            Debug.Log("TerrainTreeToPrefabConverter: No detail prototypes found.", this);
            return;
        }

        int detailWidth = data.detailWidth;
        int detailHeight = data.detailHeight;
        if (detailWidth <= 0 || detailHeight <= 0)
        {
            Debug.LogWarning("TerrainTreeToPrefabConverter: Detail resolution is invalid.", this);
            return;
        }

        GameObject detailRoot = null;
        if (parent == null)
        {
            GameObject root = new GameObject("ConvertedTerrainDetails");
            root.transform.SetParent(terrain.transform, false);
            detailRoot = root;
        }
        else
        {
            detailRoot = parent.gameObject;
        }

        int converted = 0;
        float stepX = data.size.x / detailWidth;
        float stepZ = data.size.z / detailHeight;

        for (int protoIndex = 0; protoIndex < detailPrototypes.Length; protoIndex++)
        {
            DetailPrototype proto = detailPrototypes[protoIndex];
            GameObject protoPrefab = GetDetailPrototypePrefab(proto);
            if (protoPrefab == null)
            {
                if (debugDetailConversion)
                {
                    Debug.Log($"TerrainTreeToPrefabConverter: Detail prototype {protoIndex} has no prefab (skip).", this);
                }
                continue;
            }

            bool shouldConvert = ShouldConvertDetailPrefab(protoPrefab);
            if (debugDetailConversion)
            {
                Debug.Log($"TerrainTreeToPrefabConverter: Detail prototype {protoIndex} prefab '{protoPrefab.name}' convert={shouldConvert}.", this);
            }

            if (!shouldConvert)
            {
                continue;
            }

            int[,] layer = data.GetDetailLayer(0, 0, detailWidth, detailHeight, protoIndex);
            int layerCount = debugDetailConversion ? SumDetailLayer(layer) : 0;
            if (debugDetailConversion)
            {
                Debug.Log($"TerrainTreeToPrefabConverter: Detail prototype {protoIndex} count={layerCount}.", this);
            }
            bool anyConverted = false;

            for (int z = 0; z < detailHeight; z++)
            {
                for (int x = 0; x < detailWidth; x++)
                {
                    int count = layer[z, x];
                    if (count <= 0)
                    {
                        continue;
                    }

                    anyConverted = true;
                    int cappedCount = Mathf.Min(count, Mathf.Max(1, maxDetailInstancesPerCell));
                    for (int i = 0; i < cappedCount; i++)
                    {
                        if (maxDetailInstancesTotal > 0 && converted >= maxDetailInstancesTotal)
                        {
                            goto Done;
                        }

                        float jitterX;
                        float jitterZ;
                        float rotY;
                        float widthScale;
                        float heightScale;

                        if (deterministicDetailPlacement)
                        {
                            float h1 = HashTo01(detailPlacementSeed, protoIndex, x, z, i, 1);
                            float h2 = HashTo01(detailPlacementSeed, protoIndex, x, z, i, 2);
                            float h3 = HashTo01(detailPlacementSeed, protoIndex, x, z, i, 3);
                            float h4 = HashTo01(detailPlacementSeed, protoIndex, x, z, i, 4);
                            float h5 = HashTo01(detailPlacementSeed, protoIndex, x, z, i, 5);

                            jitterX = Mathf.Lerp(0.5f - detailCellJitter * 0.5f, 0.5f + detailCellJitter * 0.5f, h1);
                            jitterZ = Mathf.Lerp(0.5f - detailCellJitter * 0.5f, 0.5f + detailCellJitter * 0.5f, h2);
                            rotY = h3 * 360f;
                            widthScale = Mathf.Lerp(proto.minWidth, proto.maxWidth, h4);
                            heightScale = Mathf.Lerp(proto.minHeight, proto.maxHeight, h5);
                        }
                        else
                        {
                            jitterX = Random.value;
                            jitterZ = Random.value;
                            rotY = Random.Range(0f, 360f);
                            widthScale = Random.Range(proto.minWidth, proto.maxWidth);
                            heightScale = Random.Range(proto.minHeight, proto.maxHeight);
                        }

                        float offsetX = (x + jitterX) * stepX;
                        float offsetZ = (z + jitterZ) * stepZ;
                        Vector3 worldPos = terrain.transform.position + new Vector3(offsetX, 0f, offsetZ);
                        worldPos.y = terrain.SampleHeight(worldPos) + terrain.transform.position.y;

                        Quaternion worldRot = Quaternion.Euler(0f, rotY, 0f);
                        GameObject spawned = Instantiate(protoPrefab, worldPos, worldRot, detailRoot.transform);
                        AssignConvertedObjectReferences(spawned);

                        Vector3 baseScale = spawned.transform.localScale;
                        spawned.transform.localScale = new Vector3(baseScale.x * widthScale, baseScale.y * heightScale, baseScale.z * widthScale);
                        ApplyTerrainPlacement(spawned.transform, terrain);

                        converted++;
                    }

                    if (clearConvertedDetailMeshes)
                    {
                        layer[z, x] = 0;
                    }
                }
            }

            if (clearConvertedDetailMeshes && anyConverted)
            {
                data.SetDetailLayer(0, 0, protoIndex, layer);
            }
        }

Done:
        Debug.Log($"TerrainTreeToPrefabConverter: Converted {converted} detail mesh instances.", this);
    }

    private static int SumDetailLayer(int[,] layer)
    {
        if (layer == null)
        {
            return 0;
        }

        int total = 0;
        int width = layer.GetLength(1);
        int height = layer.GetLength(0);
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                total += layer[z, x];
            }
        }

        return total;
    }

    private bool ShouldConvertDetailPrefab(GameObject protoPrefab)
    {
        if (protoPrefab == null)
        {
            return false;
        }

        if (includePickableItems && protoPrefab.GetComponentInChildren<InventoryItem>(true) != null)
        {
            return true;
        }

        if (detailPrefabsToConvert == null || detailPrefabsToConvert.Count == 0)
        {
            return false;
        }

        if (detailPrefabsToConvert.Contains(protoPrefab))
        {
            return true;
        }

        string protoName = NormalizePrefabName(protoPrefab.name);
        for (int i = 0; i < detailPrefabsToConvert.Count; i++)
        {
            GameObject candidate = detailPrefabsToConvert[i];
            if (candidate == null)
            {
                continue;
            }

            if (NormalizePrefabName(candidate.name) == protoName)
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizePrefabName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        string name = rawName.Trim().ToLowerInvariant();
        name = name.Replace("(clone)", string.Empty).Trim();
        name = name.Replace(" variant", string.Empty).Trim();
        return name;
    }

    private static GameObject GetDetailPrototypePrefab(DetailPrototype proto)
    {
        if (proto == null)
        {
            return null;
        }

#if UNITY_2021_2_OR_NEWER
        if (proto.usePrototypeMesh && proto.prototype != null)
        {
            return proto.prototype;
        }
#else
        if (proto.prototype != null)
        {
            return proto.prototype;
        }
#endif

        return null;
    }

    private static Dictionary<string, int> BuildPrototypeNameMap(TreePrototype[] prototypes)
    {
        Dictionary<string, int> map = new Dictionary<string, int>();
        if (prototypes == null)
        {
            return map;
        }

        for (int i = 0; i < prototypes.Length; i++)
        {
            TreePrototype proto = prototypes[i];
            if (proto == null || proto.prefab == null)
            {
                continue;
            }

            string key = NormalizePrefabName(proto.prefab.name);
            if (!map.ContainsKey(key))
            {
                map.Add(key, i);
            }
        }

        return map;
    }

    private static List<GameObject> CollectRestoreCandidates(Transform root, bool includeInactive, bool useAllDescendants)
    {
        List<GameObject> result = new List<GameObject>();
        if (root == null)
        {
            return result;
        }

        if (!useAllDescendants)
        {
            int childCount = root.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (!includeInactive && !child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                result.Add(child.gameObject);
            }

            return result;
        }

        Transform[] descendants = root.GetComponentsInChildren<Transform>(includeInactive);
        for (int i = 0; i < descendants.Length; i++)
        {
            Transform t = descendants[i];
            if (t == null || t == root)
            {
                continue;
            }

            // Keep only top-most descendants under root to avoid duplicating nested prefab parts.
            if (t.parent != root)
            {
                continue;
            }

            result.Add(t.gameObject);
        }

        return result;
    }

    private static int ResolvePrototypeIndexForRestoredObject(GameObject candidate, TreePrototype[] prototypes, Dictionary<string, int> prototypeByName)
    {
        if (candidate == null)
        {
            return -1;
        }

#if UNITY_EDITOR
        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(candidate);
        if (source != null)
        {
            for (int i = 0; i < prototypes.Length; i++)
            {
                TreePrototype proto = prototypes[i];
                if (proto != null && proto.prefab == source)
                {
                    return i;
                }
            }
        }
#endif

        string normalized = NormalizePrefabName(candidate.name);
        return prototypeByName.TryGetValue(normalized, out int mappedIndex)
            ? mappedIndex
            : -1;
    }

    private static bool TryCreateTreeInstanceFromObject(
        Transform source,
        Terrain terrain,
        TreePrototype[] prototypes,
        int prototypeIndex,
        out TreeInstance treeInstance)
    {
        treeInstance = default;

        if (source == null || terrain == null || terrain.terrainData == null)
        {
            return false;
        }

        TerrainData data = terrain.terrainData;
        Vector3 local = source.position - terrain.transform.position;
        Vector3 normalized = new Vector3(
            data.size.x > 0f ? local.x / data.size.x : 0f,
            data.size.y > 0f ? local.y / data.size.y : 0f,
            data.size.z > 0f ? local.z / data.size.z : 0f);

        if (normalized.x < 0f || normalized.x > 1f || normalized.z < 0f || normalized.z > 1f)
        {
            return false;
        }

        normalized.x = Mathf.Clamp01(normalized.x);
        normalized.y = Mathf.Clamp01(normalized.y);
        normalized.z = Mathf.Clamp01(normalized.z);

        Vector3 sourceScale = source.localScale;
        Vector3 prefabScale = Vector3.one;
        TreePrototype prototype = prototypes[prototypeIndex];
        if (prototype != null && prototype.prefab != null)
        {
            prefabScale = prototype.prefab.transform.localScale;
        }

        float widthScaleX = Mathf.Abs(prefabScale.x) > 0.0001f ? sourceScale.x / prefabScale.x : 1f;
        float widthScaleZ = Mathf.Abs(prefabScale.z) > 0.0001f ? sourceScale.z / prefabScale.z : widthScaleX;
        float widthScale = Mathf.Max(0.01f, (widthScaleX + widthScaleZ) * 0.5f);
        float heightScale = Mathf.Abs(prefabScale.y) > 0.0001f
            ? Mathf.Max(0.01f, sourceScale.y / prefabScale.y)
            : 1f;

        treeInstance = new TreeInstance
        {
            position = normalized,
            widthScale = widthScale,
            heightScale = heightScale,
            rotation = source.eulerAngles.y * Mathf.Deg2Rad,
            prototypeIndex = prototypeIndex,
            color = Color.white,
            lightmapColor = Color.white
        };

        return true;
    }

    private void AssignConvertedObjectReferences(GameObject convertedObject)
    {
        if (convertedObject == null)
        {
            return;
        }

        TreeHandler[] treeHandlers = convertedObject.GetComponentsInChildren<TreeHandler>(true);
        if (treeHandlers == null || treeHandlers.Length == 0)
        {
            return;
        }

        InventoryAddHandler resolvedAddHandler = ResolveInventoryAddHandler();
        for (int i = 0; i < treeHandlers.Length; i++)
        {
            TreeHandler treeHandler = treeHandlers[i];
            if (treeHandler == null)
            {
                continue;
            }

            if (resolvedAddHandler != null)
            {
                treeHandler.inventoryAddHandler = resolvedAddHandler;
            }

            InventoryItem resolvedItem = ResolveInventoryItem(treeHandler, convertedObject);
            if (resolvedItem != null)
            {
                treeHandler.inventoryItem = resolvedItem;
            }
        }
    }

    private Quaternion GetConvertedTreeRotation(TreeInstance instance, GameObject prototypePrefab, int instanceIndex)
    {
        float yawDegrees = instance.rotation * Mathf.Rad2Deg;
        if (ShouldRandomizeBranch01Yaw(prototypePrefab))
        {
            int positionX = Mathf.RoundToInt(instance.position.x * 100000f);
            int positionZ = Mathf.RoundToInt(instance.position.z * 100000f);
            yawDegrees += HashTo01(branch01RotationSeed, instance.prototypeIndex, instanceIndex, positionX, positionZ, 0) * 360f;
        }

        return Quaternion.Euler(0f, yawDegrees, 0f);
    }

    private bool ShouldRandomizeBranch01Yaw(GameObject prototypePrefab)
    {
        if (!randomizeBranch01Yaw || prototypePrefab == null || string.IsNullOrWhiteSpace(branch01PrototypeName))
        {
            return false;
        }

        return MatchesPrototypeName(prototypePrefab, branch01PrototypeName);
    }

    private static bool MatchesPrototypeName(GameObject prototypePrefab, string prototypeName)
    {
        if (prototypePrefab == null || string.IsNullOrWhiteSpace(prototypeName))
        {
            return false;
        }

        string prefabName = NormalizePrefabName(prototypePrefab.name);
        string targetName = NormalizePrefabName(prototypeName);
        return prefabName == targetName || prefabName.Contains(targetName);
    }

    private InventoryAddHandler ResolveInventoryAddHandler()
    {
        if (inventoryAddHandler != null)
        {
            cachedInventoryAddHandler = inventoryAddHandler;
            return inventoryAddHandler;
        }

        if (cachedInventoryAddHandler == null)
        {
#if UNITY_2023_1_OR_NEWER
            cachedInventoryAddHandler = FindFirstObjectByType<InventoryAddHandler>(FindObjectsInactive.Include);
#else
            cachedInventoryAddHandler = FindObjectOfType<InventoryAddHandler>(true);
#endif
        }

        return cachedInventoryAddHandler;
    }

    private InventoryItem ResolveInventoryItem(TreeHandler treeHandler, GameObject convertedObject)
    {
        

        if (treeHandler.inventoryItem != null)
        {
            return treeHandler.inventoryItem;
        }

        InventoryItem resolvedItem = treeHandler.GetComponent<InventoryItem>();
        if (resolvedItem == null)
        {
            resolvedItem = treeHandler.GetComponentInChildren<InventoryItem>(true);
        }

        if (resolvedItem == null && convertedObject != null)
        {
            resolvedItem = convertedObject.GetComponentInChildren<InventoryItem>(true);
        }

     

        if (resolvedItem != null)
        {
            resolvedItem.ResolveReferences();
        }

        return resolvedItem;
    }

    private void ApplyTerrainPlacement(Transform target, Terrain terrain)
    {
        if (target == null || terrain == null || terrain.terrainData == null)
        {
            return;
        }

        if (alignConvertedObjectsToTerrainNormal)
        {
            AlignObjectToTerrainNormal(target, terrain);
        }

        if (snapConvertedObjectsToTerrain)
        {
            SnapObjectBaseToTerrain(target, terrain);
        }
    }

    private void SnapObjectBaseToTerrain(Transform target, Terrain terrain)
    {
        Vector3 position = target.position;
        float terrainHeight = terrain.SampleHeight(position) + terrain.transform.position.y + terrainSurfaceOffset;
        float groundEmbedDepth = GetAdditionalGroundEmbedDepth(target);

        if (TryGetGroundingBounds(target, out Bounds bounds))
        {
            position.y += terrainHeight - bounds.min.y - groundEmbedDepth;
        }
        else
        {
            position.y = terrainHeight - groundEmbedDepth;
        }

        target.position = position;
    }

    private float GetAdditionalGroundEmbedDepth(Transform target)
    {
        if (mineStoneEmbedDepth <= 0f || target == null)
        {
            return 0f;
        }

        return target.GetComponentInChildren<MineStone>(true) != null
            ? mineStoneEmbedDepth
            : 0f;
    }

    private static bool TryGetGroundingBounds(Transform target, out Bounds bounds)
    {
        if (TryGetMineStoneBounds(target, out bounds))
        {
            return true;
        }

        if (TryGetObjectBounds(target, includeInactive: false, out bounds))
        {
            return true;
        }

        return TryGetObjectBounds(target, includeInactive: true, out bounds);
    }

    private static bool TryGetMineStoneBounds(Transform target, out Bounds bounds)
    {
        bounds = default;
        if (target == null)
        {
            return false;
        }

        MineStone mineStone = target.GetComponentInChildren<MineStone>(true);
        if (mineStone == null)
        {
            return false;
        }

        bool hasBounds = false;

        if (mineStone.mainstoneparts != null && mineStone.mainstoneparts.Length > 0)
        {
            for (int i = 0; i < mineStone.mainstoneparts.Length; i++)
            {
                GameObject part = mineStone.mainstoneparts[i];
                if (part == null)
                {
                    continue;
                }

                if (TryEncapsulateRenderers(part.transform, includeInactive: false, ref bounds, ref hasBounds))
                {
                    continue;
                }

                TryEncapsulateColliders(part.transform, includeInactive: false, ref bounds, ref hasBounds);
            }
        }

        if (hasBounds)
        {
            return true;
        }

        return false;
    }

    private static bool TryGetObjectBounds(Transform target, bool includeInactive, out Bounds bounds)
    {
        bounds = default;
        if (target == null)
        {
            return false;
        }

        bool hasBounds = false;
        TryEncapsulateRenderers(target, includeInactive, ref bounds, ref hasBounds);

        if (hasBounds)
        {
            return true;
        }

        TryEncapsulateColliders(target, includeInactive, ref bounds, ref hasBounds);

        return hasBounds;
    }

    private static bool TryEncapsulateRenderers(Transform target, bool includeInactive, ref Bounds bounds, ref bool hasBounds)
    {
        if (target == null)
        {
            return hasBounds;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(includeInactive);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer currentRenderer = renderers[i];
            if (currentRenderer == null)
            {
                continue;
            }

            if (!includeInactive && (!currentRenderer.enabled || !currentRenderer.gameObject.activeInHierarchy))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = currentRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(currentRenderer.bounds);
            }
        }

        return hasBounds;
    }

    private static bool TryEncapsulateColliders(Transform target, bool includeInactive, ref Bounds bounds, ref bool hasBounds)
    {
        if (target == null)
        {
            return hasBounds;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(includeInactive);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider currentCollider = colliders[i];
            if (currentCollider == null)
            {
                continue;
            }

            if (!includeInactive && (!currentCollider.enabled || !currentCollider.gameObject.activeInHierarchy || currentCollider.isTrigger))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = currentCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(currentCollider.bounds);
            }
        }

        return hasBounds;
    }

    private static void AlignObjectToTerrainNormal(Transform target, Terrain terrain)
    {
        if (target == null || terrain == null || terrain.terrainData == null)
        {
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        Vector3 localPosition = target.position - terrain.transform.position;
        float normalizedX = terrainData.size.x > 0f ? Mathf.Clamp01(localPosition.x / terrainData.size.x) : 0f;
        float normalizedZ = terrainData.size.z > 0f ? Mathf.Clamp01(localPosition.z / terrainData.size.z) : 0f;
        Vector3 terrainNormal = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);

        if (terrainNormal.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector3 projectedForward = Vector3.ProjectOnPlane(target.forward, terrainNormal);
        if (projectedForward.sqrMagnitude < 0.0001f)
        {
            projectedForward = Vector3.ProjectOnPlane(target.right, terrainNormal);
        }

        if (projectedForward.sqrMagnitude < 0.0001f)
        {
            projectedForward = Vector3.Cross(terrainNormal, Vector3.right);
        }

        target.rotation = Quaternion.LookRotation(projectedForward.normalized, terrainNormal.normalized);
    }

    private static float HashTo01(int seed, int a, int b, int c, int d, int e)
    {
        unchecked
        {
            int h = seed;
            h = h * 31 + a;
            h = h * 31 + b;
            h = h * 31 + c;
            h = h * 31 + d;
            h = h * 31 + e;
            uint uh = (uint)h;
            return (uh % 1000003) / 1000003f;
        }
    }
}
