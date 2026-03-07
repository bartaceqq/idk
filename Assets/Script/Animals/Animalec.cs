using System.Collections.Generic;
using ithappy.Animals_FREE;
using UnityEngine;

public class Animalec : MonoBehaviour
{
    public CreatureMover creatureMover;
    public Terrain terrain;
    public int xmin;
    public int xmax;
    public int zmin;
    public int zmax;
    public List<InventoryItem> itemstogain = new List<InventoryItem>();
    public GameObject player;

    public int health = 100;
    public float speed;
    public float DetectionRange = 8f;
    public float fleeDurationAfterHit = 3f;
    public float fleeDistance = 10f;
    public float fleeRepathSeconds = 0.2f;
    public Material hitmaterial;
    public SkinnedMeshRenderer meshRenderer;
    public float hitFlashSeconds = 0.2f;
    public bool disableCollidersOnDeath = true;
    public bool disableAnimatorOnDeath = false;
    public bool destroyOnDeath = true;
    public float destroyDelaySeconds = 0.5f;
    [SerializeField]
    private float arriveDistance = 0.75f;
    [SerializeField, Range(0f, 60f)]
    private float maxWalkableSlope = 40f;
    [SerializeField]
    private int maxRandomPointAttempts = 24;
    [SerializeField]
    private float minWaypointDistance = 6f;
    [SerializeField]
    private float minRoamWaitSeconds = 0.35f;
    [SerializeField]
    private float maxRoamWaitSeconds = 1f;

    private Transform moveToTarget;
    private Vector3 currentDestination;
    private bool hasDestination;
    private float nextPickTime;
    private float fleeUntilTime;
    private float nextFleeRepathTime;

    private Renderer targetRenderer;
    private Material originalMaterial;
    private Coroutine flashRoutine;
    private Coroutine deathRoutine;
    private Transform playerTransform;
    private bool isDead;
    private Animator cachedAnimator;
    private CharacterController cachedCharacterController;
    private MovePlayerInput cachedInputMover;

    private void Start()
    {
        if (creatureMover == null)
        {
            creatureMover = GetComponent<CreatureMover>();
        }

        if (terrain == null)
        {
            terrain = Terrain.activeTerrain;
        }

        if (creatureMover == null || terrain == null)
        {
            enabled = false;
            return;
        }

        ResolvePlayer();
        targetRenderer = meshRenderer != null ? meshRenderer : GetComponentInChildren<Renderer>();
        if (targetRenderer != null)
        {
            originalMaterial = targetRenderer.material;
        }
        cachedAnimator = GetComponent<Animator>();
        cachedCharacterController = GetComponent<CharacterController>();
        cachedInputMover = GetComponent<MovePlayerInput>();

        CreateMoveTarget();
        PickAndMoveToNewDestination();
    }

    private void Update()
    {
        if (isDead)
        {
            creatureMover.SetInput(Vector2.zero, transform.position + transform.forward, false, false);
            return;
        }

        ResolvePlayer();

        if (ShouldFleeFromPlayer())
        {
            UpdateFleeMovement();
            return;
        }

        if (!hasDestination)
        {
            if (Time.time >= nextPickTime)
            {
                PickAndMoveToNewDestination();
            }
            return;
        }

        if (HasReachedDestination())
        {
            hasDestination = false;
            creatureMover.MoveToTransform(null);

            var waitMin = Mathf.Max(0f, Mathf.Min(minRoamWaitSeconds, maxRoamWaitSeconds));
            var waitMax = Mathf.Max(waitMin, Mathf.Max(minRoamWaitSeconds, maxRoamWaitSeconds));
            nextPickTime = Time.time + Random.Range(waitMin, waitMax);
        }
    }

    private void OnDestroy()
    {
        if (moveToTarget != null)
        {
            Destroy(moveToTarget.gameObject);
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }
        if (deathRoutine != null)
        {
            StopCoroutine(deathRoutine);
        }

        if (targetRenderer != null && originalMaterial != null)
        {
            targetRenderer.material = originalMaterial;
        }
    }

    private void PickAndMoveToNewDestination()
    {
        if (!TryGetRandomWalkablePoint(out var point))
        {
            nextPickTime = Time.time + 0.25f;
            return;
        }

        currentDestination = point;
        moveToTarget.position = point;
        hasDestination = true;
        creatureMover.MoveToTransform(moveToTarget);
    }

