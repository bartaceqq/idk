using UnityEngine;

// Controls Movement Animation Script behavior.
public class MovementAnimationScript : MonoBehaviour
{
    public Animator animator;
    public float jumpInterruptBlendSeconds = 0.04f;
    public float jumpInterruptIgnoreActionSeconds = 0.12f;

    private static readonly int AttackWeaponStateHash = Animator.StringToHash("AttackWeapon");
    private static readonly int AttackTwoHandedStateHash = Animator.StringToHash("AttackTwoHanded");
    private static readonly int PunchLeftStateHash = Animator.StringToHash("PunchLeft");
    private static readonly int PunchRightStateHash = Animator.StringToHash("PunchRight");
    private static readonly int MiningStateHash = Animator.StringToHash("Mining");
    private static readonly int ChopStateHash = Animator.StringToHash("Chop");
    private static readonly int JumpStateHash = Animator.StringToHash("Jump");
    private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");
    private static readonly int IdleStateHash = Animator.StringToHash("Idle");
    private static readonly int IdleFullPathHash = Animator.StringToHash("Base Layer.Idle");

    private float _jumpInterruptedUntil;
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

        if (value && IsBlockingActionState())
        {
            return;
        }

        animator.SetBool(parameterName, value);
    }

    // Handle Is Blocking Action State.
    public bool IsBlockingActionState()
    {
        if (animator == null || !animator.isActiveAndEnabled)
        {
            return false;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (IsBlockingActionState(current))
        {
            return true;
        }

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            if (IsBlockingActionState(next))
            {
                return true;
            }
        }

        return false;
    }

    // Handle Force Exit Jump Animation.
    public void ForceExitJumpAnimation()
    {
        if (animator == null || !animator.isActiveAndEnabled || !IsAnimatorInJumpState())
        {
            return;
        }

        animator.ResetTrigger(JumpTriggerHash);

        float blendDuration = Mathf.Max(0f, jumpInterruptBlendSeconds);
        if (animator.HasState(0, IdleFullPathHash))
        {
            animator.CrossFadeInFixedTime(IdleFullPathHash, blendDuration, 0);
        }
        else if (animator.HasState(0, IdleStateHash))
        {
            animator.CrossFadeInFixedTime(IdleStateHash, blendDuration, 0);
        }

        _jumpInterruptedUntil = Time.time + Mathf.Max(jumpInterruptIgnoreActionSeconds, blendDuration + 0.02f);
    }

    // Handle Is Blocking Action State For State.
    private bool IsBlockingActionState(AnimatorStateInfo state)
    {
        if (!IsActionState(state))
        {
            return false;
        }

        if (state.shortNameHash == JumpStateHash && Time.time < _jumpInterruptedUntil)
        {
            return false;
        }

        return true;
    }

    // Handle Is Animator In Jump State.
    private bool IsAnimatorInJumpState()
    {
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.shortNameHash == JumpStateHash)
        {
            return true;
        }

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            return next.shortNameHash == JumpStateHash;
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

