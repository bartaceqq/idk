using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public abstract class CustomEnemyAIBase : MonoBehaviour
{
    [Header("References")]
    public LookingController lookingController;
    public GameObject PlayerNormal;
    public Transform playertransform;
    public EnemiesHandler enemiesHandler;

    [Header("Behavior")]
    public float followRange = 15f;
    public float detectionRange = 15f;
    public float attackRange = 2f;
    public float attackRangeBuffer = 0.35f;
    public float attackCooldown = 1.2f;
    public float attackAnimLockSeconds = 0.8f;

    [Header("Detection")]
    public bool autoFindPlayerByTag = true;
    public string playerTag = "Player";
    public bool debugRangeLogs;
    public float debugLogInterval = 0.25f;

    [Header("Roaming")]
    public bool enableRoaming = true;
    public float roamRadius = 25f;
    public float roamRepathInterval = 2f;
    public float roamMinMoveDistance = 3f;
    public int roamDestinationTries = 8;

    [Header("Custom Movement")]
    public float moveSpeed = 7f;
    public float acceleration = 18f;
    public float rotationSpeed = 10f;
    public float bodyRadius = 0.5f;
    public float bodyHeight = 2f;
    public float groundProbeHeight = 2.5f;
    public float groundProbeDistance = 6f;
    public float minimumGroundNormalY = 0.45f;
    public LayerMask groundLayerMask = ~0;
    public LayerMask obstacleLayerMask = ~0;
    public bool disableAnimatorRootMotion = true;
    public float obstacleProbeDistance = 1.25f;
    public float obstacleSideProbeDistance = 1.75f;
    public float obstacleSkinWidth = 0.08f;
    public float destinationReachDistance = 0.45f;
    public float separationRadius = 1.1f;
    public float separationStrength = 0.75f;

    [Header("Stuck Recovery")]
    public float stuckCheckInterval = 0.35f;
    public float stuckDistanceThreshold = 0.08f;
    public float stuckRecoverySeconds = 0.8f;
    public float stuckRecoveryTurnAngle = 80f;

    [Header("Performance")]
    [Tooltip("How often AI logic runs. Higher value = lower CPU use.")]
    [Range(0.02f, 0.5f)] public float aiThinkInterval = 0.08f;
    [Tooltip("How often player target references are refreshed when needed.")]
    [Range(0.1f, 2f)] public float playerResolveInterval = 0.5f;
    [Tooltip("Minimum target movement before refreshing the current chase destination.")]
    [Range(0.05f, 3f)] public float minRepathDistance = 0.75f;

    private float _nextAttackTime;
    private float _attackAnimUnlockTime;
    private Vector3 _roamAnchor;
    private float _nextRoamRepathTime;
    private float _nextThinkTime;
    private float _nextResolvePlayerTime;
    private Vector3 _lastRequestedDestination;
    private bool _hasRequestedDestination;
    private Vector3 _currentVelocity;
    private bool _hasMoveDestination;
    private Vector3 _moveDestination;
    private float _moveStopDistance;
    private float _nextDebugLogTime;
    private bool _wasPlayerInRangeLastFrame;
    private float _nextStuckCheckTime;
    private Vector3 _lastStuckCheckPosition;
    private float _stuckRecoveryUntil;
    private float _stuckTurnSign = 1f;

    protected virtual void Awake()
    {
        DisableUnityNavMeshAgentComponent();
        DisableAnimatorRootMotion();
    }

    protected virtual void Start()
    {
        if (enemiesHandler != null && !enemiesHandler.enemies.Contains(gameObject))
        {
            enemiesHandler.enemies.Add(gameObject);
        }

        _roamAnchor = transform.position;
        _lastStuckCheckPosition = transform.position;
        if (detectionRange <= 0f)
        {
            detectionRange = followRange;
        }

        _nextThinkTime = Time.time + Random.Range(0f, Mathf.Max(0.02f, aiThinkInterval));
        _nextResolvePlayerTime = 0f;
        _hasRequestedDestination = false;
    }

    protected virtual void Update()
    {
        if (Time.time >= _nextThinkTime)
        {
            _nextThinkTime = Time.time + Mathf.Max(0.02f, aiThinkInterval);
            Think();
        }

        MoveAlongDestination(Time.deltaTime);
        UpdateStuckRecovery();
    }

    protected abstract void OnEnemyAttack();
    protected abstract void SetWalkAnimation(bool status);

    public void LockActions(float seconds)
    {
        float lockUntil = Time.time + Mathf.Max(0f, seconds);
        if (lockUntil > _attackAnimUnlockTime)
        {
            _attackAnimUnlockTime = lockUntil;
        }

        StopMoving();
        SetWalkAnimation(false);
    }

    protected void TriggerEnemyAttack()
    {
        StopMoving();
        FacePlayer();
        SetWalkAnimation(false);
        OnEnemyAttack();
    }

    private void Think()
    {
        ResolvePlayerTransformThrottled();

        if (playertransform == null)
        {
            StopMoving();
            SetWalkAnimation(false);
            return;
        }

        if (Time.time < _attackAnimUnlockTime)
        {
            StopMoving();
            FacePlayer();
            SetWalkAnimation(false);
            return;
        }

        float distanceToPlayer = HorizontalDistance(transform.position, playertransform.position);
        float effectiveFollowRange = GetEffectiveFollowRange();
        bool playerInRange = distanceToPlayer <= effectiveFollowRange;

        DebugPlayerDistance(distanceToPlayer, effectiveFollowRange, playerInRange);

        if (!playerInRange)
        {
            TryRoamAroundSpawn();
            return;
        }

        _nextRoamRepathTime = 0f;
        if (IsPlayerInAttackRange(distanceToPlayer))
        {
            StopMoving();
            FacePlayer();
            SetWalkAnimation(false);
            TryAttack();
            return;
        }

        ChasePlayer();
    }

    private void ResolvePlayerTransform()
    {
        Transform resolved = null;

        if (lookingController != null)
        {
            if (PlayerNormal != null)
            {
                resolved = PlayerNormal.transform;
            }
            else if (lookingController.normalcapsule != null)
            {
                resolved = lookingController.normalcapsule.transform;
            }
        }

        if (resolved == null)
        {
            if (PlayerNormal != null && PlayerNormal.activeInHierarchy)
            {
                resolved = PlayerNormal.transform;
            }
            else if (PlayerNormal != null)
            {
                resolved = PlayerNormal.transform;
            }
        }

        if (resolved == null)
        {
            resolved = TryFindPlayerByTag();
        }

        playertransform = resolved;
    }

    private void ResolvePlayerTransformThrottled()
    {
        if (Time.time < _nextResolvePlayerTime)
        {
            return;
        }

        _nextResolvePlayerTime = Time.time + Mathf.Max(0.1f, playerResolveInterval);
        ResolvePlayerTransform();
    }

    private Transform TryFindPlayerByTag()
    {
        if (!autoFindPlayerByTag || string.IsNullOrWhiteSpace(playerTag))
        {
            return null;
        }

        try
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag(playerTag);
            return taggedPlayer != null ? taggedPlayer.transform : null;
        }
        catch (UnityException)
        {
            return null;
        }
    }

    private void ChasePlayer()
    {
        if (playertransform == null)
        {
            return;
        }

        Vector3 target = playertransform.position;
        float minDelta = Mathf.Max(0.01f, minRepathDistance);
        bool shouldRefreshDestination = !_hasRequestedDestination ||
                                        (target - _lastRequestedDestination).sqrMagnitude >= minDelta * minDelta;

        if (!shouldRefreshDestination && _hasMoveDestination)
        {
            return;
        }

        SetMoveDestination(target, Mathf.Max(0.1f, attackRange * 0.8f));
        _lastRequestedDestination = target;
        _hasRequestedDestination = true;
    }

    private void StopMoving()
    {
        _hasMoveDestination = false;
        _hasRequestedDestination = false;
        _currentVelocity = Vector3.zero;
    }

    private void TryAttack()
    {
        if (Time.time < _nextAttackTime)
        {
            return;
        }

        _nextAttackTime = Time.time + Mathf.Max(0f, attackCooldown);
        _attackAnimUnlockTime = Time.time + Mathf.Max(0f, attackAnimLockSeconds);
        TriggerEnemyAttack();
    }

    private void TryRoamAroundSpawn()
    {
        if (!enableRoaming || roamRadius <= 0.1f)
        {
            StopMoving();
            SetWalkAnimation(false);
            return;
        }

        if (_hasMoveDestination && Time.time < _nextRoamRepathTime)
        {
            return;
        }

        if (!TryGetRoamDestination(out Vector3 roamDestination))
        {
            StopMoving();
            SetWalkAnimation(false);
            _nextRoamRepathTime = Time.time + 0.5f;
            return;
        }

        SetMoveDestination(roamDestination, Mathf.Max(0.2f, destinationReachDistance));
        _nextRoamRepathTime = Time.time + Mathf.Max(0.1f, roamRepathInterval);
        _lastRequestedDestination = roamDestination;
        _hasRequestedDestination = true;
    }

    private bool TryGetRoamDestination(out Vector3 destination)
    {
        destination = Vector3.zero;
        int tries = Mathf.Max(1, roamDestinationTries);
        float minDistance = Mathf.Max(0f, roamMinMoveDistance);

        for (int i = 0; i < tries; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
            Vector3 candidate = _roamAnchor + new Vector3(randomCircle.x, 0f, randomCircle.y);
            if (!TryProjectToGround(candidate, out Vector3 groundedCandidate))
            {
                continue;
            }

            if (HorizontalDistance(transform.position, groundedCandidate) < minDistance)
            {
                continue;
            }

            if (IsOwnSpaceBlocked(groundedCandidate))
            {
                continue;
            }

            destination = groundedCandidate;
            return true;
        }

        return false;
    }

    private bool IsPlayerInAttackRange(float distanceToPlayer)
    {
        float effectiveAttackRange = Mathf.Max(0.1f, attackRange + Mathf.Max(0f, attackRangeBuffer));
        return distanceToPlayer <= effectiveAttackRange;
    }

    private float GetEffectiveFollowRange()
    {
        float configuredFollowRange = Mathf.Max(0f, followRange);
        float configuredDetectionRange = Mathf.Max(0f, detectionRange);
        float configuredRange = Mathf.Max(configuredFollowRange, configuredDetectionRange);
        return Mathf.Max(attackRange + 0.1f, configuredRange);
    }

    private void SetMoveDestination(Vector3 destination, float stopDistance)
    {
        if (TryProjectToGround(destination, out Vector3 groundedDestination))
        {
            destination = groundedDestination;
        }

        _moveDestination = destination;
        _moveStopDistance = Mathf.Max(0.05f, stopDistance);
        _hasMoveDestination = true;
    }

    private void MoveAlongDestination(float deltaTime)
    {
        if (!_hasMoveDestination || deltaTime <= 0f)
        {
            _currentVelocity = Vector3.MoveTowards(_currentVelocity, Vector3.zero, acceleration * deltaTime);
            SetWalkAnimation(false);
            SnapToGround();
            return;
        }

        Vector3 toDestination = _moveDestination - transform.position;
        toDestination.y = 0f;
        float distance = toDestination.magnitude;
        if (distance <= Mathf.Max(0.05f, _moveStopDistance))
        {
            StopMoving();
            SetWalkAnimation(false);
            return;
        }

        Vector3 desiredDirection = toDestination / distance;
        desiredDirection = ApplySeparation(desiredDirection);

        if (Time.time < _stuckRecoveryUntil)
        {
            desiredDirection = Quaternion.Euler(0f, _stuckTurnSign * stuckRecoveryTurnAngle, 0f) * desiredDirection;
        }

        Vector3 openDirection = FindBestOpenDirection(desiredDirection);
        Vector3 desiredVelocity = openDirection * Mathf.Max(0f, moveSpeed);
        _currentVelocity = Vector3.MoveTowards(_currentVelocity, desiredVelocity, acceleration * deltaTime);

        float moveDistance = _currentVelocity.magnitude * deltaTime;
        if (moveDistance > 0.0001f)
        {
            Vector3 moveDirection = _currentVelocity.normalized;
            if (TryGetBlockingHit(moveDirection, moveDistance + obstacleSkinWidth, out RaycastHit hit))
            {
                Vector3 slideDirection = Vector3.ProjectOnPlane(moveDirection, hit.normal);
                slideDirection.y = 0f;
                if (slideDirection.sqrMagnitude > 0.0001f &&
                    !TryGetBlockingHit(slideDirection.normalized, moveDistance * 0.75f + obstacleSkinWidth, out _))
                {
                    transform.position += slideDirection.normalized * moveDistance * 0.75f;
                }
                else
                {
                    BeginStuckRecovery();
                    _currentVelocity = Vector3.zero;
                }
            }
            else
            {
                transform.position += moveDirection * moveDistance;
            }

            FaceDirection(_currentVelocity.sqrMagnitude > 0.0001f ? _currentVelocity.normalized : openDirection);
        }

        SnapToGround();
        SetWalkAnimation(_currentVelocity.sqrMagnitude > 0.05f);
    }

    private Vector3 ApplySeparation(Vector3 desiredDirection)
    {
        float safeRadius = Mathf.Max(0f, separationRadius);
        if (safeRadius <= 0f || separationStrength <= 0f)
        {
            return desiredDirection;
        }

        Collider[] overlaps = Physics.OverlapSphere(transform.position, safeRadius, obstacleLayerMask, QueryTriggerInteraction.Ignore);
        Vector3 separation = Vector3.zero;
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider candidate = overlaps[i];
            if (candidate == null || candidate.transform.IsChildOf(transform))
            {
                continue;
            }

            CustomEnemyAIBase otherEnemy = candidate.GetComponentInParent<CustomEnemyAIBase>();
            if (otherEnemy == null || otherEnemy == this)
            {
                continue;
            }

            Vector3 away = transform.position - otherEnemy.transform.position;
            away.y = 0f;
            float distanceSqr = away.sqrMagnitude;
            if (distanceSqr <= 0.0001f)
            {
                continue;
            }

            separation += away.normalized / Mathf.Max(0.25f, Mathf.Sqrt(distanceSqr));
        }

        if (separation.sqrMagnitude <= 0.0001f)
        {
            return desiredDirection;
        }

        Vector3 blended = desiredDirection + separation.normalized * separationStrength;
        blended.y = 0f;
        return blended.sqrMagnitude > 0.0001f ? blended.normalized : desiredDirection;
    }

    private Vector3 FindBestOpenDirection(Vector3 desiredDirection)
    {
        if (!TryGetBlockingHit(desiredDirection, obstacleProbeDistance, out _))
        {
            return desiredDirection;
        }

        float[] testAngles = { 25f, -25f, 45f, -45f, 70f, -70f, 100f, -100f, 135f, -135f };
        Vector3 bestDirection = Vector3.zero;
        float bestScore = float.MinValue;

        for (int i = 0; i < testAngles.Length; i++)
        {
            Vector3 candidate = Quaternion.Euler(0f, testAngles[i], 0f) * desiredDirection;
            float probeDistance = i < 4 ? obstacleSideProbeDistance : obstacleProbeDistance;
            if (TryGetBlockingHit(candidate, probeDistance, out _))
            {
                continue;
            }

            float alignmentScore = Vector3.Dot(candidate.normalized, desiredDirection);
            if (alignmentScore > bestScore)
            {
                bestScore = alignmentScore;
                bestDirection = candidate.normalized;
            }
        }

        if (bestDirection.sqrMagnitude > 0.0001f)
        {
            return bestDirection;
        }

        BeginStuckRecovery();
        return Quaternion.Euler(0f, _stuckTurnSign * stuckRecoveryTurnAngle, 0f) * desiredDirection;
    }

    private bool TryGetBlockingHit(Vector3 direction, float distance, out RaycastHit bestHit)
    {
        bestHit = default;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f || distance <= 0f)
        {
            return false;
        }

        Vector3 normalizedDirection = direction.normalized;
        GetCapsulePoints(transform.position, out Vector3 bottom, out Vector3 top);
        float radius = Mathf.Max(0.05f, bodyRadius);
        RaycastHit[] hits = Physics.CapsuleCastAll(
            bottom,
            top,
            radius,
            normalizedDirection,
            Mathf.Max(0.01f, distance),
            obstacleLayerMask,
            QueryTriggerInteraction.Ignore);

        float bestDistance = float.MaxValue;
        bool found = false;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            Collider hitCollider = hit.collider;
            if (ShouldIgnoreMovementCollider(hitCollider))
            {
                continue;
            }

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHit = hit;
                found = true;
            }
        }

        return found;
    }

    private bool IsOwnSpaceBlocked(Vector3 position)
    {
        GetCapsulePoints(position, out Vector3 bottom, out Vector3 top);
        Collider[] overlaps = Physics.OverlapCapsule(
            bottom,
            top,
            Mathf.Max(0.05f, bodyRadius),
            obstacleLayerMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap = overlaps[i];
            if (ShouldIgnoreMovementCollider(overlap))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void GetCapsulePoints(Vector3 basePosition, out Vector3 bottom, out Vector3 top)
    {
        float radius = Mathf.Max(0.05f, bodyRadius);
        float height = Mathf.Max(radius * 2f, bodyHeight);
        bottom = basePosition + Vector3.up * radius;
        top = basePosition + Vector3.up * (height - radius);
    }

    private void SnapToGround()
    {
        if (TryProjectToGround(transform.position, out Vector3 groundedPosition))
        {
            transform.position = groundedPosition;
        }
    }

    private bool TryProjectToGround(Vector3 position, out Vector3 groundedPosition)
    {
        groundedPosition = position;
        Vector3 rayStart = position + Vector3.up * Mathf.Max(0.1f, groundProbeHeight);
        float rayDistance = Mathf.Max(0.1f, groundProbeHeight + groundProbeDistance);
        RaycastHit[] hits = Physics.RaycastAll(
            rayStart,
            Vector3.down,
            rayDistance,
            groundLayerMask,
            QueryTriggerInteraction.Ignore);

        float bestY = float.NegativeInfinity;
        bool found = false;
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (ShouldIgnoreGroundCollider(hit.collider) || hit.normal.y < minimumGroundNormalY)
            {
                continue;
            }

            if (hit.point.y > bestY)
            {
                bestY = hit.point.y;
                groundedPosition = new Vector3(position.x, hit.point.y, position.z);
                found = true;
            }
        }

        if (found)
        {
            return true;
        }

        Terrain terrain = FindTerrainAt(position);
        if (terrain == null)
        {
            return false;
        }

        groundedPosition = new Vector3(
            position.x,
            terrain.SampleHeight(position) + terrain.transform.position.y,
            position.z);
        return true;
    }

    private static Terrain FindTerrainAt(Vector3 worldPosition)
    {
        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++)
        {
            Terrain terrain = terrains[i];
            if (terrain == null || terrain.terrainData == null)
            {
                continue;
            }

            Vector3 terrainPosition = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;
            if (worldPosition.x >= terrainPosition.x &&
                worldPosition.x <= terrainPosition.x + size.x &&
                worldPosition.z >= terrainPosition.z &&
                worldPosition.z <= terrainPosition.z + size.z)
            {
                return terrain;
            }
        }

        return null;
    }

    private void FacePlayer()
    {
        if (playertransform == null)
        {
            return;
        }

        Vector3 toPlayer = playertransform.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        FaceDirection(toPlayer.normalized);
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * Mathf.Max(0.1f, rotationSpeed));
    }

    private void UpdateStuckRecovery()
    {
        if (!_hasMoveDestination || Time.time < _nextStuckCheckTime)
        {
            return;
        }

        _nextStuckCheckTime = Time.time + Mathf.Max(0.05f, stuckCheckInterval);
        float requiredMove = Mathf.Max(0.01f, stuckDistanceThreshold);
        float movedSqr = (transform.position - _lastStuckCheckPosition).sqrMagnitude;
        _lastStuckCheckPosition = transform.position;

        if (movedSqr >= requiredMove * requiredMove)
        {
            return;
        }

        if (HorizontalDistance(transform.position, _moveDestination) <= _moveStopDistance + 0.5f)
        {
            return;
        }

        BeginStuckRecovery();
    }

    private void BeginStuckRecovery()
    {
        _stuckRecoveryUntil = Time.time + Mathf.Max(0.05f, stuckRecoverySeconds);
        _stuckTurnSign = Random.value < 0.5f ? -1f : 1f;
        _nextRoamRepathTime = 0f;
    }

    private void DebugPlayerDistance(float distanceToPlayer, float effectiveFollowRange, bool playerInRange)
    {
        if (!debugRangeLogs)
        {
            _wasPlayerInRangeLastFrame = playerInRange;
            return;
        }

        bool rangeStateChanged = playerInRange != _wasPlayerInRangeLastFrame;
        bool shouldLogNow = Time.time >= _nextDebugLogTime || rangeStateChanged;
        if (shouldLogNow)
        {
            string stateText = playerInRange ? "IN RANGE -> CHASE" : "OUT OF RANGE -> ROAM";
            Debug.Log(
                $"[{GetType().Name}:{name}] distance={distanceToPlayer:F2}, range={effectiveFollowRange:F2}, state={stateText}",
                this);
            _nextDebugLogTime = Time.time + Mathf.Max(0.05f, debugLogInterval);
        }

        _wasPlayerInRangeLastFrame = playerInRange;
    }

    private void DisableUnityNavMeshAgentComponent()
    {
        Component navMeshAgentComponent = GetComponent("NavMeshAgent");
        if (navMeshAgentComponent is Behaviour navMeshAgentBehaviour)
        {
            navMeshAgentBehaviour.enabled = false;
        }
    }

    private void DisableAnimatorRootMotion()
    {
        if (!disableAnimatorRootMotion)
        {
            return;
        }

        Animator[] animators = GetComponentsInChildren<Animator>();
        for (int i = 0; i < animators.Length; i++)
        {
            if (animators[i] != null)
            {
                animators[i].applyRootMotion = false;
            }
        }
    }

    private bool ShouldIgnoreMovementCollider(Collider candidate)
    {
        if (candidate == null || candidate is TerrainCollider)
        {
            return true;
        }

        Transform candidateTransform = candidate.transform;
        if (candidateTransform == null || candidateTransform.IsChildOf(transform))
        {
            return true;
        }

        if (IsPlayerTransform(candidateTransform))
        {
            return true;
        }

        return candidate.GetComponentInParent<CustomEnemyAIBase>() != null;
    }

    private bool ShouldIgnoreGroundCollider(Collider candidate)
    {
        if (candidate == null)
        {
            return true;
        }

        Transform candidateTransform = candidate.transform;
        if (candidateTransform == null || candidateTransform.IsChildOf(transform))
        {
            return true;
        }

        if (IsPlayerTransform(candidateTransform))
        {
            return true;
        }

        return candidate.GetComponentInParent<CustomEnemyAIBase>() != null;
    }

    private bool IsPlayerTransform(Transform candidate)
    {
        if (IsSameOrChild(candidate, playertransform))
        {
            return true;
        }

        if (PlayerNormal != null && IsSameOrChild(candidate, PlayerNormal.transform))
        {
            return true;
        }

        if (lookingController != null)
        {
            if (IsSameOrChild(candidate, lookingController.transform))
            {
                return true;
            }

            if (lookingController.normalcapsule != null && IsSameOrChild(candidate, lookingController.normalcapsule.transform))
            {
                return true;
            }

        }

        return false;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static bool IsSameOrChild(Transform candidate, Transform root)
    {
        return candidate != null && root != null && (candidate == root || candidate.IsChildOf(root));
    }

    private void OnDrawGizmosSelected()
    {
        float effectiveFollowRange = GetEffectiveFollowRange();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, effectiveFollowRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (_hasMoveDestination)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, _moveDestination);
            Gizmos.DrawWireSphere(_moveDestination, destinationReachDistance);
        }
    }
}

