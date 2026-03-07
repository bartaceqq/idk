using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

// Controls Random Zombie Script behavior.
public class RandomZombieScript : MonoBehaviour
{
    [Header("References")]
    public LookingController lookingController;
    public GameObject PlayerNormal;
    public GameObject PlayerBuilding;
    public Transform playertransform;
    public NavMeshAgent navMeshAgent;
    public ZombieAnimationScript zombieAnimationScript;
    
  

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

    [Header("Roaming")]
    public bool enableRoaming = true;
    public float roamRadius = 25f;
    public float roamRepathInterval = 2f;
    public float roamMinMoveDistance = 3f;
    public int roamDestinationTries = 8;

    [Header("Throw Visual")]
    public GameObject thrownItemPrefab;
    public Transform throwOrigin;
    public float throwSpawnDelay = 0.35f;
    public float throwArcHeight = 1.2f;
    public float throwTravelTime = 0.7f;
    public float throwTargetHeightOffset = 1.0f;
    public float thrownItemLifetimeAfterImpact = 0.1f;

    private float _nextAttackTime;
    private float _attackAnimUnlockTime;
    private Vector3 _roamAnchor;
    private float _nextRoamRepathTime;

    void Awake()
    {
        if (navMeshAgent == null)
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }
        if (zombieAnimationScript == null)
        {
            zombieAnimationScript = GetComponent<ZombieAnimationScript>();
            if (zombieAnimationScript == null)
            {
                zombieAnimationScript = GetComponentInChildren<ZombieAnimationScript>();
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

        if (Time.time < _attackAnimUnlockTime)
        {
            StopMoving();
            FacePlayer();
            SetWalkAnimation(false);
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, playertransform.position);
        float rangeToUse = detectionRange > 0f ? detectionRange : followRange;
        float effectiveFollowRange = Mathf.Max(attackRange + 0.1f, rangeToUse);

        if (distanceToPlayer > effectiveFollowRange)
        {
            TryRoamAroundSpawn();
            return;
        }

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
        Attack();
    }

    // Handle Attack.
    public void Attack()
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

    // Handle Set Walk Animation.
    private void SetWalkAnimation(bool status)
    {
        if (zombieAnimationScript != null)
        {
            zombieAnimationScript.MoveAnim(status);
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

    // Handle Throw Item Routine.
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

    // Handle Move Projectile Arc.
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

    // Handle Ensure Projectile Vfx Is Playing.
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
