using System;
using System.Collections.Generic;
using UnityEngine;

// Controls Craftable Item behavior.
public class CraftableItem : MonoBehaviour
{
    public Sprite sprite;
    public string name;
    public List<string> neededResources = new List<string>();
    public int slotnumber = -1;
    [Header("Crafting Station")]
    public string craftingStationId = "HandCrafting";

    [Header("Craft Result")]
    public InventoryItem craftedInventoryItem;
    public InventoryItemType itemType = InventoryItemType.Usable;
    public GameObject itemPrefab;
    public Vector3 buildRotationEuler = Vector3.zero;
    public Vector3 buildScale = Vector3.one;
    public int craftAmount = 1;

    [Header("Availability")]
    public bool placed = false;
    public bool locked = false;
    public int minlvl = 1;

    private InventoryItem runtimeCraftedInventoryItem;

    // Run in the editor when values change in Inspector.
    private void OnValidate()
    {
        craftAmount = Mathf.Max(1, craftAmount);
        ValidateBuildScale();

        if (craftedInventoryItem != null)
        {
            SyncCraftResultToInventoryItem(craftedInventoryItem, null);
        }

        if (!RequiresPrefab() || ResolveCraftPrefab() != null)
        {
            return;
        }

        Debug.LogWarning($"{name}: Type {itemType} requires a prefab (itemPrefab or craftedInventoryItem.itemPrefab).", this);
    }

    // Handle Requires Prefab.
    public bool RequiresPrefab()
    {
        return itemType == InventoryItemType.Tool || itemType == InventoryItemType.Building;
    }

    // Handle Resolve Craft Prefab.
    public GameObject ResolveCraftPrefab()
    {
        if (itemPrefab != null)
        {
            return itemPrefab;
        }

        if (craftedInventoryItem != null)
        {
            return craftedInventoryItem.itemPrefab;
        }

        return null;
    }

    // Handle Try Resolve Crafted Inventory Item.
    public bool TryResolveCraftedInventoryItem(SlotManager fallbackSlotManager, out InventoryItem resolvedItem, out string reason)
    {
        resolvedItem = craftedInventoryItem;
        reason = string.Empty;

        if (resolvedItem == null)
        {
            resolvedItem = GetOrCreateRuntimeCraftedInventoryItem();
        }

        if (resolvedItem == null)
        {
            reason = "Missing crafted inventory item definition.";
            return false;
        }

        SyncCraftResultToInventoryItem(resolvedItem, fallbackSlotManager);

        if (RequiresPrefab() && resolvedItem.itemPrefab == null)
        {
            reason = $"Type {itemType} requires a prefab for crafted item {resolvedItem.name}.";
            return false;
        }

        return true;
    }

    // Handle Get Or Create Runtime Crafted Inventory Item.
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

    // Handle Sync Craft Result To Inventory Item.
    private void SyncCraftResultToInventoryItem(InventoryItem target, SlotManager fallbackSlotManager)
    {
        if (target == null)
        {
            return;
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

        target.buildRotationEuler = buildRotationEuler;
        target.buildScale = buildScale;

        target.mingain = Mathf.Max(1, target.mingain);
        target.maxgain = Mathf.Max(target.mingain, target.maxgain);

        if (fallbackSlotManager != null && target.slotManager == null)
        {
            target.slotManager = fallbackSlotManager;
        }
    }

    // Handle Validate Build Scale.
    private void ValidateBuildScale()
    {
        buildScale.x = ValidateScaleAxis(buildScale.x);
        buildScale.y = ValidateScaleAxis(buildScale.y);
        buildScale.z = ValidateScaleAxis(buildScale.z);
    }

    // Handle Validate Scale Axis.
    private static float ValidateScaleAxis(float axisValue)
    {
        if (Mathf.Abs(axisValue) < 0.0001f)
        {
            return 1f;
        }

        return axisValue;
    }
}
