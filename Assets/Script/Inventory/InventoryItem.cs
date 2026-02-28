using UnityEngine;

public class InventoryItem : MonoBehaviour
{
    private static SlotManager cachedSlotManager;

    public Sprite inventorysprite;
    public string name;
    public int mingain;
    public int maxgain;
    public SlotManager slotManager;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ResolveReferences();
        }
    }

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

    private static SlotManager FindSlotManagerInScene()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<SlotManager>(FindObjectsInactive.Include);
#else
        return FindObjectOfType<SlotManager>(true);
#endif
    }
}
