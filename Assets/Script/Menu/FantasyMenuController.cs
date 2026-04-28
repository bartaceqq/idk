using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FantasyMenuController : MonoBehaviour
{
    private enum SettingsTab
    {
        Display,
        Keybind,
        Audio,
        Graphics
    }

    [Header("Scene")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    [Header("Layout")]
    [SerializeField] private RectTransform scaledRoot;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite mainBackgroundSprite;
    [SerializeField] private Sprite settingsBackgroundSprite;
    [SerializeField] private GameObject menuShellRoot;
    [SerializeField] private Color mainBackgroundTint = new Color(0.10f, 0.13f, 0.09f, 1f);
    [SerializeField] private Color settingsBackgroundTint = new Color(0.11f, 0.14f, 0.10f, 1f);
    [SerializeField] private GameObject mainScreen;
    [SerializeField] private GameObject settingsScreen;
    [SerializeField] private GameObject creditsScreen;
    [SerializeField] private TMP_Text statusText;

    [Header("Main Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button exitButton;

    [Header("Settings Navigation")]
    [SerializeField] private Button settingsBackButton;
    [SerializeField] private Button settingsApplyButton;
    [SerializeField] private Button displayTabButton;
    [SerializeField] private Button keybindTabButton;
    [SerializeField] private Button audioTabButton;
    [SerializeField] private Button graphicsTabButton;
    [SerializeField] private Image displayTabImage;
    [SerializeField] private Image keybindTabImage;
    [SerializeField] private Image audioTabImage;
    [SerializeField] private Image graphicsTabImage;
    [SerializeField] private Sprite tabNormalSprite;
    [SerializeField] private Sprite tabActiveSprite;
    [SerializeField] private Color tabNormalColor = new Color(0.29f, 0.20f, 0.12f, 1f);
    [SerializeField] private Color tabActiveColor = new Color(0.27f, 0.40f, 0.20f, 1f);
    [SerializeField] private GameObject displayTabContent;
    [SerializeField] private GameObject keybindTabContent;
    [SerializeField] private GameObject audioTabContent;
    [SerializeField] private GameObject graphicsTabContent;
    [SerializeField] private TMP_Text settingsStatusText;

    [Header("Credits")]
    [SerializeField] private Button creditsBackButton;

    [Header("Display Controls")]
    [SerializeField] private Button resolutionPreviousButton;
    [SerializeField] private Button resolutionNextButton;
    [SerializeField] private TMP_Text resolutionValueText;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vSyncToggle;
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private TMP_Text brightnessValueText;
    [SerializeField] private Slider uiScaleSlider;
    [SerializeField] private TMP_Text uiScaleValueText;

    private SettingsTab currentTab = SettingsTab.Display;
    private GameSettings.ResolutionChoice[] resolutionChoices = Array.Empty<GameSettings.ResolutionChoice>();
    private int currentResolutionIndex;
    private bool suppressCallbacks;

    private void Awake()
    {
        RandomizeMenuCameraView();
        GameSettings.EnsureDefaults();
        BindUi();
        PopulateResolutionChoices();
        LoadSettingsToUi();
        ShowMain();
    }

    private void OnEnable()
    {
        GameSettings.SettingsChanged += OnExternalSettingsChanged;
    }

    private void OnDisable()
    {
        GameSettings.SettingsChanged -= OnExternalSettingsChanged;
    }

    private void BindUi()
    {
        BindButton(newGameButton, () => LoadGameplayScene(true));
        BindButton(continueButton, ContinueGame);
        BindButton(loadGameButton, LoadSavedGame);
        BindButton(settingsButton, ShowSettings);
        BindButton(creditsButton, ShowCredits);
        BindButton(exitButton, QuitGame);

        BindButton(settingsBackButton, ShowMain);
        BindButton(settingsApplyButton, ApplySettings);

        BindButton(displayTabButton, () => ShowTab(SettingsTab.Display));
        BindButton(keybindTabButton, () => ShowTab(SettingsTab.Keybind));
        BindButton(audioTabButton, () => ShowTab(SettingsTab.Audio));
        BindButton(graphicsTabButton, () => ShowTab(SettingsTab.Graphics));

        BindButton(creditsBackButton, ShowMain);

        BindButton(resolutionPreviousButton, () => StepResolution(-1));
        BindButton(resolutionNextButton, () => StepResolution(1));

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        }

        if (vSyncToggle != null)
        {
            vSyncToggle.onValueChanged.RemoveAllListeners();
            vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.onValueChanged.RemoveAllListeners();
            brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        }

        if (uiScaleSlider != null)
        {
            uiScaleSlider.onValueChanged.RemoveAllListeners();
            uiScaleSlider.onValueChanged.AddListener(OnUiScaleChanged);
        }
    }

    private static void BindButton(Button button, UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void PopulateResolutionChoices()
    {
        resolutionChoices = GameSettings.GetResolutionChoices();
        currentResolutionIndex = FindResolutionIndex(GameSettings.ResolutionWidth, GameSettings.ResolutionHeight, GameSettings.RefreshRate);
        UpdateResolutionLabel();
    }

    private int FindResolutionIndex(int width, int height, int refreshRate)
    {
        if (resolutionChoices == null || resolutionChoices.Length == 0)
        {
            return 0;
        }

        for (int i = 0; i < resolutionChoices.Length; i++)
        {
            GameSettings.ResolutionChoice choice = resolutionChoices[i];
            if (choice.Width == width && choice.Height == height && (refreshRate <= 0 || choice.RefreshRate == refreshRate))
            {
                return i;
            }
        }

        int bestIndex = 0;
        long bestScore = long.MaxValue;
        for (int i = 0; i < resolutionChoices.Length; i++)
        {
            GameSettings.ResolutionChoice choice = resolutionChoices[i];
            long areaDiff = Mathf.Abs((choice.Width * choice.Height) - (width * height));
            long refreshDiff = Mathf.Abs(choice.RefreshRate - refreshRate);
            long score = (areaDiff * 10L) + refreshDiff;
            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void LoadSettingsToUi()
    {
        suppressCallbacks = true;

        PopulateResolutionChoices();

        bool isFullscreen = GameSettings.FullScreenMode != FullScreenMode.Windowed;
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = isFullscreen;
        }

        if (vSyncToggle != null)
        {
            vSyncToggle.isOn = GameSettings.VSync;
        }

        if (brightnessSlider != null)
        {
            brightnessSlider.minValue = 0.45f;
            brightnessSlider.maxValue = 1.45f;
            brightnessSlider.wholeNumbers = false;
            brightnessSlider.value = GameSettings.Brightness;
        }

        if (uiScaleSlider != null)
        {
            uiScaleSlider.minValue = 0.75f;
            uiScaleSlider.maxValue = 1.35f;
            uiScaleSlider.wholeNumbers = false;
            uiScaleSlider.value = GameSettings.UIScale;
        }

        UpdateBrightnessLabel(GameSettings.Brightness);
        UpdateUiScaleLabel(GameSettings.UIScale);
        ApplyUiScalePreview(GameSettings.UIScale);

        suppressCallbacks = false;
        SetSettingsStatus(string.Empty);
        UpdateTabVisuals();
        ShowTab(currentTab);
    }

    private void ShowMain()
    {
        SetPanelState(true, false, false);
        SetBackground(mainBackgroundSprite, mainBackgroundTint);
        SetStatus(string.Empty);
    }

    private void ShowSettings()
    {
        SetPanelState(false, true, false);
        SetBackground(settingsBackgroundSprite, settingsBackgroundTint);
        ShowTab(SettingsTab.Display);
    }

    private void ShowCredits()
    {
        SetPanelState(false, false, true);
        SetBackground(mainBackgroundSprite, mainBackgroundTint);
        SetStatus(string.Empty);
    }

    private void SetPanelState(bool showMain, bool showSettings, bool showCredits)
    {
        if (menuShellRoot != null)
        {
            menuShellRoot.SetActive(showMain || showCredits);
        }

        if (mainScreen != null)
        {
            mainScreen.SetActive(showMain);
        }

        if (settingsScreen != null)
        {
            settingsScreen.SetActive(showSettings);
        }

        if (creditsScreen != null)
        {
            creditsScreen.SetActive(showCredits);
        }
    }

    private void SetBackground(Sprite sprite, Color tint)
    {
        if (backgroundImage == null)
        {
            return;
        }

        if (sprite != null)
        {
            backgroundImage.sprite = sprite;
        }

        backgroundImage.color = tint;
    }

    private void ShowTab(SettingsTab tab)
    {
        currentTab = tab;

        if (displayTabContent != null)
        {
            displayTabContent.SetActive(tab == SettingsTab.Display);
        }

        if (keybindTabContent != null)
        {
            keybindTabContent.SetActive(tab == SettingsTab.Keybind);
        }

        if (audioTabContent != null)
        {
            audioTabContent.SetActive(tab == SettingsTab.Audio);
        }

        if (graphicsTabContent != null)
        {
            graphicsTabContent.SetActive(tab == SettingsTab.Graphics);
        }

        UpdateTabVisuals();
    }

    private void UpdateTabVisuals()
    {
        SetTabSprite(displayTabImage, currentTab == SettingsTab.Display);
        SetTabSprite(keybindTabImage, currentTab == SettingsTab.Keybind);
        SetTabSprite(audioTabImage, currentTab == SettingsTab.Audio);
        SetTabSprite(graphicsTabImage, currentTab == SettingsTab.Graphics);
    }

    private void SetTabSprite(Image image, bool isActive)
    {
        if (image == null)
        {
            return;
        }

        if (isActive && tabActiveSprite != null)
        {
            image.sprite = tabActiveSprite;
        }
        else if (tabNormalSprite != null)
        {
            image.sprite = tabNormalSprite;
        }

        image.color = isActive ? tabActiveColor : tabNormalColor;
    }

    private void StepResolution(int delta)
    {
        if (resolutionChoices == null || resolutionChoices.Length == 0)
        {
            return;
        }

        int newIndex = currentResolutionIndex + delta;
        if (newIndex < 0)
        {
            newIndex = resolutionChoices.Length - 1;
        }
        else if (newIndex >= resolutionChoices.Length)
        {
            newIndex = 0;
        }

        currentResolutionIndex = newIndex;
        ApplyResolutionChoice();
    }

    private void ApplyResolutionChoice()
    {
        if (resolutionChoices == null || resolutionChoices.Length == 0)
        {
            return;
        }

        GameSettings.ResolutionChoice choice = resolutionChoices[currentResolutionIndex];
        GameSettings.ResolutionWidth = choice.Width;
        GameSettings.ResolutionHeight = choice.Height;
        GameSettings.RefreshRate = choice.RefreshRate;
        UpdateResolutionLabel();
        MarkSettingsDirty("Resolution changed.");
    }

    private void UpdateResolutionLabel()
    {
        if (resolutionValueText == null || resolutionChoices == null || resolutionChoices.Length == 0)
        {
            return;
        }

        GameSettings.ResolutionChoice choice = resolutionChoices[Mathf.Clamp(currentResolutionIndex, 0, resolutionChoices.Length - 1)];
        resolutionValueText.text = choice.RefreshRate > 0
            ? $"{choice.Width} x {choice.Height} @ {choice.RefreshRate}Hz"
            : $"{choice.Width} x {choice.Height}";
    }

    private void OnFullscreenChanged(bool value)
    {
        if (suppressCallbacks)
        {
            return;
        }

        GameSettings.FullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        MarkSettingsDirty(value ? "Fullscreen enabled." : "Fullscreen disabled.");
    }

    private void OnVSyncChanged(bool value)
    {
        if (suppressCallbacks)
        {
            return;
        }

        GameSettings.VSync = value;
        MarkSettingsDirty(value ? "VSync enabled." : "VSync disabled.");
    }

    private void OnBrightnessChanged(float value)
    {
        UpdateBrightnessLabel(value);
        if (suppressCallbacks)
        {
            return;
        }

        GameSettings.Brightness = value;
        GameSettings.NotifyChanged();
        MarkSettingsDirty($"Brightness {Mathf.RoundToInt(value * 100f)}%.");
    }

    private void OnUiScaleChanged(float value)
    {
        UpdateUiScaleLabel(value);
        ApplyUiScalePreview(value);
        if (suppressCallbacks)
        {
            return;
        }

        GameSettings.UIScale = value;
        GameSettings.NotifyChanged();
        MarkSettingsDirty($"UI scale {Mathf.RoundToInt(value * 100f)}%.");
    }

    private void UpdateBrightnessLabel(float value)
    {
        if (brightnessValueText != null)
        {
            brightnessValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }

    private void UpdateUiScaleLabel(float value)
    {
        if (uiScaleValueText != null)
        {
            uiScaleValueText.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }
    }

    private void ApplyUiScalePreview(float value)
    {
        if (scaledRoot != null)
        {
            float clamped = Mathf.Clamp(value, 0.75f, 1.35f);
            scaledRoot.localScale = new Vector3(clamped, clamped, 1f);
        }
    }

    private void MarkSettingsDirty(string message)
    {
        SetSettingsStatus($"Pending changes: {message}");
    }

    private void SetSettingsStatus(string message)
    {
        if (settingsStatusText != null)
        {
            settingsStatusText.text = message;
        }
    }

    private void ApplySettings()
    {
        GameSettings.SaveAndApply();
        ApplyUiScalePreview(GameSettings.UIScale);
        SetSettingsStatus("Settings applied.");
    }

    private void OnExternalSettingsChanged()
    {
        if (suppressCallbacks)
        {
            return;
        }

        ApplyUiScalePreview(GameSettings.UIScale);
        UpdateBrightnessLabel(GameSettings.Brightness);
        UpdateUiScaleLabel(GameSettings.UIScale);
    }

    private void ContinueGame()
    {
        if (TryLoadSavedScene())
        {
            return;
        }

        SetStatus("No saved game. Start a new game.");
    }

    private void LoadSavedGame()
    {
        if (TryLoadSavedScene())
        {
            return;
        }

        SetStatus("No saved game found.");
    }

    private bool TryLoadSavedScene()
    {
        string savedScene = PlayerPrefs.GetString("onemorenight.save.scene", string.Empty);
        if (string.IsNullOrWhiteSpace(savedScene))
        {
            return false;
        }

        LoadSceneByName(savedScene);
        return true;
    }

    private void LoadGameplayScene(bool newGame)
    {
        if (newGame)
        {
            PlayerPrefs.DeleteKey("onemorenight.save.scene");
        }

        LoadSceneByName(gameplaySceneName);
    }

    private void LoadSceneByName(string sceneName)
    {
        string trimmed = sceneName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            SetStatus("Gameplay scene name is empty.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(trimmed))
        {
            SetStatus($"Scene '{trimmed}' is not in Build Settings.");
            return;
        }

        GameSettings.SaveAndApply();
        SceneManager.LoadScene(trimmed);
    }

    private void QuitGame()
    {
        GameSettings.SaveAndApply();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private static void RandomizeMenuCameraView()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Vector3 focusPoint = new Vector3(0f, 1.2f, 0f);
        float yaw = UnityEngine.Random.Range(-165f, 165f);
        float distance = UnityEngine.Random.Range(6f, 10f);
        float height = UnityEngine.Random.Range(1.4f, 3.6f);

        Vector3 orbitDirection = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 position = focusPoint - (orbitDirection * distance);
        position.y = height;

        Vector3 target = focusPoint + new Vector3(
            UnityEngine.Random.Range(-0.8f, 0.8f),
            UnityEngine.Random.Range(-0.2f, 0.5f),
            UnityEngine.Random.Range(-0.8f, 0.8f));

        camera.transform.position = position;
        camera.transform.rotation = Quaternion.LookRotation((target - position).normalized, Vector3.up);
    }
}
