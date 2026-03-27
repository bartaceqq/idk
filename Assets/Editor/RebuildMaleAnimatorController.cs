using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class RebuildMaleAnimatorController
{
    private const string ControllerPath = "Assets/Animations/PrepReWork/AnimsOnly/RemadeController.controller";
    private const string PrepReworkFolder = "Assets/Animations/PrepReWork/AnimsOnly";
    private const string IdlePath = PrepReworkFolder + "/Idle.anim";
    private const string WalkPath = PrepReworkFolder + "/Walking.anim";
    private const string RunPath = PrepReworkFolder + "/Run.anim";
    private const string JumpPath = PrepReworkFolder + "/Jump.anim";
    private const string AxeComboPath = PrepReworkFolder + "/AxeCombo.anim";
    private const string DefaultUpperBodyMaskPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Avatar Mask/Crossbow UpperBody AvatarMask.mask";
    private const string GeneratedUpperBodyMaskPath = "Assets/Animations/Male/GeneratedUpperBody.mask";
    private const string PendingRebuildFlagRelativePath = "Temp/Codex_RebuildRemadeController.flag";

    private static string PendingRebuildFlagPath
    {
        get
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, PendingRebuildFlagRelativePath);
        }
    }

    [InitializeOnLoadMethod]
    private static void RegisterPendingRebuildWatcher()
    {
        EditorApplication.update -= RebuildIfRequestedByFlag;
        EditorApplication.update += RebuildIfRequestedByFlag;
    }

    private static void RebuildIfRequestedByFlag()
    {
        if (!File.Exists(PendingRebuildFlagPath))
        {
            return;
        }

        try
        {
            File.Delete(PendingRebuildFlagPath);
        }
        catch (IOException ioException)
        {
            Debug.LogWarning($"Could not delete rebuild flag '{PendingRebuildFlagRelativePath}': {ioException.Message}");
        }

        Rebuild();
    }

    [MenuItem("Tools/Animation/Rebuild Remade Controller")]
    public static void Rebuild()
    {
        AnimationClip idle = LoadClip(IdlePath, true);
        if (idle == null)
        {
            Debug.LogError("Rebuild RemadeController aborted: Idle.anim is required.");
            return;
        }

        AnimationClip walk = LoadClip(WalkPath);
        AnimationClip run = LoadClip(RunPath);
        AnimationClip jump = LoadClip(JumpPath);
        AnimationClip axeCombo = LoadClip(AxeComboPath);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            if (controller == null)
            {
                Debug.LogError($"Could not create controller at {ControllerPath}");
                return;
            }
        }

        ResetParameters(controller);

        AnimatorControllerLayer[] layers = controller.layers;
        if (layers == null || layers.Length == 0)
        {
            controller.AddLayer("Base Layer");
            layers = controller.layers;
        }

        AnimatorControllerLayer baseLayer = layers[0];
        AnimatorStateMachine baseStateMachine = baseLayer.stateMachine;
        ClearStateMachine(baseStateMachine);
        RebuildBaseLayer(baseStateMachine, idle, walk, run, jump, axeCombo);
        RebuildUpperBodyLayer(controller, idle, axeCombo);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("RemadeController rebuilt from PrepReWork clips. Missing future states remain parameterized but inactive.");
    }

    [MenuItem("Tools/Animation/Rebuild MaleAnim Controller", false, 1001)]
    public static void RebuildLegacyMenuItem()
    {
        Rebuild();
    }

    private static void ResetParameters(AnimatorController controller)
    {
        for (int i = controller.parameters.Length - 1; i >= 0; i--)
        {
            controller.RemoveParameter(controller.parameters[i]);
        }

        AddTriggerParameter(controller, "Swing");
        AddBoolParameter(controller, "Foreward");
        AddBoolParameter(controller, "Idle");
        AddBoolParameter(controller, "Sprinting");
        AddTriggerParameter(controller, "Mine");
        AddTriggerParameter(controller, "Attack");
        AddTriggerParameter(controller, "AttackHeavy");
        AddTriggerParameter(controller, "PunchLeft");
        AddTriggerParameter(controller, "PunchRight");
        AddTriggerParameter(controller, "Jump");
        AddBoolParameter(controller, "WalkingBackWards");
        AddBoolParameter(controller, "WalkingLeft");
        AddBoolParameter(controller, "WalkingRight");
        AddBoolParameter(controller, "WalkingForwardLeft");
        AddBoolParameter(controller, "WalkingForwardRight");
        AddBoolParameter(controller, "SprintingForwardLeft");
        AddBoolParameter(controller, "SprintingForwardRight");
        AddBoolParameter(controller, "GunEquipped");
        AddBoolParameter(controller, "GunUnequipped");
        AddTriggerParameter(controller, "GunShoot");
        AddTriggerParameter(controller, "GunReload");
        AddFloatParameter(controller, "IdleSpeed", 1f);
        AddFloatParameter(controller, "WalkSpeed", 1f);
        AddFloatParameter(controller, "RunSpeed", 1f);
        AddFloatParameter(controller, "WalkBackwardSpeed", 1f);
        AddFloatParameter(controller, "LeftStrafeSpeed", 1f);
        AddFloatParameter(controller, "RightStrafeSpeed", 1f);
        AddFloatParameter(controller, "JumpSpeed", 1f);
        AddFloatParameter(controller, "RunningJumpSpeed", 1f);
        AddFloatParameter(controller, "AttackLightSpeed", 1f);
        AddFloatParameter(controller, "AttackHeavySpeed", 1f);
        AddFloatParameter(controller, "PunchLeftSpeed", 1f);
        AddFloatParameter(controller, "PunchRightSpeed", 1f);
        AddFloatParameter(controller, "MineSpeed", 1f);
        AddFloatParameter(controller, "UpperChopSpeed", 1f);
        AddFloatParameter(controller, "GunAimSpeed", 1f);
        AddFloatParameter(controller, "GunShootSpeed", 1f);
        AddFloatParameter(controller, "GunReloadSpeed", 1f);
    }

    private static void RebuildBaseLayer(
        AnimatorStateMachine stateMachine,
        AnimationClip idle,
        AnimationClip walk,
        AnimationClip run,
        AnimationClip jump,
        AnimationClip axeCombo)
    {
        AnimatorState idleState = AddState(stateMachine, "Idle", new Vector3(420f, 220f, 0f), idle, "IdleSpeed");
        AnimatorState forwardState = AddOptionalState(stateMachine, "ForeWard", new Vector3(700f, 320f, 0f), walk, "WalkSpeed");
        AnimatorState sprintState = AddOptionalState(stateMachine, "SprintForeWard", new Vector3(700f, 150f, 0f), run, "RunSpeed");
        AnimatorState jumpState = AddOptionalState(stateMachine, "Jump", new Vector3(420f, 540f, 0f), jump, "JumpSpeed");
        AnimatorState chopState = AddOptionalState(stateMachine, "Chop", new Vector3(180f, 700f, 0f), axeCombo, "UpperChopSpeed");

        if (jumpState != null)
        {
            jumpState.tag = "Action";
        }

        if (chopState != null)
        {
            chopState.tag = "Action";
        }

        stateMachine.defaultState = idleState;

        if (forwardState != null)
        {
            AddAnyBoolTransition(stateMachine, forwardState, "Foreward", true, 0.05f);
            AddBoolTransition(forwardState, idleState, "Idle", true, false, 0.05f);
        }

        if (sprintState != null)
        {
            AddAnyBoolTransition(stateMachine, sprintState, "Sprinting", true, 0.05f);
            AddBoolTransition(sprintState, idleState, "Idle", true, false, 0.05f);
        }

        if (jumpState != null)
        {
            AddAnyTriggerTransition(stateMachine, jumpState, "Jump", 0.02f);
            AddExitTimeTransition(jumpState, idleState, 1f, 0.05f);
        }

        if (chopState != null)
        {
            AddAnyTriggerTransition(stateMachine, chopState, "Swing", 0.02f);
            AddExitTimeTransition(chopState, idleState, 1f, 0.05f);
        }
    }

    private static void RebuildUpperBodyLayer(
        AnimatorController controller,
        AnimationClip idle,
        AnimationClip axeCombo)
    {
        int upperLayerIndex = GetOrCreateLayerIndex(controller, "UpperBody");
        if (upperLayerIndex < 0)
        {
            Debug.LogError("Rebuild RemadeController aborted: could not create/find UpperBody layer.");
            return;
        }

        AnimatorControllerLayer[] layers = controller.layers;
        AnimatorControllerLayer upperLayer = layers[upperLayerIndex];
        AnimatorStateMachine upperStateMachine = upperLayer.stateMachine;
        if (upperStateMachine == null)
        {
            upperStateMachine = new AnimatorStateMachine { name = "UpperBody" };
            AssetDatabase.AddObjectToAsset(upperStateMachine, controller);
            upperLayer.stateMachine = upperStateMachine;
            layers[upperLayerIndex] = upperLayer;
            controller.layers = layers;
        }

        upperLayer.blendingMode = AnimatorLayerBlendingMode.Override;
        upperLayer.defaultWeight = 0f;
        upperLayer.avatarMask = ResolveUpperBodyMask(upperLayer.avatarMask);
        layers[upperLayerIndex] = upperLayer;
        controller.layers = layers;

        ClearStateMachine(upperStateMachine);

        AnimatorState upperIdleState = AddState(upperStateMachine, "UpperBodyIdle", new Vector3(420f, 220f, 0f), idle, "IdleSpeed");
        upperStateMachine.defaultState = upperIdleState;

        if (axeCombo != null)
        {
            AnimatorState upperChopState = AddState(upperStateMachine, "UpperChop", new Vector3(560f, 160f, 0f), axeCombo, "UpperChopSpeed");
            upperChopState.tag = "Action";
            AddAnyTriggerTransition(upperStateMachine, upperChopState, "Swing", 0.02f);
            AddExitTimeTransition(upperChopState, upperIdleState, 1f, 0.05f);
        }
    }

    private static AnimationClip LoadClip(string path, bool required = false)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null && required)
        {
            Debug.LogError($"Missing animation clip at: {path}");
        }

        return clip;
    }

    private static AnimatorState AddState(
        AnimatorStateMachine stateMachine,
        string name,
        Vector3 position,
        Motion motion,
        string speedParameterName)
    {
        AnimatorState state = stateMachine.AddState(name, position);
        state.motion = motion;
        ConfigureStateSpeedParameter(state, speedParameterName);
        return state;
    }

    private static AnimatorState AddOptionalState(
        AnimatorStateMachine stateMachine,
        string name,
        Vector3 position,
        Motion motion,
        string speedParameterName)
    {
        if (motion == null)
        {
            return null;
        }

        return AddState(stateMachine, name, position, motion, speedParameterName);
    }

    private static void AddTriggerParameter(AnimatorController controller, string name)
    {
        controller.AddParameter(name, AnimatorControllerParameterType.Trigger);
    }

    private static void AddBoolParameter(AnimatorController controller, string name, bool defaultValue = false)
    {
        controller.AddParameter(new AnimatorControllerParameter
        {
            name = name,
            type = AnimatorControllerParameterType.Bool,
            defaultBool = defaultValue
        });
    }

    private static void AddFloatParameter(AnimatorController controller, string name, float defaultValue)
    {
        controller.AddParameter(new AnimatorControllerParameter
        {
            name = name,
            type = AnimatorControllerParameterType.Float,
            defaultFloat = defaultValue
        });
    }

    private static void AddAnyTriggerTransition(AnimatorStateMachine stateMachine, AnimatorState to, string triggerName, float duration)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
    }

    private static void ConfigureStateSpeedParameter(AnimatorState state, string parameterName)
    {
        if (state == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        state.speed = 1f;
        state.speedParameter = parameterName;
        state.speedParameterActive = true;
    }

    private static AvatarMask ResolveUpperBodyMask(AvatarMask currentMask)
    {
        if (currentMask != null)
        {
            return currentMask;
        }

        AvatarMask loadedMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(DefaultUpperBodyMaskPath);
        if (loadedMask != null)
        {
            return loadedMask;
        }

        AvatarMask generatedMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(GeneratedUpperBodyMaskPath);
        if (generatedMask != null)
        {
            return generatedMask;
        }

        generatedMask = new AvatarMask();
        generatedMask.name = "GeneratedUpperBody";

        generatedMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Root, false);
        generatedMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
        generatedMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
        generatedMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftLeg, false);
        generatedMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightLeg, false);
        generatedMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
        generatedMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
        generatedMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
        generatedMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);

        AssetDatabase.CreateAsset(generatedMask, GeneratedUpperBodyMaskPath);
        return generatedMask;
    }

    private static int GetOrCreateLayerIndex(AnimatorController controller, string layerName)
    {
        AnimatorControllerLayer[] layers = controller.layers;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].name == layerName)
            {
                return i;
            }
        }

        controller.AddLayer(layerName);
        layers = controller.layers;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].name == layerName)
            {
                return i;
            }
        }

        return -1;
    }

    private static void ClearStateMachine(AnimatorStateMachine stateMachine)
    {
        for (int i = stateMachine.anyStateTransitions.Length - 1; i >= 0; i--)
        {
            stateMachine.RemoveAnyStateTransition(stateMachine.anyStateTransitions[i]);
        }

        for (int i = stateMachine.entryTransitions.Length - 1; i >= 0; i--)
        {
            stateMachine.RemoveEntryTransition(stateMachine.entryTransitions[i]);
        }

        for (int i = stateMachine.states.Length - 1; i >= 0; i--)
        {
            stateMachine.RemoveState(stateMachine.states[i].state);
        }

        for (int i = stateMachine.stateMachines.Length - 1; i >= 0; i--)
        {
            stateMachine.RemoveStateMachine(stateMachine.stateMachines[i].stateMachine);
        }
    }

    private static void AddAnyBoolTransition(AnimatorStateMachine stateMachine, AnimatorState to, string parameterName, bool value, float duration)
    {
        AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameterName);
    }

    private static void AddExitTimeTransition(AnimatorState from, AnimatorState to, float exitTime, float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = duration;
    }

    private static void AddBoolTransition(
        AnimatorState from,
        AnimatorState to,
        string conditionName,
        bool value,
        bool hasExitTime,
        float duration)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = hasExitTime;
        transition.duration = duration;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, conditionName);
    }
}
