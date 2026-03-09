using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public static bool IsInventoryOpen { get; private set; }

    public Image backgroundofwholeinventory;
    public List<SlotInsideUI> slotlist = new List<SlotInsideUI>();
    public bool UIShown = false;
    public KeyCode key;

    private void Awake()
    {
        EnsureSlotList();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
     EnsureSlotList();
     EnableInventory(UIShown);

    }

    void OnDisable()
    {
        IsInventoryOpen = false;
        ApplyCursorState();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(key))
        {
            UIShown = !UIShown;
            EnableInventory(UIShown);
        }
    }
    public bool AddItem(InventoryItem item, int count)
    {
        EnsureSlotList();
        if (item == null || count <= 0)
        {
            return false;
        }

        // Stack into an existing slot first.
        for (int i = 0; i < slotlist.Count; i++)
        {
            SlotInsideUI slot = slotlist[i];
            if (slot == null || !slot.occupied)
            {
                continue;
            }

            if (!IsSameItem(slot, item))
            {
                continue;
            }

            slot.count += count;
            slot.Item = item;
            slot.nameofslot = !string.IsNullOrWhiteSpace(item.nameofitem) ? item.nameofitem : item.name;
            if (slot.text != null)
            {
                slot.text.text = slot.count.ToString();
            }

            return true;
        }

        // Otherwise place into the lowest-id empty slot.
        int number = 30;
        SlotInsideUI finalslot = null;
        foreach(SlotInsideUI slot in slotlist)
        {
            if(slot.id < number && !slot.occupied)
            {
                number = slot.id;
                finalslot =slot;
            }
        }
        if (finalslot == null)
        {
            return false;
        }

        finalslot.occupied = true;
        if (finalslot.image != null)
        {
            finalslot.image.sprite = item.inventorysprite;
        }
        finalslot.nameofslot = !string.IsNullOrWhiteSpace(item.nameofitem)
            ? item.nameofitem
            : item.name;
        finalslot.count = count;
        finalslot.Item = item;
        if (finalslot.text != null)
        {
            finalslot.text.text = count.ToString();
        }
        return true;
    }

    public void EnableInventory(bool status)
    {
        UIShown = status;
        IsInventoryOpen = status;
        ApplyCursorState();

        EnsureSlotList();
        if (backgroundofwholeinventory != null)
        {
            backgroundofwholeinventory.enabled = status;
        }
        foreach(SlotInsideUI slot in slotlist)
        {
            if (slot == null)
            {
                continue;
            }

            if (slot.image != null) slot.image.enabled = status;
            if(!slot.occupied) slot.image.enabled = false;
            if (slot.background != null) slot.background.enabled = status;
            if (slot.text != null) slot.text.enabled = status;
        }
    }

    // Handle Apply Cursor State.
    private static void ApplyCursorState()
    {
        bool uiOpen = IsInventoryOpen || InventoryController.IsInventoryOpen || CraftingManager.IsCraftingOpen;
        Cursor.lockState = uiOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = uiOpen;
    }

    // Handle Ensure Slot List.
    private void EnsureSlotList()
    {
        if (slotlist == null)
        {
            slotlist = new List<SlotInsideUI>();
        }

        if (slotlist.Count > 0)
        {
            return;
        }

        SlotInsideUI[] discovered = GetComponentsInChildren<SlotInsideUI>(true);
        for (int i = 0; i < discovered.Length; i++)
        {
            SlotInsideUI slot = discovered[i];
            if (slot != null && !slotlist.Contains(slot))
            {
                slotlist.Add(slot);
            }
        }
    }

    // Handle Is Same Item.
    private static bool IsSameItem(SlotInsideUI slot, InventoryItem item)
    {
        if (slot == null || item == null)
        {
            return false;
        }

        if (slot.Item == item)
        {
            return true;
        }

        string slotName = !string.IsNullOrWhiteSpace(slot.nameofslot)
            ? slot.nameofslot
            : (slot.Item != null ? slot.Item.nameofitem : string.Empty);
        string itemName = !string.IsNullOrWhiteSpace(item.nameofitem) ? item.nameofitem : item.name;

        if (string.IsNullOrWhiteSpace(slotName) || string.IsNullOrWhiteSpace(itemName))
        {
            return false;
        }

        return string.Equals(slotName.Trim(), itemName.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }
}
