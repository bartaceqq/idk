using System.Collections.Generic;
using UnityEngine;

// Spawns pickable prefabs near the player for specific Terrain Detail meshes,
// then removes the detail instance when collected.
public class TerrainDetailPickupSpawner : MonoBehaviour
{
    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private Transform player;
    [SerializeField] private List<DetailPickupConfig> pickupConfigs = new List<DetailPickupConfig>();

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 8f;
    [SerializeField] private float despawnRadius = 14f;
    [SerializeField] private int maxSpawnedActive = 50;
    [SerializeField] private int maxPerCell = 1;
    [SerializeField] private bool deterministicPlacement = true;
    [SerializeField] private int placementSeed = 1337;
    [SerializeField, Range(0f, 1f)] private float cellJitter = 0.6f;
    [SerializeField, Range(0.02f, 1f)] private float spawnRefreshInterval = 0.15f;
    [SerializeField, Range(0.1f, 3f)] private float resolveReferencesInterval = 1f;

    private readonly Dictionary<long, DetailPickupMarker> active = new Dictionary<long, DetailPickupMarker>();
    private int cachedDetailIndex = -1;
    private float _nextSpawnRefreshTime;
    private float _nextResolveReferencesTime;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (Time.time >= _nextResolveReferencesTime)
        {
            ResolveReferences();
            _nextResolveReferencesTime = Time.time + Mathf.Max(0.1f, resolveReferencesInterval);
        }

        if (targetTerrain == null || player == null || pickupConfigs == null || pickupConfigs.Count == 0)
        {
            return;
        }

        if (Time.time < _nextSpawnRefreshTime)
        {
            return;
        }

        _nextSpawnRefreshTime = Time.time + Mathf.Max(0.02f, spawnRefreshInterval);

        for (int i = 0; i < pickupConfigs.Count; i++)
        {
            DetailPickupConfig config = pickupConfigs[i];
            if (config == null || config.pickupPrefab == null)
            {
                continue;
            }

            int detailIndex = ResolveDetailPrototypeIndex(config);
            if (detailIndex < 0)
            {
                continue;
            }

            SpawnNearby(detailIndex, config);
        }

