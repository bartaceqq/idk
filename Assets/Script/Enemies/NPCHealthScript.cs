using UnityEngine;
public class NPCHealthScript : MonoBehaviour
{
    public Animator animator; public float hp = 100f; public string deathStateName = "Death";
    public string deathTriggerName = "Death"; private bool _isDead;
    public bool IsDead => _isDead; void Awake()
    {
        if (animator == null) { animator = GetComponentInChildren<Animator>(); }
    }
    public void TakeDemage(float damage)
    {
        if (_isDead || damage <= 0f) { return; }
        hp = Mathf.Max(0f, hp - damage); if (hp <= 0f) { Die(); }
    }
    public void Die()
    {
        if (_isDead) { return; }
        _isDead = true; XPRewards.GrantMonsterKilledXP(this); PlayDeathAnimation(); StopEnemyBehaviour();
    }
    private void PlayDeathAnimation()
    {
        if (animator == null) { return; }
        SetBoolIfExists("Walking", false); SetBoolIfExists("Move", false);
        ResetTriggerIfExists("Throw"); ResetTriggerIfExists("Attack");
        ResetTriggerIfExists("Damage");
        int deathStateHash = Animator.StringToHash(deathStateName);
        if (!string.IsNullOrEmpty(deathStateName) && animator.HasState(0, deathStateHash))
        {
            animator.Play(deathStateHash, 0, 0f); return;
        }
        if (!string.IsNullOrEmpty(deathTriggerName) && HasParameter(deathTriggerName, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(deathTriggerName); return;
        }
        string[] fallbackStateNames = { "Dead", "Die", "DeathZombie", "DeathSkeleton" };
        foreach (string stateName in fallbackStateNames)
        {
            int stateHash = Animator.StringToHash(stateName); if (animator.HasState(0, stateHash))
            {
                animator.Play(stateHash, 0, 0f); return;
            }
        }
        Debug.LogWarning($"No death state/trigger found on animator '{animator.runtimeAnimatorController?.name}' for '{name}'.");
    }
    private void StopEnemyBehaviour()
    {
        CustomEnemyAIBase enemyAi = GetComponent<CustomEnemyAIBase>();
        if (enemyAi == null) { enemyAi = GetComponentInParent<CustomEnemyAIBase>(); }
        if (enemyAi != null) { enemyAi.enabled = false; }
    }
    private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName)) { return false; }
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == type && parameter.name == parameterName) { return true; }
        }
        return false;
    }
    private void SetBoolIfExists(string parameterName, bool value)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Bool)) { animator.SetBool(parameterName, value); }
    }
    private void ResetTriggerIfExists(string parameterName)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Trigger)) { animator.ResetTrigger(parameterName); }
    }
}
