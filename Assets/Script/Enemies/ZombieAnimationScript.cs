using UnityEngine;

// Controls Zombie Animation Script behavior.
public class ZombieAnimationScript : MonoBehaviour
{
    public Animator animator;
    // Handle Throw Anim.
    public void ThrowAnim()
    {
        animator.SetTrigger("Throw");
    }
    // Handle Move Anim.
    public void MoveAnim(bool status)
    {
        animator.SetBool("Walking", status);
    }
}

