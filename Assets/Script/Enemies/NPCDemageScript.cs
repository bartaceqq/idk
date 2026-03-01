using System.Collections;
using UnityEngine;

// Controls NPCDemage Script behavior.
public class NPCDemageScript : MonoBehaviour
{
    public Animator animator;
    public NPCHealthScript npcHealthScript;
    public Material demagemat;
    public Material origimat;
    public SkinnedMeshRenderer meshRenderer;
    public float flashSeconds = 0.5f;
    public float reactionLockSeconds = 0.6f;
    public float defaultDamage = 50f;
    public string damageStateName = "Damage";
    public string damageTriggerName = "Damage";

    private Renderer _targetRenderer;
    private Coroutine _flashRoutine;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (npcHealthScript == null)
        {
            npcHealthScript = GetComponent<NPCHealthScript>();
        }

        _targetRenderer = meshRenderer != null ? meshRenderer : GetComponentInChildren<Renderer>();
        if (origimat == null && _targetRenderer != null)
        {
            origimat = _targetRenderer.sharedMaterial;
        }
    }

    // Handle Take Demage.
    public void TakeDemage()
    {
        TakeDemage(defaultDamage);
    }

    // Handle Take Demage.
    public void TakeDemage(float damage)
    {
        if (npcHealthScript != null && npcHealthScript.IsDead)
        {
            return;
        }

        if (npcHealthScript != null)
        {
            npcHealthScript.TakeDemage(damage);
        }

        if (npcHealthScript != null && npcHealthScript.IsDead)
        {
            return;
        }

        PlayDamageReaction();
        LockEnemyActions();

        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
        }
        _flashRoutine = StartCoroutine(FlashDamageMaterial());
    }

    // Handle Play Damage Reaction.
    private void PlayDamageReaction()
    {
        if (animator == null) return;

        SetBoolIfExists("Walking", false);
        SetBoolIfExists("Move", false);
        ResetTriggerIfExists("Throw");
        ResetTriggerIfExists("Attack");

        int damageStateHash = Animator.StringToHash(damageStateName);
        if (!string.IsNullOrEmpty(damageStateName) && animator.HasState(0, damageStateHash))
        {
            // Force immediate transition to damage reaction, interrupting current animation.
            animator.Play(damageStateHash, 0, 0f);
            return;
        }

        if (!string.IsNullOrEmpty(damageTriggerName) && HasParameter(damageTriggerName, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(damageTriggerName);
            animator.SetTrigger(damageTriggerName);
        }
    }

    // Handle Lock Enemy Actions.
    private void LockEnemyActions()
    {
        float lockSeconds = Mathf.Max(0f, reactionLockSeconds);
        if (lockSeconds <= 0f) return;

        RandomZombieScript zombie = GetComponent<RandomZombieScript>();
        if (zombie == null)
        {
            zombie = GetComponentInParent<RandomZombieScript>();
        }
        if (zombie != null)
        {
            zombie.LockActions(lockSeconds);
        }

        RandomSkeletonScript skeleton = GetComponent<RandomSkeletonScript>();
        if (skeleton == null)
        {
            skeleton = GetComponentInParent<RandomSkeletonScript>();
        }
        if (skeleton != null)
        {
            skeleton.LockActions(lockSeconds);
        }
    }

    // Handle Set Bool If Exists.
    private void SetBoolIfExists(string parameterName, bool value)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(parameterName, value);
        }
    }

    // Handle Reset Trigger If Exists.
    private void ResetTriggerIfExists(string parameterName)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(parameterName);
        }
    }

    // Handle Has Parameter.
    private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == type && parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    // Handle Flash Damage Material.
    private IEnumerator FlashDamageMaterial()
    {
        if (_targetRenderer == null)
        {
            yield break;
        }

        Material restoreMaterial = origimat != null ? origimat : _targetRenderer.sharedMaterial;
        if (demagemat != null)
        {
            _targetRenderer.material = demagemat;
        }

        yield return new WaitForSeconds(flashSeconds);

        if (_targetRenderer != null && restoreMaterial != null)
        {
            _targetRenderer.material = restoreMaterial;
        }

        _flashRoutine = null;
    }
}
