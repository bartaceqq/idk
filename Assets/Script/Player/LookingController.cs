using UnityEngine;
using UnityEngine.InputSystem;

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
        SyncSwitchStateFromActiveCapsule();
    }

    // Run this logic every frame.
    private void Update()
    {
        if (Input.GetKeyDown(keycode))
        {
            Switch();
        }
    }

    // Handle Switch.
    public void Switch()
    {
        if (normalcapsule == null || buildingcapsule == null)
        {
            Debug.LogWarning("LookingController: assign normalcapsule and buildingcapsule.");
            return;
        }

        SyncSwitchStateFromActiveCapsule();

        bool switchToBuilding = !switched;
        GameObject sourceCapsule = switchToBuilding ? normalcapsule : buildingcapsule;
        GameObject targetCapsule = switchToBuilding ? buildingcapsule : normalcapsule;
        Transform sourceLook = ResolveLookTransform(sourceCapsule, switchToBuilding ? normalLookTransform : buildingLookTransform);
        Transform targetLook = ResolveLookTransform(targetCapsule, switchToBuilding ? buildingLookTransform : normalLookTransform);

        Vector3 sharedPosition = sourceCapsule.transform.position;
        Quaternion sharedRotation = sourceCapsule.transform.rotation;
        Quaternion sharedLookRotation = sourceLook != null ? sourceLook.rotation : sharedRotation;

        if (position != null)
        {
            position.position = sharedPosition;
            position.rotation = sharedRotation;
        }

        targetCapsule.transform.position = sharedPosition;
        targetCapsule.transform.rotation = sharedRotation;
        if (targetLook != null)
        {
            targetLook.rotation = sharedLookRotation;
        }

        // Disable source first so its OnDisable runs before target OnEnable.
        sourceCapsule.SetActive(false);
        targetCapsule.SetActive(true);
        switched = switchToBuilding;

        if (switched)
        {
            if (animator != null) animator.enabled = false;
        }
        else
        {
            if (animator != null) animator.enabled = true;
        }

        ActivatePrimaryPlayerInput(targetCapsule);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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

    // Handle Sync Switch State From Active Capsule.
    private void SyncSwitchStateFromActiveCapsule()
    {
        bool normalActive = normalcapsule != null && normalcapsule.activeInHierarchy;
        bool buildingActive = buildingcapsule != null && buildingcapsule.activeInHierarchy;

        if (normalActive && !buildingActive)
        {
            switched = false;
            return;
        }

        if (buildingActive && !normalActive)
        {
            switched = true;
        }
    }

    // Handle Activate Primary Player Input.
    private static void ActivatePrimaryPlayerInput(GameObject capsule)
    {
        if (capsule == null)
        {
            return;
        }

        PlayerInput playerInput = capsule.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.ActivateInput();
        }
    }
}
