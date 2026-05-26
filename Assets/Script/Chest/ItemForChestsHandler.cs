using System.Collections.Generic; using UnityEngine;

public class ItemForChestsHandler : MonoBehaviour {
    public List<InventoryItem> common = new List<InventoryItem>();
     public List<InventoryItem> uncommon = new List<InventoryItem>();
      public List<InventoryItem> rare = new List<InventoryItem>();
       public List<InventoryItem> epic = new List<InventoryItem>();
        public List<InventoryItem> legendary = new List<InventoryItem>();
    public List<InventoryItem> ReturnRandomThree(string type) {
        List<InventoryItem> sourceItems = GetItemsForType(type);
        if (sourceItems == null || sourceItems.Count == 0) { return null; }

        return PickRandomItems(sourceItems, 3); }

    private List<InventoryItem> GetItemsForType(string type) {
        switch (type) {
            case "common": return common;
            case "uncommon": return uncommon;
            case "rare": return rare;
            case "epic": return epic;
            case "legendary": return legendary;
            default: return null; } }

    private static List<InventoryItem> PickRandomItems(List<InventoryItem> sourceItems, int wantedCount) {
        int itemCount = Mathf.Min(wantedCount, sourceItems.Count);
        List<InventoryItem> pickedItems = new List<InventoryItem>(itemCount);
        HashSet<int> usedIndexes = new HashSet<int>();

        while (pickedItems.Count < itemCount) {
            int index = Random.Range(0, sourceItems.Count);
            if (!usedIndexes.Add(index)) { continue; }

            pickedItems.Add(sourceItems[index]); }

        return pickedItems; } }
