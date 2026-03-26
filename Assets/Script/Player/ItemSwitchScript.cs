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
    private ActionScript _actionScript;
    private static readonly KeyCode[] WeaponSlotHotkeys =
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4,
        KeyCode.Alpha5,
        KeyCode.Alpha6,
        KeyCode.Alpha7,
        KeyCode.Alpha8,
        KeyCode.Alpha9
    };

    private void Update()
    {
        if (GameplayUiState.IsGameplayInputBlocked)
        {
            return;
        }

        EnsureCurrentSelectionIsAllowed();

        if (requireWeaponSlotAssignment)
        {
            if (TryHandleWeaponSlotHotkeys())
            {
                return;
            }

            return;
        }

        TryHandleLegacyItemHotkeys();
    }

    // Handle Try Handle Weapon Slot Hotkeys.
    private bool TryHandleWeaponSlotHotkeys()
    {
        List<WeaponSlot> orderedSlots = WeaponSlot.GetOrderedWeaponSlots();
        int slotCount = Mathf.Min(orderedSlots.Count, WeaponSlotHotkeys.Length);
        for (int i = 0; i < slotCount; i++)
        {
            if (!Input.GetKeyDown(WeaponSlotHotkeys[i]))
            {
                continue;
            }

            WeaponSlot slot = orderedSlots[i];
            if (slot == null)
            {
                return true;
            }

            string slotItemName = slot.GetAssignedItemName();
            if (string.IsNullOrEmpty(slotItemName))
            {
                UnequipCurrentItem();
                return true;
            }

            ToggleItemByName(slotItemName);
            return true;
        }

        return false;
    }

    // Handle Try Handle Legacy Item Hotkeys.
    private void TryHandleLegacyItemHotkeys()
    {
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

    // Handle Toggle Item By Name.
    public void ToggleItemByName(string itemNameToToggle)
    {
        string normalized = NormalizeItemName(itemNameToToggle);
        if (string.IsNullOrEmpty(normalized))
        {
            return;
        }

        if (string.Equals(NormalizeItemName(currentitemname), normalized, System.StringComparison.OrdinalIgnoreCase))
        {
            UnequipCurrentItem();
            return;
        }

        if (!TryResolveItemByName(normalized, out Item resolvedItem) || !CanUseItem(resolvedItem))
        {
            return;
        }

        SwitchToItem(resolvedItem);
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

        UnequipCurrentItem();
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
            ResolveActionScript()?.CancelUpperBodyAction();
            SetItemObjectVisible(item, false);
        }

        currentitemid = targetItem.ID;
        currentitemname = targetItem.name;
        item = targetItem;
        SetItemObjectVisible(targetItem, true);
    }

    // Handle Unequip Current Item.
    private void UnequipCurrentItem()
    {
        if (item != null)
        {
            ResolveActionScript()?.CancelUpperBodyAction();
            SetItemObjectVisible(item, false);
        }

        item = null;
        currentitemid = 0;
        currentitemname = string.Empty;
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

    // Handle Try Resolve Item By Name.
    private bool TryResolveItemByName(string itemNameToResolve, out Item resolvedItem)
    {
        resolvedItem = null;
        string normalized = NormalizeItemName(itemNameToResolve);
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

            if (!string.Equals(NormalizeItemName(candidate.name), normalized, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            resolvedItem = candidate;
            return true;
        }

        return false;
    }

    // Handle Resolve Action Script.
    private ActionScript ResolveActionScript()
    {
        if (_actionScript != null)
        {
            return _actionScript;
        }

        _actionScript = GetComponent<ActionScript>();
        if (_actionScript != null)
        {
            return _actionScript;
        }

        _actionScript = GetComponentInParent<ActionScript>();
        if (_actionScript != null)
        {
            return _actionScript;
        }

#if UNITY_2023_1_OR_NEWER
        _actionScript = FindFirstObjectByType<ActionScript>(FindObjectsInactive.Include);
#else
        _actionScript = FindObjectOfType<ActionScript>(true);
#endif

        return _actionScript;
    }
}