public class RandomZombieScript : CustomEnemyAIBase
{
    [Header("Zombie References")]
    public ZombieAnimationScript zombieAnimationScript;

    [Header("Throw Visual")]
    public GameObject thrownItemPrefab;
    public Transform throwOrigin;
    public float throwSpawnDelay = 0.35f;
    public float throwArcHeight = 1.2f;
    public float throwTravelTime = 0.7f;
    public float throwTargetHeightOffset = 1.0f;
    public float thrownItemLifetimeAfterImpact = 0.1f;

    protected override void Awake()
    {
        base.Awake();

        if (zombieAnimationScript == null)
        {
            zombieAnimationScript = GetComponent<ZombieAnimationScript>();
            if (zombieAnimationScript == null)
            {
                zombieAnimationScript = GetComponentInChildren<ZombieAnimationScript>();
            }
        }
    }

    public void Attack()
    {
        TriggerEnemyAttack();
    }

    protected override void OnEnemyAttack()
    {
        if (zombieAnimationScript != null)
        {
            zombieAnimationScript.ThrowAnim();
        }

        if (thrownItemPrefab != null)
        {
            StartCoroutine(ThrowItemRoutine());
        }
    }

    protected override void SetWalkAnimation(bool status)
    {
        if (zombieAnimationScript != null)
        {
            zombieAnimationScript.MoveAnim(status);
        }
    }

