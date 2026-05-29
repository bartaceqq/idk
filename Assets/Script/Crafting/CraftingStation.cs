using UnityEngine;
[AddComponentMenu("Crafting/Crafting Station")]
public class CraftingStation : MonoBehaviour
{
    public string stationId = "BasicCraftingTable"; public float interactionRange = 5f;
    public Transform interactionPoint; public bool useXZDistanceOnly = true;
    public Vector3 GetInteractionPosition() { return interactionPoint != null ? interactionPoint.position : transform.position; }
    public float GetDistanceSqrTo(Transform target)
    {
        if (target == null) { return float.MaxValue; }
        Vector3 delta = target.position - GetInteractionPosition();
        if (useXZDistanceOnly) { delta.y = 0f; }
        return delta.sqrMagnitude;
    }
    public bool IsInRange(Transform target, float fallbackRange)
    {
        float range = interactionRange > 0f ? interactionRange : fallbackRange;
        range = Mathf.Max(0.01f, range); return GetDistanceSqrTo(target) <= range * range;
    }
    public string GetNormalizedStationId() { return NormalizeStationId(stationId); }
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(stationId)) { stationId = "BasicCraftingTable"; }
        interactionRange = Mathf.Max(0.1f, interactionRange);
    }
    private static string NormalizeStationId(string rawId)
    {
        if (string.IsNullOrWhiteSpace(rawId)) { return string.Empty; }
        return rawId.Trim();
    }
}
