using System.Collections.Generic;
using UnityEngine;

// Controls proximity-based enemy hit detection for sword attacks.
public class RadiusForAttackScript : MonoBehaviour
{
    public GameObject player;
    public EnemiesHandler enemiesHandler;
    public float attackRadius = 5f;
    public float attackDamage = 40f;
    public LayerMask enemyMask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    private readonly Collider[] _overlapHits = new Collider[128];
    private readonly HashSet<NPCDemageScript> _uniqueTargets = new HashSet<NPCDemageScript>();

    void Awake()
    {
        ResolveReferences();
    }

    // Handle Resolve References.
    private void ResolveReferences()
    {
        if (player == null)
        {
            player = gameObject;
        }

        if (enemiesHandler == null)
        {
            enemiesHandler = FindFirstObjectByType<EnemiesHandler>();
        }
    }

    // Handle Attack.
    public void Attack()
    {
        ResolveReferences();
        if (player == null)
        {
            return;
        }

        Vector3 origin = player.transform.position;
        float radius = Mathf.Max(0.01f, attackRadius);
        float radiusSqr = radius * radius;

        _uniqueTargets.Clear();

        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            radius,
            _overlapHits,
            enemyMask,
            triggerInteraction);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _overlapHits[i];
            if (hit == null || hit.transform.IsChildOf(player.transform))
            {
                continue;
            }

            NPCDemageScript damageTarget = hit.GetComponent<NPCDemageScript>();
            if (damageTarget == null)
            {
                damageTarget = hit.GetComponentInParent<NPCDemageScript>();
            }
            if (damageTarget == null)
            {
                damageTarget = hit.GetComponentInChildren<NPCDemageScript>();
            }

            if (damageTarget != null)
            {
                _uniqueTargets.Add(damageTarget);
            }
        }

        // Fallback for enemies without colliders in mask or incomplete setup.
        if (_uniqueTargets.Count == 0)
        {
            CollectTargetsFromEnemyLists(origin, radiusSqr);
        }

        foreach (NPCDemageScript damageTarget in _uniqueTargets)
        {
            if (damageTarget == null)
            {
                continue;
            }

            Vector3 delta = damageTarget.transform.position - origin;
            if (delta.sqrMagnitude > radiusSqr)
            {
                continue;
            }

            damageTarget.TakeDemage(attackDamage);
        }
    }

    // Handle Collect Targets From Enemy Lists.
    private void CollectTargetsFromEnemyLists(Vector3 origin, float radiusSqr)
    {
        if (enemiesHandler != null && enemiesHandler.enemies != null)
        {
            foreach (GameObject enemy in enemiesHandler.enemies)
            {
                if (enemy == null)
                {
                    continue;
                }

                Vector3 delta = enemy.transform.position - origin;
                if (delta.sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                NPCDemageScript fromList = enemy.GetComponent<NPCDemageScript>();
                if (fromList == null)
                {
                    fromList = enemy.GetComponentInChildren<NPCDemageScript>();
                }
                if (fromList == null)
                {
                    fromList = enemy.GetComponentInParent<NPCDemageScript>();
                }

                if (fromList != null)
                {
                    _uniqueTargets.Add(fromList);
                }
            }
        }

        NPCDemageScript[] allDamageTargets = FindObjectsByType<NPCDemageScript>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < allDamageTargets.Length; i++)
        {
            NPCDemageScript damageTarget = allDamageTargets[i];
            if (damageTarget == null)
            {
                continue;
            }

            Vector3 delta = damageTarget.transform.position - origin;
            if (delta.sqrMagnitude <= radiusSqr)
            {
                _uniqueTargets.Add(damageTarget);
            }
        }
    }
}
