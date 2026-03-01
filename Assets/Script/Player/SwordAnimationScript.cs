using UnityEngine;

// Controls Sword Animation Script behavior.
public class SwordAnimationScript : MonoBehaviour
{
    public Animator animator;
    // Handle Attack.
    public void Attack()
    {
        animator.SetTrigger("Attack");
    }
}

