using UnityEngine;

// Controls Movement Animation Script behavior.
public class MovementAnimationScript : MonoBehaviour
{
    public Animator animator;

    private static readonly int AttackWeaponStateHash = Animator.StringToHash("AttackWeapon");
    private static readonly int AttackTwoHandedStateHash = Animator.StringToHash("AttackTwoHanded");
    private static readonly int PunchLeftStateHash = Animator.StringToHash("PunchLeft");
    private static readonly int PunchRightStateHash = Animator.StringToHash("PunchRight");
    private static readonly int MiningStateHash = Animator.StringToHash("Mining");
    private static readonly int ChopStateHash = Animator.StringToHash("Chop");
    private static readonly int JumpStateHash = Animator.StringToHash("Jump");
    // Handle Walk Animation Foreward.
    public void WalkAnimation_Foreward(bool status)

    {
        SetMovementBool("Foreward", status);
    }
    // Handle Run Animation Foreward.
    public void RunAnimation_Foreward(bool status)

    {
        SetMovementBool("Sprinting", status);
    }
    // Handle Idle Animation.
    public void IdleAnimation(bool status)
    {
        if (animator == null)
        {
            return;
        }

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
    public void WalkBackWards(bool status)
    {
        SetMovementBool("WalkingBackWards", status);
    }

    // Handle Walk Left.
    public void WalkLeft(bool status)
    {
        SetMovementBool("WalkingLeft", status);
    }

    // Handle Walk Right.
    public void WalkRight(bool status)
    {
        SetMovementBool("WalkingRight", status);
    }

    // Handle Walk Forward Left.
    public void WalkForwardLeft(bool status)
    {
        SetMovementBool("WalkingForwardLeft", status);
    }

    // Handle Walk Forward Right.
    public void WalkForwardRight(bool status)
    {
        SetMovementBool("WalkingForwardRight", status);
    }

    // Handle Sprint Forward Left.
    public void SprintForwardLeft(bool status)
    {
        SetMovementBool("SprintingForwardLeft", status);
    }

    // Handle Sprint Forward Right.
    public void SprintForwardRight(bool status)
    {
        SetMovementBool("SprintingForwardRight", status);
    }

    // Handle Set Movement Bool.
    private void SetMovementBool(string parameterName, bool value)
    {
        if (animator == null)
        {
            return;
        }

        if (value && IsAnimatorInActionState())
        {
            return;
        }

        animator.SetBool(parameterName, value);
    }

    // Handle Is Animator In Action State.
    private bool IsAnimatorInActionState()
    {
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

