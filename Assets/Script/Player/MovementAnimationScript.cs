using UnityEngine;

// Controls Movement Animation Script behavior.
public class MovementAnimationScript : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public bool autoFindAnimatorInChildren = true;

    [Header("Jump")]
    public float jumpInterruptBlendSeconds = 0.04f;
    public float jumpInterruptIgnoreActionSeconds = 0.12f;

    [Header("Animation Speeds")]
    public float idleAnimationSpeed = 1f;
    public float walkAnimationSpeed = 1f;
    public float runAnimationSpeed = 1f;
    public float walkBackwardAnimationSpeed = 1f;
    public float leftStrafeAnimationSpeed = 1f;
    public float rightStrafeAnimationSpeed = 1f;
    public float jumpAnimationSpeed = 1f;
    public float runningJumpAnimationSpeed = 1f;

    private static readonly int AttackWeaponStateHash = Animator.StringToHash("AttackWeapon");
    private static readonly int AttackTwoHandedStateHash = Animator.StringToHash("AttackTwoHanded");
    private static readonly int PunchLeftStateHash = Animator.StringToHash("PunchLeft");
    private static readonly int PunchRightStateHash = Animator.StringToHash("PunchRight");
    private static readonly int MiningStateHash = Animator.StringToHash("Mining");
    private static readonly int ChopStateHash = Animator.StringToHash("Chop");
    private static readonly int JumpStateHash = Animator.StringToHash("Jump");
    private static readonly int RunningJumpStateHash = Animator.StringToHash("RunningJump");
    private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");
    private static readonly int IdleStateHash = Animator.StringToHash("Idle");
    private static readonly int IdleFullPathHash = Animator.StringToHash("Base Layer.Idle");

    private static readonly int ForewardBoolHash = Animator.StringToHash("Foreward");
    private static readonly int IdleBoolHash = Animator.StringToHash("Idle");
    private static readonly int SprintingBoolHash = Animator.StringToHash("Sprinting");
    private static readonly int WalkingBackWardsBoolHash = Animator.StringToHash("WalkingBackWards");
    private static readonly int WalkingLeftBoolHash = Animator.StringToHash("WalkingLeft");
    private static readonly int WalkingRightBoolHash = Animator.StringToHash("WalkingRight");
    private static readonly int WalkingForwardLeftBoolHash = Animator.StringToHash("WalkingForwardLeft");
    private static readonly int WalkingForwardRightBoolHash = Animator.StringToHash("WalkingForwardRight");
    private static readonly int SprintingForwardLeftBoolHash = Animator.StringToHash("SprintingForwardLeft");
    private static readonly int SprintingForwardRightBoolHash = Animator.StringToHash("SprintingForwardRight");

    private static readonly int IdleSpeedHash = Animator.StringToHash("IdleSpeed");
    private static readonly int WalkSpeedHash = Animator.StringToHash("WalkSpeed");
    private static readonly int RunSpeedHash = Animator.StringToHash("RunSpeed");
    private static readonly int WalkBackwardSpeedHash = Animator.StringToHash("WalkBackwardSpeed");
    private static readonly int LeftStrafeSpeedHash = Animator.StringToHash("LeftStrafeSpeed");
    private static readonly int RightStrafeSpeedHash = Animator.StringToHash("RightStrafeSpeed");
    private static readonly int JumpSpeedHash = Animator.StringToHash("JumpSpeed");
    private static readonly int RunningJumpSpeedHash = Animator.StringToHash("RunningJumpSpeed");

    private float _jumpInterruptedUntil;

    private void Awake()
    {
        EnsureAnimator();
        ApplyConfiguredAnimationSpeeds();
    }

    private void OnEnable()
    {
        EnsureAnimator();
        ApplyConfiguredAnimationSpeeds();
    }

    private void OnValidate()
    {
        EnsureAnimator();
        ApplyConfiguredAnimationSpeeds();
    }

    // Handle Walk Animation Foreward.
    public void WalkAnimation_Foreward(bool status)
    {
        SetMovementBool(ForewardBoolHash, status, WalkSpeedHash, walkAnimationSpeed);
    }

    // Handle Run Animation Foreward.
    public void RunAnimation_Foreward(bool status)
    {
        SetMovementBool(SprintingBoolHash, status, RunSpeedHash, runAnimationSpeed);
    }

    // Handle Idle Animation.
    public void IdleAnimation(bool status)
    {
        if (!EnsureAnimator())
        {
            return;
        }

        ApplyConfiguredAnimationSpeeds();
        animator.SetBool(IdleBoolHash, status);
    }

    // Handle Jump Animation.
    public void JumpAnimation()
    {
        if (!EnsureAnimator())
        {
            return;
        }

        ApplyConfiguredAnimationSpeeds();
        animator.ResetTrigger(JumpTriggerHash);
        animator.SetTrigger(JumpTriggerHash);
    }

    // Handle Walk Backwards.
    public void WalkBackWards(bool status)
    {
        SetMovementBool(WalkingBackWardsBoolHash, status, WalkBackwardSpeedHash, walkBackwardAnimationSpeed);
    }

    // Handle Walk Left.
    public void WalkLeft(bool status)
    {
        SetMovementBool(WalkingLeftBoolHash, status, LeftStrafeSpeedHash, leftStrafeAnimationSpeed);
    }

    // Handle Walk Right.
    public void WalkRight(bool status)
    {
        SetMovementBool(WalkingRightBoolHash, status, RightStrafeSpeedHash, rightStrafeAnimationSpeed);
    }

    // Handle Walk Forward Left.
    public void WalkForwardLeft(bool status)
    {
        SetMovementBool(WalkingForwardLeftBoolHash, status, LeftStrafeSpeedHash, leftStrafeAnimationSpeed);
    }

    // Handle Walk Forward Right.
    public void WalkForwardRight(bool status)
    {
        SetMovementBool(WalkingForwardRightBoolHash, status, RightStrafeSpeedHash, rightStrafeAnimationSpeed);
    }

    // Handle Sprint Forward Left.
    public void SprintForwardLeft(bool status)
    {
        SetMovementBool(SprintingForwardLeftBoolHash, status, RunSpeedHash, runAnimationSpeed);
    }

    // Handle Sprint Forward Right.
    public void SprintForwardRight(bool status)
    {
        SetMovementBool(SprintingForwardRightBoolHash, status, RunSpeedHash, runAnimationSpeed);
    }

    // Handle Is Blocking Action State.
    public bool IsBlockingActionState()
    {
        if (!EnsureAnimator() || !animator.isActiveAndEnabled)
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
        if (!EnsureAnimator() || !animator.isActiveAndEnabled || !IsAnimatorInJumpState())
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

    // Handle Set Movement Bool.
    private void SetMovementBool(int parameterHash, bool value, int speedHash, float speedValue)
    {
        if (!EnsureAnimator())
        {
            return;
        }

        ApplyConfiguredAnimationSpeeds();
        if (value)
        {
            SetFloatIfExists(speedHash, speedValue);
        }

        if (value && IsBlockingActionState())
        {
            return;
        }

        animator.SetBool(parameterHash, value);
    }

    // Handle Ensure Animator.
    private bool EnsureAnimator()
    {
        if (animator != null)
        {
            animator.applyRootMotion = false;
            return true;
        }

        if (autoFindAnimatorInChildren)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        return animator != null;
    }

    // Handle Apply Configured Animation Speeds.
    private void ApplyConfiguredAnimationSpeeds()
    {
        if (animator == null)
        {
            return;
        }

        SetFloatIfExists(IdleSpeedHash, ResolveConfiguredSpeed(idleAnimationSpeed));
        SetFloatIfExists(WalkSpeedHash, ResolveConfiguredSpeed(walkAnimationSpeed));
        SetFloatIfExists(RunSpeedHash, ResolveConfiguredSpeed(runAnimationSpeed));
        SetFloatIfExists(WalkBackwardSpeedHash, ResolveConfiguredSpeed(walkBackwardAnimationSpeed));
        SetFloatIfExists(LeftStrafeSpeedHash, ResolveConfiguredSpeed(leftStrafeAnimationSpeed));
        SetFloatIfExists(RightStrafeSpeedHash, ResolveConfiguredSpeed(rightStrafeAnimationSpeed));
        SetFloatIfExists(JumpSpeedHash, ResolveConfiguredSpeed(jumpAnimationSpeed));
        SetFloatIfExists(RunningJumpSpeedHash, ResolveConfiguredSpeed(runningJumpAnimationSpeed));
    }

    // Handle Set Float If Exists.
    private void SetFloatIfExists(int parameterHash, float value)
    {
        if (animator == null)
        {
            return;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.type == AnimatorControllerParameterType.Float &&
                parameter.nameHash == parameterHash)
            {
                animator.SetFloat(parameterHash, Mathf.Max(0f, value));
                return;
            }
        }
    }

    private static float ResolveConfiguredSpeed(float value)
    {
        return value > 0f ? value : 1f;
    }

    // Handle Is Blocking Action State For State.
    private bool IsBlockingActionState(AnimatorStateInfo state)
    {
        if (!IsActionState(state))
        {
            return false;
        }

        if ((state.shortNameHash == JumpStateHash || state.shortNameHash == RunningJumpStateHash) &&
            Time.time < _jumpInterruptedUntil)
        {
            return false;
        }

        return true;
    }

    // Handle Is Animator In Jump State.
    private bool IsAnimatorInJumpState()
    {
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.shortNameHash == JumpStateHash || current.shortNameHash == RunningJumpStateHash)
        {
            return true;
        }

        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            return next.shortNameHash == JumpStateHash || next.shortNameHash == RunningJumpStateHash;
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
               stateHash == JumpStateHash ||
               stateHash == RunningJumpStateHash;
    }
}
