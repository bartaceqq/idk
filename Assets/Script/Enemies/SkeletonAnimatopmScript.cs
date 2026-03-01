using UnityEngine;

// Controls Skeleton Animatopm Script behavior.
public class SkeletonAnimatopmScript : MonoBehaviour
{
    public Animator animator;
    // Handle Throw Anim.
    public void ThrowAnim()
    {
        animator.SetTrigger("Attack");
    }
    // Handle Move Anim.
    public void MoveAnim(bool status)
    {
        animator.SetBool("Move", status);
    }
}

