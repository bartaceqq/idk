using UnityEngine;
using Yarn.Unity;
public class StartCommunication : MonoBehaviour
{
    public GameObject player; public GameObject tree; public float Range = 10f;
    public KeyCode key = KeyCode.E; public Animator animator;
    [Header("Legacy (unused by Yarn)")] public VisualCommunication visualCommunication;
    [Header("Yarn Spinner")] public DialogueRunner dialogueRunner;
    public string yarnStartNode = "Start"; public bool stopDialogueWhenOutOfRange = true;
    public bool allowAdvanceWithInteractionKey = true; private bool _stopRequestedDueToRange;
    private static StartCommunication _activeInteraction;
    private const float DialogueStartupHoldSeconds = 0.5f;
    private float _talkingHoldUntil;
    private bool _dialogueStartRequested;
    private void Awake() { ResolveDialogueRunner(); }
    private void Update()
    {
        bool inRange = GetDistance() <= Range; if (!inRange) { HandleOutOfRange(); return; }
        _stopRequestedDueToRange = false;
        if (!GameplayUiState.IsMenuOpen && GameSettings.GetKeyDown(GameSettings.Key.Interact, key) && IsNearestAvailableNpc()) { HandleInteractPressed(); }
        bool isDialogueRunning = IsDialogueRunning();
        if (isDialogueRunning && (_activeInteraction == null || _activeInteraction == this))
        {
            _activeInteraction = this; _dialogueStartRequested = false;
        }
        bool shouldTalk = isDialogueRunning && _activeInteraction == this ||
        _activeInteraction == this && Time.unscaledTime < _talkingHoldUntil;
        SetTalkingAnimation(shouldTalk);
        if (shouldTalk) { FaceTarget(player); }
        if (_activeInteraction == this && !isDialogueRunning && Time.unscaledTime >= _talkingHoldUntil)
        {
            _activeInteraction = null; _dialogueStartRequested = false;
        }
    }
    private void HandleInteractPressed()
    {
        if (!ResolveDialogueRunner())
        {
            Debug.LogWarning("StartCommunication: No DialogueRunner found in scene.", this); return;
        }
        if (dialogueRunner.IsDialogueRunning)
        {
            if (_activeInteraction != null && _activeInteraction != this) { return; }
            _activeInteraction = this;
            if (allowAdvanceWithInteractionKey) { dialogueRunner.RequestNextLine(); }
            return;
        }
        if (_dialogueStartRequested) { return; }
        string nodeName = ResolveYarnNodeName(); FaceTarget(player); SetTalkingAnimation(true);
        _activeInteraction = this; _dialogueStartRequested = true;
        _talkingHoldUntil = Time.unscaledTime + DialogueStartupHoldSeconds;
        _ = dialogueRunner.StartDialogue(nodeName);
    }
    private string ResolveYarnNodeName()
    {
        string configuredNode = string.IsNullOrWhiteSpace(yarnStartNode) ? "Start" : yarnStartNode.Trim();
        string objectNode = NormalizeYarnNodeName(gameObject.name);
        if (!string.Equals(configuredNode, "Start", System.StringComparison.OrdinalIgnoreCase) ||
        string.IsNullOrEmpty(objectNode)) { return configuredNode; }
        return HasYarnNode(objectNode) ? objectNode : configuredNode;
    }
    private bool HasYarnNode(string nodeName)
    {
        if (dialogueRunner == null || dialogueRunner.YarnProject == null || string.IsNullOrWhiteSpace(nodeName)) { return false; }
        try
        {
            string[] nodeNames = dialogueRunner.YarnProject.NodeNames;
            for (int i = 0; i < nodeNames.Length; i++)
            {
                if (string.Equals(nodeNames[i], nodeName, System.StringComparison.Ordinal)) { return true; }
            }
            return false;
        }
        catch (System.Exception) { return false; }
    }
    private static string NormalizeYarnNodeName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) { return string.Empty; }
        string normalized = rawName.Trim();
        if (normalized.EndsWith("(Clone)", System.StringComparison.OrdinalIgnoreCase)) { normalized = normalized.Substring(0, normalized.Length - "(Clone)".Length).Trim(); }
        return normalized.Replace(" ", string.Empty);
    }
    private void HandleOutOfRange()
    {
        if (_activeInteraction == this && stopDialogueWhenOutOfRange && !_stopRequestedDueToRange && IsDialogueRunning() && dialogueRunner != null)
        {
            _stopRequestedDueToRange = true; _ = dialogueRunner.Stop();
        }
        if (_activeInteraction == this)
        {
            _activeInteraction = null; _dialogueStartRequested = false; _talkingHoldUntil = 0f;
        }
        SetTalkingAnimation(false);
        FaceTarget(tree);
    }
    private bool IsNearestAvailableNpc()
    {
        if (_activeInteraction != null && _activeInteraction != this) { return false; }
        StartCommunication[] allNpcs = UnitySceneSearch.FindAll<StartCommunication>(false);
        float myDistance = GetDistance();
        for (int i = 0; i < allNpcs.Length; i++)
        {
            StartCommunication other = allNpcs[i];
            if (other == null || other == this || !other.enabled || !other.gameObject.activeInHierarchy) { continue; }
            float otherDistance = other.GetDistance();
            if (otherDistance <= other.Range && otherDistance + 0.01f < myDistance) { return false; }
        }
        return true;
    }
    private bool ResolveDialogueRunner()
    {
        if (dialogueRunner == null) { DialogueState.TryGetDialogueRunner(out dialogueRunner); }
        if (dialogueRunner != null) { DialogueState.RegisterDialogueRunner(dialogueRunner); }
        return dialogueRunner != null;
    }
    private bool IsDialogueRunning() { return dialogueRunner != null && dialogueRunner.IsDialogueRunning; }
    private void SetTalkingAnimation(bool status)
    {
        if (animator != null) { animator.SetBool("Talking", status); }
    }
    private void FaceTarget(GameObject target)
    {
        if (target == null) { return; }
        transform.LookAt(target.transform); Vector3 rot = transform.eulerAngles; rot.x = 0f;
        transform.eulerAngles = rot;
    }
    private void OnDisable()
    {
        if (_activeInteraction == this) { _activeInteraction = null; }
        _dialogueStartRequested = false; _talkingHoldUntil = 0f; SetTalkingAnimation(false);
    }
    public float GetDistance()
    {
        if (player == null) { return float.MaxValue; }
        return Vector3.Distance(player.transform.position, gameObject.transform.position);
    }
}
