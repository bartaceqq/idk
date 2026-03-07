using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class ChestItemGenerator : MonoBehaviour
{
    public List<Image> images;
    public List<Image> background;
    public List<Image> whitspace;
    public ItemForChestsHandler itemForChestsHandler;
    public InventoryAddHandler inventoryAddHandler;
    public bool rolling =false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void GeneratedItems(string type)
    {
        List<InventoryItem> listofitems = itemForChestsHandler.returnrandomthree(type);
        for(int i =0; i < 3; i++)
        {
            images[i].sprite = listofitems[i].inventorysprite;
            inventoryAddHandler.AddItemToInventory(listofitems[i]);
        }
        
    }
     public IEnumerator RealEnum(string type)
    {
        GeneratedItems(type);
        yield return new WaitForSeconds(2f);
        ChangeVisibility(false);
    }
     public IEnumerator WaitFiveSeconds(string type)
    {
       ChangeVisibility(true);
        rolling = true;
        StartCoroutine(Roll(type));
        yield return new WaitForSeconds(2f);
        rolling = false;
        StartCoroutine(RealEnum(type));
       
    }
    public IEnumerator Roll(string type)
    {
         List<InventoryItem> listofitems = itemForChestsHandler.returnrandomthree(type);
        for(int i =0; i < 3; i++)
        {
            images[i].sprite = listofitems[i].inventorysprite;
            
        }
        yield return new WaitForSeconds(0.05f);
        if (rolling)
        {
             StartCoroutine(Roll(type));
        }
    }
    public void ChangeVisibility(bool status)
    {
         foreach(Image image in images)
        {
            image.enabled = status;
        }
         foreach(Image image in background)
        {
            image.enabled = status;
        }
         foreach(Image image in whitspace)
        {
            image.enabled = status;
        }
    }
}

