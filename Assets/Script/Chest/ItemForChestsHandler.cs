using System.Collections.Generic;
using UnityEngine;

public class ItemForChestsHandler : MonoBehaviour
{
    public List<InventoryItem> common = new List<InventoryItem>();
     public List<InventoryItem> uncommon = new List<InventoryItem>();
      public List<InventoryItem> rare = new List<InventoryItem>();
       public List<InventoryItem> epic = new List<InventoryItem>();
        public List<InventoryItem> legendary = new List<InventoryItem>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public List<InventoryItem> returnrandomthree(string type)
    {
        switch (type)
        {
            case "common":
            List<int> numberscom = new List<int>();
                for(int i =0;i <3; i++)
                {
                    int number = Random.Range(0, common.Count);
                    while (numberscom.Contains(number))
                    {
                        number = Random.Range(0, common.Count);
                    }
                    numberscom.Add(number);
                }
            List<InventoryItem> returnlistcom = new List<InventoryItem>();
            foreach(int num in numberscom)
                {
                    returnlistcom.Add(common[num]);
                }
                return returnlistcom;
            break;
             case "uncommon":
            List<int> numbersuncom = new List<int>();
                for(int i =0;i <3; i++)
                {
                    int number = Random.Range(0, uncommon.Count);
                    while (numbersuncom.Contains(number))
                    {
                        number = Random.Range(0, uncommon.Count);
                    }
                    numbersuncom.Add(number);
                }
            List<InventoryItem> returnlistuncom = new List<InventoryItem>();
            foreach(int num in numbersuncom)
                {
                    returnlistuncom.Add(uncommon[num]);
                }
                return returnlistuncom;
            break;
            case "rare":
            List<int> numbersare = new List<int>();
                for(int i =0;i <3; i++)
                {
                    int number = Random.Range(0, rare.Count);
                    while (numbersare.Contains(number))
                    {
                        number = Random.Range(0, rare.Count);
                    }
                    numbersare.Add(number);
                }
            List<InventoryItem> returnlistrare = new List<InventoryItem>();
            foreach(int num in numbersare)
                {
                    returnlistrare.Add(rare[num]);
                }
                return returnlistrare;
            break;
            case "epic":
            List<int> numbersepic = new List<int>();
                for(int i =0;i <3; i++)
                {
                    int number = Random.Range(0, epic.Count);
                    while (numbersepic.Contains(number))
                    {
                        number = Random.Range(0, epic.Count);
                    }
                    numbersepic.Add(number);
                }
            List<InventoryItem> returnlistepic = new List<InventoryItem>();
            foreach(int num in numbersepic)
                {
                    returnlistepic.Add(epic[num]);
                }
                return returnlistepic;
            break;
             case "legendary":
            List<int> numberslegendary = new List<int>();
                for(int i =0;i <3; i++)
                {
                    int number = Random.Range(0, legendary.Count);
                    while (numberslegendary.Contains(number))
                    {
                        number = Random.Range(0, legendary.Count);
                    }
                    numberslegendary.Add(number);
                }
            List<InventoryItem> returnlislegendary = new List<InventoryItem>();
            foreach(int num in numberslegendary)
                {
                    returnlislegendary.Add(legendary[num]);
                }
                return returnlislegendary;
            break;
            default:
            return null;
            break;
        }
    }
}
