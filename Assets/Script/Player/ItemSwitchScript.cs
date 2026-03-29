using System.Collections;
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
    private Coroutine _pendingSwordTransition;
    private bool _isSwordTransitioning;
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
        EnsureCurrentSwordTrailEffect();

        if (_isSwordTransitioning)
        {
            return;
        }

        if (GameplayUiState.IsGameplayInputBlocked)
        {
            return;
        }

        ActionScript resolvedActionScript = ResolveActionScript();
        if (resolvedActionScript != null &&
            (resolvedActionScript.IsGameplayInputLocked() || resolvedActionScript.IsSwordBlockActive()))
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
            if (ShouldReserveSwordSpecialHotkey(WeaponSlotHotkeys[i]))
            {
                continue;
            }

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
        if (targetItem == null || _isSwordTransitioning || targetItem == item)
        {
            return;
        }

        Item previousItem = item;
        bool previousIsSword = IsSwordItem(previousItem);
        bool targetIsSword = IsSwordItem(targetItem);

        if (previousItem != null && previousItem != targetItem && previousIsSword)
        {
            StartSwordTransition(previousItem, targetItem);
            return;
        }

        CancelPendingSwordTransition();

        if (previousItem != null && previousItem != targetItem)
        {
            ActionScript resolvedActionScript = ResolveActionScript();
            resolvedActionScript?.CancelUpperBodyAction();
            resolvedActionScript?.CancelSwordBlock();
            SetItemObjectVisible(previousItem, false);
        }

        ActivateItemImmediate(targetItem);
        if (targetIsSword)
        {
            EnsureSwordTrailEffect(targetItem);
            ResolveActionScript()?.TryEquipSword();
        }
    }

    // Handle Unequip Current Item.
    private void UnequipCurrentItem()
    {
        if (_isSwordTransitioning)
        {
            return;
        }

        if (item != null && IsSwordItem(item))
        {
            StartSwordTransition(item, null);
            return;
        }

        CancelPendingSwordTransition();

        if (item != null)
        {
            ActionScript resolvedActionScript = ResolveActionScript();
            resolvedActionScript?.CancelUpperBodyAction();
            resolvedActionScript?.CancelSwordBlock();
            SetItemObjectVisible(item, false);
        }

        ClearCurrentSelection();
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

    // Handle Ensure Current Sword Trail Effect.
    private void EnsureCurrentSwordTrailEffect()
    {
        EnsureSwordTrailEffect(item);
    }

    // Handle Ensure Sword Trail Effect.
    private void EnsureSwordTrailEffect(Item targetItem)
    {
        if (targetItem == null || targetItem.itemobject == null)
        {
            return;
        }

        string mappedName = MapCommonWeaponName(targetItem.name);
        if (!string.Equals(mappedName, "Sword", System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (targetItem.itemobject.GetComponent<SwordTrailEffect>() == null)
        {
            targetItem.itemobject.AddComponent<SwordTrailEffect>();
        }
    }

    // Handle Start Sword Transition.
    private void StartSwordTransition(Item swordItem, Item nextItem)
    {
        if (swordItem == null)
        {
            return;
        }

        CancelPendingSwordTransition();
        _pendingSwordTransition = StartCoroutine(PlaySwordHideTransition(swordItem, nextItem));
    }

    // Handle Cancel Pending Sword Transition.
    private void CancelPendingSwordTransition()
    {
        if (_pendingSwordTransition == null)
        {
            return;
        }

        StopCoroutine(_pendingSwordTransition);
        _pendingSwordTransition = null;
        _isSwordTransitioning = false;
    }

    // Handle Play Sword Hide Transition.
    private IEnumerator PlaySwordHideTransition(Item swordItem, Item nextItem)
    {
        _isSwordTransitioning = true;

        ActionScript resolvedActionScript = ResolveActionScript();
        resolvedActionScript?.CancelUpperBodyAction();
        resolvedActionScript?.CancelSwordBlock();

        bool playedHide = resolvedActionScript != null && resolvedActionScript.TryUnequipSword();
        if (playedHide && resolvedActionScript != null)
        {
            float timeoutAt = Time.time + Mathf.Max(0.2f, resolvedActionScript.GetRemainingGameplayInputLockSeconds() + 0.25f);
            while (resolvedActionScript.IsGameplayInputLocked() && Time.time < timeoutAt)
            {
                yield return null;
            }
        }

        SetItemObjectVisible(swordItem, false);
        ClearCurrentSelection();

        if (nextItem != null)
        {
            ActivateItemImmediate(nextItem);
            if (IsSwordItem(nextItem))
            {
                EnsureSwordTrailEffect(nextItem);
                ResolveActionScript()?.TryEquipSword();
            }
        }

        _isSwordTransitioning = false;
        _pendingSwordTransition = null;
    }

    // Handle Activate Item Immediate.
    private void ActivateItemImmediate(Item targetItem)
    {
        if (targetItem == null)
        {
            return;
        }

        currentitemid = targetItem.ID;
        currentitemname = targetItem.name;
        item = targetItem;
        SetItemObjectVisible(targetItem, true);
    }

    // Handle Clear Current Selection.
    private void ClearCurrentSelection()
    {
        item = null;
        currentitemid = 0;
        currentitemname = string.Empty;
    }

    // Handle Is Sword Item.
    private bool IsSwordItem(Item candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        return string.Equals(
            MapCommonWeaponName(candidate.name),
            "Sword",
            System.StringComparison.OrdinalIgnoreCase);
    }

    // Handle Normalize Item Name.
    private static string NormalizeItemName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        string normalized = rawName.Trim();
        if (normalized.EndsWith("(Clone)", System.StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - "(Clone)".Length).Trim();
        }

        return normalized;
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

    // Handle Should Reserve Sword Special Hotkey.
    private bool ShouldReserveSwordSpecialHotkey(KeyCode key)
    {
        if (key != KeyCode.Alpha3 &&
            key != KeyCode.Alpha4 &&
            key != KeyCode.Alpha5)
        {
            return false;
        }

        return IsSwordEquipped();
    }

    // Handle Is Sword Equipped.
    private bool IsSwordEquipped()
    {
        string equippedName = NormalizeItemName(currentitemname);
        if (string.IsNullOrEmpty(equippedName) && item != null)
        {
            equippedName = NormalizeItemName(item.name);
        }

        if (!string.IsNullOrEmpty(equippedName))
        {
            string mappedName = MapCommonWeaponName(equippedName);
            if (!string.IsNullOrEmpty(mappedName))
            {
                return string.Equals(mappedName, "Sword", System.StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(equippedName, "Sword", System.StringComparison.OrdinalIgnoreCase);
        }

        return currentitemid == 3;
    }

    // Handle Map Common Weapon Name.
    private static string MapCommonWeaponName(string rawName)
    {
        string normalized = NormalizeItemName(rawName);
        if (string.IsNullOrEmpty(normalized))
        {
            return string.Empty;
        }

        string token = normalized.Replace(" ", string.Empty).ToLowerInvariant();
        if (token.Contains("sword"))
        {
            return "Sword";
        }

        if (token.Contains("pickaxe") || token.Contains("pick"))
        {
            return "Pickaxe";
        }

        if (token.Contains("axe"))
        {
            return "Axe";
        }

        return string.Empty;
    }
}
