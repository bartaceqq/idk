using System; using System.Collections; using System.Collections.Generic; using TMPro; using UnityEngine; using UnityEngine.EventSystems; using UnityEngine.Events; using UnityEngine.SceneManagement; using UnityEngine.UI;

public sealed class FantasyMenuController : MonoBehaviour {
    private static FantasyMenuController instance;

    private enum SettingsTab {
        Display,
        Keybind,
        Audio,
        Graphics }

    private struct KeybindEntry {
        public string KeyId;
        public KeyCode Fallback;
        public Button Button;
        public TMP_Text Label; }

    [Header("Scene")]
    [SerializeField] private string gameplaySceneName = "SampleScene";
    [SerializeField] private bool randomizeMenuCameraOnStart = false;

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

    [Header("Loading")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private TMP_Text loadingTitleText;
    [SerializeField] private TMP_Text loadingMessageText;
    [SerializeField] private TMP_Text loadingStageText;
    [SerializeField] private TMP_Text loadingProgressText;
    [SerializeField] private Slider loadingProgressSlider;
    [SerializeField] private RectTransform loadingSpinner;
    [SerializeField] private float minimumLoadingSeconds = 1.25f;
    [SerializeField] private float postSceneWarmupSeconds = 0.35f;
    [SerializeField] private float textureStreamingTimeoutSeconds = 4f;

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
    [SerializeField] private Button settingsSaveButton;
    [SerializeField] private Button settingsExitGameButton;
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

    [Header("Audio Controls")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private TMP_Text masterVolumeValueText;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private TMP_Text musicVolumeValueText;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private TMP_Text sfxVolumeValueText;
    [SerializeField] private Slider ambienceVolumeSlider;
    [SerializeField] private TMP_Text ambienceVolumeValueText;
    [SerializeField] private Toggle mutedToggle;

    [Header("Graphics Controls")]
    [SerializeField] private Button qualityPreviousButton;
    [SerializeField] private Button qualityNextButton;
    [SerializeField] private TMP_Text qualityValueText;
    [SerializeField] private Button frameRatePreviousButton;
    [SerializeField] private Button frameRateNextButton;
    [SerializeField] private TMP_Text frameRateValueText;
    [SerializeField] private Slider renderScaleSlider;
    [SerializeField] private TMP_Text renderScaleValueText;
    [SerializeField] private Button antiAliasingPreviousButton;
    [SerializeField] private Button antiAliasingNextButton;
    [SerializeField] private TMP_Text antiAliasingValueText;
    [SerializeField] private Button shadowQualityPreviousButton;
    [SerializeField] private Button shadowQualityNextButton;
    [SerializeField] private TMP_Text shadowQualityValueText;
    [SerializeField] private Slider shadowDistanceSlider;
    [SerializeField] private TMP_Text shadowDistanceValueText;
    [SerializeField] private Button textureQualityPreviousButton;
    [SerializeField] private Button textureQualityNextButton;
    [SerializeField] private TMP_Text textureQualityValueText;
    [SerializeField] private Toggle anisotropicFilteringToggle;
    [SerializeField] private Slider viewDistanceSlider;
    [SerializeField] private TMP_Text viewDistanceValueText;
    [SerializeField] private Toggle bloomToggle;
    [SerializeField] private Toggle motionBlurToggle;

    [Header("Keybind Controls")]
    [SerializeField] private Button moveForwardKeyButton;
    [SerializeField] private Button moveBackwardKeyButton;
    [SerializeField] private Button moveLeftKeyButton;
    [SerializeField] private Button moveRightKeyButton;
    [SerializeField] private Button jumpKeyButton;
    [SerializeField] private Button sprintKeyButton;
    [SerializeField] private Button interactKeyButton;
    [SerializeField] private Button attackKeyButton;
    [SerializeField] private Button inventoryKeyButton;
    [SerializeField] private TMP_Text keybindInfoText;

    private SettingsTab currentTab = SettingsTab.Display;
    private GameSettings.ResolutionChoice[] resolutionChoices = Array.Empty<GameSettings.ResolutionChoice>();
    private int currentResolutionIndex;
    private bool suppressCallbacks;
    private readonly List<KeybindEntry> keybindEntries = new List<KeybindEntry>();
    private string pendingKeybindId = string.Empty;
    private KeyCode pendingKeybindFallback = KeyCode.None;
    private Button pendingKeybindButton;
    private TMP_Text pendingKeybindLabel;
    private bool isWaitingForKeybind;
    private int keybindCaptureStartFrame;
    private readonly int[] frameRateOptions = { 0, 30, 60, 90, 120, 144, 165, 240 };
    private readonly int[] antiAliasingOptions = { 0, 2, 4, 8 };
    private readonly string[] shadowQualityLabels = { "Off", "Hard", "All" };
    private readonly string[] textureQualityLabels = { "Full", "Half", "Quarter", "Eighth" };
    private string[] qualityNames = Array.Empty<string>();
    private int qualityOptionIndex;
    private int frameRateOptionIndex;
    private int antiAliasingOptionIndex;
    private int shadowQualityOptionIndex;
    private int textureQualityOptionIndex;
    private string menuSceneName = string.Empty;
    private GameObject menuCanvasRoot;
    private GameObject menuEventSystemRoot;
    private RectTransform settingsFooterRow;
    private bool gameplaySettingsOpen;
    private bool isLoadingScene;
    private bool loadSavedGameAfterSceneLoad;
    private float loadingStartedAt;
    private string loadingBaseMessage = "Loading";

    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return; }

        instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        menuSceneName = SceneManager.GetActiveScene().name;
        CachePersistentRoots();
        PersistUiRoots();

        if (randomizeMenuCameraOnStart) { RandomizeMenuCameraView(); }
        GameSettings.EnsureDefaults();
        BindUi();
        PopulateResolutionChoices();
        LoadSettingsToUi();
        ShowMain(); }

    private void OnEnable() { GameSettings.SettingsChanged += OnExternalSettingsChanged; }

    private void OnDisable() { GameSettings.SettingsChanged -= OnExternalSettingsChanged; }

    private void OnDestroy() { if (instance != this) { return; }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameplayUiState.SetExternalMenuOpen(false);
        instance = null; }

    private void Update() {
        if (isLoadingScene) {
            AnimateLoadingVisuals();
            return; }

        if (isWaitingForKeybind) {
            CaptureNextKeybind();
            return; }

        if (!IsInGameplayScene() || !Input.GetKeyDown(KeyCode.Escape)) { return; }

        if (gameplaySettingsOpen) {
            CloseGameplaySettingsOverlay();
            return; }

        if (!GameplayUiState.IsMenuOpen) { OpenGameplaySettingsOverlay(); } }

