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

    private static readonly List<WeaponSlot> ActiveSlots = new List<WeaponSlot>();

    private void Awake()
    {
        ResolveReferences();
        UpdateVisual();
    }

    private void OnEnable()
    {
        if (!ActiveSlots.Contains(this))
        {
            ActiveSlots.Add(this);
        }

        UpdateVisual();
        SyncAllWeaponSlotsToItemSwitch();
    }

    private void OnDisable()
    {
        ActiveSlots.Remove(this);
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
        SlotInsideUI source = GetDraggedSlot(eventData) ?? SlotInsideUI.CurrentDragSource;
        if (source != null && source.HasItem())
        {
            AssignFromInventorySlot(source);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null) return;
        if (eventData.button == PointerEventData.InputButton.Right) ClearEquippedItem();
        if (eventData.button == PointerEventData.InputButton.Left) ToggleAssignedItem();
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

    public string GetAssignedItemName()
    {
        string directName = NormalizeName(equippedItemName);
        if (directName.Length > 0) return directName;
        return equippedItemReference != null ? NormalizeName(BestInventoryName(equippedItemReference)) : string.Empty;
    }

    public static List<WeaponSlot> GetOrderedWeaponSlots()
    {
        ActiveSlots.RemoveAll(slot => slot == null);
        List<WeaponSlot> ordered = new List<WeaponSlot>(ActiveSlots);
        ordered.Sort(CompareTopLeft);
        return ordered;
    }

    private void AssignFromInventorySlot(SlotInsideUI source)
    {
        ResolveReferences();
        InventoryItem sourceItem = source.Item;
        string sourceName = BestSlotName(source);

        if (itemSwitchScript == null || !TryResolveEquipName(sourceItem, sourceName, out string equipName))
        {
            Debug.LogWarning($"WeaponSlot: No matching ItemSwitchScript item found for '{sourceName}'.", this);
            return;
        }

        if (!IsAllowed(sourceItem, equipName))
        {
            Debug.LogWarning($"WeaponSlot: '{sourceName}' is not allowed in this slot.", this);
            return;
        }

        equippedItemReference = sourceItem;
        equippedItemName = equipName;
        equippedSprite = source.image != null && source.image.sprite != null
            ? source.image.sprite
            : sourceItem != null ? sourceItem.inventorysprite : null;

        UpdateVisual();
        SyncAllWeaponSlotsToItemSwitch();
    }

    private bool TryResolveEquipName(InventoryItem item, string sourceName, out string equipName)
    {
        equipName = string.Empty;
        string[] names =
        {
            sourceName,
            item != null ? item.nameofitem : string.Empty,
            item != null ? item.name : string.Empty,
            item != null && item.itemPrefab != null ? item.itemPrefab.name : string.Empty,
            MapCommonWeaponName(sourceName)
        };

        foreach (string rawName in names)
        {
            string name = NormalizeName(rawName);
            if (name.Length > 0 && itemSwitchScript.HasItemNamed(name))
            {
                equipName = name;
                return true;
            }
        }

        return false;
    }

    private bool IsAllowed(InventoryItem item, string resolvedName)
    {
        if (item != null)
        {
            return (allowToolItems && item.itemType == InventoryItemType.Tool) ||
                   (allowSwordItems && item.itemType == InventoryItemType.Sword);
        }

        string mapped = MapCommonWeaponName(resolvedName);
        return mapped == "Sword" ? allowSwordItems : allowToolItems && mapped.Length > 0;
    }

    private void ToggleAssignedItem()
    {
        string itemName = GetAssignedItemName();
        if (itemName.Length == 0) return;
        ResolveReferences();
        itemSwitchScript?.ToggleItemByName(itemName);
    }

    private void ResolveReferences()
    {
        if (Backgroundimage == null) Backgroundimage = GetComponent<Image>();
        if (iconImage == null || iconImage.gameObject == gameObject) iconImage = FindIconImage();
        if (itemSwitchScript == null) itemSwitchScript = GetComponentInParent<ItemSwitchScript>();
        if (itemSwitchScript == null) itemSwitchScript = FindItemSwitchScript();
    }

    private Image FindIconImage()
    {
        Transform namedIcon = transform.Find("ImagePlace") ?? transform.Find("WhiteInside");
        if (namedIcon != null && namedIcon.TryGetComponent(out Image image)) return image;

        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            if (image != null && image.gameObject != gameObject && image.name != "BlackBakground")
            {
                return image;
            }
        }

        return null;
    }

    private void UpdateVisual()
    {
        ResolveReferences();
        bool visible = !Application.isPlaying || InventoryManager.IsInventoryOpen;
        bool hasItem = equippedSprite != null;
        bool separateIcon = iconImage != null && iconImage.gameObject != gameObject;

        if (Backgroundimage != null)
        {
            if (Backgroundimage.sprite == null) Backgroundimage.sprite = FindSharedBackgroundSprite(this);
            Backgroundimage.enabled = visible && Backgroundimage.sprite != null;
            Backgroundimage.raycastTarget = visible && !separateIcon;
        }

        if (iconImage == null) return;
        iconImage.sprite = equippedSprite;
        iconImage.color = hasItem ? Color.white : new Color(1f, 1f, 1f, 0f);
        iconImage.enabled = visible && (hasItem || separateIcon || !hideIconWhenEmpty);
        iconImage.raycastTarget = visible && separateIcon;
        iconImage.preserveAspect = hasItem;
        HideUnusedPlaceholderImages();
    }

    private void HideUnusedPlaceholderImages()
    {
        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            if (image != null && image != iconImage && image.gameObject != gameObject && image.name != "BlackBakground" && image.sprite == null)
            {
                image.enabled = false;
            }
        }
    }

    private static void SyncAllWeaponSlotsToItemSwitch()
    {
        HashSet<string> equippedNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        HashSet<ItemSwitchScript> switches = new HashSet<ItemSwitchScript>();

        foreach (WeaponSlot slot in GetOrderedWeaponSlots())
        {
            slot.ResolveReferences();
            if (slot.itemSwitchScript != null) switches.Add(slot.itemSwitchScript);
            string name = slot.GetAssignedItemName();
            if (name.Length > 0) equippedNames.Add(name);
        }

        if (switches.Count == 0)
        {
            ItemSwitchScript fallback = FindItemSwitchScript();
            if (fallback != null) switches.Add(fallback);
        }

        foreach (ItemSwitchScript switchScript in switches)
        {
            switchScript?.ApplyEquippedItemNames(equippedNames);
        }
    }

    private static SlotInsideUI GetDraggedSlot(PointerEventData eventData)
    {
        return eventData != null && eventData.pointerDrag != null
            ? eventData.pointerDrag.GetComponent<SlotInsideUI>() ?? eventData.pointerDrag.GetComponentInParent<SlotInsideUI>()
            : null;
    }

    private static ItemSwitchScript FindItemSwitchScript()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindAnyObjectByType<ItemSwitchScript>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<ItemSwitchScript>(true);
