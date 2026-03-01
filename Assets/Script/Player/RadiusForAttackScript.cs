using UnityEngine;

// Controls Radius For Attack Script behavior.
public class RadiusForAttackScript : MonoBehaviour
{
    public GameObject player;
    public EnemiesHandler enemiesHandler;
    public float attackRadius = 5f;


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

        float radiusSqr = attackRadius * attackRadius;
        bool usedEnemyList = enemiesHandler != null && enemiesHandler.enemies != null && enemiesHandler.enemies.Count > 0;

        if (usedEnemyList)
        {
            foreach (GameObject enemy in enemiesHandler.enemies)
            {
                if (enemy == null) continue;

                TryApplyDamage(enemy.transform.position, enemy, radiusSqr);
            }
            return;
        }

        NPCDemageScript[] allDamageTargets = FindObjectsByType<NPCDemageScript>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (NPCDemageScript damageTarget in allDamageTargets)
        {
            if (damageTarget == null) continue;
            TryApplyDamage(damageTarget.transform.position, damageTarget.gameObject, radiusSqr);
        }
    }

    // Handle Try Apply Damage.
    private void TryApplyDamage(Vector3 enemyPosition, GameObject enemyObject, float radiusSqr)
    {
        Vector3 delta = enemyPosition - player.transform.position;
        if (delta.sqrMagnitude > radiusSqr)
        {
            return;
        }

        NPCDemageScript nPCDemageScript = enemyObject.GetComponent<NPCDemageScript>();
        if (nPCDemageScript == null)
        {
            nPCDemageScript = enemyObject.GetComponentInChildren<NPCDemageScript>();
        }

        if (nPCDemageScript != null)
        {
            // Use target's configured defaultDamage from NPCDemageScript inspector.
            nPCDemageScript.TakeDemage(40f);
        }
    }
}
