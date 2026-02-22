using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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
    public float attackRange = 2f;
    public float attackCooldown = 1.2f;
    public float attackAnimLockSeconds = 0.8f;
    public EnemiesHandler enemiesHandler;

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

        if (navMeshAgent != null)
        {
            navMeshAgent.stoppingDistance = attackRange;
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

        if (distanceToPlayer > followRange)
        {
            StopMoving();
            SetWalkAnimation(false);
            return;
        }

        if (distanceToPlayer <= attackRange)
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

        playertransform = resolved;
    }

    private void ChasePlayer()
    {
        if (!CanUseNavMeshAgent() || playertransform == null)
        {
            return;
        }

        navMeshAgent.isStopped = false;
        navMeshAgent.SetDestination(playertransform.position);
    }

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

    private void SetWalkAnimation(bool status)
    {
        if (zombieAnimationScript != null)
        {
            zombieAnimationScript.MoveAnim(status);
        }
    }

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

    private bool CanUseNavMeshAgent()
    {
        return navMeshAgent != null
               && navMeshAgent.enabled
               && navMeshAgent.gameObject.activeInHierarchy
               && navMeshAgent.isOnNavMesh;
    }
}
