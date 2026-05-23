using UnityEngine;
using Yarn.Unity;

public class StartCommunication : MonoBehaviour
{
    public GameObject player;
    public GameObject tree;
    public float Range = 10f;
    public KeyCode key = KeyCode.E;
    public Animator animator;
    [Header("Legacy (unused by Yarn)")]
    public VisualCommunication visualCommunication;

    [Header("Yarn Spinner")]
    public DialogueRunner dialogueRunner;
    public string yarnStartNode = "Start";
    public bool stopDialogueWhenOutOfRange = true;
    public bool allowAdvanceWithInteractionKey = true;

    private bool _stopRequestedDueToRange;

    private void Awake()
    {
        ResolveDialogueRunner();
    }

    private void Update()
    {
        bool inRange = GetDistance() <= Range;
        if (!inRange)
        {
            HandleOutOfRange();
            return;
        }

        _stopRequestedDueToRange = false;

        if (!GameplayUiState.IsMenuOpen && GameSettings.GetKeyDown(GameSettings.Key.Interact, key))
        {
            HandleInteractPressed();
        }

        bool isDialogueRunning = IsDialogueRunning();
        SetTalkingAnimation(isDialogueRunning);

        if (isDialogueRunning)
        {
            FaceTarget(player);
        }
    }

    private void HandleInteractPressed()
    {
        if (!ResolveDialogueRunner())
        {
            Debug.LogWarning("StartCommunication: No DialogueRunner found in scene.", this);
            return;
        }

        if (dialogueRunner.IsDialogueRunning)
        {
            if (allowAdvanceWithInteractionKey)
            {
                dialogueRunner.RequestNextLine();
            }
            return;
        }

        string nodeName = string.IsNullOrWhiteSpace(yarnStartNode) ? "Start" : yarnStartNode.Trim();
        FaceTarget(player);
        SetTalkingAnimation(true);
        _ = dialogueRunner.StartDialogue(nodeName);
    }

    private void HandleOutOfRange()
    {
        if (stopDialogueWhenOutOfRange && !_stopRequestedDueToRange && IsDialogueRunning() && dialogueRunner != null)
        {
            _stopRequestedDueToRange = true;
            _ = dialogueRunner.Stop();
        }

        SetTalkingAnimation(false);
        FaceTarget(tree);
    }

    private bool ResolveDialogueRunner()
    {
        if (dialogueRunner == null)
        {
            DialogueState.TryGetDialogueRunner(out dialogueRunner);
        }

        if (dialogueRunner != null)
        {
            DialogueState.RegisterDialogueRunner(dialogueRunner);
        }

        return dialogueRunner != null;
    }

    private bool IsDialogueRunning()
    {
        return dialogueRunner != null && dialogueRunner.IsDialogueRunning;
    }

    private void SetTalkingAnimation(bool status)
    {
        if (animator != null)
        {
            animator.SetBool("Talking", status);
        }
    }

    private void FaceTarget(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        transform.LookAt(target.transform);
        Vector3 rot = transform.eulerAngles;
        rot.x = 0f;
        transform.eulerAngles = rot;
    }

    private void OnDisable()
    {
        SetTalkingAnimation(false);
    }

    public float GetDistance()
    {
        if (player == null)
        {
            return float.MaxValue;
        }

        return Vector3.Distance(player.transform.position, gameObject.transform.position);
    }
}
