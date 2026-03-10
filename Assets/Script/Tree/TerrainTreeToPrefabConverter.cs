using System.Collections.Generic;
using UnityEngine;

// Controls Terrain Tree To Prefab Converter behavior.
public class TerrainTreeToPrefabConverter : MonoBehaviour
{
    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private Transform parentForSpawnedTrees;
    [Tooltip("If enabled, converter will only convert resource prefabs (CutTree/MineStone).")]
    [SerializeField] private bool convertOnlyCuttableTrees = true;
    [SerializeField] private bool includeMineableStones = true;
    [SerializeField] private bool includePickableItems = true;
    [SerializeField] private bool clearConvertedTerrainTrees = true;
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

                if (!hasCutTree && !hasMineStone && !hasPickableItem)
                {
                    remainingInstances.Add(instance);
                    continue;
                }
            }

            Vector3 worldPosition = targetTerrain.transform.position + Vector3.Scale(instance.position, terrainData.size);
            Quaternion worldRotation = Quaternion.Euler(0f, instance.rotation * Mathf.Rad2Deg, 0f);

            GameObject spawnedTree = Instantiate(prototypePrefab, worldPosition, worldRotation, parentForSpawnedTrees);

            Vector3 baseScale = spawnedTree.transform.localScale;
            spawnedTree.transform.localScale = new Vector3(
                baseScale.x * instance.widthScale,
                baseScale.y * instance.heightScale,
                baseScale.z * instance.widthScale
            );

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

                        Vector3 baseScale = spawned.transform.localScale;
                        spawned.transform.localScale = new Vector3(baseScale.x * widthScale, baseScale.y * heightScale, baseScale.z * widthScale);

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
