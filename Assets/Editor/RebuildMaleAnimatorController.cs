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
    private const string RunningJumpPath = PrepReworkFolder + "/RunningJump.anim";
    private const string WalkBackwardPath = PrepReworkFolder + "/WalkingBackwards.anim";
    private const string LeftStrafePath = PrepReworkFolder + "/LeftStrafeWalking.anim";
    private const string RightStrafePath = PrepReworkFolder + "/RightStrafeWalking.anim";
    private const string RightStrafeTypoPath = PrepReworkFolder + "/RightStrafeWalkling.anim";
    private const string EmotePath = PrepReworkFolder + "/Emote.anim";
    private const string MinePath = PrepReworkFolder + "/Mine.anim";
    private const string AxeFirstPath = PrepReworkFolder + "/AxeFirst.anim";
    private const string AxeSecondPath = PrepReworkFolder + "/AxeSecond.anim";
    private const string SwordAttackPath = PrepReworkFolder + "/SwordAttack.anim";
    private const string SwordAttack2Path = PrepReworkFolder + "/SwordAttack2.anim";
    private const string SpecialAttack1Path = PrepReworkFolder + "/SpecialAttack1.anim";
    private const string SpecialAttack2Path = PrepReworkFolder + "/SpecialAttack2.anim";
    private const string SpecialAttack3Path = PrepReworkFolder + "/SpecialAttack3.anim";
    private const string PullOutSwordPath = PrepReworkFolder + "/PullOutSword.anim";
    private const string HideSwordPath = PrepReworkFolder + "/HideSword.anim";
    private const string BlockEnterPath = PrepReworkFolder + "/BlokEnter.anim";
    private const string BlockLoopPath = PrepReworkFolder + "/BlockLoop.anim";
    private const string BlockExitPath = PrepReworkFolder + "/BlockExit.anim";
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
        AnimationClip runningJump = LoadClip(RunningJumpPath);
        AnimationClip walkBackward = LoadClip(WalkBackwardPath);
        AnimationClip leftStrafe = LoadClip(LeftStrafePath);
        AnimationClip rightStrafe = LoadFirstExistingClip(RightStrafeTypoPath, RightStrafePath);
        AnimationClip emote = LoadClip(EmotePath);
        AnimationClip mine = LoadClip(MinePath);
        AnimationClip axeFirst = LoadClip(AxeFirstPath);
        AnimationClip axeSecond = LoadClip(AxeSecondPath);
        AnimationClip swordAttack = LoadClip(SwordAttackPath);
        AnimationClip swordAttack2 = LoadClip(SwordAttack2Path);
        AnimationClip specialAttack1 = LoadClip(SpecialAttack1Path);
        AnimationClip specialAttack2 = LoadClip(SpecialAttack2Path);
        AnimationClip specialAttack3 = LoadClip(SpecialAttack3Path);
        AnimationClip pullOutSword = LoadClip(PullOutSwordPath);
        AnimationClip hideSword = LoadClip(HideSwordPath);
        AnimationClip blockEnter = LoadClip(BlockEnterPath);
        AnimationClip blockLoop = LoadClip(BlockLoopPath);
        AnimationClip blockExit = LoadClip(BlockExitPath);

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
        RebuildBaseLayer(
            baseStateMachine,
            idle,
            walk,
            run,
            jump,
            runningJump,
            walkBackward,
            leftStrafe,
            rightStrafe,
            emote,
            swordAttack,
            swordAttack2,
            specialAttack1,
            specialAttack2,
            specialAttack3,
            pullOutSword,
            hideSword,
            blockEnter,
            blockLoop,
            blockExit);
        RebuildUpperBodyLayer(controller, idle, axeFirst, axeSecond, mine);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("RemadeController rebuilt from PrepReWork clips.");
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
        AddBoolParameter(controller, "SwordBlocking");
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
        AnimationClip runningJump,
        AnimationClip walkBackward,
        AnimationClip leftStrafe,
        AnimationClip rightStrafe,
        AnimationClip emote,
        AnimationClip swordAttack,
        AnimationClip swordAttack2,
        AnimationClip specialAttack1,
        AnimationClip specialAttack2,
        AnimationClip specialAttack3,
        AnimationClip pullOutSword,
        AnimationClip hideSword,
        AnimationClip blockEnter,
        AnimationClip blockLoop,
        AnimationClip blockExit)
    {
        AnimatorState idleState = AddState(stateMachine, "Idle", new Vector3(420f, 220f, 0f), idle, "IdleSpeed");
        AnimatorState forwardState = AddOptionalState(stateMachine, "ForeWard", new Vector3(700f, 220f, 0f), walk, "WalkSpeed");
        AnimatorState sprintState = AddOptionalState(stateMachine, "SprintForeWard", new Vector3(700f, 100f, 0f), run, "RunSpeed");
        AnimatorState backwardState = AddOptionalState(stateMachine, "WalkingBackWards", new Vector3(700f, 340f, 0f), walkBackward != null ? walkBackward : walk, "WalkBackwardSpeed");
        AnimatorState leftState = AddOptionalState(stateMachine, "WalkingLeft", new Vector3(920f, 220f, 0f), leftStrafe != null ? leftStrafe : walk, "LeftStrafeSpeed");
        AnimatorState rightState = AddOptionalState(stateMachine, "WalkingRight", new Vector3(920f, 340f, 0f), rightStrafe != null ? rightStrafe : walk, "RightStrafeSpeed");
        AnimatorState forwardLeftState = AddOptionalState(stateMachine, "WalkingForwardLeft", new Vector3(920f, 100f, 0f), walk, "LeftStrafeSpeed");
        AnimatorState forwardRightState = AddOptionalState(stateMachine, "WalkingForwardRight", new Vector3(920f, 460f, 0f), walk, "RightStrafeSpeed");
        AnimatorState sprintForwardLeftState = AddOptionalState(stateMachine, "SprintingForwardLeft", new Vector3(1120f, 100f, 0f), run, "RunSpeed");
        AnimatorState sprintForwardRightState = AddOptionalState(stateMachine, "SprintingForwardRight", new Vector3(1120f, 460f, 0f), run, "RunSpeed");
        AnimatorState jumpState = AddOptionalState(stateMachine, "Jump", new Vector3(420f, 520f, 0f), jump, "JumpSpeed");
        AnimatorState runningJumpState = AddOptionalState(stateMachine, "RunningJump", new Vector3(620f, 520f, 0f), runningJump, "RunningJumpSpeed");
        AddOptionalState(stateMachine, "Emote", new Vector3(820f, 520f, 0f), emote, null);
        AnimatorState swordAttackState = AddOptionalState(stateMachine, "SwordAttack", new Vector3(180f, 680f, 0f), swordAttack, "AttackLightSpeed");
        AnimatorState swordAttack2State = AddOptionalState(stateMachine, "SwordAttack2", new Vector3(180f, 840f, 0f), swordAttack2, "AttackLightSpeed");
        AnimatorState specialAttack1State = AddOptionalState(stateMachine, "SpecialAttack1", new Vector3(420f, 700f, 0f), specialAttack1, "AttackHeavySpeed");
        AnimatorState specialAttack2State = AddOptionalState(stateMachine, "SpecialAttack2", new Vector3(420f, 860f, 0f), specialAttack2, "AttackHeavySpeed");
        AnimatorState specialAttack3State = AddOptionalState(stateMachine, "SpecialAttack3", new Vector3(420f, 1020f, 0f), specialAttack3, "AttackHeavySpeed");
        AnimatorState pullOutSwordState = AddOptionalState(stateMachine, "PullOutSword", new Vector3(660f, 700f, 0f), pullOutSword, "AttackLightSpeed");
        AnimatorState hideSwordState = AddOptionalState(stateMachine, "HideSword", new Vector3(660f, 860f, 0f), hideSword, "AttackLightSpeed");
        AnimatorState blockEnterState = AddOptionalState(stateMachine, "BlokEnter", new Vector3(900f, 700f, 0f), blockEnter, "AttackHeavySpeed");
        AnimatorState blockLoopState = AddOptionalState(stateMachine, "BlockLoop", new Vector3(900f, 860f, 0f), blockLoop, "AttackHeavySpeed");
        AnimatorState blockExitState = AddOptionalState(stateMachine, "BlockExit", new Vector3(900f, 1020f, 0f), blockExit, "AttackHeavySpeed");

        stateMachine.defaultState = idleState;

        AddMovementStateTransitions(stateMachine, forwardState, idleState, "Foreward", useIdleExit: true);
        AddMovementStateTransitions(stateMachine, sprintState, idleState, "Sprinting", useIdleExit: true);
        AddMovementStateTransitions(stateMachine, backwardState, idleState, "WalkingBackWards");
        AddMovementStateTransitions(stateMachine, leftState, idleState, "WalkingLeft");
        AddMovementStateTransitions(stateMachine, rightState, idleState, "WalkingRight");
        AddMovementStateTransitions(stateMachine, forwardLeftState, idleState, "WalkingForwardLeft");
        AddMovementStateTransitions(stateMachine, forwardRightState, idleState, "WalkingForwardRight");
        AddMovementStateTransitions(stateMachine, sprintForwardLeftState, idleState, "SprintingForwardLeft");
        AddMovementStateTransitions(stateMachine, sprintForwardRightState, idleState, "SprintingForwardRight");

        ConfigureActionState(jumpState, idleState);
        ConfigureActionState(runningJumpState, idleState);
        ConfigureActionState(swordAttackState, idleState);
        ConfigureActionState(swordAttack2State, idleState);
        ConfigureActionState(specialAttack1State, idleState);
        ConfigureActionState(specialAttack2State, idleState);
        ConfigureActionState(specialAttack3State, idleState);
        ConfigureActionState(pullOutSwordState, idleState);
        ConfigureActionState(hideSwordState, idleState);

        if (jumpState != null)
        {
            AddAnyTriggerTransition(stateMachine, jumpState, "Jump", 0.02f);
        }

        if (swordAttackState != null)
        {
            AddAnyTriggerTransition(stateMachine, swordAttackState, "Attack", 0.02f);
        }

        if (specialAttack1State != null)
        {
            AddAnyTriggerTransition(stateMachine, specialAttack1State, "AttackHeavy", 0.02f);
        }

        if (blockEnterState != null)
        {
            blockEnterState.tag = "Action";
            if (blockLoopState != null)
            {
                blockLoopState.tag = "Action";
                AddExitTimeTransition(blockEnterState, blockLoopState, 1f, 0.03f);
            }
            else
            {
                AddExitTimeTransition(blockEnterState, idleState, 1f, 0.05f);
            }

            if (blockExitState != null)
            {
                AddBoolTransition(blockEnterState, blockExitState, "SwordBlocking", false, false, 0.03f);
            }
        }

        if (blockLoopState != null)
        {
            blockLoopState.tag = "Action";
            if (blockExitState != null)
            {
                blockExitState.tag = "Action";
                AddBoolTransition(blockLoopState, blockExitState, "SwordBlocking", false, false, 0.03f);
            }
            else
            {
                AddBoolTransition(blockLoopState, idleState, "SwordBlocking", false, false, 0.05f);
            }
        }

        if (blockExitState != null)
        {
            blockExitState.tag = "Action";
            AddExitTimeTransition(blockExitState, idleState, 1f, 0.05f);
        }
    }

    private static void RebuildUpperBodyLayer(
        AnimatorController controller,
        AnimationClip idle,
        AnimationClip axeFirst,
        AnimationClip axeSecond,
        AnimationClip mine)
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

        if (axeFirst != null)
        {
            AnimatorState upperChopState = AddState(upperStateMachine, "UpperChop", new Vector3(620f, 140f, 0f), axeFirst, "UpperChopSpeed");
            upperChopState.tag = "Action";
            AddAnyTriggerTransition(upperStateMachine, upperChopState, "Swing", 0.02f);
            AddExitTimeTransition(upperChopState, upperIdleState, 1f, 0.05f);
        }

        if (axeSecond != null)
        {
            AnimatorState upperChopSecondState = AddState(upperStateMachine, "UpperChopSecond", new Vector3(820f, 140f, 0f), axeSecond, "UpperChopSpeed");
            upperChopSecondState.tag = "Action";
            AddExitTimeTransition(upperChopSecondState, upperIdleState, 1f, 0.05f);
        }

        if (mine != null)
        {
            AnimatorState upperMineState = AddState(upperStateMachine, "UpperMining", new Vector3(620f, 320f, 0f), mine, "MineSpeed");
            upperMineState.tag = "Action";
            AddAnyTriggerTransition(upperStateMachine, upperMineState, "Mine", 0.02f);
            AddExitTimeTransition(upperMineState, upperIdleState, 1f, 0.05f);
        }
    }

    private static void AddMovementStateTransitions(
        AnimatorStateMachine stateMachine,
        AnimatorState movementState,
        AnimatorState idleState,
        string parameterName,
        bool useIdleExit = false)
    {
        if (movementState == null || idleState == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return;
        }

        AddAnyBoolTransition(stateMachine, movementState, parameterName, true, 0.05f);
        if (useIdleExit)
        {
            AddBoolTransition(movementState, idleState, "Idle", true, false, 0.05f);
        }
        else
        {
            AddBoolTransition(movementState, idleState, parameterName, false, false, 0.05f);
        }
    }

    private static void ConfigureActionState(AnimatorState actionState, AnimatorState idleState)
    {
        if (actionState == null || idleState == null)
        {
            return;
        }

        actionState.tag = "Action";
        AddExitTimeTransition(actionState, idleState, 1f, 0.05f);
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

    private static AnimationClip LoadFirstExistingClip(params string[] paths)
    {
        if (paths == null)
        {
            return null;
        }

        for (int i = 0; i < paths.Length; i++)
        {
            AnimationClip clip = LoadClip(paths[i]);
            if (clip != null)
            {
                return clip;
            }
        }

        return null;
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
