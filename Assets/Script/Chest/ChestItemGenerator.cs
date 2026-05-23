using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

public class ChestItemGenerator : MonoBehaviour
{
    private static InfoHandler cachedInfoHandler;

    public List<Image> images;
    public List<Image> background;
    public List<Image> whitspace;
    public ItemForChestsHandler itemForChestsHandler;
    public InventoryAddHandler inventoryAddHandler;
    public InfoHandler infoHandler;
    public bool rolling =false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ResolveReferences();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void GeneratedItems(string type)
    {
        ResolveReferences();
        if (itemForChestsHandler == null)
        {
            return;
        }

        List<InventoryItem> listofitems = itemForChestsHandler.returnrandomthree(type);
        if (listofitems == null || listofitems.Count == 0)
        {
            return;
        }

        int count = Mathf.Min(3, Mathf.Min(images.Count, listofitems.Count));
        for(int i =0; i < count; i++)
        {
            InventoryItem item = listofitems[i];
            if (item == null)
            {
                continue;
            }

            images[i].sprite = item.inventorysprite;

            bool added = inventoryAddHandler != null && inventoryAddHandler.AddItemToInventory(item);
            if (added && infoHandler != null)
            {
                infoHandler.QueueInfo($"Gained {ToDisplayName(item.name)}", item.inventorysprite);
            }
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
        if (itemForChestsHandler == null)
        {
            yield break;
        }

        List<InventoryItem> listofitems = itemForChestsHandler.returnrandomthree(type);
        if (listofitems == null || listofitems.Count == 0)
        {
            yield break;
        }

        int count = Mathf.Min(3, Mathf.Min(images.Count, listofitems.Count));
        for(int i =0; i < count; i++)
        {
            InventoryItem item = listofitems[i];
            if (item != null)
            {
                images[i].sprite = item.inventorysprite;
            }
            
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

    // Handle Resolve References.
    private void ResolveReferences()
    {
        if (infoHandler == null)
        {
            if (cachedInfoHandler == null)
            {
                cachedInfoHandler = FindInfoHandlerInScene();
            }

            infoHandler = cachedInfoHandler;
        }
        else
        {
            cachedInfoHandler = infoHandler;
        }
    }

    // Handle Find Info Handler In Scene.
    private static InfoHandler FindInfoHandlerInScene()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<InfoHandler>(FindObjectsInactive.Include);
#else
        return FindObjectOfType<InfoHandler>(true);
#endif
    }

    // Handle To Display Name.
    private static string ToDisplayName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "Item";
        }

        string normalized = rawName.Trim().Replace('_', ' ');
        string[] parts = normalized.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(parts[i]))
            {
                continue;
            }

            string lower = parts[i].ToLowerInvariant();
            parts[i] = char.ToUpperInvariant(lower[0]) + lower.Substring(1);
        }

        return string.Join(" ", parts);
    }
}

