using UnityEngine;

// Controls Action Script behavior.
public class ActionScript : MonoBehaviour
{
    public bool enoughstamina;
    public StaminaScript staminaScript; 
    public string currentutil;
    [Header("Animation Locks")]
    public float swordMovementAnimationLockSeconds = 0.9f;
    public float swordHeavyMovementAnimationLockSeconds = 1.35f;
    public float unarmedPunchMovementAnimationLockSeconds = 0.6f;
    public MovementAnimationScript movementAnimationScript;
    public AxeAnimationScript axeAnimationScript;
    public PickaxeAnimationScript pickaxeAnimationScript;
    public SwordAnimationScript swordAnimationScript;

    private float movementAnimationLockUntil;
    private int unarmedPunchStep;

    private static readonly int AttackWeaponStateHash = Animator.StringToHash("AttackWeapon");
    private static readonly int AttackTwoHandedStateHash = Animator.StringToHash("AttackTwoHanded");
    private static readonly int PunchLeftStateHash = Animator.StringToHash("PunchLeft");
    private static readonly int PunchRightStateHash = Animator.StringToHash("PunchRight");
    private static readonly int MiningStateHash = Animator.StringToHash("Mining");
    private static readonly int ChopStateHash = Animator.StringToHash("Chop");
    private static readonly int JumpStateHash = Animator.StringToHash("Jump");
   
    
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
        AttackLight();
    }

    // Handle Attack Light.
    public void AttackLight()
    {
        LockMovementAnimations(swordMovementAnimationLockSeconds);
        if (swordAnimationScript != null)
        {
            swordAnimationScript.AttackLight();
        }
    }

    // Handle Attack Heavy.
    public void AttackHeavy()
    {
        LockMovementAnimations(swordHeavyMovementAnimationLockSeconds);
        if (swordAnimationScript != null)
        {
            swordAnimationScript.AttackHeavy();
        }
    }

    // Handle Unarmed Punch Combo.
    public void UnarmedPunchCombo()
    {
        bool punchLeft = (unarmedPunchStep % 2) == 0;
        LockMovementAnimations(unarmedPunchMovementAnimationLockSeconds);

        if (swordAnimationScript != null)
        {
            if (punchLeft)
            {
                swordAnimationScript.PunchLeft();
            }
            else
            {
                swordAnimationScript.PunchRight();
            }
        }

        unarmedPunchStep = (unarmedPunchStep + 1) % 4;
    }

    // Handle Reset Unarmed Punch Combo.
    public void ResetUnarmedPunchCombo()
    {
        unarmedPunchStep = 0;
    }
    public void Jump()
    {
        movementAnimationScript.JumpAnimation();
    }
    public void WalkBackwards(bool status)
    {
        movementAnimationScript.WalkBackWards(status);
    }

    // Handle Walk Left.
    public void WalkLeft(bool status)
    {
        if (movementAnimationScript == null)
        {
            return;
        }

        movementAnimationScript.WalkLeft(status);
    }

    // Handle Walk Right.
    public void WalkRight(bool status)
    {
        if (movementAnimationScript == null)
        {
            return;
        }

        movementAnimationScript.WalkRight(status);
    }

    // Handle Walk Forward Left.
    public void WalkForwardLeft(bool status)
    {
        if (movementAnimationScript == null)
        {
            return;
        }

        movementAnimationScript.WalkForwardLeft(status);
    }

    // Handle Walk Forward Right.
    public void WalkForwardRight(bool status)
    {
        if (movementAnimationScript == null)
        {
            return;
        }

        movementAnimationScript.WalkForwardRight(status);
    }

    // Handle Sprint Forward Left.
    public void SprintForwardLeft(bool status)
    {
        if (movementAnimationScript == null)
        {
            return;
        }

        movementAnimationScript.SprintForwardLeft(status);
    }

    // Handle Sprint Forward Right.
    public void SprintForwardRight(bool status)
    {
        if (movementAnimationScript == null)
        {
            return;
        }

        movementAnimationScript.SprintForwardRight(status);
    }

    // Handle Is Movement Animation Locked.
    public bool IsMovementAnimationLocked()
    {
        return Time.time < movementAnimationLockUntil || IsAnimatorInActionState();
    }

    // Handle Lock Movement Animations.
    public void LockMovementAnimations(float seconds)
    {
        float lockDuration = Mathf.Max(0f, seconds);
        if (lockDuration <= 0f)
        {
            return;
        }

        // Clear movement bools immediately so attack clips cannot be interrupted by movement this frame.
        ForceStopMovementAnimations();

        float lockUntil = Time.time + lockDuration;
        if (lockUntil > movementAnimationLockUntil)
        {
            movementAnimationLockUntil = lockUntil;
        }
    }

    // Handle Force Stop Movement Animations.
    private void ForceStopMovementAnimations()
    {
        if (movementAnimationScript == null)
        {
            return;
        }

        movementAnimationScript.IdleAnimation(false);
        movementAnimationScript.WalkAnimation_Foreward(false);
        movementAnimationScript.RunAnimation_Foreward(false);
        movementAnimationScript.WalkBackWards(false);
        movementAnimationScript.WalkLeft(false);
        movementAnimationScript.WalkRight(false);
        movementAnimationScript.WalkForwardLeft(false);
        movementAnimationScript.WalkForwardRight(false);
        movementAnimationScript.SprintForwardLeft(false);
        movementAnimationScript.SprintForwardRight(false);
    }

    // Handle Is Animator In Action State.
    private bool IsAnimatorInActionState()
    {
        Animator animator = movementAnimationScript != null
            ? movementAnimationScript.animator
            : null;

        if (animator == null && swordAnimationScript != null)
        {
            animator = swordAnimationScript.animator;
        }

        if (animator == null || !animator.isActiveAndEnabled)
        {
            return false;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (IsActionState(current))
        {
            return true;
        }

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            if (IsActionState(next))
            {
                return true;
            }
        }

        return false;
    }

    // Handle Is Action State.
    private static bool IsActionState(AnimatorStateInfo state)
    {
        if (state.IsTag("Action"))
        {
            return true;
        }

        int stateHash = state.shortNameHash;
        return stateHash == AttackWeaponStateHash ||
               stateHash == AttackTwoHandedStateHash ||
               stateHash == PunchLeftStateHash ||
               stateHash == PunchRightStateHash ||
               stateHash == MiningStateHash ||
               stateHash == ChopStateHash ||
               stateHash == JumpStateHash;
    }

}

