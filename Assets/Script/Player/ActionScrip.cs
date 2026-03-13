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
    public string upperBodyLayerName = "UpperBody";
    public float upperBodyLayerBlendSpeed = 18f;
    public float upperBodyMinimumActiveSeconds = 0.25f;
    public float upperBodyStateBlendTime = 0.02f;
    public string upperBodyLightAttackStateName = "UpperAttackWeapon";
    public string upperBodyHeavyAttackStateName = "UpperAttackTwoHanded";
    public string upperBodyPunchLeftStateName = "UpperPunchLeft";
    public string upperBodyPunchRightStateName = "UpperPunchRight";
    public string upperBodyMiningStateName = "UpperMining";
    public string upperBodyChopStateName = "UpperChop";
    public MovementAnimationScript movementAnimationScript;
    public AxeAnimationScript axeAnimationScript;
    public PickaxeAnimationScript pickaxeAnimationScript;
    public SwordAnimationScript swordAnimationScript;

    private float movementAnimationLockUntil;
    private float upperBodyLayerActiveUntil;
    private int unarmedPunchStep;
    private bool upperBodyExternalHold;

    private static readonly int AttackWeaponStateHash = Animator.StringToHash("AttackWeapon");
    private static readonly int AttackTwoHandedStateHash = Animator.StringToHash("AttackTwoHanded");
    private static readonly int PunchLeftStateHash = Animator.StringToHash("PunchLeft");
    private static readonly int PunchRightStateHash = Animator.StringToHash("PunchRight");
    private static readonly int MiningStateHash = Animator.StringToHash("Mining");
    private static readonly int ChopStateHash = Animator.StringToHash("Chop");
    private static readonly int JumpStateHash = Animator.StringToHash("Jump");

    private void Update()
    {
        UpdateUpperBodyLayerWeight();
    }

    // Handle Chop.
    public void Chop()
    {
        ActivateUpperBodyLayer(chopUpperBodySeconds);
        if (!TryPlayUpperBodyState(upperBodyChopStateName) && axeAnimationScript != null)
        {
            axeAnimationScript.ChopAnimation();
        }
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
        if (!TryPlayUpperBodyState(upperBodyLightAttackStateName) && swordAnimationScript != null)
        {
            swordAnimationScript.AttackLight();
        }
    }

    // Handle Attack Heavy.
    public void AttackHeavy()
    {
        ActivateUpperBodyLayer(swordHeavyMovementAnimationLockSeconds);
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

