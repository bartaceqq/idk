using UnityEngine;

// Controls Action Script behavior.
public class ActionScript : MonoBehaviour
{
    public bool enoughstamina;
    public StaminaScript staminaScript; 
    public string currentutil;
    [Header("Animation Locks")]
    public float swordMovementAnimationLockSeconds = 0.35f;
    public MovementAnimationScript movementAnimationScript;
    public AxeAnimationScript axeAnimationScript;
    public PickaxeAnimationScript pickaxeAnimationScript;
    public SwordAnimationScript swordAnimationScript;

    private float movementAnimationLockUntil;
   
    
    // Handle Chop.
    public void Chop()
    {
        axeAnimationScript.ChopAnimation();
    }
    // Handle Walk.
    public void Walk(bool status)
    {
        
        
         movementAnimationScript.WalkAnimation_Foreward(status);
        
    }
    // Handle Sprint.
    public void Sprint(bool status, bool playAnimation)
    {
        if (movementAnimationScript != null)
        {
            movementAnimationScript.RunAnimation_Foreward(status && playAnimation);
        }

        if (staminaScript == null)
        {
            return;
        }

        if (status)
        {
            staminaScript.ReduceStamina();
        }
        else
        {
            staminaScript.AddStamina();
        }
    }
    // Handle Idle.
    public void Idle(bool status)
    {
        movementAnimationScript.IdleAnimation(status);
    }
    // Handle Mine.
    public void Mine()
    {
        pickaxeAnimationScript.Mine();
    }
    // Handle Attack.
    public void Attack()
    {
        LockMovementAnimations(swordMovementAnimationLockSeconds);
        swordAnimationScript.Attack();
    }
    public void Jump()
    {
        movementAnimationScript.JumpAnimation();
    }
    public void WalkBackwards(bool status)
    {
        movementAnimationScript.WalkBackWards(status);
    }

    // Handle Is Movement Animation Locked.
    public bool IsMovementAnimationLocked()
    {
        return Time.time < movementAnimationLockUntil;
    }

    // Handle Lock Movement Animations.
    public void LockMovementAnimations(float seconds)
    {
        float lockDuration = Mathf.Max(0f, seconds);
        if (lockDuration <= 0f)
        {
            return;
        }

        float lockUntil = Time.time + lockDuration;
        if (lockUntil > movementAnimationLockUntil)
        {
            movementAnimationLockUntil = lockUntil;
        }
    }

}

