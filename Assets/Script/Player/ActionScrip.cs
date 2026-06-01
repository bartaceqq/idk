using UnityEngine;
public class ActionScript : MonoBehaviour
{
    public bool enoughstamina;
    public StaminaScript staminaScript; public string currentutil;
    [Header("Animation Locks")] public float swordMovementAnimationLockSeconds = 0.9f;
    public float swordHeavyMovementAnimationLockSeconds = 1.35f;
    public float unarmedPunchMovementAnimationLockSeconds = 0.6f;
    public float mineUpperBodySeconds = 0.9f; public float chopUpperBodySeconds = 0.9f;
    public float chopRepeatDelaySeconds = 0.35f;
    public string upperBodyChopSpeedParameterName = "UpperChopSpeed";
    public string upperBodyLayerName = "UpperBody";
    public string upperBodyIdleStateName = "UpperBodyIdle";
    public float upperBodyLayerBlendSpeed = 18f;
    public float upperBodyMinimumActiveSeconds = 0.25f;
    public float upperBodyStateBlendTime = 0.02f;
    public float upperBodyActionCompletionThreshold = 0.98f;
    public string upperBodyLightAttackStateName = "UpperAttackWeapon";
    public string upperBodyHeavyAttackStateName = "UpperAttackTwoHanded";
    public string upperBodyPunchLeftStateName = "UpperPunchLeft";
    public string upperBodyPunchRightStateName = "UpperPunchRight";
    public string upperBodyMiningStateName = "UpperMining";
    public string upperBodyChopStateName = "UpperChop";
    public string upperBodyChopSecondStateName = "UpperChopSecond";
    [Header("Action Animation Speeds")] public float lightAttackAnimationSpeed = 1f;
    public float heavyAttackAnimationSpeed = 1f; public float punchLeftAnimationSpeed = 1f;
    public float punchRightAnimationSpeed = 1f; public float mineAnimationSpeed = 1f;
    public float gunAimAnimationSpeed = 1f; public float gunShootAnimationSpeed = 1f;
    public float gunReloadAnimationSpeed = 1f; [Header("Axe Combo")]
    [Range(0f, 0.99f)] public float axeComboHoldNormalizedTime = 0.79f;
    [Header("Full Body Sword Actions")] public string baseLayerName = "Base Layer";
    public float fullBodyActionBlendTime = 0.03f;
    [Range(0.5f, 0.999f)] public float fullBodyActionCompletionThreshold = 0.98f;
    public string swordEquipStateName = "PullOutSword";
    public string swordUnequipStateName = "HideSword";
    public string swordBlockEnterStateName = "BlokEnter";
    public string swordBlockLoopStateName = "BlockLoop";
    public string swordBlockExitStateName = "BlockExit";
    public string[] lightAttackStateNames = { "SwordAttack", "SwordAttack2" };
    public string[] heavyAttackStateNames = { "SpecialAttack1", "SpecialAttack2", "SpecialAttack3" };
    [Header("Emote")] public string emoteStateName = "Emote";
    public float emoteBlendTime = 0.05f; public float emoteCancelBlendTime = 0f;
    [Header("Emote Audio")] public AudioClip emoteLoopClip;
    public AudioSource emoteLoopAudioSource;
    [Range(0f, 1f)] public float emoteLoopVolume = 1f;
    public MovementAnimationScript movementAnimationScript;
    public AxeAnimationScript axeAnimationScript;
    public PickaxeAnimationScript pickaxeAnimationScript;
    public SwordAnimationScript swordAnimationScript;
    private float movementAnimationLockUntil; private float upperBodyLayerActiveUntil;
    private float _nextChopAllowedTime; private int unarmedPunchStep;
    private bool upperBodyExternalHold; private bool queuedAxeChop;
    private int _nextLightAttackIndex; private int _nextHeavyAttackIndex;
    private PlayerRootMotionDriver _rootMotionDriver;
    private ItemSwitchScript _itemSwitchScript;
    private static readonly int AttackWeaponStateHash = Animator.StringToHash("AttackWeapon");
    private static readonly int AttackTwoHandedStateHash = Animator.StringToHash("AttackTwoHanded");
    private static readonly int PunchLeftStateHash = Animator.StringToHash("PunchLeft");
    private static readonly int PunchRightStateHash = Animator.StringToHash("PunchRight");
    private static readonly int MiningStateHash = Animator.StringToHash("Mining");
    private static readonly int ChopStateHash = Animator.StringToHash("Chop");
    private static readonly int JumpStateHash = Animator.StringToHash("Jump");
    private const string AttackLightSpeedParameterName = "AttackLightSpeed";
    private const string AttackHeavySpeedParameterName = "AttackHeavySpeed";
    private const string PunchLeftSpeedParameterName = "PunchLeftSpeed";
    private const string PunchRightSpeedParameterName = "PunchRightSpeed";
    private const string MineSpeedParameterName = "MineSpeed";
    private const string GunAimSpeedParameterName = "GunAimSpeed";
    private const string GunShootSpeedParameterName = "GunShootSpeed";
    private const string GunReloadSpeedParameterName = "GunReloadSpeed";
    private const string SwordBlockingParameterName = "SwordBlocking"; private void Awake()
    {
        EnsurePlayerRootMotionDriver(); ApplyConfiguredActionAnimationSpeeds();
        ConfigureEmoteLoopAudioSource();
    }
    private void OnEnable()
    {
        EnsurePlayerRootMotionDriver(); ApplyConfiguredActionAnimationSpeeds();
        ConfigureEmoteLoopAudioSource();
    }
    private void OnDisable() { StopEmoteLoopAudio(); }
    private void OnValidate()
    {
        ApplyConfiguredActionAnimationSpeeds();
        ConfigureEmoteLoopAudioSource();
    }
    private void Update()
    {
        UpdateQueuedAxeChopState();
        UpdateUpperBodyLayerWeight(); UpdateAnimatorRootMotionState();
    }
    public void Chop() { TryChop(); }
    public bool CanTryChop()
    {
        if (IsUpperBodyStateActive(upperBodyChopStateName)) { return !queuedAxeChop; }
        if (IsUpperBodyStateActive(upperBodyChopSecondStateName)) { return false; }
        if (IsUpperBodyActionLocked()) { return false; }
        return Time.time >= _nextChopAllowedTime;
    }
    public bool TryChop()
    {
        CancelEmoteIfActive();
        if (TryQueueActiveChop())
        {
            _nextChopAllowedTime = Time.time + GetChopRepeatDelaySeconds(); return true;
        }
        if (IsUpperBodyStateActive(upperBodyChopSecondStateName)) { return false; }
        if (IsUpperBodyActionLocked()) { return false; }
        if (Time.time < _nextChopAllowedTime) { return false; }
        float repeatDelay = GetChopRepeatDelaySeconds();
        ActivateUpperBodyLayer(chopUpperBodySeconds); ResetQueuedAxeChop();
        SetUpperBodyChopAnimationSpeed(); ApplyConfiguredActionAnimationSpeeds();
        bool played = TryPlayUpperBodyState(upperBodyChopStateName);
        if (!played) { played = TryPlayUpperBodyState(upperBodyLightAttackStateName); }
        if (!played && axeAnimationScript != null) { played = axeAnimationScript.TryPlayChopAnimation(); }
        if (!played && swordAnimationScript != null)
        {
            swordAnimationScript.AttackLight();
            played = true;
        }
        if (played) { _nextChopAllowedTime = Time.time + repeatDelay; }
        return played;
    }
    public void Walk(bool status) { if (movementAnimationScript != null) { movementAnimationScript.WalkAnimation_Foreward(status); } }
    public void Sprint(bool status, bool playAnimation)
    {
        if (movementAnimationScript != null) { movementAnimationScript.RunAnimation_Foreward(status && playAnimation); }
        if (staminaScript == null) { return; }
        if (status) { staminaScript.ReduceStamina(); }
        else if (playAnimation) { staminaScript.AddStamina(); }
    }
    public void Idle(bool status) { movementAnimationScript.IdleAnimation(status); }
    public void Mine() { TryMine(); }
    public bool TryMine()
    {
        CancelEmoteIfActive();
        if (IsUpperBodyActionLocked()) { return false; }
        ActivateUpperBodyLayer(mineUpperBodySeconds); ApplyConfiguredActionAnimationSpeeds();
        bool played = TryPlayUpperBodyState(upperBodyMiningStateName);
        if (!played && pickaxeAnimationScript != null)
        {
            pickaxeAnimationScript.Mine();
            played = true;
        }
        return played;
    }
    public void Attack() { AttackLight(); }
    public void AttackLight() { TryAttackLight(); }
    public bool TryAttackLight() { return TryWeaponAttack(false, -1); }
    public void AttackHeavy() { TryAttackHeavy(); }
    public bool TryAttackHeavy() { return TryWeaponAttack(true, -1); }
    public bool TryAttackSpecial(int specialIndex) { return TryWeaponAttack(true, specialIndex); }
    private bool TryWeaponAttack(bool heavyAttack, int specialIndex)
    {
        CancelEmoteIfActive();
        if (IsUpperBodyActionLocked() || IsGameplayInputLocked()) { return false; }
        ApplyConfiguredActionAnimationSpeeds(); bool played;
        if (specialIndex >= 0) { played = TryPlayIndexedFullBodyState(heavyAttackStateNames, specialIndex, ref _nextHeavyAttackIndex); }
        else if (heavyAttack) { played = TryPlayNextFullBodyState(heavyAttackStateNames, ref _nextHeavyAttackIndex); }
        else { played = TryPlayNextFullBodyState(lightAttackStateNames, ref _nextLightAttackIndex); }
        if (!played)
        {
            ActivateUpperBodyLayer(heavyAttack ? swordHeavyMovementAnimationLockSeconds : swordMovementAnimationLockSeconds);
            played = TryPlayUpperBodyState(heavyAttack ? upperBodyHeavyAttackStateName : upperBodyLightAttackStateName);
        }
        if (!played && swordAnimationScript != null)
        {
            if (heavyAttack) { swordAnimationScript.AttackHeavy(); } else { swordAnimationScript.AttackLight(); }
            played = true;
        }
        if (played) { ForceStopMovementAnimations(); }
        return played;
    }
    public bool TryEquipSword()
    {
        CancelEmoteIfActive();
        ApplyConfiguredActionAnimationSpeeds();
        return TryPlaySwordFullBodyState(swordEquipStateName);
    }
    public bool TryUnequipSword()
    {
        CancelEmoteIfActive(); ApplyConfiguredActionAnimationSpeeds();
        Animator animator = ResolveCharacterAnimator();
        TrySetAnimatorBoolParameter(animator, SwordBlockingParameterName, false);
        return TryPlaySwordFullBodyState(swordUnequipStateName);
    }
    public bool TryBeginSwordBlock()
    {
        CancelEmoteIfActive();
        Animator animator = ResolveCharacterAnimator(); if (animator == null) { return false; }
        if (TryGetActiveSwordBlockStateInfo(animator, out _))
        {
            TrySetAnimatorBoolParameter(animator, SwordBlockingParameterName, true);
            ForceStopMovementAnimations(); return true;
        }
        if (IsUpperBodyActionLocked() || IsGameplayInputLocked()) { return false; }
        if (!TryGetBaseLayerIndex(animator, out int layerIndex)) { return false; }
        ApplyConfiguredActionAnimationSpeeds();
        TrySetAnimatorBoolParameter(animator, SwordBlockingParameterName, true);
        bool played = TryPlayAnimatorState(animator, layerIndex, swordBlockEnterStateName, fullBodyActionBlendTime);
        if (!played) { played = TryPlayAnimatorState(animator, layerIndex, swordBlockLoopStateName, fullBodyActionBlendTime); }
        if (!played)
        {
            TrySetAnimatorBoolParameter(animator, SwordBlockingParameterName, false);
            return false;
        }
        ForceStopMovementAnimations(); return true;
    }
    public void StopSwordBlock()
    {
        Animator animator = ResolveCharacterAnimator();
        if (animator == null) { return; }
        TrySetAnimatorBoolParameter(animator, SwordBlockingParameterName, false);
        if (!TryGetBaseLayerIndex(animator, out int layerIndex) ||
        !TryGetActiveSwordBlockStateInfo(animator, out AnimatorStateInfo stateInfo)) { return; }
        if (MatchesStateName(stateInfo, swordBlockExitStateName)) { return; }
        TryPlayAnimatorState(animator, layerIndex, swordBlockExitStateName, fullBodyActionBlendTime);
    }
    public void CancelSwordBlock()
    {
        Animator animator = ResolveCharacterAnimator();
        TrySetAnimatorBoolParameter(animator, SwordBlockingParameterName, false);
    }
    public bool IsSwordBlockActive()
    {
        Animator animator = ResolveCharacterAnimator();
        return TryGetActiveSwordBlockStateInfo(animator, out _);
    }
    public void UnarmedPunchCombo() { TryUnarmedPunchCombo(); }
    public bool TryUnarmedPunchCombo()
    {
        CancelEmoteIfActive();
        if (IsUpperBodyActionLocked()) { return false; }
        bool punchLeft = (unarmedPunchStep % 2) == 0;
        ActivateUpperBodyLayer(unarmedPunchMovementAnimationLockSeconds);
        ApplyConfiguredActionAnimationSpeeds(); string targetUpperBodyState = punchLeft
        ? upperBodyPunchLeftStateName : upperBodyPunchRightStateName;
        bool played = TryPlayUpperBodyState(targetUpperBodyState);
        if (!played && swordAnimationScript != null)
        {
            if (punchLeft) { swordAnimationScript.PunchLeft(); } else { swordAnimationScript.PunchRight(); }
            played = true;
        }
        if (played) { unarmedPunchStep = (unarmedPunchStep + 1) % 4; }
        return played;
    }
    public void ResetUnarmedPunchCombo() { unarmedPunchStep = 0; }
    public float GetChopRepeatDelaySeconds() { return Mathf.Max(0.01f, chopRepeatDelaySeconds); }
    public float GetRemainingChopCooldown() { return Mathf.Max(0f, _nextChopAllowedTime - Time.time); }
    public bool IsUpperBodyActionLocked()
    {
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetActionPlaybackLayerIndex(animator, out int layerIndex)) { return false; }
        return IsAnimatorLayerBlockingNewAction(animator, layerIndex);
    }
    public float GetRemainingUpperBodyActionLockSeconds()
    {
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetActionPlaybackLayerIndex(animator, out int layerIndex) ||
        !TryGetActiveActionStateInfo(animator, layerIndex, out AnimatorStateInfo stateInfo)) { return 0f; }
        float remainingNormalized = Mathf.Max(0f, GetUpperBodyActionCompletionThreshold() - stateInfo.normalizedTime);
        float playbackSpeed = Mathf.Abs(stateInfo.speed * stateInfo.speedMultiplier);
        if (playbackSpeed < 0.01f) { playbackSpeed = 1f; }
        return remainingNormalized * stateInfo.length / playbackSpeed;
    }
    public void ForceEndJumpAnimation()
    {
        movementAnimationLockUntil = 0f;
        if (movementAnimationScript != null) { movementAnimationScript.ForceExitJumpAnimation(); }
    }
    public void Jump(float expectedAirTimeSeconds = -1f, bool preferRunningJumpSpeed = false)
    {
        if (movementAnimationScript == null) { return; }
        movementAnimationScript.JumpAnimation(expectedAirTimeSeconds, preferRunningJumpSpeed);
    }
    public void SyncJumpAnimationToAirTime(float remainingAirTimeSeconds)
    {
        if (movementAnimationScript == null) { return; }
        movementAnimationScript.SyncJumpAnimationToAirTime(remainingAirTimeSeconds);
    }
    public void WalkBackwards(bool status) { movementAnimationScript.WalkBackWards(status); }
    public void WalkLeft(bool status)
    {
        if (movementAnimationScript == null) { return; }
        movementAnimationScript.WalkLeft(status);
    }
    public void WalkRight(bool status)
    {
        if (movementAnimationScript == null) { return; }
        movementAnimationScript.WalkRight(status);
    }
    public void SprintLeft(bool status)
    {
        if (movementAnimationScript == null) { return; }
        movementAnimationScript.SprintLeft(status);
    }
    public void SprintRight(bool status)
    {
        if (movementAnimationScript == null) { return; }
        movementAnimationScript.SprintRight(status);
    }
    public void WalkForwardLeft(bool status)
    {
        if (movementAnimationScript == null) { return; }
        movementAnimationScript.WalkForwardLeft(status);
    }
    public void WalkForwardRight(bool status)
    {
        if (movementAnimationScript == null) { return; }
        movementAnimationScript.WalkForwardRight(status);
    }
    public void SprintForwardLeft(bool status)
    {
        if (movementAnimationScript == null) { return; }
        movementAnimationScript.SprintForwardLeft(status);
    }
    public void SprintForwardRight(bool status)
    {
        if (movementAnimationScript == null) { return; }
        movementAnimationScript.SprintForwardRight(status);
    }
    public void UpdateLocomotionAnimations(bool idle, bool forwardWalk, bool forwardRun,
    bool backward, bool leftStrafe, bool rightStrafe, bool sprintLeft, bool sprintRight,
    bool forwardLeftWalk, bool forwardRightWalk, bool forwardLeftRun, bool forwardRightRun,
    bool sprintingForStamina)
    {
        if (staminaScript != null)
        {
            if (sprintingForStamina) { staminaScript.ReduceStamina(); } else { staminaScript.AddStamina(); }
        }
        if (movementAnimationScript == null) { return; }
        movementAnimationScript.SetLocomotion(idle, forwardWalk, forwardRun, backward,
        leftStrafe, rightStrafe, sprintLeft, sprintRight, forwardLeftWalk, forwardRightWalk,
        forwardLeftRun, forwardRightRun);
    }
    public bool IsMovementAnimationLocked() { return Time.time < movementAnimationLockUntil || IsAnimatorInActionState(); }
    public bool IsGameplayInputLocked()
    {
        Animator animator = ResolveCharacterAnimator();
        return TryGetActiveFullBodyActionStateInfo(animator, out _);
    }
    public bool TryStartEmote()
    {
        if (string.IsNullOrWhiteSpace(emoteStateName) ||
    IsUpperBodyActionLocked() || IsGameplayInputLocked() ||
    IsSwordBlockActive()) { return false; }
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetBaseLayerIndex(animator, out int layerIndex)) { return false; }
        ApplyConfiguredActionAnimationSpeeds();
        TrySetAnimatorBoolParameter(animator, SwordBlockingParameterName, false);
        bool played = TryPlayAnimatorState(animator, layerIndex, emoteStateName, Mathf.Max(0f, emoteBlendTime));
        if (played) { ForceStopMovementAnimations(); PlayEmoteLoopAudio(); }
        return played;
    }
    public void StopEmote()
    {
        StopEmoteLoopAudio();
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetBaseLayerIndex(animator, out int layerIndex) ||
        !TryGetAnimatorStateInfo(animator, layerIndex, emoteStateName, out _)) { return; }
        TryPlayAnimatorState(animator, layerIndex, "Idle", Mathf.Max(0f, emoteCancelBlendTime));
    }
    public bool IsEmoteActive()
    {
        Animator animator = ResolveCharacterAnimator();
        return TryGetBaseLayerIndex(animator, out int layerIndex) &&
        TryGetAnimatorStateInfo(animator, layerIndex, emoteStateName, out _);
    }
    public float GetRemainingGameplayInputLockSeconds()
    {
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetActiveFullBodyActionStateInfo(animator, out AnimatorStateInfo stateInfo)) { return 0f; }
        float remainingNormalized = Mathf.Max(0f, GetFullBodyActionCompletionThreshold() - NormalizeStateProgress(stateInfo));
        float playbackSpeed = Mathf.Abs(stateInfo.speed * stateInfo.speedMultiplier);
        if (playbackSpeed < 0.01f) { playbackSpeed = 1f; }
        return remainingNormalized * stateInfo.length / playbackSpeed;
    }
    public bool TryGetActiveSwordAttackStateInfo(out AnimatorStateInfo stateInfo, out bool isHeavyAttack)
    {
        Animator animator = ResolveCharacterAnimator();
        return TryGetActiveSwordAttackStateInfo(animator, out stateInfo, out isHeavyAttack);
    }
    private bool TryPlaySwordFullBodyState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName) || IsUpperBodyActionLocked() || IsGameplayInputLocked()) { return false; }
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetBaseLayerIndex(animator, out int layerIndex)) { return false; }
        ApplyConfiguredActionAnimationSpeeds();
        TrySetAnimatorBoolParameter(animator, SwordBlockingParameterName, false);
        bool played = TryPlayAnimatorState(animator, layerIndex, stateName, fullBodyActionBlendTime);
        if (played) { ForceStopMovementAnimations(); }
        return played;
    }
    public bool ShouldConsumeAnimatorRootMotion()
    {
        Animator animator = ResolveCharacterAnimator();
        return TryGetActiveFullBodyRootMotionStateInfo(animator, out _);
    }
    public void LockMovementAnimations(float seconds)
    {
        float lockDuration = Mathf.Max(0f, seconds); if (lockDuration <= 0f) { return; }
        ForceStopMovementAnimations(); float lockUntil = Time.time + lockDuration;
        if (lockUntil > movementAnimationLockUntil) { movementAnimationLockUntil = lockUntil; }
    }
    private void ForceStopMovementAnimations()
    {
        if (movementAnimationScript == null) { return; }
        movementAnimationScript.IdleAnimation(false);
        movementAnimationScript.WalkAnimation_Foreward(false);
        movementAnimationScript.RunAnimation_Foreward(false);
        movementAnimationScript.WalkBackWards(false); movementAnimationScript.WalkLeft(false);
        movementAnimationScript.WalkRight(false); movementAnimationScript.SprintLeft(false);
        movementAnimationScript.SprintRight(false); movementAnimationScript.WalkForwardLeft(false);
        movementAnimationScript.WalkForwardRight(false);
        movementAnimationScript.SprintForwardLeft(false);
        movementAnimationScript.SprintForwardRight(false);
    }
    private void CancelEmoteIfActive() { if (IsEmoteActive()) { StopEmote(); } }
    private void ConfigureEmoteLoopAudioSource()
    {
        if (emoteLoopAudioSource == null && Application.isPlaying) { emoteLoopAudioSource = gameObject.AddComponent<AudioSource>(); }
        if (emoteLoopAudioSource == null) { return; }
        emoteLoopAudioSource.playOnAwake = false;
        emoteLoopAudioSource.loop = true; emoteLoopAudioSource.spatialBlend = 0f;
        emoteLoopAudioSource.clip = emoteLoopClip;
        emoteLoopAudioSource.volume = Mathf.Clamp01(emoteLoopVolume);
    }
    private void PlayEmoteLoopAudio()
    {
        ConfigureEmoteLoopAudioSource();
        if (emoteLoopAudioSource == null || emoteLoopClip == null) { return; }
        emoteLoopAudioSource.clip = emoteLoopClip;
        emoteLoopAudioSource.volume = Mathf.Clamp01(emoteLoopVolume);
        if (!emoteLoopClip.preloadAudioData) { emoteLoopClip.LoadAudioData(); }
        if (!emoteLoopAudioSource.isPlaying) { emoteLoopAudioSource.Play(); }
    }
    private void StopEmoteLoopAudio() { if (emoteLoopAudioSource != null && emoteLoopAudioSource.isPlaying) { emoteLoopAudioSource.Stop(); } }
    private void UpdateAnimatorRootMotionState()
    {
        if (movementAnimationScript == null) { return; }
        EnsurePlayerRootMotionDriver();
        movementAnimationScript.SetAnimatorRootMotionEnabled(ShouldConsumeAnimatorRootMotion());
    }
    private PlayerRootMotionDriver EnsurePlayerRootMotionDriver()
    {
        Animator animator = ResolveCharacterAnimator(); if (animator == null) { return null; }
        if (_rootMotionDriver != null)
        {
            if (_rootMotionDriver.gameObject == animator.gameObject) { return _rootMotionDriver; }
            _rootMotionDriver = null;
        }
        _rootMotionDriver = animator.GetComponent<PlayerRootMotionDriver>();
        if (_rootMotionDriver == null) { _rootMotionDriver = animator.gameObject.AddComponent<PlayerRootMotionDriver>(); }
        return _rootMotionDriver;
    }
    private void ActivateUpperBodyLayer(float activeSeconds)
    {
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetUpperBodyLayerIndex(animator, out int layerIndex)) { return; }
        float holdSeconds = Mathf.Max(upperBodyMinimumActiveSeconds, activeSeconds);
        float activeUntil = Time.time + holdSeconds;
        if (activeUntil > upperBodyLayerActiveUntil) { upperBodyLayerActiveUntil = activeUntil; }
        if (animator.GetLayerWeight(layerIndex) < 1f) { animator.SetLayerWeight(layerIndex, 1f); }
    }
    private void UpdateUpperBodyLayerWeight()
    {
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetUpperBodyLayerIndex(animator, out int layerIndex)) { return; }
        TryForceCompletedUpperBodyActionToIdle(animator, layerIndex);
        bool timerActive = Time.time < upperBodyLayerActiveUntil;
        bool layerPlayingAction = IsAnimatorLayerInActionState(animator, layerIndex);
        float targetWeight = (timerActive || layerPlayingAction || upperBodyExternalHold) ? 1f : 0f;
        float currentWeight = animator.GetLayerWeight(layerIndex);
        float blendSpeed = Mathf.Max(1f, upperBodyLayerBlendSpeed);
        float nextWeight = Mathf.MoveTowards(currentWeight, targetWeight, blendSpeed * Time.deltaTime);
        if (!Mathf.Approximately(currentWeight, nextWeight)) { animator.SetLayerWeight(layerIndex, nextWeight); }
    }
    public void SetUpperBodyExternalHold(bool active) { upperBodyExternalHold = active; }
    public void CancelUpperBodyAction()
    {
        upperBodyExternalHold = false;
        upperBodyLayerActiveUntil = 0f; ResetQueuedAxeChop();
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetUpperBodyLayerIndex(animator, out int layerIndex)) { return; }
        TryPlayUpperBodyIdle(animator, layerIndex, 0f); animator.SetLayerWeight(layerIndex, 0f);
    }
    private static bool IsAnimatorLayerInActionState(Animator animator, int layerIndex)
    {
        if (animator == null || layerIndex < 0 || layerIndex >= animator.layerCount) { return false; }
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (IsActionState(current)) { return true; }
        if (animator.IsInTransition(layerIndex))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layerIndex);
            if (IsActionState(next)) { return true; }
        }
        return false;
    }
    private Animator ResolveCharacterAnimator()
    {
        if (movementAnimationScript != null && movementAnimationScript.animator != null) { return movementAnimationScript.animator; }
        if (swordAnimationScript != null && swordAnimationScript.animator != null) { return swordAnimationScript.animator; }
        if (pickaxeAnimationScript != null && pickaxeAnimationScript.animator != null) { return pickaxeAnimationScript.animator; }
        if (axeAnimationScript != null && axeAnimationScript.axeanimator != null) { return axeAnimationScript.axeanimator; }
        return null;
    }
    private bool TryGetActionPlaybackLayerIndex(Animator animator, out int layerIndex)
    {
        if (TryGetUpperBodyLayerIndex(animator, out layerIndex)) { return true; }
        if (animator != null && animator.layerCount > 0) { layerIndex = 0; return true; }
        layerIndex = -1; return false;
    }
    private bool TryGetUpperBodyLayerIndex(Animator animator, out int layerIndex)
    {
        layerIndex = -1;
        if (animator == null || string.IsNullOrWhiteSpace(upperBodyLayerName)) { return false; }
        layerIndex = animator.GetLayerIndex(upperBodyLayerName); return layerIndex >= 0;
    }
    private bool TryGetBaseLayerIndex(Animator animator, out int layerIndex)
    {
        layerIndex = -1; if (animator == null || animator.layerCount <= 0) { return false; }
        if (!string.IsNullOrWhiteSpace(baseLayerName)) { layerIndex = animator.GetLayerIndex(baseLayerName); }
        if (layerIndex < 0) { layerIndex = 0; }
        return layerIndex >= 0 && layerIndex < animator.layerCount;
    }
    private bool TryPlayAnimatorState(Animator animator, int layerIndex, string stateName, float blendTime)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName)) { return false; }
        string layerName = animator.GetLayerName(layerIndex);
        int fullPathHash = Animator.StringToHash($"{layerName}.{stateName}");
        int shortNameHash = Animator.StringToHash(stateName); int stateHash;
        if (animator.HasState(layerIndex, fullPathHash))
        {
            stateHash = fullPathHash;
        }
        else if (animator.HasState(layerIndex, shortNameHash)) { stateHash = shortNameHash; } else { return false; }
        float resolvedBlendTime = Mathf.Max(0f, blendTime);
        if (resolvedBlendTime > 0f) { animator.CrossFadeInFixedTime(stateHash, resolvedBlendTime, layerIndex); } else { animator.Play(stateHash, layerIndex, 0f); }
        return true;
    }
    private bool TryPlayUpperBodyState(string stateName)
    {
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetUpperBodyLayerIndex(animator, out int layerIndex) ||
        string.IsNullOrWhiteSpace(stateName)) { return false; }
        int fullPathHash = Animator.StringToHash($"{upperBodyLayerName}.{stateName}");
        int shortNameHash = Animator.StringToHash(stateName); int stateHash;
        if (animator.HasState(layerIndex, fullPathHash))
        {
            stateHash = fullPathHash;
        }
        else if (animator.HasState(layerIndex, shortNameHash)) { stateHash = shortNameHash; } else { return false; }
        float blendTime = Mathf.Max(0f, upperBodyStateBlendTime);
        if (blendTime > 0f) { animator.CrossFadeInFixedTime(stateHash, blendTime, layerIndex); } else { animator.Play(stateHash, layerIndex, 0f); }
        return true;
    }
    private void TryForceCompletedUpperBodyActionToIdle(Animator animator, int layerIndex)
    {
        if (animator == null || upperBodyExternalHold || Time.time < upperBodyLayerActiveUntil ||
        animator.IsInTransition(layerIndex)) { return; }
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (!IsActionState(current)) { return; }
        if (current.normalizedTime < GetUpperBodyActionCompletionThreshold()) { return; }
        if (TryPlayUpperBodyIdle(animator, layerIndex, upperBodyStateBlendTime)) { animator.SetLayerWeight(layerIndex, 0f); }
    }
    private bool TryPlayUpperBodyIdle(Animator animator, int layerIndex, float blendTime)
    {
        if (animator == null || layerIndex < 0 || string.IsNullOrWhiteSpace(upperBodyIdleStateName)) { return false; }
        int fullPathHash = Animator.StringToHash($"{upperBodyLayerName}.{upperBodyIdleStateName}");
        int shortNameHash = Animator.StringToHash(upperBodyIdleStateName); int stateHash;
        if (animator.HasState(layerIndex, fullPathHash))
        {
            stateHash = fullPathHash;
        }
        else if (animator.HasState(layerIndex, shortNameHash)) { stateHash = shortNameHash; } else { return false; }
        float resolvedBlend = Mathf.Max(0f, blendTime);
        if (resolvedBlend > 0f) { animator.CrossFadeInFixedTime(stateHash, resolvedBlend, layerIndex); } else { animator.Play(stateHash, layerIndex, 0f); }
        return true;
    }
    private void SetUpperBodyChopAnimationSpeed() { SetUpperBodyChopAnimationSpeed(null); }
    private void SetUpperBodyChopAnimationSpeed(float? overrideSpeed)
    {
        Animator animator = ResolveCharacterAnimator(); if (animator == null) { return; }
        float chopSpeed = overrideSpeed ?? ResolveChopAnimationSpeed();
        TrySetAnimatorFloatParameter(animator, upperBodyChopSpeedParameterName, chopSpeed);
    }
    private void ApplyConfiguredActionAnimationSpeeds()
    {
        Animator animator = ResolveCharacterAnimator(); if (animator == null) { return; }
        float swordAnimationSpeedMultiplier = ResolveEquippedSwordAnimationSpeedMultiplier();
        TrySetAnimatorFloatParameter(animator, AttackLightSpeedParameterName, ResolveConfiguredSpeed(lightAttackAnimationSpeed) * swordAnimationSpeedMultiplier);
        TrySetAnimatorFloatParameter(animator, AttackHeavySpeedParameterName, ResolveConfiguredSpeed(heavyAttackAnimationSpeed) * swordAnimationSpeedMultiplier);
        TrySetAnimatorFloatParameter(animator, PunchLeftSpeedParameterName, ResolveConfiguredSpeed(punchLeftAnimationSpeed));
        TrySetAnimatorFloatParameter(animator, PunchRightSpeedParameterName, ResolveConfiguredSpeed(punchRightAnimationSpeed));
        TrySetAnimatorFloatParameter(animator, MineSpeedParameterName, ResolveConfiguredSpeed(mineAnimationSpeed));
        TrySetAnimatorFloatParameter(animator, GunAimSpeedParameterName, ResolveConfiguredSpeed(gunAimAnimationSpeed));
        TrySetAnimatorFloatParameter(animator, GunShootSpeedParameterName, ResolveConfiguredSpeed(gunShootAnimationSpeed));
        TrySetAnimatorFloatParameter(animator, GunReloadSpeedParameterName, ResolveConfiguredSpeed(gunReloadAnimationSpeed));
        SetUpperBodyChopAnimationSpeed();
    }
    private float ResolveEquippedSwordAnimationSpeedMultiplier()
    {
        ItemSwitchScript itemSwitchScript = ResolveItemSwitchScript();
        if (itemSwitchScript == null || !itemSwitchScript.TryGetEquippedSword(out Sword equippedSword)) { return 1f; }
        return equippedSword.GetResolvedAnimationSpeed();
    }
    private bool TryQueueActiveChop()
    {
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetUpperBodyLayerIndex(animator, out int layerIndex) ||
        !TryGetAnimatorStateInfo(animator, layerIndex, upperBodyChopStateName, out _)) { return false; }
        queuedAxeChop = true; upperBodyLayerActiveUntil = Mathf.Max(upperBodyLayerActiveUntil,
        Time.time + Mathf.Max(upperBodyMinimumActiveSeconds, chopUpperBodySeconds));
        return true;
    }
    private bool TryPlayNextFullBodyState(string[] stateNames, ref int nextStateIndex)
    {
        if (stateNames == null || stateNames.Length == 0) { return false; }
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetBaseLayerIndex(animator, out int layerIndex)) { return false; }
        int startIndex = Mathf.Clamp(nextStateIndex, 0, stateNames.Length - 1);
        for (int offset = 0; offset < stateNames.Length; offset++)
        {
            int index = (startIndex + offset) % stateNames.Length;
            if (!TryPlayAnimatorState(animator, layerIndex, stateNames[index], fullBodyActionBlendTime)) { continue; }
            nextStateIndex = (index + 1) % stateNames.Length; return true;
        }
        return false;
    }
    private bool TryPlayIndexedFullBodyState(string[] stateNames, int stateIndex, ref int nextStateIndex)
    {
        if (stateNames == null || stateNames.Length == 0) { return false; }
        int clampedIndex = Mathf.Clamp(stateIndex, 0, stateNames.Length - 1);
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetBaseLayerIndex(animator, out int layerIndex) ||
        !TryPlayAnimatorState(animator, layerIndex, stateNames[clampedIndex], fullBodyActionBlendTime)) { return false; }
        nextStateIndex = (clampedIndex + 1) % stateNames.Length; return true;
    }
    private void UpdateQueuedAxeChopState()
    {
        if (!queuedAxeChop) { return; }
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetUpperBodyLayerIndex(animator, out int layerIndex) ||
        !TryGetAnimatorStateInfo(animator, layerIndex, upperBodyChopStateName, out AnimatorStateInfo chopState))
        {
            ResetQueuedAxeChop(); return;
        }
        if (animator.IsInTransition(layerIndex)) { return; }
        float comboQueueThreshold = Mathf.Clamp(axeComboHoldNormalizedTime, 0.01f,
        Mathf.Max(0.01f, upperBodyActionCompletionThreshold));
        if (chopState.normalizedTime < comboQueueThreshold) { return; }
        ActivateUpperBodyLayer(chopUpperBodySeconds); SetUpperBodyChopAnimationSpeed();
        if (!TryPlayUpperBodyState(upperBodyChopSecondStateName))
        {
            ResetQueuedAxeChop();
            return;
        }
        queuedAxeChop = false;
        _nextChopAllowedTime = Time.time + GetChopRepeatDelaySeconds();
    }
    private void ResetQueuedAxeChop()
    {
        if (!queuedAxeChop) { return; }
        queuedAxeChop = false;
        SetUpperBodyChopAnimationSpeed();
    }
    private bool TryGetAnimatorStateInfo(
        Animator animator, int layerIndex, string stateName, out AnimatorStateInfo stateInfo)
    {
        stateInfo = default;
        if (animator == null || string.IsNullOrWhiteSpace(stateName)) { return false; }
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (MatchesStateName(current, stateName)) { stateInfo = current; return true; }
        if (!animator.IsInTransition(layerIndex)) { return false; }
        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layerIndex);
        if (!MatchesStateName(next, stateName)) { return false; }
        stateInfo = next; return true;
    }
    private bool MatchesStateName(AnimatorStateInfo state, string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName)) { return false; }
        return state.IsName(stateName) || state.IsName($"{upperBodyLayerName}.{stateName}") ||
        state.IsName($"{baseLayerName}.{stateName}");
    }
    private bool MatchesAnyStateName(AnimatorStateInfo state, string[] stateNames)
    {
        if (stateNames == null) { return false; }
        for (int i = 0; i < stateNames.Length; i++) { if (MatchesStateName(state, stateNames[i])) { return true; } }
        return false;
    }
    private bool IsUpperBodyStateActive(string stateName)
    {
        Animator animator = ResolveCharacterAnimator();
        return TryGetUpperBodyLayerIndex(animator, out int layerIndex) &&
        TryGetAnimatorStateInfo(animator, layerIndex, stateName, out _);
    }
    private float GetUpperBodyActionCompletionThreshold() { return Mathf.Max(0.5f, Mathf.Clamp01(upperBodyActionCompletionThreshold)); }
    private float GetFullBodyActionCompletionThreshold() { return Mathf.Max(0.5f, Mathf.Clamp01(fullBodyActionCompletionThreshold)); }
    private static float NormalizeStateProgress(AnimatorStateInfo state)
    {
        float normalizedTime = state.normalizedTime;
        if (normalizedTime > 1f) { normalizedTime %= 1f; }
        return Mathf.Clamp01(normalizedTime);
    }
    private bool IsFullBodyActionState(AnimatorStateInfo state)
    {
        return MatchesAnyStateName(state, lightAttackStateNames) ||
        MatchesAnyStateName(state, heavyAttackStateNames) ||
        MatchesStateName(state, swordEquipStateName) ||
        MatchesStateName(state, swordUnequipStateName);
    }
    private bool IsSwordAttackState(AnimatorStateInfo state, out bool isHeavyAttack)
    {
        isHeavyAttack = false;
        if (MatchesAnyStateName(state, lightAttackStateNames)) { return true; }
        if (MatchesAnyStateName(state, heavyAttackStateNames))
        {
            isHeavyAttack = true;
            return true;
        }
        return false;
    }
    private bool IsSwordBlockState(AnimatorStateInfo state)
    {
        return MatchesStateName(state, swordBlockEnterStateName) ||
        MatchesStateName(state, swordBlockLoopStateName) ||
        MatchesStateName(state, swordBlockExitStateName);
    }
    private bool TryGetActiveFullBodyActionStateInfo(Animator animator, out AnimatorStateInfo stateInfo)
    {
        stateInfo = default;
        if (!TryGetBaseLayerIndex(animator, out int layerIndex) || !animator.isActiveAndEnabled) { return false; }
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (IsFullBodyActionState(current)) { stateInfo = current; return true; }
        if (!animator.IsInTransition(layerIndex)) { return false; }
        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layerIndex);
        if (!IsFullBodyActionState(next)) { return false; }
        stateInfo = next; return true;
    }
    private bool TryGetActiveSwordAttackStateInfo(Animator animator, out AnimatorStateInfo stateInfo, out bool isHeavyAttack)
    {
        stateInfo = default; isHeavyAttack = false;
        if (!TryGetBaseLayerIndex(animator, out int layerIndex) || !animator.isActiveAndEnabled) { return false; }
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (IsSwordAttackState(current, out isHeavyAttack)) { stateInfo = current; return true; }
        if (!animator.IsInTransition(layerIndex)) { return false; }
        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layerIndex);
        if (!IsSwordAttackState(next, out isHeavyAttack)) { return false; }
        stateInfo = next;
        return true;
    }
    private bool TryGetActiveSwordBlockStateInfo(Animator animator, out AnimatorStateInfo stateInfo)
    {
        stateInfo = default;
        if (!TryGetBaseLayerIndex(animator, out int layerIndex) || !animator.isActiveAndEnabled) { return false; }
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (IsSwordBlockState(current)) { stateInfo = current; return true; }
        if (!animator.IsInTransition(layerIndex)) { return false; }
        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layerIndex);
        if (!IsSwordBlockState(next)) { return false; }
        stateInfo = next; return true;
    }
    private bool TryGetActiveFullBodyRootMotionStateInfo(Animator animator, out AnimatorStateInfo stateInfo)
    {
        stateInfo = default;
        if (!TryGetBaseLayerIndex(animator, out int layerIndex) || !animator.isActiveAndEnabled) { return false; }
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (!IsFullBodyActionState(current)) { return false; }
        if (!animator.IsInTransition(layerIndex)) { stateInfo = current; return true; }
        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layerIndex);
        if (!IsFullBodyActionState(next)) { return false; }
        stateInfo = next; return true;
    }
    private bool TryGetActiveActionStateInfo(Animator animator, int layerIndex, out AnimatorStateInfo stateInfo)
    {
        stateInfo = default;
        if (animator == null || layerIndex < 0 || layerIndex >= animator.layerCount || !animator.isActiveAndEnabled) { return false; }
        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (IsActionState(current)) { stateInfo = current; return true; }
        if (!animator.IsInTransition(layerIndex)) { return false; }
        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layerIndex);
        if (!IsActionState(next)) { return false; }
        stateInfo = next; return true;
    }
    private bool IsAnimatorLayerBlockingNewAction(Animator animator, int layerIndex)
    {
        if (!TryGetActiveActionStateInfo(animator, layerIndex, out AnimatorStateInfo actionState)) { return false; }
        if (animator.IsInTransition(layerIndex)) { return true; }
        return actionState.normalizedTime < GetUpperBodyActionCompletionThreshold();
    }
    private float ResolveChopAnimationSpeed()
    {
        if (axeAnimationScript != null) { return axeAnimationScript.GetResolvedSwingAnimationSpeed(); }
        return 1f;
    }
    private ItemSwitchScript ResolveItemSwitchScript()
    {
        if (_itemSwitchScript != null) { return _itemSwitchScript; }
        _itemSwitchScript = GetComponent<ItemSwitchScript>();
        if (_itemSwitchScript != null) { return _itemSwitchScript; }
        _itemSwitchScript = GetComponentInParent<ItemSwitchScript>();
        if (_itemSwitchScript != null) { return _itemSwitchScript; }
        _itemSwitchScript = UnitySceneSearch.FindFirst<ItemSwitchScript>();
        return _itemSwitchScript;
    }
    private static float ResolveConfiguredSpeed(float value) { return value > 0f ? value : 1f; }
    private static bool TrySetAnimatorFloatParameter(Animator animator, string parameterName, float value)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName)) { return false; }
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.type != AnimatorControllerParameterType.Float ||
            !string.Equals(parameter.name, parameterName, System.StringComparison.Ordinal)) { continue; }
            animator.SetFloat(parameterName, Mathf.Max(0.01f, value)); return true;
        }
        return false;
    }
    private static bool TrySetAnimatorBoolParameter(Animator animator, string parameterName, bool value)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName)) { return false; }
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.type != AnimatorControllerParameterType.Bool ||
            !string.Equals(parameter.name, parameterName, System.StringComparison.Ordinal)) { continue; }
            animator.SetBool(parameterName, value); return true;
        }
        return false;
    }
    private bool IsAnimatorInActionState()
    {
        if (movementAnimationScript != null && movementAnimationScript.IsBlockingActionState()) { return true; }
        Animator animator = movementAnimationScript != null ? movementAnimationScript.animator : null;
        if (animator == null && swordAnimationScript != null) { animator = swordAnimationScript.animator; }
        if (animator == null || !animator.isActiveAndEnabled ||
        (movementAnimationScript != null && animator == movementAnimationScript.animator)) { return false; }
        return IsAnimatorLayerInActionState(animator, 0);
    }
    private static bool IsActionState(AnimatorStateInfo state)
    {
        if (state.IsTag("Action")) { return true; }
        int stateHash = state.shortNameHash; return stateHash == AttackWeaponStateHash ||
        stateHash == AttackTwoHandedStateHash || stateHash == PunchLeftStateHash ||
        stateHash == PunchRightStateHash || stateHash == MiningStateHash ||
        stateHash == ChopStateHash || stateHash == JumpStateHash;
    }
}