    private bool HasReachedDestination()
    {
        var self = transform.position;
        var target = currentDestination;
        self.y = 0f;
        target.y = 0f;
        return (self - target).sqrMagnitude <= arriveDistance * arriveDistance;
    }

    private bool TryGetRandomWalkablePoint(out Vector3 point)
    {
        point = transform.position;

        if (terrain == null || terrain.terrainData == null)
        {
            return false;
        }

        var minX = Mathf.Min(xmin, xmax);
        var maxX = Mathf.Max(xmin, xmax);
        var minZ = Mathf.Min(zmin, zmax);
        var maxZ = Mathf.Max(zmin, zmax);
        var minDistanceSqr = Mathf.Max(0f, minWaypointDistance) * Mathf.Max(0f, minWaypointDistance);
        var self = transform.position;
        self.y = 0f;

        for (var i = 0; i < maxRandomPointAttempts; i++)
        {
            var x = Random.Range(minX, maxX + 1f);
            var z = Random.Range(minZ, maxZ + 1f);
            var candidate = BuildTerrainPoint(x, z);
            var candidateFlat = candidate;
            candidateFlat.y = 0f;

            if (!IsInsideTerrain(candidate))
            {
                continue;
            }

            if (!IsSlopeWalkable(candidate))
            {
                continue;
            }

            if ((candidateFlat - self).sqrMagnitude < minDistanceSqr)
            {
                continue;
            }

            point = candidate;
            return true;
        }

        return false;
    }

    private Vector3 BuildTerrainPoint(float x, float z)
    {
        var terrainPosition = terrain.transform.position;
        var worldXZ = new Vector3(x, 0f, z);
        var y = terrain.SampleHeight(worldXZ) + terrainPosition.y;
        return new Vector3(x, y, z);
    }

    private bool IsInsideTerrain(in Vector3 worldPoint)
    {
        var terrainPosition = terrain.transform.position;
        var size = terrain.terrainData.size;

        return worldPoint.x >= terrainPosition.x
            && worldPoint.x <= terrainPosition.x + size.x
            && worldPoint.z >= terrainPosition.z
            && worldPoint.z <= terrainPosition.z + size.z;
    }

    private bool IsSlopeWalkable(in Vector3 worldPoint)
    {
        var terrainPosition = terrain.transform.position;
        var size = terrain.terrainData.size;

        var normalizedX = (worldPoint.x - terrainPosition.x) / size.x;
        var normalizedZ = (worldPoint.z - terrainPosition.z) / size.z;

        if (normalizedX < 0f || normalizedX > 1f || normalizedZ < 0f || normalizedZ > 1f)
        {
            return false;
        }

        var steepness = terrain.terrainData.GetSteepness(normalizedX, normalizedZ);
        return steepness <= maxWalkableSlope;
    }

    private void CreateMoveTarget()
    {
        if (moveToTarget != null)
        {
            return;
        }

        var targetObject = new GameObject($"{name}_MoveTarget");
        targetObject.hideFlags = HideFlags.HideInHierarchy;
        moveToTarget = targetObject.transform;
        moveToTarget.position = transform.position;
    }

    public void TakeDamage(float damage)
    {
        if (isDead || damage <= 0f)
        {
            return;
        }

        int appliedDamage = Mathf.CeilToInt(damage);
        health = Mathf.Max(0, health - appliedDamage);

        StartFlee(fleeDurationAfterHit);
        TriggerHitFlash();

        if (health <= 0)
        {
            Die();
        }
    }

    // Keep compatibility with existing typo-based calls.
    public void TakeDemage(float damage)
    {
        TakeDamage(damage);
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        hasDestination = false;
        fleeUntilTime = 0f;
        nextFleeRepathTime = 0f;
        creatureMover.MoveToTransform(null);

        if (creatureMover != null)
        {
            creatureMover.enabled = false;
        }

        if (cachedInputMover != null)
        {
            cachedInputMover.enabled = false;
        }

        if (cachedCharacterController != null)
        {
            cachedCharacterController.enabled = false;
        }

        if (disableAnimatorOnDeath && cachedAnimator != null)
        {
            cachedAnimator.enabled = false;
        }

        if (disableCollidersOnDeath)
        {
            DisableAllColliders();
        }

        if (destroyOnDeath)
        {
            if (deathRoutine != null)
            {
                StopCoroutine(deathRoutine);
            }

            deathRoutine = StartCoroutine(DestroyAfterDelay());
        }
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            playerTransform = player.transform;
            return;
        }

