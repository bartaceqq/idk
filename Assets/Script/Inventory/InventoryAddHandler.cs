using UnityEngine;

// Handles adding usable inventory items through a single method call.
public class InventoryAddHandler : MonoBehaviour
{
    [SerializeField] private SlotManager slotManager;

    // Handle Add Item To Inventory.
    public bool AddItemToInventory(InventoryItem inventoryItem)
    {
        if (inventoryItem == null)
        {
            Debug.LogWarning("InventoryAddHandler: InventoryItem is null.", this);
            return false;
        }

        if (inventoryItem.itemType != InventoryItemType.Usable)
        {
            Debug.LogWarning($"InventoryAddHandler: Item '{inventoryItem.name}' is not Usable.", inventoryItem);
            return false;
        }

        SlotManager targetSlotManager = ResolveSlotManager(inventoryItem);
        if (targetSlotManager == null)
        {
            Debug.LogWarning($"InventoryAddHandler: No SlotManager found for item '{inventoryItem.name}'.", this);
            return false;
        }

        return targetSlotManager.AddItem(inventoryItem);
    }

    // Handle Resolve Slot Manager.
    private SlotManager ResolveSlotManager(InventoryItem inventoryItem)
    {
        if (slotManager != null)
        {
            return slotManager;
        }

        if (inventoryItem != null)
        {
            inventoryItem.ResolveReferences();
            if (inventoryItem.slotManager != null)
            {
                slotManager = inventoryItem.slotManager;
                return slotManager;
            }
        }

#if UNITY_2023_1_OR_NEWER
        slotManager = FindFirstObjectByType<SlotManager>(FindObjectsInactive.Include);
#else
        slotManager = FindObjectOfType<SlotManager>(true);
#endif

        return slotManager;
    }
}
