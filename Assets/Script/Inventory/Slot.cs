using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Controls Slot behavior.
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
        if (slotManager != null)
        {
            slotManager.RegisterSlot(this);
        }
    }

    // Handle Add Item.
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

    // Handle Get Count.
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

    // Handle Matches Item.
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

    // Handle Update UI.
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
