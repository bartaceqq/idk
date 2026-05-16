using UnityEngine;

// Stores sword-specific combat stats and resolves sword components from equipped items.
public class Sword : MonoBehaviour
{
    [Header("Sword Stats")]
    [Min(0f)] public float damage = 40f;
    [Min(0.01f)] public float speed = 1f;
    [Min(0.01f)] public float animationSpeed = 1f;

    private void OnValidate()
    {
        damage = Mathf.Max(0f, damage);
        speed = Mathf.Max(0.01f, speed);
        animationSpeed = Mathf.Max(0.01f, animationSpeed);
    }

    public float GetResolvedDamage()
    {
        return Mathf.Max(0f, damage);
    }

    public float GetResolvedSpeed()
    {
        return speed > 0f ? speed : 1f;
    }

    public float GetResolvedAnimationSpeed()
    {
        return animationSpeed > 0f ? animationSpeed : 1f;
    }

    public static bool TryResolve(Item item, out Sword sword)
    {
        sword = null;
        if (item == null)
        {
            return false;
        }

        sword = item.GetComponent<Sword>();
        if (sword != null)
        {
            return true;
        }

        if (item.itemobject == null)
        {
            return false;
        }

        sword = item.itemobject.GetComponent<Sword>();
        return sword != null;
    }
}