    private IEnumerator ThrowItemRoutine()
    {
        if (throwSpawnDelay > 0f)
        {
            yield return new WaitForSeconds(throwSpawnDelay);
        }

        if (playertransform == null || thrownItemPrefab == null)
        {
            yield break;
        }

        Vector3 startPos = throwOrigin != null ? throwOrigin.position : transform.position + Vector3.up * 1.4f;
        Vector3 endPos = playertransform.position + Vector3.up * throwTargetHeightOffset;

        GameObject thrownItem = Instantiate(thrownItemPrefab, startPos, Quaternion.identity);
        if (thrownItem == null)
        {
            yield break;
        }

        EnsureProjectileVfxIsPlaying(thrownItem);

        Rigidbody rb = thrownItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        yield return StartCoroutine(MoveProjectileArc(thrownItem.transform, startPos, endPos, throwTravelTime, throwArcHeight));

        if (thrownItem != null)
        {
            if (thrownItemLifetimeAfterImpact > 0f)
            {
                Destroy(thrownItem, thrownItemLifetimeAfterImpact);
            }
            else
            {
                Destroy(thrownItem);
            }
        }
    }

    private IEnumerator MoveProjectileArc(Transform projectile, Vector3 startPos, Vector3 endPos, float travelTime, float arcHeight)
    {
        if (projectile == null)
        {
            yield break;
        }

        float safeTravelTime = Mathf.Max(0.05f, travelTime);
        float elapsed = 0f;
        Vector3 previousPos = startPos;

        while (elapsed < safeTravelTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeTravelTime);
            Vector3 linearPos = Vector3.Lerp(startPos, endPos, t);
            float arcOffset = 4f * arcHeight * t * (1f - t);
            Vector3 nextPos = linearPos + Vector3.up * arcOffset;

            projectile.position = nextPos;

            Vector3 forward = nextPos - previousPos;
            if (forward.sqrMagnitude > 0.0001f)
            {
                projectile.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
            }

            previousPos = nextPos;
            yield return null;
        }

        if (projectile != null)
        {
            projectile.position = endPos;
        }
    }

    private static void EnsureProjectileVfxIsPlaying(GameObject projectileRoot)
    {
        if (projectileRoot == null)
        {
            return;
        }

        VisualEffect[] effects = projectileRoot.GetComponentsInChildren<VisualEffect>(true);
        foreach (VisualEffect effect in effects)
        {
            if (effect == null)
            {
                continue;
            }

            effect.enabled = true;
            effect.Reinit();
            effect.Play();
        }
    }
}
