using UnityEngine;

public class TreeTest : MonoBehaviour
{
    public GameObject treechopped;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Instantiate(treechopped, this.gameObject.transform.position, this.gameObject.transform.rotation);
            Destroy(this);
        }
    }
}
