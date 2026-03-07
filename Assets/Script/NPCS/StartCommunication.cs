using UnityEngine;

public class StartCommunication : MonoBehaviour
{
    public GameObject player;
    public GameObject tree;
    public float Range = 10f;
    public KeyCode key;
    public Animator animator;
    public VisualCommunication visualCommunication;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(key) && GetDistance()<= Range)
        {
            animator.SetBool("Talking", true);
            this.transform.LookAt(player.transform);
            Vector3 rot = transform.eulerAngles;
            rot.x = 0f; // your X value
            transform.eulerAngles = rot;
            visualCommunication.StartAddingWords();

        }
        if(GetDistance() > Range)
        {
           animator.SetBool("Talking", false); 
           this.transform.LookAt(tree.transform);
        }
    }
    public float GetDistance()
    {
         float dist = Vector3.Distance(player.transform.position, this.gameObject.transform.position);
         return dist;
    }
}
