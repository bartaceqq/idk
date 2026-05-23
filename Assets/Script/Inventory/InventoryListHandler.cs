using System.Collections.Generic;
using UnityEngine;

public class InventoryListHandler : MonoBehaviour
{
    public Dictionary<InventoryItem, int> itemlist = new Dictionary<InventoryItem, int>();
    private readonly Dictionary<string, InventoryItem> itemKeyByName = new Dictionary<string, InventoryItem>(System.StringComparer.OrdinalIgnoreCase);
    private static readonly System.Reflection.FieldInfo SlotItemReferenceField = typeof(Slot).GetField("inventoryItemReference");

    // Handle Add Item.
    public void AddItem(InventoryItem inventoryItem, int amount)
    {
        if (inventoryItem == null || amount <= 0)
        {
            return;
        }

        InventoryItem itemKey = ResolveOrRegisterItemKey(inventoryItem.name, inventoryItem);
        AddAmount(itemKey, amount);
    }

    // Handle Rebuild From Slots.
    public void RebuildFromSlots(List<Slot> slots)
    {
        itemlist.Clear();
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            Slot slot = slots[i];
            if (slot == null || slot.IsEmpty())
            {
                continue;
            }

            InventoryItem itemKey = ResolveItemKeyFromSlot(slot);
            int safeCount = Mathf.Max(0, slot.count);

            if (itemKey == null || safeCount <= 0)
            {
                continue;
            }

            AddAmount(itemKey, safeCount);
        }
    }

    // Handle Add Amount.
    private void AddAmount(InventoryItem itemKey, int amount)
    {
        if (itemKey == null || amount <= 0)
        {
            return;
        }

        if (itemlist.TryGetValue(itemKey, out int currentAmount))
        {
            itemlist[itemKey] = currentAmount + amount;
            return;
        }

        itemlist[itemKey] = amount;
    }

    // Handle Resolve Item Key From Slot.
    private InventoryItem ResolveItemKeyFromSlot(Slot slot)
    {
        if (slot == null)
        {
            return null;
        }

        // Optional compatibility path: use Slot.inventoryItemReference when present.
        if (SlotItemReferenceField != null)
        {
            InventoryItem slotReference = SlotItemReferenceField.GetValue(slot) as InventoryItem;
            if (slotReference != null)
            {
                return ResolveOrRegisterItemKey(slotReference.name, slotReference);
            }
        }

        return ResolveOrRegisterItemKey(slot.itemName, null);
    }

    // Handle Resolve Or Register Item Key.
    private InventoryItem ResolveOrRegisterItemKey(string itemName, InventoryItem preferredItem)
    {
        string normalizedName = NormalizeItemName(itemName);
        if (string.IsNullOrEmpty(normalizedName))
        {
            return preferredItem;
        }

        if (preferredItem != null)
        {
            itemKeyByName[normalizedName] = preferredItem;
            return preferredItem;
        }

        if (itemKeyByName.TryGetValue(normalizedName, out InventoryItem cachedItem) && cachedItem != null)
        {
            return cachedItem;
        }

        InventoryItem foundItem = FindInventoryItemInScene(normalizedName);
        if (foundItem != null)
        {
            itemKeyByName[normalizedName] = foundItem;
        }

        return foundItem;
    }

    // Handle Normalize Item Name.
    private static string NormalizeItemName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return string.Empty;
        }

        return itemName.Trim();
    }

    // Handle Find Inventory Item In Scene.
    private static InventoryItem FindInventoryItemInScene(string normalizedItemName)
    {
        if (string.IsNullOrEmpty(normalizedItemName))
        {
            return null;
        }

#if UNITY_2023_1_OR_NEWER
        InventoryItem[] allItems = FindObjectsByType<InventoryItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        InventoryItem[] allItems = FindObjectsOfType<InventoryItem>(true);
#endif

        for (int i = 0; i < allItems.Length; i++)
        {
            InventoryItem candidate = allItems[i];
            if (candidate == null)
            {
                continue;
            }

            if (string.Equals(NormalizeItemName(candidate.name), normalizedItemName, System.StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }
}
