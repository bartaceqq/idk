using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Controls Weapon Slot behavior.
public class WeaponSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public ItemSwitchScript itemSwitchScript;
    public Image iconImage;
    public bool allowToolItems = true;
    public bool allowSwordItems = true;
    public bool hideIconWhenEmpty = true;

    public InventoryItem equippedItemReference;
    public string equippedItemName;
    public Sprite equippedSprite;

    private static readonly List<WeaponSlot> ActiveWeaponSlots = new List<WeaponSlot>();

    private void Awake()
    {
        ResolveReferences();
        UpdateVisual();
    }

    private void OnEnable()
    {
        if (!ActiveWeaponSlots.Contains(this))
        {
            ActiveWeaponSlots.Add(this);
        }

        SyncAllWeaponSlotsToItemSwitch();
    }

    private void OnDisable()
    {
        ActiveWeaponSlots.Remove(this);
        SyncAllWeaponSlotsToItemSwitch();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ResolveReferences();
            UpdateVisual();
        }
    }

    // Handle On Drop.
    public void OnDrop(PointerEventData eventData)
    {
        Slot sourceSlot = Slot.CurrentDragSource;
        if (sourceSlot == null || sourceSlot.IsEmpty())
        {
            return;
        }

        TryAssignFromSlot(sourceSlot, true);
    }

    // Handle On Pointer Click.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button == PointerEventData.InputButton.Right)
        {
            ClearEquippedItem();
        }
    }

    // Handle Clear Equipped Item.
    public void ClearEquippedItem()
    {
        equippedItemReference = null;
        equippedItemName = string.Empty;
        equippedSprite = null;
        UpdateVisual();
        SyncAllWeaponSlotsToItemSwitch();
    }

    // Handle Try Assign From Slot.
    private bool TryAssignFromSlot(Slot sourceSlot, bool logWarnings)
    {
        if (sourceSlot == null || sourceSlot.IsEmpty())
        {
            return false;
        }

        InventoryItem sourceItem = ResolveInventoryItemFromSlot(sourceSlot);
        string sourceName = NormalizeItemName(sourceSlot.itemName);
        if (!CanAcceptItem(sourceItem, sourceName, out string rejectReason))
        {
            if (logWarnings)
            {
                Debug.LogWarning($"WeaponSlot: {rejectReason}", this);
            }

            return false;
        }

        equippedItemReference = sourceItem;
        equippedItemName = sourceName;
        equippedSprite = sourceSlot.sprite;
        UpdateVisual();
        SyncAllWeaponSlotsToItemSwitch();
        return true;
    }

    // Handle Can Accept Item.
    private bool CanAcceptItem(InventoryItem sourceItem, string sourceItemName, out string reason)
    {
        reason = string.Empty;

        if (sourceItem == null)
        {
            reason = "Dragged slot has no InventoryItem reference.";
            return false;
        }

        bool isTool = sourceItem.itemType == InventoryItemType.Tool;
        bool isSword = sourceItem.itemType == InventoryItemType.Sword;
        bool typeAllowed = (allowToolItems && isTool) || (allowSwordItems && isSword);
        if (!typeAllowed)
        {
            reason = $"Only configured weapon types are allowed. Received type: {sourceItem.itemType}.";
            return false;
        }

        ResolveReferences();
        if (itemSwitchScript == null)
        {
            reason = "ItemSwitchScript reference is missing.";
            return false;
        }

        if (string.IsNullOrEmpty(sourceItemName))
        {
            reason = "Dragged item name is empty.";
            return false;
        }

        if (!itemSwitchScript.HasItemNamed(sourceItemName))
        {
            reason = $"No Item entry with name '{sourceItemName}' exists in ItemSwitchScript.";
            return false;
        }

        return true;
    }

    // Handle Resolve Inventory Item From Slot.
    private static InventoryItem ResolveInventoryItemFromSlot(Slot sourceSlot)
    {
        if (sourceSlot == null)
        {
            return null;
        }

        if (sourceSlot.inventoryItemReference != null)
        {
            return sourceSlot.inventoryItemReference;
        }

        string sourceName = NormalizeItemName(sourceSlot.itemName);
        if (string.IsNullOrEmpty(sourceName))
        {
            return null;
        }

#if UNITY_2023_1_OR_NEWER
        InventoryItem[] allItems = Object.FindObjectsByType<InventoryItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        InventoryItem[] allItems = Object.FindObjectsOfType<InventoryItem>(true);
#endif

        for (int i = 0; i < allItems.Length; i++)
        {
            InventoryItem candidate = allItems[i];
            if (candidate == null)
            {
                continue;
            }

            if (string.Equals(NormalizeItemName(candidate.name), sourceName, System.StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }

    // Handle Resolve References.
    private void ResolveReferences()
    {
        if (iconImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].gameObject != gameObject)
                {
                    iconImage = images[i];
                    break;
                }
            }
        }

        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }

        if (itemSwitchScript == null)
        {
            itemSwitchScript = GetComponentInParent<ItemSwitchScript>();
        }

        if (itemSwitchScript == null)
        {
#if UNITY_2023_1_OR_NEWER
            itemSwitchScript = Object.FindFirstObjectByType<ItemSwitchScript>(FindObjectsInactive.Include);
#else
            itemSwitchScript = Object.FindObjectOfType<ItemSwitchScript>(true);
#endif
        }
    }

    // Handle Update Visual.
    private void UpdateVisual()
    {
        ResolveReferences();
        if (iconImage == null)
        {
            return;
        }

        bool hasItem = equippedSprite != null;
        iconImage.sprite = equippedSprite;

        bool isRootImage = iconImage.gameObject == gameObject;
        if (!hasItem && hideIconWhenEmpty && !isRootImage)
        {
            iconImage.enabled = false;
            return;
        }

        iconImage.enabled = true;
    }

    // Handle Sync All Weapon Slots To Item Switch.
    private static void SyncAllWeaponSlotsToItemSwitch()
    {
        HashSet<string> equippedNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        HashSet<ItemSwitchScript> switchScripts = new HashSet<ItemSwitchScript>();

        for (int i = ActiveWeaponSlots.Count - 1; i >= 0; i--)
        {
            WeaponSlot slot = ActiveWeaponSlots[i];
            if (slot == null)
            {
                ActiveWeaponSlots.RemoveAt(i);
                continue;
            }

            slot.ResolveReferences();
            if (slot.itemSwitchScript != null)
            {
                switchScripts.Add(slot.itemSwitchScript);
            }

            string normalizedName = NormalizeItemName(slot.equippedItemName);
            if (!string.IsNullOrEmpty(normalizedName))
            {
                equippedNames.Add(normalizedName);
            }
        }

        if (switchScripts.Count == 0)
        {
#if UNITY_2023_1_OR_NEWER
            ItemSwitchScript fallback = Object.FindFirstObjectByType<ItemSwitchScript>(FindObjectsInactive.Include);
#else
            ItemSwitchScript fallback = Object.FindObjectOfType<ItemSwitchScript>(true);
#endif

            if (fallback != null)
            {
                switchScripts.Add(fallback);
            }
        }

        foreach (ItemSwitchScript switchScript in switchScripts)
        {
            if (switchScript != null)
            {
                switchScript.ApplyEquippedItemNames(equippedNames);
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
