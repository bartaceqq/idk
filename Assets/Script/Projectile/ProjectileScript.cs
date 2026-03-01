using UnityEngine;

// Controls Projectile Script behavior.
public class ProjectileScript : MonoBehaviour
{
    // Handle On Collision Enter.
    public void OnCollisionEnter(Collider other)
    {
        Debug.Log(other.tag);
    }
}

