using System.Collections.Generic;
using UnityEngine;
public class ChestController : MonoBehaviour
{
    public string chosenlist = "common";
    public List<InventoryItem> commonlist = new List<InventoryItem>();
    public List<InventoryItem> uncommonlist = new List<InventoryItem>();
    public List<InventoryItem> rarelist = new List<InventoryItem>();
    public List<InventoryItem> epiclist = new List<InventoryItem>();
    public List<InventoryItem> legendarylist = new List<InventoryItem>();
    [SerializeField] private SlotManager targetSlotManager;
    public bool AddItemToInventory(InventoryItem inventoryItem, int amount = 1)
    {
        if (inventoryItem == null)
        {
            Debug.LogWarning("ChestController: InventoryItem is null.", this); return false;
        }
        inventoryItem.ResolveReferences();
        SlotManager slotManager = ResolveSlotManager(inventoryItem); if (slotManager == null)
        {
            Debug.LogWarning($"ChestController: No SlotManager found for '{inventoryItem.name}'.", this);
            return false;
        }
        return amount > 0 ? slotManager.AddItem(inventoryItem, amount)
            : slotManager.AddItem(inventoryItem);
    }
    private SlotManager ResolveSlotManager(InventoryItem inventoryItem)
    {
        if (targetSlotManager != null) { return targetSlotManager; }
        if (inventoryItem != null && inventoryItem.slotManager != null) { return inventoryItem.slotManager; }
        targetSlotManager = UnitySceneSearch.FindFirst<SlotManager>(); return targetSlotManager;
    }
    public void GetRandomItemAndAddThemToTheInventory() { }
    public List<InventoryItem> GetSelectedList()
    {
        switch (chosenlist.ToLowerInvariant())
        {
            case "common": return commonlist;
            case "uncommon": return uncommonlist;
            case "rare":
                return rarelist;
            case "epic": return epiclist;
            case "legendary": return legendarylist;
            default:
                Debug.LogWarning($"ChestController: Unknown list '{chosenlist}', using commonlist.", this);
                return commonlist;
        }
    }
}
