using UnityEngine;
public class ProjectileScript : MonoBehaviour {
    public void OnCollisionEnter(Collision other) { Debug.Log(other.gameObject.tag); } }

