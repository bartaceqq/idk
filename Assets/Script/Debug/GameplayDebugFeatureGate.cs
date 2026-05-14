using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameplayDebugFeatureGate : MonoBehaviour
{
    public enum FeaturePreset
    {
        WalkingOnly,
        BuildController,
        InventoryCrafting,
        Interactions,
        WorldRuntime,
        EnemiesAnimals,
        Everything
    }

    private const FeaturePreset DefaultPreset = FeaturePreset.WalkingOnly;
    private const string PlayerPrefsPresetKey = "idk.debug.featurePreset";
    private const string EnvironmentPresetKey = "IDK_DEBUG_FEATURE_PRESET";
    private const string CommandLinePresetArg = "-idkFeaturePreset";

    private static readonly HashSet<string> AlwaysAllowedBehaviours = new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(GameplayDebugFeatureGate),
        nameof(FPSControllerTest),
        "PlayerInput",
        "InputActionManager"
    };

    private static readonly HashSet<string> BuildControllerBehaviours = new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(LookingController),
        nameof(RayCastScriptTest),
        nameof(SnapPoint),
        nameof(WallSnapPoints),
        nameof(FloorScript),
        nameof(StairScript),
        nameof(RuntimeBuildPiece),
        nameof(BuildPreviewMarker)
    };

    private static readonly HashSet<string> InventoryCraftingBehaviours = new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(InventoryController),
        nameof(InventoryManager),
        nameof(InventoryItem),
        nameof(InventoryAddHandler),
        nameof(InventoryListHandler),
        nameof(Slot),
        nameof(SlotManager),
        nameof(SlotInsideUI),
        nameof(WeaponSlot),
        nameof(CraftingManager),
        nameof(CraftingProcessHandler),
        nameof(CraftingStation),
        nameof(CraftableSlot),
        nameof(CraftableItem),
        "Item"
    };

    private static readonly HashSet<string> InteractionBehaviours = new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(RayScript),
        nameof(ActionScript),
        nameof(ItemSwitchScript),
        nameof(TestHitting),
        nameof(Sword),
        nameof(SwordTrailEffect),
        nameof(ProjectileScript),
        "XPTest",
        "TreeTest"
    };

    private static readonly HashSet<string> WorldRuntimeBehaviours = new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(ResHandler),
        nameof(TerrainTreeToPrefabConverter),
        nameof(TerrainTreeProxySpawner),
        nameof(TreeHandler),
        nameof(CutTree),
        nameof(MineStone),
        nameof(StoneColliderScript),
        nameof(DetailPickupMarker),
        nameof(ChestChecker),
        nameof(ChestController),
        nameof(ChestItemGenerator),
        nameof(ItemForChestsHandler),
        "ColliderScript"
    };

    private static readonly HashSet<string> EnemiesAnimalsBehaviours = new HashSet<string>(StringComparer.Ordinal)
    {
        nameof(RandomZombieScript),
        nameof(RandomSkeletonScript),
        nameof(NPCHealthScript),
        nameof(NPCDemageScript),
        nameof(Animalec),
        nameof(NPCText),
        nameof(StartCommunication),
        nameof(VisualCommunication)
    };

    private readonly List<Behaviour> disabledBehaviours = new List<Behaviour>(512);
    private FeaturePreset activePreset;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallAfterSceneLoad()
    {
        GameplayDebugFeatureGate existing = FindFirstObjectByType<GameplayDebugFeatureGate>();
        if (existing != null)
        {
            existing.ApplyResolvedPreset();
            return;
        }

        GameObject gateObject = new GameObject(nameof(GameplayDebugFeatureGate));
        DontDestroyOnLoad(gateObject);
        gateObject.AddComponent<GameplayDebugFeatureGate>();
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplyResolvedPreset();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyResolvedPreset();
    }

    private void ApplyResolvedPreset()
    {
        activePreset = ResolvePreset();
        ApplyPreset(activePreset);
    }

    private void ApplyPreset(FeaturePreset preset)
    {
        ForceNormalWalkingCapsule();
        RestoreAllowedBehaviours(preset);

        if (preset == FeaturePreset.Everything)
        {
            Debug.Log("GameplayDebugFeatureGate: preset Everything, no systems disabled.");
            return;
        }

        DisableBlockedBehaviours(preset);
        DisableBlockedAnimators(preset);
        StopBlockedParticles(preset);
        Debug.Log($"GameplayDebugFeatureGate active preset: {preset}. Use {CommandLinePresetArg} <PresetName> to test another group.");
    }

    private static FeaturePreset ResolvePreset()
    {
        if (TryParsePreset(GetCommandLinePreset(), out FeaturePreset commandLinePreset))
        {
            return commandLinePreset;
        }

        if (TryParsePreset(Environment.GetEnvironmentVariable(EnvironmentPresetKey), out FeaturePreset environmentPreset))
        {
            return environmentPreset;
        }

        if (PlayerPrefs.HasKey(PlayerPrefsPresetKey) &&
            TryParsePreset(PlayerPrefs.GetString(PlayerPrefsPresetKey), out FeaturePreset savedPreset))
        {
            return savedPreset;
        }

        return DefaultPreset;
    }

    private static string GetCommandLinePreset()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], CommandLinePresetArg, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static bool TryParsePreset(string rawPreset, out FeaturePreset preset)
    {
        if (!string.IsNullOrWhiteSpace(rawPreset) &&
            Enum.TryParse(rawPreset.Trim(), true, out preset))
        {
            return true;
        }

        preset = DefaultPreset;
        return false;
    }

    private void ForceNormalWalkingCapsule()
    {
        LookingController[] controllers = FindObjectsByType<LookingController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < controllers.Length; i++)
        {
            LookingController controller = controllers[i];
            if (controller == null || controller.normalcapsule == null || controller.buildingcapsule == null)
            {
                continue;
            }

            if (controller.buildingcapsule.activeInHierarchy)
            {
                controller.normalcapsule.transform.SetPositionAndRotation(
                    controller.buildingcapsule.transform.position,
                    controller.buildingcapsule.transform.rotation);
            }

            controller.buildingcapsule.SetActive(false);
            controller.normalcapsule.SetActive(true);
            controller.switched = false;
        }
    }

    private void DisableBlockedBehaviours(FeaturePreset preset)
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || !behaviour.enabled || IsAllowedBehaviour(behaviour, preset))
            {
                continue;
            }

            behaviour.enabled = false;
            TrackDisabledBehaviour(behaviour);
        }
    }

    private void DisableBlockedAnimators(FeaturePreset preset)
    {
        if (preset != FeaturePreset.WalkingOnly)
        {
            return;
        }

        Animator[] animators = FindObjectsByType<Animator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator == null || !animator.enabled || IsUnderWalkingPlayer(animator.transform))
            {
                continue;
            }

            animator.enabled = false;
            TrackDisabledBehaviour(animator);
        }
    }

    private static void StopBlockedParticles(FeaturePreset preset)
    {
        if (preset != FeaturePreset.WalkingOnly)
        {
            return;
        }

        ParticleSystem[] particleSystems = FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null || IsUnderWalkingPlayer(particleSystem.transform))
            {
                continue;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void RestoreAllowedBehaviours(FeaturePreset preset)
    {
        for (int i = disabledBehaviours.Count - 1; i >= 0; i--)
        {
            Behaviour behaviour = disabledBehaviours[i];
            if (behaviour == null)
            {
                disabledBehaviours.RemoveAt(i);
                continue;
            }

            if (preset == FeaturePreset.Everything || IsAllowedBehaviour(behaviour, preset))
            {
                behaviour.enabled = true;
                disabledBehaviours.RemoveAt(i);
            }
        }
    }

    private void TrackDisabledBehaviour(Behaviour behaviour)
    {
        if (!disabledBehaviours.Contains(behaviour))
        {
            disabledBehaviours.Add(behaviour);
        }
    }

    private static bool IsAllowedBehaviour(Behaviour behaviour, FeaturePreset preset)
    {
        if (behaviour == null)
        {
            return false;
        }

        string typeName = behaviour.GetType().Name;
        if (AlwaysAllowedBehaviours.Contains(typeName))
        {
            return true;
        }

        if (preset >= FeaturePreset.BuildController && BuildControllerBehaviours.Contains(typeName))
        {
            return true;
        }

        if (preset >= FeaturePreset.InventoryCrafting && InventoryCraftingBehaviours.Contains(typeName))
        {
            return true;
        }

        if (preset >= FeaturePreset.Interactions && InteractionBehaviours.Contains(typeName))
        {
            return true;
        }

        if (preset >= FeaturePreset.WorldRuntime && WorldRuntimeBehaviours.Contains(typeName))
        {
            return true;
        }

        return preset >= FeaturePreset.EnemiesAnimals && EnemiesAnimalsBehaviours.Contains(typeName);
    }

    private static bool IsUnderWalkingPlayer(Transform target)
    {
        if (target == null)
        {
            return false;
        }

        FPSControllerTest controller = target.GetComponentInParent<FPSControllerTest>(true);
        return controller != null && controller.gameObject.activeInHierarchy;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("IDK/Debug Feature Preset/0 Walking Only")]
    private static void SetWalkingOnlyPreset()
    {
        SetPresetFromEditor(FeaturePreset.WalkingOnly);
    }

    [UnityEditor.MenuItem("IDK/Debug Feature Preset/1 Build Controller")]
    private static void SetBuildControllerPreset()
    {
        SetPresetFromEditor(FeaturePreset.BuildController);
    }

    [UnityEditor.MenuItem("IDK/Debug Feature Preset/2 Inventory Crafting")]
    private static void SetInventoryCraftingPreset()
    {
        SetPresetFromEditor(FeaturePreset.InventoryCrafting);
    }

    [UnityEditor.MenuItem("IDK/Debug Feature Preset/3 Interactions")]
    private static void SetInteractionsPreset()
    {
        SetPresetFromEditor(FeaturePreset.Interactions);
    }

    [UnityEditor.MenuItem("IDK/Debug Feature Preset/4 World Runtime")]
    private static void SetWorldRuntimePreset()
    {
        SetPresetFromEditor(FeaturePreset.WorldRuntime);
    }

    [UnityEditor.MenuItem("IDK/Debug Feature Preset/5 Enemies Animals")]
    private static void SetEnemiesAnimalsPreset()
    {
        SetPresetFromEditor(FeaturePreset.EnemiesAnimals);
    }

    [UnityEditor.MenuItem("IDK/Debug Feature Preset/6 Everything")]
    private static void SetEverythingPreset()
    {
        SetPresetFromEditor(FeaturePreset.Everything);
    }

    private static void SetPresetFromEditor(FeaturePreset preset)
    {
        PlayerPrefs.SetString(PlayerPrefsPresetKey, preset.ToString());
        PlayerPrefs.Save();

        GameplayDebugFeatureGate gate = FindFirstObjectByType<GameplayDebugFeatureGate>();
        if (gate != null)
        {
            gate.ApplyPreset(preset);
        }

        Debug.Log($"Gameplay debug feature preset set to {preset}. Restart Play Mode if a disabled system does not fully reinitialize.");
    }
#endif
}