    private void BindUi() {
        EnsureSettingsFooterButtons();

        BindButton(newGameButton, () => LoadGameplayScene(true));
        BindButton(continueButton, ContinueGame);
        BindButton(loadGameButton, LoadSavedGame);
        BindButton(settingsButton, ShowSettings);
        BindButton(creditsButton, ShowCredits);
        BindButton(exitButton, QuitGame);

        BindButton(settingsBackButton, ShowMain);
        BindButton(settingsApplyButton, ApplySettings);
        BindButton(settingsSaveButton, SaveCurrentGameFromSettings);
        BindButton(settingsExitGameButton, QuitGame);

        BindButton(displayTabButton, () => ShowTab(SettingsTab.Display));
        BindButton(keybindTabButton, () => ShowTab(SettingsTab.Keybind));
        BindButton(audioTabButton, () => ShowTab(SettingsTab.Audio));
        BindButton(graphicsTabButton, () => ShowTab(SettingsTab.Graphics));

        BindButton(creditsBackButton, ShowMain);

        BindButton(resolutionPreviousButton, () => StepResolution(-1));
        BindButton(resolutionNextButton, () => StepResolution(1));

        BindToggle(fullscreenToggle, OnFullscreenChanged);
        BindToggle(vSyncToggle, OnVSyncChanged);
        BindSlider(brightnessSlider, OnBrightnessChanged);
        BindSlider(uiScaleSlider, OnUiScaleChanged);

        BindAudioUi();
        BindGraphicsUi();
        BindKeybindUi(); }

    private void EnsureSettingsFooterButtons() { if (settingsScreen == null) { return; }

        if (settingsFooterRow == null) {
            Transform footer = FindChildByName(settingsScreen.transform, "Settings Footer Row");
            if (footer != null) { settingsFooterRow = footer as RectTransform; } }

        if (settingsFooterRow == null) { return; }

        if (settingsSaveButton == null) { settingsSaveButton = FindButtonByName(settingsFooterRow, "Settings Save Button"); }

        if (settingsExitGameButton == null) { settingsExitGameButton = FindButtonByName(settingsFooterRow, "Settings Exit Game Button"); }

        if (settingsSaveButton == null && settingsApplyButton != null) {
            settingsSaveButton = Instantiate(settingsApplyButton, settingsFooterRow);
            settingsSaveButton.name = "Settings Save Button";
            SetButtonLabel(settingsSaveButton, "Save"); }

        if (settingsExitGameButton == null) {
            Button source = settingsBackButton != null ? settingsBackButton : exitButton;
            if (source != null) {
                settingsExitGameButton = Instantiate(source, settingsFooterRow);
                settingsExitGameButton.name = "Settings Exit Game Button";
                SetButtonLabel(settingsExitGameButton, "Exit Game"); } }

        ConfigureSettingsFooterLayout(); }

    private void ConfigureSettingsFooterLayout() { if (settingsFooterRow == null) { return; }

        HorizontalLayoutGroup layoutGroup = settingsFooterRow.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup != null) { layoutGroup.enabled = false; }

