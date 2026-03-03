using UnityEngine;

// Controls Projectile Script behavior.
public class ProjectileScript : MonoBehaviour
{
    // Handle On Collision Enter.
    public void OnCollisionEnter(Collision other)
    {
        Debug.Log(other.gameObject.tag);
    }
}

