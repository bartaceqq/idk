using UnityEngine;

// Gives the player starter items at the beginning of the game for testing.
public class StarterItems : MonoBehaviour
{
    public int startAmount = 20;

    private void Start()
    {
        InventoryAddHandler addHandler = FindInventoryAddHandler();
        if (addHandler == null)
        {
            Debug.LogWarning("StarterItems: Could not find InventoryAddHandler in scene.", this);
            return;
        }

        InventoryItem stoneItem = FindInventoryItemByTag("LittleStone");
        if (stoneItem != null)
        {
            addHandler.AddItemToInventoryAmount(stoneItem, startAmount);
        }
        else
        {
            Debug.LogWarning("StarterItems: No GameObject with tag 'LittleStone' and InventoryItem found.", this);
        }

        InventoryItem stickItem = FindInventoryItemByTag("Stick");
        if (stickItem != null)
        {
            addHandler.AddItemToInventoryAmount(stickItem, startAmount);
        }
        else
        {
            Debug.LogWarning("StarterItems: No GameObject with tag 'Stick' and InventoryItem found.", this);
        }
    }

    private static InventoryItem FindInventoryItemByTag(string tag)
    {
        GameObject obj = GameObject.FindWithTag(tag);
        if (obj == null)
        {
            return null;
        }

        InventoryItem item = obj.GetComponent<InventoryItem>();
        if (item == null)
        {
            item = obj.GetComponentInParent<InventoryItem>();
        }
        if (item == null)
        {
            item = obj.GetComponentInChildren<InventoryItem>(true);
        }

        return item;
    }

    private static InventoryAddHandler FindInventoryAddHandler()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<InventoryAddHandler>(FindObjectsInactive.Include);
#else
        return FindObjectOfType<InventoryAddHandler>(true);
#endif
    }
}
