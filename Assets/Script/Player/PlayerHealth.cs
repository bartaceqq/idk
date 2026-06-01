using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
[DisallowMultipleComponent]
public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f; public float currentHealth = 100f;
    public float healDelaySeconds = 10f; public float healPerSecond = 8f;
    public bool blockDamageWhileSwordBlocking = true;
    [Header("Death")] public float deathFadeInSeconds = 0.75f;
    public float deathMessageHoldSeconds = 1.25f; public float deathFadeOutSeconds = 0.45f;
    public string deathMessage = "YOU DIED";
    public bool healFromZero = true; public Image healthFill;
    private static PlayerHealth _instance; private float _lastDamageTime;
    private float _nextBarResolveTime; private bool _deathSequenceStarted;
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
        if (blockDamageWhileSwordBlocking && IsSwordBlockActive()) { return; }
        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        _lastDamageTime = Time.time; UpdateHealthBar();
        if (currentHealth <= 0f) { StartDeathSequence(); }
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
    private void StartDeathSequence()
    {
        if (_deathSequenceStarted) { return; }
        _deathSequenceStarted = true;
        GameObject runnerObject = new GameObject("Death Sequence Runner");
        DontDestroyOnLoad(runnerObject);
        DeathSequenceRunner runner = runnerObject.AddComponent<DeathSequenceRunner>();
        runner.Begin(deathMessage, deathFadeInSeconds, deathMessageHoldSeconds, deathFadeOutSeconds);
    }
    private static GameObject CreateDeathOverlay(out CanvasGroup canvasGroup, out TMP_Text messageText)
    {
        GameObject root = new GameObject("Death Screen Overlay");
        DontDestroyOnLoad(root);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = short.MaxValue;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        root.AddComponent<GraphicRaycaster>();
        canvasGroup = root.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f; canvasGroup.blocksRaycasts = true; canvasGroup.interactable = false;

        GameObject backgroundObject = new GameObject("Black Background");
        backgroundObject.transform.SetParent(root.transform, false);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = Color.black;
        RectTransform backgroundRect = background.rectTransform;
        backgroundRect.anchorMin = Vector2.zero; backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero; backgroundRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("Death Message");
        textObject.transform.SetParent(root.transform, false);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 96f; text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white; text.fontStyle = FontStyles.Bold;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
        messageText = text;
        return root;
    }
    private sealed class DeathSequenceRunner : MonoBehaviour
    {
        private string message;
        private float fadeInSeconds;
        private float holdSeconds;
        private float fadeOutSeconds;
        public void Begin(string deathText, float fadeIn, float hold, float fadeOut)
        {
            message = deathText; fadeInSeconds = fadeIn; holdSeconds = hold; fadeOutSeconds = fadeOut;
            StartCoroutine(Run());
        }
        private IEnumerator Run()
        {
            GameplayUiState.SetExternalMenuOpen(true);
            Time.timeScale = 1f;
            GameObject overlay = CreateDeathOverlay(out CanvasGroup canvasGroup, out TMP_Text messageText);
            if (messageText != null) { messageText.text = string.IsNullOrWhiteSpace(message) ? "YOU DIED" : message; }
            float fadeIn = Mathf.Max(0.01f, fadeInSeconds);
            for (float elapsed = 0f; elapsed < fadeIn; elapsed += Time.unscaledDeltaTime)
            {
                if (canvasGroup != null) { canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeIn); }
                yield return null;
            }
            if (canvasGroup != null) { canvasGroup.alpha = 1f; }
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, holdSeconds));

            StartCutSceneScript.SkipNextIntroCutscenes();
            Scene activeScene = SceneManager.GetActiveScene();
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(activeScene.name);
            if (loadOperation != null) { while (!loadOperation.isDone) { yield return null; } }

            float fadeOut = Mathf.Max(0.01f, fadeOutSeconds);
            for (float elapsed = 0f; elapsed < fadeOut; elapsed += Time.unscaledDeltaTime)
            {
                if (canvasGroup != null) { canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeOut); }
                yield return null;
            }
            if (overlay != null) { Destroy(overlay); }
            GameplayUiState.SetExternalMenuOpen(false);
            Destroy(gameObject);
        }
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
    private bool IsSwordBlockActive()
    {
        ActionScript actionScript = GetComponent<ActionScript>();
        if (actionScript == null) { actionScript = GetComponentInParent<ActionScript>(); }
        if (actionScript == null) { actionScript = GetComponentInChildren<ActionScript>(true); }
        if (actionScript == null && transform.root != null) { actionScript = transform.root.GetComponentInChildren<ActionScript>(true); }
        if (actionScript == null) { actionScript = UnitySceneSearch.FindFirst<ActionScript>(); }
        return actionScript != null && actionScript.IsSwordBlockActive();
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
