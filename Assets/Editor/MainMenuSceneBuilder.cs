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
        public Image DisplayTabImage;
        public Image KeybindTabImage;
        public Image AudioTabImage;
        public Image GraphicsTabImage;
        public GameObject DisplayTabContent;
        public GameObject KeybindTabContent;
        public GameObject AudioTabContent;
        public GameObject GraphicsTabContent;
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
    }

    private sealed class CreditsMenuRefs
    {
        public Button BackButton;
    }

    [MenuItem("Tools/One More Night/Rebuild Main Menu Scene")]
    public static void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateWorldCamera();
        CreateWorldLight();
        CreateEventSystem();

        TMP_FontAsset font = ResolveFont();
        Sprite uiSprite = ResolveBuiltinSprite("UI/Skin/UISprite.psd");
        Sprite knobSprite = ResolveBuiltinSprite("UI/Skin/Knob.psd");

        if (uiSprite == null)
        {
            Debug.LogError("Could not load built-in UI sprite. Aborting menu build.");
            return;
        }

        if (knobSprite == null)
        {
            knobSprite = uiSprite;
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
        SettingsMenuRefs settingsRefs = BuildSettingsScreen(settingsScreen, font, uiSprite, knobSprite);
        CreditsMenuRefs creditsRefs = BuildCreditsScreen(creditsScreen, font, uiSprite);

        settingsScreen.gameObject.SetActive(false);
        creditsScreen.gameObject.SetActive(false);

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
        Sprite knobSprite)
    {
        SettingsMenuRefs refs = new SettingsMenuRefs();

        Image titlePlate = CreateImage("Settings Title Plate", parent, uiSprite, new Color(0.19f, 0.12f, 0.08f, 0.72f), false);
        titlePlate.type = Image.Type.Sliced;
        SetTopRect(titlePlate.rectTransform, 6f, -6f, 120f);

        TMP_Text title = CreateText(
            "Settings Title",
            parent,
            "SETTINGS",
            font,
            56f,
            TextAlignmentOptions.Center,
            FontStyles.Bold,
            TextPrimary);
        SetTopRect(title.rectTransform, 14f, -16f, 84f);
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.enableAutoSizing = true;
        title.fontSizeMin = 34f;
        title.fontSizeMax = 62f;

        RectTransform tabsRow = CreateRect("Settings Tabs", parent);
        tabsRow.anchorMin = new Vector2(0f, 1f);
        tabsRow.anchorMax = new Vector2(1f, 1f);
        tabsRow.pivot = new Vector2(0.5f, 1f);
        tabsRow.offsetMin = new Vector2(0f, -176f);
        tabsRow.offsetMax = new Vector2(0f, -104f);

        HorizontalLayoutGroup tabsLayout = tabsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 10f;
        tabsLayout.padding = new RectOffset(0, 0, 0, 0);
        tabsLayout.childControlHeight = true;
        tabsLayout.childControlWidth = true;
        tabsLayout.childForceExpandHeight = false;
        tabsLayout.childForceExpandWidth = true;
        tabsLayout.childAlignment = TextAnchor.MiddleCenter;

        refs.DisplayTabButton = CreateFlatButton(tabsRow, "Display Tab", "Display", font, true, 58f, 0f);
        refs.KeybindTabButton = CreateFlatButton(tabsRow, "Keybind Tab", "Keybind", font, false, 58f, 0f);
        refs.AudioTabButton = CreateFlatButton(tabsRow, "Audio Tab", "Audio", font, false, 58f, 0f);
        refs.GraphicsTabButton = CreateFlatButton(tabsRow, "Graphics Tab", "Graphics", font, false, 58f, 0f);

        refs.DisplayTabImage = refs.DisplayTabButton.image;
        refs.KeybindTabImage = refs.KeybindTabButton.image;
        refs.AudioTabImage = refs.AudioTabButton.image;
        refs.GraphicsTabImage = refs.GraphicsTabButton.image;

        Image contentPanel = CreateImage("Settings Content Panel", parent, uiSprite, CardColor, false);
        contentPanel.type = Image.Type.Sliced;
        Outline panelOutline = contentPanel.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.27f, 0.18f, 0.10f, 0.55f);
        panelOutline.effectDistance = new Vector2(1f, -1f);
        RectTransform contentPanelRect = contentPanel.rectTransform;
        contentPanelRect.anchorMin = new Vector2(0f, 0f);
        contentPanelRect.anchorMax = new Vector2(1f, 1f);
        contentPanelRect.offsetMin = new Vector2(0f, 152f);
        contentPanelRect.offsetMax = new Vector2(0f, -174f);

        RectTransform tabContentRoot = CreateRect("Tab Content Root", contentPanelRect);
        StretchRect(tabContentRoot, new Vector2(16f, 16f), new Vector2(-16f, -16f));

        RectTransform displayContent = CreateRect("Display Tab Content", tabContentRoot);
        StretchRect(displayContent);

        RectTransform keybindContent = CreateRect("Keybind Tab Content", tabContentRoot);
        StretchRect(keybindContent);

        RectTransform audioContent = CreateRect("Audio Tab Content", tabContentRoot);
        StretchRect(audioContent);

        RectTransform graphicsContent = CreateRect("Graphics Tab Content", tabContentRoot);
        StretchRect(graphicsContent);

        BuildDisplayTabContent(displayContent, font, uiSprite, knobSprite, refs);
        BuildPlaceholderTab(keybindContent, font, "Bindings board");
        BuildPlaceholderTab(audioContent, font, "Sound board");
        BuildPlaceholderTab(graphicsContent, font, "Visual board");

        refs.DisplayTabContent = displayContent.gameObject;
        refs.KeybindTabContent = keybindContent.gameObject;
        refs.AudioTabContent = audioContent.gameObject;
        refs.GraphicsTabContent = graphicsContent.gameObject;

        TMP_Text status = CreateText(
            "Settings Status Text",
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
        status.rectTransform.offsetMin = new Vector2(12f, 94f);
        status.rectTransform.offsetMax = new Vector2(-12f, 136f);
        refs.StatusText = status;

        RectTransform footerRow = CreateRect("Settings Footer Row", parent);
        footerRow.anchorMin = new Vector2(0f, 0f);
        footerRow.anchorMax = new Vector2(1f, 0f);
        footerRow.pivot = new Vector2(0.5f, 0f);
        footerRow.offsetMin = new Vector2(0f, 18f);
        footerRow.offsetMax = new Vector2(0f, 86f);

        HorizontalLayoutGroup footerLayout = footerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerLayout.spacing = 12f;
        footerLayout.padding = new RectOffset(0, 0, 0, 0);
        footerLayout.childControlHeight = true;
        footerLayout.childControlWidth = false;
        footerLayout.childForceExpandHeight = false;
        footerLayout.childForceExpandWidth = false;
        footerLayout.childAlignment = TextAnchor.MiddleCenter;

        refs.BackButton = CreateFlatButton(footerRow, "Settings Back Button", "Back", font, false, 56f, 180f);
        refs.ApplyButton = CreateFlatButton(footerRow, "Settings Apply Button", "Apply", font, true, 56f, 180f);

        return refs;
    }

    private static void BuildDisplayTabContent(
        RectTransform parent,
        TMP_FontAsset font,
        Sprite uiSprite,
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
        layout.childAlignment = TextAnchor.UpperCenter;

        RectTransform resolutionRow = CreateSettingsRow(parent, font, uiSprite, "Resolution");
        RectTransform resolutionControl = CreateRowControlContainer(resolutionRow);
        refs.ResolutionPreviousButton = CreateSmallIconButton(resolutionControl, "Resolution Previous", "<", font);

        Image resolutionValuePanel = CreateImage("Resolution Value Panel", resolutionControl, uiSprite, ValuePanelColor, true);
        RectTransform resolutionValueRect = resolutionValuePanel.rectTransform;
        LayoutElement resolutionValueLayout = resolutionValuePanel.gameObject.AddComponent<LayoutElement>();
        resolutionValueLayout.preferredHeight = 40f;
        resolutionValueLayout.minHeight = 40f;
        resolutionValueLayout.minWidth = 280f;
        resolutionValueLayout.preferredWidth = 340f;
        resolutionValueLayout.flexibleWidth = 1f;
        refs.ResolutionValueText = CreateText(
            "Resolution Value Text",
            resolutionValueRect,
            "1920 x 1080",
            font,
            28f,
            TextAlignmentOptions.Center,
            FontStyles.Normal,
            TextPrimary);
        StretchRect(refs.ResolutionValueText.rectTransform);
        refs.ResolutionValueText.textWrappingMode = TextWrappingModes.NoWrap;
        refs.ResolutionValueText.overflowMode = TextOverflowModes.Ellipsis;
        refs.ResolutionValueText.enableAutoSizing = true;
        refs.ResolutionValueText.fontSizeMin = 18f;
        refs.ResolutionValueText.fontSizeMax = 30f;

        refs.ResolutionNextButton = CreateSmallIconButton(resolutionControl, "Resolution Next", ">", font);

        RectTransform fullscreenRow = CreateSettingsRow(parent, font, uiSprite, "Fullscreen");
        RectTransform fullscreenControl = CreateRowControlContainer(fullscreenRow);
        refs.FullscreenToggle = CreateFlatToggle(fullscreenControl, "Fullscreen Toggle", uiSprite, true);

        RectTransform vsyncRow = CreateSettingsRow(parent, font, uiSprite, "VSync");
        RectTransform vsyncControl = CreateRowControlContainer(vsyncRow);
        refs.VSyncToggle = CreateFlatToggle(vsyncControl, "VSync Toggle", uiSprite, true);

        RectTransform brightnessRow = CreateSettingsRow(parent, font, uiSprite, "Brightness");
        RectTransform brightnessControl = CreateRowControlContainer(brightnessRow);
        refs.BrightnessSlider = CreateFlatSlider(brightnessControl, "Brightness Slider", uiSprite, knobSprite);

        Image brightnessValuePanel = CreateImage("Brightness Value Panel", brightnessControl, uiSprite, ValuePanelColor, true);
        RectTransform brightnessValueRect = brightnessValuePanel.rectTransform;
        LayoutElement brightnessValueLayout = brightnessValuePanel.gameObject.AddComponent<LayoutElement>();
        brightnessValueLayout.preferredHeight = 40f;
        brightnessValueLayout.minHeight = 40f;
        brightnessValueLayout.preferredWidth = 84f;
        brightnessValueLayout.minWidth = 84f;
        refs.BrightnessValueText = CreateText(
            "Brightness Value",
            brightnessValueRect,
            "100%",
            font,
            24f,
            TextAlignmentOptions.Center,
            FontStyles.Normal,
            TextPrimary);
        StretchRect(refs.BrightnessValueText.rectTransform);
        refs.BrightnessValueText.textWrappingMode = TextWrappingModes.NoWrap;
        refs.BrightnessValueText.enableAutoSizing = true;
        refs.BrightnessValueText.fontSizeMin = 16f;
        refs.BrightnessValueText.fontSizeMax = 26f;

        RectTransform uiScaleRow = CreateSettingsRow(parent, font, uiSprite, "UI Scale");
        RectTransform uiScaleControl = CreateRowControlContainer(uiScaleRow);
        refs.UiScaleSlider = CreateFlatSlider(uiScaleControl, "UI Scale Slider", uiSprite, knobSprite);

        Image uiScaleValuePanel = CreateImage("UI Scale Value Panel", uiScaleControl, uiSprite, ValuePanelColor, true);
        RectTransform uiScaleValueRect = uiScaleValuePanel.rectTransform;
        LayoutElement uiScaleValueLayout = uiScaleValuePanel.gameObject.AddComponent<LayoutElement>();
        uiScaleValueLayout.preferredHeight = 40f;
        uiScaleValueLayout.minHeight = 40f;
        uiScaleValueLayout.preferredWidth = 84f;
        uiScaleValueLayout.minWidth = 84f;
        refs.UiScaleValueText = CreateText(
            "UI Scale Value",
            uiScaleValueRect,
            "100%",
            font,
            24f,
            TextAlignmentOptions.Center,
            FontStyles.Normal,
            TextPrimary);
        StretchRect(refs.UiScaleValueText.rectTransform);
        refs.UiScaleValueText.textWrappingMode = TextWrappingModes.NoWrap;
        refs.UiScaleValueText.enableAutoSizing = true;
        refs.UiScaleValueText.fontSizeMin = 16f;
        refs.UiScaleValueText.fontSizeMax = 26f;
    }

    private static RectTransform CreateSettingsRow(RectTransform parent, TMP_FontAsset font, Sprite uiSprite, string label)
    {
        RectTransform row = CreateRect($"{label} Row", parent);
        Image rowImage = row.gameObject.AddComponent<Image>();
        rowImage.sprite = uiSprite;
        rowImage.type = Image.Type.Sliced;
        rowImage.color = RowColor;
        rowImage.raycastTarget = false;

        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 64f;
        rowLayout.minHeight = 64f;

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.padding = new RectOffset(16, 16, 10, 10);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        RectTransform labelRect = CreateRect("Label", row);
        LayoutElement labelLayout = labelRect.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 230f;
        labelLayout.minWidth = 230f;

        TMP_Text labelText = CreateText(
            "Label Text",
            labelRect,
            label,
            font,
            27f,
            TextAlignmentOptions.MidlineLeft,
            FontStyles.Bold,
            TextPrimary);
        StretchRect(labelText.rectTransform);
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
        labelText.overflowMode = TextOverflowModes.Ellipsis;
        labelText.enableAutoSizing = true;
        labelText.fontSizeMin = 18f;
        labelText.fontSizeMax = 28f;

        return row;
    }

    private static RectTransform CreateRowControlContainer(RectTransform row)
    {
        RectTransform controls = CreateRect("Controls", row);
        LayoutElement controlLayout = controls.gameObject.AddComponent<LayoutElement>();
        controlLayout.flexibleWidth = 1f;

        HorizontalLayoutGroup controlsLayout = controls.gameObject.AddComponent<HorizontalLayoutGroup>();
        controlsLayout.spacing = 12f;
        controlsLayout.padding = new RectOffset(0, 0, 0, 0);
        controlsLayout.childAlignment = TextAnchor.MiddleCenter;
        controlsLayout.childControlHeight = true;
        controlsLayout.childControlWidth = false;
        controlsLayout.childForceExpandHeight = false;
        controlsLayout.childForceExpandWidth = false;

        return controls;
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
        Button button = CreateFlatButton(parent, name, label, font, false, 40f, 46f, false);
        TMP_Text labelText = button.GetComponentInChildren<TMP_Text>(true);
        if (labelText != null)
        {
            labelText.fontSize = 25f;
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
        layout.preferredWidth = 34f;
        layout.minWidth = 34f;
        layout.preferredHeight = 34f;
        layout.minHeight = 34f;

        Image boxImage = toggleObject.GetComponent<Image>();
        boxImage.sprite = uiSprite;
        boxImage.type = Image.Type.Sliced;
        boxImage.color = ValuePanelColor;
        boxImage.raycastTarget = true;

        RectTransform checkRect = CreateRect("Checkmark", rect);
        StretchRect(checkRect, new Vector2(6f, 6f), new Vector2(-6f, -6f));
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
        layout.preferredHeight = 40f;
        layout.minHeight = 40f;
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
        trackRect.offsetMin = new Vector2(0f, -6f);
        trackRect.offsetMax = new Vector2(0f, 6f);
        Image trackImage = trackRect.gameObject.AddComponent<Image>();
        trackImage.sprite = uiSprite;
        trackImage.type = Image.Type.Sliced;
        trackImage.color = SliderTrackColor;
        trackImage.raycastTarget = true;

        RectTransform fillArea = CreateRect("Fill Area", rect);
        StretchRect(fillArea, new Vector2(8f, 14f), new Vector2(-8f, -14f));

        RectTransform fillRect = CreateRect("Fill", fillArea);
        StretchRect(fillRect);
        Image fillImage = fillRect.gameObject.AddComponent<Image>();
        fillImage.sprite = uiSprite;
        fillImage.type = Image.Type.Sliced;
        fillImage.color = AccentColor;
        fillImage.raycastTarget = false;

        RectTransform handleSlideArea = CreateRect("Handle Slide Area", rect);
        StretchRect(handleSlideArea, new Vector2(8f, 4f), new Vector2(-8f, -4f));

        RectTransform handleRect = CreateRect("Handle", handleSlideArea);
        handleRect.anchorMin = new Vector2(0f, 0.5f);
        handleRect.anchorMax = new Vector2(0f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(24f, 24f);
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
        SetObject(serializedController, "displayTabImage", settingsRefs.DisplayTabImage);
        SetObject(serializedController, "keybindTabImage", settingsRefs.KeybindTabImage);
        SetObject(serializedController, "audioTabImage", settingsRefs.AudioTabImage);
        SetObject(serializedController, "graphicsTabImage", settingsRefs.GraphicsTabImage);
        SetObject(serializedController, "tabNormalSprite", tabNormalSprite);
        SetObject(serializedController, "tabActiveSprite", tabActiveSprite);
        SetColor(serializedController, "tabNormalColor", NeutralButtonColor);
        SetColor(serializedController, "tabActiveColor", AccentColor);
        SetObject(serializedController, "displayTabContent", settingsRefs.DisplayTabContent);
        SetObject(serializedController, "keybindTabContent", settingsRefs.KeybindTabContent);
        SetObject(serializedController, "audioTabContent", settingsRefs.AudioTabContent);
        SetObject(serializedController, "graphicsTabContent", settingsRefs.GraphicsTabContent);
        SetObject(serializedController, "settingsStatusText", settingsRefs.StatusText);

        SetObject(serializedController, "creditsBackButton", creditsRefs.BackButton);

        SetObject(serializedController, "resolutionPreviousButton", settingsRefs.ResolutionPreviousButton);
        SetObject(serializedController, "resolutionNextButton", settingsRefs.ResolutionNextButton);
        SetObject(serializedController, "resolutionValueText", settingsRefs.ResolutionValueText);
        SetObject(serializedController, "fullscreenToggle", settingsRefs.FullscreenToggle);
        SetObject(serializedController, "vSyncToggle", settingsRefs.VSyncToggle);
        SetObject(serializedController, "brightnessSlider", settingsRefs.BrightnessSlider);
        SetObject(serializedController, "brightnessValueText", settingsRefs.BrightnessValueText);
        SetObject(serializedController, "uiScaleSlider", settingsRefs.UiScaleSlider);
        SetObject(serializedController, "uiScaleValueText", settingsRefs.UiScaleValueText);

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
