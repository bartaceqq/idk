using UnityEngine;

public enum InventoryItemType
{
    Usable = 0,
    Tool = 1,
    Sword = 2
}

public class InventoryItem : MonoBehaviour
{
    public Sprite inventorysprite;
    public string nameofitem;
    public InventoryItemType itemType = InventoryItemType.Usable;
    public GameObject itemPrefab;
    public int mingain = 1;
    public int maxgain = 1;

    private void Awake()
    {
        ValidateItemSetup(false);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ValidateItemSetup(true);
        }
    }

    public void ResolveReferences()
    {
        ValidateItemSetup(false);
    }

    public bool RequiresPrefab()
    {
        return itemType == InventoryItemType.Tool || itemType == InventoryItemType.Sword;
    }

    public bool HasRequiredPrefab()
    {
        return !RequiresPrefab() || itemPrefab != null;
    }

    private void ValidateItemSetup(bool logWarning)
    {
        mingain = Mathf.Max(1, mingain);
        maxgain = Mathf.Max(mingain, maxgain);

        if (logWarning && !HasRequiredPrefab())
        {
            Debug.LogWarning($"{name}: Item type {itemType} requires itemPrefab.", this);
        }

        if (logWarning && itemType == InventoryItemType.Sword && itemPrefab != null && itemPrefab.GetComponent<Sword>() == null)
        {
            Debug.LogWarning($"{name}: Sword items should use an itemPrefab with a Sword component.", this);
        }
    }
}
