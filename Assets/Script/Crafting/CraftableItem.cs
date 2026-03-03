using UnityEngine;
using System.Collections.Generic;
using System;
public class CraftableItem : MonoBehaviour
{
   
    public Sprite sprite;
    public string name;
    public List<String> neededResources;
    public int slotnumber;
    
    public bool placed = false;
    public bool locked = false;
    public int minlvl = 1;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
