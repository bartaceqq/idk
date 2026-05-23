using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public static class GameSettings
{
    public static class Key
    {
        public const string MoveForward = "MoveForward";
        public const string MoveBackward = "MoveBackward";
        public const string MoveLeft = "MoveLeft";
        public const string MoveRight = "MoveRight";
        public const string Jump = "Jump";
        public const string Sprint = "Sprint";
        public const string Crouch = "Crouch";
        public const string Interact = "Interact";
        public const string Attack = "Attack";
        public const string Inventory = "Inventory";
        public const string Crafting = "Crafting";
        public const string Upgrade = "Upgrade";
        public const string Emote = "Emote";
        public const string WeaponSlot1 = "WeaponSlot1";
        public const string WeaponSlot2 = "WeaponSlot2";
        public const string WeaponSlot3 = "WeaponSlot3";
        public const string WeaponSlot4 = "WeaponSlot4";
        public const string WeaponSlot5 = "WeaponSlot5";
        public const string WeaponSlot6 = "WeaponSlot6";
        public const string WeaponSlot7 = "WeaponSlot7";
        public const string WeaponSlot8 = "WeaponSlot8";
        public const string WeaponSlot9 = "WeaponSlot9";
        public const string SwordSpecial1 = "SwordSpecial1";
        public const string SwordSpecial2 = "SwordSpecial2";
        public const string SwordSpecial3 = "SwordSpecial3";
    }

    public const string Prefix = "onemorenight.settings.";

    private const string VersionKey = Prefix + "version";
    private const int Version = 1;

    private static readonly Dictionary<string, KeyCode> DefaultKeys = new Dictionary<string, KeyCode>
    {
        { Key.MoveForward, KeyCode.W },
        { Key.MoveBackward, KeyCode.S },
        { Key.MoveLeft, KeyCode.A },
        { Key.MoveRight, KeyCode.D },
        { Key.Jump, KeyCode.Space },
        { Key.Sprint, KeyCode.LeftShift },
        { Key.Crouch, KeyCode.C },
        { Key.Interact, KeyCode.E },
        { Key.Attack, KeyCode.Mouse0 },
        { Key.Inventory, KeyCode.I },
        { Key.Crafting, KeyCode.T },
        { Key.Upgrade, KeyCode.K },
        { Key.Emote, KeyCode.H },
        { Key.WeaponSlot1, KeyCode.Alpha1 },
        { Key.WeaponSlot2, KeyCode.Alpha2 },
        { Key.WeaponSlot3, KeyCode.Alpha3 },
        { Key.WeaponSlot4, KeyCode.Alpha4 },
        { Key.WeaponSlot5, KeyCode.Alpha5 },
        { Key.WeaponSlot6, KeyCode.Alpha6 },
        { Key.WeaponSlot7, KeyCode.Alpha7 },
        { Key.WeaponSlot8, KeyCode.Alpha8 },
        { Key.WeaponSlot9, KeyCode.Alpha9 },
        { Key.SwordSpecial1, KeyCode.Alpha3 },
        { Key.SwordSpecial2, KeyCode.Alpha4 },
        { Key.SwordSpecial3, KeyCode.Alpha5 }
    };

    private static readonly Dictionary<int, float> BaseAudioSourceVolumes = new Dictionary<int, float>();

    public static event Action SettingsChanged;

    public static int ResolutionWidth
    {
        get => GetInt("display.width", Screen.currentResolution.width > 0 ? Screen.currentResolution.width : 1920);
        set => SetInt("display.width", Mathf.Max(640, value));
    }

    public static int ResolutionHeight
    {
        get => GetInt("display.height", Screen.currentResolution.height > 0 ? Screen.currentResolution.height : 1080);
        set => SetInt("display.height", Mathf.Max(360, value));
    }

    public static int RefreshRate
    {
        get => GetInt("display.refreshRate", 0);
        set => SetInt("display.refreshRate", Mathf.Max(0, value));
    }

    public static FullScreenMode FullScreenMode
    {
        get => (FullScreenMode)GetInt("display.fullscreenMode", (int)UnityEngine.FullScreenMode.FullScreenWindow);
        set => SetInt("display.fullscreenMode", (int)value);
    }

    public static bool VSync
    {
        get => GetBool("display.vsync", true);
        set => SetBool("display.vsync", value);
    }

    public static float Brightness
    {
        get => GetFloat("display.brightness", 1f);
        set => SetFloat("display.brightness", Mathf.Clamp(value, 0.45f, 1.45f));
    }

    public static float UIScale
    {
        get => GetFloat("display.uiScale", 1f);
        set => SetFloat("display.uiScale", Mathf.Clamp(value, 0.75f, 1.35f));
    }

    public static int TargetFrameRate
    {
        get => GetInt("display.targetFrameRate", 0);
        set => SetInt("display.targetFrameRate", Mathf.Clamp(value, 0, 240));
    }

    public static int QualityIndex
    {
        get => GetInt("graphics.qualityIndex", QualitySettings.GetQualityLevel());
        set => SetInt("graphics.qualityIndex", Mathf.Clamp(value, 0, Mathf.Max(0, QualitySettings.names.Length - 1)));
    }

    public static float RenderScale
    {
        get => GetFloat("graphics.renderScale", 1f);
        set => SetFloat("graphics.renderScale", Mathf.Clamp(value, 0.5f, 1.5f));
    }

    public static int AntiAliasing
    {
        get => GetInt("graphics.antiAliasing", 2);
        set => SetInt("graphics.antiAliasing", ClosestAntiAliasing(value));
    }

    public static int ShadowQuality
    {
        get => GetInt("graphics.shadowQuality", 2);
        set => SetInt("graphics.shadowQuality", Mathf.Clamp(value, 0, 2));
    }

    public static float ShadowDistance
    {
        get => GetFloat("graphics.shadowDistance", 100f);
        set => SetFloat("graphics.shadowDistance", Mathf.Clamp(value, 0f, 500f));
    }

    public static int TextureQuality
    {
        get => GetInt("graphics.textureQuality", 0);
        set => SetInt("graphics.textureQuality", Mathf.Clamp(value, 0, 3));
    }

    public static bool AnisotropicFiltering
    {
        get => GetBool("graphics.anisotropicFiltering", true);
        set => SetBool("graphics.anisotropicFiltering", value);
    }

    public static float ViewDistance
    {
        get => GetFloat("graphics.viewDistance", 1f);
        set => SetFloat("graphics.viewDistance", Mathf.Clamp(value, 0.45f, 2f));
    }

    public static bool Bloom
    {
        get => GetBool("graphics.bloom", true);
        set => SetBool("graphics.bloom", value);
    }

    public static bool MotionBlur
    {
        get => GetBool("graphics.motionBlur", false);
        set => SetBool("graphics.motionBlur", value);
    }

    public static float MasterVolume
    {
        get => GetFloat("audio.master", 1f);
        set => SetFloat("audio.master", Mathf.Clamp01(value));
    }

    public static float MusicVolume
    {
        get => GetFloat("audio.music", 0.8f);
        set => SetFloat("audio.music", Mathf.Clamp01(value));
    }

    public static float SfxVolume
    {
        get => GetFloat("audio.sfx", 0.9f);
        set => SetFloat("audio.sfx", Mathf.Clamp01(value));
    }

    public static float AmbienceVolume
    {
        get => GetFloat("audio.ambience", 0.8f);
        set => SetFloat("audio.ambience", Mathf.Clamp01(value));
    }

    public static bool Muted
    {
        get => GetBool("audio.muted", false);
        set => SetBool("audio.muted", value);
    }

    public static void EnsureDefaults()
    {
        if (PlayerPrefs.GetInt(VersionKey, 0) == Version)
        {
            return;
        }

        ResolutionWidth = Screen.currentResolution.width > 0 ? Screen.currentResolution.width : 1920;
        ResolutionHeight = Screen.currentResolution.height > 0 ? Screen.currentResolution.height : 1080;
        RefreshRate = 0;
        FullScreenMode = UnityEngine.FullScreenMode.FullScreenWindow;
        VSync = true;
        Brightness = 1f;
        UIScale = 1f;
        TargetFrameRate = 0;

        QualityIndex = QualitySettings.GetQualityLevel();
        RenderScale = 1f;
        AntiAliasing = 2;
        ShadowQuality = 2;
        ShadowDistance = Mathf.Clamp(QualitySettings.shadowDistance > 0f ? QualitySettings.shadowDistance : 100f, 0f, 500f);
        TextureQuality = 0;
        AnisotropicFiltering = true;
        ViewDistance = 1f;
        Bloom = true;
        MotionBlur = false;

        MasterVolume = 1f;
        MusicVolume = 0.8f;
        SfxVolume = 0.9f;
        AmbienceVolume = 0.8f;
        Muted = false;

        foreach (KeyValuePair<string, KeyCode> binding in DefaultKeys)
        {
            SetKey(binding.Key, binding.Value, false);
        }

        PlayerPrefs.SetInt(VersionKey, Version);
        PlayerPrefs.Save();
    }

    public static void ResetToDefaults()
    {
        string[] keys =
        {
            "display.width", "display.height", "display.refreshRate", "display.fullscreenMode",
            "display.vsync", "display.brightness", "display.uiScale", "display.targetFrameRate",
            "graphics.qualityIndex", "graphics.renderScale", "graphics.antiAliasing",
            "graphics.shadowQuality", "graphics.shadowDistance", "graphics.textureQuality",
            "graphics.anisotropicFiltering", "graphics.viewDistance", "graphics.bloom",
            "graphics.motionBlur", "audio.master", "audio.music", "audio.sfx",
            "audio.ambience", "audio.muted"
        };

        for (int i = 0; i < keys.Length; i++)
        {
            PlayerPrefs.DeleteKey(Prefix + keys[i]);
        }

        foreach (string keyId in DefaultKeys.Keys)
        {
            PlayerPrefs.DeleteKey(KeyPrefKey(keyId));
        }

        PlayerPrefs.DeleteKey(VersionKey);
        EnsureDefaults();
        ApplyAllSettings();
        RaiseChanged();
    }

    public static void ApplyAllSettings()
    {
        EnsureDefaults();
        ApplyDisplaySettings();
        ApplyGraphicsSettings();
        ApplyAudioSettings();
        ApplyInputOverridesToScene();
    }

    public static void ApplyDisplaySettings()
    {
        EnsureDefaults();

        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Application.targetFrameRate = VSync || TargetFrameRate <= 0 ? -1 : TargetFrameRate;

        if (Application.isPlaying)
        {
#if UNITY_2022_2_OR_NEWER
            int requestedRefreshRate = RefreshRate;
            UnityEngine.RefreshRate refreshRate = requestedRefreshRate > 0
                ? new UnityEngine.RefreshRate { numerator = (uint)requestedRefreshRate, denominator = 1 }
                : Screen.currentResolution.refreshRateRatio;
            Screen.SetResolution(ResolutionWidth, ResolutionHeight, FullScreenMode, refreshRate);
#else
            Screen.SetResolution(ResolutionWidth, ResolutionHeight, FullScreenMode, RefreshRate);
#endif
        }
    }

    public static void ApplyGraphicsSettings()
    {
        EnsureDefaults();

        string[] qualityNames = QualitySettings.names;
        if (qualityNames != null && qualityNames.Length > 0)
        {
            int clampedQuality = Mathf.Clamp(QualityIndex, 0, qualityNames.Length - 1);
            if (QualitySettings.GetQualityLevel() != clampedQuality)
            {
                QualitySettings.SetQualityLevel(clampedQuality, true);
            }
        }

        QualitySettings.antiAliasing = AntiAliasing;
        QualitySettings.shadowDistance = ShadowDistance;
        QualitySettings.lodBias = ViewDistance;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering
            ? UnityEngine.AnisotropicFiltering.ForceEnable
            : UnityEngine.AnisotropicFiltering.Disable;
        QualitySettings.shadows = ShadowQuality switch
        {
            0 => UnityEngine.ShadowQuality.Disable,
            1 => UnityEngine.ShadowQuality.HardOnly,
            _ => UnityEngine.ShadowQuality.All
        };

        SetTextureMipmapLimit(TextureQuality);
        ApplyRenderPipelineSettings();
        ApplyCameraSettingsToScene();
        ApplyVolumeComponentState("Bloom", Bloom);
        ApplyVolumeComponentState("MotionBlur", MotionBlur);
    }

    public static void ApplyAudioSettings()
    {
        EnsureDefaults();
        AudioListener.volume = Muted ? 0f : 1f;

        AudioSource[] sources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include);
        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource source = sources[i];
            if (source == null)
            {
                continue;
            }

            int id = source.GetInstanceID();
            if (!BaseAudioSourceVolumes.TryGetValue(id, out float baseVolume))
            {
                baseVolume = source.volume;
                BaseAudioSourceVolumes[id] = baseVolume;
            }

            source.volume = Muted ? 0f : baseVolume * MasterVolume * ResolveAudioChannelVolume(source);
        }
    }

    public static void ApplyInputOverridesToScene()
    {
        UnityEngine.InputSystem.PlayerInput[] inputs = UnityEngine.Object.FindObjectsByType<UnityEngine.InputSystem.PlayerInput>(FindObjectsInactive.Include);
        for (int i = 0; i < inputs.Length; i++)
        {
            ApplyInputOverrides(inputs[i]);
        }
    }

    public static void ApplyInputOverrides(UnityEngine.InputSystem.PlayerInput playerInput)
    {
        if (playerInput == null || playerInput.actions == null)
        {
            return;
        }

        InputActionMap playerMap = playerInput.actions.FindActionMap("Player", false);
        if (playerMap == null)
        {
            return;
        }

        ApplyMoveBinding(playerMap, "up", Key.MoveForward, KeyCode.W);
        ApplyMoveBinding(playerMap, "down", Key.MoveBackward, KeyCode.S);
        ApplyMoveBinding(playerMap, "left", Key.MoveLeft, KeyCode.A);
        ApplyMoveBinding(playerMap, "right", Key.MoveRight, KeyCode.D);
        ApplyKeyboardBinding(playerMap, "Jump", Key.Jump, KeyCode.Space);
        ApplyKeyboardBinding(playerMap, "Sprint", Key.Sprint, KeyCode.LeftShift);
        ApplyKeyboardBinding(playerMap, "Crouch", Key.Crouch, KeyCode.C);
        ApplyKeyboardBinding(playerMap, "Interact", Key.Interact, KeyCode.E);
        ApplyKeyboardBinding(playerMap, "Attack", Key.Attack, KeyCode.Mouse0);
        ApplyKeyboardBinding(playerMap, "Previous", Key.WeaponSlot1, KeyCode.Alpha1);
        ApplyKeyboardBinding(playerMap, "Next", Key.WeaponSlot2, KeyCode.Alpha2);
    }

    public static KeyCode GetDefaultKey(string keyId, KeyCode fallback = KeyCode.None)
    {
        return DefaultKeys.TryGetValue(keyId, out KeyCode key) ? key : fallback;
    }

    public static KeyCode GetKey(string keyId, KeyCode fallback = KeyCode.None)
    {
        string stored = PlayerPrefs.GetString(KeyPrefKey(keyId), string.Empty);
        if (!string.IsNullOrWhiteSpace(stored) && Enum.TryParse(stored, true, out KeyCode parsed))
        {
            return parsed;
        }

        return GetDefaultKey(keyId, fallback);
    }

    public static void SetKey(string keyId, KeyCode keyCode, bool save = true)
    {
        PlayerPrefs.SetString(KeyPrefKey(keyId), keyCode.ToString());
        if (save)
        {
            PlayerPrefs.Save();
            ApplyInputOverridesToScene();
            RaiseChanged();
        }
    }

    public static bool GetKeyDown(string keyId, KeyCode fallback = KeyCode.None)
    {
        KeyCode keyCode = GetKey(keyId, fallback);
        return keyCode != KeyCode.None && Input.GetKeyDown(keyCode);
    }

    public static bool GetKeyHeld(string keyId, KeyCode fallback = KeyCode.None)
    {
        KeyCode keyCode = GetKey(keyId, fallback);
        return keyCode != KeyCode.None && Input.GetKey(keyCode);
    }

    public static bool GetMouseButtonDown(string keyId, int fallbackButton)
    {
        KeyCode keyCode = GetKey(keyId, fallbackButton == 1 ? KeyCode.Mouse1 : KeyCode.Mouse0);
        if (TryGetMouseButtonIndex(keyCode, out int buttonIndex))
        {
            return Input.GetMouseButtonDown(buttonIndex);
        }

        return Input.GetKeyDown(keyCode);
    }

    public static bool GetMouseButtonHeld(string keyId, int fallbackButton)
    {
        KeyCode keyCode = GetKey(keyId, fallbackButton == 1 ? KeyCode.Mouse1 : KeyCode.Mouse0);
        if (TryGetMouseButtonIndex(keyCode, out int buttonIndex))
        {
            return Input.GetMouseButton(buttonIndex);
        }

        return Input.GetKey(keyCode);
    }

    public static void SaveAndApply()
    {
        PlayerPrefs.SetInt(VersionKey, Version);
        PlayerPrefs.Save();
        ApplyAllSettings();
        RaiseChanged();
    }

    public static string GetKeyDisplayName(string keyId, KeyCode fallback = KeyCode.None)
    {
        return ToDisplayName(GetKey(keyId, fallback));
    }

    public static string ToDisplayName(KeyCode keyCode)
    {
        if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
        {
            return ((int)(keyCode - KeyCode.Alpha0)).ToString();
        }

        if (keyCode >= KeyCode.Keypad0 && keyCode <= KeyCode.Keypad9)
        {
            return "Num " + (int)(keyCode - KeyCode.Keypad0);
        }

        return keyCode switch
        {
            KeyCode.Mouse0 => "Mouse 1",
            KeyCode.Mouse1 => "Mouse 2",
            KeyCode.Mouse2 => "Mouse 3",
            KeyCode.LeftShift => "Left Shift",
            KeyCode.RightShift => "Right Shift",
            KeyCode.LeftControl => "Left Ctrl",
            KeyCode.RightControl => "Right Ctrl",
            KeyCode.LeftAlt => "Left Alt",
            KeyCode.RightAlt => "Right Alt",
            KeyCode.Return => "Enter",
            KeyCode.Escape => "Esc",
            KeyCode.Space => "Space",
            KeyCode.None => "Unbound",
            _ => NicifyKeyName(keyCode.ToString())
        };
    }

    public static string[] QualityNames()
    {
        string[] names = QualitySettings.names;
        return names == null || names.Length == 0 ? new[] { "Default" } : names;
    }

    public static ResolutionChoice[] GetResolutionChoices()
    {
        List<ResolutionChoice> choices = new List<ResolutionChoice>();
        Resolution[] resolutions = Screen.resolutions;
        if (resolutions != null && resolutions.Length > 0)
        {
            for (int i = 0; i < resolutions.Length; i++)
            {
                Resolution resolution = resolutions[i];
                if (resolution.width < 640 || resolution.height < 360)
                {
                    continue;
                }

                ResolutionChoice choice = new ResolutionChoice(
                    resolution.width,
                    resolution.height,
                    RefreshRateValue(resolution));

                if (!choices.Contains(choice))
                {
                    choices.Add(choice);
                }
            }
        }

        if (choices.Count == 0)
        {
            choices.Add(new ResolutionChoice(1280, 720, 0));
            choices.Add(new ResolutionChoice(1600, 900, 0));
            choices.Add(new ResolutionChoice(1920, 1080, 0));
            choices.Add(new ResolutionChoice(2560, 1440, 0));
            choices.Add(new ResolutionChoice(3840, 2160, 0));
        }

        choices.Sort((a, b) =>
        {
            int areaCompare = (a.Width * a.Height).CompareTo(b.Width * b.Height);
            return areaCompare != 0 ? areaCompare : a.RefreshRate.CompareTo(b.RefreshRate);
        });

        return choices.ToArray();
    }

    public static void NotifyChanged()
    {
        RaiseChanged();
    }

    private static int GetInt(string key, int fallback)
    {
        return PlayerPrefs.GetInt(Prefix + key, fallback);
    }

    private static void SetInt(string key, int value)
    {
        PlayerPrefs.SetInt(Prefix + key, value);
    }

    private static float GetFloat(string key, float fallback)
    {
        return PlayerPrefs.GetFloat(Prefix + key, fallback);
    }

    private static void SetFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(Prefix + key, value);
    }

    private static bool GetBool(string key, bool fallback)
    {
        return PlayerPrefs.GetInt(Prefix + key, fallback ? 1 : 0) != 0;
    }

    private static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(Prefix + key, value ? 1 : 0);
    }

    private static string KeyPrefKey(string keyId)
    {
        return Prefix + "key." + keyId;
    }

    private static int ClosestAntiAliasing(int requested)
    {
        if (requested <= 0)
        {
            return 0;
        }

        if (requested <= 2)
        {
            return 2;
        }

        if (requested <= 4)
        {
            return 4;
        }

        return 8;
    }

    private static void ApplyMoveBinding(InputActionMap playerMap, string partName, string keyId, KeyCode fallback)
    {
        InputAction action = playerMap.FindAction("Move", false);
        if (action == null)
        {
            return;
        }

        KeyCode keyCode = GetKey(keyId, fallback);
        string path = ToKeyboardPath(keyCode);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (!binding.isPartOfComposite)
            {
                continue;
            }

            if (!string.Equals(binding.name, partName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!ContainsGroup(binding.groups, "Keyboard&Mouse"))
            {
                continue;
            }

            action.ApplyBindingOverride(i, new InputBinding { overridePath = path });
            return;
        }
    }

    private static void ApplyKeyboardBinding(InputActionMap playerMap, string actionName, string keyId, KeyCode fallback)
    {
        InputAction action = playerMap.FindAction(actionName, false);
        if (action == null)
        {
            return;
        }

        KeyCode keyCode = GetKey(keyId, fallback);
        string path = ToInputSystemPath(keyCode);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (binding.isComposite || binding.isPartOfComposite)
            {
                continue;
            }

            if (!ContainsGroup(binding.groups, "Keyboard&Mouse"))
            {
                continue;
            }

            bool bindingIsMouse = binding.path != null && binding.path.IndexOf("<Mouse>", StringComparison.OrdinalIgnoreCase) >= 0;
            bool pathIsMouse = path.IndexOf("<Mouse>", StringComparison.OrdinalIgnoreCase) >= 0;
            if (bindingIsMouse != pathIsMouse && HasMatchingDeviceBinding(action, pathIsMouse))
            {
                continue;
            }

            action.ApplyBindingOverride(i, new InputBinding { overridePath = path });
            return;
        }
    }

    private static bool HasMatchingDeviceBinding(InputAction action, bool wantsMouse)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (!ContainsGroup(binding.groups, "Keyboard&Mouse"))
            {
                continue;
            }

            bool isMouse = binding.path != null && binding.path.IndexOf("<Mouse>", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isMouse == wantsMouse)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsGroup(string groups, string group)
    {
        return string.IsNullOrEmpty(groups) || groups.IndexOf(group, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string ToInputSystemPath(KeyCode keyCode)
    {
        if (TryGetMouseButtonIndex(keyCode, out int mouseButton))
        {
            return mouseButton switch
            {
                0 => "<Mouse>/leftButton",
                1 => "<Mouse>/rightButton",
                2 => "<Mouse>/middleButton",
                _ => string.Empty
            };
        }

        return ToKeyboardPath(keyCode);
    }

    private static string ToKeyboardPath(KeyCode keyCode)
    {
        if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
        {
            return "<Keyboard>/" + keyCode.ToString().ToLowerInvariant();
        }

        if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
        {
            return "<Keyboard>/" + (int)(keyCode - KeyCode.Alpha0);
        }

        if (keyCode >= KeyCode.Keypad0 && keyCode <= KeyCode.Keypad9)
        {
            return "<Keyboard>/numpad" + (int)(keyCode - KeyCode.Keypad0);
        }

        return keyCode switch
        {
            KeyCode.Space => "<Keyboard>/space",
            KeyCode.Return => "<Keyboard>/enter",
            KeyCode.KeypadEnter => "<Keyboard>/numpadEnter",
            KeyCode.Tab => "<Keyboard>/tab",
            KeyCode.Escape => "<Keyboard>/escape",
            KeyCode.Backspace => "<Keyboard>/backspace",
            KeyCode.LeftShift => "<Keyboard>/leftShift",
            KeyCode.RightShift => "<Keyboard>/rightShift",
            KeyCode.LeftControl => "<Keyboard>/leftCtrl",
            KeyCode.RightControl => "<Keyboard>/rightCtrl",
            KeyCode.LeftAlt => "<Keyboard>/leftAlt",
            KeyCode.RightAlt => "<Keyboard>/rightAlt",
            KeyCode.UpArrow => "<Keyboard>/upArrow",
            KeyCode.DownArrow => "<Keyboard>/downArrow",
            KeyCode.LeftArrow => "<Keyboard>/leftArrow",
            KeyCode.RightArrow => "<Keyboard>/rightArrow",
            KeyCode.Minus => "<Keyboard>/minus",
            KeyCode.Equals => "<Keyboard>/equals",
            KeyCode.LeftBracket => "<Keyboard>/leftBracket",
            KeyCode.RightBracket => "<Keyboard>/rightBracket",
            KeyCode.Semicolon => "<Keyboard>/semicolon",
            KeyCode.Quote => "<Keyboard>/quote",
            KeyCode.Comma => "<Keyboard>/comma",
            KeyCode.Period => "<Keyboard>/period",
            KeyCode.Slash => "<Keyboard>/slash",
            KeyCode.Backslash => "<Keyboard>/backslash",
            KeyCode.BackQuote => "<Keyboard>/backquote",
            _ => string.Empty
        };
    }

    private static bool TryGetMouseButtonIndex(KeyCode keyCode, out int buttonIndex)
    {
        switch (keyCode)
        {
            case KeyCode.Mouse0:
                buttonIndex = 0;
                return true;
            case KeyCode.Mouse1:
                buttonIndex = 1;
                return true;
            case KeyCode.Mouse2:
                buttonIndex = 2;
                return true;
            default:
                buttonIndex = -1;
                return false;
        }
    }

    private static void SetTextureMipmapLimit(int limit)
    {
        PropertyInfo globalLimit = typeof(QualitySettings).GetProperty("globalTextureMipmapLimit", BindingFlags.Public | BindingFlags.Static);
        if (globalLimit != null && globalLimit.CanWrite)
        {
            globalLimit.SetValue(null, limit, null);
            return;
        }

        PropertyInfo masterLimit = typeof(QualitySettings).GetProperty("masterTextureLimit", BindingFlags.Public | BindingFlags.Static);
        if (masterLimit != null && masterLimit.CanWrite)
        {
            masterLimit.SetValue(null, limit, null);
        }
    }

    private static void ApplyRenderPipelineSettings()
    {
        RenderPipelineAsset pipelineAsset = GraphicsSettings.currentRenderPipeline;
        if (pipelineAsset == null)
        {
            return;
        }

        SetPropertyIfPresent(pipelineAsset, "renderScale", RenderScale);
        SetPropertyIfPresent(pipelineAsset, "msaaSampleCount", AntiAliasing);
        SetPropertyIfPresent(pipelineAsset, "shadowDistance", ShadowDistance);
    }

    private static void SetPropertyIfPresent(object target, string propertyName, object value)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property == null || !property.CanWrite)
        {
            return;
        }

        try
        {
            property.SetValue(target, value, null);
        }
        catch (Exception)
        {
            // Some render pipeline properties are editor-only or validated internally.
        }
    }

    private static void ApplyCameraSettingsToScene()
    {
        Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
        float farClip = Mathf.Lerp(180f, 900f, Mathf.InverseLerp(0.45f, 2f, ViewDistance));
        for (int i = 0; i < cameras.Length; i++)
        {
            Camera camera = cameras[i];
            if (camera != null && camera.farClipPlane > 10f)
            {
                camera.farClipPlane = Mathf.Max(camera.nearClipPlane + 10f, farClip);
            }
        }
    }

    private static void ApplyVolumeComponentState(string componentName, bool enabled)
    {
        Volume[] volumes = UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include);
        for (int i = 0; i < volumes.Length; i++)
        {
            Volume volume = volumes[i];
            if (volume == null || volume.profile == null)
            {
                continue;
            }

            List<VolumeComponent> components = volume.profile.components;
            if (components == null)
            {
                continue;
            }

            for (int c = 0; c < components.Count; c++)
            {
                VolumeComponent component = components[c];
                if (component == null || component.GetType().Name != componentName)
                {
                    continue;
                }

                component.active = enabled;
            }
        }
    }

    private static int RefreshRateValue(Resolution resolution)
    {
#if UNITY_2022_2_OR_NEWER
        return Mathf.RoundToInt((float)resolution.refreshRateRatio.value);
#else
        return resolution.refreshRate;
#endif
    }

    private static void RaiseChanged()
    {
        SettingsChanged?.Invoke();
    }

    private static float ResolveAudioChannelVolume(AudioSource source)
    {
        string name = source.name;
        if (source.transform.parent != null)
        {
            name += " " + source.transform.parent.name;
        }

        name = name.ToLowerInvariant();
        if (name.Contains("music") || name.Contains("theme") || name.Contains("song"))
        {
            return MusicVolume;
        }

        if (name.Contains("ambient") || name.Contains("ambience") || name.Contains("rain") || name.Contains("wind"))
        {
            return AmbienceVolume;
        }

        return SfxVolume;
    }

    private static string NicifyKeyName(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        List<char> chars = new List<char>(raw.Length + 8);
        for (int i = 0; i < raw.Length; i++)
        {
            char current = raw[i];
            if (i > 0 && char.IsUpper(current) && !char.IsUpper(raw[i - 1]))
            {
                chars.Add(' ');
            }

            chars.Add(current);
        }

        return new string(chars.ToArray());
    }

    [Serializable]
    public struct ResolutionChoice : IEquatable<ResolutionChoice>
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int RefreshRate;

        public ResolutionChoice(int width, int height, int refreshRate)
        {
            Width = width;
            Height = height;
            RefreshRate = refreshRate;
        }

        public bool Equals(ResolutionChoice other)
        {
            return Width == other.Width && Height == other.Height && RefreshRate == other.RefreshRate;
        }

        public override bool Equals(object obj)
        {
            return obj is ResolutionChoice other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Width;
                hashCode = (hashCode * 397) ^ Height;
                hashCode = (hashCode * 397) ^ RefreshRate;
                return hashCode;
            }
        }

        public override string ToString()
        {
            string aspect = GetAspectLabel(Width, Height);
            return RefreshRate > 0
                ? $"{Width} x {Height} ({aspect}) {RefreshRate}Hz"
                : $"{Width} x {Height} ({aspect})";
        }

        private static string GetAspectLabel(int width, int height)
        {
            int divisor = GreatestCommonDivisor(width, height);
            return $"{width / divisor}:{height / divisor}";
        }

        private static int GreatestCommonDivisor(int a, int b)
        {
            a = Mathf.Abs(a);
            b = Mathf.Abs(b);
            while (b != 0)
            {
                int remainder = a % b;
                a = b;
                b = remainder;
            }

            return Mathf.Max(1, a);
        }
    }
}
