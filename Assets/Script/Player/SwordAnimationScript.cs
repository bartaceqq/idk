using UnityEngine;

public class SwordAnimationScript : MonoBehaviour
{
    public Animator animator;
    public string lightAttackTrigger = "Attack";
    public string heavyAttackTrigger = "AttackHeavy";
    public string punchLeftTrigger = "PunchLeft";
    public string punchRightTrigger = "PunchRight";

    public void Attack()
    {
        AttackLight();
    }

    public void AttackLight()
    {
        SetTrigger(lightAttackTrigger);
    }

    public void AttackHeavy()
    {
        SetTrigger(heavyAttackTrigger);
    }

    public void PunchLeft()
    {
        SetTrigger(punchLeftTrigger);
    }

    public void PunchRight()
    {
        SetTrigger(punchRightTrigger);
    }

    private void SetTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(triggerName))
        {
            return;
        }

        if (!HasTrigger(triggerName))
        {
            return;
        }

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
    }

    private bool HasTrigger(string triggerName)
    {
        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == AnimatorControllerParameterType.Trigger &&
                string.Equals(parameters[i].name, triggerName, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
