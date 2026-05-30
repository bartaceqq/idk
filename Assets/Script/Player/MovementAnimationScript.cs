using UnityEngine;
public class MovementAnimationScript : MonoBehaviour
{
    [Header("References")] public Animator animator;
    public bool autoFindAnimatorInChildren = true;
    [Header("Jump")] public float jumpInterruptBlendSeconds = 0.04f;
    public float jumpInterruptIgnoreActionSeconds = 0.12f;
    public bool syncJumpAnimationToAirTime = true;
    public float minimumSyncedJumpAnimationSpeed = 0.05f;
    public float maximumSyncedJumpAnimationSpeed = 6f;
    [Header("Animation Speeds")] public float idleAnimationSpeed = 1f;
    public float walkAnimationSpeed = 1f; public float runAnimationSpeed = 1f;
    public float walkBackwardAnimationSpeed = 1f; public float leftStrafeAnimationSpeed = 1f;
    public float rightStrafeAnimationSpeed = 1f; public float jumpAnimationSpeed = 1f;
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
    private static readonly int RunningJumpFullPathHash = Animator.StringToHash("Base Layer.RunningJump");
    private static readonly int ForewardBoolHash = Animator.StringToHash("Foreward");
    private static readonly int IdleBoolHash = Animator.StringToHash("Idle");
    private static readonly int SprintingBoolHash = Animator.StringToHash("Sprinting");
    private static readonly int WalkingBackWardsBoolHash = Animator.StringToHash("WalkingBackWards");
    private static readonly int WalkingLeftBoolHash = Animator.StringToHash("WalkingLeft");
    private static readonly int WalkingRightBoolHash = Animator.StringToHash("WalkingRight");
    private static readonly int SprintingLeftBoolHash = Animator.StringToHash("SprintingLeft");
    private static readonly int SprintingRightBoolHash = Animator.StringToHash("SprintingRight");
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
    private RuntimeAnimatorController _cachedAnimatorController;
    private AnimationClip _jumpClip; private AnimationClip _runningJumpClip;
    private bool _rootMotionEnabled; private void Awake()
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
    public void WalkAnimation_Foreward(bool status) { SetMovementBool(ForewardBoolHash, status, WalkSpeedHash, walkAnimationSpeed); }
    public void RunAnimation_Foreward(bool status) { SetMovementBool(SprintingBoolHash, status, RunSpeedHash, runAnimationSpeed); }
    public void IdleAnimation(bool status)
    {
        if (!EnsureAnimator()) { return; }
        ApplyConfiguredAnimationSpeeds(); animator.SetBool(IdleBoolHash, status);
    }
    public void JumpAnimation() { JumpAnimation(-1f); }
    public void JumpAnimation(float expectedAirTimeSeconds, bool preferRunningJumpSpeed = false)
    {
        if (!EnsureAnimator()) { return; }
        ApplyConfiguredAnimationSpeeds();
        ApplySyncedJumpAnimationSpeed(preferRunningJumpSpeed, expectedAirTimeSeconds, 0f);
        animator.ResetTrigger(JumpTriggerHash);
        if (preferRunningJumpSpeed && animator.HasState(0, RunningJumpFullPathHash))
        {
            animator.CrossFadeInFixedTime(RunningJumpFullPathHash, 0.02f, 0, 0f); return;
        }
        animator.SetTrigger(JumpTriggerHash);
    }
    public void SyncJumpAnimationToAirTime(float remainingAirTimeSeconds)
    {
        if (!syncJumpAnimationToAirTime || !EnsureAnimator() || !animator.isActiveAndEnabled) { return; }
        if (remainingAirTimeSeconds <= 0f) { return; }
        if (TryGetJumpStateProgress(out bool preferRunningJumpSpeed, out float normalizedProgress)) { ApplySyncedJumpAnimationSpeed(preferRunningJumpSpeed, remainingAirTimeSeconds, normalizedProgress); } else { ApplySyncedJumpAnimationSpeed(false, remainingAirTimeSeconds, 0f); }
    }
    public void WalkBackWards(bool status) { SetMovementBool(WalkingBackWardsBoolHash, status, WalkBackwardSpeedHash, walkBackwardAnimationSpeed); }
    public void WalkLeft(bool status) { SetMovementBool(WalkingLeftBoolHash, status, LeftStrafeSpeedHash, leftStrafeAnimationSpeed); }
    public void WalkRight(bool status) { SetMovementBool(WalkingRightBoolHash, status, RightStrafeSpeedHash, rightStrafeAnimationSpeed); }
    public void SprintLeft(bool status) { SetMovementBool(SprintingLeftBoolHash, status, RunSpeedHash, runAnimationSpeed); }
    public void SprintRight(bool status) { SetMovementBool(SprintingRightBoolHash, status, RunSpeedHash, runAnimationSpeed); }
    public void WalkForwardLeft(bool status) { SetMovementBool(WalkingForwardLeftBoolHash, status, WalkSpeedHash, walkAnimationSpeed); }
    public void WalkForwardRight(bool status) { SetMovementBool(WalkingForwardRightBoolHash, status, WalkSpeedHash, walkAnimationSpeed); }
    public void SprintForwardLeft(bool status) { SetMovementBool(SprintingForwardLeftBoolHash, status, RunSpeedHash, runAnimationSpeed); }
    public void SprintForwardRight(bool status) { SetMovementBool(SprintingForwardRightBoolHash, status, RunSpeedHash, runAnimationSpeed); }
    public void SetLocomotion(bool idle, bool forwardWalk, bool forwardRun, bool backward,
    bool leftStrafe, bool rightStrafe, bool sprintLeft, bool sprintRight,
    bool forwardLeftWalk, bool forwardRightWalk, bool forwardLeftRun, bool forwardRightRun)
    {
        if (!EnsureAnimator()) { return; }
        ApplyConfiguredAnimationSpeeds();
        if (IsBlockingActionState()) { return; }
        if (forwardWalk || forwardLeftWalk || forwardRightWalk) { SetFloatIfExists(WalkSpeedHash, ResolveConfiguredSpeed(walkAnimationSpeed)); }
        if (forwardRun || sprintLeft || sprintRight || forwardLeftRun || forwardRightRun) { SetFloatIfExists(RunSpeedHash, ResolveConfiguredSpeed(runAnimationSpeed)); }
        if (backward) { SetFloatIfExists(WalkBackwardSpeedHash, ResolveConfiguredSpeed(walkBackwardAnimationSpeed)); }
        if (leftStrafe) { SetFloatIfExists(LeftStrafeSpeedHash, ResolveConfiguredSpeed(leftStrafeAnimationSpeed)); }
        if (rightStrafe) { SetFloatIfExists(RightStrafeSpeedHash, ResolveConfiguredSpeed(rightStrafeAnimationSpeed)); }
        animator.SetBool(IdleBoolHash, idle);
        animator.SetBool(ForewardBoolHash, forwardWalk);
        animator.SetBool(SprintingBoolHash, forwardRun);
        animator.SetBool(WalkingBackWardsBoolHash, backward);
        animator.SetBool(WalkingLeftBoolHash, leftStrafe);
        animator.SetBool(WalkingRightBoolHash, rightStrafe);
        animator.SetBool(SprintingLeftBoolHash, sprintLeft);
        animator.SetBool(SprintingRightBoolHash, sprintRight);
        animator.SetBool(WalkingForwardLeftBoolHash, forwardLeftWalk);
        animator.SetBool(WalkingForwardRightBoolHash, forwardRightWalk);
        animator.SetBool(SprintingForwardLeftBoolHash, forwardLeftRun);
        animator.SetBool(SprintingForwardRightBoolHash, forwardRightRun);
    }
    public bool IsBlockingActionState()
    {
        if (!EnsureAnimator() || !animator.isActiveAndEnabled) { return false; }
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (IsBlockingActionState(current)) { return true; }
        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            if (IsBlockingActionState(next)) { return true; }
        }
        return false;
    }
    public void ForceExitJumpAnimation()
    {
        if (!EnsureAnimator() || !animator.isActiveAndEnabled || !IsAnimatorInJumpState()) { return; }
        animator.ResetTrigger(JumpTriggerHash); ResetJumpAnimationSpeedParameters();
        float blendDuration = Mathf.Max(0f, jumpInterruptBlendSeconds);
        if (animator.HasState(0, IdleFullPathHash)) { animator.CrossFadeInFixedTime(IdleFullPathHash, blendDuration, 0); } else if (animator.HasState(0, IdleStateHash)) { animator.CrossFadeInFixedTime(IdleStateHash, blendDuration, 0); }
        _jumpInterruptedUntil = Time.time + Mathf.Max(jumpInterruptIgnoreActionSeconds, blendDuration + 0.02f);
    }
    private void SetMovementBool(int parameterHash, bool value, int speedHash, float speedValue)
    {
        if (!EnsureAnimator()) { return; }
        ApplyConfiguredAnimationSpeeds(); if (value) { SetFloatIfExists(speedHash, ResolveConfiguredSpeed(speedValue)); }
        if (value && IsBlockingActionState()) { return; }
        animator.SetBool(parameterHash, value);
    }
    private bool EnsureAnimator()
    {
        if (animator != null) { ApplyAnimatorRootMotionSetting(); return true; }
        if (autoFindAnimatorInChildren) { animator = GetComponentInChildren<Animator>(true); }
        if (animator == null) { animator = GetComponent<Animator>(); }
        if (animator == null) { animator = GetComponentInChildren<Animator>(true); }
        ApplyAnimatorRootMotionSetting(); return animator != null;
    }
    public void SetAnimatorRootMotionEnabled(bool enabled)
    {
        _rootMotionEnabled = enabled;
        ApplyAnimatorRootMotionSetting();
    }
    private void ApplyConfiguredAnimationSpeeds()
    {
        if (animator == null) { return; }
        SetFloatIfExists(IdleSpeedHash, ResolveConfiguredSpeed(idleAnimationSpeed));
        SetFloatIfExists(WalkSpeedHash, ResolveConfiguredSpeed(walkAnimationSpeed));
        SetFloatIfExists(RunSpeedHash, ResolveConfiguredSpeed(runAnimationSpeed));
        SetFloatIfExists(WalkBackwardSpeedHash, ResolveConfiguredSpeed(walkBackwardAnimationSpeed));
        SetFloatIfExists(LeftStrafeSpeedHash, ResolveConfiguredSpeed(leftStrafeAnimationSpeed));
        SetFloatIfExists(RightStrafeSpeedHash, ResolveConfiguredSpeed(rightStrafeAnimationSpeed));
        SetFloatIfExists(JumpSpeedHash, ResolveConfiguredSpeed(jumpAnimationSpeed));
        SetFloatIfExists(RunningJumpSpeedHash, ResolveConfiguredSpeed(runningJumpAnimationSpeed));
    }
    private void SetFloatIfExists(int parameterHash, float value)
    {
        if (animator == null) { return; }
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.type == AnimatorControllerParameterType.Float &&
            parameter.nameHash == parameterHash)
            {
                animator.SetFloat(parameterHash, Mathf.Max(0.01f, value)); return;
            }
        }
    }
    private static float ResolveConfiguredSpeed(float value) { return value > 0f ? value : 1f; }
    private void ApplyAnimatorRootMotionSetting()
    {
        if (animator == null) { return; }
        animator.applyRootMotion = _rootMotionEnabled; if (!Application.isPlaying) { return; }
        if (animator.gameObject.GetComponent<PlayerRootMotionDriver>() == null) { animator.gameObject.AddComponent<PlayerRootMotionDriver>(); }
    }
    private void ApplySyncedJumpAnimationSpeed(bool preferRunningJumpSpeed, float remainingAirTimeSeconds, float normalizedProgress)
    {
        CacheJumpClipsIfNeeded(); int speedHash = ResolveJumpSpeedHash(preferRunningJumpSpeed);
        float configuredSpeed = ResolveConfiguredSpeed(speedHash == RunningJumpSpeedHash
        ? runningJumpAnimationSpeed : jumpAnimationSpeed);
        if (!syncJumpAnimationToAirTime || remainingAirTimeSeconds <= 0f)
        {
            SetFloatIfExists(speedHash, configuredSpeed); return;
        }
        float clipLength = GetJumpClipLength(preferRunningJumpSpeed); if (clipLength <= 0f)
        {
            SetFloatIfExists(speedHash, configuredSpeed); return;
        }
        float clampedProgress = Mathf.Clamp01(normalizedProgress);
        float remainingClipSeconds = clipLength * Mathf.Max(0.01f, 1f - clampedProgress);
        float targetAirTimeSeconds = Mathf.Max(0.01f, remainingAirTimeSeconds);
        float syncedSpeed = configuredSpeed * (remainingClipSeconds / targetAirTimeSeconds);
        syncedSpeed = Mathf.Clamp(syncedSpeed, Mathf.Max(0.01f, minimumSyncedJumpAnimationSpeed),
        Mathf.Max(minimumSyncedJumpAnimationSpeed, maximumSyncedJumpAnimationSpeed));
        SetFloatIfExists(speedHash, syncedSpeed);
    }
    private void ResetJumpAnimationSpeedParameters()
    {
        SetFloatIfExists(JumpSpeedHash, ResolveConfiguredSpeed(jumpAnimationSpeed));
        SetFloatIfExists(RunningJumpSpeedHash, ResolveConfiguredSpeed(runningJumpAnimationSpeed));
    }
    private bool TryGetJumpStateProgress(out bool preferRunningJumpSpeed, out float normalizedProgress)
    {
        preferRunningJumpSpeed = false; normalizedProgress = 0f;
        if (animator == null) { return false; }
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (IsJumpState(current, out preferRunningJumpSpeed))
        {
            normalizedProgress = NormalizeStateProgress(current); return true;
        }
        if (!animator.IsInTransition(0)) { return false; }
        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
        if (!IsJumpState(next, out preferRunningJumpSpeed)) { return false; }
        normalizedProgress = NormalizeStateProgress(next); return true;
    }
    private float GetJumpClipLength(bool preferRunningJumpSpeed)
    {
        if (!EnsureAnimator()) { return 0f; }
        CacheJumpClipsIfNeeded();
        AnimationClip clip = preferRunningJumpSpeed && _runningJumpClip != null ? _runningJumpClip
        : _jumpClip != null ? _jumpClip : _runningJumpClip;
        return clip != null ? clip.length : 0f;
    }
    private int ResolveJumpSpeedHash(bool preferRunningJumpSpeed)
    {
        return preferRunningJumpSpeed && _runningJumpClip != null ? RunningJumpSpeedHash
        : JumpSpeedHash;
    }
    private void CacheJumpClipsIfNeeded()
    {
        if (animator == null)
        {
            _cachedAnimatorController = null; _jumpClip = null; _runningJumpClip = null; return;
        }
        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == _cachedAnimatorController && (_jumpClip != null || _runningJumpClip != null)) { return; }
        _cachedAnimatorController = controller; _jumpClip = null; _runningJumpClip = null;
        if (controller == null) { return; }
        AnimationClip[] clips = controller.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null) { continue; }
            if (_jumpClip == null && clip.name == "Jump") { _jumpClip = clip; }
            if (_runningJumpClip == null && clip.name == "RunningJump") { _runningJumpClip = clip; }
        }
    }
    private static bool IsJumpState(AnimatorStateInfo state, out bool preferRunningJumpSpeed)
    {
        preferRunningJumpSpeed = state.shortNameHash == RunningJumpStateHash;
        return state.shortNameHash == JumpStateHash || preferRunningJumpSpeed;
    }
    private static float NormalizeStateProgress(AnimatorStateInfo state)
    {
        float normalizedTime = state.normalizedTime;
        if (normalizedTime > 1f) { normalizedTime %= 1f; }
        return Mathf.Clamp01(normalizedTime);
    }
    private bool IsBlockingActionState(AnimatorStateInfo state)
    {
        if (!IsActionState(state)) { return false; }
        if ((state.shortNameHash == JumpStateHash || state.shortNameHash == RunningJumpStateHash) &&
        Time.time < _jumpInterruptedUntil) { return false; }
        return true;
    }
    private bool IsAnimatorInJumpState()
    {
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
        if (current.shortNameHash == JumpStateHash || current.shortNameHash == RunningJumpStateHash) { return true; }
        if (animator.IsInTransition(0))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(0);
            return next.shortNameHash == JumpStateHash || next.shortNameHash == RunningJumpStateHash;
        }
        return false;
    }
    private static bool IsActionState(AnimatorStateInfo state)
    {
        if (state.IsTag("Action")) { return true; }
        int stateHash = state.shortNameHash; return stateHash == AttackWeaponStateHash ||
        stateHash == AttackTwoHandedStateHash || stateHash == PunchLeftStateHash ||
        stateHash == PunchRightStateHash || stateHash == MiningStateHash ||
        stateHash == ChopStateHash || stateHash == JumpStateHash ||
        stateHash == RunningJumpStateHash;
    }
}
