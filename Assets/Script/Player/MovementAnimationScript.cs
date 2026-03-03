using UnityEngine;

// Controls Movement Animation Script behavior.
public class MovementAnimationScript : MonoBehaviour
{
    public Animator animator;
    // Handle Walk Animation Foreward.
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
    // Handle Run Animation Foreward.
    public void RunAnimation_Foreward(bool status)

    {
        if(status){
            animator.SetBool("Sprinting", true);   
        }
        else
        {
            animator.SetBool("Sprinting", false);   
        }
    }
    // Handle Idle Animation.
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
    public void JumpAnimation()
    {
        animator.SetTrigger("Jump");
    }
  
}

