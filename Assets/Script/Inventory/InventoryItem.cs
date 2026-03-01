using UnityEngine;

// Controls Inventory Item behavior.
public class InventoryItem : MonoBehaviour
{
    private static SlotManager cachedSlotManager;

    public Sprite inventorysprite;
    public string name;
    public int mingain;
    public int maxgain;
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
        }
    }

    // Handle Resolve References.
    public void ResolveReferences()
    {
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
}
