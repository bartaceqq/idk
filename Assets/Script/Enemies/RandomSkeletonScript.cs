using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

// Controls Random Skeleton Script behavior.
public class RandomSkeletonScript : MonoBehaviour
{
    [Header("References")]
    public LookingController lookingController;
    public GameObject PlayerNormal;
    public GameObject PlayerBuilding;
    public Transform playertransform;
    public NavMeshAgent navMeshAgent;
    public SkeletonAnimatopmScript skeletonAnimationScript;
  

    [Header("Behavior")]
    public float followRange = 15f;
    public float detectionRange = 15f;
    public float attackRange = 2f;
    public float attackRangeBuffer = 0.35f;
    public float attackCooldown = 1.2f;
    public float attackAnimLockSeconds = 0.8f;
    public EnemiesHandler enemiesHandler;

    [Header("Detection")]
    public bool autoFindPlayerByTag = true;
    public string playerTag = "Player";
    public bool debugRangeLogs = true;
    public float debugLogInterval = 0.25f;

    [Header("Roaming")]
    public bool enableRoaming = true;
    public float roamRadius = 25f;
    public float roamRepathInterval = 2f;
    public float roamMinMoveDistance = 3f;
    public int roamDestinationTries = 8;

    private float _nextAttackTime;
    private float _attackAnimUnlockTime;
    private Vector3 _roamAnchor;
    private float _nextRoamRepathTime;
    private float _nextDebugLogTime;
    private bool _wasPlayerInRangeLastFrame;