        DespawnFar();
    }

    private void ResolveReferences()
    {
        if (targetTerrain == null)
        {
            targetTerrain = GetComponent<Terrain>();
            if (targetTerrain == null)
            {
                targetTerrain = FindFirstObjectByType<Terrain>(FindObjectsInactive.Include);
            }
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    private int ResolveDetailPrototypeIndex(DetailPickupConfig config)
    {
        if (config.cachedDetailIndex >= 0)
        {
            return config.cachedDetailIndex;
        }

        if (config.detailPrototypeIndex >= 0)
        {
            config.cachedDetailIndex = config.detailPrototypeIndex;
            return config.cachedDetailIndex;
        }

        if (targetTerrain == null || targetTerrain.terrainData == null || config.detailPrototypePrefab == null)
        {
            return -1;
        }

        DetailPrototype[] protos = targetTerrain.terrainData.detailPrototypes;
        for (int i = 0; i < protos.Length; i++)
        {
            GameObject protoPrefab = GetDetailPrototypePrefab(protos[i]);
            if (protoPrefab == config.detailPrototypePrefab)
            {
                config.cachedDetailIndex = i;
                return config.cachedDetailIndex;
            }
        }

        return -1;
    }

    private void SpawnNearby(int detailIndex, DetailPickupConfig config)
    {
        if (active.Count >= maxSpawnedActive)
        {
            return;
        }

        TerrainData data = targetTerrain.terrainData;
        int detailWidth = data.detailWidth;
        int detailHeight = data.detailHeight;

        Vector3 local = player.position - targetTerrain.transform.position;
        int centerX = Mathf.FloorToInt(local.x / data.size.x * detailWidth);
        int centerZ = Mathf.FloorToInt(local.z / data.size.z * detailHeight);

        float cellSizeX = data.size.x / detailWidth;
        float cellSizeZ = data.size.z / detailHeight;
        int radiusX = Mathf.CeilToInt(spawnRadius / cellSizeX);
        int radiusZ = Mathf.CeilToInt(spawnRadius / cellSizeZ);

        int minX = Mathf.Clamp(centerX - radiusX, 0, detailWidth - 1);
        int maxX = Mathf.Clamp(centerX + radiusX, 0, detailWidth - 1);
        int minZ = Mathf.Clamp(centerZ - radiusZ, 0, detailHeight - 1);
        int maxZ = Mathf.Clamp(centerZ + radiusZ, 0, detailHeight - 1);

        int width = maxX - minX + 1;
        int height = maxZ - minZ + 1;
        int[,] layer = data.GetDetailLayer(minX, minZ, width, height, detailIndex);

        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < width; x++)
            {
                if (active.Count >= maxSpawnedActive)
                {
                    return;
                }

                int count = layer[z, x];
                if (count <= 0)
                {
                    continue;
                }

                int cellX = minX + x;
                int cellZ = minZ + z;
                long key = PackKey(cellX, cellZ);
                if (active.ContainsKey(key))
                {
                    continue;
                }

                int toSpawn = Mathf.Min(count, Mathf.Max(1, maxPerCell));
                for (int i = 0; i < toSpawn; i++)
                {
                    if (active.Count >= maxSpawnedActive)
                    {
                        return;
                    }

                    Vector3 worldPos = DetailCellToWorld(data, cellX, cellZ, i, detailIndex, cellSizeX, cellSizeZ);
                    GameObject spawned = Instantiate(config.pickupPrefab, worldPos, Quaternion.Euler(0f, SampleRotation(cellX, cellZ, i), 0f), transform);

                    InventoryItem item = spawned.GetComponentInChildren<InventoryItem>(true);
                    if (item != null)
                    {
                        item.ResolveReferences();
                    }

                    DetailPickupMarker marker = spawned.GetComponent<DetailPickupMarker>();
                    if (marker == null)
                    {
                    marker = spawned.AddComponent<DetailPickupMarker>();
                }

                    marker.Initialize(targetTerrain, detailIndex, cellX, cellZ, this);
                    active[key] = marker;
                }
            }
        }
    }

    private void DespawnFar()
    {
        if (active.Count == 0)
        {
            return;
        }

        float maxDistanceSqr = despawnRadius * despawnRadius;
        Vector3 playerPos = player.position;

        List<long> toRemove = null;
        foreach (KeyValuePair<long, DetailPickupMarker> pair in active)
        {
            DetailPickupMarker marker = pair.Value;
            if (marker == null)
            {
                continue;
            }

            float distSqr = (playerPos - marker.transform.position).sqrMagnitude;
            if (distSqr > maxDistanceSqr)
            {
                marker.Despawn();
                if (toRemove == null)
                {
                    toRemove = new List<long>();
                }
                toRemove.Add(pair.Key);
            }
        }

        if (toRemove != null)
        {
            for (int i = 0; i < toRemove.Count; i++)
            {
                active.Remove(toRemove[i]);
            }
        }
    }

    internal void NotifyCollected(int cellX, int cellZ)
    {
        long key = PackKey(cellX, cellZ);
        active.Remove(key);
    }

    private Vector3 DetailCellToWorld(TerrainData data, int cellX, int cellZ, int index, int detailIndex, float cellSizeX, float cellSizeZ)
    {
        float jitterX;
        float jitterZ;
        if (deterministicPlacement)
        {
            float h1 = HashTo01(placementSeed, detailIndex, cellX, cellZ, index, 1);
            float h2 = HashTo01(placementSeed, detailIndex, cellX, cellZ, index, 2);
            jitterX = Mathf.Lerp(0.5f - cellJitter * 0.5f, 0.5f + cellJitter * 0.5f, h1);
            jitterZ = Mathf.Lerp(0.5f - cellJitter * 0.5f, 0.5f + cellJitter * 0.5f, h2);
        }
        else
        {
            jitterX = Random.value;
            jitterZ = Random.value;
        }

        float worldX = (cellX + jitterX) * cellSizeX;
        float worldZ = (cellZ + jitterZ) * cellSizeZ;
        Vector3 worldPos = targetTerrain.transform.position + new Vector3(worldX, 0f, worldZ);
        worldPos.y = targetTerrain.SampleHeight(worldPos) + targetTerrain.transform.position.y;
        return worldPos;
    }

    private float SampleRotation(int cellX, int cellZ, int index)
    {
        if (!deterministicPlacement)
        {
            return Random.Range(0f, 360f);
        }

        float h = HashTo01(placementSeed, 0, cellX, cellZ, index, 3);
        return h * 360f;
    }

    private static long PackKey(int x, int z)
    {
        return ((long)x << 32) ^ (uint)z;
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

    [System.Serializable]
    public class DetailPickupConfig
    {
        public GameObject detailPrototypePrefab;
        public GameObject pickupPrefab;
        public int detailPrototypeIndex = -1;
        [HideInInspector] public int cachedDetailIndex = -1;
    }
}
