using System.Collections.Generic;
using Sydewa;
using UnityEngine;

// Spawns enemies on terrain when night starts.
public class SpawnNpcsAtNight : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject zombie;
    public GameObject skeleton;

    [Header("Spawn Area")]
    public Terrain[] terrains;
    public int monstersPerTerrain = 20;
    public int maxSpawnAttemptsPerMonster = 25;
    [Tooltip("Legacy name: now used as the physics ground probe radius, not a NavMesh radius.")]
    public float navMeshSampleRadius = 12f;
    public float minDistanceFromPlayer = 20f;
    public Transform spawnedParent;
    [Tooltip("Hard cap for alive enemies spawned by this spawner. Set <= 0 to disable cap.")]
    public int maxTotalAliveFromThisSpawner = 60;
    public float spawnClearRadius = 0.65f;
    public float spawnClearHeight = 2f;
    public LayerMask spawnBlockMask = ~0;

    [Header("Night Settings")]
    public LightingManager lightingManager;
    public bool spawnOnStartIfAlreadyNight = true;
    public bool spawnEveryNight = true;
    public bool clearPreviousSpawnedOnNewNight;
    public bool treatAsNightWhenNoLightingManager = true;
    public bool useFallbackNightHours = true;
    [Range(0f, 24f)] public float fallbackNightStartsAtHour = 18f;
    [Range(0f, 24f)] public float fallbackNightEndsAtHour = 6f;

    [Header("Day Settings")]
    public bool spawnDuringDay = true;
    public bool spawnOnStartIfAlreadyDay = true;
    public bool spawnEveryDay = true;
    public bool clearPreviousSpawnedOnNewDay = true;
    [Range(0f, 1f)] public float dayMonsterRatio = 0.25f;

    [Header("Sword Hit Feedback")]
    [Tooltip("Adds the same sword-hit behavior used by the practice capsule onto spawned monsters.")]
    public bool applyPracticeCapsuleHitFeedbackToMonsters = true;
    public TestHitting practiceCapsuleHitTemplate;
    public Material fallbackMonsterHitMaterial;

    [Header("Enemy References")]
    public LookingController lookingController;
    public EnemiesHandler enemiesHandler;

    private readonly List<GameObject> _spawnedEnemies = new List<GameObject>();
    private bool _wasNightLastFrame;
    private bool _spawnedThisNight;
    private bool _spawnedThisDay;
    private bool _spawnedAtLeastOnce;
    private Transform _playerNormal;
    private Transform _playerBuilding;

    // Initialize references and optional first-night spawn.
    private void Start()
    {
        ResolveReferences();
        ResolveTerrains();
        ApplyMonsterHitFeedback(zombie);
        ApplyMonsterHitFeedback(skeleton);

        bool isNight = IsNightNow();
        _wasNightLastFrame = isNight;

        if (isNight && spawnOnStartIfAlreadyNight)
        {
            SpawnNightWave();
            _spawnedThisNight = true;
            _spawnedAtLeastOnce = true;
        }
        else if (!isNight && spawnDuringDay && spawnOnStartIfAlreadyDay)
        {
            SpawnDayWave();
            _spawnedThisDay = true;
            _spawnedAtLeastOnce = true;
        }
    }

    // Spawn at night transition.
    private void Update()
    {
        bool isNight = IsNightNow();

        if (isNight && !_wasNightLastFrame)
        {
            _spawnedThisDay = false;
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

            bool shouldSpawnThisDay = spawnDuringDay && (spawnEveryDay || !_spawnedAtLeastOnce);
            if (shouldSpawnThisDay && !_spawnedThisDay)
            {
                if (clearPreviousSpawnedOnNewDay)
                {
                    ClearSpawnedEnemies();
                }

                SpawnDayWave();
                _spawnedThisDay = true;
                _spawnedAtLeastOnce = true;
            }
        }

        _wasNightLastFrame = isNight;
    }

    // Handle Spawn Night Wave.
    public void SpawnNightWave()
    {
        SpawnWave(isNightWave: true);
    }

    // Handle Spawn Day Wave.
    public void SpawnDayWave()
    {
        SpawnWave(isNightWave: false);
    }

    // Handle Spawn Wave.
    private void SpawnWave(bool isNightWave)
    {
        ResolveReferences();
        ResolveTerrains();
        PruneMissingSpawnedEnemies();

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

        int monstersPerTerrainForWave = GetMonstersPerTerrainForWave(isNightWave);
        if (monstersPerTerrainForWave <= 0)
        {
            return;
        }

        int spawnBudget = maxTotalAliveFromThisSpawner <= 0
            ? int.MaxValue
            : Mathf.Max(0, maxTotalAliveFromThisSpawner - _spawnedEnemies.Count);

        if (spawnBudget <= 0)
        {
            Debug.Log($"SpawnNpcsAtNight: spawn skipped, cap reached ({_spawnedEnemies.Count}/{maxTotalAliveFromThisSpawner}).");
            return;
        }

        int totalSpawned = 0;
        for (int terrainIndex = 0; terrainIndex < terrains.Length; terrainIndex++)
        {
            if (totalSpawned >= spawnBudget)
            {
                break;
            }

            Terrain terrain = terrains[terrainIndex];
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            int spawnedOnThisTerrain = 0;
            for (int i = 0; i < monstersPerTerrainForWave; i++)
            {
                if (totalSpawned >= spawnBudget)
                {
                    break;
                }

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

            if (spawnedOnThisTerrain < monstersPerTerrainForWave)
            {
                Debug.LogWarning(
                    $"SpawnNpcsAtNight: terrain '{terrain.name}' spawned {spawnedOnThisTerrain}/{monstersPerTerrainForWave}. " +
                    "Increase maxSpawnAttemptsPerMonster or check spawn clearance near the player.");
            }
        }

        string waveName = isNightWave ? "night" : "day";
        Debug.Log($"SpawnNpcsAtNight: spawned {totalSpawned} enemies for {waveName} wave.");
    }

    // Handle Get Monsters Per Terrain For Wave.
    private int GetMonstersPerTerrainForWave(bool isNightWave)
    {
        int baseCount = Mathf.Max(0, monstersPerTerrain);
        if (isNightWave)
        {
            return baseCount;
        }

        if (!spawnDuringDay)
        {
            return 0;
        }

        float ratio = Mathf.Clamp01(dayMonsterRatio);
        if (ratio <= 0f || baseCount <= 0)
        {
            return 0;
        }

        return Mathf.Max(1, Mathf.RoundToInt(baseCount * ratio));
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

    // Handle Prune Missing Spawned Enemies.
    private void PruneMissingSpawnedEnemies()
    {
        for (int i = _spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (_spawnedEnemies[i] == null)
            {
                _spawnedEnemies.RemoveAt(i);
            }
        }
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

        ResolvePracticeCapsuleHitTemplate();

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

    // Handle Resolve Practice Capsule Hit Template.
    private void ResolvePracticeCapsuleHitTemplate()
    {
        if (practiceCapsuleHitTemplate != null)
        {
            return;
        }

        TestHitting[] hitTargets = FindObjectsByType<TestHitting>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < hitTargets.Length; i++)
        {
            TestHitting candidate = hitTargets[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.GetComponentInParent<NPCDemageScript>() != null)
            {
                continue;
            }

            practiceCapsuleHitTemplate = candidate;
            return;
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

            if (TryProjectSpawnPosition(terrain, candidate, out Vector3 groundedCandidate) &&
                IsSpawnPositionClear(groundedCandidate))
            {
                spawnPosition = groundedCandidate;
                return true;
            }
        }

        return false;
    }

    // Handle Try Project Spawn Position.
    private bool TryProjectSpawnPosition(Terrain terrain, Vector3 candidate, out Vector3 spawnPosition)
    {
        spawnPosition = candidate;
        if (terrain == null || terrain.terrainData == null)
        {
            return false;
        }

        Vector3 terrainOrigin = terrain.transform.position;
        spawnPosition.y = terrain.SampleHeight(candidate) + terrainOrigin.y;

        float probeRadius = Mathf.Max(0.1f, navMeshSampleRadius);
        Vector3 rayStart = spawnPosition + Vector3.up * probeRadius;
        RaycastHit[] hits = Physics.RaycastAll(
            rayStart,
            Vector3.down,
            probeRadius * 2f + 2f,
            spawnBlockMask,
            QueryTriggerInteraction.Ignore);

        float bestY = spawnPosition.y;
        bool foundGround = false;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null || hit.normal.y < 0.45f)
            {
                continue;
            }

            if (!foundGround || hit.point.y > bestY)
            {
                bestY = hit.point.y;
                foundGround = true;
            }
        }

        spawnPosition.y = foundGround ? bestY : spawnPosition.y;
        return true;
    }

    // Handle Is Spawn Position Clear.
    private bool IsSpawnPositionClear(Vector3 spawnPosition)
    {
        float radius = Mathf.Max(0.1f, spawnClearRadius);
        float height = Mathf.Max(radius * 2f, spawnClearHeight);
        Vector3 bottom = spawnPosition + Vector3.up * radius;
        Vector3 top = spawnPosition + Vector3.up * (height - radius);
        Collider[] overlaps = Physics.OverlapCapsule(
            bottom,
            top,
            radius,
            spawnBlockMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];
            if (overlap == null || overlap is TerrainCollider)
            {
                continue;
            }

            return false;
        }

        return true;
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

        ApplyMonsterHitFeedback(enemy);
    }

    // Handle Apply Monster Hit Feedback.
    private void ApplyMonsterHitFeedback(GameObject enemy)
    {
        if (!applyPracticeCapsuleHitFeedbackToMonsters || enemy == null)
        {
            return;
        }

        if (!enemy.scene.IsValid())
        {
            return;
        }

        Collider targetCollider = enemy.GetComponent<Collider>();
        if (targetCollider == null)
        {
            targetCollider = enemy.GetComponentInChildren<Collider>(true);
        }

        if (targetCollider == null)
        {
            return;
        }

        TestHitting hitFeedback = targetCollider.GetComponent<TestHitting>();
        if (hitFeedback == null)
        {
            hitFeedback = targetCollider.gameObject.AddComponent<TestHitting>();
        }

        Renderer targetRenderer = enemy.GetComponentInChildren<Renderer>(true);
        if (targetRenderer != null)
        {
            hitFeedback.meshRenderer = targetRenderer;
        }

        Material hitMaterial = ResolveMonsterHitMaterial(enemy);
        if (hitMaterial != null)
        {
            hitFeedback.hitmat = hitMaterial;
        }
    }

    // Handle Resolve Monster Hit Material.
    private Material ResolveMonsterHitMaterial(GameObject enemy)
    {
        if (practiceCapsuleHitTemplate != null && practiceCapsuleHitTemplate.hitmat != null)
        {
            return practiceCapsuleHitTemplate.hitmat;
        }

        if (fallbackMonsterHitMaterial != null)
        {
            return fallbackMonsterHitMaterial;
        }

        NPCDemageScript damageScript = enemy.GetComponent<NPCDemageScript>();
        if (damageScript == null)
        {
            damageScript = enemy.GetComponentInChildren<NPCDemageScript>(true);
        }

        return damageScript != null ? damageScript.demagemat : null;
    }
}
