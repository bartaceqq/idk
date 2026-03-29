using UnityEngine;

// Applies animator root motion to the parent player controller while combat actions are active.
[DisallowMultipleComponent]
public class PlayerRootMotionDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private ActionScript actionScript;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Transform playerRoot;
    [SerializeField] private bool applyRotation = true;
    [SerializeField] private bool applyVerticalRootMotion;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void OnAnimatorMove()
    {
        if (!ResolveReferences() ||
            animator == null ||
            !animator.applyRootMotion ||
            actionScript == null ||
            !actionScript.ShouldConsumeAnimatorRootMotion())
        {
            return;
        }

        Transform targetRoot = playerRoot != null ? playerRoot : transform.root;
        if (targetRoot == null)
        {
            return;
        }

        Vector3 deltaPosition = animator.deltaPosition;

        if (!applyVerticalRootMotion)
        {
            deltaPosition.y = 0f;
        }

        if (!IsNearlyZero(deltaPosition))
        {
            if (characterController != null)
            {
                characterController.Move(deltaPosition);
            }
            else
            {
                targetRoot.position += deltaPosition;
            }
        }

        if (!applyRotation)
        {
            return;
        }

        float deltaYaw = Mathf.DeltaAngle(0f, animator.deltaRotation.eulerAngles.y);
        if (Mathf.Abs(deltaYaw) > 0.001f)
        {
            targetRoot.Rotate(0f, deltaYaw, 0f, Space.World);
        }
    }

    // Handle Resolve References.
    private bool ResolveReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (actionScript == null)
        {
            actionScript = GetComponentInParent<ActionScript>();
        }

        if (characterController == null)
        {
            characterController = GetComponentInParent<CharacterController>();
        }

        if (actionScript == null && characterController != null)
        {
            FPSController fpsController = characterController.GetComponent<FPSController>();
            if (fpsController != null)
            {
                actionScript = fpsController.actionScript;
            }
        }

        if (playerRoot == null)
        {
            playerRoot = characterController != null
                ? characterController.transform
                : transform.parent != null
                    ? transform.parent
                    : transform.root;
        }

        return animator != null && actionScript != null;
    }

    // Handle Is Nearly Zero.
    private static bool IsNearlyZero(Vector3 vector)
    {
        return vector.sqrMagnitude <= 0.000001f;
    }
}