    void Awake()
    {
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }
        if (skeletonAnimationScript == null)
        {
            skeletonAnimationScript = GetComponent<SkeletonAnimatopmScript>();
            if (skeletonAnimationScript == null)
            {
                skeletonAnimationScript = GetComponentInChildren<SkeletonAnimatopmScript>();
            }
        }
    }

    void Start()
    {
        if (enemiesHandler != null && !enemiesHandler.enemies.Contains(gameObject))
        {
            enemiesHandler.enemies.Add(gameObject);
        }

        _roamAnchor = transform.position;
        if (detectionRange <= 0f)
        {
            detectionRange = followRange;
        }

        if (navMeshAgent != null)
        {
            navMeshAgent.stoppingDistance = Mathf.Max(0.1f, attackRange - 0.25f);
        }
    }

    void Update()
    {
        ResolvePlayerTransform();

        if (!CanUseNavMeshAgent())
        {
            SetWalkAnimation(false);
            return;
        }
        if (playertransform == null)
        {
            StopMoving();
            SetWalkAnimation(false);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playertransform.position);
        float rangeToUse = detectionRange > 0f ? detectionRange : followRange;
        float effectiveFollowRange = Mathf.Max(attackRange + 0.1f, rangeToUse);
        bool playerInRange = distanceToPlayer <= effectiveFollowRange;

        DebugPlayerDistance(distanceToPlayer, effectiveFollowRange, playerInRange);

        if (!playerInRange)
        {
            TryRoamAroundSpawn();
            return;
        }

        // Player entered detection range: cancel locked/roam behavior and chase now.
        _attackAnimUnlockTime = 0f;
        _nextRoamRepathTime = 0f;

        bool playerInAttackRange = IsPlayerInAttackRange(distanceToPlayer);
        if (playerInAttackRange)
        {
            StopMoving();
            FacePlayer();
            SetWalkAnimation(false);
            TryAttack();
            return;
        }

        ChasePlayer();
        SetWalkAnimation(true);
    }

    // Handle Resolve Player Transform.
    private void ResolvePlayerTransform()
    {
        Transform resolved = null;

        if (lookingController != null)
        {
            if (lookingController.switched)
            {
                // switched == true => building capsule is active
                if (PlayerBuilding != null)
                {
                    resolved = PlayerBuilding.transform;
                }
                else if (lookingController.buildingcapsule != null)
                {
                    resolved = lookingController.buildingcapsule.transform;
                }
            }
            else
            {
                // switched == false => normal capsule is active
                if (PlayerNormal != null)
                {
                    resolved = PlayerNormal.transform;
                }
                else if (lookingController.normalcapsule != null)
                {
                    resolved = lookingController.normalcapsule.transform;
                }
            }
        }

        if (resolved == null)
        {
            if (PlayerNormal != null && PlayerNormal.activeInHierarchy)
            {
                resolved = PlayerNormal.transform;
            }
            else if (PlayerBuilding != null && PlayerBuilding.activeInHierarchy)
            {
                resolved = PlayerBuilding.transform;
            }
            else if (PlayerNormal != null)
            {
                resolved = PlayerNormal.transform;
            }
            else if (PlayerBuilding != null)
            {
                resolved = PlayerBuilding.transform;
            }
        }

        if (resolved == null)
        {
            resolved = TryFindPlayerByTag();
        }

        playertransform = resolved;
    }

    // Handle Try Find Player By Tag.
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

    // Handle Chase Player.
    private void ChasePlayer()
    {
        if (!CanUseNavMeshAgent() || playertransform == null)
        {
            return;
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(playertransform.position);
    }

    // Handle Stop Moving.
    private void StopMoving()
    {
        if (!CanUseNavMeshAgent())
        {
            return;
        }

        navMeshAgent.isStopped = true;
        if (navMeshAgent.hasPath)
        {
            navMeshAgent.ResetPath();
        }
    }

    // Handle Face Player.
    private void FacePlayer()
    {
        Vector3 toPlayer = playertransform.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }

    // Handle Try Attack.
    private void TryAttack()
    {
        if (Time.time < _nextAttackTime)
        {
            return;
        }

        _nextAttackTime = Time.time + attackCooldown;
        _attackAnimUnlockTime = Time.time + attackAnimLockSeconds;
        if (debugRangeLogs)
        {
            Debug.Log($"[Skeleton:{name}] ATTACK triggered", this);
        }
        Attack();
    }

    // Handle Attack.
    public void Attack()
    {
        if (skeletonAnimationScript != null)
        {
            skeletonAnimationScript.ThrowAnim();
        }
    }

    // Handle Set Walk Animation.
    private void SetWalkAnimation(bool status)
    {
        if (skeletonAnimationScript != null)
        {
            skeletonAnimationScript.MoveAnim(status);
        }
    }

    // Handle Lock Actions.
    public void LockActions(float seconds)
    {
        float lockUntil = Time.time + Mathf.Max(0f, seconds);
        if (lockUntil > _attackAnimUnlockTime)
        {
            _attackAnimUnlockTime = lockUntil;
        }

        if (CanUseNavMeshAgent())
        {
            StopMoving();
        }
        SetWalkAnimation(false);
    }

    // Handle Try Roam Around Spawn.
    private void TryRoamAroundSpawn()
    {
        if (!CanUseNavMeshAgent())
        {
            SetWalkAnimation(false);
            return;
        }

        if (!enableRoaming || roamRadius <= 0.1f)
        {
            StopMoving();
            SetWalkAnimation(false);
            return;
        }

        bool reachedDestination =
            !navMeshAgent.pathPending &&
            navMeshAgent.hasPath &&
            navMeshAgent.remainingDistance <= Mathf.Max(navMeshAgent.stoppingDistance + 0.2f, 0.35f);

        if (reachedDestination)
        {
            _nextRoamRepathTime = 0f;
        }

        if (Time.time < _nextRoamRepathTime && navMeshAgent.hasPath)
        {
            navMeshAgent.isStopped = false;
            SetWalkAnimation(true);
            return;
        }

        if (!TryGetRoamDestination(out Vector3 roamDestination))
        {
            StopMoving();
            SetWalkAnimation(false);
            _nextRoamRepathTime = Time.time + 0.5f;
            return;
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(roamDestination);
        SetWalkAnimation(true);
        _nextRoamRepathTime = Time.time + Mathf.Max(0.1f, roamRepathInterval);
    }

    // Handle Try Get Roam Destination.
    private bool TryGetRoamDestination(out Vector3 destination)
    {
        destination = Vector3.zero;
        int tries = Mathf.Max(1, roamDestinationTries);
        float minDistance = Mathf.Max(0f, roamMinMoveDistance);

        for (int i = 0; i < tries; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * roamRadius;
            Vector3 candidate = _roamAnchor + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, Mathf.Max(1f, roamRadius * 0.5f), NavMesh.AllAreas))
            {
                if (Vector3.Distance(transform.position, hit.position) < minDistance)
                {
                    continue;
                }

                destination = hit.position;
                return true;
            }
        }

        return false;
    }

    // Handle Is Player In Attack Range.
    private bool IsPlayerInAttackRange(float distanceToPlayer)
    {
        float effectiveAttackRange = Mathf.Max(0.1f, attackRange + Mathf.Max(0f, attackRangeBuffer));
        if (distanceToPlayer <= effectiveAttackRange)
        {
            return true;
        }

        if (!CanUseNavMeshAgent())
        {
            return false;
        }

        if (navMeshAgent.pathPending)
        {
            return false;
        }

        if (navMeshAgent.hasPath)
        {
            float remaining = navMeshAgent.remainingDistance;
            float stopThreshold = Mathf.Max(navMeshAgent.stoppingDistance + 0.2f, effectiveAttackRange);
            return remaining <= stopThreshold;
        }

        return false;
    }

    // Handle Debug Player Distance.
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
                $"[Skeleton:{name}] distance={distanceToPlayer:F2}, range={effectiveFollowRange:F2}, state={stateText}",
                this);
            _nextDebugLogTime = Time.time + Mathf.Max(0.05f, debugLogInterval);
        }

        _wasPlayerInRangeLastFrame = playerInRange;
    }

    // Handle Can Use Nav Mesh Agent.
    private bool CanUseNavMeshAgent()
    {
        return navMeshAgent != null
               && navMeshAgent.enabled
               && navMeshAgent.gameObject.activeInHierarchy
               && navMeshAgent.isOnNavMesh;
    }

    // Draw follow/attack ranges for tuning.
    private void OnDrawGizmosSelected()
    {
        float rangeToUse = detectionRange > 0f ? detectionRange : followRange;
        float effectiveFollowRange = Mathf.Max(attackRange + 0.1f, rangeToUse);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, effectiveFollowRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
