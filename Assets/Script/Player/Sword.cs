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
        string token = ResolveTierName();
        if (token.Contains("flamingore")) { return 130f; }
        if (token.Contains("plasma")) { return 110f; }
        if (token.Contains("radium")) { return 95f; }
        if (token.Contains("diamond")) { return 80f; }
        if (token.Contains("gold")) { return 65f; }
        if (token.Contains("iron")) { return 52f; }
        return 0f;
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
