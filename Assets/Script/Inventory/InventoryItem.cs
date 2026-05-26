using UnityEngine;

public enum InventoryItemType {
    Usable = 0,
    Tool = 1,
    Sword = 2,
    Building = 3 }
public class InventoryItem : MonoBehaviour {
    private static SlotManager cachedSlotManager;

    public Sprite inventorysprite;
    public string nameofitem;
    public InventoryItemType itemType = InventoryItemType.Usable;
    public GameObject itemPrefab;
    [Header("Build Placement")] public Vector3 buildRotationEuler = Vector3.zero;
    public Vector3 buildScale = Vector3.one;
    public int mingain = 1;
    public int maxgain = 1;
    public SlotManager slotManager;

    // Initialize references before gameplay starts.
    private void Awake() { ResolveReferences(); }

    // Run in the editor when values change in Inspector.
    private void OnValidate() {
        if (!Application.isPlaying) {
            ResolveReferences();
            ValidateItemSetup(true); } }
    public void ResolveReferences() {
        ValidateItemSetup(false);

        if (slotManager != null) {
            cachedSlotManager = slotManager;
            return; }

        if (cachedSlotManager == null) { cachedSlotManager = FindSlotManagerInScene(); }

        slotManager = cachedSlotManager; }
    private static SlotManager FindSlotManagerInScene() {
        return UnitySceneSearch.FindFirst<SlotManager>();
    }
    public bool RequiresPrefab() {
        return itemType == InventoryItemType.Tool ||
               itemType == InventoryItemType.Sword ||
               itemType == InventoryItemType.Building; }
    public bool HasRequiredPrefab() { return !RequiresPrefab() || itemPrefab != null; }
    private void ValidateItemSetup(bool logWarning) {
        if (mingain <= 0) { mingain = 1; }

        if (maxgain <= 0) { maxgain = mingain; }

        if (maxgain < mingain) { maxgain = mingain; }

        if (!logWarning || HasRequiredPrefab()) {
            ValidateBuildScale(); } else {
            Debug.LogWarning($"{name}: Item type {itemType} requires itemPrefab.", this);
            ValidateBuildScale(); }

        ValidateSwordSetup(logWarning); }
    private void ValidateBuildScale() {
        buildScale.x = ValidateScaleAxis(buildScale.x);
        buildScale.y = ValidateScaleAxis(buildScale.y);
        buildScale.z = ValidateScaleAxis(buildScale.z); }
    private void ValidateSwordSetup(bool logWarning) {
        if (!logWarning || itemType != InventoryItemType.Sword || itemPrefab == null) { return; }

        if (itemPrefab.GetComponent<Sword>() == null) { Debug.LogWarning($"{name}: Sword items should use an itemPrefab with a Sword component.", this); } }
    private static float ValidateScaleAxis(float axisValue) {
        if (Mathf.Abs(axisValue) < 0.0001f) { return 1f; }

        return axisValue; } }
