using UnityEngine;
using UnityEngine.UI;
[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f; public float currentHealth = 100f;
    public float healDelaySeconds = 10f; public float healPerSecond = 8f;
    public bool healFromZero = true; public Image healthFill;
    private static PlayerHealth _instance; private float _lastDamageTime;
    private float _nextBarResolveTime;
    public float NormalizedHealth => maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureScenePlayerHealth() { FindOrCreate(); }
    private void Awake()
    {
        if (_instance == null) { _instance = this; }
        maxHealth = Mathf.Max(1f, maxHealth);
        if (currentHealth <= 0f) { currentHealth = maxHealth; }
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        _lastDamageTime = Time.time; ResolveHealthBar(true); UpdateHealthBar();
    }
    private void OnEnable()
    {
        if (_instance == null) { _instance = this; }
        ResolveHealthBar(true); UpdateHealthBar();
    }
    private void Update()
    {
        if (healthFill == null && Time.unscaledTime >= _nextBarResolveTime) { ResolveHealthBar(false); }
        if (currentHealth < maxHealth && (healFromZero || currentHealth > 0f) &&
        Time.time - _lastDamageTime >= healDelaySeconds)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.Max(0f, healPerSecond) * Time.deltaTime);
            UpdateHealthBar();
        }
    }
    public void TakeDamage(float damage)
    {
        if (damage <= 0f || maxHealth <= 0f) { return; }
        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        _lastDamageTime = Time.time; UpdateHealthBar();
    }
    public void Heal(float amount)
    {
        if (amount <= 0f || maxHealth <= 0f) { return; }
        currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth); UpdateHealthBar();
    }
    public static bool DamageTarget(Transform target, float damage)
    {
        PlayerHealth health = ResolveTargetHealth(target);
        if (health == null) { health = FindOrCreate(); }
        if (health == null) { return false; }
        health.TakeDamage(damage); return true;
    }
    public static PlayerHealth FindOrCreate()
    {
        if (_instance != null) { return _instance; }
        _instance = UnitySceneSearch.FindFirst<PlayerHealth>();
        if (_instance != null) { return _instance; }
        GameObject host = ResolveHealthHost();
        return host != null ? host.AddComponent<PlayerHealth>() : null;
    }
    private static PlayerHealth ResolveTargetHealth(Transform target)
    {
        if (target == null) { return _instance; }
        PlayerHealth health = target.GetComponent<PlayerHealth>();
        if (health != null) { return health; }
        health = target.GetComponentInParent<PlayerHealth>();
        if (health != null) { return health; }
        health = target.GetComponentInChildren<PlayerHealth>(true);
        return health != null ? health : _instance;
    }
    private static GameObject ResolveHealthHost()
    {
        LookingController lookingController = UnitySceneSearch.FindFirst<LookingController>();
        if (lookingController != null) { return lookingController.gameObject; }
        try
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null) { return taggedPlayer; }
        }
        catch (UnityException) { }
        FPSController fpsController = UnitySceneSearch.FindFirst<FPSController>();
        return fpsController != null ? fpsController.gameObject : null;
    }
    private void ResolveHealthBar(bool immediate)
    {
        _nextBarResolveTime = Time.unscaledTime + (immediate ? 0.1f : 0.5f);
        if (healthFill != null) { ConfigureFillImage(healthFill); return; }
        Image[] images = UnitySceneSearch.FindAll<Image>();
        Image fallback = null;
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i]; if (image == null || image.name != "Fill") { continue; }
            if (HasAncestor(image.transform, "Bar1") && HasAncestor(image.transform, "LeftBottomBar"))
            {
                healthFill = image; ConfigureFillImage(healthFill); return;
            }
            if (fallback == null && HasAncestor(image.transform, "Bar1")) { fallback = image; }
        }
        if (fallback != null) { healthFill = fallback; ConfigureFillImage(healthFill); }
    }
    private static bool HasAncestor(Transform child, string ancestorName)
    {
        for (Transform current = child; current != null; current = current.parent)
        { if (current.name == ancestorName) { return true; } }
        return false;
    }
    private static void ConfigureFillImage(Image image)
    {
        if (image == null) { return; }
        image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left; image.fillClockwise = true;
    }
    private void UpdateHealthBar()
    {
        if (healthFill == null) { ResolveHealthBar(false); }
        if (healthFill != null) { healthFill.fillAmount = NormalizedHealth; }
    }
}
