using System.Collections.Generic;
using UnityEngine;

// Controls Item Switch Script behavior.
public class ItemSwitchScript : MonoBehaviour
{
    public List<Item> items = new List<Item>();
    public int currentitemid;
    public string currentitemname;
    public bool requireWeaponSlotAssignment = true;

    public Item item;

    private readonly HashSet<string> equippedItemNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

    private void Update()
    {
        EnsureCurrentSelectionIsAllowed();

        for (int i = 0; i < items.Count; i++)
        {
            Item candidate = items[i];
            if (candidate == null || !Input.GetKeyDown(candidate.key))
            {
                continue;
            }

            if (!CanUseItem(candidate))
            {
                continue;
            }

            SwitchToItem(candidate);
            return;
        }
    }

    // Handle Apply Equipped Item Names.
    public void ApplyEquippedItemNames(IEnumerable<string> names)
    {
        equippedItemNames.Clear();
        if (names != null)
        {
            foreach (string name in names)
            {
                string normalized = NormalizeItemName(name);
                if (!string.IsNullOrEmpty(normalized))
                {
                    equippedItemNames.Add(normalized);
                }
            }
        }

        EnsureCurrentSelectionIsAllowed();
    }

    // Handle Has Item Named.
    public bool HasItemNamed(string itemNameToFind)
    {
        string normalized = NormalizeItemName(itemNameToFind);
        if (string.IsNullOrEmpty(normalized))
        {
            return false;
        }

        for (int i = 0; i < items.Count; i++)
        {
            Item candidate = items[i];
            if (candidate == null)
            {
                continue;
            }

            if (string.Equals(NormalizeItemName(candidate.name), normalized, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // Handle Can Use Item.
    private bool CanUseItem(Item candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        if (!requireWeaponSlotAssignment)
        {
            return true;
        }

        if (equippedItemNames.Count == 0)
        {
            return false;
        }

        string normalized = NormalizeItemName(candidate.name);
        return !string.IsNullOrEmpty(normalized) && equippedItemNames.Contains(normalized);
    }

    // Handle Ensure Current Selection Is Allowed.
    private void EnsureCurrentSelectionIsAllowed()
    {
        if (item == null || CanUseItem(item))
        {
            return;
        }

        SetItemObjectVisible(item, false);
        item = null;
        currentitemid = 0;
        currentitemname = string.Empty;
    }

    // Handle Switch To Item.
    private void SwitchToItem(Item targetItem)
    {
        if (targetItem == null)
        {
            return;
        }

        if (item != null && item != targetItem)
        {
            SetItemObjectVisible(item, false);
        }

        currentitemid = targetItem.ID;
        currentitemname = targetItem.name;
        item = targetItem;
        SetItemObjectVisible(targetItem, true);
    }

    // Handle Set Item Object Visible.
    private static void SetItemObjectVisible(Item targetItem, bool visible)
    {
        if (targetItem == null || targetItem.itemobject == null)
        {
            return;
        }

        Renderer[] renderers = targetItem.itemobject.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = visible;
            }
        }
    }

    // Handle Normalize Item Name.
    private static string NormalizeItemName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        return rawName.Trim();
    }
}
