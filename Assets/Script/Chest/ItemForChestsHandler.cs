using System.Collections.Generic;
using UnityEngine;

public class ItemForChestsHandler : MonoBehaviour
{
    public List<InventoryItem> common = new List<InventoryItem>();
    public List<InventoryItem> uncommon = new List<InventoryItem>();
    public List<InventoryItem> rare = new List<InventoryItem>();
    public List<InventoryItem> epic = new List<InventoryItem>();
    public List<InventoryItem> legendary = new List<InventoryItem>();

    public List<InventoryItem> returnrandomthree(string type)
    {
        return GetRandomItems(GetRarityList(type), 3);
    }

    private List<InventoryItem> GetRarityList(string type)
    {
        switch (type)
        {
            case "common":
                return common;
            case "uncommon":
                return uncommon;
            case "rare":
                return rare;
            case "epic":
                return epic;
            case "legendary":
                return legendary;
            default:
                return null;
        }
    }

    private static List<InventoryItem> GetRandomItems(List<InventoryItem> source, int requestedCount)
    {
        List<InventoryItem> result = new List<InventoryItem>();
        if (source == null || source.Count == 0 || requestedCount <= 0)
        {
            return result;
        }

        List<int> pickedIndexes = new List<int>();
        int itemCount = Mathf.Min(requestedCount, source.Count);
        while (result.Count < itemCount)
        {
            int index = Random.Range(0, source.Count);
            if (pickedIndexes.Contains(index))
            {
                continue;
            }

            pickedIndexes.Add(index);
            result.Add(source[index]);
        }

        return result;
    }
}
