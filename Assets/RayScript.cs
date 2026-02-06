using UnityEngine;

public class RayScript : MonoBehaviour
{
    public Camera camera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RayCheck();
        }
    }
    public void RayCheck()
    {
        if (camera == null)
        {
            camera = Camera.main;
            if (camera == null) return;
        }

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f))
        {
            Debug.Log(hit.collider.gameObject.name);
            if (hit.collider.CompareTag("Tree") || hit.collider.transform.root.CompareTag("Tree"))
            {
                ColliderScript colliderScript = hit.collider.gameObject.GetComponent<ColliderScript>();
                if (colliderScript == null)
                {
                    colliderScript = hit.collider.GetComponentInParent<ColliderScript>();
                }
                if (colliderScript != null)
                {
                    colliderScript.Trigger();
                }
            }
            
        }
    }
}
