using UnityEngine;

public class ZombieAnimationScript : MonoBehaviour
{
    public Animator animator;
    public void ThrowAnim()
    {
        animator.SetTrigger("Throw");
    }
    public void MoveAnim(bool status)
    {
        animator.SetBool("Walking", status);
    }
}