        PositionFooterButton(settingsSaveButton, new Vector2(0f, 0.5f), new Vector2(112f, 0f));
        PositionFooterButton(settingsBackButton, new Vector2(0.5f, 0.5f), new Vector2(-110f, 0f));
        PositionFooterButton(settingsApplyButton, new Vector2(0.5f, 0.5f), new Vector2(110f, 0f));
        PositionFooterButton(settingsExitGameButton, new Vector2(1f, 0.5f), new Vector2(-112f, 0f)); }

    private static void PositionFooterButton(Button button, Vector2 anchor, Vector2 anchoredPosition) { if (button == null) { return; }

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform == null) { return; }

        rectTransform.anchorMin = anchor;
        rectTransform.anchorMax = anchor;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(200f, 56f);

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement != null) { layoutElement.ignoreLayout = true; } }

    private static Button FindButtonByName(Transform root, string name) { if (root == null || string.IsNullOrEmpty(name)) { return null; }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++) { if (string.Equals(buttons[i].name, name, StringComparison.Ordinal)) { return buttons[i]; } }

        return null; }

    private static Transform FindChildByName(Transform root, string name) { if (root == null || string.IsNullOrEmpty(name)) { return null; }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++) { if (string.Equals(transforms[i].name, name, StringComparison.Ordinal)) { return transforms[i]; } }

        return null; }

    private static void SetButtonLabel(Button button, string labelText) { if (button == null) { return; }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) { label.text = labelText; } }

    private static void BindButton(Button button, UnityAction action) { if (button == null) { return; }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action); }

    private void BindAudioUi() {
        BindSlider(masterVolumeSlider, OnMasterVolumeChanged);
        BindSlider(musicVolumeSlider, OnMusicVolumeChanged);
        BindSlider(sfxVolumeSlider, OnSfxVolumeChanged);
        BindSlider(ambienceVolumeSlider, OnAmbienceVolumeChanged);

        BindToggle(mutedToggle, OnMutedChanged); }

    private void BindGraphicsUi() {
        BindButton(qualityPreviousButton, () => StepQuality(-1));
        BindButton(qualityNextButton, () => StepQuality(1));
        BindButton(frameRatePreviousButton, () => StepFrameRate(-1));
        BindButton(frameRateNextButton, () => StepFrameRate(1));
        BindButton(antiAliasingPreviousButton, () => StepAntiAliasing(-1));
        BindButton(antiAliasingNextButton, () => StepAntiAliasing(1));
        BindButton(shadowQualityPreviousButton, () => StepShadowQuality(-1));
        BindButton(shadowQualityNextButton, () => StepShadowQuality(1));
        BindButton(textureQualityPreviousButton, () => StepTextureQuality(-1));
        BindButton(textureQualityNextButton, () => StepTextureQuality(1));

        BindSlider(renderScaleSlider, OnRenderScaleChanged);
        BindSlider(shadowDistanceSlider, OnShadowDistanceChanged);
        BindSlider(viewDistanceSlider, OnViewDistanceChanged);

        BindToggle(anisotropicFilteringToggle, OnAnisotropicFilteringChanged);
        BindToggle(bloomToggle, OnBloomChanged);
        BindToggle(motionBlurToggle, OnMotionBlurChanged);
        HideAdvancedGraphicsRows(); }

    private void BindKeybindUi() {
        keybindEntries.Clear();
        BindKeybindButton(moveForwardKeyButton, GameSettings.Key.MoveForward, KeyCode.W);
        BindKeybindButton(moveBackwardKeyButton, GameSettings.Key.MoveBackward, KeyCode.S);
        BindKeybindButton(moveLeftKeyButton, GameSettings.Key.MoveLeft, KeyCode.A);
        BindKeybindButton(moveRightKeyButton, GameSettings.Key.MoveRight, KeyCode.D);
        BindKeybindButton(jumpKeyButton, GameSettings.Key.Jump, KeyCode.Space);
        BindKeybindButton(sprintKeyButton, GameSettings.Key.Sprint, KeyCode.LeftShift);
        BindKeybindButton(interactKeyButton, GameSettings.Key.Interact, KeyCode.E);
        BindKeybindButton(attackKeyButton, GameSettings.Key.Attack, KeyCode.Mouse0);
        BindKeybindButton(inventoryKeyButton, GameSettings.Key.Inventory, KeyCode.I); }

    private void BindSlider(Slider slider, UnityAction<float> action) { if (slider == null) { return; }

        slider.onValueChanged.RemoveAllListeners();
        slider.onValueChanged.AddListener(action); }
    private void BindToggle(Toggle toggle, UnityAction<bool> action) { if (toggle == null) { return; }

        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener(action); }
    private static void SetupSlider(Slider slider, float minValue, float maxValue, bool wholeNumbers, float value) { if (slider == null) { return; }

        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.wholeNumbers = wholeNumbers;
        slider.value = value; }

    private void BindKeybindButton(Button button, string keyId, KeyCode fallback) { if (button == null) { return; }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        keybindEntries.Add(new KeybindEntry {
            KeyId = keyId,
            Fallback = fallback,
            Button = button,
            Label = label
        });

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => BeginKeybindCapture(keyId, fallback, button, label)); }

    private void PopulateResolutionChoices() {
        resolutionChoices = GameSettings.GetResolutionChoices();
        currentResolutionIndex = FindResolutionIndex(GameSettings.ResolutionWidth, GameSettings.ResolutionHeight, GameSettings.RefreshRate);
        UpdateResolutionLabel(); }

    private int FindResolutionIndex(int width, int height, int refreshRate) { if (resolutionChoices == null || resolutionChoices.Length == 0) { return 0; }

        for (int i = 0; i < resolutionChoices.Length; i++) {
            GameSettings.ResolutionChoice choice = resolutionChoices[i];
            if (choice.Width == width && choice.Height == height && (refreshRate <= 0 || choice.RefreshRate == refreshRate)) { return i; } }

        int bestIndex = 0;
        long bestScore = long.MaxValue;
        for (int i = 0; i < resolutionChoices.Length; i++) {
            GameSettings.ResolutionChoice choice = resolutionChoices[i];
            long areaDiff = Mathf.Abs((choice.Width * choice.Height) - (width * height));
            long refreshDiff = Mathf.Abs(choice.RefreshRate - refreshRate);
            long score = (areaDiff * 10L) + refreshDiff;
            if (score < bestScore) {
                bestScore = score;
                bestIndex = i; } }

        return bestIndex; }

    private void LoadSettingsToUi() {
        suppressCallbacks = true;

        PopulateResolutionChoices();

        bool isFullscreen = GameSettings.FullScreenMode != FullScreenMode.Windowed;
        if (fullscreenToggle != null) { fullscreenToggle.isOn = isFullscreen; }

        if (vSyncToggle != null) { vSyncToggle.isOn = GameSettings.VSync; }

        SetupSlider(brightnessSlider, 0.45f, 1.45f, false, GameSettings.Brightness);
        SetupSlider(uiScaleSlider, 0.75f, 1.35f, false, GameSettings.UIScale);

        UpdateBrightnessLabel(GameSettings.Brightness);
        UpdateUiScaleLabel(GameSettings.UIScale);
        ApplyUiScalePreview(GameSettings.UIScale);
        LoadAudioSettingsToUi();
        LoadGraphicsSettingsToUi();
        UpdateKeybindLabels();

        suppressCallbacks = false;
        SetSettingsStatus(string.Empty);
        UpdateTabVisuals();
        ShowTab(currentTab); }

    private void ShowMain() {
        HideLoadingScreen();

        if (IsInGameplayScene()) {
            CloseGameplaySettingsOverlay();
            return; }

        gameplaySettingsOpen = false;
        if (menuCanvasRoot != null) { menuCanvasRoot.SetActive(true); }

        SetPanelState(true, false, false);
        SetBackground(mainBackgroundSprite, mainBackgroundTint);
        SetStatus(string.Empty);
        GameplayUiState.SetExternalMenuOpen(true); }

    private void ShowSettings() { if (isLoadingScene) { return; }

        UpdateSettingsFooterContextButtons(false);
        SetPanelState(false, true, false);
        SetBackground(settingsBackgroundSprite, settingsBackgroundTint);
        ShowTab(currentTab);
        GameplayUiState.SetExternalMenuOpen(true); }

    private void ShowCredits() { if (isLoadingScene) { return; }

        SetPanelState(false, false, true);
        SetBackground(mainBackgroundSprite, mainBackgroundTint);
        SetStatus(string.Empty);
        GameplayUiState.SetExternalMenuOpen(true); }

    private void SetPanelState(bool showMain, bool showSettings, bool showCredits) { if (menuShellRoot != null) { menuShellRoot.SetActive(showMain || showCredits); }

        if (mainScreen != null) { mainScreen.SetActive(showMain); }

        if (settingsScreen != null) { settingsScreen.SetActive(showSettings); }

        if (creditsScreen != null) { creditsScreen.SetActive(showCredits); } }

    private void SetBackground(Sprite sprite, Color tint) { if (backgroundImage == null) { return; }

        if (sprite != null) { backgroundImage.sprite = sprite; }

        backgroundImage.enabled = true;
        backgroundImage.color = tint; }

    private void ShowTab(SettingsTab tab) {
        currentTab = tab;

        if (displayTabContent != null) { displayTabContent.SetActive(tab == SettingsTab.Display); }

        if (keybindTabContent != null) { keybindTabContent.SetActive(tab == SettingsTab.Keybind); }

        if (audioTabContent != null) { audioTabContent.SetActive(tab == SettingsTab.Audio); }

        if (graphicsTabContent != null) { graphicsTabContent.SetActive(tab == SettingsTab.Graphics); }

        UpdateTabVisuals(); }

    private void UpdateTabVisuals() {
        SetTabSprite(displayTabImage, currentTab == SettingsTab.Display);
        SetTabSprite(keybindTabImage, currentTab == SettingsTab.Keybind);
        SetTabSprite(audioTabImage, currentTab == SettingsTab.Audio);
        SetTabSprite(graphicsTabImage, currentTab == SettingsTab.Graphics); }

    private void SetTabSprite(Image image, bool isActive) { if (image == null) { return; }

        if (isActive && tabActiveSprite != null) { image.sprite = tabActiveSprite; } else if (tabNormalSprite != null) { image.sprite = tabNormalSprite; }

        image.color = isActive ? tabActiveColor : tabNormalColor; }

    private void StepResolution(int delta) { if (resolutionChoices == null || resolutionChoices.Length == 0) { return; }

        int newIndex = currentResolutionIndex + delta;
        if (newIndex < 0) { newIndex = resolutionChoices.Length - 1; } else if (newIndex >= resolutionChoices.Length) { newIndex = 0; }

        currentResolutionIndex = newIndex;
        ApplyResolutionChoice(); }

    private void ApplyResolutionChoice() { if (resolutionChoices == null || resolutionChoices.Length == 0) { return; }

        GameSettings.ResolutionChoice choice = resolutionChoices[currentResolutionIndex];
        GameSettings.ResolutionWidth = choice.Width;
        GameSettings.ResolutionHeight = choice.Height;
        GameSettings.RefreshRate = choice.RefreshRate;
        UpdateResolutionLabel();
        MarkSettingsDirty("Resolution changed."); }

    private void UpdateResolutionLabel() { if (resolutionValueText == null || resolutionChoices == null || resolutionChoices.Length == 0) { return; }

        GameSettings.ResolutionChoice choice = resolutionChoices[Mathf.Clamp(currentResolutionIndex, 0, resolutionChoices.Length - 1)];
        resolutionValueText.text = choice.RefreshRate > 0
            ? $"{choice.Width} x {choice.Height} @ {choice.RefreshRate}Hz"
            : $"{choice.Width} x {choice.Height}"; }

    private void OnFullscreenChanged(bool value) { if (suppressCallbacks) { return; }

        GameSettings.FullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        MarkSettingsDirty(value ? "Fullscreen enabled." : "Fullscreen disabled."); }

    private void OnVSyncChanged(bool value) { if (suppressCallbacks) { return; }

        GameSettings.VSync = value;
        MarkSettingsDirty(value ? "VSync enabled." : "VSync disabled."); }

    private void OnBrightnessChanged(float value) {
        UpdateBrightnessLabel(value);
        if (suppressCallbacks) { return; }

        GameSettings.Brightness = value;
        GameSettings.NotifyChanged();
        MarkSettingsDirty($"Brightness {Mathf.RoundToInt(value * 100f)}%."); }

    private void OnUiScaleChanged(float value) {
        UpdateUiScaleLabel(value);
        ApplyUiScalePreview(value);
        if (suppressCallbacks) { return; }

        GameSettings.UIScale = value;
        GameSettings.NotifyChanged();
        MarkSettingsDirty($"UI scale {Mathf.RoundToInt(value * 100f)}%."); }

    private void UpdateBrightnessLabel(float value) { if (brightnessValueText != null) { brightnessValueText.text = $"{Mathf.RoundToInt(value * 100f)}%"; } }

    private void UpdateUiScaleLabel(float value) { if (uiScaleValueText != null) { uiScaleValueText.text = $"{Mathf.RoundToInt(value * 100f)}%"; } }

    private void ApplyUiScalePreview(float value) {
        if (scaledRoot != null) {
            float clamped = Mathf.Clamp(value, 0.75f, 1.35f);
            scaledRoot.localScale = new Vector3(clamped, clamped, 1f); } }

    private void LoadAudioSettingsToUi() {
        SetupSlider(masterVolumeSlider, 0f, 1f, false, GameSettings.MasterVolume);
        SetupSlider(musicVolumeSlider, 0f, 1f, false, GameSettings.MusicVolume);
        SetupSlider(sfxVolumeSlider, 0f, 1f, false, GameSettings.SfxVolume);
        SetupSlider(ambienceVolumeSlider, 0f, 1f, false, GameSettings.AmbienceVolume);

        if (mutedToggle != null) { mutedToggle.isOn = GameSettings.Muted; }

        UpdateVolumeLabel(masterVolumeValueText, GameSettings.MasterVolume);
        UpdateVolumeLabel(musicVolumeValueText, GameSettings.MusicVolume);
        UpdateVolumeLabel(sfxVolumeValueText, GameSettings.SfxVolume);
        UpdateVolumeLabel(ambienceVolumeValueText, GameSettings.AmbienceVolume); }

    private void LoadGraphicsSettingsToUi() {
        qualityNames = GameSettings.QualityNames();
        qualityOptionIndex = Mathf.Clamp(GameSettings.QualityIndex, 0, Mathf.Max(0, qualityNames.Length - 1));
        frameRateOptionIndex = FindClosestOptionIndex(frameRateOptions, GameSettings.TargetFrameRate);
        antiAliasingOptionIndex = FindClosestOptionIndex(antiAliasingOptions, GameSettings.AntiAliasing);
        shadowQualityOptionIndex = Mathf.Clamp(GameSettings.ShadowQuality, 0, shadowQualityLabels.Length - 1);
        textureQualityOptionIndex = Mathf.Clamp(GameSettings.TextureQuality, 0, textureQualityLabels.Length - 1);

        SetupSlider(renderScaleSlider, 0.5f, 1.5f, false, GameSettings.RenderScale);
        SetupSlider(shadowDistanceSlider, 0f, 500f, false, GameSettings.ShadowDistance);
        SetupSlider(viewDistanceSlider, 0.45f, 2f, false, GameSettings.ViewDistance);

        if (anisotropicFilteringToggle != null) { anisotropicFilteringToggle.isOn = GameSettings.AnisotropicFiltering; }

        if (bloomToggle != null) { bloomToggle.isOn = GameSettings.Bloom; }

        if (motionBlurToggle != null) { motionBlurToggle.isOn = GameSettings.MotionBlur; }

        UpdateQualityLabel();
        UpdateFrameRateLabel();
        UpdateAntiAliasingLabel();
        UpdateShadowQualityLabel();
        UpdateTextureQualityLabel();
        UpdateRenderScaleLabel(GameSettings.RenderScale);
        UpdateShadowDistanceLabel(GameSettings.ShadowDistance);
        UpdateViewDistanceLabel(GameSettings.ViewDistance); }

    private void OnMasterVolumeChanged(float value) { ApplyVolumeChange(masterVolumeValueText, value, v => GameSettings.MasterVolume = v, "Master"); }

    private void OnMusicVolumeChanged(float value) { ApplyVolumeChange(musicVolumeValueText, value, v => GameSettings.MusicVolume = v, "Music"); }

    private void OnSfxVolumeChanged(float value) { ApplyVolumeChange(sfxVolumeValueText, value, v => GameSettings.SfxVolume = v, "SFX"); }

    private void OnAmbienceVolumeChanged(float value) { ApplyVolumeChange(ambienceVolumeValueText, value, v => GameSettings.AmbienceVolume = v, "Ambience"); }

    private void ApplyVolumeChange(TMP_Text label, float value, Action<float> setVolume, string volumeName) {
        UpdateVolumeLabel(label, value);
        if (suppressCallbacks) { return; }

        setVolume(value);
        GameSettings.ApplyAudioSettings();
        GameSettings.NotifyChanged();
        MarkSettingsDirty($"{volumeName} volume {Mathf.RoundToInt(value * 100f)}%."); }

    private void OnMutedChanged(bool value) { if (suppressCallbacks) { return; }

        GameSettings.Muted = value;
        GameSettings.ApplyAudioSettings();
        GameSettings.NotifyChanged();
        MarkSettingsDirty(value ? "Audio muted." : "Audio unmuted."); }

    private void StepQuality(int delta) { if (qualityNames == null || qualityNames.Length == 0) { qualityNames = GameSettings.QualityNames(); }

        qualityOptionIndex = WrapIndex(qualityOptionIndex + delta, Mathf.Max(1, qualityNames.Length));
        GameSettings.ApplyGraphicsPreset(qualityOptionIndex);
        GameSettings.NotifyChanged();
        LoadGraphicsSettingsToUi();
        MarkSettingsDirty($"Quality {qualityNames[Mathf.Clamp(qualityOptionIndex, 0, qualityNames.Length - 1)]}."); }

    private void StepFrameRate(int delta) {
        frameRateOptionIndex = WrapIndex(frameRateOptionIndex + delta, frameRateOptions.Length);
        int target = frameRateOptions[frameRateOptionIndex];
        GameSettings.TargetFrameRate = target;
        GameSettings.ApplyDisplaySettings();
        GameSettings.NotifyChanged();
        UpdateFrameRateLabel();
        MarkSettingsDirty(target <= 0 ? "Frame rate unlimited." : $"Target frame rate {target}."); }

    private void StepAntiAliasing(int delta) {
        antiAliasingOptionIndex = WrapIndex(antiAliasingOptionIndex + delta, antiAliasingOptions.Length);
        int value = antiAliasingOptions[antiAliasingOptionIndex];
        GameSettings.AntiAliasing = value;
        GameSettings.ApplyGraphicsSettings();
        GameSettings.NotifyChanged();
        UpdateAntiAliasingLabel();
        MarkSettingsDirty(value == 0 ? "Anti-aliasing off." : $"Anti-aliasing {value}x."); }

    private void StepShadowQuality(int delta) {
        shadowQualityOptionIndex = WrapIndex(shadowQualityOptionIndex + delta, shadowQualityLabels.Length);
        GameSettings.ShadowQuality = shadowQualityOptionIndex;
        GameSettings.ApplyGraphicsSettings();
        GameSettings.NotifyChanged();
        UpdateShadowQualityLabel();
        MarkSettingsDirty($"Shadows {shadowQualityLabels[shadowQualityOptionIndex]}."); }

    private void StepTextureQuality(int delta) {
        textureQualityOptionIndex = WrapIndex(textureQualityOptionIndex + delta, textureQualityLabels.Length);
        GameSettings.TextureQuality = textureQualityOptionIndex;
        GameSettings.ApplyGraphicsSettings();
        GameSettings.NotifyChanged();
        UpdateTextureQualityLabel();
        MarkSettingsDirty($"Texture quality {textureQualityLabels[textureQualityOptionIndex]}."); }

    private void OnRenderScaleChanged(float value) {
        UpdateRenderScaleLabel(value);
        if (suppressCallbacks) { return; }

        GameSettings.RenderScale = value;
        GameSettings.ApplyGraphicsSettings();
        GameSettings.NotifyChanged();
        MarkSettingsDirty($"Render scale {value:0.00}x."); }

    private void OnShadowDistanceChanged(float value) {
        UpdateShadowDistanceLabel(value);
        if (suppressCallbacks) { return; }

        GameSettings.ShadowDistance = value;
        GameSettings.ApplyGraphicsSettings();
        GameSettings.NotifyChanged();
        MarkSettingsDirty($"Shadow distance {Mathf.RoundToInt(value)}m."); }

    private void OnViewDistanceChanged(float value) {
        UpdateViewDistanceLabel(value);
        if (suppressCallbacks) { return; }

        GameSettings.ViewDistance = value;
        GameSettings.ApplyGraphicsSettings();
        GameSettings.NotifyChanged();
        MarkSettingsDirty($"View distance {value:0.00}x."); }

    private void OnAnisotropicFilteringChanged(bool value) { if (suppressCallbacks) { return; }

        GameSettings.AnisotropicFiltering = value;
        GameSettings.ApplyGraphicsSettings();
        GameSettings.NotifyChanged();
        MarkSettingsDirty(value ? "Anisotropic filtering enabled." : "Anisotropic filtering disabled."); }

    private void OnBloomChanged(bool value) { if (suppressCallbacks) { return; }

        GameSettings.Bloom = value;
        GameSettings.ApplyGraphicsSettings();
        GameSettings.NotifyChanged();
        MarkSettingsDirty(value ? "Bloom enabled." : "Bloom disabled."); }

    private void OnMotionBlurChanged(bool value) { if (suppressCallbacks) { return; }

        GameSettings.MotionBlur = value;
        GameSettings.ApplyGraphicsSettings();
        GameSettings.NotifyChanged();
        MarkSettingsDirty(value ? "Motion blur enabled." : "Motion blur disabled."); }

    private void BeginKeybindCapture(string keyId, KeyCode fallback, Button button, TMP_Text label) {
        isWaitingForKeybind = true;
        pendingKeybindId = keyId;
        pendingKeybindFallback = fallback;
        pendingKeybindButton = button;
        pendingKeybindLabel = label;
        keybindCaptureStartFrame = Time.frameCount;

        if (pendingKeybindLabel != null) { pendingKeybindLabel.text = "Press key..."; }

        if (keybindInfoText != null) { keybindInfoText.text = "Press any key (Esc cancels)"; }

        SetSettingsStatus($"Rebinding {keyId}..."); }

    private void CaptureNextKeybind() { if (Time.frameCount == keybindCaptureStartFrame) { return; }

        if (Input.GetKeyDown(KeyCode.Escape)) {
            CancelKeybindCapture();
            return; }

        Array values = Enum.GetValues(typeof(KeyCode));
        for (int i = 0; i < values.Length; i++) {
            KeyCode keyCode = (KeyCode)values.GetValue(i);
            if (keyCode == KeyCode.None) { continue; }

            if (!Input.GetKeyDown(keyCode)) { continue; }

            ApplyCapturedKeybind(keyCode);
            return; } }

    private void CancelKeybindCapture() {
        isWaitingForKeybind = false;
        pendingKeybindId = string.Empty;
        pendingKeybindButton = null;
        pendingKeybindLabel = null;
        if (keybindInfoText != null) { keybindInfoText.text = string.Empty; }

        UpdateKeybindLabels();
        SetSettingsStatus("Keybind capture canceled."); }

    private void ApplyCapturedKeybind(KeyCode keyCode) {
        isWaitingForKeybind = false;
        GameSettings.SetKey(pendingKeybindId, keyCode, false);
        GameSettings.ApplyInputOverridesToScene();
        GameSettings.NotifyChanged();
        UpdateKeybindLabels();

        if (keybindInfoText != null) { keybindInfoText.text = string.Empty; }

        MarkSettingsDirty($"{pendingKeybindId} -> {GameSettings.ToDisplayName(keyCode)}.");
        pendingKeybindId = string.Empty;
        pendingKeybindButton = null;
        pendingKeybindLabel = null; }

    private void UpdateKeybindLabels() {
        for (int i = 0; i < keybindEntries.Count; i++) {
            KeybindEntry entry = keybindEntries[i];
            if (entry.Label == null) { continue; }

            if (isWaitingForKeybind && entry.Button == pendingKeybindButton) {
                entry.Label.text = "Press key...";
                continue; }

            entry.Label.text = GameSettings.GetKeyDisplayName(entry.KeyId, entry.Fallback); } }

    private void UpdateVolumeLabel(TMP_Text text, float value) { if (text != null) { text.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%"; } }

    private void UpdateQualityLabel() { if (qualityValueText == null) { return; }

        if (qualityNames == null || qualityNames.Length == 0) { qualityNames = GameSettings.QualityNames(); }

        int index = Mathf.Clamp(qualityOptionIndex, 0, qualityNames.Length - 1);
        qualityValueText.text = qualityNames[index]; }

    private void HideAdvancedGraphicsRows() {
        SetGraphicsRowVisible(frameRatePreviousButton, false);
        SetGraphicsRowVisible(frameRateNextButton, false);
        SetGraphicsRowVisible(frameRateValueText, false);
        SetGraphicsRowVisible(renderScaleSlider, false);
        SetGraphicsRowVisible(renderScaleValueText, false);
        SetGraphicsRowVisible(antiAliasingPreviousButton, false);
        SetGraphicsRowVisible(antiAliasingNextButton, false);
        SetGraphicsRowVisible(antiAliasingValueText, false);
        SetGraphicsRowVisible(shadowQualityPreviousButton, false);
        SetGraphicsRowVisible(shadowQualityNextButton, false);
        SetGraphicsRowVisible(shadowQualityValueText, false);
        SetGraphicsRowVisible(shadowDistanceSlider, false);
        SetGraphicsRowVisible(shadowDistanceValueText, false);
        SetGraphicsRowVisible(textureQualityPreviousButton, false);
        SetGraphicsRowVisible(textureQualityNextButton, false);
        SetGraphicsRowVisible(textureQualityValueText, false);
        SetGraphicsRowVisible(anisotropicFilteringToggle, false);
        SetGraphicsRowVisible(viewDistanceSlider, false);
        SetGraphicsRowVisible(viewDistanceValueText, false);
        SetGraphicsRowVisible(bloomToggle, false);
        SetGraphicsRowVisible(motionBlurToggle, false); }

    private void SetGraphicsRowVisible(Component control, bool visible) {
        if (control == null) { return; }

        Transform row = FindGraphicsRow(control.transform);
        if (row != null && !IsQualityRow(row)) {
            row.gameObject.SetActive(visible);
            return; }

        control.gameObject.SetActive(visible); }

    private Transform FindGraphicsRow(Transform control) {
        if (control == null || graphicsTabContent == null) { return null; }

        Transform content = graphicsTabContent.transform;
        Transform current = control;
        while (current != null && current.parent != content) { current = current.parent; }

        return current; }

    private bool IsQualityRow(Transform row) {
        if (row == null) { return false; }

        return IsControlInRow(row, qualityPreviousButton) ||
               IsControlInRow(row, qualityNextButton) ||
               IsControlInRow(row, qualityValueText); }

    private static bool IsControlInRow(Transform row, Component control) {
        return row != null && control != null && control.transform.IsChildOf(row); }

    private void UpdateFrameRateLabel() {
        if (frameRateValueText != null) {
            int value = frameRateOptions[Mathf.Clamp(frameRateOptionIndex, 0, frameRateOptions.Length - 1)];
            frameRateValueText.text = value <= 0 ? "Unlimited" : value.ToString(); } }

    private void UpdateAntiAliasingLabel() {
        if (antiAliasingValueText != null) {
            int value = antiAliasingOptions[Mathf.Clamp(antiAliasingOptionIndex, 0, antiAliasingOptions.Length - 1)];
            antiAliasingValueText.text = value <= 0 ? "Off" : $"{value}x"; } }

    private void UpdateShadowQualityLabel() {
        if (shadowQualityValueText != null) {
            int index = Mathf.Clamp(shadowQualityOptionIndex, 0, shadowQualityLabels.Length - 1);
            shadowQualityValueText.text = shadowQualityLabels[index]; } }

    private void UpdateTextureQualityLabel() {
        if (textureQualityValueText != null) {
            int index = Mathf.Clamp(textureQualityOptionIndex, 0, textureQualityLabels.Length - 1);
            textureQualityValueText.text = textureQualityLabels[index]; } }

    private void UpdateRenderScaleLabel(float value) { if (renderScaleValueText != null) { renderScaleValueText.text = $"{value:0.00}x"; } }

    private void UpdateShadowDistanceLabel(float value) { if (shadowDistanceValueText != null) { shadowDistanceValueText.text = $"{Mathf.RoundToInt(value)}m"; } }

    private void UpdateViewDistanceLabel(float value) { if (viewDistanceValueText != null) { viewDistanceValueText.text = $"{value:0.00}x"; } }

    private static int FindClosestOptionIndex(int[] values, int target) { if (values == null || values.Length == 0) { return 0; }

        int bestIndex = 0;
        int bestDelta = int.MaxValue;
        for (int i = 0; i < values.Length; i++) {
            int delta = Mathf.Abs(values[i] - target);
            if (delta < bestDelta) {
                bestDelta = delta;
                bestIndex = i; } }

        return bestIndex; }

    private static int WrapIndex(int value, int length) { if (length <= 0) { return 0; }

        int result = value % length;
        if (result < 0) { result += length; }

        return result; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { if (instance != this) { return; }

        if (IsMenuScene(scene.name)) {
            isLoadingScene = false;
            gameplaySettingsOpen = false;
            if (menuCanvasRoot != null) { menuCanvasRoot.SetActive(true); }

            HideLoadingScreen();
            ShowMain();
            return; }

        if (isLoadingScene) { return; }

        HideLoadingScreen();
        CloseGameplaySettingsOverlay(); }

    private void CachePersistentRoots() {
        if (menuCanvasRoot == null) {
            Canvas rootCanvas = null;
            if (scaledRoot != null) { rootCanvas = scaledRoot.GetComponentInParent<Canvas>(); }

            if (rootCanvas == null && settingsScreen != null) { rootCanvas = settingsScreen.GetComponentInParent<Canvas>(true); }

            if (rootCanvas == null && backgroundImage != null) { rootCanvas = backgroundImage.GetComponentInParent<Canvas>(true); }

            if (rootCanvas != null) { menuCanvasRoot = rootCanvas.gameObject; } }

        if (menuEventSystemRoot == null && EventSystem.current != null) { menuEventSystemRoot = EventSystem.current.gameObject; } }

    private void PersistUiRoots() { if (menuCanvasRoot != null) { DontDestroyOnLoad(menuCanvasRoot.transform.root.gameObject); }

        if (menuEventSystemRoot != null) { DontDestroyOnLoad(menuEventSystemRoot.transform.root.gameObject); } }

    private bool IsInGameplayScene() {
        Scene activeScene = SceneManager.GetActiveScene();
        return !IsMenuScene(activeScene.name); }

    private bool IsMenuScene(string sceneName) { return string.Equals(sceneName, menuSceneName, StringComparison.Ordinal); }

    private void OpenGameplaySettingsOverlay() {
        gameplaySettingsOpen = true;
        if (menuCanvasRoot != null) { menuCanvasRoot.SetActive(true); }

        if (menuEventSystemRoot != null) { menuEventSystemRoot.SetActive(true); }

        SetPanelState(false, true, false);
        UpdateSettingsFooterContextButtons(true);
        ShowTab(currentTab);
        SetStatus(string.Empty);
        if (backgroundImage != null) { backgroundImage.enabled = false; }

        LoadSettingsToUi();
        GameplayUiState.SetExternalMenuOpen(true); }

    private void CloseGameplaySettingsOverlay() {
        gameplaySettingsOpen = false;
        SetPanelState(false, false, false);
        SetStatus(string.Empty);
        SetSettingsStatus(string.Empty);
        if (menuCanvasRoot != null && IsInGameplayScene()) { menuCanvasRoot.SetActive(false); }

        if (backgroundImage != null) { backgroundImage.enabled = false; }

        GameplayUiState.SetExternalMenuOpen(false); }

    private void MarkSettingsDirty(string message) { SetSettingsStatus($"Pending changes: {message}"); }

    private void SetSettingsStatus(string message) { if (settingsStatusText != null) { settingsStatusText.text = message; } }

    private void ApplySettings() {
        isWaitingForKeybind = false;
        pendingKeybindId = string.Empty;
        pendingKeybindButton = null;
        pendingKeybindLabel = null;
        if (keybindInfoText != null) { keybindInfoText.text = string.Empty; }

        GameSettings.SaveAndApply();
        ApplyUiScalePreview(GameSettings.UIScale);
        LoadAudioSettingsToUi();
        LoadGraphicsSettingsToUi();
        UpdateKeybindLabels();
        SetSettingsStatus("Settings applied."); }

    private void UpdateSettingsFooterContextButtons(bool inGameplayEscOverlay) { if (settingsSaveButton != null) { settingsSaveButton.gameObject.SetActive(inGameplayEscOverlay); }

        if (settingsExitGameButton != null) { settingsExitGameButton.gameObject.SetActive(inGameplayEscOverlay); } }

    private void OnExternalSettingsChanged() { if (suppressCallbacks) { return; }

        LoadSettingsToUi(); }

    private void ContinueGame() { if (TryLoadSavedScene("Continuing saved game...")) { return; }

        SetStatus("No saved game. Start a new game."); }

    private void LoadSavedGame() { if (TryLoadSavedScene("Loading saved game...")) { return; }

        SetStatus("No saved game found."); }

    private bool TryLoadSavedScene(string loadingMessage) { if (!GameSaveManager.TryGetSavedSceneName(out string savedScene)) { return false; }

        loadSavedGameAfterSceneLoad = true;
        LoadSceneByName(savedScene, loadingMessage);
        return true; }

    private void LoadGameplayScene(bool newGame) {
        if (newGame) {
            GameSaveManager.DeleteSave();
            loadSavedGameAfterSceneLoad = false; }

        LoadSceneByName(gameplaySceneName, newGame ? "Starting new game..." : "Loading game..."); }

    private void SaveCurrentGameFromSettings() {
        ApplySettings();
        if (!IsInGameplayScene()) {
            SetSettingsStatus("Save is only available in game.");
            return; }

        if (GameSaveManager.SaveCurrentGame(out string message)) {
            SetSettingsStatus(message);
            return; }

        SetSettingsStatus(message); }

    private void LoadSceneByName(string sceneName, string loadingMessage) { if (isLoadingScene) { return; }

        string trimmed = sceneName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) {
            SetStatus("Gameplay scene name is empty.");
            return; }

        if (!Application.CanStreamedLevelBeLoaded(trimmed)) {
            SetStatus($"Scene '{trimmed}' is not in Build Settings.");
            return; }

        StartCoroutine(LoadSceneWithLoading(trimmed, loadingMessage)); }

    private IEnumerator LoadSceneWithLoading(string sceneName, string loadingMessage) {
        isLoadingScene = true;
        ShowLoadingScreen(loadingMessage);
        yield return null;

        GameSettings.SaveAndApply();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null) {
            isLoadingScene = false;
            HideLoadingScreen();
            ShowMain();
            SetStatus($"Could not load scene '{sceneName}'.");
            yield break; }

        operation.allowSceneActivation = false;
        SetLoadingStage("Loading scene data");
        while (operation.progress < 0.9f) {
            SetLoadingProgress(Mathf.Lerp(0.05f, 0.82f, Mathf.Clamp01(operation.progress / 0.9f)));
            yield return null; }

        while (Time.unscaledTime - loadingStartedAt < minimumLoadingSeconds) {
            SetLoadingStage("Preparing world");
            SetLoadingProgress(Mathf.MoveTowards(loadingProgressSlider != null ? loadingProgressSlider.value : 0.82f, 0.88f, Time.unscaledDeltaTime * 0.2f));
            yield return null; }

        SetLoadingStage("Activating world");
        SetLoadingProgress(0.9f);
        operation.allowSceneActivation = true;

        while (!operation.isDone) { yield return null; }

        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (!loadedScene.IsValid()) { loadedScene = SceneManager.GetActiveScene(); }

        yield return WarmupLoadedScene(loadedScene);

        if (loadSavedGameAfterSceneLoad) {
            SetLoadingStage("Restoring save");
            SetLoadingProgress(0.995f);
            loadSavedGameAfterSceneLoad = false;
            if (!GameSaveManager.LoadSavedGameIntoActiveScene(out string saveMessage)) { SetStatus(saveMessage); }
            yield return null; }

        SetLoadingProgress(1f);
        isLoadingScene = false;
        HideLoadingScreen();
        CloseGameplaySettingsOverlay(); }

    private IEnumerator WarmupLoadedScene(Scene scene) {
        SetLoadingStage("Warming shaders");
        SetLoadingProgress(0.92f);
        yield return null;

        Shader.WarmupAllShaders();
        yield return null;

        SetLoadingStage("Preparing materials");
        TouchSceneRenderers(scene);
        SetLoadingProgress(0.95f);
        yield return new WaitForEndOfFrame();

        bool previousForceLoadAll = Texture.streamingTextureForceLoadAll;
        Texture.streamingTextureForceLoadAll = true;

        float warmupEnd = Time.unscaledTime + postSceneWarmupSeconds;
        while (Time.unscaledTime < warmupEnd) {
            SetLoadingStage("Uploading textures");
            SetLoadingProgress(Mathf.Lerp(0.95f, 0.98f, 1f - ((warmupEnd - Time.unscaledTime) / Mathf.Max(0.01f, postSceneWarmupSeconds))));
            yield return null; }

        float timeoutAt = Time.unscaledTime + textureStreamingTimeoutSeconds;
        while (Texture.streamingTextureLoadingCount > 0 && Time.unscaledTime < timeoutAt) {
            SetLoadingStage($"Loading textures ({Texture.streamingTextureLoadingCount})");
            SetLoadingProgress(0.98f);
            yield return null; }

        Texture.streamingTextureForceLoadAll = previousForceLoadAll;
        SetLoadingStage("Finishing");
        SetLoadingProgress(0.99f);
        yield return null; }

    private static void TouchSceneRenderers(Scene scene) {
        Renderer[] renderers = UnitySceneSearch.FindAll<Renderer>();
        for (int i = 0; i < renderers.Length; i++) {
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.gameObject.scene != scene) { continue; }

            Material[] materials = renderer.sharedMaterials;
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++) {
                Material material = materials[materialIndex];
                if (material == null) { continue; }

                Shader shader = material.shader;
                if (shader == null) { continue; }

                string[] textureNames = material.GetTexturePropertyNames();
                for (int textureIndex = 0; textureIndex < textureNames.Length; textureIndex++) {
                    Texture texture = material.GetTexture(textureNames[textureIndex]);
                    if (texture == null) { continue; }

                    _ = texture.width;
                    _ = texture.height; } } } }

    private void ShowLoadingScreen(string message) {
        loadingStartedAt = Time.unscaledTime;
        if (menuCanvasRoot != null) { menuCanvasRoot.SetActive(true); }

        if (menuEventSystemRoot != null) { menuEventSystemRoot.SetActive(true); }

        SetPanelState(false, false, false);
        SetBackground(null, new Color(0f, 0f, 0f, 1f));
        if (loadingScreen != null) { loadingScreen.SetActive(true); }

        if (loadingTitleText != null) { loadingTitleText.text = "LOADING"; }

        if (loadingMessageText != null) {
            loadingBaseMessage = string.IsNullOrWhiteSpace(message) ? "Loading" : message.Trim().TrimEnd('.');
            loadingMessageText.text = loadingBaseMessage; }

        SetLoadingStage("Starting");
        SetLoadingProgress(0f);
        GameplayUiState.SetExternalMenuOpen(true); }

    private void HideLoadingScreen() { if (loadingScreen != null) { loadingScreen.SetActive(false); } }

    private void SetLoadingProgress(float progress) {
        float clamped = Mathf.Clamp01(progress);
        if (loadingProgressSlider != null) { loadingProgressSlider.SetValueWithoutNotify(clamped); }

        if (loadingProgressText != null) { loadingProgressText.text = $"{Mathf.RoundToInt(clamped * 100f)}%"; } }

    private void SetLoadingStage(string stage) { if (loadingStageText != null) { loadingStageText.text = stage; } }

    private void AnimateLoadingVisuals() { if (loadingSpinner != null) { loadingSpinner.Rotate(0f, 0f, -180f * Time.unscaledDeltaTime); }

        if (loadingMessageText != null) {
            int dots = Mathf.FloorToInt(Time.unscaledTime * 2.5f) % 4;
            loadingMessageText.text = loadingBaseMessage + new string('.', dots); } }

    private void QuitGame() {
        GameSettings.SaveAndApply();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetStatus(string message) { if (statusText != null) { statusText.text = message; } }

    private static void RandomizeMenuCameraView() {
        Camera camera = Camera.main;
        if (camera == null) { return; }

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
        camera.transform.rotation = Quaternion.LookRotation((target - position).normalized, Vector3.up); } }
