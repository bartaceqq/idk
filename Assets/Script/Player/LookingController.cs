using UnityEngine;

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

    private void Update()
    {
        if (Input.GetKeyDown(keycode))
        {
            Switch();
        }
    }

    public void Switch()
    {
        if (normalcapsule == null || buildingcapsule == null)
        {
            Debug.LogWarning("LookingController: assign normalcapsule and buildingcapsule.");
            return;
        }

        GameObject sourceCapsule = switched ? buildingcapsule : normalcapsule;
        GameObject targetCapsule = switched ? normalcapsule : buildingcapsule;
        Transform sourceLook = ResolveLookTransform(sourceCapsule, switched ? buildingLookTransform : normalLookTransform);
        Transform targetLook = ResolveLookTransform(targetCapsule, switched ? normalLookTransform : buildingLookTransform);

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

        if (switched)
        {
            normalcapsule.SetActive(true);
            buildingcapsule.SetActive(false);
            switched = false;
            if (animator != null) animator.enabled = true;
        }
        else
        {
            normalcapsule.SetActive(false);
            buildingcapsule.SetActive(true);
            switched = true;
            if (animator != null) animator.enabled = false;
        }

        if (targetLook != null)
        {
            targetLook.rotation = sharedLookRotation;
        }
    }

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
}
