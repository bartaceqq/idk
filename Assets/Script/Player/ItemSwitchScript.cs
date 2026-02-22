using System.Collections.Generic;
using UnityEngine;

public class ItemSwitchScript : MonoBehaviour
{
    public List<Item> items = new List<Item>();
    public int currentitemid;
    public string currentitemname;

    public Item item;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach(Item item in items)
        {
            if (Input.GetKeyDown(item.key))
            {
                if (this.item != null && this.item.itemobject != null)
                {
                    MeshRenderer meshRenderer = this.item.itemobject.GetComponent<MeshRenderer>();
                    if (meshRenderer != null)
                    {
                        meshRenderer.enabled = false;
                    }
                }

                currentitemid = item.ID;
                currentitemname = item.name;
                this.item = item;

                if (item.itemobject != null)
                {
                    MeshRenderer meshRenderer2 = item.itemobject.GetComponent<MeshRenderer>();
                    if (meshRenderer2 != null)
                    {
                        meshRenderer2.enabled = true;
                    }
                }
                
            }
        }
    }
}
