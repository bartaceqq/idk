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
            if (Input.GetKey(item.key))
            {
                MeshRenderer meshRenderer = this.item.itemobject.GetComponent<MeshRenderer>();
                meshRenderer.enabled = false;
                currentitemid = item.ID;
                currentitemname = item.name;
                this.item = item;
                MeshRenderer meshRenderer2 = item.itemobject.GetComponent<MeshRenderer>();
                meshRenderer2.enabled = true;
                
            }
        }
    }
}
