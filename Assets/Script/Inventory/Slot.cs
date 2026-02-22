using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Slot : MonoBehaviour
{
    public Sprite sprite;
    public int count;
    public SlotManager slotManager;
    public string itemName;
    public Image image;
    public TMP_Text counttext;

    void Start()
    {
        if (slotManager != null && !slotManager.slots.Contains(this))
        {
            slotManager.slots.Add(this);
        }
    }

    public void AddItem(InventoryItem inventoryItem)
    {
        if (sprite == null)
        {
            sprite = inventoryItem.inventorysprite;
            itemName = inventoryItem.name;
        }

        count += GetCount(inventoryItem);
        UpdateUI();
    }

    public int GetCount(InventoryItem inventoryItem)
    {
        if (inventoryItem.mingain == inventoryItem.maxgain)
        {
            return inventoryItem.mingain;
        }

        int min = Mathf.Min(inventoryItem.mingain, inventoryItem.maxgain);
        int max = Mathf.Max(inventoryItem.mingain, inventoryItem.maxgain);
        return Random.Range(min, max + 1);
    }

    public bool MatchesItem(InventoryItem inventoryItem)
    {
        if (inventoryItem == null)
        {
            return false;
        }

        return sprite != null &&
               sprite == inventoryItem.inventorysprite &&
               itemName == inventoryItem.name;
    }

    public void UpdateUI()
    {
        if (image != null)
        {
            image.sprite = sprite;
        }

        if (counttext != null)
        {
            counttext.text = count.ToString();
        }
    }
}
