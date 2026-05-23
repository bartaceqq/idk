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

    private Coroutine craftRoutine;
    private bool craftInProgress;
    private float clickLockedUntil;
    private string defaultButtonText = "CRAFT";

    private List<SlotInsideUI> Slots => inventoryManager != null ? inventoryManager.slotlist : null;

    private void Start()
    {
        ResolveReferences();
        SetupCraftButton();
        AutoSelectCraftableItem();
        ResetProgressUi();
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
        if (IsCraftLocked() || !ValidateCraft(out Dictionary<string, int> cost, out InventoryItem result, out int amount, true))
        {
            SetCanCraft(false);
            return;
        }

        if (!Consume(cost))
        {
            SetCanCraft(false);
            return;
        }

        craftInProgress = true;
        clickLockedUntil = Time.unscaledTime + craftClickCooldownSeconds;
        SetCanCraft(false);

        if (craftRoutine != null)
        {
            StopCoroutine(craftRoutine);
        }

        craftRoutine = StartCoroutine(FinishCraftAfterDelay(result, amount));
    }

    private void RefreshCraftAvailability()
    {
        hasenough = ValidateCraft(out _, out _, out _, false);
        SetCanCraft(hasenough);
    }

    private bool ValidateCraft(out Dictionary<string, int> cost, out InventoryItem result, out int amount, bool logWarnings)
    {
        cost = BuildCost();
        result = null;
        amount = 0;

        if (craftableItem == null)
        {
            if (logWarnings) Debug.LogWarning("CraftingProcessHandler: No craftable item selected.", this);
            return false;
        }

        amount = Mathf.Max(1, craftableItem.craftAmount);
        if (!craftableItem.TryResolveCraftedInventoryItem(out result, out string reason) || result == null)
        {
            if (logWarnings) Debug.LogWarning($"CraftingProcessHandler: {reason}", this);
            return false;
        }

        return HasEnough(cost) && HasRoomFor(result);
    }

    private Dictionary<string, int> BuildCost()
    {
        Dictionary<string, int> cost = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (craftableItem == null || craftableItem.neededResources == null)
        {
            return cost;
        }

        foreach (string rawRequirement in craftableItem.neededResources)
        {
            if (!TryParseRequirement(rawRequirement, out string itemName, out int amount))
            {
                continue;
            }

            string key = CleanName(itemName);
            if (key.Length == 0)
            {
                continue;
            }

            cost.TryGetValue(key, out int current);
            cost[key] = current + amount;
        }

        return cost;
    }

    private bool HasEnough(Dictionary<string, int> cost)
    {
        if (cost.Count == 0)
        {
            return true;
        }

        Dictionary<string, int> available = CountInventoryItems();
        foreach (KeyValuePair<string, int> need in cost)
        {
            if (!available.TryGetValue(need.Key, out int count) || count < need.Value)
            {
                return false;
            }
        }

        return true;
    }

    private Dictionary<string, int> CountInventoryItems()
    {
        Dictionary<string, int> items = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (Slots == null)
        {
            return items;
        }

        foreach (SlotInsideUI slot in Slots)
        {
            if (slot == null || !slot.occupied || slot.count <= 0)
            {
                continue;
            }

            string key = CleanName(GetSlotItemName(slot));
            if (key.Length == 0)
            {
                continue;
            }

            items.TryGetValue(key, out int current);
            items[key] = current + slot.count;
        }

        return items;
    }

    private bool HasRoomFor(InventoryItem item)
    {
        if (Slots == null || item == null)
        {
            return false;
        }

        foreach (SlotInsideUI slot in Slots)
        {
            if (slot != null && (!slot.occupied || IsSameItem(slot, item)))
            {
                return true;
            }
        }

        return false;
    }

    private bool Consume(Dictionary<string, int> cost)
    {
        if (cost.Count == 0)
        {
            return true;
        }

        if (Slots == null || !HasEnough(cost))
        {
            return false;
        }

        foreach (KeyValuePair<string, int> need in cost)
        {
            int remaining = need.Value;
            foreach (SlotInsideUI slot in Slots)
            {
                if (remaining <= 0)
                {
                    break;
                }

                if (slot == null || !slot.occupied || slot.count <= 0 || CleanName(GetSlotItemName(slot)) != need.Key)
                {
                    continue;
                }

                int taken = Mathf.Min(slot.count, remaining);
                slot.count -= taken;
                remaining -= taken;
                RefreshInventorySlot(slot);
            }

            if (remaining > 0)
            {
                return false;
            }
        }

        return true;
    }

    private IEnumerator FinishCraftAfterDelay(InventoryItem result, int amount)
    {
        float duration = Mathf.Max(0f, craftDurationSeconds);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            SetProgress(duration <= 0f ? 1f : elapsed / duration);
            yield return null;
        }

        SetProgress(1f);
        bool added = inventoryManager != null && inventoryManager.AddItem(result, amount);
        craftInProgress = false;
        craftRoutine = null;
        ResetProgressUi();

        hasenough = added && ValidateCraft(out _, out _, out _, false);
        SetCanCraft(hasenough);
    }

    private void AutoSelectCraftableItem()
    {
        if (craftingManager == null || craftingManager.slots == null || IsSelectedItemVisible())
        {
            return;
        }

        craftableItem = null;
        foreach (CraftableSlot slot in craftingManager.slots)
        {
            if (slot != null && slot.occupied && !slot.locked && slot.craftableItemReference != null)
            {
                craftableItem = slot.craftableItemReference;
                return;
            }
        }
    }

    private bool IsSelectedItemVisible()
    {
        if (craftableItem == null || craftingManager == null || craftingManager.slots == null)
        {
            return false;
        }

        foreach (CraftableSlot slot in craftingManager.slots)
        {
            if (slot != null && slot.occupied && !slot.locked && slot.craftableItemReference == craftableItem)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateSelectedSlotVisuals()
    {
        if (craftingManager == null || craftingManager.slots == null)
        {
            return;
        }

        foreach (CraftableSlot slot in craftingManager.slots)
        {
            slot?.SetSelectedVisual(craftableItem != null && slot.craftableItemReference == craftableItem);
        }
    }

    private void ResolveReferences()
    {
        if (craftingManager == null)
        {
            craftingManager = GetComponentInParent<CraftingManager>();
        }

        if (craftingManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            craftingManager = FindAnyObjectByType<CraftingManager>(FindObjectsInactive.Include);
#else
            craftingManager = FindObjectOfType<CraftingManager>(true);
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

    private void SetupCraftButton()
    {
        if (button == null)
        {
            return;
        }

        craftButtonLabel = craftButtonLabel != null ? craftButtonLabel : button.GetComponentInChildren<TextMeshProUGUI>(true);
        craftProgressSlider = craftProgressSlider != null ? craftProgressSlider : button.GetComponentInChildren<Slider>(true);

        if (craftButtonLabel != null && !string.IsNullOrWhiteSpace(craftButtonLabel.text))
        {
            defaultButtonText = craftButtonLabel.text;
        }

        button.onClick.RemoveListener(Craft);
        if (!HasPersistentCraftBinding(button))
        {
            button.onClick.AddListener(Craft);
        }

        EnsureProgressSliderExists();
    }

    private bool HasPersistentCraftBinding(Button targetButton)
    {
        UnityEventBase click = targetButton.onClick;
        for (int i = 0; i < click.GetPersistentEventCount(); i++)
        {
            if (click.GetPersistentTarget(i) == this && click.GetPersistentMethodName(i) == nameof(Craft))
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureProgressSliderExists()
    {
        RectTransform parent = button != null ? button.transform as RectTransform : null;
        if (craftProgressSlider != null || parent == null)
        {
            return;
        }

        RectTransform root = CreateUiRect("CraftProgressSlider", parent, typeof(Image), typeof(Slider));
        root.SetSiblingIndex(0);
        Image background = root.GetComponent<Image>();
        background.color = new Color32(28, 24, 18, 200);
        background.raycastTarget = false;

        RectTransform fill = CreateUiRect("Fill", root, typeof(Image));
        fill.offsetMin = new Vector2(4f, 4f);
        fill.offsetMax = new Vector2(-4f, -4f);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color32(118, 193, 96, 255);
        fillImage.raycastTarget = false;

        craftProgressSlider = root.GetComponent<Slider>();
        craftProgressSlider.interactable = false;
        craftProgressSlider.minValue = 0f;
        craftProgressSlider.maxValue = 1f;
        craftProgressSlider.fillRect = fill;
        craftProgressSlider.handleRect = null;
    }

    private static RectTransform CreateUiRect(string objectName, RectTransform parent, params Type[] extraComponents)
    {
        Type[] components = new Type[extraComponents.Length + 2];
        components[0] = typeof(RectTransform);
        components[1] = typeof(CanvasRenderer);
        Array.Copy(extraComponents, 0, components, 2, extraComponents.Length);

        RectTransform rect = new GameObject(objectName, components).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private void ResetProgressUi()
    {
        if (craftProgressSlider != null)
        {
            craftProgressSlider.value = 0f;
            craftProgressSlider.gameObject.SetActive(false);
        }

        if (craftButtonLabel != null)
        {
            craftButtonLabel.text = defaultButtonText;
        }
    }

    private void SetProgress(float value)
    {
        float progress = Mathf.Clamp01(value);
        if (craftProgressSlider != null)
        {
            craftProgressSlider.gameObject.SetActive(true);
            craftProgressSlider.value = progress;
        }

        if (showCraftPercentOnButton && craftButtonLabel != null)
        {
            craftButtonLabel.text = $"{craftingLabelPrefix} {Mathf.RoundToInt(progress * 100f)}%";
        }
    }

    private bool IsCraftLocked()
    {
        return craftInProgress || Time.unscaledTime < clickLockedUntil;
    }

    private void SetCanCraft(bool canCraft)
    {
        if (button != null)
        {
            button.interactable = canCraft && !IsCraftLocked();
        }
    }

    private static void RefreshInventorySlot(SlotInsideUI slot)
    {
        if (slot.count <= 0)
        {
            slot.count = 0;
            slot.occupied = false;
            slot.nameofslot = string.Empty;
            slot.Item = null;
            if (slot.image != null) slot.image.sprite = null;
        }

        if (slot.text != null)
        {
            slot.text.text = slot.count > 0 ? slot.count.ToString() : "0";
        }
    }

    private static bool IsSameItem(SlotInsideUI slot, InventoryItem item)
    {
        return slot != null && item != null &&
               (slot.Item == item || CleanName(GetSlotItemName(slot)) == CleanName(GetItemName(item)));
    }

    private static string GetSlotItemName(SlotInsideUI slot)
    {
        if (slot == null) return string.Empty;
        if (!string.IsNullOrWhiteSpace(slot.nameofslot)) return slot.nameofslot;
        return slot.Item != null ? GetItemName(slot.Item) : string.Empty;
    }

    private static string GetItemName(InventoryItem item)
    {
        if (item == null) return string.Empty;
        return !string.IsNullOrWhiteSpace(item.nameofitem) ? item.nameofitem : item.name;
    }

    private static string CleanName(string raw)
    {
        return string.IsNullOrWhiteSpace(raw) ? string.Empty : raw.Trim().Replace('_', ' ').ToLowerInvariant();
    }

    private static bool TryParseRequirement(string raw, out string itemName, out int amount)
    {
        itemName = string.Empty;
        amount = 1;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        string text = raw.Trim();
        int colon = text.LastIndexOf(':');
        if (colon > 0 && colon < text.Length - 1)
        {
            itemName = text.Substring(0, colon).Trim();
            return int.TryParse(text.Substring(colon + 1).Trim(), out amount) && amount > 0;
        }

        int open = text.LastIndexOf('(');
        int close = text.LastIndexOf(')');
        if (open > 0 && close == text.Length - 1 && close > open + 1)
        {
            itemName = text.Substring(0, open).Trim();
            return int.TryParse(text.Substring(open + 1, close - open - 1).Trim(), out amount) && amount > 0;
        }

        itemName = text;
        return true;
    }
}
