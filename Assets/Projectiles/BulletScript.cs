using UnityEngine;

public class BulletScript : MonoBehaviour
{
    [SerializeField, Min(0f)] private float damage = 25f;
    [SerializeField] private bool destroyOnImpact = true;

    private bool _hasImpacted;

    private void OnCollisionEnter(Collision collision)
    {
        HandleImpact(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleImpact(other);
    }

    private void HandleImpact(Collider hitCollider)
    {
        if (_hasImpacted || hitCollider == null)
        {
            return;
        }

        _hasImpacted = true;
        ApplyDamage(hitCollider);

        if (destroyOnImpact)
        {
            Destroy(gameObject);
        }
    }

    private void ApplyDamage(Collider hitCollider)
    {
        NPCDemageScript enemyDamageScript = FindTarget<NPCDemageScript>(hitCollider);
        if (enemyDamageScript != null)
        {
            enemyDamageScript.TakeDemage(damage);
            return;
        }

        NPCHealthScript enemyHealthScript = FindTarget<NPCHealthScript>(hitCollider);
        if (enemyHealthScript != null)
        {
            enemyHealthScript.TakeDemage(damage);
            return;
        }

        Animalec animal = FindTarget<Animalec>(hitCollider);
        if (animal != null)
        {
            animal.TakeDamage(damage);
        }
    }

    private static T FindTarget<T>(Component source) where T : Component
    {
        if (source == null)
        {
            return null;
        }

        T target = source.GetComponent<T>();
        if (target != null)
        {
            return target;
        }

        target = source.GetComponentInParent<T>();
        if (target != null)
        {
            return target;
        }

        return source.GetComponentInChildren<T>();
    }
}
