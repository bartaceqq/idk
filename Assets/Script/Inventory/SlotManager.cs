using System.Collections.Generic;
using UnityEngine;

// Controls Slot Manager behavior.
public class SlotManager : MonoBehaviour
{
    [SerializeField] private bool autoDiscoverSlots = true;
    public List<Slot> slots = new List<Slot>();

    // Initialize references before gameplay starts.
    private void Awake()
    {
        PrepareSlots();
    }

    // Run in the editor when values change in Inspector.
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
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
    public void AddItem(InventoryItem inventoryItem)
    {
        if (inventoryItem == null)
        {
            return;
        }

        // Always sort first so "first empty" is stable (top-left first).
        PrepareSlots();

        // 1) Stack into an existing slot with the same item.
        foreach (Slot slot in slots)
        {
            if (slot != null && slot.MatchesItem(inventoryItem))
            {
                slot.AddItem(inventoryItem);
                return;
            }
        }

        // 2) Otherwise place into the first empty slot.
        foreach (Slot slot in slots)
        {
            if (slot != null && slot.sprite == null)
            {
                slot.AddItem(inventoryItem);
                return;
            }
        }

        Debug.LogWarning("SlotManager: no free slot available for item.", this);
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
}
