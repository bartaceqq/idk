using System.Collections.Generic;
using UnityEngine;

// Controls Axe Animation Script behavior.
public class AxeAnimationScript : MonoBehaviour
{
    public Animator axeanimator;
    public float swingAnimationSpeed = 1f;
    public float minimumSwingAnimationSpeed = 0.5f;
    public float swingRepeatDelaySeconds = 0.35f;
    public string swingSpeedParameterName = "SwingSpeed";

    private static readonly int SwingTriggerHash = Animator.StringToHash("Swing");
    private float _nextSwingAllowedTime;

    // Handle Chop Animation.
    public void ChopAnimation()
    {
        TryPlayChopAnimation();
    }

    // Handle Try Play Chop Animation.
    public bool TryPlayChopAnimation()
    {
        if (axeanimator == null || !axeanimator.isActiveAndEnabled)
        {
            return false;
        }

        if (Time.time < _nextSwingAllowedTime)
        {
            return false;
        }

        float resolvedSwingSpeed = GetResolvedSwingAnimationSpeed();
        if (!TrySetAnimatorFloatParameter(swingSpeedParameterName, resolvedSwingSpeed))
        {
            axeanimator.speed = resolvedSwingSpeed;
        }

        axeanimator.ResetTrigger(SwingTriggerHash);
        axeanimator.SetTrigger(SwingTriggerHash);
        _nextSwingAllowedTime = Time.time + Mathf.Max(0.01f, swingRepeatDelaySeconds);
        return true;
    }

    // Handle Get Resolved Swing Animation Speed.
    public float GetResolvedSwingAnimationSpeed()
    {
        return Mathf.Max(minimumSwingAnimationSpeed, swingAnimationSpeed);
    }

    // Handle Try Set Animator Float Parameter.
    private bool TrySetAnimatorFloatParameter(string parameterName, float value)
    {
        if (axeanimator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = axeanimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = parameters[i];
            if (parameter.type != AnimatorControllerParameterType.Float ||
                !string.Equals(parameter.name, parameterName, System.StringComparison.Ordinal))
            {
                continue;
            }

            axeanimator.SetFloat(parameterName, value);
            return true;
        }

        return false;
    }
}

