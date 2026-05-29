using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MainMenuSceneBuilder
{
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

    private static readonly Color MainBackgroundColor = new Color(0.10f, 0.13f, 0.09f, 1f);
    private static readonly Color SettingsBackgroundColor = new Color(0.11f, 0.14f, 0.10f, 1f);
    private static readonly Color ShellColor = new Color(0.23f, 0.15f, 0.09f, 0.95f);
    private static readonly Color ShellInnerColor = new Color(0.74f, 0.63f, 0.43f, 0.92f);
    private static readonly Color CardColor = new Color(0.64f, 0.53f, 0.36f, 0.88f);
    private static readonly Color RowColor = new Color(0.15f, 0.10f, 0.07f, 0.26f);
    private static readonly Color RowLabelColor = new Color(0.96f, 0.90f, 0.78f, 1f);
    private static readonly Color TextPrimary = new Color(0.97f, 0.91f, 0.78f, 1f);
    private static readonly Color TextSecondary = new Color(0.82f, 0.72f, 0.56f, 1f);
    private static readonly Color AccentColor = new Color(0.27f, 0.40f, 0.20f, 1f);
    private static readonly Color AccentColorHover = new Color(0.33f, 0.49f, 0.24f, 1f);
    private static readonly Color AccentColorPressed = new Color(0.22f, 0.32f, 0.16f, 1f);
    private static readonly Color NeutralButtonColor = new Color(0.29f, 0.20f, 0.12f, 1f);
    private static readonly Color NeutralButtonHover = new Color(0.35f, 0.25f, 0.15f, 1f);
    private static readonly Color NeutralButtonPressed = new Color(0.22f, 0.15f, 0.10f, 1f);
    private static readonly Color DisabledButtonColor = new Color(0.24f, 0.20f, 0.16f, 0.65f);
    private static readonly Color ValuePanelColor = new Color(0.13f, 0.10f, 0.08f, 0.95f);
    private static readonly Color SliderTrackColor = new Color(0.16f, 0.12f, 0.09f, 1f);
    private static readonly Color SliderKnobColor = new Color(0.88f, 0.77f, 0.46f, 1f);

    private sealed class MainMenuRefs
    {
        public Button NewGameButton;
        public Button ContinueButton;
        public Button LoadGameButton;
        public Button SettingsButton;
        public Button CreditsButton;
        public Button ExitButton;
        public TMP_Text StatusText;
    }

    private sealed class SettingsMenuRefs
    {
        public Button BackButton;
        public Button ApplyButton;
        public Button DisplayTabButton;
        public Button KeybindTabButton;
        public Button AudioTabButton;
        public Button GraphicsTabButton;
        public Button DistanceTabButton;
        public Image DisplayTabImage;
        public Image KeybindTabImage;
        public Image AudioTabImage;
        public Image GraphicsTabImage;
        public Image DistanceTabImage;
        public GameObject DisplayTabContent;
        public GameObject KeybindTabContent;
        public GameObject AudioTabContent;
        public GameObject GraphicsTabContent;
        public GameObject DistanceTabContent;
        public TMP_Text StatusText;
        public Button ResolutionPreviousButton;
        public Button ResolutionNextButton;
        public TMP_Text ResolutionValueText;
        public Toggle FullscreenToggle;
        public Toggle VSyncToggle;
        public Slider BrightnessSlider;
        public TMP_Text BrightnessValueText;
        public Slider UiScaleSlider;
        public TMP_Text UiScaleValueText;

        public Slider MasterVolumeSlider;
        public TMP_Text MasterVolumeValueText;
        public Slider MusicVolumeSlider;
        public TMP_Text MusicVolumeValueText;
        public Slider SfxVolumeSlider;
        public TMP_Text SfxVolumeValueText;
        public Slider AmbienceVolumeSlider;
        public TMP_Text AmbienceVolumeValueText;
        public Toggle MutedToggle;

        public Button QualityPreviousButton;
        public Button QualityNextButton;
        public TMP_Text QualityValueText;
        public Button FrameRatePreviousButton;
        public Button FrameRateNextButton;
        public TMP_Text FrameRateValueText;
        public Slider RenderScaleSlider;
        public TMP_Text RenderScaleValueText;
        public Button AntiAliasingPreviousButton;
        public Button AntiAliasingNextButton;
        public TMP_Text AntiAliasingValueText;
        public Button ShadowQualityPreviousButton;
        public Button ShadowQualityNextButton;
        public TMP_Text ShadowQualityValueText;
        public Slider ShadowDistanceSlider;
        public TMP_Text ShadowDistanceValueText;
        public Button TextureQualityPreviousButton;
        public Button TextureQualityNextButton;
        public TMP_Text TextureQualityValueText;
        public Toggle AnisotropicFilteringToggle;
        public Slider ViewDistanceSlider;
        public TMP_Text ViewDistanceValueText;
        public Toggle BloomToggle;
        public Toggle MotionBlurToggle;

        public Button MoveForwardKeyButton;
        public Button MoveBackwardKeyButton;
        public Button MoveLeftKeyButton;
        public Button MoveRightKeyButton;
        public Button JumpKeyButton;
        public Button SprintKeyButton;
        public Button InteractKeyButton;
        public Button AttackKeyButton;
        public Button InventoryKeyButton;
        public TMP_Text KeybindInfoText;
    }

    private sealed class CreditsMenuRefs
    {
        public Button BackButton;
    }

    private sealed class LoadingScreenRefs
    {
        public GameObject Root;
        public TMP_Text TitleText;
        public TMP_Text MessageText;
        public TMP_Text StageText;
        public TMP_Text ProgressText;
        public Slider ProgressSlider;
        public RectTransform Spinner;
    }

    [MenuItem("Tools/One More Night/Rebuild Main Menu Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Main menu scene rebuild is disabled during Play Mode.");
            return;
        }

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateWorldCamera();
        CreateWorldLight();
        CreateEventSystem();

        TMP_FontAsset font = ResolveFont();
        Sprite uiSprite = ResolveBuiltinSprite("UI/Skin/UISprite.psd");
        Sprite knobSprite = ResolveBuiltinSprite("UI/Skin/Knob.psd");
        Sprite panelSprite = ResolveBuiltinSprite("UI/Skin/Background.psd");

        if (uiSprite == null)
        {
            Debug.LogError("Could not load built-in UI sprite. Aborting menu build.");
            return;
        }

        if (knobSprite == null)
        {
            knobSprite = uiSprite;
        }

        if (panelSprite == null)
        {
            panelSprite = uiSprite;
        }

        GameObject canvasObject = new GameObject(
            "Main Menu Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        StretchRect(canvasRect);

        Image backgroundImage = null;

        RectTransform scaledRoot = CreateRect("Scaled Root", canvasRect);
        StretchRect(scaledRoot);

        RectTransform shellRoot = CreateRect("Menu Shell Root", scaledRoot);
        StretchRect(shellRoot);

        RectTransform shellShadow = CreateRect("Menu Shell Shadow", shellRoot);
        shellShadow.gameObject.AddComponent<CanvasRenderer>().cullTransparentMesh = true;
        Image shellShadowImage = shellShadow.gameObject.AddComponent<Image>();
        shellShadowImage.sprite = uiSprite;
        shellShadowImage.type = Image.Type.Sliced;
        shellShadowImage.color = new Color(0f, 0f, 0f, 0.45f);
        shellShadowImage.raycastTarget = false;
        SetAnchoredRect(shellShadow, new Vector2(780f, 900f), new Vector2(8f, -8f), new Vector2(0.5f, 0.5f));

        Image shellImage = CreateImage("Menu Shell", shellRoot, uiSprite, ShellColor, false);
        shellImage.type = Image.Type.Sliced;
        shellImage.pixelsPerUnitMultiplier = 1f;
        SetAnchoredRect(shellImage.rectTransform, new Vector2(780f, 900f), new Vector2(0f, 0f), new Vector2(0.5f, 0.5f));
        Outline shellOutline = shellImage.gameObject.AddComponent<Outline>();
        shellOutline.effectColor = new Color(0.62f, 0.49f, 0.23f, 0.52f);
        shellOutline.effectDistance = new Vector2(2f, -2f);

        Image shellInner = CreateImage("Menu Shell Inner", shellImage.rectTransform, uiSprite, ShellInnerColor, false);
        shellInner.type = Image.Type.Sliced;
        StretchRect(shellInner.rectTransform, new Vector2(14f, 14f), new Vector2(-14f, -14f));

        AddFrameStud(shellImage.rectTransform, uiSprite, new Vector2(0f, 1f), new Vector2(-8f, 8f));
        AddFrameStud(shellImage.rectTransform, uiSprite, new Vector2(1f, 1f), new Vector2(8f, 8f));
        AddFrameStud(shellImage.rectTransform, uiSprite, new Vector2(0f, 0f), new Vector2(-8f, -8f));
        AddFrameStud(shellImage.rectTransform, uiSprite, new Vector2(1f, 0f), new Vector2(8f, -8f));

        RectTransform shellContent = CreateRect("Menu Shell Content", shellInner.rectTransform);
        StretchRect(shellContent, new Vector2(22f, 22f), new Vector2(-22f, -22f));

        RectTransform mainScreen = CreateRect("Main Screen", shellContent);
        StretchRect(mainScreen);

        RectTransform settingsScreen = CreateRect("Settings Screen", scaledRoot);
        StretchRect(settingsScreen);

        RectTransform creditsScreen = CreateRect("Credits Screen", shellContent);
        StretchRect(creditsScreen);

        MainMenuRefs mainRefs = BuildMainScreen(mainScreen, font, uiSprite);
        SettingsMenuRefs settingsRefs = BuildSettingsScreen(settingsScreen, font, uiSprite, knobSprite, panelSprite);
        CreditsMenuRefs creditsRefs = BuildCreditsScreen(creditsScreen, font, uiSprite);
        LoadingScreenRefs loadingRefs = BuildLoadingScreen(canvasRect, font, uiSprite, knobSprite, panelSprite);

        settingsScreen.gameObject.SetActive(false);
        creditsScreen.gameObject.SetActive(false);
        loadingRefs.Root.SetActive(false);

        GameObject controllerObject = new GameObject("Fantasy Menu Controller");
        FantasyMenuController controller = controllerObject.AddComponent<FantasyMenuController>();
        AssignControllerReferences(
            controller,
            scaledRoot,
            backgroundImage,
            null,
            null,
            shellRoot.gameObject,
            mainScreen.gameObject,
            settingsScreen.gameObject,
            creditsScreen.gameObject,
            mainRefs,
            settingsRefs,
            creditsRefs,
            loadingRefs,
            uiSprite,
            uiSprite);

        GameObject settingsBootstrapper = new GameObject("Game Settings Bootstrapper");
        settingsBootstrapper.AddComponent<GameSettingsBootstrapper>();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), MenuScenePath);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static MainMenuRefs BuildMainScreen(RectTransform parent, TMP_FontAsset font, Sprite uiSprite)
    {
        MainMenuRefs refs = new MainMenuRefs();

        Image titlePlate = CreateImage("Main Title Plate", parent, uiSprite, new Color(0.19f, 0.12f, 0.08f, 0.72f), false);
        titlePlate.type = Image.Type.Sliced;
        SetTopRect(titlePlate.rectTransform, 8f, -6f, 118f);

        TMP_Text title = CreateText(
            "Main Title",
            parent,
            "ONE MORE NIGHT",
            font,
            62f,
            TextAlignmentOptions.Center,
            FontStyles.Bold,
            TextPrimary);
        SetTopRect(title.rectTransform, 14f, -16f, 84f);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.overflowMode = TextOverflowModes.Ellipsis;
        title.enableAutoSizing = true;
        title.fontSizeMin = 40f;
        title.fontSizeMax = 68f;

        Image buttonBackdrop = CreateImage("Main Button Backdrop", parent, uiSprite, new Color(0.16f, 0.10f, 0.06f, 0.34f), false);
        buttonBackdrop.type = Image.Type.Sliced;
        buttonBackdrop.rectTransform.anchorMin = new Vector2(0f, 0f);
        buttonBackdrop.rectTransform.anchorMax = new Vector2(1f, 1f);
        buttonBackdrop.rectTransform.offsetMin = new Vector2(0f, 126f);
        buttonBackdrop.rectTransform.offsetMax = new Vector2(0f, -168f);

        RectTransform buttonColumn = CreateRect("Main Button Column", parent);
        buttonColumn.anchorMin = new Vector2(0f, 0f);
        buttonColumn.anchorMax = new Vector2(1f, 1f);
        buttonColumn.offsetMin = new Vector2(22f, 136f);
        buttonColumn.offsetMax = new Vector2(-22f, -176f);

        VerticalLayoutGroup buttonsLayout = buttonColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        buttonsLayout.spacing = 14f;
        buttonsLayout.padding = new RectOffset(0, 0, 0, 0);
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childControlWidth = true;
        buttonsLayout.childForceExpandHeight = false;
        buttonsLayout.childForceExpandWidth = false;
        buttonsLayout.childAlignment = TextAnchor.UpperCenter;

        refs.NewGameButton = CreateFlatButton(buttonColumn, "New Game Button", "New Game", font, false, 72f, 0f);
        refs.ContinueButton = CreateFlatButton(buttonColumn, "Continue Button", "Continue", font, true, 72f, 0f);
        refs.LoadGameButton = CreateFlatButton(buttonColumn, "Load Game Button", "Load Game", font, false, 72f, 0f);
        refs.SettingsButton = CreateFlatButton(buttonColumn, "Settings Button", "Settings", font, false, 72f, 0f);
        refs.ExitButton = CreateFlatButton(buttonColumn, "Exit Button", "Exit", font, false, 72f, 0f);
        refs.CreditsButton = CreateFlatButton(buttonColumn, "Credits Button", "Credits", font, false, 64f, 0f);

        TMP_Text status = CreateText(
            "Main Status Text",
            parent,
            string.Empty,
            font,
            20f,
            TextAlignmentOptions.Center,
            FontStyles.Italic,
            TextSecondary);
        status.textWrappingMode = TextWrappingModes.NoWrap;
        status.overflowMode = TextOverflowModes.Ellipsis;
        status.rectTransform.anchorMin = new Vector2(0f, 0f);
        status.rectTransform.anchorMax = new Vector2(1f, 0f);
        status.rectTransform.pivot = new Vector2(0.5f, 0f);
        status.rectTransform.offsetMin = new Vector2(12f, 18f);
        status.rectTransform.offsetMax = new Vector2(-12f, 74f);
        refs.StatusText = status;

        return refs;
    }

    private static SettingsMenuRefs BuildSettingsScreen(
        RectTransform parent,
        TMP_FontAsset font,
        Sprite uiSprite,
        Sprite knobSprite,
        Sprite panelSprite)
    {
        SettingsMenuRefs refs = new SettingsMenuRefs();

        RectTransform settingsRoot = CreateRect("Settings Root", parent);
        StretchRect(settingsRoot);

        Image dimmer = CreateImage("Settings Dimmer", settingsRoot, panelSprite, new Color(0f, 0f, 0f, 0.36f), false);
        StretchRect(dimmer.rectTransform);
        dimmer.type = Image.Type.Sliced;

        RectTransform settingsFrame = CreateRect("Settings Frame", settingsRoot);
        StretchRect(settingsFrame, new Vector2(116f, 52f), new Vector2(-116f, -52f));

        Image settingsFrameImage = CreateImage("Settings Frame Background", settingsFrame, panelSprite, new Color(0.24f, 0.20f, 0.15f, 0.95f), false);
        StretchRect(settingsFrameImage.rectTransform);
        settingsFrameImage.type = Image.Type.Sliced;

        Outline frameOutline = settingsFrameImage.gameObject.AddComponent<Outline>();
        frameOutline.effectColor = new Color(0.15f, 0.10f, 0.06f, 0.75f);
        frameOutline.effectDistance = new Vector2(1f, -1f);
        Shadow frameShadow = settingsFrameImage.gameObject.AddComponent<Shadow>();
        frameShadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
        frameShadow.effectDistance = new Vector2(0f, -4f);

        Image titlePlate = CreateImage("Settings Title Plate", settingsFrame, panelSprite, new Color(0.22f, 0.17f, 0.12f, 0.82f), false);
        titlePlate.type = Image.Type.Sliced;
        SetTopRect(titlePlate.rectTransform, 10f, -10f, 112f);

        TMP_Text title = CreateText(
            "Settings Title",
            settingsFrame,
            "SETTINGS",
            font,
            56f,
            TextAlignmentOptions.Center,
            FontStyles.Bold,
            TextPrimary);
        SetTopRect(title.rectTransform, 16f, -16f, 86f);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.enableAutoSizing = true;
        title.fontSizeMin = 34f;
        title.fontSizeMax = 62f;

        RectTransform tabsRow = CreateRect("Settings Tabs", settingsFrame);
        tabsRow.anchorMin = new Vector2(0f, 1f);
        tabsRow.anchorMax = new Vector2(1f, 1f);
        tabsRow.pivot = new Vector2(0.5f, 1f);
        tabsRow.offsetMin = new Vector2(16f, -194f);
        tabsRow.offsetMax = new Vector2(-16f, -118f);

        HorizontalLayoutGroup tabsLayout = tabsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 12f;
        tabsLayout.padding = new RectOffset(0, 0, 0, 0);
        tabsLayout.childControlHeight = true;
        tabsLayout.childControlWidth = true;
        tabsLayout.childForceExpandHeight = false;
        tabsLayout.childForceExpandWidth = true;
        tabsLayout.childAlignment = TextAnchor.MiddleCenter;

        refs.DisplayTabButton = CreateFlatButton(tabsRow, "Display Tab", "Display", font, true, 64f, 0f);
        refs.KeybindTabButton = CreateFlatButton(tabsRow, "Keybind Tab", "Keybind", font, false, 64f, 0f);
        refs.AudioTabButton = CreateFlatButton(tabsRow, "Audio Tab", "Audio", font, false, 64f, 0f);
        refs.GraphicsTabButton = CreateFlatButton(tabsRow, "Graphics Tab", "Graphics", font, false, 64f, 0f);
        refs.DistanceTabButton = CreateFlatButton(tabsRow, "Distance Tab", "Distance", font, false, 64f, 0f);

        refs.DisplayTabImage = refs.DisplayTabButton.image;
        refs.KeybindTabImage = refs.KeybindTabButton.image;
        refs.AudioTabImage = refs.AudioTabButton.image;
        refs.GraphicsTabImage = refs.GraphicsTabButton.image;
        refs.DistanceTabImage = refs.DistanceTabButton.image;

        Image contentPanel = CreateImage("Settings Content Panel", settingsFrame, panelSprite, new Color(0.57f, 0.47f, 0.32f, 0.92f), false);
        contentPanel.type = Image.Type.Sliced;
        RectTransform contentPanelRect = contentPanel.rectTransform;
        contentPanelRect.anchorMin = new Vector2(0f, 0f);
        contentPanelRect.anchorMax = new Vector2(1f, 1f);
        contentPanelRect.offsetMin = new Vector2(16f, 176f);
        contentPanelRect.offsetMax = new Vector2(-16f, -204f);

        RectTransform tabContentRoot = CreateRect("Tab Content Root", contentPanelRect);
        StretchRect(tabContentRoot, new Vector2(12f, 12f), new Vector2(-12f, -12f));

        RectTransform displayContent = CreateRect("Display Tab Content", tabContentRoot);
        StretchRect(displayContent);

        RectTransform keybindContent = CreateRect("Keybind Tab Content", tabContentRoot);
        StretchRect(keybindContent);

        RectTransform audioContent = CreateRect("Audio Tab Content", tabContentRoot);
        StretchRect(audioContent);

        RectTransform graphicsContent = CreateRect("Graphics Tab Content", tabContentRoot);
        StretchRect(graphicsContent);

        RectTransform distanceContent = CreateRect("Distance Tab Content", tabContentRoot);
        StretchRect(distanceContent);

        RectTransform displayScrollContent = CreateScrollableTabContent(displayContent, panelSprite);
        RectTransform keybindScrollContent = CreateScrollableTabContent(keybindContent, panelSprite);
        RectTransform audioScrollContent = CreateScrollableTabContent(audioContent, panelSprite);
        RectTransform graphicsScrollContent = CreateScrollableTabContent(graphicsContent, panelSprite);
        RectTransform distanceScrollContent = CreateScrollableTabContent(distanceContent, panelSprite);

        BuildDisplayTabContent(displayScrollContent, font, panelSprite, knobSprite, refs);
        BuildKeybindTabContent(keybindScrollContent, font, panelSprite, refs);
        BuildAudioTabContent(audioScrollContent, font, panelSprite, knobSprite, refs);
        BuildGraphicsTabContent(graphicsScrollContent, font, panelSprite, knobSprite, refs);
        BuildDistanceTabContent(distanceScrollContent, font, panelSprite, knobSprite, refs);

        refs.DisplayTabContent = displayContent.gameObject;
        refs.KeybindTabContent = keybindContent.gameObject;
        refs.AudioTabContent = audioContent.gameObject;
        refs.GraphicsTabContent = graphicsContent.gameObject;
        refs.DistanceTabContent = distanceContent.gameObject;

        TMP_Text status = CreateText(
            "Settings Status Text",
            settingsFrame,
            string.Empty,
            font,
            22f,
            TextAlignmentOptions.Center,
            FontStyles.Italic,
            TextSecondary);
        status.textWrappingMode = TextWrappingModes.NoWrap;
        status.overflowMode = TextOverflowModes.Ellipsis;
        status.rectTransform.anchorMin = new Vector2(0f, 0f);
        status.rectTransform.anchorMax = new Vector2(1f, 0f);
        status.rectTransform.pivot = new Vector2(0.5f, 0f);
        status.rectTransform.offsetMin = new Vector2(16f, 114f);
        status.rectTransform.offsetMax = new Vector2(-16f, 162f);
        refs.StatusText = status;

        RectTransform footerRow = CreateRect("Settings Footer Row", settingsFrame);
        footerRow.anchorMin = new Vector2(0f, 0f);
        footerRow.anchorMax = new Vector2(1f, 0f);
        footerRow.pivot = new Vector2(0.5f, 0f);
        footerRow.offsetMin = new Vector2(0f, 26f);
        footerRow.offsetMax = new Vector2(0f, 100f);

        HorizontalLayoutGroup footerLayout = footerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerLayout.spacing = 14f;
        footerLayout.padding = new RectOffset(0, 0, 0, 0);
        footerLayout.childControlHeight = true;
        footerLayout.childControlWidth = false;
        footerLayout.childForceExpandHeight = false;
        footerLayout.childForceExpandWidth = false;
        footerLayout.childAlignment = TextAnchor.MiddleCenter;

        refs.BackButton = CreateFlatButton(footerRow, "Settings Back Button", "Back", font, false, 56f, 200f);
        refs.ApplyButton = CreateFlatButton(footerRow, "Settings Apply Button", "Apply", font, true, 56f, 200f);

        return refs;
    }

    private static void BuildDisplayTabContent(
        RectTransform parent,
        TMP_FontAsset font,
        Sprite panelSprite,
        Sprite knobSprite,
        SettingsMenuRefs refs)
    {
        VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperLeft;

        RectTransform resolutionRow = CreateSettingsRow(parent, font, panelSprite, "Resolution");
        RectTransform resolutionControl = CreateRowControlContainer(resolutionRow);
        refs.ResolutionPreviousButton = CreateSmallIconButton(resolutionControl, "Resolution Previous", "<", font);

        Image resolutionValuePanel = CreateImage("Resolution Value Panel", resolutionControl, panelSprite, ValuePanelColor, true);
        resolutionValuePanel.type = Image.Type.Sliced;
        LayoutElement resolutionValueLayout = resolutionValuePanel.gameObject.AddComponent<LayoutElement>();
        resolutionValueLayout.preferredHeight = 52f;
        resolutionValueLayout.minHeight = 52f;
        resolutionValueLayout.minWidth = 360f;
        resolutionValueLayout.preferredWidth = 560f;
        resolutionValueLayout.flexibleWidth = 1f;

        refs.ResolutionValueText = CreateText(
            "Resolution Value Text",
            resolutionValuePanel.rectTransform,
            "1920 x 1080",
            font,
            27f,
            TextAlignmentOptions.Center,
            FontStyles.Normal,
            TextPrimary);
        StretchRect(refs.ResolutionValueText.rectTransform, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        refs.ResolutionValueText.textWrappingMode = TextWrappingModes.NoWrap;
        refs.ResolutionValueText.overflowMode = TextOverflowModes.Ellipsis;
        refs.ResolutionValueText.enableAutoSizing = true;
        refs.ResolutionValueText.fontSizeMin = 19f;
        refs.ResolutionValueText.fontSizeMax = 29f;

        refs.ResolutionNextButton = CreateSmallIconButton(resolutionControl, "Resolution Next", ">", font);

        RectTransform fullscreenRow = CreateSettingsRow(parent, font, panelSprite, "Fullscreen");
        RectTransform fullscreenControl = CreateRowControlContainer(fullscreenRow);
        refs.FullscreenToggle = CreateFlatToggle(fullscreenControl, "Fullscreen Toggle", panelSprite, true);
        AddFlexibleSpacer(fullscreenControl);

        RectTransform vsyncRow = CreateSettingsRow(parent, font, panelSprite, "VSync");
        RectTransform vsyncControl = CreateRowControlContainer(vsyncRow);
        refs.VSyncToggle = CreateFlatToggle(vsyncControl, "VSync Toggle", panelSprite, true);
        AddFlexibleSpacer(vsyncControl);

        RectTransform brightnessRow = CreateSettingsRow(parent, font, panelSprite, "Brightness");
        RectTransform brightnessControl = CreateRowControlContainer(brightnessRow);
        refs.BrightnessSlider = CreateFlatSlider(brightnessControl, "Brightness Slider", panelSprite, knobSprite);

        Image brightnessValuePanel = CreateImage("Brightness Value Panel", brightnessControl, panelSprite, ValuePanelColor, true);
        brightnessValuePanel.type = Image.Type.Sliced;
        LayoutElement brightnessValueLayout = brightnessValuePanel.gameObject.AddComponent<LayoutElement>();
        brightnessValueLayout.preferredHeight = 52f;
        brightnessValueLayout.minHeight = 52f;
        brightnessValueLayout.preferredWidth = 112f;
        brightnessValueLayout.minWidth = 112f;
        refs.BrightnessValueText = CreateText(
            "Brightness Value",
            brightnessValuePanel.rectTransform,
            "100%",
            font,
            26f,
            TextAlignmentOptions.Center,
            FontStyles.Normal,
            TextPrimary);
        StretchRect(refs.BrightnessValueText.rectTransform);
        refs.BrightnessValueText.textWrappingMode = TextWrappingModes.NoWrap;
        refs.BrightnessValueText.enableAutoSizing = true;
        refs.BrightnessValueText.fontSizeMin = 18f;
        refs.BrightnessValueText.fontSizeMax = 28f;

        RectTransform uiScaleRow = CreateSettingsRow(parent, font, panelSprite, "UI Scale");
        RectTransform uiScaleControl = CreateRowControlContainer(uiScaleRow);
        refs.UiScaleSlider = CreateFlatSlider(uiScaleControl, "UI Scale Slider", panelSprite, knobSprite);

        Image uiScaleValuePanel = CreateImage("UI Scale Value Panel", uiScaleControl, panelSprite, ValuePanelColor, true);
        uiScaleValuePanel.type = Image.Type.Sliced;
        LayoutElement uiScaleValueLayout = uiScaleValuePanel.gameObject.AddComponent<LayoutElement>();
        uiScaleValueLayout.preferredHeight = 52f;
        uiScaleValueLayout.minHeight = 52f;
        uiScaleValueLayout.preferredWidth = 112f;
        uiScaleValueLayout.minWidth = 112f;
        refs.UiScaleValueText = CreateText(
            "UI Scale Value",
            uiScaleValuePanel.rectTransform,
            "100%",
            font,
            26f,
            TextAlignmentOptions.Center,
            FontStyles.Normal,
            TextPrimary);
        StretchRect(refs.UiScaleValueText.rectTransform);
        refs.UiScaleValueText.textWrappingMode = TextWrappingModes.NoWrap;
        refs.UiScaleValueText.enableAutoSizing = true;
        refs.UiScaleValueText.fontSizeMin = 18f;
        refs.UiScaleValueText.fontSizeMax = 28f;
    }

    private static void BuildKeybindTabContent(
        RectTransform parent,
        TMP_FontAsset font,
        Sprite panelSprite,
        SettingsMenuRefs refs)
    {
        VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperLeft;

        refs.MoveForwardKeyButton = CreateKeybindRow(parent, font, panelSprite, "Move Forward", "W");
        refs.MoveBackwardKeyButton = CreateKeybindRow(parent, font, panelSprite, "Move Backward", "S");
        refs.MoveLeftKeyButton = CreateKeybindRow(parent, font, panelSprite, "Move Left", "A");
        refs.MoveRightKeyButton = CreateKeybindRow(parent, font, panelSprite, "Move Right", "D");
        refs.JumpKeyButton = CreateKeybindRow(parent, font, panelSprite, "Jump", "Space");
        refs.SprintKeyButton = CreateKeybindRow(parent, font, panelSprite, "Sprint", "Left Shift");
        refs.InteractKeyButton = CreateKeybindRow(parent, font, panelSprite, "Interact", "E");
        refs.AttackKeyButton = CreateKeybindRow(parent, font, panelSprite, "Attack", "Mouse 1");
        refs.InventoryKeyButton = CreateKeybindRow(parent, font, panelSprite, "Inventory", "I");

        RectTransform infoRow = CreateRect("Keybind Info Row", parent);
        LayoutElement infoLayout = infoRow.gameObject.AddComponent<LayoutElement>();
        infoLayout.preferredHeight = 58f;
        infoLayout.minHeight = 58f;
        Image infoImage = infoRow.gameObject.AddComponent<Image>();
        infoImage.sprite = panelSprite;
        infoImage.type = Image.Type.Sliced;
        infoImage.color = new Color(0.18f, 0.13f, 0.09f, 0.40f);
        infoImage.raycastTarget = false;

        refs.KeybindInfoText = CreateText(
            "Keybind Info Text",
            infoRow,
            string.Empty,
            font,
            22f,
            TextAlignmentOptions.Center,
            FontStyles.Italic,
            TextSecondary);
        StretchRect(refs.KeybindInfoText.rectTransform, new Vector2(10f, 0f), new Vector2(-10f, 0f));
        refs.KeybindInfoText.textWrappingMode = TextWrappingModes.NoWrap;
        refs.KeybindInfoText.overflowMode = TextOverflowModes.Ellipsis;
    }

    private static void BuildAudioTabContent(
        RectTransform parent,
        TMP_FontAsset font,
        Sprite panelSprite,
        Sprite knobSprite,
        SettingsMenuRefs refs)
    {
        VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperLeft;

        CreateSliderValueRow(parent, font, panelSprite, knobSprite, "Master Volume", out refs.MasterVolumeSlider, out refs.MasterVolumeValueText);
        CreateSliderValueRow(parent, font, panelSprite, knobSprite, "Music Volume", out refs.MusicVolumeSlider, out refs.MusicVolumeValueText);
        CreateSliderValueRow(parent, font, panelSprite, knobSprite, "SFX Volume", out refs.SfxVolumeSlider, out refs.SfxVolumeValueText);
        CreateSliderValueRow(parent, font, panelSprite, knobSprite, "Ambience Volume", out refs.AmbienceVolumeSlider, out refs.AmbienceVolumeValueText);

        RectTransform mutedRow = CreateSettingsRow(parent, font, panelSprite, "Muted");
        RectTransform mutedControls = CreateRowControlContainer(mutedRow);
        refs.MutedToggle = CreateFlatToggle(mutedControls, "Muted Toggle", panelSprite, false);
        AddFlexibleSpacer(mutedControls);
    }

    private static void BuildGraphicsTabContent(
        RectTransform parent,
        TMP_FontAsset font,
        Sprite panelSprite,
        Sprite knobSprite,
        SettingsMenuRefs refs)
    {
        VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperLeft;

        CreateOptionCycleRow(parent, font, panelSprite, "Quality", out refs.QualityPreviousButton, out refs.QualityValueText, out refs.QualityNextButton, "High");
        CreateOptionCycleRow(parent, font, panelSprite, "Frame Rate", out refs.FrameRatePreviousButton, out refs.FrameRateValueText, out refs.FrameRateNextButton, "Unlimited");
        CreateSliderValueRow(parent, font, panelSprite, knobSprite, "Render Scale", out refs.RenderScaleSlider, out refs.RenderScaleValueText);
        CreateOptionCycleRow(parent, font, panelSprite, "Anti Aliasing", out refs.AntiAliasingPreviousButton, out refs.AntiAliasingValueText, out refs.AntiAliasingNextButton, "2x");
        CreateOptionCycleRow(parent, font, panelSprite, "Shadow Quality", out refs.ShadowQualityPreviousButton, out refs.ShadowQualityValueText, out refs.ShadowQualityNextButton, "All");
        CreateSliderValueRow(parent, font, panelSprite, knobSprite, "Shadow Distance", out refs.ShadowDistanceSlider, out refs.ShadowDistanceValueText);
        CreateOptionCycleRow(parent, font, panelSprite, "Texture Quality", out refs.TextureQualityPreviousButton, out refs.TextureQualityValueText, out refs.TextureQualityNextButton, "Full");

        RectTransform anisotropicRow = CreateSettingsRow(parent, font, panelSprite, "Anisotropic");
        RectTransform anisotropicControls = CreateRowControlContainer(anisotropicRow);
        refs.AnisotropicFilteringToggle = CreateFlatToggle(anisotropicControls, "Anisotropic Filtering Toggle", panelSprite, true);
        AddFlexibleSpacer(anisotropicControls);

        RectTransform bloomRow = CreateSettingsRow(parent, font, panelSprite, "Bloom");
        RectTransform bloomControls = CreateRowControlContainer(bloomRow);
        refs.BloomToggle = CreateFlatToggle(bloomControls, "Bloom Toggle", panelSprite, true);
        AddFlexibleSpacer(bloomControls);

        RectTransform blurRow = CreateSettingsRow(parent, font, panelSprite, "Motion Blur");
        RectTransform blurControls = CreateRowControlContainer(blurRow);
        refs.MotionBlurToggle = CreateFlatToggle(blurControls, "Motion Blur Toggle", panelSprite, false);
        AddFlexibleSpacer(blurControls);
    }

    private static void BuildDistanceTabContent(
        RectTransform parent,
        TMP_FontAsset font,
        Sprite panelSprite,
        Sprite knobSprite,
        SettingsMenuRefs refs)
    {
        VerticalLayoutGroup layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.childAlignment = TextAnchor.UpperLeft;

        CreateSliderValueRow(parent, font, panelSprite, knobSprite, "Object Range", out refs.ViewDistanceSlider, out refs.ViewDistanceValueText);
    }

    private static RectTransform CreateScrollableTabContent(RectTransform tabRoot, Sprite panelSprite)
    {
        RectTransform scrollRoot = CreateRect("Scroll View", tabRoot);
        StretchRect(scrollRoot);
        Image scrollRootImage = scrollRoot.gameObject.AddComponent<Image>();
        scrollRootImage.sprite = panelSprite;
        scrollRootImage.type = Image.Type.Sliced;
        scrollRootImage.color = new Color(0f, 0f, 0f, 0.001f);
        scrollRootImage.raycastTarget = true;

        ScrollRect scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 50f;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.135f;
        scrollRect.viewport = null;
        scrollRect.content = null;

        Image viewportImage = CreateImage("Viewport", scrollRoot, panelSprite, new Color(0f, 0f, 0f, 0.01f), true);
        viewportImage.type = Image.Type.Sliced;
        RectTransform viewport = viewportImage.rectTransform;
        StretchRect(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform content = CreateRect("Content", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(0f, 0f);
        content.offsetMax = new Vector2(0f, 0f);
        content.sizeDelta = new Vector2(0f, 0f);

        ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.verticalNormalizedPosition = 1f;

        return content;
    }

    private static Button CreateKeybindRow(RectTransform parent, TMP_FontAsset font, Sprite panelSprite, string label, string defaultBinding)
    {
        RectTransform row = CreateSettingsRow(parent, font, panelSprite, label);
        RectTransform controls = CreateRowControlContainer(row);
        Button keyButton = CreateFlatButton(controls, $"{label} Button", defaultBinding, font, false, 52f, 230f);
        AddFlexibleSpacer(controls);
        return keyButton;
    }

    private static void CreateOptionCycleRow(
        RectTransform parent,
        TMP_FontAsset font,
        Sprite panelSprite,
        string label,
        out Button previousButton,
        out TMP_Text valueText,
        out Button nextButton,
        string defaultValue)
    {
        RectTransform row = CreateSettingsRow(parent, font, panelSprite, label);
        RectTransform controls = CreateRowControlContainer(row);

        previousButton = CreateSmallIconButton(controls, $"{label} Previous", "<", font);

        Image valuePanel = CreateImage($"{label} Value Panel", controls, panelSprite, ValuePanelColor, true);
        valuePanel.type = Image.Type.Sliced;
        LayoutElement valueLayout = valuePanel.gameObject.AddComponent<LayoutElement>();
        valueLayout.preferredHeight = 52f;
        valueLayout.minHeight = 52f;
        valueLayout.preferredWidth = 360f;
        valueLayout.minWidth = 240f;
        valueLayout.flexibleWidth = 1f;

        valueText = CreateText(
            $"{label} Value Text",
            valuePanel.rectTransform,
            defaultValue,
            font,
            26f,
            TextAlignmentOptions.Center,
            FontStyles.Normal,
            TextPrimary);
        StretchRect(valueText.rectTransform, new Vector2(10f, 0f), new Vector2(-10f, 0f));
        valueText.textWrappingMode = TextWrappingModes.NoWrap;
        valueText.overflowMode = TextOverflowModes.Ellipsis;
        valueText.enableAutoSizing = true;
        valueText.fontSizeMin = 17f;
        valueText.fontSizeMax = 29f;

        nextButton = CreateSmallIconButton(controls, $"{label} Next", ">", font);
    }

    private static void CreateSliderValueRow(
        RectTransform parent,
        TMP_FontAsset font,
        Sprite panelSprite,
        Sprite knobSprite,
        string label,
        out Slider slider,
        out TMP_Text valueText)
    {
        RectTransform row = CreateSettingsRow(parent, font, panelSprite, label);
        RectTransform controls = CreateRowControlContainer(row);

        slider = CreateFlatSlider(controls, $"{label} Slider", panelSprite, knobSprite);

        Image valuePanel = CreateImage($"{label} Value Panel", controls, panelSprite, ValuePanelColor, true);
        valuePanel.type = Image.Type.Sliced;
        LayoutElement valueLayout = valuePanel.gameObject.AddComponent<LayoutElement>();
        valueLayout.preferredHeight = 52f;
        valueLayout.minHeight = 52f;
        valueLayout.preferredWidth = 112f;
        valueLayout.minWidth = 112f;
        valueLayout.flexibleWidth = 0f;

        valueText = CreateText(
            $"{label} Value Text",
            valuePanel.rectTransform,
            "100%",
            font,
            24f,
            TextAlignmentOptions.Center,
            FontStyles.Normal,
            TextPrimary);
        StretchRect(valueText.rectTransform);
        valueText.textWrappingMode = TextWrappingModes.NoWrap;
        valueText.enableAutoSizing = true;
        valueText.fontSizeMin = 17f;
        valueText.fontSizeMax = 28f;
    }

    private static RectTransform CreateSettingsRow(RectTransform parent, TMP_FontAsset font, Sprite panelSprite, string label)
    {
        const float labelLeft = 24f;
        const float labelRight = 306f;
        const float controlLeft = 326f;

        RectTransform row = CreateRect($"{label} Row", parent);
        Image rowImage = row.gameObject.AddComponent<Image>();
        rowImage.sprite = panelSprite;
        rowImage.type = Image.Type.Sliced;
        rowImage.color = new Color(0.18f, 0.13f, 0.09f, 0.58f);
        rowImage.raycastTarget = false;

        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 80f;
        rowLayout.minHeight = 80f;

        RectTransform labelRect = CreateRect("Label", row);
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(0f, 1f);
        labelRect.pivot = new Vector2(0f, 0.5f);
        labelRect.offsetMin = new Vector2(labelLeft, 0f);
        labelRect.offsetMax = new Vector2(labelRight, 0f);

        TMP_Text labelText = CreateText(
            "Label Text",
            labelRect,
            label,
            font,
            30f,
            TextAlignmentOptions.MidlineLeft,
            FontStyles.Bold,
            RowLabelColor);
        StretchRect(labelText.rectTransform);
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        labelText.overflowMode = TextOverflowModes.Ellipsis;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 22f;
        labelText.fontSizeMax = 34f;

        RectTransform controls = CreateRect("Controls", row);
        controls.anchorMin = new Vector2(0f, 0f);
        controls.anchorMax = new Vector2(1f, 1f);
        controls.pivot = new Vector2(0.5f, 0.5f);
        controls.offsetMin = new Vector2(controlLeft, 10f);
        controls.offsetMax = new Vector2(-20f, -10f);

        return row;
    }

    private static RectTransform CreateRowControlContainer(RectTransform row)
    {
        RectTransform controls = row.Find("Controls") as RectTransform;
        if (controls == null)
        {
            controls = CreateRect("Controls", row);
            controls.anchorMin = new Vector2(0f, 0f);
            controls.anchorMax = new Vector2(1f, 1f);
            controls.offsetMin = new Vector2(326f, 10f);
            controls.offsetMax = new Vector2(-20f, -10f);
        }

        HorizontalLayoutGroup controlsLayout = controls.gameObject.AddComponent<HorizontalLayoutGroup>();
        controlsLayout.spacing = 12f;
        controlsLayout.padding = new RectOffset(0, 0, 0, 0);
        controlsLayout.childAlignment = TextAnchor.MiddleLeft;
        controlsLayout.childControlHeight = true;
        controlsLayout.childControlWidth = true;
        controlsLayout.childForceExpandHeight = false;
        controlsLayout.childForceExpandWidth = false;

        return controls;
    }

    private static void AddFlexibleSpacer(RectTransform parent)
    {
        RectTransform spacer = CreateRect("Spacer", parent);
        LayoutElement layout = spacer.gameObject.AddComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.minWidth = 0f;
        layout.preferredWidth = 0f;
    }

    private static void BuildPlaceholderTab(RectTransform parent, TMP_FontAsset font, string text)
    {
        TMP_Text placeholder = CreateText(
            "Placeholder Text",
            parent,
            text,
            font,
            28f,
            TextAlignmentOptions.Center,
            FontStyles.Italic,
            TextSecondary);
        StretchRect(placeholder.rectTransform, new Vector2(12f, 12f), new Vector2(-12f, -12f));
        placeholder.textWrappingMode = TextWrappingModes.Normal;
    }

    private static CreditsMenuRefs BuildCreditsScreen(RectTransform parent, TMP_FontAsset font, Sprite uiSprite)
    {
        CreditsMenuRefs refs = new CreditsMenuRefs();

        TMP_Text title = CreateText(
            "Credits Title",
            parent,
            "CREDITS",
            font,
            56f,
            TextAlignmentOptions.Center,
            FontStyles.Bold,
            TextPrimary);
        SetTopRect(title.rectTransform, 0f, -20f, 90f);

        Image panel = CreateImage("Credits Panel", parent, uiSprite, CardColor, false);
        panel.type = Image.Type.Sliced;
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.offsetMin = new Vector2(0f, 128f);
        panelRect.offsetMax = new Vector2(0f, -132f);

        TMP_Text body = CreateText(
            "Credits Body",
            panelRect,
            "You survived the crash.\nNow every night on this island\nis lived by sword, fire, and old laws.",
            font,
            30f,
            TextAlignmentOptions.Center,
            FontStyles.Normal,
            TextPrimary);
        StretchRect(body.rectTransform, new Vector2(18f, 18f), new Vector2(-18f, -18f));
        body.textWrappingMode = TextWrappingModes.Normal;
        body.lineSpacing = 8f;

        RectTransform footer = CreateRect("Credits Footer", parent);
        footer.anchorMin = new Vector2(0f, 0f);
        footer.anchorMax = new Vector2(1f, 0f);
        footer.pivot = new Vector2(0.5f, 0f);
        footer.offsetMin = new Vector2(0f, 24f);
        footer.offsetMax = new Vector2(0f, 90f);

        HorizontalLayoutGroup footerLayout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerLayout.spacing = 12f;
        footerLayout.padding = new RectOffset(0, 0, 0, 0);
        footerLayout.childAlignment = TextAnchor.MiddleCenter;
        footerLayout.childControlHeight = true;
        footerLayout.childControlWidth = false;
        footerLayout.childForceExpandHeight = false;
        footerLayout.childForceExpandWidth = false;

        refs.BackButton = CreateFlatButton(footer, "Credits Back Button", "Back", font, false, 56f, 180f);

        return refs;
    }

    private static LoadingScreenRefs BuildLoadingScreen(
        RectTransform parent,
        TMP_FontAsset font,
        Sprite uiSprite,
        Sprite knobSprite,
        Sprite panelSprite)
    {
        LoadingScreenRefs refs = new LoadingScreenRefs();

        RectTransform root = CreateRect("Loading Screen", parent);
        StretchRect(root);
        refs.Root = root.gameObject;

        Image blocker = CreateImage("Loading Blocker", root, uiSprite, new Color(0f, 0f, 0f, 0.92f), true);
        blocker.type = Image.Type.Sliced;
        StretchRect(blocker.rectTransform);

        Image topShade = CreateImage("Loading Top Shade", root, uiSprite, new Color(0.29f, 0.20f, 0.10f, 0.36f), false);
        topShade.type = Image.Type.Sliced;
        topShade.rectTransform.anchorMin = new Vector2(0f, 1f);
        topShade.rectTransform.anchorMax = new Vector2(1f, 1f);
        topShade.rectTransform.pivot = new Vector2(0.5f, 1f);
        topShade.rectTransform.offsetMin = new Vector2(0f, -180f);
        topShade.rectTransform.offsetMax = Vector2.zero;

        Image bottomShade = CreateImage("Loading Bottom Shade", root, uiSprite, new Color(0.10f, 0.07f, 0.04f, 0.52f), false);
        bottomShade.type = Image.Type.Sliced;
        bottomShade.rectTransform.anchorMin = Vector2.zero;
        bottomShade.rectTransform.anchorMax = new Vector2(1f, 0f);
        bottomShade.rectTransform.pivot = new Vector2(0.5f, 0f);
        bottomShade.rectTransform.offsetMin = Vector2.zero;
        bottomShade.rectTransform.offsetMax = new Vector2(0f, 180f);

        Image panel = CreateImage("Loading Panel", root, panelSprite, new Color(0.22f, 0.17f, 0.12f, 0.98f), false);
        panel.type = Image.Type.Sliced;
        SetAnchoredRect(panel.rectTransform, new Vector2(860f, 430f), Vector2.zero, new Vector2(0.5f, 0.5f));

        Outline panelOutline = panel.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.70f, 0.58f, 0.30f, 0.78f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        Shadow panelShadow = panel.gameObject.AddComponent<Shadow>();
        panelShadow.effectColor = new Color(0f, 0f, 0f, 0.42f);
        panelShadow.effectDistance = new Vector2(0f, -5f);

        RectTransform content = CreateRect("Loading Content", panel.rectTransform);
        StretchRect(content, new Vector2(54f, 40f), new Vector2(-54f, -40f));

        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 18f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleCenter;

        TMP_Text brand = CreateText(
            "Loading Brand",
            content,
            "ONE MORE NIGHT",
            font,
            22f,
            TextAlignmentOptions.Center,
            FontStyles.Bold,
            TextSecondary);
        brand.textWrappingMode = TextWrappingModes.NoWrap;
        LayoutElement brandLayout = brand.gameObject.AddComponent<LayoutElement>();
        brandLayout.preferredHeight = 28f;
        brandLayout.minHeight = 24f;

        TMP_Text title = CreateText(
            "Loading Title",
            content,
            "LOADING",
            font,
            60f,
            TextAlignmentOptions.Center,
            FontStyles.Bold,
            TextPrimary);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.enableAutoSizing = true;
        title.fontSizeMin = 34f;
        title.fontSizeMax = 64f;
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 76f;
        titleLayout.minHeight = 66f;
        refs.TitleText = title;

        TMP_Text message = CreateText(
            "Loading Message",
            content,
            "Loading...",
            font,
            26f,
            TextAlignmentOptions.Center,
            FontStyles.Italic,
            TextSecondary);
        message.textWrappingMode = TextWrappingModes.Normal;
        message.enableAutoSizing = true;
        message.fontSizeMin = 18f;
        message.fontSizeMax = 28f;
        LayoutElement messageLayout = message.gameObject.AddComponent<LayoutElement>();
        messageLayout.preferredHeight = 50f;
        messageLayout.minHeight = 42f;
        refs.MessageText = message;

        TMP_Text stage = CreateText(
            "Loading Stage",
            content,
            "Starting",
            font,
            21f,
            TextAlignmentOptions.Center,
            FontStyles.Normal,
            TextSecondary);
        stage.textWrappingMode = TextWrappingModes.NoWrap;
        stage.overflowMode = TextOverflowModes.Ellipsis;
        LayoutElement stageLayout = stage.gameObject.AddComponent<LayoutElement>();
        stageLayout.preferredHeight = 32f;
        stageLayout.minHeight = 28f;
        refs.StageText = stage;

        Slider progressSlider = CreateFlatSlider(content, "Loading Progress Slider", uiSprite, knobSprite);
        progressSlider.interactable = false;
        progressSlider.transition = Selectable.Transition.None;
        progressSlider.SetValueWithoutNotify(0f);
        LayoutElement progressLayout = progressSlider.GetComponent<LayoutElement>();
        if (progressLayout != null)
        {
            progressLayout.preferredHeight = 52f;
            progressLayout.minHeight = 52f;
            progressLayout.preferredWidth = 620f;
            progressLayout.minWidth = 520f;
        }

        refs.ProgressSlider = progressSlider;

        TMP_Text progressText = CreateText(
            "Loading Progress Text",
            content,
            "0%",
            font,
            24f,
            TextAlignmentOptions.Center,
            FontStyles.Bold,
            TextPrimary);
        progressText.textWrappingMode = TextWrappingModes.NoWrap;
        LayoutElement progressTextLayout = progressText.gameObject.AddComponent<LayoutElement>();
        progressTextLayout.preferredHeight = 36f;
        progressTextLayout.minHeight = 32f;
        refs.ProgressText = progressText;

        Image spinner = CreateImage("Loading Spinner", panel.rectTransform, uiSprite, AccentColorHover, false);
        spinner.type = Image.Type.Sliced;
        SetAnchoredRect(spinner.rectTransform, new Vector2(54f, 54f), new Vector2(340f, 142f), new Vector2(0.5f, 0.5f));
        spinner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        Outline spinnerOutline = spinner.gameObject.AddComponent<Outline>();
        spinnerOutline.effectColor = new Color(0.70f, 0.58f, 0.30f, 0.78f);
        spinnerOutline.effectDistance = new Vector2(1f, -1f);
        refs.Spinner = spinner.rectTransform;

        return refs;
    }

    private static Button CreateFlatButton(
        RectTransform parent,
        string name,
        string label,
        TMP_FontAsset font,
        bool accent,
        float height,
        float width,
        bool leftAligned = false)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
        layout.minHeight = height;
        if (width > 0f)
        {
            layout.preferredWidth = width;
            layout.minWidth = width;
        }
        else
        {
            layout.flexibleWidth = 1f;
        }

        Image image = buttonObject.GetComponent<Image>();
        image.type = Image.Type.Sliced;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.navigation = NoNavigation();

        Color normal = accent ? AccentColor : NeutralButtonColor;
        Color highlighted = accent ? AccentColorHover : NeutralButtonHover;
        Color pressed = accent ? AccentColorPressed : NeutralButtonPressed;

        image.color = normal;

        ColorBlock block = button.colors;
        block.normalColor = normal;
        block.highlightedColor = highlighted;
        block.pressedColor = pressed;
        block.selectedColor = highlighted;
        block.disabledColor = DisabledButtonColor;
        block.colorMultiplier = 1f;
        block.fadeDuration = 0.08f;
        button.colors = block;

        Outline frame = buttonObject.AddComponent<Outline>();
        frame.effectColor = accent
            ? new Color(0.70f, 0.58f, 0.30f, 0.78f)
            : new Color(0.53f, 0.36f, 0.18f, 0.70f);
        frame.effectDistance = new Vector2(1f, -1f);

        Shadow buttonShadow = buttonObject.AddComponent<Shadow>();
        buttonShadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
        buttonShadow.effectDistance = new Vector2(0f, -2f);

        TMP_Text text = CreateText(
            "Label",
            rect,
            label,
            font,
            height >= 70f ? 36f : 30f,
            leftAligned ? TextAlignmentOptions.MidlineLeft : TextAlignmentOptions.Center,
            FontStyles.Bold,
            TextPrimary);
        StretchRect(
            text.rectTransform,
            new Vector2(leftAligned ? 20f : 10f, 8f),
            new Vector2(-10f, -8f));
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.enableAutoSizing = true;
        text.fontSizeMin = height >= 70f ? 22f : 18f;
        text.fontSizeMax = height >= 70f ? 40f : 32f;

        return button;
    }

    private static Button CreateSmallIconButton(RectTransform parent, string name, string label, TMP_FontAsset font)
    {
        Button button = CreateFlatButton(parent, name, label, font, false, 52f, 78f, false);
        TMP_Text labelText = button.GetComponentInChildren<TMP_Text>(true);
        if (labelText != null)
        {
            labelText.fontSize = 32f;
            labelText.enableAutoSizing = false;
            labelText.alignment = TextAlignmentOptions.Center;
        }

        return button;
    }

    private static Toggle CreateFlatToggle(RectTransform parent, string name, Sprite uiSprite, bool isOn)
    {
        GameObject toggleObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Toggle));
        RectTransform rect = toggleObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;

        LayoutElement layout = toggleObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 50f;
        layout.minWidth = 50f;
        layout.preferredHeight = 50f;
        layout.minHeight = 50f;

        Image boxImage = toggleObject.GetComponent<Image>();
        boxImage.sprite = uiSprite;
        boxImage.type = Image.Type.Sliced;
        boxImage.color = ValuePanelColor;
        boxImage.raycastTarget = true;

        RectTransform checkRect = CreateRect("Checkmark", rect);
        StretchRect(checkRect, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        Image checkImage = checkRect.gameObject.AddComponent<Image>();
        checkImage.sprite = uiSprite;
        checkImage.type = Image.Type.Sliced;
        checkImage.color = AccentColorHover;
        checkImage.raycastTarget = false;

        Toggle toggle = toggleObject.GetComponent<Toggle>();
        toggle.targetGraphic = boxImage;
        toggle.graphic = checkImage;
        toggle.isOn = isOn;
        toggle.navigation = NoNavigation();

        return toggle;
    }

    private static Slider CreateFlatSlider(RectTransform parent, string name, Sprite uiSprite, Sprite knobSprite)
    {
        GameObject sliderObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
        RectTransform rect = sliderObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;

        LayoutElement layout = sliderObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 52f;
        layout.minHeight = 52f;
        layout.preferredWidth = 640f;
        layout.minWidth = 520f;
        layout.flexibleWidth = 1f;

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.navigation = NoNavigation();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        slider.wholeNumbers = false;

        RectTransform trackRect = CreateRect("Track", rect);
        trackRect.anchorMin = new Vector2(0f, 0.5f);
        trackRect.anchorMax = new Vector2(1f, 0.5f);
        trackRect.pivot = new Vector2(0.5f, 0.5f);
        trackRect.offsetMin = new Vector2(0f, -8f);
        trackRect.offsetMax = new Vector2(0f, 8f);
        Image trackImage = trackRect.gameObject.AddComponent<Image>();
        trackImage.sprite = uiSprite;
        trackImage.type = Image.Type.Sliced;
        trackImage.color = SliderTrackColor;
        trackImage.raycastTarget = true;

        RectTransform fillArea = CreateRect("Fill Area", rect);
        StretchRect(fillArea, new Vector2(10f, 18f), new Vector2(-10f, -18f));

        RectTransform fillRect = CreateRect("Fill", fillArea);
        StretchRect(fillRect);
        Image fillImage = fillRect.gameObject.AddComponent<Image>();
        fillImage.sprite = uiSprite;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = AccentColor;
        fillImage.raycastTarget = false;

        RectTransform handleSlideArea = CreateRect("Handle Slide Area", rect);
        StretchRect(handleSlideArea, new Vector2(10f, 4f), new Vector2(-10f, -4f));

        RectTransform handleRect = CreateRect("Handle", handleSlideArea);
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(34f, 34f);
        handleRect.anchoredPosition = Vector2.zero;
        Image handleImage = handleRect.gameObject.AddComponent<Image>();
        handleImage.sprite = knobSprite;
        handleImage.type = Image.Type.Sliced;
        handleImage.color = SliderKnobColor;
        handleImage.raycastTarget = true;

        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private static void BuildLowPolyBackdrop(RectTransform canvasRect, Sprite uiSprite)
    {
        RectTransform backdropRoot = CreateRect("Low Poly Backdrop", canvasRect);
        StretchRect(backdropRoot);

        Color[] colors =
        {
            new Color(0.12f, 0.19f, 0.11f, 0.48f),
            new Color(0.15f, 0.23f, 0.14f, 0.34f),
            new Color(0.19f, 0.27f, 0.17f, 0.28f),
            new Color(0.11f, 0.17f, 0.10f, 0.33f),
            new Color(0.20f, 0.16f, 0.10f, 0.30f),
            new Color(0.15f, 0.12f, 0.08f, 0.38f),
            new Color(0.17f, 0.24f, 0.13f, 0.28f),
        };

        Vector2[] sizes =
        {
            new Vector2(760f, 320f),
            new Vector2(620f, 280f),
            new Vector2(560f, 260f),
            new Vector2(640f, 340f),
            new Vector2(560f, 230f),
            new Vector2(700f, 290f),
            new Vector2(460f, 210f),
        };

        Vector2[] positions =
        {
            new Vector2(260f, 410f),
            new Vector2(180f, 170f),
            new Vector2(220f, -40f),
            new Vector2(120f, -290f),
            new Vector2(560f, -250f),
            new Vector2(420f, 260f),
            new Vector2(640f, 10f),
        };

        float[] rotations = { -15f, 18f, -10f, 24f, -22f, 12f, -8f };

        for (int i = 0; i < colors.Length; i++)
        {
            Image shard = CreateImage($"Backdrop Shard {i + 1}", backdropRoot, uiSprite, colors[i], false);
            RectTransform shardRect = shard.rectTransform;
            shardRect.anchorMin = new Vector2(0f, 0.5f);
            shardRect.anchorMax = new Vector2(0f, 0.5f);
            shardRect.pivot = new Vector2(0.5f, 0.5f);
            shardRect.sizeDelta = sizes[i];
            shardRect.anchoredPosition = positions[i];
            shardRect.localRotation = Quaternion.Euler(0f, 0f, rotations[i]);
            shardRect.localScale = Vector3.one;
            shard.type = Image.Type.Sliced;
        }

        Image sunDisc = CreateImage("Dawn Disc", backdropRoot, uiSprite, new Color(0.85f, 0.62f, 0.28f, 0.26f), false);
        RectTransform sunRect = sunDisc.rectTransform;
        sunRect.anchorMin = new Vector2(0f, 1f);
        sunRect.anchorMax = new Vector2(0f, 1f);
        sunRect.pivot = new Vector2(0.5f, 0.5f);
        sunRect.sizeDelta = new Vector2(230f, 230f);
        sunRect.anchoredPosition = new Vector2(280f, -180f);
        sunDisc.type = Image.Type.Sliced;

        Image horizonBand = CreateImage("Sea Haze", backdropRoot, uiSprite, new Color(0.72f, 0.63f, 0.48f, 0.10f), false);
        horizonBand.type = Image.Type.Sliced;
        horizonBand.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        horizonBand.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        horizonBand.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        horizonBand.rectTransform.offsetMin = new Vector2(0f, -32f);
        horizonBand.rectTransform.offsetMax = new Vector2(0f, 32f);

        Image dimmer = CreateImage("Global Dimmer", backdropRoot, uiSprite, new Color(0f, 0f, 0f, 0.24f), false);
        StretchRect(dimmer.rectTransform);
    }

    private static void AddFrameStud(RectTransform parent, Sprite uiSprite, Vector2 anchor, Vector2 offset)
    {
        Image stud = CreateImage("Frame Stud", parent, uiSprite, new Color(0.74f, 0.60f, 0.30f, 0.92f), false);
        RectTransform studRect = stud.rectTransform;
        studRect.anchorMin = anchor;
        studRect.anchorMax = anchor;
        studRect.pivot = anchor;
        studRect.sizeDelta = new Vector2(16f, 16f);
        studRect.anchoredPosition = offset;
        studRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
        stud.type = Image.Type.Sliced;

        Outline studOutline = stud.gameObject.AddComponent<Outline>();
        studOutline.effectColor = new Color(0.22f, 0.14f, 0.08f, 0.78f);
        studOutline.effectDistance = new Vector2(1f, -1f);
    }

    private static Image CreateImage(string name, RectTransform parent, Sprite sprite, Color color, bool raycastTarget)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = raycastTarget;
        image.preserveAspect = false;

        return image;
    }

    private static TMP_Text CreateText(
        string name,
        RectTransform parent,
        string value,
        TMP_FontAsset font,
        float fontSize,
        TextAlignmentOptions alignment,
        FontStyles style,
        Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.enableAutoSizing = false;
        text.characterSpacing = 0f;

        return text;
    }

    private static RectTransform CreateRect(string name, RectTransform parent)
    {
        GameObject rectObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = rectObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.localScale = Vector3.one;
        return rect;
    }

    private static void SetTopRect(RectTransform rect, float x, float yFromTop, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(x, yFromTop - height);
        rect.offsetMax = new Vector2(-x, yFromTop);
    }

    private static void StretchRect(RectTransform rect)
    {
        StretchRect(rect, Vector2.zero, Vector2.zero);
    }

    private static void StretchRect(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetAnchoredRect(RectTransform rect, Vector2 size, Vector2 anchoredPosition, Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
    }

    private static Navigation NoNavigation()
    {
        Navigation navigation = new Navigation();
        navigation.mode = Navigation.Mode.None;
        return navigation;
    }

    private static void AssignControllerReferences(
        FantasyMenuController controller,
        RectTransform scaledRoot,
        Image backgroundImage,
        Sprite mainBackgroundSprite,
        Sprite settingsBackgroundSprite,
        GameObject menuShellRoot,
        GameObject mainScreen,
        GameObject settingsScreen,
        GameObject creditsScreen,
        MainMenuRefs mainRefs,
        SettingsMenuRefs settingsRefs,
        CreditsMenuRefs creditsRefs,
        LoadingScreenRefs loadingRefs,
        Sprite tabNormalSprite,
        Sprite tabActiveSprite)
    {
        SerializedObject serializedController = new SerializedObject(controller);

        SerializedProperty gameplaySceneName = serializedController.FindProperty("gameplaySceneName");
        if (gameplaySceneName != null)
        {
            gameplaySceneName.stringValue = "SampleScene";
        }

        SetObject(serializedController, "scaledRoot", scaledRoot);
        SetObject(serializedController, "backgroundImage", backgroundImage);
        SetObject(serializedController, "mainBackgroundSprite", mainBackgroundSprite);
        SetObject(serializedController, "settingsBackgroundSprite", settingsBackgroundSprite);
        SetObject(serializedController, "menuShellRoot", menuShellRoot);
        SetColor(serializedController, "mainBackgroundTint", MainBackgroundColor);
        SetColor(serializedController, "settingsBackgroundTint", SettingsBackgroundColor);
        SetObject(serializedController, "mainScreen", mainScreen);
        SetObject(serializedController, "settingsScreen", settingsScreen);
        SetObject(serializedController, "creditsScreen", creditsScreen);
        SetObject(serializedController, "statusText", mainRefs.StatusText);

        SetObject(serializedController, "newGameButton", mainRefs.NewGameButton);
        SetObject(serializedController, "continueButton", mainRefs.ContinueButton);
        SetObject(serializedController, "loadGameButton", mainRefs.LoadGameButton);
        SetObject(serializedController, "settingsButton", mainRefs.SettingsButton);
        SetObject(serializedController, "creditsButton", mainRefs.CreditsButton);
        SetObject(serializedController, "exitButton", mainRefs.ExitButton);

        SetObject(serializedController, "settingsBackButton", settingsRefs.BackButton);
        SetObject(serializedController, "settingsApplyButton", settingsRefs.ApplyButton);
        SetObject(serializedController, "displayTabButton", settingsRefs.DisplayTabButton);
        SetObject(serializedController, "keybindTabButton", settingsRefs.KeybindTabButton);
        SetObject(serializedController, "audioTabButton", settingsRefs.AudioTabButton);
        SetObject(serializedController, "graphicsTabButton", settingsRefs.GraphicsTabButton);
        SetObject(serializedController, "distanceTabButton", settingsRefs.DistanceTabButton);
        SetObject(serializedController, "displayTabImage", settingsRefs.DisplayTabImage);
        SetObject(serializedController, "keybindTabImage", settingsRefs.KeybindTabImage);
        SetObject(serializedController, "audioTabImage", settingsRefs.AudioTabImage);
        SetObject(serializedController, "graphicsTabImage", settingsRefs.GraphicsTabImage);
        SetObject(serializedController, "distanceTabImage", settingsRefs.DistanceTabImage);
        SetObject(serializedController, "tabNormalSprite", tabNormalSprite);
        SetObject(serializedController, "tabActiveSprite", tabActiveSprite);
        SetColor(serializedController, "tabNormalColor", NeutralButtonColor);
        SetColor(serializedController, "tabActiveColor", AccentColor);
        SetObject(serializedController, "displayTabContent", settingsRefs.DisplayTabContent);
        SetObject(serializedController, "keybindTabContent", settingsRefs.KeybindTabContent);
        SetObject(serializedController, "audioTabContent", settingsRefs.AudioTabContent);
        SetObject(serializedController, "graphicsTabContent", settingsRefs.GraphicsTabContent);
        SetObject(serializedController, "distanceTabContent", settingsRefs.DistanceTabContent);
        SetObject(serializedController, "settingsStatusText", settingsRefs.StatusText);

        SetObject(serializedController, "creditsBackButton", creditsRefs.BackButton);

        SetObject(serializedController, "loadingScreen", loadingRefs.Root);
        SetObject(serializedController, "loadingTitleText", loadingRefs.TitleText);
        SetObject(serializedController, "loadingMessageText", loadingRefs.MessageText);
        SetObject(serializedController, "loadingStageText", loadingRefs.StageText);
        SetObject(serializedController, "loadingProgressText", loadingRefs.ProgressText);
        SetObject(serializedController, "loadingProgressSlider", loadingRefs.ProgressSlider);
        SetObject(serializedController, "loadingSpinner", loadingRefs.Spinner);

        SetObject(serializedController, "resolutionPreviousButton", settingsRefs.ResolutionPreviousButton);
        SetObject(serializedController, "resolutionNextButton", settingsRefs.ResolutionNextButton);
        SetObject(serializedController, "resolutionValueText", settingsRefs.ResolutionValueText);
        SetObject(serializedController, "fullscreenToggle", settingsRefs.FullscreenToggle);
        SetObject(serializedController, "vSyncToggle", settingsRefs.VSyncToggle);
        SetObject(serializedController, "brightnessSlider", settingsRefs.BrightnessSlider);
        SetObject(serializedController, "brightnessValueText", settingsRefs.BrightnessValueText);
        SetObject(serializedController, "uiScaleSlider", settingsRefs.UiScaleSlider);
        SetObject(serializedController, "uiScaleValueText", settingsRefs.UiScaleValueText);
        SetObject(serializedController, "masterVolumeSlider", settingsRefs.MasterVolumeSlider);
        SetObject(serializedController, "masterVolumeValueText", settingsRefs.MasterVolumeValueText);
        SetObject(serializedController, "musicVolumeSlider", settingsRefs.MusicVolumeSlider);
        SetObject(serializedController, "musicVolumeValueText", settingsRefs.MusicVolumeValueText);
        SetObject(serializedController, "sfxVolumeSlider", settingsRefs.SfxVolumeSlider);
        SetObject(serializedController, "sfxVolumeValueText", settingsRefs.SfxVolumeValueText);
        SetObject(serializedController, "ambienceVolumeSlider", settingsRefs.AmbienceVolumeSlider);
        SetObject(serializedController, "ambienceVolumeValueText", settingsRefs.AmbienceVolumeValueText);
        SetObject(serializedController, "mutedToggle", settingsRefs.MutedToggle);

        SetObject(serializedController, "qualityPreviousButton", settingsRefs.QualityPreviousButton);
        SetObject(serializedController, "qualityNextButton", settingsRefs.QualityNextButton);
        SetObject(serializedController, "qualityValueText", settingsRefs.QualityValueText);
        SetObject(serializedController, "frameRatePreviousButton", settingsRefs.FrameRatePreviousButton);
        SetObject(serializedController, "frameRateNextButton", settingsRefs.FrameRateNextButton);
        SetObject(serializedController, "frameRateValueText", settingsRefs.FrameRateValueText);
        SetObject(serializedController, "renderScaleSlider", settingsRefs.RenderScaleSlider);
        SetObject(serializedController, "renderScaleValueText", settingsRefs.RenderScaleValueText);
        SetObject(serializedController, "antiAliasingPreviousButton", settingsRefs.AntiAliasingPreviousButton);
        SetObject(serializedController, "antiAliasingNextButton", settingsRefs.AntiAliasingNextButton);
        SetObject(serializedController, "antiAliasingValueText", settingsRefs.AntiAliasingValueText);
        SetObject(serializedController, "shadowQualityPreviousButton", settingsRefs.ShadowQualityPreviousButton);
        SetObject(serializedController, "shadowQualityNextButton", settingsRefs.ShadowQualityNextButton);
        SetObject(serializedController, "shadowQualityValueText", settingsRefs.ShadowQualityValueText);
        SetObject(serializedController, "shadowDistanceSlider", settingsRefs.ShadowDistanceSlider);
        SetObject(serializedController, "shadowDistanceValueText", settingsRefs.ShadowDistanceValueText);
        SetObject(serializedController, "textureQualityPreviousButton", settingsRefs.TextureQualityPreviousButton);
        SetObject(serializedController, "textureQualityNextButton", settingsRefs.TextureQualityNextButton);
        SetObject(serializedController, "textureQualityValueText", settingsRefs.TextureQualityValueText);
        SetObject(serializedController, "anisotropicFilteringToggle", settingsRefs.AnisotropicFilteringToggle);
        SetObject(serializedController, "viewDistanceSlider", settingsRefs.ViewDistanceSlider);
        SetObject(serializedController, "viewDistanceValueText", settingsRefs.ViewDistanceValueText);
        SetObject(serializedController, "bloomToggle", settingsRefs.BloomToggle);
        SetObject(serializedController, "motionBlurToggle", settingsRefs.MotionBlurToggle);

        SetObject(serializedController, "moveForwardKeyButton", settingsRefs.MoveForwardKeyButton);
        SetObject(serializedController, "moveBackwardKeyButton", settingsRefs.MoveBackwardKeyButton);
        SetObject(serializedController, "moveLeftKeyButton", settingsRefs.MoveLeftKeyButton);
        SetObject(serializedController, "moveRightKeyButton", settingsRefs.MoveRightKeyButton);
        SetObject(serializedController, "jumpKeyButton", settingsRefs.JumpKeyButton);
        SetObject(serializedController, "sprintKeyButton", settingsRefs.SprintKeyButton);
        SetObject(serializedController, "interactKeyButton", settingsRefs.InteractKeyButton);
        SetObject(serializedController, "attackKeyButton", settingsRefs.AttackKeyButton);
        SetObject(serializedController, "inventoryKeyButton", settingsRefs.InventoryKeyButton);
        SetObject(serializedController, "keybindInfoText", settingsRefs.KeybindInfoText);

        serializedController.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.objectReferenceValue = value;
        }
    }

    private static void SetColor(SerializedObject serializedObject, string propertyName, Color value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.colorValue = value;
        }
    }

    private static void CreateWorldCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.backgroundColor = Color.black;
        camera.fieldOfView = 50f;

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

        cameraObject.transform.position = position;
        cameraObject.transform.rotation = Quaternion.LookRotation((target - position).normalized, Vector3.up);
    }

    private static void CreateWorldLight()
    {
        GameObject lightObject = new GameObject("Menu Key Light", typeof(Light));
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.89f, 0.74f);
        light.intensity = 1.08f;
        lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
    }

    private static void CreateEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        eventSystemObject.transform.position = Vector3.zero;
    }

    private static TMP_FontAsset ResolveFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        if (font == null)
        {
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/Cinzel/static/Cinzel-Black SDF.asset");
        }

        if (font == null)
        {
            try
            {
                font = TMP_Settings.defaultFontAsset;
            }
            catch (NullReferenceException)
            {
                font = null;
            }
        }

        if (font == null)
        {
            Font osFont = Font.CreateDynamicFontFromOSFont("Arial", 90);
            if (osFont != null)
            {
                font = TMP_FontAsset.CreateFontAsset(osFont);
                if (font != null)
                {
                    const string fallbackFolder = "Assets/Generated";
                    const string fallbackPath = "Assets/Generated/MenuFallbackFont.asset";
                    if (!AssetDatabase.IsValidFolder(fallbackFolder))
                    {
                        AssetDatabase.CreateFolder("Assets", "Generated");
                    }

                    AssetDatabase.CreateAsset(font, fallbackPath);
                    AssetDatabase.SaveAssets();
                    font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fallbackPath);
                }
            }
        }

        return font;
    }

    private static Sprite ResolveBuiltinSprite(string resourcePath)
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>(resourcePath);
    }

    private static void EnsureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        AddSceneIfExists(scenes, MenuScenePath);
        AddSceneIfExists(scenes, GameplayScenePath);

        EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
        for (int i = 0; i < existing.Length; i++)
        {
            EditorBuildSettingsScene scene = existing[i];
            if (scene == null || string.IsNullOrWhiteSpace(scene.path))
            {
                continue;
            }

            if (scene.path == MenuScenePath || scene.path == GameplayScenePath)
            {
                continue;
            }

            scenes.Add(scene);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddSceneIfExists(List<EditorBuildSettingsScene> scenes, string path)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null || path == MenuScenePath)
        {
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }
    }
}
