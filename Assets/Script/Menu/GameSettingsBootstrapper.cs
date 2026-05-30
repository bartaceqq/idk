using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
[DefaultExecutionOrder(-10000)]
public sealed class GameSettingsBootstrapper : MonoBehaviour
{
    private static GameSettingsBootstrapper instance; private Canvas brightnessCanvas;
    private Image brightnessOverlay;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RuntimeBootstrap()
    {
        if (instance != null) { return; }
        GameObject root = new GameObject("Game Settings Bootstrapper"); DontDestroyOnLoad(root);
        instance = root.AddComponent<GameSettingsBootstrapper>();
    }
    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this; DontDestroyOnLoad(gameObject); SceneManager.sceneLoaded += OnSceneLoaded;
        GameSettings.SettingsChanged += OnSettingsChanged; EnsureBrightnessOverlay();
        GameSettings.ApplyAllSettings(); ApplyBrightnessOverlay();
    }
    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            GameSettings.SettingsChanged -= OnSettingsChanged; instance = null;
        }
    }
    private void Start() { StartCoroutine(ApplyAfterSceneStart()); }
    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.F10)) { NpcTradeInventory.DevGrantLumberMinerQuestItems(); }
#endif
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureBrightnessOverlay();
        GameSettings.ApplyAllSettings(); ApplyBrightnessOverlay();
        StartCoroutine(ApplyAfterSceneStart());
    }
    private void OnSettingsChanged()
    {
        EnsureBrightnessOverlay(); ApplyBrightnessOverlay();
    }
    private IEnumerator ApplyAfterSceneStart()
    {
        yield return null;
        GameSettings.ApplyAllSettings(); ApplyBrightnessOverlay();
    }
    private void EnsureBrightnessOverlay()
    {
        if (brightnessOverlay != null) { return; }
        GameObject canvasObject = new GameObject("Brightness Overlay Canvas");
        canvasObject.transform.SetParent(transform, false);
        brightnessCanvas = canvasObject.AddComponent<Canvas>();
        brightnessCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        brightnessCanvas.sortingOrder = short.MaxValue;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
        GraphicRaycaster raycaster = canvasObject.AddComponent<GraphicRaycaster>();
        raycaster.enabled = false; GameObject imageObject = new GameObject("Brightness Overlay");
        imageObject.transform.SetParent(canvasObject.transform, false);
        brightnessOverlay = imageObject.AddComponent<Image>();
        brightnessOverlay.raycastTarget = false;
        RectTransform rect = brightnessOverlay.rectTransform; rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one; rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
    private void ApplyBrightnessOverlay()
    {
        if (brightnessOverlay == null) { return; }
        float brightness = GameSettings.Brightness;
        if (brightness < 1f)
        {
            brightnessOverlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01((1f - brightness) * 0.7f));
        }
        else { brightnessOverlay.color = new Color(1f, 0.94f, 0.78f, Mathf.Clamp01((brightness - 1f) * 0.22f)); }
    }
}
