using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FantasyMenuController : MonoBehaviour
{
    private enum SettingsTab
    {
        Display,
        Graphics,
        Audio,
        Keybind
    }

    [Header("Scene")]
    [SerializeField] private string gameplaySceneName = "SampleScene";

    private TMP_FontAsset titleFont;
    private TMP_FontAsset bodyFont;
    private RectTransform scaledRoot;
    private RectTransform mainScreen;
    private RectTransform settingsScreen;
    private RectTransform creditsScreen;
    private RectTransform settingsContent;
    private TMP_Text statusText;
    private TMP_Text settingsStatusText;
    private SettingsTab currentTab;
    private readonly Dictionary<SettingsTab, Button> tabButtons = new Dictionary<SettingsTab, Button>();
    private readonly List<ChoiceControl> choiceControls = new List<ChoiceControl>();
    private GameSettings.ResolutionChoice[] resolutionChoices;

    private string waitingForKeyId;
    private TMP_Text waitingForKeyText;

    private Sprite parchmentSprite;
    private Sprite greenButtonSprite;
    private Sprite greenButtonPressedSprite;
    private Sprite blueButtonSprite;
    private Sprite woodSprite;
    private Sprite darkSprite;
    private Sprite goldSprite;
    private Sprite backgroundSprite;
    private Sprite sliderTrackSprite;
    private Sprite sliderFillSprite;
    private Sprite checkboxSprite;

    private static readonly KeyCode[] RebindableKeys =
    {
        KeyCode.A, KeyCode.B, KeyCode.C, KeyCode.D, KeyCode.E, KeyCode.F, KeyCode.G, KeyCode.H,
        KeyCode.I, KeyCode.J, KeyCode.K, KeyCode.L, KeyCode.M, KeyCode.N, KeyCode.O, KeyCode.P,
        KeyCode.Q, KeyCode.R, KeyCode.S, KeyCode.T, KeyCode.U, KeyCode.V, KeyCode.W, KeyCode.X,
        KeyCode.Y, KeyCode.Z,
        KeyCode.Alpha0, KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4,
        KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9,
        KeyCode.Space, KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftControl, KeyCode.RightControl,
        KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.Tab, KeyCode.Return, KeyCode.Escape,
        KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.Minus, KeyCode.Equals, KeyCode.LeftBracket, KeyCode.RightBracket,
        KeyCode.Semicolon, KeyCode.Quote, KeyCode.Comma, KeyCode.Period, KeyCode.Slash,
        KeyCode.Backslash, KeyCode.BackQuote
    };

    private void Awake()
    {
        GameSettings.EnsureDefaults();
        CacheFonts();
        CreateSprites();
        EnsureEventSystem();
        BuildBackgroundScene();
        BuildCanvas();
        ShowMain();
    }

    private void OnEnable()
    {
        GameSettings.SettingsChanged += ApplyUiScale;
    }

    private void OnDisable()
    {
        GameSettings.SettingsChanged -= ApplyUiScale;
    }

    private void Update()
    {
        if (!string.IsNullOrEmpty(waitingForKeyId))
        {
            CaptureRebindInput();
        }
    }

    private void CacheFonts()
    {
        titleFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Cinzel/static/Cinzel-Black SDF");
        bodyFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Cinzel/static/Cinzel-Black SDF");
        if (bodyFont == null)
        {
            bodyFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }
    }

    private void CreateSprites()
    {
        parchmentSprite = CreateTexturedSprite("Menu Parchment", 256, 128, new Color32(214, 158, 83, 255), new Color32(255, 226, 153, 255), 20, 0.075f);
        greenButtonSprite = CreateTexturedSprite("Menu Green", 256, 96, new Color32(28, 78, 12, 255), new Color32(74, 134, 18, 255), 18, 0.06f);
        greenButtonPressedSprite = CreateTexturedSprite("Menu Green Pressed", 256, 96, new Color32(18, 54, 10, 255), new Color32(58, 112, 14, 255), 18, 0.04f);
        blueButtonSprite = CreateTexturedSprite("Menu Blue", 256, 96, new Color32(26, 40, 65, 255), new Color32(57, 77, 112, 255), 18, 0.055f);
        woodSprite = CreateTexturedSprite("Menu Wood", 256, 96, new Color32(78, 42, 18, 255), new Color32(148, 86, 35, 255), 12, 0.1f);
        darkSprite = CreateTexturedSprite("Menu Dark", 128, 64, new Color32(20, 17, 13, 245), new Color32(48, 36, 24, 245), 10, 0.08f);
        goldSprite = CreateTexturedSprite("Menu Gold", 96, 96, new Color32(159, 91, 13, 255), new Color32(255, 207, 63, 255), 6, 0.04f);
        sliderTrackSprite = CreateTexturedSprite("Menu Slider Track", 256, 42, new Color32(36, 25, 16, 255), new Color32(87, 63, 39, 255), 10, 0.05f);
        sliderFillSprite = CreateTexturedSprite("Menu Slider Fill", 256, 42, new Color32(26, 90, 18, 255), new Color32(100, 164, 31, 255), 8, 0.04f);
        checkboxSprite = CreateTexturedSprite("Menu Checkbox", 96, 96, new Color32(32, 56, 16, 255), new Color32(100, 156, 32, 255), 12, 0.05f);
        backgroundSprite = CreateBackgroundSprite();
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void BuildBackgroundScene()
    {
        Camera existingCamera = Camera.main;
        if (existingCamera == null)
        {
            GameObject cameraObject = new GameObject("Menu Camera");
            existingCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
        }

        existingCamera.transform.position = new Vector3(0f, 3.2f, -9.5f);
        existingCamera.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
        existingCamera.fieldOfView = 48f;
        existingCamera.clearFlags = CameraClearFlags.SolidColor;
        existingCamera.backgroundColor = new Color(0.025f, 0.031f, 0.025f);

        if (existingCamera.GetComponent<AudioListener>() == null)
        {
            existingCamera.gameObject.AddComponent<AudioListener>();
        }

        Light directional = FindFirstObjectByType<Light>();
        if (directional == null)
        {
            GameObject lightObject = new GameObject("Moon Key Light");
            directional = lightObject.AddComponent<Light>();
            directional.type = LightType.Directional;
        }

        directional.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
        directional.color = new Color(0.78f, 0.87f, 1f);
        directional.intensity = 1.05f;

        Material groundMaterial = CreateMaterial("Menu Ground", new Color(0.08f, 0.11f, 0.07f), 0.25f);
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Menu Forest Floor";
        ground.transform.position = new Vector3(0f, -0.15f, 2f);
        ground.transform.localScale = new Vector3(4.5f, 1f, 4.5f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

        Material trunkMaterial = CreateMaterial("Menu Trunk", new Color(0.19f, 0.11f, 0.055f), 0.35f);
        Material leafMaterial = CreateMaterial("Menu Leaves", new Color(0.045f, 0.16f, 0.055f), 0.18f);
        for (int i = 0; i < 16; i++)
        {
            float side = i % 2 == 0 ? -1f : 1f;
            float x = side * UnityEngine.Random.Range(4.8f, 8.5f);
            float z = UnityEngine.Random.Range(-1.5f, 9f);
            float height = UnityEngine.Random.Range(2.8f, 4.8f);
            CreateTree(new Vector3(x, 0f, z), height, trunkMaterial, leafMaterial);
        }

        Material emberMaterial = CreateMaterial("Menu Ember", new Color(1f, 0.48f, 0.08f), 0f);
        CreateTorch(new Vector3(-2.8f, 0.55f, 1.2f), trunkMaterial, emberMaterial);
        CreateTorch(new Vector3(2.8f, 0.55f, 1.2f), trunkMaterial, emberMaterial);
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("Main Menu Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Image background = CreateImage(canvasRect, "Painted Background", backgroundSprite);
        Stretch(background.rectTransform);
        background.color = Color.white;

        scaledRoot = CreateRect(canvasRect, "Scaled Menu Root");
        Stretch(scaledRoot);
        scaledRoot.pivot = new Vector2(0.5f, 0.5f);
        ApplyUiScale();

        mainScreen = CreateRect(scaledRoot, "Main Screen");
        Stretch(mainScreen);
        settingsScreen = CreateRect(scaledRoot, "Settings Screen");
        Stretch(settingsScreen);
        creditsScreen = CreateRect(scaledRoot, "Credits Screen");
        Stretch(creditsScreen);

        BuildMainScreen();
        BuildSettingsScreen();
        BuildCreditsScreen();
    }

    private void BuildMainScreen()
    {
        RectTransform content = CreateRect(mainScreen, "Main Content");
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(1180f, 900f);
        content.anchoredPosition = new Vector2(0f, 15f);

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 20f;
        layout.padding = new RectOffset(0, 0, 30, 30);
        layout.childControlHeight = false;
        layout.childControlWidth = false;

        RectTransform title = CreateBanner(content, "ONE MORE NIGHT", new Vector2(1080f, 170f), 72f);
        AddLayout(title, 1080f, 170f);

        RectTransform buttonStack = CreateRect(content, "Main Button Stack");
        AddLayout(buttonStack, 620f, 560f);
        VerticalLayoutGroup buttonLayout = buttonStack.gameObject.AddComponent<VerticalLayoutGroup>();
        buttonLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonLayout.spacing = 16f;
        buttonLayout.childControlHeight = false;
        buttonLayout.childControlWidth = false;

        CreateMenuButton(buttonStack, "New Game", new Vector2(560f, 78f), () => LoadGameplayScene(true));
        CreateMenuButton(buttonStack, "Continue", new Vector2(560f, 78f), ContinueGame);
        CreateMenuButton(buttonStack, "Load Game", new Vector2(560f, 78f), ContinueGame);
        CreateMenuButton(buttonStack, "Settings", new Vector2(560f, 78f), ShowSettings);
        CreateMenuButton(buttonStack, "Credits", new Vector2(420f, 68f), ShowCredits);
        CreateMenuButton(buttonStack, "Exit", new Vector2(560f, 78f), QuitGame);

        statusText = CreateText(content, "Status Text", string.Empty, 24f, TextAlignmentOptions.Center);
        statusText.color = new Color(0.95f, 0.78f, 0.38f);
        AddLayout(statusText.rectTransform, 900f, 44f);

        CreateSideBanner(mainScreen, -615f);
        CreateSideBanner(mainScreen, 615f);
    }

    private void BuildSettingsScreen()
    {
        RectTransform content = CreateRect(settingsScreen, "Settings Content Root");
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = new Vector2(1260f, 930f);
        content.anchoredPosition = new Vector2(0f, 0f);

        VerticalLayoutGroup rootLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.childAlignment = TextAnchor.MiddleCenter;
        rootLayout.spacing = 12f;
        rootLayout.childControlHeight = false;
        rootLayout.childControlWidth = false;

        RectTransform title = CreateBanner(content, "SETTINGS", new Vector2(1120f, 150f), 78f);
        AddLayout(title, 1120f, 150f);

        RectTransform tabs = CreateRect(content, "Settings Tabs");
        AddLayout(tabs, 1120f, 78f);
        HorizontalLayoutGroup tabLayout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.childAlignment = TextAnchor.MiddleCenter;
        tabLayout.spacing = 14f;
        tabLayout.childControlHeight = false;
        tabLayout.childControlWidth = false;

        CreateTabButton(tabs, SettingsTab.Display, "Display");
        CreateTabButton(tabs, SettingsTab.Graphics, "Graphics");
        CreateTabButton(tabs, SettingsTab.Audio, "Audio");
        CreateTabButton(tabs, SettingsTab.Keybind, "Keybind");

        RectTransform frame = CreateFramedPanel(content, "Settings Frame", new Vector2(1120f, 570f));
        AddLayout(frame, 1120f, 570f);

        ScrollRect scrollRect = frame.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 32f;

        RectTransform viewport = CreateRect(frame, "Viewport");
        Stretch(viewport);
        viewport.offsetMin = new Vector2(28f, 28f);
        viewport.offsetMax = new Vector2(-28f, -28f);
        viewport.gameObject.AddComponent<RectMask2D>();

        settingsContent = CreateRect(viewport, "Settings Rows");
        settingsContent.anchorMin = new Vector2(0f, 1f);
        settingsContent.anchorMax = new Vector2(1f, 1f);
        settingsContent.pivot = new Vector2(0.5f, 1f);
        settingsContent.offsetMin = new Vector2(0f, 0f);
        settingsContent.offsetMax = new Vector2(0f, 0f);

        VerticalLayoutGroup rowsLayout = settingsContent.gameObject.AddComponent<VerticalLayoutGroup>();
        rowsLayout.childAlignment = TextAnchor.UpperCenter;
        rowsLayout.spacing = 12f;
        rowsLayout.padding = new RectOffset(8, 8, 8, 8);
        rowsLayout.childControlHeight = false;
        rowsLayout.childControlWidth = true;
        ContentSizeFitter fitter = settingsContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.viewport = viewport;
        scrollRect.content = settingsContent;

        RectTransform footer = CreateRect(content, "Settings Footer");
        AddLayout(footer, 1120f, 92f);
        HorizontalLayoutGroup footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerLayout.childAlignment = TextAnchor.MiddleCenter;
        footerLayout.spacing = 34f;
        footerLayout.childControlHeight = false;
        footerLayout.childControlWidth = false;

        CreateMenuButton(footer, "Back", new Vector2(250f, 66f), ShowMain);
        CreateMenuButton(footer, "Defaults", new Vector2(250f, 66f), ResetDefaults);
        CreateMenuButton(footer, "Apply", new Vector2(300f, 72f), ApplySettings);

        settingsStatusText = CreateText(content, "Settings Status", string.Empty, 22f, TextAlignmentOptions.Center);
        settingsStatusText.color = new Color(0.95f, 0.78f, 0.38f);
        AddLayout(settingsStatusText.rectTransform, 1000f, 34f);

        CreateSideBanner(settingsScreen, -655f);
        CreateSideBanner(settingsScreen, 655f);
    }

    private void BuildCreditsScreen()
    {
        RectTransform panel = CreateFramedPanel(creditsScreen, "Credits Panel", new Vector2(820f, 520f));
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 20f;
        layout.padding = new RectOffset(56, 56, 56, 56);
        layout.childControlHeight = false;
        layout.childControlWidth = false;

        TMP_Text title = CreateText(panel, "Credits Title", "ONE MORE NIGHT", 46f, TextAlignmentOptions.Center);
        title.color = new Color(1f, 0.82f, 0.32f);
        AddLayout(title.rectTransform, 700f, 70f);

        TMP_Text body = CreateText(panel, "Credits Body", "Menu, settings, and runtime options scene\nBuilt for this Unity project", 28f, TextAlignmentOptions.Center);
        body.color = new Color(0.19f, 0.12f, 0.055f);
        AddLayout(body.rectTransform, 700f, 170f);

        CreateMenuButton(panel, "Back", new Vector2(300f, 68f), ShowMain);
    }

    private void ShowMain()
    {
        waitingForKeyId = null;
        mainScreen.gameObject.SetActive(true);
        settingsScreen.gameObject.SetActive(false);
        creditsScreen.gameObject.SetActive(false);
        SetStatus(string.Empty);
    }

    private void ShowSettings()
    {
        waitingForKeyId = null;
        mainScreen.gameObject.SetActive(false);
        settingsScreen.gameObject.SetActive(true);
        creditsScreen.gameObject.SetActive(false);
        ShowTab(currentTab);
    }

    private void ShowCredits()
    {
        waitingForKeyId = null;
        mainScreen.gameObject.SetActive(false);
        settingsScreen.gameObject.SetActive(false);
        creditsScreen.gameObject.SetActive(true);
    }

    private void CreateTabButton(RectTransform parent, SettingsTab tab, string label)
    {
        Button button = CreateMenuButton(parent, label, new Vector2(255f, 66f), () => ShowTab(tab), false);
        tabButtons[tab] = button;
    }

    private void ShowTab(SettingsTab tab)
    {
        currentTab = tab;
        waitingForKeyId = null;
        choiceControls.Clear();
        ClearChildren(settingsContent);

        foreach (KeyValuePair<SettingsTab, Button> pair in tabButtons)
        {
            Image image = pair.Value.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = pair.Key == tab ? greenButtonSprite : blueButtonSprite;
            }
        }

        switch (tab)
        {
            case SettingsTab.Display:
                BuildDisplaySettings();
                break;
            case SettingsTab.Graphics:
                BuildGraphicsSettings();
                break;
            case SettingsTab.Audio:
                BuildAudioSettings();
                break;
            case SettingsTab.Keybind:
                BuildKeybindSettings();
                break;
        }

        Canvas.ForceUpdateCanvases();
    }

    private void BuildDisplaySettings()
    {
        resolutionChoices = GameSettings.GetResolutionChoices();
        string[] resolutionOptions = new string[resolutionChoices.Length];
        int selectedResolution = 0;
        for (int i = 0; i < resolutionChoices.Length; i++)
        {
            resolutionOptions[i] = resolutionChoices[i].ToString();
            if (resolutionChoices[i].Width == GameSettings.ResolutionWidth &&
                resolutionChoices[i].Height == GameSettings.ResolutionHeight)
            {
                selectedResolution = i;
            }
        }

        AddChoiceRow("Resolution", resolutionOptions, selectedResolution, index =>
        {
            GameSettings.ResolutionWidth = resolutionChoices[index].Width;
            GameSettings.ResolutionHeight = resolutionChoices[index].Height;
            GameSettings.RefreshRate = resolutionChoices[index].RefreshRate;
            MarkSettingsChanged("Resolution will apply when you press Apply.");
        });

        FullScreenMode[] modes =
        {
            FullScreenMode.ExclusiveFullScreen,
            FullScreenMode.FullScreenWindow,
            FullScreenMode.Windowed,
            FullScreenMode.MaximizedWindow
        };
        string[] modeLabels = { "Exclusive Fullscreen", "Borderless", "Windowed", "Maximized Window" };
        int selectedMode = Mathf.Max(0, Array.IndexOf(modes, GameSettings.FullScreenMode));
        AddChoiceRow("Window Mode", modeLabels, selectedMode, index =>
        {
            GameSettings.FullScreenMode = modes[index];
            MarkSettingsChanged("Window mode will apply when you press Apply.");
        });

        AddToggleRow("VSync", GameSettings.VSync, value =>
        {
            GameSettings.VSync = value;
            MarkSettingsChanged("VSync will apply when you press Apply.");
        });

        int[] fpsValues = { 0, 30, 60, 90, 120, 144, 165, 240 };
        string[] fpsLabels = { "Uncapped", "30", "60", "90", "120", "144", "165", "240" };
        int selectedFps = Mathf.Max(0, Array.IndexOf(fpsValues, GameSettings.TargetFrameRate));
        AddChoiceRow("Target FPS", fpsLabels, selectedFps, index =>
        {
            GameSettings.TargetFrameRate = fpsValues[index];
            MarkSettingsChanged("Frame limit will apply when you press Apply.");
        });

        AddSliderRow("Brightness", 0.45f, 1.45f, GameSettings.Brightness, value =>
        {
            GameSettings.Brightness = value;
            GameSettings.NotifyChanged();
            MarkSettingsChanged("Brightness adjusted.");
        }, value => Mathf.RoundToInt(value * 100f) + "%");

        AddSliderRow("UI Scale", 0.75f, 1.35f, GameSettings.UIScale, value =>
        {
            GameSettings.UIScale = value;
            ApplyUiScale();
            MarkSettingsChanged("UI scale adjusted.");
        }, value => Mathf.RoundToInt(value * 100f) + "%");
    }

    private void BuildGraphicsSettings()
    {
        string[] qualityNames = GameSettings.QualityNames();
        AddChoiceRow("Quality Preset", qualityNames, Mathf.Clamp(GameSettings.QualityIndex, 0, qualityNames.Length - 1), index =>
        {
            GameSettings.QualityIndex = index;
            MarkSettingsChanged("Quality preset will apply when you press Apply.");
        });

        AddSliderRow("Render Scale", 0.5f, 1.5f, GameSettings.RenderScale, value =>
        {
            GameSettings.RenderScale = value;
            MarkSettingsChanged("Render scale will apply when you press Apply.");
        }, value => Mathf.RoundToInt(value * 100f) + "%");

        int[] aaValues = { 0, 2, 4, 8 };
        string[] aaLabels = { "Off", "2x", "4x", "8x" };
        int aaIndex = Mathf.Max(0, Array.IndexOf(aaValues, GameSettings.AntiAliasing));
        AddChoiceRow("Anti Aliasing", aaLabels, aaIndex, index =>
        {
            GameSettings.AntiAliasing = aaValues[index];
            MarkSettingsChanged("Anti aliasing will apply when you press Apply.");
        });

        string[] shadowLabels = { "Off", "Hard", "Hard + Soft" };
        AddChoiceRow("Shadows", shadowLabels, GameSettings.ShadowQuality, index =>
        {
            GameSettings.ShadowQuality = index;
            MarkSettingsChanged("Shadow quality will apply when you press Apply.");
        });

        AddSliderRow("Shadow Distance", 0f, 500f, GameSettings.ShadowDistance, value =>
        {
            GameSettings.ShadowDistance = value;
            MarkSettingsChanged("Shadow distance will apply when you press Apply.");
        }, value => Mathf.RoundToInt(value) + " m");

        string[] textureLabels = { "Full", "Half", "Quarter", "Eighth" };
        AddChoiceRow("Texture Quality", textureLabels, GameSettings.TextureQuality, index =>
        {
            GameSettings.TextureQuality = index;
            MarkSettingsChanged("Texture quality will apply when you press Apply.");
        });

        AddToggleRow("Anisotropic Filtering", GameSettings.AnisotropicFiltering, value =>
        {
            GameSettings.AnisotropicFiltering = value;
            MarkSettingsChanged("Anisotropic filtering will apply when you press Apply.");
        });

        AddSliderRow("View Distance", 0.45f, 2f, GameSettings.ViewDistance, value =>
        {
            GameSettings.ViewDistance = value;
            MarkSettingsChanged("View distance will apply when you press Apply.");
        }, value => Mathf.RoundToInt(value * 100f) + "%");

        AddToggleRow("Bloom", GameSettings.Bloom, value =>
        {
            GameSettings.Bloom = value;
            MarkSettingsChanged("Bloom will apply when you press Apply.");
        });

        AddToggleRow("Motion Blur", GameSettings.MotionBlur, value =>
        {
            GameSettings.MotionBlur = value;
            MarkSettingsChanged("Motion blur will apply when you press Apply.");
        });
    }

    private void BuildAudioSettings()
    {
        AddToggleRow("Mute All", GameSettings.Muted, value =>
        {
            GameSettings.Muted = value;
            GameSettings.ApplyAudioSettings();
            MarkSettingsChanged("Audio mute adjusted.");
        });

        AddSliderRow("Master Volume", 0f, 1f, GameSettings.MasterVolume, value =>
        {
            GameSettings.MasterVolume = value;
            GameSettings.ApplyAudioSettings();
            MarkSettingsChanged("Master volume adjusted.");
        }, value => Mathf.RoundToInt(value * 100f) + "%");

        AddSliderRow("Music Volume", 0f, 1f, GameSettings.MusicVolume, value =>
        {
            GameSettings.MusicVolume = value;
            GameSettings.ApplyAudioSettings();
            MarkSettingsChanged("Music volume adjusted.");
        }, value => Mathf.RoundToInt(value * 100f) + "%");

        AddSliderRow("SFX Volume", 0f, 1f, GameSettings.SfxVolume, value =>
        {
            GameSettings.SfxVolume = value;
            GameSettings.ApplyAudioSettings();
            MarkSettingsChanged("SFX volume adjusted.");
        }, value => Mathf.RoundToInt(value * 100f) + "%");

        AddSliderRow("Ambience Volume", 0f, 1f, GameSettings.AmbienceVolume, value =>
        {
            GameSettings.AmbienceVolume = value;
            GameSettings.ApplyAudioSettings();
            MarkSettingsChanged("Ambience volume adjusted.");
        }, value => Mathf.RoundToInt(value * 100f) + "%");
    }

    private void BuildKeybindSettings()
    {
        AddKeybindRow("Move Forward", GameSettings.Key.MoveForward, KeyCode.W);
        AddKeybindRow("Move Backward", GameSettings.Key.MoveBackward, KeyCode.S);
        AddKeybindRow("Move Left", GameSettings.Key.MoveLeft, KeyCode.A);
        AddKeybindRow("Move Right", GameSettings.Key.MoveRight, KeyCode.D);
        AddKeybindRow("Jump", GameSettings.Key.Jump, KeyCode.Space);
        AddKeybindRow("Sprint", GameSettings.Key.Sprint, KeyCode.LeftShift);
        AddKeybindRow("Crouch", GameSettings.Key.Crouch, KeyCode.C);
        AddKeybindRow("Interact", GameSettings.Key.Interact, KeyCode.E);
        AddKeybindRow("Attack", GameSettings.Key.Attack, KeyCode.Mouse0);
        AddKeybindRow("Inventory", GameSettings.Key.Inventory, KeyCode.I);
        AddKeybindRow("Crafting", GameSettings.Key.Crafting, KeyCode.T);
        AddKeybindRow("Upgrade", GameSettings.Key.Upgrade, KeyCode.K);
        AddKeybindRow("Build Mode", GameSettings.Key.BuildMode, KeyCode.B);
        AddKeybindRow("Emote", GameSettings.Key.Emote, KeyCode.H);
        AddKeybindRow("Weapon Slot 1", GameSettings.Key.WeaponSlot1, KeyCode.Alpha1);
        AddKeybindRow("Weapon Slot 2", GameSettings.Key.WeaponSlot2, KeyCode.Alpha2);
        AddKeybindRow("Weapon Slot 3", GameSettings.Key.WeaponSlot3, KeyCode.Alpha3);
        AddKeybindRow("Weapon Slot 4", GameSettings.Key.WeaponSlot4, KeyCode.Alpha4);
        AddKeybindRow("Weapon Slot 5", GameSettings.Key.WeaponSlot5, KeyCode.Alpha5);
        AddKeybindRow("Weapon Slot 6", GameSettings.Key.WeaponSlot6, KeyCode.Alpha6);
        AddKeybindRow("Weapon Slot 7", GameSettings.Key.WeaponSlot7, KeyCode.Alpha7);
        AddKeybindRow("Weapon Slot 8", GameSettings.Key.WeaponSlot8, KeyCode.Alpha8);
        AddKeybindRow("Weapon Slot 9", GameSettings.Key.WeaponSlot9, KeyCode.Alpha9);
        AddKeybindRow("Sword Special 1", GameSettings.Key.SwordSpecial1, KeyCode.Alpha3);
        AddKeybindRow("Sword Special 2", GameSettings.Key.SwordSpecial2, KeyCode.Alpha4);
        AddKeybindRow("Sword Special 3", GameSettings.Key.SwordSpecial3, KeyCode.Alpha5);
    }

    private void ApplySettings()
    {
        GameSettings.SaveAndApply();
        MarkSettingsChanged("Settings applied and saved.");
        RefreshChoiceLabels();
    }

    private void ResetDefaults()
    {
        GameSettings.ResetToDefaults();
        ApplyUiScale();
        ShowTab(currentTab);
        MarkSettingsChanged("Defaults restored.");
    }

    private void ContinueGame()
    {
        string savedScene = PlayerPrefs.GetString("onemorenight.save.scene", string.Empty);
        if (string.IsNullOrWhiteSpace(savedScene))
        {
            SetStatus("No saved game found. Starting the game scene.");
            LoadGameplayScene(false);
            return;
        }

        LoadSceneByName(savedScene);
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
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            SetStatus("Gameplay scene name is empty.");
            return;
        }

        GameSettings.SaveAndApply();
        SceneManager.LoadScene(sceneName.Trim());
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

    private void AddChoiceRow(string label, string[] options, int selectedIndex, Action<int> onChanged)
    {
        RectTransform row = CreateSettingsRow(label);
        RectTransform control = CreateControlArea(row);
        ChoiceControl choice = new ChoiceControl(options, Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, options.Length - 1)), onChanged);

        Button left = CreateSmallButton(control, "<", new Vector2(62f, 54f), choice.Previous);
        RectTransform valueBox = CreateRect(control, label + " Value Box");
        AddLayout(valueBox, 390f, 54f);
        Image valueBackground = valueBox.gameObject.AddComponent<Image>();
        valueBackground.sprite = darkSprite;
        valueBackground.type = Image.Type.Sliced;
        valueBackground.color = Color.white;
        TMP_Text valueText = CreateText(valueBox, label + " Value", string.Empty, 25f, TextAlignmentOptions.Center);
        Stretch(valueText.rectTransform);
        valueText.rectTransform.offsetMin = new Vector2(14f, 3f);
        valueText.rectTransform.offsetMax = new Vector2(-14f, -3f);
        valueText.raycastTarget = false;
        Button right = CreateSmallButton(control, ">", new Vector2(62f, 54f), choice.Next);

        choice.ValueText = valueText;
        choice.Refresh();
        choiceControls.Add(choice);
        left.navigation = NoNavigation();
        right.navigation = NoNavigation();
    }

    private void AddSliderRow(string label, float min, float max, float value, Action<float> onChanged, Func<float, string> formatter)
    {
        RectTransform row = CreateSettingsRow(label);
        RectTransform control = CreateControlArea(row);

        Slider slider = CreateSlider(control, min, max, value);
        AddLayout(slider.GetComponent<RectTransform>(), 410f, 54f);

        TMP_Text valueText = CreateText(control, label + " Value", formatter(value), 24f, TextAlignmentOptions.Center);
        valueText.color = new Color(1f, 0.86f, 0.46f);
        AddLayout(valueText.rectTransform, 120f, 54f);

        slider.onValueChanged.AddListener(newValue =>
        {
            valueText.text = formatter(newValue);
            onChanged?.Invoke(newValue);
        });
    }

    private void AddToggleRow(string label, bool value, Action<bool> onChanged)
    {
        RectTransform row = CreateSettingsRow(label);
        RectTransform control = CreateControlArea(row);

        Toggle toggle = CreateToggle(control, value);
        AddLayout(toggle.GetComponent<RectTransform>(), 80f, 58f);
        TMP_Text valueText = CreateText(control, label + " Value", value ? "Enabled" : "Disabled", 25f, TextAlignmentOptions.Left);
        valueText.color = new Color(0.22f, 0.13f, 0.055f);
        AddLayout(valueText.rectTransform, 420f, 58f);

        toggle.onValueChanged.AddListener(newValue =>
        {
            valueText.text = newValue ? "Enabled" : "Disabled";
            onChanged?.Invoke(newValue);
        });
    }

    private void AddKeybindRow(string label, string keyId, KeyCode fallback)
    {
        RectTransform row = CreateSettingsRow(label);
        RectTransform control = CreateControlArea(row);

        Button button = CreateMenuButton(control, GameSettings.GetKeyDisplayName(keyId, fallback), new Vector2(330f, 58f), null, false);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        button.onClick.AddListener(() =>
        {
            waitingForKeyId = keyId;
            waitingForKeyText = text;
            if (waitingForKeyText != null)
            {
                waitingForKeyText.text = "Press a key";
            }

            MarkSettingsChanged("Press a keyboard key or mouse button. Esc cancels.");
        });

        Button reset = CreateSmallButton(control, "Reset", new Vector2(150f, 58f), () =>
        {
            GameSettings.SetKey(keyId, GameSettings.GetDefaultKey(keyId, fallback));
            text.text = GameSettings.GetKeyDisplayName(keyId, fallback);
            MarkSettingsChanged(label + " reset.");
        });
        reset.navigation = NoNavigation();
    }

    private RectTransform CreateSettingsRow(string label)
    {
        RectTransform row = CreateRect(settingsContent, label + " Row");
        AddLayout(row, 1000f, 72f);
        HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleCenter;
        rowLayout.spacing = 20f;
        rowLayout.padding = new RectOffset(10, 10, 7, 7);
        rowLayout.childControlHeight = false;
        rowLayout.childControlWidth = false;

        Image background = row.gameObject.AddComponent<Image>();
        background.sprite = parchmentSprite;
        background.type = Image.Type.Sliced;
        background.color = new Color(1f, 0.96f, 0.84f, 0.92f);

        TMP_Text labelText = CreateText(row, label + " Label", label, 27f, TextAlignmentOptions.Left);
        labelText.color = new Color(0.2f, 0.11f, 0.045f);
        AddLayout(labelText.rectTransform, 350f, 58f);
        return row;
    }

    private RectTransform CreateControlArea(RectTransform row)
    {
        RectTransform control = CreateRect(row, "Control");
        AddLayout(control, 590f, 58f);
        HorizontalLayoutGroup controlLayout = control.gameObject.AddComponent<HorizontalLayoutGroup>();
        controlLayout.childAlignment = TextAnchor.MiddleRight;
        controlLayout.spacing = 12f;
        controlLayout.childControlHeight = false;
        controlLayout.childControlWidth = false;
        return control;
    }

    private Slider CreateSlider(RectTransform parent, float min, float max, float value)
    {
        RectTransform root = CreateRect(parent, "Slider");
        Slider slider = root.gameObject.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = Mathf.Clamp(value, min, max);
        slider.wholeNumbers = false;
        slider.navigation = NoNavigation();

        Image background = CreateImage(root, "Background", sliderTrackSprite);
        Stretch(background.rectTransform);
        background.type = Image.Type.Sliced;

        RectTransform fillArea = CreateRect(root, "Fill Area");
        Stretch(fillArea);
        fillArea.offsetMin = new Vector2(14f, 10f);
        fillArea.offsetMax = new Vector2(-14f, -10f);
        Image fill = CreateImage(fillArea, "Fill", sliderFillSprite);
        Stretch(fill.rectTransform);
        fill.type = Image.Type.Sliced;
        slider.fillRect = fill.rectTransform;

        RectTransform handleArea = CreateRect(root, "Handle Slide Area");
        Stretch(handleArea);
        handleArea.offsetMin = new Vector2(18f, 0f);
        handleArea.offsetMax = new Vector2(-18f, 0f);
        Image handle = CreateImage(handleArea, "Handle", goldSprite);
        handle.rectTransform.sizeDelta = new Vector2(42f, 42f);
        handle.type = Image.Type.Sliced;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        return slider;
    }

    private Toggle CreateToggle(RectTransform parent, bool value)
    {
        RectTransform root = CreateRect(parent, "Toggle");
        Toggle toggle = root.gameObject.AddComponent<Toggle>();
        toggle.isOn = value;
        toggle.navigation = NoNavigation();

        Image box = CreateImage(root, "Box", checkboxSprite);
        box.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        box.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        box.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        box.rectTransform.sizeDelta = new Vector2(54f, 54f);
        box.type = Image.Type.Sliced;

        TMP_Text check = CreateText(box.rectTransform, "Check", "V", 38f, TextAlignmentOptions.Center);
        check.color = new Color(1f, 0.84f, 0.22f);
        Stretch(check.rectTransform);

        toggle.targetGraphic = box;
        toggle.graphic = check;
        return toggle;
    }

    private Button CreateMenuButton(RectTransform parent, string label, Vector2 size, Action onClick, bool addLayout = true)
    {
        RectTransform root = CreateRect(parent, label + " Button");
        root.sizeDelta = size;
        if (addLayout)
        {
            AddLayout(root, size.x, size.y);
        }

        Image image = root.gameObject.AddComponent<Image>();
        image.sprite = greenButtonSprite;
        image.type = Image.Type.Sliced;

        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        SpriteState spriteState = button.spriteState;
        spriteState.pressedSprite = greenButtonPressedSprite;
        spriteState.highlightedSprite = greenButtonPressedSprite;
        button.spriteState = spriteState;
        button.navigation = NoNavigation();
        if (onClick != null)
        {
            button.onClick.AddListener(() => onClick());
        }

        TMP_Text text = CreateText(root, "Text", label, Mathf.Clamp(size.y * 0.45f, 24f, 44f), TextAlignmentOptions.Center);
        text.color = new Color(1f, 0.82f, 0.26f);
        text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(24f, 4f);
        text.rectTransform.offsetMax = new Vector2(-24f, -4f);

        AddCornerGem(root, new Vector2(0f, 0.5f), new Vector2(-7f, 0f), 34f);
        AddCornerGem(root, new Vector2(1f, 0.5f), new Vector2(7f, 0f), 34f);
        return button;
    }

    private Button CreateSmallButton(RectTransform parent, string label, Vector2 size, Action onClick)
    {
        Button button = CreateMenuButton(parent, label, size, onClick, true);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.fontSize = Mathf.Min(text.fontSize, 25f);
        }

        return button;
    }

    private RectTransform CreateBanner(RectTransform parent, string label, Vector2 size, float fontSize)
    {
        RectTransform banner = CreateFramedPanel(parent, label + " Banner", size);
        TMP_Text text = CreateText(banner, "Title", label, fontSize, TextAlignmentOptions.Center);
        text.color = new Color(0.22f, 0.12f, 0.045f);
        text.fontStyle = FontStyles.Bold;
        Stretch(text.rectTransform);
        text.rectTransform.offsetMin = new Vector2(72f, 8f);
        text.rectTransform.offsetMax = new Vector2(-72f, -8f);
        AddTopGem(banner);
        return banner;
    }

    private RectTransform CreateFramedPanel(RectTransform parent, string name, Vector2 size)
    {
        RectTransform root = CreateRect(parent, name);
        root.sizeDelta = size;

        Image body = root.gameObject.AddComponent<Image>();
        body.sprite = parchmentSprite;
        body.type = Image.Type.Sliced;
        body.color = Color.white;

        AddFrameBar(root, "Top Frame", new Vector2(0.5f, 1f), new Vector2(0f, 9f), new Vector2(size.x + 42f, 34f));
        AddFrameBar(root, "Bottom Frame", new Vector2(0.5f, 0f), new Vector2(0f, -9f), new Vector2(size.x + 42f, 34f));
        AddFrameBar(root, "Left Frame", new Vector2(0f, 0.5f), new Vector2(-9f, 0f), new Vector2(34f, size.y + 40f));
        AddFrameBar(root, "Right Frame", new Vector2(1f, 0.5f), new Vector2(9f, 0f), new Vector2(34f, size.y + 40f));

        AddCornerGem(root, new Vector2(0f, 0f), new Vector2(-8f, -8f), 42f);
        AddCornerGem(root, new Vector2(1f, 0f), new Vector2(8f, -8f), 42f);
        AddCornerGem(root, new Vector2(0f, 1f), new Vector2(-8f, 8f), 42f);
        AddCornerGem(root, new Vector2(1f, 1f), new Vector2(8f, 8f), 42f);
        return root;
    }

    private void AddFrameBar(RectTransform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size)
    {
        Image bar = CreateImage(parent, name, woodSprite);
        bar.rectTransform.anchorMin = anchor;
        bar.rectTransform.anchorMax = anchor;
        bar.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        bar.rectTransform.anchoredPosition = offset;
        bar.rectTransform.sizeDelta = size;
        bar.type = Image.Type.Sliced;
    }

    private void AddCornerGem(RectTransform parent, Vector2 anchor, Vector2 offset, float size)
    {
        Image gem = CreateImage(parent, "Gem", goldSprite);
        gem.rectTransform.anchorMin = anchor;
        gem.rectTransform.anchorMax = anchor;
        gem.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        gem.rectTransform.anchoredPosition = offset;
        gem.rectTransform.sizeDelta = new Vector2(size, size);
        gem.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        gem.type = Image.Type.Sliced;
    }

    private void AddTopGem(RectTransform parent)
    {
        Image gem = CreateImage(parent, "Top Gem", goldSprite);
        gem.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        gem.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        gem.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        gem.rectTransform.anchoredPosition = new Vector2(0f, 33f);
        gem.rectTransform.sizeDelta = new Vector2(72f, 72f);
        gem.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private void CreateSideBanner(RectTransform parent, float x)
    {
        RectTransform pole = CreateRect(parent, "Side Banner");
        pole.anchorMin = new Vector2(0.5f, 0.5f);
        pole.anchorMax = new Vector2(0.5f, 0.5f);
        pole.pivot = new Vector2(0.5f, 0.5f);
        pole.anchoredPosition = new Vector2(x, 135f);
        pole.sizeDelta = new Vector2(90f, 420f);

        Image crossbar = CreateImage(pole, "Crossbar", woodSprite);
        crossbar.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        crossbar.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        crossbar.rectTransform.sizeDelta = new Vector2(220f, 24f);
        crossbar.rectTransform.anchoredPosition = new Vector2(0f, -28f);
        crossbar.type = Image.Type.Sliced;

        Image cloth = CreateImage(pole, "Cloth", greenButtonSprite);
        cloth.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        cloth.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        cloth.rectTransform.sizeDelta = new Vector2(72f, 260f);
        cloth.rectTransform.anchoredPosition = new Vector2(0f, -52f);
        cloth.type = Image.Type.Sliced;

        TMP_Text symbol = CreateText(cloth.rectTransform, "Symbol", "Y", 68f, TextAlignmentOptions.Center);
        symbol.color = new Color(1f, 0.82f, 0.32f, 0.72f);
        Stretch(symbol.rectTransform);
    }

    private void ApplyUiScale()
    {
        if (scaledRoot != null)
        {
            float scale = GameSettings.UIScale;
            scaledRoot.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void MarkSettingsChanged(string message)
    {
        if (settingsStatusText != null)
        {
            settingsStatusText.text = message;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void RefreshChoiceLabels()
    {
        for (int i = 0; i < choiceControls.Count; i++)
        {
            choiceControls[i].Refresh();
        }
    }

    private void CaptureRebindInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelRebind();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            CompleteRebind(KeyCode.Mouse0);
            return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            CompleteRebind(KeyCode.Mouse1);
            return;
        }

        if (Input.GetMouseButtonDown(2))
        {
            CompleteRebind(KeyCode.Mouse2);
            return;
        }

        for (int i = 0; i < RebindableKeys.Length; i++)
        {
            KeyCode candidate = RebindableKeys[i];
            if (Input.GetKeyDown(candidate))
            {
                CompleteRebind(candidate);
                return;
            }
        }
    }

    private void CompleteRebind(KeyCode keyCode)
    {
        string keyId = waitingForKeyId;
        waitingForKeyId = null;
        GameSettings.SetKey(keyId, keyCode);
        if (waitingForKeyText != null)
        {
            waitingForKeyText.text = GameSettings.ToDisplayName(keyCode);
        }

        waitingForKeyText = null;
        MarkSettingsChanged("Keybind saved.");
    }

    private void CancelRebind()
    {
        string keyId = waitingForKeyId;
        waitingForKeyId = null;
        if (waitingForKeyText != null)
        {
            waitingForKeyText.text = GameSettings.GetKeyDisplayName(keyId);
        }

        waitingForKeyText = null;
        MarkSettingsChanged("Keybind cancelled.");
    }

    private static Navigation NoNavigation()
    {
        return new Navigation { mode = Navigation.Mode.None };
    }

    private TMP_Text CreateText(RectTransform parent, string name, string value, float size, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(parent, name);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = bodyFont;
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(12f, size * 0.55f);
        text.fontSizeMax = size;
        text.alignment = alignment;
        text.color = new Color(0.16f, 0.09f, 0.035f);
        text.raycastTarget = false;
        return text;
    }

    private Image CreateImage(RectTransform parent, string name, Sprite sprite)
    {
        RectTransform rect = CreateRect(parent, name);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        return image;
    }

    private RectTransform CreateRect(RectTransform parent, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj.AddComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AddLayout(RectTransform rect, float width, float height)
    {
        LayoutElement element = rect.gameObject.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = rect.gameObject.AddComponent<LayoutElement>();
        }

        element.preferredWidth = width;
        element.preferredHeight = height;
        element.minWidth = width;
        element.minHeight = height;
    }

    private static void ClearChildren(RectTransform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private Sprite CreateTexturedSprite(string name, int width, int height, Color32 dark, Color32 light, int border, float grainScale)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color darkColor = dark;
        Color lightColor = light;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float noise = Mathf.PerlinNoise((x + 17f) * grainScale, (y + 31f) * grainScale);
                float edge = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                float edgeT = Mathf.Clamp01(edge / Mathf.Max(1f, border));
                Color color = Color.Lerp(darkColor, lightColor, Mathf.Lerp(0.18f, noise, edgeT));
                color = Color.Lerp(darkColor * 0.55f, color, edgeT);
                color.a = lightColor.a;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    private Sprite CreateBackgroundSprite()
    {
        int width = 512;
        int height = 512;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Menu Background";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color top = new Color(0.025f, 0.038f, 0.033f);
        Color middle = new Color(0.09f, 0.12f, 0.075f);
        Color bottom = new Color(0.025f, 0.025f, 0.02f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float v = y / (float)(height - 1);
                Color color = v > 0.42f
                    ? Color.Lerp(middle, top, Mathf.InverseLerp(0.42f, 1f, v))
                    : Color.Lerp(bottom, middle, Mathf.InverseLerp(0f, 0.42f, v));

                float nx = (x / (float)width - 0.5f) * 2f;
                float ny = (y / (float)height - 0.5f) * 2f;
                float vignette = Mathf.Clamp01(1f - ((nx * nx) + (ny * ny * 0.85f)) * 0.55f);
                float grain = Mathf.PerlinNoise(x * 0.035f, y * 0.035f) * 0.08f;
                color *= 0.64f + vignette * 0.5f + grain;
                color.a = 1f;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private Material CreateMaterial(string name, Color color, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = name;
        material.color = color;
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        return material;
    }

    private void CreateTree(Vector3 position, float height, Material trunkMaterial, Material leafMaterial)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Menu Tree Trunk";
        trunk.transform.position = position + new Vector3(0f, height * 0.35f, 0f);
        trunk.transform.localScale = new Vector3(0.18f, height * 0.35f, 0.18f);
        trunk.GetComponent<Renderer>().sharedMaterial = trunkMaterial;

        GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leaves.name = "Menu Tree Crown";
        leaves.transform.position = position + new Vector3(0f, height * 0.9f, 0f);
        leaves.transform.localScale = new Vector3(height * 0.34f, height * 0.62f, height * 0.34f);
        leaves.GetComponent<Renderer>().sharedMaterial = leafMaterial;
    }

    private void CreateTorch(Vector3 position, Material woodMaterial, Material emberMaterial)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Menu Torch Post";
        post.transform.position = position;
        post.transform.localScale = new Vector3(0.08f, 0.7f, 0.08f);
        post.GetComponent<Renderer>().sharedMaterial = woodMaterial;

        GameObject ember = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ember.name = "Menu Torch Ember";
        ember.transform.position = position + new Vector3(0f, 0.78f, 0f);
        ember.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
        ember.GetComponent<Renderer>().sharedMaterial = emberMaterial;

        Light light = ember.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.55f, 0.16f);
        light.intensity = 2.6f;
        light.range = 5f;
    }

    private sealed class ChoiceControl
    {
        private readonly string[] options;
        private readonly Action<int> onChanged;
        private int index;

        public TMP_Text ValueText { private get; set; }

        public ChoiceControl(string[] options, int index, Action<int> onChanged)
        {
            this.options = options ?? Array.Empty<string>();
            this.index = Mathf.Clamp(index, 0, Mathf.Max(0, this.options.Length - 1));
            this.onChanged = onChanged;
        }

        public void Previous()
        {
            if (options.Length == 0)
            {
                return;
            }

            index = (index - 1 + options.Length) % options.Length;
            Refresh();
            onChanged?.Invoke(index);
        }

        public void Next()
        {
            if (options.Length == 0)
            {
                return;
            }

            index = (index + 1) % options.Length;
            Refresh();
            onChanged?.Invoke(index);
        }

        public void Refresh()
        {
            if (ValueText != null)
            {
                ValueText.text = options.Length == 0 ? "None" : options[index];
            }
        }
    }
}
