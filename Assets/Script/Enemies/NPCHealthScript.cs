using UnityEngine;
using UnityEngine.AI;

// Controls NPCHealth Script behavior.
public class NPCHealthScript : MonoBehaviour
{
    public Animator animator;
    public float hp = 100f;
    public string deathStateName = "Death";
    public string deathTriggerName = "Death";

    private bool _isDead;
    public bool IsDead => _isDead;

    void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    // Handle Take Demage.
    public void TakeDemage(float damage)
    {
        if (_isDead || damage <= 0f)
        {
            return;
        }

        hp = Mathf.Max(0f, hp - damage);
        if (hp <= 0f)
        {
            Die();
        }
    }
    // Handle Die.
    public void Die()
    {
        if (_isDead)
        {
            return;
        }

        _isDead = true;
        PlayDeathAnimation();
        StopEnemyBehaviour();
    }

    // Handle Play Death Animation.
    private void PlayDeathAnimation()
    {
        if (animator == null)
        {
            return;
        }

        SetBoolIfExists("Walking", false);
        SetBoolIfExists("Move", false);
        ResetTriggerIfExists("Throw");
        ResetTriggerIfExists("Attack");
        ResetTriggerIfExists("Damage");

        int deathStateHash = Animator.StringToHash(deathStateName);
        if (!string.IsNullOrEmpty(deathStateName) && animator.HasState(0, deathStateHash))
        {
            animator.Play(deathStateHash, 0, 0f);
            return;
        }

        if (!string.IsNullOrEmpty(deathTriggerName) && HasParameter(deathTriggerName, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(deathTriggerName);
            return;
        }

        // Fallback for controllers that use a different naming convention.
        string[] fallbackStateNames = { "Dead", "Die", "DeathZombie", "DeathSkeleton" };
        foreach (string stateName in fallbackStateNames)
        {
            int stateHash = Animator.StringToHash(stateName);
            if (animator.HasState(0, stateHash))
            {
                animator.Play(stateHash, 0, 0f);
                return;
            }
        }

        Debug.LogWarning($"No death state/trigger found on animator '{animator.runtimeAnimatorController?.name}' for '{name}'.");
    }

    // Handle Stop Enemy Behaviour.
    private void StopEnemyBehaviour()
    {
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = GetComponentInParent<NavMeshAgent>();
        }
        if (agent != null && agent.enabled && agent.gameObject.activeInHierarchy)
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                if (agent.hasPath)
                {
                    agent.ResetPath();
                }
            }

            agent.enabled = false;
        }

        RandomZombieScript zombie = GetComponent<RandomZombieScript>();
        if (zombie == null)
        {
            zombie = GetComponentInParent<RandomZombieScript>();
        }
        if (zombie != null)
        {
            zombie.enabled = false;
        }

        RandomSkeletonScript skeleton = GetComponent<RandomSkeletonScript>();
        if (skeleton == null)
        {
            skeleton = GetComponentInParent<RandomSkeletonScript>();
        }
        if (skeleton != null)
        {
            skeleton.enabled = false;
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
}
