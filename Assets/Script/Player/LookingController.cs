using UnityEngine;

public class LookingController : MonoBehaviour
{
    public Animator animator;
    public GameObject normalcapsule;
    public Transform position;
    public Transform normalLookTransform;

    private void Start()
    {
        SwitchToNormalMode();
    }

    private void Update()
    {
        if (GameplayUiState.IsGameplayInputBlocked)
        {
            GameplayUiState.ApplyCursorState();
        }
    }

    public void Switch()
    {
        SwitchToNormalMode();
    }

    public void SwitchToNormalMode()
    {
        if (normalcapsule != null)
        {
            normalcapsule.SetActive(true);
        }

        if (animator != null)
        {
            animator.enabled = true;
        }

        GameplayUiState.ApplyCursorState();
    }
}
