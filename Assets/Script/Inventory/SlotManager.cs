using System.Collections.Generic;
using UnityEngine;

public class SlotManager : MonoBehaviour
{
    public List<Slot> slots = new List<Slot>();

    public void AddItem(InventoryItem inventoryItem)
    {
        if (inventoryItem == null)
        {
            return;
        }

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
}
