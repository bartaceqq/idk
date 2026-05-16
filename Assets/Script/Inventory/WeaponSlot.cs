using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

        UpdateVisual();
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

    public void OnDrop(PointerEventData eventData)
    {
        SlotInsideUI sourceSlot = ResolveDraggedRemakeSlot(eventData) ?? SlotInsideUI.CurrentDragSource;
        if (sourceSlot == null || !sourceSlot.HasItem())
        {
            return;
        }

        TryAssignFromRemakeSlot(sourceSlot, true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ClearEquippedItem();
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            ToggleAssignedItem();
        }
    }

    public void ClearEquippedItem()
    {
        equippedItemReference = null;
        equippedItemName = string.Empty;
        equippedSprite = null;
        UpdateVisual();
        SyncAllWeaponSlotsToItemSwitch();
    }

    public void RefreshVisual()
    {
        UpdateVisual();
    }

    public static List<WeaponSlot> GetOrderedWeaponSlots()
    {
        PruneNullWeaponSlots();
        List<WeaponSlot> ordered = new List<WeaponSlot>(ActiveWeaponSlots);
        ordered.Sort(CompareSlotsTopLeft);
        return ordered;
    }

    public string GetAssignedItemName()
    {
        string normalized = NormalizeItemName(equippedItemName);
        if (!string.IsNullOrEmpty(normalized))
        {
            return normalized;
        }

        if (equippedItemReference == null)
        {
            return string.Empty;
        }

        normalized = NormalizeItemName(equippedItemReference.nameofitem);
        return !string.IsNullOrEmpty(normalized) ? normalized : NormalizeItemName(equippedItemReference.name);
    }

    private bool TryAssignFromRemakeSlot(SlotInsideUI sourceSlot, bool logWarnings)
    {
        InventoryItem sourceItem = sourceSlot.Item;
        string sourceName = ResolveSlotItemName(sourceSlot);
        if (!CanAcceptItem(sourceItem, sourceName, out string resolvedEquipName, out string reason))
        {
            if (logWarnings)
            {
                Debug.LogWarning($"WeaponSlot: {reason}", this);
            }

            return false;
        }

        Sprite sourceSprite = sourceSlot.image != null ? sourceSlot.image.sprite : null;
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

        bool allowed = IsAllowedItemType(sourceItem, resolvedEquipName);
        if (!allowed)
        {
            reason = sourceItem != null
                ? $"Only configured weapon types are allowed. Received type: {sourceItem.itemType}."
                : "Dragged item does not match allowed weapon categories.";
            return false;
        }

        return true;
    }

    private bool IsAllowedItemType(InventoryItem sourceItem, string resolvedName)
    {
        if (sourceItem != null)
        {
            return (allowToolItems && sourceItem.itemType == InventoryItemType.Tool) ||
                   (allowSwordItems && sourceItem.itemType == InventoryItemType.Sword);
        }

        string mapped = MapCommonWeaponName(resolvedName);
        return string.Equals(mapped, "Sword", System.StringComparison.OrdinalIgnoreCase)
            ? allowSwordItems
            : allowToolItems && !string.IsNullOrEmpty(mapped);
    }

    private void ResolveReferences()
    {
        if (Backgroundimage == null)
        {
            Backgroundimage = GetComponent<Image>();
        }

        if (iconImage != null && iconImage.gameObject == gameObject)
        {
            iconImage = null;
        }

        if (iconImage == null)
        {
            Transform preferred = transform.Find("ImagePlace");
            if (preferred == null) preferred = transform.Find("WhiteInside");
            if (preferred != null) iconImage = preferred.GetComponent<Image>();
        }

        if (iconImage == null)
        {
            Image[] images = GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                Image candidate = images[i];
                if (candidate != null && candidate.gameObject != gameObject && candidate.name != "BlackBakground")
                {
                    iconImage = candidate;
                    break;
                }
            }
        }

        if (itemSwitchScript == null)
        {
            itemSwitchScript = GetComponentInParent<ItemSwitchScript>();
        }

        if (itemSwitchScript == null)
        {
#if UNITY_2023_1_OR_NEWER
            itemSwitchScript = Object.FindAnyObjectByType<ItemSwitchScript>(FindObjectsInactive.Include);
#else
            itemSwitchScript = Object.FindObjectOfType<ItemSwitchScript>(true);
#endif
        }
    }

    private void UpdateVisual()
    {
        ResolveReferences();
        bool visible = IsInventoryVisible();
        bool hasItem = equippedSprite != null;
        bool hasDedicatedSurface = iconImage != null && iconImage.gameObject != gameObject;

        if (Backgroundimage != null)
        {
            if (Backgroundimage.sprite == null)
            {
                Backgroundimage.sprite = FindSharedBackgroundSprite(this);
            }

            Backgroundimage.enabled = visible && Backgroundimage.sprite != null;
            Backgroundimage.raycastTarget = visible && !hasDedicatedSurface;
        }

        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = equippedSprite;
        iconImage.color = hasItem ? Color.white : new Color(1f, 1f, 1f, 0f);
        iconImage.enabled = visible && (hasItem || hasDedicatedSurface || !hideIconWhenEmpty);
        iconImage.raycastTarget = visible && hasDedicatedSurface;
        iconImage.preserveAspect = hasItem;
        HideExtraPlaceholderImages();
    }

    private void HideExtraPlaceholderImages()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image candidate = images[i];
            if (candidate != null && candidate != iconImage && candidate.gameObject != gameObject && candidate.name != "BlackBakground" && candidate.sprite == null)
            {
                candidate.enabled = false;
            }
        }
    }

    private void ToggleAssignedItem()
    {
        string assignedItemName = GetAssignedItemName();
        if (string.IsNullOrEmpty(assignedItemName))
        {
            return;
        }

        ResolveReferences();
        itemSwitchScript?.ToggleItemByName(assignedItemName);
    }

    private bool TryResolveEquipName(InventoryItem sourceItem, string sourceItemName, out string resolvedName)
    {
        resolvedName = string.Empty;
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
            if (itemSwitchScript.HasItemNamed(candidates[i]))
            {
                resolvedName = candidates[i];
                return true;
            }
        }

        for (int i = 0; i < candidates.Count; i++)
        {
            string mapped = MapCommonWeaponName(candidates[i]);
            if (!string.IsNullOrEmpty(mapped) && itemSwitchScript.HasItemNamed(mapped))
            {
                resolvedName = mapped;
                return true;
            }
        }

        return false;
    }

    private static SlotInsideUI ResolveDraggedRemakeSlot(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null)
        {
            return null;
        }

        return eventData.pointerDrag.GetComponent<SlotInsideUI>() ?? eventData.pointerDrag.GetComponentInParent<SlotInsideUI>();
    }

    private static string ResolveSlotItemName(SlotInsideUI sourceSlot)
    {
        if (sourceSlot == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(sourceSlot.nameofslot)) return NormalizeItemName(sourceSlot.nameofslot);
        if (sourceSlot.Item == null) return string.Empty;
        if (!string.IsNullOrWhiteSpace(sourceSlot.Item.nameofitem)) return NormalizeItemName(sourceSlot.Item.nameofitem);
        if (!string.IsNullOrWhiteSpace(sourceSlot.Item.name)) return NormalizeItemName(sourceSlot.Item.name);
        return sourceSlot.Item.itemPrefab != null ? NormalizeItemName(sourceSlot.Item.itemPrefab.name) : string.Empty;
    }

    private static void SyncAllWeaponSlotsToItemSwitch()
    {
        HashSet<string> equippedNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        HashSet<ItemSwitchScript> switchScripts = new HashSet<ItemSwitchScript>();
        PruneNullWeaponSlots();

        for (int i = 0; i < ActiveWeaponSlots.Count; i++)
        {
            WeaponSlot slot = ActiveWeaponSlots[i];
            slot.ResolveReferences();
            if (slot.itemSwitchScript != null)
            {
                switchScripts.Add(slot.itemSwitchScript);
            }

            string assignedName = slot.GetAssignedItemName();
            if (!string.IsNullOrEmpty(assignedName))
            {
                equippedNames.Add(assignedName);
            }
        }

        if (switchScripts.Count == 0)
        {
#if UNITY_2023_1_OR_NEWER
            ItemSwitchScript fallback = Object.FindAnyObjectByType<ItemSwitchScript>(FindObjectsInactive.Include);
#else
            ItemSwitchScript fallback = Object.FindObjectOfType<ItemSwitchScript>(true);
#endif
            if (fallback != null) switchScripts.Add(fallback);
        }

        foreach (ItemSwitchScript switchScript in switchScripts)
        {
            switchScript?.ApplyEquippedItemNames(equippedNames);
        }
    }

    private static Sprite FindSharedBackgroundSprite(WeaponSlot requestingSlot)
    {
        PruneNullWeaponSlots();
        for (int i = 0; i < ActiveWeaponSlots.Count; i++)
        {
            WeaponSlot slot = ActiveWeaponSlots[i];
            if (slot != null && slot != requestingSlot && slot.Backgroundimage != null && slot.Backgroundimage.sprite != null)
            {
                return slot.Backgroundimage.sprite;
            }
        }

        return null;
    }

    private static bool IsInventoryVisible()
    {
        return !Application.isPlaying || InventoryManager.IsInventoryOpen;
    }

    private static void PruneNullWeaponSlots()
    {
        for (int i = ActiveWeaponSlots.Count - 1; i >= 0; i--)
        {
            if (ActiveWeaponSlots[i] == null)
            {
                ActiveWeaponSlots.RemoveAt(i);
            }
        }
    }

    private static void AddCandidateName(List<string> candidates, string rawName)
    {
        string normalized = NormalizeItemName(rawName);
        if (!string.IsNullOrEmpty(normalized) && !candidates.Contains(normalized))
        {
            candidates.Add(normalized);
        }
    }

    private static string NormalizeItemName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        string normalized = rawName.Trim();
        return normalized.EndsWith("(Clone)", System.StringComparison.OrdinalIgnoreCase)
            ? normalized.Substring(0, normalized.Length - "(Clone)".Length).Trim()
            : normalized;
    }

    private static string MapCommonWeaponName(string rawName)
    {
        string token = NormalizeItemName(rawName).Replace(" ", string.Empty).ToLowerInvariant();
        if (token.Contains("pickaxe") || token.Contains("pick")) return "Pickaxe";
        if (token.Contains("sword")) return "Sword";
        if (token.Contains("axe")) return "Axe";
        return string.Empty;
    }

    private static int CompareSlotsTopLeft(WeaponSlot a, WeaponSlot b)
    {
        if (a == b) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        RectTransform rectA = a.transform as RectTransform;
        RectTransform rectB = b.transform as RectTransform;
        Vector2 posA = rectA != null ? rectA.anchoredPosition : new Vector2(a.transform.position.x, a.transform.position.y);
        Vector2 posB = rectB != null ? rectB.anchoredPosition : new Vector2(b.transform.position.x, b.transform.position.y);
        int yCompare = posB.y.CompareTo(posA.y);
        return yCompare != 0 ? yCompare : posA.x.CompareTo(posB.x);
    }
}
