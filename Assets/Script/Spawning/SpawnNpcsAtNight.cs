using System.Collections.Generic;
using Sydewa;
using UnityEngine;
using UnityEngine.AI;

// Spawns enemies on terrain NavMesh when night starts.
public class SpawnNpcsAtNight : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject zombie;
    public GameObject skeleton;

    [Header("Spawn Area")]
    public Terrain[] terrains;
    public int monstersPerTerrain = 100;
    public int maxSpawnAttemptsPerMonster = 25;
    public float navMeshSampleRadius = 12f;
    public float minDistanceFromPlayer = 20f;
    public Transform spawnedParent;

    [Header("Night Settings")]
    public LightingManager lightingManager;
    public bool spawnOnStartIfAlreadyNight = true;
    public bool spawnEveryNight = true;
    public bool clearPreviousSpawnedOnNewNight;
    public bool treatAsNightWhenNoLightingManager = true;
    public bool useFallbackNightHours = true;
    [Range(0f, 24f)] public float fallbackNightStartsAtHour = 18f;
    [Range(0f, 24f)] public float fallbackNightEndsAtHour = 6f;

    [Header("Enemy References")]
    public LookingController lookingController;
    public EnemiesHandler enemiesHandler;

    private readonly List<GameObject> _spawnedEnemies = new List<GameObject>();
    private bool _wasNightLastFrame;
    private bool _spawnedThisNight;
    private bool _spawnedAtLeastOnce;
    private Transform _playerNormal;
    private Transform _playerBuilding;

    // Initialize references and optional first-night spawn.
    private void Start()
    {
        ResolveReferences();
        ResolveTerrains();

        bool isNight = IsNightNow();
        _wasNightLastFrame = isNight;

        if (isNight && spawnOnStartIfAlreadyNight)
        {
            SpawnNightWave();
            _spawnedThisNight = true;
            _spawnedAtLeastOnce = true;
        }
    }

    // Spawn at night transition.
    private void Update()
    {
        bool isNight = IsNightNow();

        if (isNight && !_wasNightLastFrame)
        {
            bool shouldSpawnThisNight = spawnEveryNight || !_spawnedAtLeastOnce;
            if (shouldSpawnThisNight && !_spawnedThisNight)
            {
                if (clearPreviousSpawnedOnNewNight)
                {
                    ClearSpawnedEnemies();
                }

                SpawnNightWave();
                _spawnedThisNight = true;
                _spawnedAtLeastOnce = true;
            }
        }
        else if (!isNight && _wasNightLastFrame)
        {
            _spawnedThisNight = false;
        }

        _wasNightLastFrame = isNight;
    }

    // Handle Spawn Night Wave.
    public void SpawnNightWave()
    {
        ResolveReferences();
        ResolveTerrains();

        if (terrains == null || terrains.Length == 0)
        {
            Debug.LogWarning("SpawnNpcsAtNight: no terrains assigned/found.");
            return;
        }

        if (zombie == null && skeleton == null)
        {
            Debug.LogWarning("SpawnNpcsAtNight: assign zombie and/or skeleton prefab.");
            return;
        }

        EnsureSpawnedParent();

        int totalSpawned = 0;
        for (int terrainIndex = 0; terrainIndex < terrains.Length; terrainIndex++)
        {
            Terrain terrain = terrains[terrainIndex];
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            int spawnedOnThisTerrain = 0;
            for (int i = 0; i < monstersPerTerrain; i++)
            {
                if (!TryGetSpawnPositionOnTerrain(terrain, out Vector3 spawnPos))
                {
                    continue;
                }

                GameObject prefab = GetRandomEnemyPrefab();
                if (prefab == null)
                {
                    continue;
                }

                Quaternion spawnRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                GameObject enemy = Instantiate(prefab, spawnPos, spawnRotation, spawnedParent);
                ConfigureSpawnedEnemy(enemy);

                _spawnedEnemies.Add(enemy);
                spawnedOnThisTerrain++;
                totalSpawned++;
            }

            if (spawnedOnThisTerrain < monstersPerTerrain)
            {
                Debug.LogWarning(
                    $"SpawnNpcsAtNight: terrain '{terrain.name}' spawned {spawnedOnThisTerrain}/{monstersPerTerrain}. " +
                    "Increase navMeshSampleRadius or ensure NavMesh covers that terrain.");
            }
        }

        Debug.Log($"SpawnNpcsAtNight: spawned {totalSpawned} enemies.");
    }

    // Handle Clear Spawned Enemies.
    public void ClearSpawnedEnemies()
    {
        for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = _spawnedEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(enemy);
            }
            else
            {
                DestroyImmediate(enemy);
            }
        }

        _spawnedEnemies.Clear();
    }

    // Handle Resolve References.
    private void ResolveReferences()
    {
        if (lightingManager == null)
        {
            lightingManager = FindFirstObjectByType<LightingManager>();
        }

        if (lookingController == null)
        {
            lookingController = FindFirstObjectByType<LookingController>();
        }

        if (enemiesHandler == null)
        {
            enemiesHandler = FindFirstObjectByType<EnemiesHandler>();
        }

        _playerNormal = null;
        _playerBuilding = null;
        if (lookingController != null)
        {
            if (lookingController.normalcapsule != null)
            {
                _playerNormal = lookingController.normalcapsule.transform;
            }
            if (lookingController.buildingcapsule != null)
            {
                _playerBuilding = lookingController.buildingcapsule.transform;
            }
        }
    }

    // Handle Resolve Terrains.
    private void ResolveTerrains()
    {
        if (terrains == null || terrains.Length == 0)
        {
            terrains = Terrain.activeTerrains;
        }
    }

    // Handle Ensure Spawned Parent.
    private void EnsureSpawnedParent()
    {
        if (spawnedParent != null)
        {
            return;
        }

        GameObject root = new GameObject("NightSpawnedEnemies");
        spawnedParent = root.transform;
    }

    // Handle Is Night Now.
    private bool IsNightNow()
    {
        if (lightingManager == null)
        {
            return treatAsNightWhenNoLightingManager;
        }

        float hour = Mathf.Repeat(lightingManager.TimeOfDay, 24f);
        bool noNightConfigured =
            lightingManager.morningInterval.x <= 0f &&
            lightingManager.afterNoonInterval.y >= 1f;

        if (useFallbackNightHours && noNightConfigured)
        {
            return IsHourInsideNightWindow(hour, fallbackNightStartsAtHour, fallbackNightEndsAtHour);
        }

        float timePercent = Mathf.Repeat(lightingManager.TimeOfDay, 24f) / 24f;
        return timePercent < lightingManager.morningInterval.x ||
               timePercent > lightingManager.afterNoonInterval.y;
    }

    // Handle Is Hour Inside Night Window.
    private static bool IsHourInsideNightWindow(float hour, float startHour, float endHour)
    {
        float safeHour = Mathf.Repeat(hour, 24f);
        float safeStart = Mathf.Repeat(startHour, 24f);
        float safeEnd = Mathf.Repeat(endHour, 24f);

        if (Mathf.Approximately(safeStart, safeEnd))
        {
            // Equal values would be ambiguous; treat as always night.
            return true;
        }

        // Night window crosses midnight: for example 18:00 -> 06:00.
        if (safeStart > safeEnd)
        {
            return safeHour >= safeStart || safeHour < safeEnd;
        }

        // Non-wrapping window: for example 1:00 -> 5:00.
        return safeHour >= safeStart && safeHour < safeEnd;
    }

    // Handle Get Random Enemy Prefab.
    private GameObject GetRandomEnemyPrefab()
    {
        if (zombie != null && skeleton != null)
        {
            return Random.value < 0.5f ? zombie : skeleton;
        }

        return zombie != null ? zombie : skeleton;
    }

    // Handle Try Get Spawn Position On Terrain.
    private bool TryGetSpawnPositionOnTerrain(Terrain terrain, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        TerrainData data = terrain.terrainData;
        Vector3 terrainOrigin = terrain.transform.position;
        Transform activePlayer = GetActivePlayerTransform();

        int attempts = Mathf.Max(1, maxSpawnAttemptsPerMonster);
        for (int i = 0; i < attempts; i++)
        {
            float randomX = Random.Range(0f, data.size.x);
            float randomZ = Random.Range(0f, data.size.z);

            Vector3 candidate = new Vector3(terrainOrigin.x + randomX, 0f, terrainOrigin.z + randomZ);
            candidate.y = terrain.SampleHeight(candidate) + terrainOrigin.y + 1f;

            if (activePlayer != null && minDistanceFromPlayer > 0f)
            {
                if (Vector3.Distance(candidate, activePlayer.position) < minDistanceFromPlayer)
                {
                    continue;
                }
            }

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
            {
                spawnPosition = hit.position;
                return true;
            }
        }

        return false;
    }

    // Handle Get Active Player Transform.
    private Transform GetActivePlayerTransform()
    {
        if (lookingController != null)
        {
            if (lookingController.switched && _playerBuilding != null)
            {
                return _playerBuilding;
            }
            if (!lookingController.switched && _playerNormal != null)
            {
                return _playerNormal;
            }
        }

        if (_playerNormal != null && _playerNormal.gameObject.activeInHierarchy)
        {
            return _playerNormal;
        }
        if (_playerBuilding != null && _playerBuilding.gameObject.activeInHierarchy)
        {
            return _playerBuilding;
        }
        if (_playerNormal != null)
        {
            return _playerNormal;
        }
        if (_playerBuilding != null)
        {
            return _playerBuilding;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        return taggedPlayer != null ? taggedPlayer.transform : null;
    }

    // Handle Configure Spawned Enemy.
    private void ConfigureSpawnedEnemy(GameObject enemy)
    {
        if (enemy == null)
        {
            return;
        }

        RandomZombieScript zombieScript = enemy.GetComponent<RandomZombieScript>();
        if (zombieScript != null)
        {
            if (zombieScript.lookingController == null)
            {
                zombieScript.lookingController = lookingController;
            }
            if (zombieScript.PlayerNormal == null && _playerNormal != null)
            {
                zombieScript.PlayerNormal = _playerNormal.gameObject;
            }
            if (zombieScript.PlayerBuilding == null && _playerBuilding != null)
            {
                zombieScript.PlayerBuilding = _playerBuilding.gameObject;
            }
            if (zombieScript.enemiesHandler == null)
            {
                zombieScript.enemiesHandler = enemiesHandler;
            }
        }

        RandomSkeletonScript skeletonScript = enemy.GetComponent<RandomSkeletonScript>();
        if (skeletonScript != null)
        {
            if (skeletonScript.lookingController == null)
            {
                skeletonScript.lookingController = lookingController;
            }
            if (skeletonScript.PlayerNormal == null && _playerNormal != null)
            {
                skeletonScript.PlayerNormal = _playerNormal.gameObject;
            }
            if (skeletonScript.PlayerBuilding == null && _playerBuilding != null)
            {
                skeletonScript.PlayerBuilding = _playerBuilding.gameObject;
            }
            if (skeletonScript.enemiesHandler == null)
            {
                skeletonScript.enemiesHandler = enemiesHandler;
            }
        }
    }
}