#endif
    }

    private static Sprite FindSharedBackgroundSprite(WeaponSlot requester)
    {
        foreach (WeaponSlot slot in ActiveSlots)
        {
            if (slot != null && slot != requester && slot.Backgroundimage != null && slot.Backgroundimage.sprite != null)
            {
                return slot.Backgroundimage.sprite;
            }
        }

        return null;
    }

    private static string BestSlotName(SlotInsideUI slot)
    {
        if (slot == null) return string.Empty;
        if (!string.IsNullOrWhiteSpace(slot.nameofslot)) return NormalizeName(slot.nameofslot);
        return slot.Item != null ? NormalizeName(BestInventoryName(slot.Item)) : string.Empty;
    }

    private static string BestInventoryName(InventoryItem item)
    {
        if (item == null) return string.Empty;
        if (!string.IsNullOrWhiteSpace(item.nameofitem)) return item.nameofitem;
        if (!string.IsNullOrWhiteSpace(item.name)) return item.name;
        return item.itemPrefab != null ? item.itemPrefab.name : string.Empty;
    }

    private static string NormalizeName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;
        string name = rawName.Trim();
        return name.EndsWith("(Clone)", System.StringComparison.OrdinalIgnoreCase)
            ? name.Substring(0, name.Length - 7).Trim()
            : name;
    }

    private static string MapCommonWeaponName(string rawName)
    {
        string token = NormalizeName(rawName).Replace(" ", string.Empty).ToLowerInvariant();
        if (token.Contains("pickaxe") || token.Contains("pick")) return "Pickaxe";
        if (token.Contains("sword")) return "Sword";
        if (token.Contains("axe")) return "Axe";
        return string.Empty;
    }

    private static int CompareTopLeft(WeaponSlot a, WeaponSlot b)
    {
        if (a == b) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        Vector2 pa = GetUiPosition(a.transform);
        Vector2 pb = GetUiPosition(b.transform);
        int y = pb.y.CompareTo(pa.y);
        return y != 0 ? y : pa.x.CompareTo(pb.x);
    }

    private static Vector2 GetUiPosition(Transform target)
    {
        return target is RectTransform rect ? rect.anchoredPosition : new Vector2(target.position.x, target.position.y);
    }
}
