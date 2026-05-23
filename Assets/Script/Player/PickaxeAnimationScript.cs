using UnityEngine;

// Controls Pickaxe Animation Script behavior.
public class PickaxeAnimationScript : MonoBehaviour
{
     public Animator animator;
    // Handle Mine.
    public void Mine()
    {
        
        animator.SetTrigger("Mine");
    }
}

