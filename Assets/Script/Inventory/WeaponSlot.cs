using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Controls Weapon Slot behavior.
public class WeaponSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public Image Backgroundimage;
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
        Slot sourceSlot = ResolveDraggedLegacySlot(eventData);
        if (sourceSlot == null)
        {
            sourceSlot = Slot.CurrentDragSource;
        }

        if (sourceSlot != null && !sourceSlot.IsEmpty())
        {
            TryAssignFromSlot(sourceSlot, true);
            return;
        }

        SlotInsideUI remakeSourceSlot = ResolveDraggedRemakeSlot(eventData);
        if (remakeSourceSlot == null)
        {
            remakeSourceSlot = SlotInsideUI.CurrentDragSource;
        }

        if (remakeSourceSlot == null || !remakeSourceSlot.HasItem())
        {
            return;
        }

        TryAssignFromRemakeSlot(remakeSourceSlot, true);
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

    // Handle Refresh Visual.
    public void RefreshVisual()
    {
        UpdateVisual();
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
        if (!CanAcceptItem(sourceItem, sourceName, out string resolvedEquipName, out string rejectReason))
        {
            if (logWarnings)
            {
                Debug.LogWarning($"WeaponSlot: {rejectReason}", this);
            }

            return false;
        }

        equippedItemReference = sourceItem;
        equippedItemName = resolvedEquipName;
        equippedSprite = sourceSlot.sprite;
        UpdateVisual();
        SyncAllWeaponSlotsToItemSwitch();
        return true;
    }

    // Handle Try Assign From Remake Slot.
    private bool TryAssignFromRemakeSlot(SlotInsideUI sourceSlot, bool logWarnings)
    {
        if (sourceSlot == null || !sourceSlot.HasItem())
        {
            return false;
        }

        InventoryItem sourceItem = sourceSlot.Item;
        string sourceName = ResolveRemakeSlotItemName(sourceSlot);
        if (!CanAcceptItem(sourceItem, sourceName, out string resolvedEquipName, out string rejectReason))
        {
            if (logWarnings)
            {
                Debug.LogWarning($"WeaponSlot: {rejectReason}", this);
            }

            return false;
        }

        Sprite sourceSprite = sourceSlot.image != null
            ? sourceSlot.image.sprite
            : null;
        if (sourceSprite == null && sourceItem != null)
        {
            sourceSprite = sourceItem.inventorysprite;
        }

        equippedItemReference = sourceItem;
        equippedItemName = resolvedEquipName;
        equippedSprite = sourceSprite;
        UpdateVisual();
        SyncAllWeaponSlotsToItemSwitch();
        return true;
    }

    // Handle Can Accept Item.
    private bool CanAcceptItem(InventoryItem sourceItem, string sourceItemName, out string resolvedEquipName, out string reason)
    {
        resolvedEquipName = string.Empty;
        reason = string.Empty;

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

        if (!TryResolveEquipName(sourceItem, sourceItemName, out resolvedEquipName))
        {
            reason = $"No Item entry with name '{sourceItemName}' exists in ItemSwitchScript.";
            return false;
        }

        bool typeAllowed;
        if (sourceItem != null)
        {
            bool isTool = sourceItem.itemType == InventoryItemType.Tool;
            bool isSword = sourceItem.itemType == InventoryItemType.Sword;
            typeAllowed = (allowToolItems && isTool) || (allowSwordItems && isSword);
        }
        else
        {
            typeAllowed = IsAllowedByResolvedName(resolvedEquipName);
        }

        if (!typeAllowed)
        {
            reason = sourceItem != null
                ? $"Only configured weapon types are allowed. Received type: {sourceItem.itemType}."
                : "Dragged item does not match allowed weapon categories.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(resolvedEquipName))
        {
            reason = "Resolved equip name is empty.";
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
            Transform preferred = transform.Find("ImagePlace");
            if (preferred == null)
            {
                preferred = transform.Find("WhiteInside");
            }

            if (preferred != null)
            {
                iconImage = preferred.GetComponent<Image>();
            }
        }

        if (iconImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i] != null && images[i].gameObject != gameObject)
                {
                    if (images[i].name == "BlackBakground")
                    {
                        continue;
                    }

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
        iconImage.enabled = hasItem || !hideIconWhenEmpty;
        HideExtraPlaceholderImages();
    }

    // Handle Hide Extra Placeholder Images.
    private void HideExtraPlaceholderImages()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image candidate = images[i];
            if (candidate == null || candidate == iconImage || candidate.gameObject == gameObject)
            {
                continue;
            }

            if (candidate.name == "BlackBakground")
            {
                continue;
            }

            if (candidate.sprite == null)
            {
                candidate.enabled = false;
            }
        }
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

        string normalized = rawName.Trim();
        if (normalized.EndsWith("(Clone)", System.StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - "(Clone)".Length).Trim();
        }

        return normalized;
    }

    // Handle Resolve Remake Slot Item Name.
    private static string ResolveRemakeSlotItemName(SlotInsideUI sourceSlot)
    {
        if (sourceSlot == null)
        {
            return string.Empty;
        }

        string slotName = NormalizeItemName(sourceSlot.nameofslot);
        if (!string.IsNullOrEmpty(slotName))
        {
            return slotName;
        }

        if (sourceSlot.Item == null)
        {
            return string.Empty;
        }

        string itemName = NormalizeItemName(sourceSlot.Item.nameofitem);
        if (!string.IsNullOrEmpty(itemName))
        {
            return itemName;
        }

        string objectName = NormalizeItemName(sourceSlot.Item.name);
        if (!string.IsNullOrEmpty(objectName))
        {
            return objectName;
        }

        if (sourceSlot.Item.itemPrefab != null)
        {
            return NormalizeItemName(sourceSlot.Item.itemPrefab.name);
        }

        return string.Empty;
    }

    // Handle Try Resolve Equip Name.
    private bool TryResolveEquipName(InventoryItem sourceItem, string sourceItemName, out string resolvedName)
    {
        resolvedName = string.Empty;
        ResolveReferences();
        if (itemSwitchScript == null)
        {
            return false;
        }

        List<string> candidates = new List<string>(8);
        AddCandidateName(candidates, sourceItemName);

        if (sourceItem != null)
        {
            AddCandidateName(candidates, sourceItem.nameofitem);
            AddCandidateName(candidates, sourceItem.name);

            if (sourceItem.itemPrefab != null)
            {
                AddCandidateName(candidates, sourceItem.itemPrefab.name);
            }
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            string candidate = candidates[i];
            if (itemSwitchScript.HasItemNamed(candidate))
            {
                resolvedName = candidate;
                return true;
            }
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            string mapped = MapCommonWeaponName(candidates[i]);
            if (string.IsNullOrEmpty(mapped))
            {
                continue;
            }

            if (itemSwitchScript.HasItemNamed(mapped))
            {
                resolvedName = mapped;
                return true;
            }
        }

        return false;
    }

    // Handle Add Candidate Name.
    private static void AddCandidateName(List<string> candidates, string rawName)
    {
        string normalized = NormalizeItemName(rawName);
        if (string.IsNullOrEmpty(normalized) || candidates.Contains(normalized))
        {
            return;
        }

        candidates.Add(normalized);
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
        if (token.Contains("pickaxe") || token.Contains("pick"))
        {
            return "Pickaxe";
        }

        if (token.Contains("sword"))
        {
            return "Sword";
        }

        if (token.Contains("axe"))
        {
            return "Axe";
        }

        return string.Empty;
    }

    // Handle Is Allowed By Resolved Name.
    private bool IsAllowedByResolvedName(string resolvedName)
    {
        string mapped = MapCommonWeaponName(resolvedName);
        if (string.IsNullOrEmpty(mapped))
        {
            return false;
        }

        if (string.Equals(mapped, "Sword", System.StringComparison.OrdinalIgnoreCase))
        {
            return allowSwordItems;
        }

        return allowToolItems;
    }

    // Handle Resolve Dragged Legacy Slot.
    private static Slot ResolveDraggedLegacySlot(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null)
        {
            return null;
        }

        Slot direct = eventData.pointerDrag.GetComponent<Slot>();
        if (direct != null)
        {
            return direct;
        }

        return eventData.pointerDrag.GetComponentInParent<Slot>();
    }

    // Handle Resolve Dragged Remake Slot.
    private static SlotInsideUI ResolveDraggedRemakeSlot(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null)
        {
            return null;
        }

        SlotInsideUI direct = eventData.pointerDrag.GetComponent<SlotInsideUI>();
        if (direct != null)
        {
            return direct;
        }

        return eventData.pointerDrag.GetComponentInParent<SlotInsideUI>();
    }
}
