using UnityEngine;
public class ProjectileScript : MonoBehaviour
{
    [SerializeField] private bool logCollisions;
    public void OnCollisionEnter(Collision other) { if (logCollisions && other != null && other.gameObject != null) { Debug.Log(other.gameObject.tag); } }
}
