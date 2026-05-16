using System.Collections.Generic;
using UnityEngine;

public class CraftableItem : MonoBehaviour
{
    public Sprite sprite;
    public new string name;
    public List<string> neededResources = new List<string>();
    public int slotnumber = -1;

    [Header("Craft Result")]
    public InventoryItem craftedInventoryItem;
    public InventoryItemType itemType = InventoryItemType.Usable;
    public GameObject itemPrefab;
    public int craftAmount = 1;

    [Header("Availability")]
    public bool placed;
    public bool locked;
    public int minlvl = 1;

    private InventoryItem runtimeCraftedInventoryItem;

    private void OnValidate()
    {
        craftAmount = Mathf.Max(1, craftAmount);

        if (craftedInventoryItem != null)
        {
            SyncCraftResultToInventoryItem(craftedInventoryItem);
        }

        if (!RequiresPrefab() || ResolveCraftPrefab() != null)
        {
            ValidateSwordPrefab();
            return;
        }

        Debug.LogWarning($"{name}: Type {itemType} requires a prefab.", this);
    }

    public bool RequiresPrefab()
    {
        return itemType == InventoryItemType.Tool || itemType == InventoryItemType.Sword;
    }

    public GameObject ResolveCraftPrefab()
    {
        if (itemPrefab != null)
        {
            return itemPrefab;
        }

        return craftedInventoryItem != null ? craftedInventoryItem.itemPrefab : null;
    }

    public bool TryResolveCraftedInventoryItem(out InventoryItem resolvedItem, out string reason)
    {
        resolvedItem = craftedInventoryItem != null ? craftedInventoryItem : GetOrCreateRuntimeCraftedInventoryItem();
        reason = string.Empty;

        if (resolvedItem == null)
        {
            reason = "Missing crafted inventory item definition.";
            return false;
        }

        SyncCraftResultToInventoryItem(resolvedItem);

        if (RequiresPrefab() && resolvedItem.itemPrefab == null)
        {
            reason = $"Type {itemType} requires a prefab for crafted item {resolvedItem.name}.";
            return false;
        }

        return true;
    }

    private InventoryItem GetOrCreateRuntimeCraftedInventoryItem()
    {
        if (runtimeCraftedInventoryItem != null)
        {
            return runtimeCraftedInventoryItem;
        }

        runtimeCraftedInventoryItem = GetComponent<InventoryItem>();
        if (runtimeCraftedInventoryItem == null)
        {
            runtimeCraftedInventoryItem = gameObject.AddComponent<InventoryItem>();
        }

        return runtimeCraftedInventoryItem;
    }

    private void SyncCraftResultToInventoryItem(InventoryItem target)
    {
        if (target == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(target.nameofitem))
        {
            target.nameofitem = name;
        }

        if (string.IsNullOrWhiteSpace(target.name))
        {
            target.name = name;
        }

        if (target.inventorysprite == null)
        {
            target.inventorysprite = sprite;
        }

        target.itemType = itemType;
        if (itemPrefab != null)
        {
            target.itemPrefab = itemPrefab;
        }

        target.mingain = 1;
        target.maxgain = Mathf.Max(1, craftAmount);
    }

    private void ValidateSwordPrefab()
    {
        if (itemType != InventoryItemType.Sword)
        {
            return;
        }

        GameObject swordPrefab = ResolveCraftPrefab();
        if (swordPrefab != null && swordPrefab.GetComponent<Sword>() == null)
        {
            Debug.LogWarning($"{name}: Crafted sword prefabs should include a Sword component.", this);
        }
    }
}
