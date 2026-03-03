using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CraftableSlot : MonoBehaviour
{
    public LevelingManager levelingManager;
    public CraftingManager craftingManager;
    public Image imageslot;
    public Image background;
    public string name;
    public bool occupied = false;
    public bool locked = false;
    public List<String> neededResources;
    public int slotnumber;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        string name = gameObject.name;
        int index = 0;

        int open = name.LastIndexOf('(');
        int close = name.LastIndexOf(')');

        if (open >= 0 && close > open)
        {
            string number = name.Substring(open + 1, close - open - 1);
            if (!int.TryParse(number, out index))
            {
                index = 0;
            }
        }
        
      slotnumber = index;

        // index = 12, or -1 if not found


        if (craftingManager == null)
        {
            Debug.LogWarning("CraftableSlot: CraftingManager is not assigned.");
            return;
        }

        if (!craftingManager.slots.Contains(this))
        {
            craftingManager.slots.Add(this);
        }

        craftingManager.Check();
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void AddCraftableItem(CraftableItem craftableItem)
    {
        if (craftableItem == null)
        {
            return;
        }

        if (imageslot == null)
        {
            Debug.LogWarning("CraftableSlot: Image slot is not assigned.");
            return;
        }

        imageslot.sprite = craftableItem.sprite;
        name = craftableItem.name;
        occupied = true;
        neededResources = craftableItem.neededResources;

        if (levelingManager == null)
        {
            locked = false;
        }
        else if (craftableItem.minlvl <= levelingManager.level)
        {
            locked = false;

        }
        else
        {
            locked = true;
        }
        craftingManager.Check();
    }
}
