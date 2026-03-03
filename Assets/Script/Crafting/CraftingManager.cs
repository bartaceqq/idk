using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public List<CraftableSlot> slots = new List<CraftableSlot>();
    public List<CraftableItem> items = new List<CraftableItem>();
    public LevelingManager levelingManager;

    private bool _checkQueued;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RefreshLists();
        QueueCheck();
        foreach (CraftableSlot slot in slots)
        {
            if (!slot.occupied)
            {
                slot.imageslot.enabled = false;
                slot.background.enabled = false;
            }
        }
    }
    // Update is called once per frame
    void Update()
    {

    }

    private void RefreshLists()
    {
        slots.RemoveAll(slot => slot == null);
        items.RemoveAll(item => item == null);

        if (slots.Count == 0)
        {
            slots.AddRange(GetComponentsInChildren<CraftableSlot>(true));
        }

        if (items.Count == 0)
        {
            items.AddRange(GetComponentsInChildren<CraftableItem>(true));
        }
    }

    private void QueueCheck()
    {
        if (_checkQueued)
        {
            return;
        }

        _checkQueued = true;
        StartCoroutine(DelayedCheck());
    }

    private System.Collections.IEnumerator DelayedCheck()
    {
        yield return null;
        _checkQueued = false;
        Check();
    }

    public void Check()
    {
        if (items.Count == 0)
        {
            Debug.LogWarning("CraftingManager: No craftable items assigned or found.");
            return;
        }

        if (slots.Count == 0)
        {
            Debug.LogWarning("CraftingManager: No craftable slots assigned or found.");
            return;
        }

        foreach (CraftableItem item in items)
        {
            if (item == null)
            {
                continue;
            }

            if (!item.placed)
            {
                CraftableSlot slot = GetLowestAvailableSlot();
                if (slot == null)
                {
                    break;
                }

                item.placed = true;
                slot.AddCraftableItem(item);
                slot.imageslot.enabled = true;
                slot.background.enabled = true;
                item.slotnumber = slot.slotnumber;
            }
        }
    }

    private CraftableSlot GetLowestAvailableSlot()
    {

        CraftableSlot best = null;
        foreach (CraftableSlot slot in slots)
        {
            if (slot.occupied)
            {
                continue;
            }

            if (best == null || slot.slotnumber < best.slotnumber)
            {
                best = slot;
            }
        }
        Debug.Log("lowest number is: " + best);
        return best;
    }

}
