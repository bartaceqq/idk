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
    public float mineUpperBodySeconds = 0.9f;
    public float chopUpperBodySeconds = 0.9f;
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

    [Header("Action Animation Speeds")]
    public float lightAttackAnimationSpeed = 1f;
    public float heavyAttackAnimationSpeed = 1f;
    public float punchLeftAnimationSpeed = 1f;
    public float punchRightAnimationSpeed = 1f;
    public float mineAnimationSpeed = 1f;
    public float gunAimAnimationSpeed = 1f;
    public float gunShootAnimationSpeed = 1f;
    public float gunReloadAnimationSpeed = 1f;

    [Header("Axe Combo")]
    [Range(0f, 0.99f)] public float axeComboHoldNormalizedTime = 0.79f;

    public MovementAnimationScript movementAnimationScript;
    public AxeAnimationScript axeAnimationScript;
    public PickaxeAnimationScript pickaxeAnimationScript;
    public SwordAnimationScript swordAnimationScript;

    private float movementAnimationLockUntil;
    private float upperBodyLayerActiveUntil;
    private float _nextChopAllowedTime;
    private int unarmedPunchStep;
    private bool upperBodyExternalHold;
    private bool axeComboContinueRequested;
    private bool axeComboHoldActive;

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

    private void Awake()
    {
        ApplyConfiguredActionAnimationSpeeds();
    }

    private void OnEnable()
    {
        ApplyConfiguredActionAnimationSpeeds();
    }

    private void OnValidate()
    {
        ApplyConfiguredActionAnimationSpeeds();
    }

    private void Update()
    {
        UpdateAxeComboHoldState();
        UpdateUpperBodyLayerWeight();
    }

    // Handle Chop.
    public void Chop()
    {
        TryChop();
    }

    // Handle Try Chop.
    public bool TryChop()
    {
        if (TryContinueActiveChopCombo())
        {
            _nextChopAllowedTime = Time.time + GetChopRepeatDelaySeconds();
            return true;
        }

        if (Time.time < _nextChopAllowedTime)
        {
            return false;
        }

        float repeatDelay = GetChopRepeatDelaySeconds();
        ActivateUpperBodyLayer(repeatDelay);
        ResetAxeComboState();
        SetUpperBodyChopAnimationSpeed();
        ApplyConfiguredActionAnimationSpeeds();

        bool played = TryPlayUpperBodyState(upperBodyChopStateName);
        if (!played)
        {
            played = TryPlayUpperBodyState(upperBodyLightAttackStateName);
        }

        if (!played && axeAnimationScript != null)
        {
            played = axeAnimationScript.TryPlayChopAnimation();
        }

        if (!played && swordAnimationScript != null)
        {
            swordAnimationScript.AttackLight();
            played = true;
        }

        if (played)
        {
            _nextChopAllowedTime = Time.time + repeatDelay;
        }

        return played;
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
        ActivateUpperBodyLayer(mineUpperBodySeconds);
        ApplyConfiguredActionAnimationSpeeds();
        if (!TryPlayUpperBodyState(upperBodyMiningStateName) && pickaxeAnimationScript != null)
        {
            pickaxeAnimationScript.Mine();
        }
    }
    // Handle Attack.
    public void Attack()
    {
        AttackLight();
    }

    // Handle Attack Light.
    public void AttackLight()
    {
        ActivateUpperBodyLayer(swordMovementAnimationLockSeconds);
        ApplyConfiguredActionAnimationSpeeds();
        if (!TryPlayUpperBodyState(upperBodyLightAttackStateName) && swordAnimationScript != null)
        {
            swordAnimationScript.AttackLight();
        }
    }

    // Handle Attack Heavy.
    public void AttackHeavy()
    {
        ActivateUpperBodyLayer(swordHeavyMovementAnimationLockSeconds);
        ApplyConfiguredActionAnimationSpeeds();
        if (!TryPlayUpperBodyState(upperBodyHeavyAttackStateName) && swordAnimationScript != null)
        {
            swordAnimationScript.AttackHeavy();
        }
    }

    // Handle Unarmed Punch Combo.
    public void UnarmedPunchCombo()
    {
        bool punchLeft = (unarmedPunchStep % 2) == 0;
        ActivateUpperBodyLayer(unarmedPunchMovementAnimationLockSeconds);
        ApplyConfiguredActionAnimationSpeeds();

        string targetUpperBodyState = punchLeft
            ? upperBodyPunchLeftStateName
            : upperBodyPunchRightStateName;

        if (!TryPlayUpperBodyState(targetUpperBodyState) && swordAnimationScript != null)
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

    // Handle Get Chop Repeat Delay Seconds.
    public float GetChopRepeatDelaySeconds()
    {
        return Mathf.Max(0.01f, chopRepeatDelaySeconds);
    }

    // Handle Get Remaining Chop Cooldown.
    public float GetRemainingChopCooldown()
    {
        return Mathf.Max(0f, _nextChopAllowedTime - Time.time);
    }

    // Handle Force End Jump Animation.
    public void ForceEndJumpAnimation()
    {
        movementAnimationLockUntil = 0f;
        if (movementAnimationScript != null)
        {
            movementAnimationScript.ForceExitJumpAnimation();
        }
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

    // Handle Activate Upper Body Layer.
    private void ActivateUpperBodyLayer(float activeSeconds)
    {
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetUpperBodyLayerIndex(animator, out int layerIndex))
        {
            return;
        }

        float holdSeconds = Mathf.Max(upperBodyMinimumActiveSeconds, activeSeconds);
        float activeUntil = Time.time + holdSeconds;
        if (activeUntil > upperBodyLayerActiveUntil)
        {
            upperBodyLayerActiveUntil = activeUntil;
        }

        if (animator.GetLayerWeight(layerIndex) < 1f)
        {
            animator.SetLayerWeight(layerIndex, 1f);
        }
    }

    // Handle Update Upper Body Layer Weight.
    private void UpdateUpperBodyLayerWeight()
    {
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetUpperBodyLayerIndex(animator, out int layerIndex))
        {
            return;
        }

        TryForceCompletedUpperBodyActionToIdle(animator, layerIndex);

        bool timerActive = Time.time < upperBodyLayerActiveUntil;
        bool layerPlayingAction = IsAnimatorLayerInActionState(animator, layerIndex);
        float targetWeight = (timerActive || layerPlayingAction || upperBodyExternalHold) ? 1f : 0f;
        float currentWeight = animator.GetLayerWeight(layerIndex);
        float blendSpeed = Mathf.Max(1f, upperBodyLayerBlendSpeed);
        float nextWeight = Mathf.MoveTowards(currentWeight, targetWeight, blendSpeed * Time.deltaTime);
        if (!Mathf.Approximately(currentWeight, nextWeight))
        {
            animator.SetLayerWeight(layerIndex, nextWeight);
        }
    }

    // Handle Set Upper Body External Hold.
    public void SetUpperBodyExternalHold(bool active)
    {
        upperBodyExternalHold = active;
    }

    // Handle Cancel Upper Body Action.
    public void CancelUpperBodyAction()
    {
        upperBodyExternalHold = false;
        upperBodyLayerActiveUntil = 0f;
        ResetAxeComboState();

        Animator animator = ResolveCharacterAnimator();
        if (!TryGetUpperBodyLayerIndex(animator, out int layerIndex))
        {
            return;
        }

        TryPlayUpperBodyIdle(animator, layerIndex, 0f);
        animator.SetLayerWeight(layerIndex, 0f);
    }

    // Handle Is Animator Layer In Action State.
    private static bool IsAnimatorLayerInActionState(Animator animator, int layerIndex)
    {
        if (animator == null || layerIndex < 0 || layerIndex >= animator.layerCount)
        {
            return false;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (IsActionState(current))
        {
            return true;
        }

        if (animator.IsInTransition(layerIndex))
        {
            AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layerIndex);
            if (IsActionState(next))
            {
                return true;
            }
        }

        return false;
    }

    // Handle Resolve Character Animator.
    private Animator ResolveCharacterAnimator()
    {
        if (movementAnimationScript != null && movementAnimationScript.animator != null)
        {
            return movementAnimationScript.animator;
        }

        if (swordAnimationScript != null && swordAnimationScript.animator != null)
        {
            return swordAnimationScript.animator;
        }

        if (pickaxeAnimationScript != null && pickaxeAnimationScript.animator != null)
        {
            return pickaxeAnimationScript.animator;
        }

        if (axeAnimationScript != null && axeAnimationScript.axeanimator != null)
        {
            return axeAnimationScript.axeanimator;
        }

        return null;
    }

    // Handle Try Get Upper Body Layer Index.
    private bool TryGetUpperBodyLayerIndex(Animator animator, out int layerIndex)
    {
        layerIndex = -1;
        if (animator == null || string.IsNullOrWhiteSpace(upperBodyLayerName))
        {
            return false;
        }

        layerIndex = animator.GetLayerIndex(upperBodyLayerName);
        return layerIndex >= 0;
    }

    // Handle Try Play Upper Body State.
    private bool TryPlayUpperBodyState(string stateName)
    {
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetUpperBodyLayerIndex(animator, out int layerIndex) ||
            string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        int fullPathHash = Animator.StringToHash($"{upperBodyLayerName}.{stateName}");
        int shortNameHash = Animator.StringToHash(stateName);
        int stateHash;

        if (animator.HasState(layerIndex, fullPathHash))
        {
            stateHash = fullPathHash;
        }
        else if (animator.HasState(layerIndex, shortNameHash))
        {
            stateHash = shortNameHash;
        }
        else
        {
            return false;
        }

        float blendTime = Mathf.Max(0f, upperBodyStateBlendTime);
        if (blendTime > 0f)
        {
            animator.CrossFadeInFixedTime(stateHash, blendTime, layerIndex);
        }
        else
        {
            animator.Play(stateHash, layerIndex, 0f);
        }

        return true;
    }

    // Handle Try Force Completed Upper Body Action To Idle.
    private void TryForceCompletedUpperBodyActionToIdle(Animator animator, int layerIndex)
    {
        if (animator == null ||
            axeComboHoldActive ||
            upperBodyExternalHold ||
            Time.time < upperBodyLayerActiveUntil ||
            animator.IsInTransition(layerIndex))
        {
            return;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (!IsActionState(current))
        {
            return;
        }

        float completionThreshold = Mathf.Clamp01(upperBodyActionCompletionThreshold);
        if (current.normalizedTime < Mathf.Max(0.5f, completionThreshold))
        {
            return;
        }

        if (TryPlayUpperBodyIdle(animator, layerIndex, upperBodyStateBlendTime))
        {
            animator.SetLayerWeight(layerIndex, 0f);
        }
    }

    // Handle Try Play Upper Body Idle.
    private bool TryPlayUpperBodyIdle(Animator animator, int layerIndex, float blendTime)
    {
        if (animator == null || layerIndex < 0 || string.IsNullOrWhiteSpace(upperBodyIdleStateName))
        {
            return false;
        }

        int fullPathHash = Animator.StringToHash($"{upperBodyLayerName}.{upperBodyIdleStateName}");
        int shortNameHash = Animator.StringToHash(upperBodyIdleStateName);
        int stateHash;

        if (animator.HasState(layerIndex, fullPathHash))
        {
            stateHash = fullPathHash;
        }
        else if (animator.HasState(layerIndex, shortNameHash))
        {
            stateHash = shortNameHash;
        }
        else
        {
            return false;
        }

        float resolvedBlend = Mathf.Max(0f, blendTime);
        if (resolvedBlend > 0f)
        {
            animator.CrossFadeInFixedTime(stateHash, resolvedBlend, layerIndex);
        }
        else
        {
            animator.Play(stateHash, layerIndex, 0f);
        }

        return true;
    }

    // Handle Set Upper Body Chop Animation Speed.
    private void SetUpperBodyChopAnimationSpeed()
    {
        SetUpperBodyChopAnimationSpeed(null);
    }

    // Handle Set Upper Body Chop Animation Speed Override.
    private void SetUpperBodyChopAnimationSpeed(float? overrideSpeed)
    {
        Animator animator = ResolveCharacterAnimator();
        if (animator == null)
        {
            return;
        }

        float chopSpeed = overrideSpeed ?? ResolveChopAnimationSpeed();

        TrySetAnimatorFloatParameter(animator, upperBodyChopSpeedParameterName, chopSpeed);
    }

    // Handle Apply Configured Action Animation Speeds.
    private void ApplyConfiguredActionAnimationSpeeds()
    {
        Animator animator = ResolveCharacterAnimator();
        if (animator == null)
        {
            return;
        }

        TrySetAnimatorFloatParameter(animator, AttackLightSpeedParameterName, ResolveConfiguredSpeed(lightAttackAnimationSpeed));
        TrySetAnimatorFloatParameter(animator, AttackHeavySpeedParameterName, ResolveConfiguredSpeed(heavyAttackAnimationSpeed));
        TrySetAnimatorFloatParameter(animator, PunchLeftSpeedParameterName, ResolveConfiguredSpeed(punchLeftAnimationSpeed));
        TrySetAnimatorFloatParameter(animator, PunchRightSpeedParameterName, ResolveConfiguredSpeed(punchRightAnimationSpeed));
        TrySetAnimatorFloatParameter(animator, MineSpeedParameterName, ResolveConfiguredSpeed(mineAnimationSpeed));
        TrySetAnimatorFloatParameter(animator, GunAimSpeedParameterName, ResolveConfiguredSpeed(gunAimAnimationSpeed));
        TrySetAnimatorFloatParameter(animator, GunShootSpeedParameterName, ResolveConfiguredSpeed(gunShootAnimationSpeed));
        TrySetAnimatorFloatParameter(animator, GunReloadSpeedParameterName, ResolveConfiguredSpeed(gunReloadAnimationSpeed));
        SetUpperBodyChopAnimationSpeed(axeComboHoldActive ? 0f : (float?)null);
    }

    // Handle Try Continue Active Chop Combo.
    private bool TryContinueActiveChopCombo()
    {
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetUpperBodyLayerIndex(animator, out int layerIndex) ||
            !TryGetAnimatorStateInfo(animator, layerIndex, upperBodyChopStateName, out _))
        {
            return false;
        }

        axeComboContinueRequested = true;
        upperBodyLayerActiveUntil = Mathf.Max(
            upperBodyLayerActiveUntil,
            Time.time + Mathf.Max(upperBodyMinimumActiveSeconds, upperBodyStateBlendTime + 0.02f));

        if (axeComboHoldActive)
        {
            axeComboHoldActive = false;
            SetUpperBodyChopAnimationSpeed();
        }

        return true;
    }

    // Handle Update Axe Combo Hold State.
    private void UpdateAxeComboHoldState()
    {
        Animator animator = ResolveCharacterAnimator();
        if (!TryGetUpperBodyLayerIndex(animator, out int layerIndex) ||
            !TryGetAnimatorStateInfo(animator, layerIndex, upperBodyChopStateName, out AnimatorStateInfo chopState))
        {
            ResetAxeComboState();
            return;
        }

        if (axeComboContinueRequested)
        {
            if (axeComboHoldActive)
            {
                axeComboHoldActive = false;
            }

            SetUpperBodyChopAnimationSpeed();
            return;
        }

        if (chopState.normalizedTime >= 1f)
        {
            ResetAxeComboState();
            return;
        }

        float completionThreshold = Mathf.Clamp01(upperBodyActionCompletionThreshold);
        float holdThreshold = Mathf.Clamp(
            axeComboHoldNormalizedTime,
            0f,
            Mathf.Max(0f, completionThreshold - 0.01f));

        if (chopState.normalizedTime < holdThreshold)
        {
            if (!axeComboHoldActive)
            {
                SetUpperBodyChopAnimationSpeed();
            }

            return;
        }

        axeComboHoldActive = true;
        SetUpperBodyChopAnimationSpeed(0f);
    }

    // Handle Reset Axe Combo State.
    private void ResetAxeComboState()
    {
        if (!axeComboContinueRequested && !axeComboHoldActive)
        {
            return;
        }

        axeComboContinueRequested = false;
        axeComboHoldActive = false;
        SetUpperBodyChopAnimationSpeed();
    }

    // Handle Try Get Animator State Info.
    private bool TryGetAnimatorStateInfo(
        Animator animator,
        int layerIndex,
        string stateName,
        out AnimatorStateInfo stateInfo)
    {
        stateInfo = default;
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(layerIndex);
        if (MatchesStateName(current, stateName))
        {
            stateInfo = current;
            return true;
        }

        if (!animator.IsInTransition(layerIndex))
        {
            return false;
        }

        AnimatorStateInfo next = animator.GetNextAnimatorStateInfo(layerIndex);
        if (!MatchesStateName(next, stateName))
        {
            return false;
        }

        stateInfo = next;
        return true;
    }

    // Handle Matches State Name.
    private bool MatchesStateName(AnimatorStateInfo state, string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        return state.IsName(stateName) || state.IsName($"{upperBodyLayerName}.{stateName}");
    }

    // Handle Resolve Chop Animation Speed.
    private float ResolveChopAnimationSpeed()
    {
        if (axeAnimationScript != null)
        {
            return axeAnimationScript.GetResolvedSwingAnimationSpeed();
        }

        return 1f;
    }

    // Handle Resolve Configured Speed.
    private static float ResolveConfiguredSpeed(float value)
    {
        return value > 0f ? value : 1f;
    }

    // Handle Try Set Animator Float Parameter.
    private static bool TrySetAnimatorFloatParameter(Animator animator, string parameterName, float value)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.type != AnimatorControllerParameterType.Float ||
                !string.Equals(parameter.name, parameterName, System.StringComparison.Ordinal))
            {
                continue;
            }

            animator.SetFloat(parameterName, Mathf.Max(0.01f, value));
            return true;
        }

        return false;
    }

    // Handle Is Animator In Action State.
    private bool IsAnimatorInActionState()
    {
        if (movementAnimationScript != null && movementAnimationScript.IsBlockingActionState())
        {
            return true;
        }

        Animator animator = movementAnimationScript != null ? movementAnimationScript.animator : null;

        if (animator == null && swordAnimationScript != null)
        {
            animator = swordAnimationScript.animator;
        }

        if (animator == null || !animator.isActiveAndEnabled ||
            (movementAnimationScript != null && animator == movementAnimationScript.animator))
        {
            return false;
        }

        return IsAnimatorLayerInActionState(animator, 0);
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

