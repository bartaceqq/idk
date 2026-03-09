using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CraftingProcessHandler : MonoBehaviour
{
    public CraftableItem craftableItem;
    public CraftingManager craftingManager;
    public InventoryListHandler inventoryListHandler;
    public SlotManager slotManager;
    public InventoryManager inventoryManager;
    public Button button;
    public bool hasenough;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(Craft);
            button.onClick.AddListener(Craft);
        }

        ResolveReferences();
        AutoSelectCraftableItem();
        UpdateSelectedSlotVisuals();
        RefreshCraftAvailability();
    }

    // Update is called once per frame
    void Update()
    {
        ResolveReferences();
        AutoSelectCraftableItem();
        UpdateSelectedSlotVisuals();
        RefreshCraftAvailability();
    }

    // Handle Select Craftable Item.
    public void SelectCraftableItem(CraftableItem selectedCraftableItem)
    {
        craftableItem = selectedCraftableItem;
        UpdateSelectedSlotVisuals();
        RefreshCraftAvailability();
    }

    public void Craft()
    {
        ResolveReferences();

        if (!TryBuildRequirementMap(out Dictionary<string, int> requiredResources))
        {
            hasenough = false;
            OnCraftMissingResources();
            return;
        }

        bool hasResources = HasEnoughResources(requiredResources);
        if (!hasResources)
        {
            hasenough = false;
            OnCraftMissingResources();
            return;
        }

        if (!TryResolveCraftResult(out InventoryItem craftedItem, out int craftedAmount, true))
        {
            hasenough = false;
            OnCraftMissingResources();
            return;
        }

        if (!CanReceiveCraftedItem())
        {
            hasenough = false;
            OnCraftMissingResources();
            return;
        }

        bool consumedResources;
        if (inventoryManager != null)
        {
            consumedResources = TryConsumeResourcesFromInventoryManager(requiredResources);
        }
        else if (slotManager != null)
        {
            consumedResources = slotManager.TryConsumeResources(requiredResources);
        }
        else
        {
            consumedResources = false;
        }

        if (!consumedResources)
        {
            hasenough = false;
            OnCraftMissingResources();
            return;
        }

        bool addedCraftedItem;
        if (inventoryManager != null)
        {
            addedCraftedItem = inventoryManager.AddItem(craftedItem, craftedAmount);
        }
        else if (slotManager != null)
        {
            addedCraftedItem = slotManager.AddItem(craftedItem, craftedAmount);
        }
        else
        {
            addedCraftedItem = false;
        }

        if (!addedCraftedItem)
        {
            hasenough = false;
            OnCraftMissingResources();
            return;
        }

        RefreshCraftAvailability();
    }

    // Handle Refresh Craft Availability.
    private void RefreshCraftAvailability()
    {
        UpdateSelectedSlotVisuals();

        if (!TryBuildRequirementMap(out Dictionary<string, int> requiredResources))
        {
            hasenough = false;
            OnCraftMissingResources();
            return;
        }

        bool hasResources = HasEnoughResources(requiredResources);
        bool canReceiveCraftedItem = CanReceiveCraftedItem();
        hasenough = hasResources && canReceiveCraftedItem;
        if (hasenough)
        {
            OnCraftHasEnoughResources();
        }
        else
        {
            OnCraftMissingResources();
        }
    }

    // Handle Update Selected Slot Visuals.
    private void UpdateSelectedSlotVisuals()
    {
        if (craftingManager == null || craftingManager.slots == null)
        {
            return;
        }

        for (int i = 0; i < craftingManager.slots.Count; i++)
        {
            CraftableSlot slot = craftingManager.slots[i];
            if (slot == null)
            {
                continue;
            }

            bool isSelected = craftableItem != null && slot.craftableItemReference == craftableItem;
            slot.SetSelectedVisual(isSelected);
        }
    }

    // Handle Has Enough Resources.
    private bool HasEnoughResources(Dictionary<string, int> requiredResources)
    {
        if (requiredResources == null || requiredResources.Count == 0)
        {
            return true;
        }

        Dictionary<string, int> availableByName = BuildAvailableByNameFromInventoryManagerSlots();
        if (availableByName.Count == 0)
        {
            availableByName = BuildAvailableByNameFromSlots();
        }
        if (availableByName.Count == 0 && inventoryListHandler != null)
        {
            Dictionary<InventoryItem, int> list = inventoryListHandler.itemlist;
            availableByName = BuildAvailableByName(list);
        }

        if (availableByName.Count == 0)
        {
            return false;
        }

        foreach (KeyValuePair<string, int> required in requiredResources)
        {
            if (!availableByName.TryGetValue(required.Key, out int availableAmount) || availableAmount < required.Value)
            {
                return false;
            }
        }

        return true;
    }

    // Handle Can Receive Crafted Item.
    private bool CanReceiveCraftedItem()
    {
        if (!TryResolveCraftResult(out InventoryItem craftedItem, out int craftedAmount, false))
        {
            return false;
        }

        if (craftedAmount <= 0)
        {
            return false;
        }

        if (inventoryManager != null && inventoryManager.slotlist != null)
        {
            for (int i = 0; i < inventoryManager.slotlist.Count; i++)
            {
                SlotInsideUI slot = inventoryManager.slotlist[i];
                if (slot != null && !slot.occupied)
                {
                    return true;
                }
            }

            return false;
        }

        return slotManager != null && slotManager.CanAddItem(craftedItem);
    }

    // Handle Try Resolve Craft Result.
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
        if (!craftableItem.TryResolveCraftedInventoryItem(slotManager, out craftedItem, out string reason))
        {
            if (logWarnings)
            {
                Debug.LogWarning($"CraftingProcessHandler: {reason}", this);
            }

            return false;
        }

        return craftedItem != null;
    }

    // Handle Build Available By Name From Slots.
    private Dictionary<string, int> BuildAvailableByNameFromSlots()
    {
        Dictionary<string, int> availableByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (slotManager == null || slotManager.slots == null)
        {
            return availableByName;
        }

        for (int i = 0; i < slotManager.slots.Count; i++)
        {
            Slot slot = slotManager.slots[i];
            if (slot == null || slot.IsEmpty() || string.IsNullOrWhiteSpace(slot.itemName))
            {
                continue;
            }

            string key = slot.itemName.Trim();
            int amount = Mathf.Max(0, slot.count);
            if (amount <= 0)
            {
                continue;
            }

            if (availableByName.TryGetValue(key, out int currentAmount))
            {
                availableByName[key] = currentAmount + amount;
            }
            else
            {
                availableByName[key] = amount;
            }
        }

        return availableByName;
    }

    // Handle Build Available By Name From Inventory Manager Slots.
    private Dictionary<string, int> BuildAvailableByNameFromInventoryManagerSlots()
    {
        Dictionary<string, int> availableByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (inventoryManager == null || inventoryManager.slotlist == null)
        {
            return availableByName;
        }

        for (int i = 0; i < inventoryManager.slotlist.Count; i++)
        {
            SlotInsideUI slot = inventoryManager.slotlist[i];
            if (slot == null || !slot.occupied)
            {
                continue;
            }

            int amount = Mathf.Max(0, slot.count);
            if (amount <= 0)
            {
                continue;
            }

            string key = GetBestSlotName(slot);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (availableByName.TryGetValue(key, out int currentAmount))
            {
                availableByName[key] = currentAmount + amount;
            }
            else
            {
                availableByName[key] = amount;
            }
        }

        return availableByName;
    }

    // Handle Try Build Requirement Map.
    private bool TryBuildRequirementMap(out Dictionary<string, int> requiredResources)
    {
        requiredResources = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (craftableItem == null)
        {
            return false;
        }

        List<string> neededResources = craftableItem.neededResources;
        if (neededResources == null || neededResources.Count == 0)
        {
            return true;
        }

        for (int i = 0; i < neededResources.Count; i++)
        {
            string neededItem = neededResources[i];
            if (!TryParseRequirement(neededItem, out string neededName, out int neededCount))
            {
                return false;
            }

            if (requiredResources.TryGetValue(neededName, out int currentRequiredAmount))
            {
                requiredResources[neededName] = currentRequiredAmount + neededCount;
            }
            else
            {
                requiredResources[neededName] = neededCount;
            }
        }

        return true;
    }

    // Handle Build Available By Name.
    private static Dictionary<string, int> BuildAvailableByName(Dictionary<InventoryItem, int> list)
    {
        Dictionary<string, int> availableByName = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (list == null)
        {
            return availableByName;
        }

        foreach (KeyValuePair<InventoryItem, int> pair in list)
        {
            if (pair.Key == null || pair.Value <= 0 || string.IsNullOrWhiteSpace(pair.Key.name))
            {
                continue;
            }

            string key = pair.Key.name.Trim();
            if (availableByName.TryGetValue(key, out int currentAmount))
            {
                availableByName[key] = currentAmount + pair.Value;
            }
            else
            {
                availableByName[key] = pair.Value;
            }
        }

        return availableByName;
    }

    // Handle Resolve References.
    private void ResolveReferences()
    {
        if (craftingManager == null)
        {
            craftingManager = GetComponentInParent<CraftingManager>();
            if (craftingManager == null)
            {
                craftingManager = FindFirstInScene<CraftingManager>();
            }
        }

        if (slotManager == null)
        {
            slotManager = FindSlotManagerForInventoryList(inventoryListHandler);
        }

        if (slotManager == null)
        {
            slotManager = FindFirstInScene<SlotManager>();
        }

        if (slotManager != null &&
            inventoryListHandler != null &&
            slotManager.inventoryListHandler != null &&
            slotManager.inventoryListHandler != inventoryListHandler)
        {
            SlotManager matchedSlotManager = FindSlotManagerForInventoryList(inventoryListHandler);
            if (matchedSlotManager != null)
            {
                slotManager = matchedSlotManager;
            }
        }

        if (inventoryListHandler == null && slotManager != null)
        {
            inventoryListHandler = slotManager.inventoryListHandler;
        }

        if (inventoryListHandler == null)
        {
            inventoryListHandler = FindFirstInScene<InventoryListHandler>();
        }

        if (slotManager != null && slotManager.inventoryListHandler == null && inventoryListHandler != null)
        {
            slotManager.inventoryListHandler = inventoryListHandler;
        }

        if (inventoryManager == null)
        {
            inventoryManager = FindFirstInScene<InventoryManager>();
        }
    }

    // Handle Auto Select Craftable Item.
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
            if (slot == null || !slot.occupied || slot.locked || slot.craftableItemReference == null)
            {
                continue;
            }

            craftableItem = slot.craftableItemReference;
            return;
        }
    }

    // Handle Is Craftable Visible In Slots.
    private bool IsCraftableVisibleInSlots(CraftableItem target)
    {
        if (target == null || craftingManager == null || craftingManager.slots == null)
        {
            return false;
        }

        for (int i = 0; i < craftingManager.slots.Count; i++)
        {
            CraftableSlot slot = craftingManager.slots[i];
            if (slot == null || !slot.occupied || slot.locked)
            {
                continue;
            }

            if (slot.craftableItemReference == target)
            {
                return true;
            }
        }

        return false;
    }

    // Handle Find First In Scene.
    private static T FindFirstInScene<T>() where T : UnityEngine.Object
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return UnityEngine.Object.FindObjectOfType<T>(true);
#endif
    }

    // Handle Find Slot Manager For Inventory List.
    private static SlotManager FindSlotManagerForInventoryList(InventoryListHandler handler)
    {
        if (handler == null)
        {
            return null;
        }

#if UNITY_2023_1_OR_NEWER
        SlotManager[] managers = UnityEngine.Object.FindObjectsByType<SlotManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        SlotManager[] managers = UnityEngine.Object.FindObjectsOfType<SlotManager>(true);
#endif

        for (int i = 0; i < managers.Length; i++)
        {
            SlotManager manager = managers[i];
            if (manager != null && manager.inventoryListHandler == handler)
            {
                return manager;
            }
        }

        SlotManager bestFallback = null;
        int bestSlotCount = -1;
        for (int i = 0; i < managers.Length; i++)
        {
            SlotManager manager = managers[i];
            if (manager == null)
            {
                continue;
            }

            int slotCount = manager.slots != null ? manager.slots.Count : 0;
            if (slotCount > bestSlotCount)
            {
                bestSlotCount = slotCount;
                bestFallback = manager;
            }
        }

        return bestFallback;
    }

    // Handle On Craft Has Enough Resources.
    private void OnCraftHasEnoughResources()
    {
        if (button != null)
        {
            button.interactable = true;
        }
    }

    // Handle On Craft Missing Resources.
    private void OnCraftMissingResources()
    {
        if (button != null)
        {
            button.interactable = false;
        }
    }

    // Supports entries like "wood" (defaults to 1) and "wood:3".
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
            string namePart = trimmed.Substring(0, separatorIndex).Trim();
            string amountPart = trimmed.Substring(separatorIndex + 1).Trim();

            if (string.IsNullOrWhiteSpace(namePart))
            {
                return false;
            }

            if (!int.TryParse(amountPart, out int parsedAmount) || parsedAmount <= 0)
            {
                return false;
            }

            itemName = namePart;
            requiredAmount = parsedAmount;
            return true;
        }

        // Also support "wood (3)" format.
        int openParenthesis = trimmed.LastIndexOf('(');
        int closeParenthesis = trimmed.LastIndexOf(')');
        if (openParenthesis > 0 && closeParenthesis == trimmed.Length - 1 && closeParenthesis > openParenthesis + 1)
        {
            string namePart = trimmed.Substring(0, openParenthesis).Trim();
            string amountPart = trimmed.Substring(openParenthesis + 1, closeParenthesis - openParenthesis - 1).Trim();

            if (string.IsNullOrWhiteSpace(namePart))
            {
                return false;
            }

            if (!int.TryParse(amountPart, out int parsedAmount) || parsedAmount <= 0)
            {
                return false;
            }

            itemName = namePart;
            requiredAmount = parsedAmount;
            return true;
        }

        itemName = trimmed;
        return true;
    }

    // Handle Try Consume Resources From Inventory Manager.
    private bool TryConsumeResourcesFromInventoryManager(Dictionary<string, int> requiredResources)
    {
        if (requiredResources == null || requiredResources.Count == 0)
        {
            return true;
        }

        if (inventoryManager == null || inventoryManager.slotlist == null)
        {
            return false;
        }

        if (!HasEnoughResources(requiredResources))
        {
            return false;
        }

        foreach (KeyValuePair<string, int> required in requiredResources)
        {
            int remaining = required.Value;
            if (remaining <= 0)
            {
                continue;
            }

            for (int i = 0; i < inventoryManager.slotlist.Count && remaining > 0; i++)
            {
                SlotInsideUI slot = inventoryManager.slotlist[i];
                if (slot == null || !slot.occupied || slot.count <= 0)
                {
                    continue;
                }

                if (!SlotMatchesRequirement(slot, required.Key))
                {
                    continue;
                }

                int consume = Mathf.Min(slot.count, remaining);
                slot.count -= consume;
                remaining -= consume;

                if (slot.count <= 0)
                {
                    ClearInventoryManagerSlot(slot);
                }
                else
                {
                    UpdateInventoryManagerSlotVisual(slot);
                }
            }

            if (remaining > 0)
            {
                return false;
            }
        }

        return true;
    }

    // Handle Slot Matches Requirement.
    private static bool SlotMatchesRequirement(SlotInsideUI slot, string requiredName)
    {
        if (slot == null || string.IsNullOrWhiteSpace(requiredName))
        {
            return false;
        }

        string required = NormalizeItemToken(requiredName);
        if (string.IsNullOrEmpty(required))
        {
            return false;
        }

        string slotName = NormalizeItemToken(GetBestSlotName(slot));
        return string.Equals(slotName, required, StringComparison.OrdinalIgnoreCase);
    }

    // Handle Get Best Slot Name.
    private static string GetBestSlotName(SlotInsideUI slot)
    {
        if (slot == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(slot.nameofslot))
        {
            return slot.nameofslot.Trim();
        }

        if (slot.Item != null && !string.IsNullOrWhiteSpace(slot.Item.nameofitem))
        {
            return slot.Item.nameofitem.Trim();
        }

        if (slot.Item != null && !string.IsNullOrWhiteSpace(slot.Item.name))
        {
            return slot.Item.name.Trim();
        }

        return string.Empty;
    }

    // Handle Normalize Item Token.
    private static string NormalizeItemToken(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        return raw.Trim().Replace('_', ' ').ToLowerInvariant();
    }

    // Handle Clear Inventory Manager Slot.
    private static void ClearInventoryManagerSlot(SlotInsideUI slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.count = 0;
        slot.occupied = false;
        slot.nameofslot = string.Empty;
        slot.Item = null;

        if (slot.image != null)
        {
            slot.image.sprite = null;
        }

        UpdateInventoryManagerSlotVisual(slot);
    }

    // Handle Update Inventory Manager Slot Visual.
    private static void UpdateInventoryManagerSlotVisual(SlotInsideUI slot)
    {
        if (slot == null || slot.text == null)
        {
            return;
        }

        slot.text.text = slot.count > 0 ? slot.count.ToString() : "0";
    }
}
