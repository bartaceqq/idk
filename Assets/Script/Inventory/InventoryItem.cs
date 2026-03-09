using UnityEngine;

public enum InventoryItemType
{
    Usable = 0,
    Tool = 1,
    Sword = 2,
    Building = 3
}

// Controls Inventory Item behavior.
public class InventoryItem : MonoBehaviour
{
    private static SlotManager cachedSlotManager;

    public Sprite inventorysprite;
    public string nameofitem;
    public InventoryItemType itemType = InventoryItemType.Usable;
    public GameObject itemPrefab;
    [Header("Build Placement")]
    public Vector3 buildRotationEuler = Vector3.zero;
    public Vector3 buildScale = Vector3.one;
    public int mingain = 1;
    public int maxgain = 1;
    public SlotManager slotManager;

    // Initialize references before gameplay starts.
    private void Awake()
    {
        ResolveReferences();
    }

    // Run in the editor when values change in Inspector.
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ResolveReferences();
            ValidateItemSetup(true);
        }
    }

    // Handle Resolve References.
    public void ResolveReferences()
    {
        ValidateItemSetup(false);

        if (slotManager != null)
        {
            cachedSlotManager = slotManager;
            return;
        }

        if (cachedSlotManager == null)
        {
            cachedSlotManager = FindSlotManagerInScene();
        }

        slotManager = cachedSlotManager;
    }

    // Handle Find Slot Manager In Scene.
    private static SlotManager FindSlotManagerInScene()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<SlotManager>(FindObjectsInactive.Include);
#else
        return FindObjectOfType<SlotManager>(true);
#endif
    }

    // Handle Requires Prefab.
    public bool RequiresPrefab()
    {
        return itemType == InventoryItemType.Tool || itemType == InventoryItemType.Building;
    }

    // Handle Has Required Prefab.
    public bool HasRequiredPrefab()
    {
        return !RequiresPrefab() || itemPrefab != null;
    }

    // Handle Validate Item Setup.
    private void ValidateItemSetup(bool logWarning)
    {
        if (mingain <= 0)
        {
            mingain = 1;
        }

        if (maxgain <= 0)
        {
            maxgain = mingain;
        }

        if (maxgain < mingain)
        {
            maxgain = mingain;
        }

        if (!logWarning || HasRequiredPrefab())
        {
            ValidateBuildScale();
        }
        else
        {
            Debug.LogWarning($"{name}: Item type {itemType} requires itemPrefab.", this);
            ValidateBuildScale();
        }
    }

    // Handle Validate Build Scale.
    private void ValidateBuildScale()
    {
        buildScale.x = ValidateScaleAxis(buildScale.x);
        buildScale.y = ValidateScaleAxis(buildScale.y);
        buildScale.z = ValidateScaleAxis(buildScale.z);
    }

    // Handle Validate Scale Axis.
    private static float ValidateScaleAxis(float axisValue)
    {
        if (Mathf.Abs(axisValue) < 0.0001f)
        {
            return 1f;
        }

        return axisValue;
    }
}
