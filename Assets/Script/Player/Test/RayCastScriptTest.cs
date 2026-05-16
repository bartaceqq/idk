using UnityEngine;

// Legacy build controller kept as a compatibility stub.
public class RayCastScriptTest : MonoBehaviour
{
    [Header("Legacy Build Prefabs (unused)")]
    public GameObject wall;
    public GameObject floor;
    public GameObject stair;

    private bool _loggedDisabled;

    private void Awake()
    {
        DisableBuildingSystem();
    }

    private void OnEnable()
    {
        DisableBuildingSystem();
    }

    private void DisableBuildingSystem()
    {
        if (!_loggedDisabled)
        {
            Debug.Log($"{nameof(RayCastScriptTest)} is disabled because build mode was removed.", this);
            _loggedDisabled = true;
        }

        enabled = false;
    }

    public bool TrySelectInventoryBuildingPrefab(GameObject prefab, string sourceItemName)
    {
        return false;
    }

    public bool TrySelectInventoryBuildingItem(InventoryItem inventoryItem)
    {
        return false;
    }
}
