using System.Collections.Generic;
using UnityEngine;

// Controls Slot Manager behavior.
public class SlotManager : MonoBehaviour
{
    public InventoryListHandler inventoryListHandler;
    [SerializeField] private bool autoDiscoverSlots = true;
    public List<Slot> slots = new List<Slot>();

    // Initialize references before gameplay starts.
    private void Awake()
    {
        ResolveInventoryListHandler();
        PrepareSlots();
        RefreshInventoryList();
    }

    // Run in the editor when values change in Inspector.
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ResolveInventoryListHandler();
            PrepareSlots();
        }
    }

    // Handle Register Slot.
    public void RegisterSlot(Slot slot)
    {
        if (slot == null)
        {
            return;
        }

        if (!slots.Contains(slot))
        {
            slots.Add(slot);
        }

        if (slot.slotManager != this)
        {
            slot.slotManager = this;
        }

        SortSlotsTopLeft();
    }

    // Handle Add Item.
    public bool AddItem(InventoryItem inventoryItem)
    {
        return AddItem(inventoryItem, -1);
    }

    // Handle Add Item.
    public bool AddItem(InventoryItem inventoryItem, int explicitAmount)
    {
        if (inventoryItem == null)
        {
            return false;
        }

        if (explicitAmount == 0)
        {
            return true;
        }

        ResolveInventoryListHandler();

        // Always sort first so "first empty" is stable (top-left first).
        PrepareSlots();

        // 1) Stack into an existing slot with the same item.
        foreach (Slot slot in slots)
        {
            if (slot != null && slot.MatchesItem(inventoryItem))
            {
                slot.AddItem(inventoryItem, inventoryListHandler, explicitAmount);
                RefreshInventoryList();
                return true;
            }
        }

        // 2) Otherwise place into the first empty slot.
        foreach (Slot slot in slots)
        {
            if (slot != null && slot.IsEmpty())
            {
                slot.AddItem(inventoryItem, inventoryListHandler, explicitAmount);
                RefreshInventoryList();
                return true;
            }
        }

        Debug.LogWarning("SlotManager: no free slot available for item.", this);
        return false;
    }

    // Handle Can Add Item.
    public bool CanAddItem(InventoryItem inventoryItem)
    {
        if (inventoryItem == null)
        {
            return false;
        }

        PrepareSlots();

        for (int i = 0; i < slots.Count; i++)
        {
            Slot slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            if (slot.MatchesItem(inventoryItem) || slot.IsEmpty())
            {
                return true;
            }
        }

        return false;
    }

    // Handle Try Consume Resources.
    public bool TryConsumeResources(Dictionary<string, int> requiredResources)
    {
        if (requiredResources == null || requiredResources.Count == 0)
        {
            return true;
        }

        PrepareSlots();

        Dictionary<string, int> availableByName = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < slots.Count; i++)
        {
            Slot slot = slots[i];
            if (slot == null || slot.IsEmpty())
            {
                continue;
            }

            string itemKey = NormalizeItemName(slot.itemName);
            if (string.IsNullOrEmpty(itemKey))
            {
                continue;
            }

            if (availableByName.TryGetValue(itemKey, out int currentAmount))
            {
                availableByName[itemKey] = currentAmount + Mathf.Max(0, slot.count);
            }
            else
            {
                availableByName[itemKey] = Mathf.Max(0, slot.count);
            }
        }

        foreach (KeyValuePair<string, int> requirement in requiredResources)
        {
            if (!availableByName.TryGetValue(requirement.Key, out int availableAmount) || availableAmount < requirement.Value)
            {
                return false;
            }
        }

        foreach (KeyValuePair<string, int> requirement in requiredResources)
        {
            int remaining = Mathf.Max(0, requirement.Value);
            if (remaining <= 0)
            {
                continue;
            }

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                Slot slot = slots[i];
                if (slot == null || slot.IsEmpty())
                {
                    continue;
                }

                if (!string.Equals(NormalizeItemName(slot.itemName), requirement.Key, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int consumed = Mathf.Min(slot.count, remaining);
                slot.count -= consumed;
                remaining -= consumed;

                if (slot.count <= 0)
                {
                    slot.count = 0;
                    slot.sprite = null;
                    slot.itemName = string.Empty;
                    slot.inventoryItemReference = null;
                }

                slot.UpdateUI();
            }
        }

        RefreshInventoryList();
        return true;
    }

    // Handle Prepare Slots.
    public void PrepareSlots()
    {
        if (autoDiscoverSlots)
        {
            Slot[] foundSlots = GetComponentsInChildren<Slot>(true);
            if (foundSlots.Length > 0)
            {
                slots.Clear();
                for (int i = 0; i < foundSlots.Length; i++)
                {
                    if (foundSlots[i] != null)
                    {
                        slots.Add(foundSlots[i]);
                    }
                }
            }
        }

        RemoveNullAndDuplicateSlots();
        LinkSlotsToThisManager();
        SortSlotsTopLeft();
    }

    // Handle Remove Null And Duplicate Slots.
    private void RemoveNullAndDuplicateSlots()
    {
        HashSet<Slot> uniqueSlots = new HashSet<Slot>();
        for (int i = slots.Count - 1; i >= 0; i--)
        {
            Slot slot = slots[i];
            if (slot == null || !uniqueSlots.Add(slot))
            {
                slots.RemoveAt(i);
            }
        }
    }

    // Handle Link Slots To This Manager.
    private void LinkSlotsToThisManager()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null)
            {
                slots[i].slotManager = this;
            }
        }
    }

    // Handle Sort Slots Top Left.
    private void SortSlotsTopLeft()
    {
        slots.Sort(CompareSlotsTopLeft);
    }

    // Handle Compare Slots Top Left.
    private int CompareSlotsTopLeft(Slot a, Slot b)
    {
        if (a == b) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        RectTransform rectA = a.transform as RectTransform;
        RectTransform rectB = b.transform as RectTransform;

        Vector2 posA = rectA != null
            ? rectA.anchoredPosition
            : new Vector2(a.transform.position.x, a.transform.position.y);
        Vector2 posB = rectB != null
            ? rectB.anchoredPosition
            : new Vector2(b.transform.position.x, b.transform.position.y);

        // Top row first (higher Y), then left to right (lower X).
        int yCompare = posB.y.CompareTo(posA.y);
        if (yCompare != 0)
        {
            return yCompare;
        }

        int xCompare = posA.x.CompareTo(posB.x);
        if (xCompare != 0)
        {
            return xCompare;
        }

        return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
    }

    // Handle Refresh Inventory List.
    public void RefreshInventoryList()
    {
        ResolveInventoryListHandler();
        if (inventoryListHandler == null)
        {
            return;
        }

        inventoryListHandler.RebuildFromSlots(slots);
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

    // Handle Resolve Inventory List Handler.
    private void ResolveInventoryListHandler()
    {
        if (inventoryListHandler != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        inventoryListHandler = FindFirstObjectByType<InventoryListHandler>(FindObjectsInactive.Include);
#else
        inventoryListHandler = FindObjectOfType<InventoryListHandler>(true);
#endif
    }
}
