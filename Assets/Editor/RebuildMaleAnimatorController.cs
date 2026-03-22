using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class RebuildMaleAnimatorController
{
    private const string ControllerPath = "Assets/Animations/Male/MaleAnim.controller";
    private const string IdlePath = "Assets/Animations/Male/Idle.anim";
    private const string ForwardPath = "Assets/Animations/Male/ForeWard.anim";
    private const string SprintPath = "Assets/Animations/Male/SprintForeWard.anim";
    private const string BackwardPath = "Assets/Animations/Male/WalkBackWards.anim";
    private const string JumpPath = "Assets/Animations/Male/Jump.anim";
    private const string AttackLightPath = "Assets/Characters_StarterPack_Blink/Art/Animations/Animations_Starter_Pack/Combat/MeleeAttack_OneHanded.fbx";
    private const string AttackHeavyPath = "Assets/Characters_StarterPack_Blink/Art/Animations/Animations_Starter_Pack/Combat/MeleeAttack_TwoHanded.fbx";
    private const string PunchLeftPath = "Assets/Characters_StarterPack_Blink/Art/Animations/Animations_Starter_Pack/Combat/PunchLeft.fbx";
    private const string PunchRightPath = "Assets/Characters_StarterPack_Blink/Art/Animations/Animations_Starter_Pack/Combat/PunchRight.fbx";
    private const string MinePath = "Assets/Animations/Male/Mining.anim";
    private const string ChopPath = "Assets/Animations/Male/Chop.anim";
    private const string AimPath = "Assets/Animations/Male/ARAim.anim";
    private const string ShootPath = "Assets/Animations/Male/ARShoot.anim";
    private const string ReloadPath = "Assets/Animations/Male/ARReload.anim";
    private const string DefaultUpperBodyMaskPath = "Assets/ExplosiveLLC/Warrior Pack Bundle 3 FREE/Crossbow Warrior Mecanim Animation Pack/Avatar Mask/Crossbow UpperBody AvatarMask.mask";
    private const string GeneratedUpperBodyMaskPath = "Assets/Animations/Male/GeneratedUpperBody.mask";
    private const string WalkLeftPath = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Left.fbx";
    private const string WalkRightPath = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Right.fbx";
    private const string WalkForwardLeftPath = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_ForwardLeft.fbx";
    private const string WalkForwardRightPath = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_ForwardRight.fbx";
    private const string SprintForwardLeftPath = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Sprint/HumanM@Sprint01_ForwardLeft.fbx";
    private const string SprintForwardRightPath = "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Sprint/HumanM@Sprint01_ForwardRight.fbx";

    [MenuItem("Tools/Animation/Rebuild MaleAnim Controller")]
    public static void Rebuild()
    {
        AnimationClip idle = LoadClip(IdlePath);
        AnimationClip forward = LoadClip(ForwardPath);
        AnimationClip sprint = LoadClip(SprintPath);
        AnimationClip backward = LoadClip(BackwardPath);
        AnimationClip jump = LoadClip(JumpPath);
        AnimationClip attackLight = LoadClip(AttackLightPath);
        AnimationClip attackHeavy = LoadClip(AttackHeavyPath);
        AnimationClip punchLeft = LoadClip(PunchLeftPath);
        AnimationClip punchRight = LoadClip(PunchRightPath);
        AnimationClip mine = LoadClip(MinePath);
        AnimationClip chop = LoadClip(ChopPath);
        AnimationClip aim = LoadClip(AimPath);
        AnimationClip shoot = LoadClip(ShootPath);
        AnimationClip reload = LoadClip(ReloadPath);
        AnimationClip walkLeft = LoadClip(WalkLeftPath);
        AnimationClip walkRight = LoadClip(WalkRightPath);
        AnimationClip walkForwardLeft = LoadClip(WalkForwardLeftPath);
        AnimationClip walkForwardRight = LoadClip(WalkForwardRightPath);
        AnimationClip sprintForwardLeft = LoadClip(SprintForwardLeftPath);
        AnimationClip sprintForwardRight = LoadClip(SprintForwardRightPath);

        if (idle == null || forward == null || sprint == null || backward == null ||
            jump == null || attackLight == null || attackHeavy == null || punchLeft == null || punchRight == null ||
            mine == null || chop == null || aim == null || shoot == null || reload == null ||
            walkLeft == null || walkRight == null || walkForwardLeft == null || walkForwardRight == null ||
            sprintForwardLeft == null || sprintForwardRight == null)
        {
            Debug.LogError("Rebuild MaleAnim aborted: one or more clips are missing.");
            return;
        }

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

        // Reset parameters.
        for (int i = controller.parameters.Length - 1; i >= 0; i--)
        {
            controller.RemoveParameter(controller.parameters[i]);
        }

        controller.AddParameter("Swing", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Foreward", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Idle", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Sprinting", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Mine", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("AttackHeavy", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("PunchLeft", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("PunchRight", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("WalkingBackWards", AnimatorControllerParameterType.Bool);
        controller.AddParameter("WalkingLeft", AnimatorControllerParameterType.Bool);
        controller.AddParameter("WalkingRight", AnimatorControllerParameterType.Bool);
        controller.AddParameter("WalkingForwardLeft", AnimatorControllerParameterType.Bool);
        controller.AddParameter("WalkingForwardRight", AnimatorControllerParameterType.Bool);
        controller.AddParameter("SprintingForwardLeft", AnimatorControllerParameterType.Bool);
        controller.AddParameter("SprintingForwardRight", AnimatorControllerParameterType.Bool);
        controller.AddParameter("GunEquipped", AnimatorControllerParameterType.Bool);
        controller.AddParameter("GunUnequipped", AnimatorControllerParameterType.Bool);
        controller.AddParameter("GunShoot", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("GunReload", AnimatorControllerParameterType.Trigger);

        AnimatorControllerLayer[] layers = controller.layers;
        if (layers == null || layers.Length == 0)
        {
            controller.AddLayer("Base Layer");
            layers = controller.layers;
        }

        AnimatorControllerLayer baseLayer = layers[0];
        AnimatorStateMachine sm = baseLayer.stateMachine;

        // Clear transitions and states.
        for (int i = sm.anyStateTransitions.Length - 1; i >= 0; i--)
        {
            sm.RemoveAnyStateTransition(sm.anyStateTransitions[i]);
        }

        for (int i = sm.entryTransitions.Length - 1; i >= 0; i--)
        {
            sm.RemoveEntryTransition(sm.entryTransitions[i]);
        }

        for (int i = sm.states.Length - 1; i >= 0; i--)
        {
            sm.RemoveState(sm.states[i].state);
        }

        // Add states.
        AnimatorState idleState = sm.AddState("Idle", new Vector3(420f, 220f, 0f));
        AnimatorState forwardState = sm.AddState("ForeWard", new Vector3(700f, 320f, 0f));
        AnimatorState sprintState = sm.AddState("SprintForeWard", new Vector3(700f, 150f, 0f));
        AnimatorState backwardState = sm.AddState("WalkBackWards", new Vector3(700f, 480f, 0f));
        AnimatorState jumpState = sm.AddState("Jump", new Vector3(420f, 540f, 0f));
        AnimatorState attackLightState = sm.AddState("AttackWeapon", new Vector3(180f, 220f, 0f));
        AnimatorState attackHeavyState = sm.AddState("AttackTwoHanded", new Vector3(180f, 320f, 0f));
        AnimatorState punchLeftState = sm.AddState("PunchLeft", new Vector3(180f, 120f, 0f));
        AnimatorState punchRightState = sm.AddState("PunchRight", new Vector3(180f, 20f, 0f));
        AnimatorState mineState = sm.AddState("Mining", new Vector3(420f, 700f, 0f));
        AnimatorState chopState = sm.AddState("Chop", new Vector3(180f, 700f, 0f));
        AnimatorState walkLeftState = sm.AddState("WalkLeft", new Vector3(980f, 320f, 0f));
        AnimatorState walkRightState = sm.AddState("WalkRight", new Vector3(980f, 420f, 0f));
        AnimatorState walkForwardLeftState = sm.AddState("WalkForwardLeft", new Vector3(980f, 210f, 0f));
        AnimatorState walkForwardRightState = sm.AddState("WalkForwardRight", new Vector3(980f, 110f, 0f));
        AnimatorState sprintForwardLeftState = sm.AddState("SprintForwardLeft", new Vector3(1220f, 160f, 0f));
        AnimatorState sprintForwardRightState = sm.AddState("SprintForwardRight", new Vector3(1220f, 60f, 0f));

        idleState.motion = idle;
        forwardState.motion = forward;
        sprintState.motion = sprint;
        backwardState.motion = backward;
        jumpState.motion = jump;
        attackLightState.motion = attackLight;
        attackHeavyState.motion = attackHeavy;
        punchLeftState.motion = punchLeft;
        punchRightState.motion = punchRight;
        mineState.motion = mine;
        // Reuse sword light attack animation for axe chop.
        chopState.motion = attackLight;
        walkLeftState.motion = walkLeft;
        walkRightState.motion = walkRight;
        walkForwardLeftState.motion = walkForwardLeft;
        walkForwardRightState.motion = walkForwardRight;
        sprintForwardLeftState.motion = sprintForwardLeft;
        sprintForwardRightState.motion = sprintForwardRight;

        attackLightState.tag = "Action";
        attackHeavyState.tag = "Action";
        punchLeftState.tag = "Action";
        punchRightState.tag = "Action";
        mineState.tag = "Action";
        chopState.tag = "Action";
        jumpState.tag = "Action";

        sm.defaultState = idleState;

        // AnyState movement transitions.
        AddAnyBoolTransition(sm, forwardState, "Foreward", true, 0.05f);
        AddAnyBoolTransition(sm, sprintState, "Sprinting", true, 0.05f);
        AddAnyBoolTransition(sm, backwardState, "WalkingBackWards", true, 0.05f);
        AddAnyBoolTransition(sm, walkLeftState, "WalkingLeft", true, 0.05f);
        AddAnyBoolTransition(sm, walkRightState, "WalkingRight", true, 0.05f);
        AddAnyBoolTransition(sm, walkForwardLeftState, "WalkingForwardLeft", true, 0.05f);
        AddAnyBoolTransition(sm, walkForwardRightState, "WalkingForwardRight", true, 0.05f);
        AddAnyBoolTransition(sm, sprintForwardLeftState, "SprintingForwardLeft", true, 0.05f);
        AddAnyBoolTransition(sm, sprintForwardRightState, "SprintingForwardRight", true, 0.05f);

        // AnyState action transitions.
        AddAnyTriggerTransition(sm, jumpState, "Jump", 0.02f);
        AddAnyTriggerTransition(sm, attackLightState, "Attack", 0.02f);
        AddAnyTriggerTransition(sm, attackHeavyState, "AttackHeavy", 0.02f);
        AddAnyTriggerTransition(sm, punchLeftState, "PunchLeft", 0.02f);
        AddAnyTriggerTransition(sm, punchRightState, "PunchRight", 0.02f);
        AddAnyTriggerTransition(sm, mineState, "Mine", 0.02f);
        AddAnyTriggerTransition(sm, chopState, "Swing", 0.02f);

        // Movement states can always return to Idle.
        AddBoolTransition(forwardState, idleState, "Idle", true, false, 0.05f);
        AddBoolTransition(sprintState, idleState, "Idle", true, false, 0.05f);
        AddBoolTransition(backwardState, idleState, "Idle", true, false, 0.05f);
        AddBoolTransition(walkLeftState, idleState, "Idle", true, false, 0.05f);
        AddBoolTransition(walkRightState, idleState, "Idle", true, false, 0.05f);
        AddBoolTransition(walkForwardLeftState, idleState, "Idle", true, false, 0.05f);
        AddBoolTransition(walkForwardRightState, idleState, "Idle", true, false, 0.05f);
        AddBoolTransition(sprintForwardLeftState, idleState, "Idle", true, false, 0.05f);
        AddBoolTransition(sprintForwardRightState, idleState, "Idle", true, false, 0.05f);

        // Action states return to idle after finishing.
        AddExitTimeTransition(jumpState, idleState, 1f, 0.05f);
        AddExitTimeTransition(attackLightState, idleState, 1f, 0.05f);
        AddExitTimeTransition(attackHeavyState, idleState, 1f, 0.05f);
        AddExitTimeTransition(punchLeftState, idleState, 1f, 0.05f);
        AddExitTimeTransition(punchRightState, idleState, 1f, 0.05f);
        AddExitTimeTransition(mineState, idleState, 1f, 0.05f);
        AddExitTimeTransition(chopState, idleState, 1f, 0.05f);

        RebuildUpperBodyLayer(controller, idle, attackLight, attackHeavy, punchLeft, punchRight, mine, attackLight, aim, shoot, reload);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("MaleAnim.controller rebuilt with movement/combat transitions (without BuildingIdle).");
    }

    private static AnimationClip LoadClip(string path)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            Debug.LogError($"Missing animation clip at: {path}");
        }

        return clip;
    }

    private static void AddAnyTriggerTransition(AnimatorStateMachine sm, AnimatorState to, string triggerName, float duration)
    {
        AnimatorStateTransition transition = sm.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = duration;
        transition.canTransitionToSelf = false;
        transition.AddCondition(AnimatorConditionMode.If, 0f, triggerName);
    }

    private static void RebuildUpperBodyLayer(
        AnimatorController controller,
        AnimationClip idle,
        AnimationClip attackLight,
        AnimationClip attackHeavy,
        AnimationClip punchLeft,
        AnimationClip punchRight,
        AnimationClip mine,
        AnimationClip chopLikeSword,
        AnimationClip aim,
        AnimationClip shoot,
        AnimationClip reload)
    {
        int upperLayerIndex = GetOrCreateLayerIndex(controller, "UpperBody");
        if (upperLayerIndex < 0)
        {
            Debug.LogError("Rebuild MaleAnim aborted: could not create/find UpperBody layer.");
            return;
        }

        AnimatorControllerLayer[] layers = controller.layers;
        AnimatorControllerLayer upperLayer = layers[upperLayerIndex];
        AnimatorStateMachine upperSm = upperLayer.stateMachine;
        if (upperSm == null)
        {
            upperSm = new AnimatorStateMachine { name = "UpperBody" };
            AssetDatabase.AddObjectToAsset(upperSm, controller);
            upperLayer.stateMachine = upperSm;
            layers[upperLayerIndex] = upperLayer;
            controller.layers = layers;
        }

        upperLayer.blendingMode = AnimatorLayerBlendingMode.Override;
        upperLayer.defaultWeight = 0f;
        upperLayer.avatarMask = ResolveUpperBodyMask(upperLayer.avatarMask);
        layers[upperLayerIndex] = upperLayer;
        controller.layers = layers;

        ClearStateMachine(upperSm);

        AnimatorState upperIdleState = upperSm.AddState("UpperBodyIdle", new Vector3(420f, 220f, 0f));
        AnimatorState upperAttackLightState = upperSm.AddState("UpperAttackWeapon", new Vector3(180f, 160f, 0f));
        AnimatorState upperAttackHeavyState = upperSm.AddState("UpperAttackTwoHanded", new Vector3(180f, 280f, 0f));
        AnimatorState upperPunchLeftState = upperSm.AddState("UpperPunchLeft", new Vector3(180f, 40f, 0f));
        AnimatorState upperPunchRightState = upperSm.AddState("UpperPunchRight", new Vector3(180f, -80f, 0f));
        AnimatorState upperMineState = upperSm.AddState("UpperMining", new Vector3(560f, 280f, 0f));
        AnimatorState upperChopState = upperSm.AddState("UpperChop", new Vector3(560f, 160f, 0f));
        AnimatorState upperAimState = upperSm.AddState("ARAim", new Vector3(800f, 160f, 0f));
        AnimatorState upperShootState = upperSm.AddState("Shoot", new Vector3(960f, 240f, 0f));
        AnimatorState upperReloadState = upperSm.AddState("ARReload", new Vector3(960f, 80f, 0f));

        upperIdleState.motion = idle;
        upperAttackLightState.motion = attackLight;
        upperAttackHeavyState.motion = attackHeavy;
        upperPunchLeftState.motion = punchLeft;
        upperPunchRightState.motion = punchRight;
        upperMineState.motion = mine;
        upperChopState.motion = chopLikeSword;
        upperAimState.motion = aim;
        upperShootState.motion = shoot;
        upperReloadState.motion = reload;

        upperAttackLightState.tag = "Action";
        upperAttackHeavyState.tag = "Action";
        upperPunchLeftState.tag = "Action";
        upperPunchRightState.tag = "Action";
        upperMineState.tag = "Action";
        upperChopState.tag = "Action";
        upperAimState.tag = "Action";
        upperShootState.tag = "Action";
        upperReloadState.tag = "Action";

        upperSm.defaultState = upperIdleState;

        AddAnyTriggerTransition(upperSm, upperAttackLightState, "Attack", 0.02f);
        AddAnyTriggerTransition(upperSm, upperAttackHeavyState, "AttackHeavy", 0.02f);
        AddAnyTriggerTransition(upperSm, upperPunchLeftState, "PunchLeft", 0.02f);
        AddAnyTriggerTransition(upperSm, upperPunchRightState, "PunchRight", 0.02f);
        AddAnyTriggerTransition(upperSm, upperMineState, "Mine", 0.02f);
        AddAnyTriggerTransition(upperSm, upperChopState, "Swing", 0.02f);
        AddAnyTriggerTransition(upperSm, upperShootState, "GunShoot", 0.02f);
        AddAnyTriggerTransition(upperSm, upperReloadState, "GunReload", 0.02f);

        AddBoolTransition(upperAimState, upperIdleState, "GunEquipped", false, false, 0.05f);
        AddBoolTransition(upperIdleState, upperAimState, "GunEquipped", true, false, 0.05f);

        AddExitTimeTransition(upperAttackLightState, upperIdleState, 1f, 0.05f);
        AddExitTimeTransition(upperAttackHeavyState, upperIdleState, 1f, 0.05f);
        AddExitTimeTransition(upperPunchLeftState, upperIdleState, 1f, 0.05f);
        AddExitTimeTransition(upperPunchRightState, upperIdleState, 1f, 0.05f);
        AddExitTimeTransition(upperMineState, upperIdleState, 1f, 0.05f);
        AddExitTimeTransition(upperChopState, upperIdleState, 1f, 0.05f);
        AddExitTimeTransition(upperShootState, upperAimState, 1f, 0.03f);
        AddExitTimeTransition(upperReloadState, upperAimState, 1f, 0.03f);
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

        // Enable only upper-body humanoid parts so base layer keeps leg/root motion.
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

    private static void ClearStateMachine(AnimatorStateMachine sm)
    {
        for (int i = sm.anyStateTransitions.Length - 1; i >= 0; i--)
        {
            sm.RemoveAnyStateTransition(sm.anyStateTransitions[i]);
        }

        for (int i = sm.entryTransitions.Length - 1; i >= 0; i--)
        {
            sm.RemoveEntryTransition(sm.entryTransitions[i]);
        }

        for (int i = sm.states.Length - 1; i >= 0; i--)
        {
            sm.RemoveState(sm.states[i].state);
        }

        for (int i = sm.stateMachines.Length - 1; i >= 0; i--)
        {
            sm.RemoveStateMachine(sm.stateMachines[i].stateMachine);
        }
    }

    private static void AddAnyBoolTransition(AnimatorStateMachine sm, AnimatorState to, string parameterName, bool value, float duration)
    {
        AnimatorStateTransition transition = sm.AddAnyStateTransition(to);
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
        float duration,
        string extraConditionName = null,
        bool? extraConditionValue = null)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = hasExitTime;
        transition.duration = duration;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, conditionName);

        if (!string.IsNullOrEmpty(extraConditionName) && extraConditionValue.HasValue)
        {
            transition.AddCondition(
                extraConditionValue.Value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                extraConditionName);
        }
    }
}
