using UnityEngine;

// Controls Sword Animation Script behavior.
public class SwordAnimationScript : MonoBehaviour
{
    public Animator animator;
    public string lightAttackTrigger = "Attack";
    public string heavyAttackTrigger = "AttackHeavy";
    public string punchLeftTrigger = "PunchLeft";
    public string punchRightTrigger = "PunchRight";

    // Handle Attack.
    public void Attack()
    {
        AttackLight();
    }

    // Handle Attack Light.
    public void AttackLight()
    {
        SetTrigger(lightAttackTrigger);
    }

    // Handle Attack Heavy.
    public void AttackHeavy()
    {
        SetTrigger(heavyAttackTrigger);
    }

    // Handle Punch Left.
    public void PunchLeft()
    {
        SetTrigger(punchLeftTrigger);
    }

    // Handle Punch Right.
    public void PunchRight()
    {
        SetTrigger(punchRightTrigger);
    }

    // Handle Set Trigger.
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

    // Handle Has Trigger.
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
