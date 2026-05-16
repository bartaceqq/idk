using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class CraftingProcessHandler : MonoBehaviour
{
    public CraftableItem craftableItem;
    public CraftingManager craftingManager;
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

    private float craftButtonCooldownUntil;
    private bool craftInProgress;
    private Coroutine craftRoutine;
    private string defaultCraftButtonLabel = "CRAFT";
    private bool hasCachedDefaultCraftButtonLabel;

    private void Start()
    {
        ResolveReferences();
        ResolveCraftUiReferences();
        EnsureCraftProgressSliderExists();
        BindCraftButtonIfNeeded();
        AutoSelectCraftableItem();
        UpdateSelectedSlotVisuals();
        ResetCraftProgressVisuals();
        RefreshCraftAvailability();
    }

    private void Update()
    {
        ResolveReferences();
        AutoSelectCraftableItem();
        UpdateSelectedSlotVisuals();
        RefreshCraftAvailability();
    }

    public void SelectCraftableItem(CraftableItem selectedCraftableItem)
    {
        craftableItem = selectedCraftableItem;
        UpdateSelectedSlotVisuals();
        RefreshCraftAvailability();
    }

    public void Craft()
    {
        if (IsCraftInteractionLocked())
        {
            return;
        }

        ResolveReferences();
        ResolveCraftUiReferences();
        EnsureCraftProgressSliderExists();

        if (!TryBuildRequirementMap(out Dictionary<string, int> requiredResources) ||
            !HasEnoughResources(requiredResources) ||
            !TryResolveCraftResult(out InventoryItem craftedItem, out int craftedAmount, true) ||
            !CanReceiveCraftedItem(craftedItem))
        {
            hasenough = false;
            UpdateCraftButtonInteractable(false);
            return;
        }

        if (!TryConsumeResources(requiredResources))
        {
            hasenough = false;
            UpdateCraftButtonInteractable(false);
            return;
        }

        craftInProgress = true;
        craftButtonCooldownUntil = Time.unscaledTime + Mathf.Max(0f, craftClickCooldownSeconds);
        UpdateCraftButtonInteractable(false);

        if (craftRoutine != null)
        {
            StopCoroutine(craftRoutine);
        }

        craftRoutine = StartCoroutine(CraftAfterDelay(craftedItem, craftedAmount));
    }

    private void RefreshCraftAvailability()
    {
        if (!TryBuildRequirementMap(out Dictionary<string, int> requiredResources) ||
            !TryResolveCraftResult(out InventoryItem craftedItem, out _, false))
        {
            hasenough = false;
            UpdateCraftButtonInteractable(false);
            return;
        }

        hasenough = HasEnoughResources(requiredResources) && CanReceiveCraftedItem(craftedItem);
        UpdateCraftButtonInteractable(hasenough);
    }

    private bool HasEnoughResources(Dictionary<string, int> requiredResources)
    {
        if (requiredResources == null || requiredResources.Count == 0)
        {
            return true;
        }

        Dictionary<string, int> available = BuildAvailableByNameFromInventory();
        foreach (KeyValuePair<string, int> required in requiredResources)
        {
            if (!available.TryGetValue(required.Key, out int count) || count < required.Value)
            {
                return false;
            }
        }

        return true;
    }

    private Dictionary<string, int> BuildAvailableByNameFromInventory()
    {
        Dictionary<string, int> available = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (inventoryManager == null || inventoryManager.slotlist == null)
        {
            return available;
        }

        for (int i = 0; i < inventoryManager.slotlist.Count; i++)
        {
            SlotInsideUI slot = inventoryManager.slotlist[i];
            if (slot == null || !slot.occupied || slot.count <= 0)
            {
                continue;
            }

            string key = NormalizeItemToken(GetBestSlotName(slot));
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            available.TryGetValue(key, out int current);
            available[key] = current + slot.count;
        }

        return available;
    }

    private bool CanReceiveCraftedItem(InventoryItem craftedItem)
    {
        if (inventoryManager == null || craftedItem == null || inventoryManager.slotlist == null)
        {
            return false;
        }

        for (int i = 0; i < inventoryManager.slotlist.Count; i++)
        {
            SlotInsideUI slot = inventoryManager.slotlist[i];
            if (slot == null)
            {
                continue;
            }

            if (!slot.occupied || IsSameItem(slot, craftedItem))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryResolveCraftResult(out InventoryItem craftedItem, out int craftedAmount, bool logWarnings)
    {
        craftedItem = null;
        craftedAmount = 0;

        if (craftableItem == null)
        {
            if (logWarnings)
            {
                Debug.LogWarning("CraftingProcessHandler: No craftable item selected.", this);
            }

            return false;
        }

        craftedAmount = Mathf.Max(1, craftableItem.craftAmount);
        if (!craftableItem.TryResolveCraftedInventoryItem(out craftedItem, out string reason))
        {
            if (logWarnings)
            {
                Debug.LogWarning($"CraftingProcessHandler: {reason}", this);
            }

            return false;
        }

        return craftedItem != null;
    }

    private bool TryBuildRequirementMap(out Dictionary<string, int> requiredResources)
    {
        requiredResources = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (craftableItem == null || craftableItem.neededResources == null)
        {
            return craftableItem != null;
        }

        for (int i = 0; i < craftableItem.neededResources.Count; i++)
        {
            if (!TryParseRequirement(craftableItem.neededResources[i], out string itemName, out int amount))
            {
                continue;
            }

            string key = NormalizeItemToken(itemName);
            if (string.IsNullOrEmpty(key))
            {
                continue;
            }

            requiredResources.TryGetValue(key, out int current);
            requiredResources[key] = current + amount;
        }

        return true;
    }

    private bool TryConsumeResources(Dictionary<string, int> requiredResources)
    {
        if (requiredResources == null || requiredResources.Count == 0)
        {
            return true;
        }

        if (inventoryManager == null || inventoryManager.slotlist == null || !HasEnoughResources(requiredResources))
        {
            return false;
        }

        foreach (KeyValuePair<string, int> required in requiredResources)
        {
            int remaining = required.Value;
            for (int i = 0; i < inventoryManager.slotlist.Count && remaining > 0; i++)
            {
                SlotInsideUI slot = inventoryManager.slotlist[i];
                if (slot == null || !slot.occupied || slot.count <= 0 || !SlotMatchesRequirement(slot, required.Key))
                {
                    continue;
                }

                int consumed = Mathf.Min(slot.count, remaining);
                slot.count -= consumed;
                remaining -= consumed;

                if (slot.count <= 0)
                {
                    ClearInventorySlot(slot);
                }
                else
                {
                    UpdateInventorySlotVisual(slot);
                }
            }

            if (remaining > 0)
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerator CraftAfterDelay(InventoryItem craftedItem, int craftedAmount)
    {
        float duration = Mathf.Max(0f, craftDurationSeconds);
        SetCraftProgress(0f);

        if (duration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetCraftProgress(elapsed / duration);
                yield return null;
            }
        }

        SetCraftProgress(1f);
        bool added = inventoryManager != null && inventoryManager.AddItem(craftedItem, craftedAmount);

        craftInProgress = false;
        craftRoutine = null;
        ResetCraftProgressVisuals();

        if (!added)
        {
            hasenough = false;
            UpdateCraftButtonInteractable(false);
            yield break;
        }

        RefreshCraftAvailability();
    }

    private void AutoSelectCraftableItem()
    {
        if (craftingManager == null || craftingManager.slots == null)
        {
            return;
        }

        if (craftableItem != null && IsCraftableVisibleInSlots(craftableItem))
        {
            return;
        }

        craftableItem = null;
        for (int i = 0; i < craftingManager.slots.Count; i++)
        {
            CraftableSlot slot = craftingManager.slots[i];
            if (slot != null && slot.occupied && !slot.locked && slot.craftableItemReference != null)
            {
                craftableItem = slot.craftableItemReference;
                return;
            }
        }
    }

    private bool IsCraftableVisibleInSlots(CraftableItem target)
    {
        if (target == null || craftingManager == null || craftingManager.slots == null)
        {
            return false;
        }

        for (int i = 0; i < craftingManager.slots.Count; i++)
        {
            CraftableSlot slot = craftingManager.slots[i];
            if (slot != null && slot.occupied && !slot.locked && slot.craftableItemReference == target)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateSelectedSlotVisuals()
    {
        if (craftingManager == null || CraftingSlotsMissing())
        {
            return;
        }

        for (int i = 0; i < craftingManager.slots.Count; i++)
        {
            CraftableSlot slot = craftingManager.slots[i];
            if (slot != null)
            {
                slot.SetSelectedVisual(craftableItem != null && slot.craftableItemReference == craftableItem);
            }
        }
    }

    private bool CraftingSlotsMissing()
    {
        return craftingManager.slots == null;
    }

    private void ResolveReferences()
    {
        if (craftingManager == null)
        {
            craftingManager = GetComponentInParent<CraftingManager>();
#if UNITY_2023_1_OR_NEWER
            if (craftingManager == null) craftingManager = FindAnyObjectByType<CraftingManager>(FindObjectsInactive.Include);
#else
            if (craftingManager == null) craftingManager = FindObjectOfType<CraftingManager>(true);
#endif
        }

        if (inventoryManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            inventoryManager = FindAnyObjectByType<InventoryManager>(FindObjectsInactive.Include);
#else
            inventoryManager = FindObjectOfType<InventoryManager>(true);
#endif
        }
    }

    private void ResolveCraftUiReferences()
    {
        if (button == null)
        {
            return;
        }

        if (craftButtonLabel == null)
        {
            craftButtonLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (!hasCachedDefaultCraftButtonLabel && craftButtonLabel != null && !string.IsNullOrWhiteSpace(craftButtonLabel.text))
        {
            defaultCraftButtonLabel = craftButtonLabel.text;
            hasCachedDefaultCraftButtonLabel = true;
        }

        if (craftProgressSlider == null)
        {
            craftProgressSlider = button.GetComponentInChildren<Slider>(true);
        }
    }

    private void BindCraftButtonIfNeeded()
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(Craft);
        if (!HasPersistentCraftBinding(button))
        {
            button.onClick.AddListener(Craft);
        }
    }

    private bool HasPersistentCraftBinding(Button targetButton)
    {
        UnityEventBase clickEvent = targetButton.onClick;
        for (int i = 0; i < clickEvent.GetPersistentEventCount(); i++)
        {
            if (clickEvent.GetPersistentTarget(i) == this &&
                string.Equals(clickEvent.GetPersistentMethodName(i), nameof(Craft), StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureCraftProgressSliderExists()
    {
        if (craftProgressSlider != null || button == null)
        {
            return;
        }

        RectTransform parent = button.transform as RectTransform;
        if (parent == null)
        {
            return;
        }

        GameObject root = new GameObject("CraftProgressSlider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Slider));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetSiblingIndex(0);

        Image background = root.GetComponent<Image>();
        background.color = new Color32(28, 24, 18, 200);
        background.raycastTarget = false;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.SetParent(rect, false);
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);

        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color32(118, 193, 96, 255);
        fillImage.raycastTarget = false;

        craftProgressSlider = root.GetComponent<Slider>();
        craftProgressSlider.interactable = false;
        craftProgressSlider.minValue = 0f;
        craftProgressSlider.maxValue = 1f;
        craftProgressSlider.fillRect = fillRect;
        craftProgressSlider.handleRect = null;
    }

    private void ResetCraftProgressVisuals()
    {
        if (craftProgressSlider != null)
        {
            craftProgressSlider.value = 0f;
            craftProgressSlider.gameObject.SetActive(false);
        }

        if (craftButtonLabel != null)
        {
            craftButtonLabel.text = defaultCraftButtonLabel;
        }
    }

    private void SetCraftProgress(float normalizedProgress)
    {
        float clamped = Mathf.Clamp01(normalizedProgress);
        if (craftProgressSlider != null)
        {
            craftProgressSlider.gameObject.SetActive(true);
            craftProgressSlider.value = clamped;
        }

        if (showCraftPercentOnButton && craftButtonLabel != null)
        {
            craftButtonLabel.text = $"{craftingLabelPrefix} {Mathf.RoundToInt(clamped * 100f)}%";
        }
    }

    private bool IsCraftInteractionLocked()
    {
        return craftInProgress || Time.unscaledTime < craftButtonCooldownUntil;
    }

    private void UpdateCraftButtonInteractable(bool canInteract)
    {
        if (button != null)
        {
            button.interactable = canInteract && !IsCraftInteractionLocked();
        }
    }

    private static bool IsSameItem(SlotInsideUI slot, InventoryItem item)
    {
        if (slot == null || item == null)
        {
            return false;
        }

        if (slot.Item == item)
        {
            return true;
        }

        return string.Equals(NormalizeItemToken(GetBestSlotName(slot)), NormalizeItemToken(GetBestItemName(item)), StringComparison.OrdinalIgnoreCase);
    }

    private static string GetBestItemName(InventoryItem item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(item.nameofitem)) return item.nameofitem;
        return item.name;
    }

    private static string GetBestSlotName(SlotInsideUI slot)
    {
        if (slot == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(slot.nameofslot)) return slot.nameofslot;
        return slot.Item != null ? GetBestItemName(slot.Item) : string.Empty;
    }

    private static bool SlotMatchesRequirement(SlotInsideUI slot, string requiredName)
    {
        return string.Equals(NormalizeItemToken(GetBestSlotName(slot)), NormalizeItemToken(requiredName), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeItemToken(string raw)
    {
        return string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim().Replace('_', ' ').ToLowerInvariant();
    }

    private static void ClearInventorySlot(SlotInsideUI slot)
    {
        slot.count = 0;
        slot.occupied = false;
        slot.nameofslot = string.Empty;
        slot.Item = null;
        if (slot.image != null)
        {
            slot.image.sprite = null;
        }

        UpdateInventorySlotVisual(slot);
    }

    private static void UpdateInventorySlotVisual(SlotInsideUI slot)
    {
        if (slot != null && slot.text != null)
        {
            slot.text.text = slot.count > 0 ? slot.count.ToString() : "0";
        }
    }

    private static bool TryParseRequirement(string rawRequirement, out string itemName, out int requiredAmount)
    {
        itemName = string.Empty;
        requiredAmount = 1;

        if (string.IsNullOrWhiteSpace(rawRequirement))
        {
            return false;
        }

        string trimmed = rawRequirement.Trim();
        int separatorIndex = trimmed.LastIndexOf(':');
        if (separatorIndex > 0 && separatorIndex < trimmed.Length - 1)
        {
            itemName = trimmed.Substring(0, separatorIndex).Trim();
            return int.TryParse(trimmed.Substring(separatorIndex + 1).Trim(), out requiredAmount) && requiredAmount > 0;
        }

        int openParenthesis = trimmed.LastIndexOf('(');
        int closeParenthesis = trimmed.LastIndexOf(')');
        if (openParenthesis > 0 && closeParenthesis == trimmed.Length - 1 && closeParenthesis > openParenthesis + 1)
        {
            itemName = trimmed.Substring(0, openParenthesis).Trim();
            return int.TryParse(trimmed.Substring(openParenthesis + 1, closeParenthesis - openParenthesis - 1).Trim(), out requiredAmount) && requiredAmount > 0;
        }

        itemName = trimmed;
        return true;
    }
}
