using UnityEngine;
using InputSystemPlayerInput = UnityEngine.InputSystem.PlayerInput;

// Controls Looking Controller behavior.
public class LookingController : MonoBehaviour
{
    public KeyCode keycode = KeyCode.B;
    public bool switched = false;
    public Animator animator;
    public GameObject normalcapsule;
    public GameObject buildingcapsule;
    public Transform position;
    public Transform normalLookTransform;
    public Transform buildingLookTransform;

    // Run setup once before the first frame.
    private void Start()
    {
        SwitchToNormalMode();
    }

    // Run this logic every frame.
    private void Update()
    {
        GameplayUiState.ApplyCursorState();

        // Build mode is removed: always keep normal gameplay capsule active.
        if (switched || (buildingcapsule != null && buildingcapsule.activeSelf))
        {
            SwitchToNormalMode();
        }
    }

    // Handle Switch.
    public void Switch()
    {
        SwitchToNormalMode();
    }

    // Handle Switch To Building Mode.
    public void SwitchToBuildingMode()
    {
        SwitchToNormalMode();
    }

    // Handle Switch To Normal Mode.
    public void SwitchToNormalMode()
    {
        GameObject activeCapsule = ResolveActiveCapsule();
        Vector3 sharedPosition = activeCapsule != null ? activeCapsule.transform.position : transform.position;
        Quaternion sharedRotation = activeCapsule != null ? activeCapsule.transform.rotation : transform.rotation;
        Quaternion sharedLookRotation = ResolveLookRotation(activeCapsule, sharedRotation);

        if (position != null)
        {
            position.SetPositionAndRotation(sharedPosition, sharedRotation);
        }

        if (normalcapsule != null)
        {
            normalcapsule.transform.SetPositionAndRotation(sharedPosition, sharedRotation);
            Transform normalLook = ResolveLookTransform(normalcapsule, normalLookTransform);
            if (normalLook != null)
            {
                normalLook.rotation = sharedLookRotation;
            }

            normalcapsule.SetActive(true);
            ActivatePrimaryPlayerInput(normalcapsule);
        }

        if (buildingcapsule != null)
        {
            buildingcapsule.transform.SetPositionAndRotation(sharedPosition, sharedRotation);
            Transform buildingLook = ResolveLookTransform(buildingcapsule, buildingLookTransform);
            if (buildingLook != null)
            {
                buildingLook.rotation = sharedLookRotation;
            }

            buildingcapsule.SetActive(false);
        }

        if (animator != null)
        {
            animator.enabled = true;
        }

        switched = false;
    }

    // Handle Resolve Active Capsule.
    private GameObject ResolveActiveCapsule()
    {
        if (normalcapsule != null && normalcapsule.activeInHierarchy)
        {
            return normalcapsule;
        }

        if (buildingcapsule != null && buildingcapsule.activeInHierarchy)
        {
            return buildingcapsule;
        }

        if (normalcapsule != null)
        {
            return normalcapsule;
        }

        return buildingcapsule;
    }

    // Handle Resolve Look Rotation.
    private Quaternion ResolveLookRotation(GameObject sourceCapsule, Quaternion fallbackRotation)
    {
        Transform sourceLook = ResolveLookTransform(sourceCapsule, sourceCapsule == normalcapsule ? normalLookTransform : buildingLookTransform);
        if (sourceLook != null)
        {
            return sourceLook.rotation;
        }

        return fallbackRotation;
    }

    // Handle Resolve Look Transform.
    private static Transform ResolveLookTransform(GameObject capsule, Transform explicitLookTransform)
    {
        if (explicitLookTransform != null)
        {
            return explicitLookTransform;
        }

        if (capsule == null)
        {
            return null;
        }

        Camera cameraInCapsule = capsule.GetComponentInChildren<Camera>(true);
        if (cameraInCapsule != null)
        {
            return cameraInCapsule.transform;
        }

        return capsule.transform;
    }

    // Handle Activate Primary Player Input.
    private static void ActivatePrimaryPlayerInput(GameObject capsule)
    {
        if (capsule == null)
        {
            return;
        }

        InputSystemPlayerInput playerInput = capsule.GetComponent<InputSystemPlayerInput>();
        if (playerInput != null)
        {
            playerInput.ActivateInput();
        }
    }
}
