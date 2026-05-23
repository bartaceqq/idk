using UnityEngine;

// Handles adding usable inventory items through a single method call.
public class InventoryAddHandler : MonoBehaviour
{
    public InventoryManager inventoryManager;

    // Handle Add Item To Inventory.
    public bool AddItemToInventory(InventoryItem Item)
    {
        if (Item == null || inventoryManager == null)
        {
            return false;
        }

        int maxExclusive = Mathf.Max(Item.mingain + 1, Item.maxgain + 1);
        int roll = Random.Range(Item.mingain, maxExclusive);

        return inventoryManager.AddItem(Item, roll);
    }

    // Handle Add Item To Inventory Amount.
    public bool AddItemToInventoryAmount(InventoryItem item, int amount)
    {
        if (item == null || inventoryManager == null || amount <= 0)
        {
            return false;
        }

        return inventoryManager.AddItem(item, amount);
    }

    // Handle Resolve Slot Manager.
  

}