        if (playerTransform != null)
        {
            return;
        }

        GameObject taggedPlayer = null;
        try
        {
            taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            taggedPlayer = null;
        }

        if (taggedPlayer != null)
        {
            player = taggedPlayer;
            playerTransform = taggedPlayer.transform;
        }
    }

    private bool ShouldFleeFromPlayer()
    {
        bool fleeFromHit = Time.time < fleeUntilTime;
        if (playerTransform == null)
        {
            return fleeFromHit;
        }

        float range = Mathf.Max(0f, DetectionRange);
        if (range <= 0f)
        {
            return fleeFromHit;
        }

        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;
        bool playerTooClose = toPlayer.sqrMagnitude <= range * range;
        return playerTooClose || fleeFromHit;
    }

    private void StartFlee(float durationSeconds)
    {
        float duration = Mathf.Max(0f, durationSeconds);
        fleeUntilTime = Mathf.Max(fleeUntilTime, Time.time + duration);
        nextFleeRepathTime = 0f;
    }

    private void UpdateFleeMovement()
    {
        hasDestination = false;

        bool shouldRepath = Time.time >= nextFleeRepathTime;
        if (shouldRepath)
        {
            nextFleeRepathTime = Time.time + Mathf.Max(0.05f, fleeRepathSeconds);

            if (TryGetFleePoint(out var fleePoint))
            {
                currentDestination = fleePoint;
                moveToTarget.position = fleePoint;
            }
        }

        creatureMover.SetInput(new Vector2(0f, 1f), currentDestination, true, false);
    }

    private bool TryGetFleePoint(out Vector3 point)
    {
        point = transform.position;

        if (terrain == null || terrain.terrainData == null)
        {
            return false;
        }

        Vector3 awayDirection = transform.forward;
        if (playerTransform != null)
        {
            awayDirection = transform.position - playerTransform.position;
        }

        awayDirection.y = 0f;
        if (awayDirection.sqrMagnitude < 0.001f)
        {
            awayDirection = transform.forward;
            awayDirection.y = 0f;
        }

        if (awayDirection.sqrMagnitude < 0.001f)
        {
            awayDirection = Vector3.forward;
        }

        awayDirection.Normalize();
        float desiredDistance = Mathf.Max(2f, fleeDistance);
        Vector3 basePos = transform.position;

        for (int i = 0; i < 12; i++)
        {
            float angleOffset = (i / 2) * 15f;
            if (i % 2 == 1)
            {
                angleOffset *= -1f;
            }

            Vector3 dir = Quaternion.Euler(0f, angleOffset, 0f) * awayDirection;
            Vector3 candidateXZ = basePos + dir * desiredDistance;
            Vector3 candidate = BuildTerrainPoint(candidateXZ.x, candidateXZ.z);

            if (!IsInsideTerrain(candidate))
            {
                continue;
            }

            if (!IsSlopeWalkable(candidate))
            {
                continue;
            }

            point = candidate;
            return true;
        }

        return TryGetRandomWalkablePoint(out point);
    }

    private void TriggerHitFlash()
    {
        if (targetRenderer == null || hitmaterial == null)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(HitFlashRoutine());
    }

    private System.Collections.IEnumerator HitFlashRoutine()
    {
        if (targetRenderer == null)
        {
            yield break;
        }

        Material restoreMaterial = originalMaterial != null ? originalMaterial : targetRenderer.material;
        targetRenderer.material = hitmaterial;
        yield return new WaitForSeconds(Mathf.Max(0f, hitFlashSeconds));

        if (targetRenderer != null && restoreMaterial != null)
        {
            targetRenderer.material = restoreMaterial;
        }

        flashRoutine = null;
    }

    private void DisableAllColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col != null)
            {
                col.enabled = false;
            }
        }
    }

    private System.Collections.IEnumerator DestroyAfterDelay()
    {
        float delay = Mathf.Max(0f, destroyDelaySeconds);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        Destroy(gameObject);
    }
}
