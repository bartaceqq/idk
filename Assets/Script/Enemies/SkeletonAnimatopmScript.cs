using UnityEngine;

public class SkeletonAnimatopmScript : MonoBehaviour
{
    public Animator animator;
    public void ThrowAnim()
    {
        animator.SetTrigger("Attack");
    }
    public void MoveAnim(bool status)
    {
        animator.SetBool("Move", status);
    }
}

