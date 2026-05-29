using System; using System.Collections; using System.Collections.Generic; using TMPro; using UnityEngine; using UnityEngine.UI; using UnityEngine.Events;

public class CraftingProcessHandler : MonoBehaviour {
    public CraftableItem craftableItem;
    public CraftingManager craftingManager;
    public InventoryListHandler inventoryListHandler;
    public SlotManager slotManager;
    public InventoryManager inventoryManager;
    public Button button;
    public bool hasenough;

    [Header("Craft Timing")]
    [SerializeField, Min(0f)] private float craftDurationSeconds = 4f;
    [SerializeField] private Slider craftProgressSlider;
    [SerializeField] private TextMeshProUGUI craftButtonLabel;
    [SerializeField] private bool showCraftPercentOnButton = true;
    [SerializeField] private string craftingLabelPrefix = "CRAFTING";

    [Header("Craft Click Guard")]
    [SerializeField, Min(0f)] private float craftClickCooldownSeconds = 0.2f;

    private float _craftButtonCooldownUntil;
    private bool _craftInProgress;
    private Coroutine _craftRoutine;
    private string _defaultCraftButtonLabel = "CRAFT";
    private bool _hasCachedDefaultCraftButtonLabel;
    void Start() {
        ResolveReferences();
        ResolveCraftUiReferences();
        EnsureCraftProgressSliderExists();
        BindCraftButtonIfNeeded();
        AutoSelectCraftableItem();
        UpdateSelectedSlotVisuals();
        ResetCraftProgressVisuals();
        RefreshCraftAvailability(); }
    void Update() {
        ResolveReferences();
        ResolveCraftUiReferences();
        AutoSelectCraftableItem();
        UpdateSelectedSlotVisuals();
        RefreshCraftAvailability(); }
    public void SelectCraftableItem(CraftableItem selectedCraftableItem) {
        craftableItem = selectedCraftableItem;
        UpdateSelectedSlotVisuals();
        RefreshCraftAvailability(); }

