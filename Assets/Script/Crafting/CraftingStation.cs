using UnityEngine;

// Legacy component kept for scene/prefab compatibility.
[AddComponentMenu("Crafting/Crafting Station")]
public class CraftingStation : MonoBehaviour
{
    [Tooltip("Unused legacy id. Crafting now works from the regular crafting menu anywhere.")]
    public string stationId = "Legacy";
}
