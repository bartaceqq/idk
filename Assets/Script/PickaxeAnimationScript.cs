using UnityEngine;

public class PickaxeAnimationScript : MonoBehaviour
{
     public Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Mine()
    {
        
        animator.SetTrigger("Mine");
    }
}