    public void Craft() { if (IsCraftInteractionLocked()) { return; }

        ResolveReferences();
        ResolveCraftUiReferences();
        EnsureCraftProgressSliderExists();

        if (!TryBuildRequirementMap(out Dictionary<string, int> requiredResources)) {
            hasenough = false;
            OnCraftMissingResources();
            return; }

        bool hasResources = HasEnoughResources(requiredResources);
        if (!hasResources) {
            hasenough = false;
            OnCraftMissingResources();
            return; }

        if (!TryResolveCraftResult(out InventoryItem craftedItem, out int craftedAmount, true)) {
            hasenough = false;
            OnCraftMissingResources();
            return; }

        if (!CanReceiveCraftedItem()) {
            hasenough = false;
            OnCraftMissingResources();
            return; }

        bool consumedResources;
        if (inventoryManager != null) {
            consumedResources = TryConsumeResourcesFromInventoryManager(requiredResources); } else if (slotManager != null) { consumedResources = slotManager.TryConsumeResources(requiredResources); } else { consumedResources = false; }

        if (!consumedResources) {
            hasenough = false;
            OnCraftMissingResources();
            return; }

        _craftInProgress = true;
        _craftButtonCooldownUntil = Time.unscaledTime + Mathf.Max(0f, craftClickCooldownSeconds);
        UpdateCraftButtonInteractable(false);

        if (_craftRoutine != null) { StopCoroutine(_craftRoutine); }

        _craftRoutine = StartCoroutine(CraftAfterDelay(craftedItem, craftedAmount)); }
    private void RefreshCraftAvailability() {
        UpdateSelectedSlotVisuals();

        if (!TryBuildRequirementMap(out Dictionary<string, int> requiredResources)) {
            hasenough = false;
            OnCraftMissingResources();
            return; }

        bool hasResources = HasEnoughResources(requiredResources);
        bool canReceiveCraftedItem = CanReceiveCraftedItem();
        hasenough = hasResources && canReceiveCraftedItem;
        if (hasenough) { OnCraftHasEnoughResources(); } else { OnCraftMissingResources(); } }
    private void UpdateSelectedSlotVisuals() { if (craftingManager == null || craftingManager.slots == null) { return; }

        for (int i = 0; i < craftingManager.slots.Count; i++) {
            CraftableSlot slot = craftingManager.slots[i];
            if (slot == null) { continue; }

            bool isSelected = craftableItem != null && slot.craftableItemReference == craftableItem;
            slot.SetSelectedVisual(isSelected); } }
    private bool HasEnoughResources(Dictionary<string, int> requiredResources) { if (requiredResources == null || requiredResources.Count == 0) { return true; }

        Dictionary<string, int> availableByName = BuildAvailableByNameFromInventoryManagerSlots();
        if (availableByName.Count == 0) { availableByName = BuildAvailableByNameFromSlots(); }
        if (availableByName.Count == 0 && inventoryListHandler != null) {
            Dictionary<InventoryItem, int> list = inventoryListHandler.itemlist;
            availableByName = BuildAvailableByName(list); }

        if (availableByName.Count == 0) { return false; }

        foreach (KeyValuePair<string, int> required in requiredResources) { if (!availableByName.TryGetValue(required.Key, out int availableAmount) || availableAmount < required.Value) { return false; } }

        return true; }
    private bool CanReceiveCraftedItem() { if (!TryResolveCraftResult(out InventoryItem craftedItem, out int craftedAmount, false)) { return false; }

        if (craftedAmount <= 0) { return false; }

        if (inventoryManager != null && inventoryManager.slotlist != null) {
            for (int i = 0; i < inventoryManager.slotlist.Count; i++) {
                SlotInsideUI slot = inventoryManager.slotlist[i];
                if (slot != null && !slot.occupied) { return true; } }

            return false; }

        return slotManager != null && slotManager.CanAddItem(craftedItem); }
    private bool TryResolveCraftResult(out InventoryItem craftedItem, out int craftedAmount, bool logWarnings) {
        craftedItem = null;
        craftedAmount = 0;

        if (craftableItem == null) { if (logWarnings) { Debug.LogWarning("CraftingProcessHandler: No craftable item selected.", this); }

            return false; }

        craftedAmount = Mathf.Max(1, craftableItem.craftAmount);
        if (!craftableItem.TryResolveCraftedInventoryItem(slotManager, out craftedItem, out string reason)) { if (logWarnings) { Debug.LogWarning($"CraftingProcessHandler: {reason}", this); }

            return false; }

        return craftedItem != null; }
    private Dictionary<string, int> BuildAvailableByNameFromSlots() {
        Dictionary<string, int> availableByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (slotManager == null || slotManager.slots == null) { return availableByName; }

        for (int i = 0; i < slotManager.slots.Count; i++) {
            Slot slot = slotManager.slots[i];
            if (slot == null || slot.IsEmpty() || string.IsNullOrWhiteSpace(slot.itemName)) { continue; }

            string key = slot.itemName.Trim();
            int amount = Mathf.Max(0, slot.count);
            if (amount <= 0) { continue; }

            if (availableByName.TryGetValue(key, out int currentAmount)) { availableByName[key] = currentAmount + amount; } else { availableByName[key] = amount; } }

        return availableByName; }
    private Dictionary<string, int> BuildAvailableByNameFromInventoryManagerSlots() {
        Dictionary<string, int> availableByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (inventoryManager == null || inventoryManager.slotlist == null) { return availableByName; }

        for (int i = 0; i < inventoryManager.slotlist.Count; i++) {
            SlotInsideUI slot = inventoryManager.slotlist[i];
            if (slot == null || !slot.occupied) { continue; }

            int amount = Mathf.Max(0, slot.count);
            if (amount <= 0) { continue; }

            string key = GetBestSlotName(slot);
            if (string.IsNullOrWhiteSpace(key)) { continue; }

            if (availableByName.TryGetValue(key, out int currentAmount)) { availableByName[key] = currentAmount + amount; } else { availableByName[key] = amount; } }

        return availableByName; }
    private bool TryBuildRequirementMap(out Dictionary<string, int> requiredResources) {
        requiredResources = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (craftableItem == null) { return false; }

        List<string> neededResources = craftableItem.neededResources;
        if (neededResources == null || neededResources.Count == 0) { return true; }

        for (int i = 0; i < neededResources.Count; i++) {
            string neededItem = neededResources[i];
            if (!TryParseRequirement(neededItem, out string neededName, out int neededCount)) { return false; }

            if (requiredResources.TryGetValue(neededName, out int currentRequiredAmount)) { requiredResources[neededName] = currentRequiredAmount + neededCount; } else { requiredResources[neededName] = neededCount; } }

        return true; }
    private static Dictionary<string, int> BuildAvailableByName(Dictionary<InventoryItem, int> list) {
        Dictionary<string, int> availableByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (list == null) { return availableByName; }

        foreach (KeyValuePair<InventoryItem, int> pair in list) { if (pair.Key == null || pair.Value <= 0) { continue; }

            string key = !string.IsNullOrWhiteSpace(pair.Key.nameofitem)
                ? pair.Key.nameofitem.Trim()
                : pair.Key.name.Trim();
            if (string.IsNullOrWhiteSpace(key)) { continue; }

            if (availableByName.TryGetValue(key, out int currentAmount)) { availableByName[key] = currentAmount + pair.Value; } else { availableByName[key] = pair.Value; } }

        return availableByName; }
    private void ResolveReferences() {
        if (craftingManager == null) {
            craftingManager = GetComponentInParent<CraftingManager>();
            if (craftingManager == null) { craftingManager = FindFirstInScene<CraftingManager>(); } }

        if (slotManager == null) { slotManager = FindSlotManagerForInventoryList(inventoryListHandler); }

        if (slotManager == null) { slotManager = FindFirstInScene<SlotManager>(); }

        if (slotManager != null &&
            inventoryListHandler != null &&
            slotManager.inventoryListHandler != null &&
            slotManager.inventoryListHandler != inventoryListHandler) {
            SlotManager matchedSlotManager = FindSlotManagerForInventoryList(inventoryListHandler);
            if (matchedSlotManager != null) { slotManager = matchedSlotManager; } }

        if (inventoryListHandler == null && slotManager != null) { inventoryListHandler = slotManager.inventoryListHandler; }

        if (inventoryListHandler == null) { inventoryListHandler = FindFirstInScene<InventoryListHandler>(); }

        if (slotManager != null && slotManager.inventoryListHandler == null && inventoryListHandler != null) { slotManager.inventoryListHandler = inventoryListHandler; }

        if (inventoryManager == null) { inventoryManager = FindFirstInScene<InventoryManager>(); } }
    private void ResolveCraftUiReferences() { if (button == null) { return; }

        if (craftButtonLabel == null) { craftButtonLabel = button.GetComponentInChildren<TextMeshProUGUI>(true); }

        if (!_hasCachedDefaultCraftButtonLabel && craftButtonLabel != null && !string.IsNullOrWhiteSpace(craftButtonLabel.text)) {
            _defaultCraftButtonLabel = craftButtonLabel.text;
            _hasCachedDefaultCraftButtonLabel = true; }

        if (craftProgressSlider == null) { craftProgressSlider = button.GetComponentInChildren<Slider>(true); } }
    private void EnsureCraftProgressSliderExists() { if (craftProgressSlider != null || button == null) { return; }

        craftProgressSlider = CreateRuntimeCraftProgressSlider(button.transform as RectTransform); }
    private void BindCraftButtonIfNeeded() { if (button == null) { return; }

        button.onClick.RemoveListener(Craft);
        if (!HasPersistentCraftBinding(button)) { button.onClick.AddListener(Craft); } }
    private Slider CreateRuntimeCraftProgressSlider(RectTransform parent) { if (parent == null) { return null; }

        GameObject rootObject = new GameObject("CraftProgressSlider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.SetParent(parent, false);
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.SetSiblingIndex(0);

        Image backgroundImage = rootObject.GetComponent<Image>();
        backgroundImage.color = new Color32(28, 24, 18, 200);
        backgroundImage.raycastTarget = false;
        backgroundImage.type = Image.Type.Simple;

        Slider slider = rootObject.GetComponent<Slider>();
        slider.interactable = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.targetGraphic = backgroundImage;

        GameObject fillAreaObject = new GameObject("Fill Area", typeof(RectTransform));
        RectTransform fillAreaRect = fillAreaObject.GetComponent<RectTransform>();
        fillAreaRect.SetParent(rootRect, false);
        fillAreaRect.anchorMin = new Vector2(0f, 0f);
        fillAreaRect.anchorMax = new Vector2(1f, 1f);
        fillAreaRect.offsetMin = new Vector2(4f, 4f);
        fillAreaRect.offsetMax = new Vector2(-4f, -4f);

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.SetParent(fillAreaRect, false);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fillObject.GetComponent<Image>();
        fillImage.color = new Color32(118, 193, 96, 255);
        fillImage.raycastTarget = false;
        fillImage.type = Image.Type.Simple;

        slider.fillRect = fillRect;
        slider.handleRect = null;
        slider.value = 0f;

        return slider; }
    private void AutoSelectCraftableItem() { if (craftingManager == null || craftingManager.slots == null) { return; }

        if (craftableItem != null && IsCraftableVisibleInSlots(craftableItem)) { return; }

        craftableItem = null;

        for (int i = 0; i < craftingManager.slots.Count; i++) {
            CraftableSlot slot = craftingManager.slots[i];
            if (slot == null || !slot.occupied || slot.locked || slot.craftableItemReference == null) { continue; }

            craftableItem = slot.craftableItemReference;
            return; } }
    private bool IsCraftableVisibleInSlots(CraftableItem target) { if (target == null || craftingManager == null || craftingManager.slots == null) { return false; }

        for (int i = 0; i < craftingManager.slots.Count; i++) {
            CraftableSlot slot = craftingManager.slots[i];
            if (slot == null || !slot.occupied || slot.locked) { continue; }

            if (slot.craftableItemReference == target) { return true; } }

        return false; }
    private static T FindFirstInScene<T>() where T : UnityEngine.Object {
        return UnitySceneSearch.FindFirst<T>();
    }
    private static SlotManager FindSlotManagerForInventoryList(InventoryListHandler handler) { if (handler == null) { return null; }

        SlotManager[] managers = UnitySceneSearch.FindAll<SlotManager>();

        for (int i = 0; i < managers.Length; i++) {
            SlotManager manager = managers[i];
            if (manager != null && manager.inventoryListHandler == handler) { return manager; } }

        SlotManager bestFallback = null;
        int bestSlotCount = -1;
        for (int i = 0; i < managers.Length; i++) {
            SlotManager manager = managers[i];
            if (manager == null) { continue; }

            int slotCount = manager.slots != null ? manager.slots.Count : 0;
            if (slotCount > bestSlotCount) {
                bestSlotCount = slotCount;
                bestFallback = manager; } }

        return bestFallback; }
    private void OnCraftHasEnoughResources() { UpdateCraftButtonInteractable(true); }
    private void OnCraftMissingResources() { UpdateCraftButtonInteractable(false); }
    private void UpdateCraftButtonInteractable(bool canInteract) { if (button == null) { return; }

        button.interactable = canInteract && !IsCraftInteractionLocked(); }
    private void ResetCraftProgressVisuals() {
        if (craftProgressSlider != null) {
            craftProgressSlider.value = 0f;
            craftProgressSlider.gameObject.SetActive(false); }

        ApplyIdleCraftButtonLabel(); }
    private void SetCraftProgress(float normalizedProgress) {
        float clampedProgress = Mathf.Clamp01(normalizedProgress);

        if (craftProgressSlider != null) { if (!craftProgressSlider.gameObject.activeSelf) { craftProgressSlider.gameObject.SetActive(true); }

            craftProgressSlider.value = clampedProgress; }

        if (showCraftPercentOnButton && craftButtonLabel != null) {
            int progressPercent = Mathf.RoundToInt(clampedProgress * 100f);
            craftButtonLabel.text = $"{craftingLabelPrefix} {progressPercent}%"; } }
    private void ApplyIdleCraftButtonLabel() { if (craftButtonLabel == null) { return; }

        craftButtonLabel.text = _defaultCraftButtonLabel; }
    private bool IsCraftInteractionLocked() { if (_craftInProgress) { return true; }

        return Time.unscaledTime < _craftButtonCooldownUntil; }
    private bool HasPersistentCraftBinding(Button targetButton) { if (targetButton == null) { return false; }

        UnityEventBase clickEvent = targetButton.onClick;
        int persistentEventCount = clickEvent.GetPersistentEventCount();
        for (int i = 0; i < persistentEventCount; i++) { if (!string.Equals(clickEvent.GetPersistentMethodName(i), nameof(Craft), StringComparison.Ordinal)) { continue; }

            UnityEngine.Object persistentTarget = clickEvent.GetPersistentTarget(i);
            if (persistentTarget == this) { return true; } }

        return false; }
    private IEnumerator CraftAfterDelay(InventoryItem craftedItem, int craftedAmount) {
        float duration = Mathf.Max(0f, craftDurationSeconds);
        SetCraftProgress(0f);

        if (duration > 0f) {
            float elapsed = 0f;
            while (elapsed < duration) {
                elapsed += Time.unscaledDeltaTime;
                SetCraftProgress(elapsed / duration);
                yield return null; } }

        SetCraftProgress(1f);

        bool addedCraftedItem = TryAddCraftedItem(craftedItem, craftedAmount);

        _craftInProgress = false;
        _craftRoutine = null;
        ResetCraftProgressVisuals();

        if (!addedCraftedItem) {
            hasenough = false;
            OnCraftMissingResources();
            yield break; }

        RefreshCraftAvailability(); }
    private bool TryAddCraftedItem(InventoryItem craftedItem, int craftedAmount) { if (craftedItem == null || craftedAmount <= 0) { return false; }

        if (inventoryManager != null) { return inventoryManager.AddItem(craftedItem, craftedAmount); }

        if (slotManager != null) { return slotManager.AddItem(craftedItem, craftedAmount); }

        return false; }

    // Supports entries like "wood" (defaults to 1) and "wood:3".
    private static bool TryParseRequirement(string rawRequirement, out string itemName, out int requiredAmount) {
        itemName = string.Empty;
        requiredAmount = 1;

        if (string.IsNullOrWhiteSpace(rawRequirement)) { return false; }

        string trimmed = rawRequirement.Trim();
        int separatorIndex = trimmed.LastIndexOf(':');

        if (separatorIndex > 0 && separatorIndex < trimmed.Length - 1) {
            string namePart = trimmed.Substring(0, separatorIndex).Trim();
            string amountPart = trimmed.Substring(separatorIndex + 1).Trim();

            if (string.IsNullOrWhiteSpace(namePart)) { return false; }

            if (!int.TryParse(amountPart, out int parsedAmount) || parsedAmount <= 0) { return false; }

            itemName = namePart;
            requiredAmount = parsedAmount;
            return true; }

        // Also support "wood (3)" format.
        int openParenthesis = trimmed.LastIndexOf('(');
        int closeParenthesis = trimmed.LastIndexOf(')');
        if (openParenthesis > 0 && closeParenthesis == trimmed.Length - 1 && closeParenthesis > openParenthesis + 1) {
            string namePart = trimmed.Substring(0, openParenthesis).Trim();
            string amountPart = trimmed.Substring(openParenthesis + 1, closeParenthesis - openParenthesis - 1).Trim();

            if (string.IsNullOrWhiteSpace(namePart)) { return false; }

            if (!int.TryParse(amountPart, out int parsedAmount) || parsedAmount <= 0) { return false; }

            itemName = namePart;
            requiredAmount = parsedAmount;
            return true; }

        itemName = trimmed;
        return true; }
    private bool TryConsumeResourcesFromInventoryManager(Dictionary<string, int> requiredResources) { if (requiredResources == null || requiredResources.Count == 0) { return true; }

        if (inventoryManager == null || inventoryManager.slotlist == null) { return false; }

        if (!HasEnoughResources(requiredResources)) { return false; }

        foreach (KeyValuePair<string, int> required in requiredResources) {
            int remaining = required.Value;
            if (remaining <= 0) { continue; }

            for (int i = 0; i < inventoryManager.slotlist.Count && remaining > 0; i++) {
                SlotInsideUI slot = inventoryManager.slotlist[i];
                if (slot == null || !slot.occupied || slot.count <= 0) { continue; }

                if (!SlotMatchesRequirement(slot, required.Key)) { continue; }

                int consume = Mathf.Min(slot.count, remaining);
                slot.count -= consume;
                remaining -= consume;

                if (slot.count <= 0) { ClearInventoryManagerSlot(slot); } else { UpdateInventoryManagerSlotVisual(slot); } }

            if (remaining > 0) { return false; } }

        return true; }
    private static bool SlotMatchesRequirement(SlotInsideUI slot, string requiredName) { if (slot == null || string.IsNullOrWhiteSpace(requiredName)) { return false; }

        string required = NormalizeItemToken(requiredName);
        if (string.IsNullOrEmpty(required)) { return false; }

        string slotName = NormalizeItemToken(GetBestSlotName(slot));
        return string.Equals(slotName, required, StringComparison.OrdinalIgnoreCase); }
    private static string GetBestSlotName(SlotInsideUI slot) { if (slot == null) { return string.Empty; }

        if (!string.IsNullOrWhiteSpace(slot.nameofslot)) { return slot.nameofslot.Trim(); }

        if (slot.Item != null && !string.IsNullOrWhiteSpace(slot.Item.nameofitem)) { return slot.Item.nameofitem.Trim(); }

        if (slot.Item != null && !string.IsNullOrWhiteSpace(slot.Item.name)) { return slot.Item.name.Trim(); }

        return string.Empty; }
    private static string NormalizeItemToken(string raw) { if (string.IsNullOrWhiteSpace(raw)) { return string.Empty; }

        return raw.Trim().Replace('_', ' ').ToLowerInvariant(); }
    private static void ClearInventoryManagerSlot(SlotInsideUI slot) { if (slot == null) { return; }

        slot.count = 0;
        slot.occupied = false;
        slot.nameofslot = string.Empty;
        slot.Item = null;

        if (slot.image != null) { slot.image.sprite = null; }

        UpdateInventoryManagerSlotVisual(slot); }
    private static void UpdateInventoryManagerSlotVisual(SlotInsideUI slot) { if (slot == null || slot.text == null) { return; }

        slot.text.text = slot.count > 0 ? slot.count.ToString() : "0"; } }
