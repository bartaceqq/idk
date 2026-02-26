using UnityEngine;

public class ProjectileScript : MonoBehaviour
{
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnCollisionEnter(Collider other)
    {
        Debug.Log(other.tag);
    }
}
