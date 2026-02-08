using UnityEngine;

public class MovementAnimationScript : MonoBehaviour
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
    public void WalkAnimation_Foreward(bool status)

    {
        if(status){
            animator.SetBool("Foreward", true);   
        }
        else
        {
            animator.SetBool("Foreward", false);   
        }
    }
    public void IdleAnimation(bool status)
    {
        if (status)
        {
            animator.SetBool("Idle", true);  
        }else
        {
           animator.SetBool("Idle", false);    
        }
    }
}
