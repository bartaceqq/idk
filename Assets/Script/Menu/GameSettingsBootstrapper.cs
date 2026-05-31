using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
[DefaultExecutionOrder(-10000)]
public sealed class GameSettingsBootstrapper : MonoBehaviour
{
    private static GameSettingsBootstrapper instance; private Image brightnessOverlay;
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
        NpcTradeInventory.UpdatePendingEndSceneLoad();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Input.GetKeyDown(KeyCode.F10)) { NpcTradeInventory.DevGrantFinalQuestItems(); }
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
        UISquareGraphicGuard.ApplyToOpenScenes();
        yield return null;
        UISquareGraphicGuard.ApplyToOpenScenes();
    }
    private void EnsureBrightnessOverlay()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (brightnessOverlay != null && brightnessOverlay.gameObject.scene == activeScene) { return; }
        Image sceneOverlay = FindSceneBrightnessOverlay();
        if (sceneOverlay == null) { return; }
        brightnessOverlay = sceneOverlay;
        brightnessOverlay.raycastTarget = false;
    }
    private void ApplyBrightnessOverlay()
    {
        ClearOtherBrightnessOverlays(brightnessOverlay);
        if (brightnessOverlay == null) { return; }
        float brightness = GameSettings.Brightness;
        if (brightness < 1f)
        {
            brightnessOverlay.color = new Color(0f, 0f, 0f, Mathf.Clamp01((1f - brightness) * 0.7f));
        }
        else { brightnessOverlay.color = new Color(1f, 0.94f, 0.78f, Mathf.Clamp01((brightness - 1f) * 0.22f)); }
    }
    private static void ClearOtherBrightnessOverlays(Image activeOverlay)
    {
        Image[] images = Resources.FindObjectsOfTypeAll<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image == activeOverlay || image.gameObject == null) { continue; }
            if (!image.gameObject.scene.IsValid()) { continue; }
            if (image.name != "Brightness Overlay") { continue; }
            image.raycastTarget = false;
            image.color = Color.clear;
        }
    }
    private static Image FindSceneBrightnessOverlay()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Image fallback = null;
        Image[] images = Resources.FindObjectsOfTypeAll<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image.gameObject == null) { continue; }
            if (!image.gameObject.scene.IsValid()) { continue; }
            if (image.name != "Brightness Overlay") { continue; }
            if (image.gameObject.scene == activeScene) { return image; }
            if (fallback == null || image.gameObject.activeInHierarchy) { fallback = image; }
        }
        return fallback;
    }
}
