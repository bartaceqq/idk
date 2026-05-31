using UnityEngine;
public class Sword : MonoBehaviour
{
    [Header("Sword Stats")]
    [Min(0f)] public float damage = 40f; [Min(0.01f)] public float speed = 1f;
    [Min(0.01f)] public float animationSpeed = 1f; private void OnValidate()
    {
        damage = Mathf.Max(0f, damage); speed = Mathf.Max(0.01f, speed);
        animationSpeed = Mathf.Max(0.01f, animationSpeed);
    }
    public float GetResolvedDamage()
    {
        float tierDamage = ResolveTierDamage();
        return tierDamage > 0f ? tierDamage : Mathf.Max(0f, damage);
    }
    public float GetResolvedSpeed() { return speed > 0f ? speed : 1f; }
    public float GetResolvedAnimationSpeed() { return animationSpeed > 0f ? animationSpeed : 1f; }
    public static bool TryResolve(Item item, out Sword sword)
    {
        sword = null;
        if (item == null) { return false; }
        sword = item.GetComponent<Sword>();
        if (sword != null) { return true; }
        if (item.itemobject == null) { return false; }
        sword = item.itemobject.GetComponent<Sword>(); return sword != null;
    }
    private float ResolveTierDamage()
    {
        return ToolTierUtility.ResolveSwordDamage(ResolveTierName(), 0f);
    }
    private string ResolveTierName()
    {
        Item ownerItem = GetComponent<Item>();
        if (ownerItem == null) { ownerItem = GetComponentInParent<Item>(); }
        string source = ownerItem != null && !string.IsNullOrWhiteSpace(ownerItem.name) ? ownerItem.name : gameObject.name;
        return string.IsNullOrWhiteSpace(source) ? string.Empty
        : source.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }
}

public static class ToolTierUtility
{
    public static int ResolveRequiredHitsToBreak(string itemName, int fallbackHits)
    {
        string token = Normalize(itemName);
        if (token.Contains("flamingore")) { return 1; }
        if (token.Contains("plasma")) { return 2; }
        if (token.Contains("radium")) { return 3; }
        if (token.Contains("diamond")) { return 4; }
        if (token.Contains("gold")) { return 5; }
        if (token.Contains("iron")) { return 6; }
        if (token.Contains("stone")) { return 7; }
        return fallbackHits;
    }

    public static float ResolveSwordDamage(string itemName, float fallbackDamage)
    {
        string token = Normalize(itemName);
        if (token.Contains("flamingore")) { return 160f; }
        if (token.Contains("plasma")) { return 130f; }
        if (token.Contains("radium")) { return 105f; }
        if (token.Contains("diamond")) { return 85f; }
        if (token.Contains("gold")) { return 70f; }
        if (token.Contains("iron")) { return 55f; }
        if (token.Contains("stone")) { return 40f; }
        return fallbackDamage;
    }

    private static string Normalize(string itemName)
    {
        return string.IsNullOrWhiteSpace(itemName) ? string.Empty
        : itemName.Trim().Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }
}
