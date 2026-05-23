using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PrepReworkMovementAnimationSync
{
    private const string SourceFolder = "Assets/Animations/PrepReWork";
    private const string DestinationFolder = "Assets/Animations/PrepReWork/AnimsOnly";
    private const string ControllerPath = DestinationFolder + "/RemadeController.controller";
    private const string PendingSyncFlagRelativePath = "Temp/Codex_PrepReworkMovementSync.flag";
    private const float PositionCurveRepairThreshold = 10f;
    private const float PositionCurveRepairScale = 0.01f;

    private static readonly string[] RightStrafeClipPaths =
    {
        DestinationFolder + "/RightStrafeWalkling.anim",
        DestinationFolder + "/RightStrafeWalking.anim",
    };

    private static string PendingSyncFlagPath
    {
        get
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, PendingSyncFlagRelativePath);
        }
    }

    private static readonly ClipImportDefinition[] ClipImports =
    {
        new("Left Strafe Walking (2).fbx", "LeftStrafeWalking", DestinationFolder + "/LeftStrafeWalking.anim", true, 0.01f),
        new("Right Strafe Walking (1).fbx", "RightStrafeWalking", DestinationFolder + "/RightStrafeWalking.anim", true, 0.01f, RightStrafeClipPaths[0]),
        new("Walking Backwards (2).fbx", "WalkingBackwards", DestinationFolder + "/WalkingBackwards.anim", true, 0.01f),
        new("Running Jump (1).fbx", "RunningJump", DestinationFolder + "/RunningJump.anim", false, 0.01f),
    };

    private static readonly ExtractedClipDefinition[] ExtractedClipRepairs =
    {
        new(new[] { DestinationFolder + "/LeftStrafeWalking.anim" }, true),
        new(RightStrafeClipPaths, true),
        new(new[] { DestinationFolder + "/WalkingBackwards.anim" }, true),
        new(new[] { DestinationFolder + "/RunningJump.anim" }, false),
        new(new[] { DestinationFolder + "/Emote.anim" }, true),
    };

    [InitializeOnLoadMethod]
    private static void RegisterPendingSyncWatcher()
    {
        EditorApplication.update -= SyncIfRequestedByFlag;
        EditorApplication.update += SyncIfRequestedByFlag;
    }

    private static void SyncIfRequestedByFlag()
    {
        if (!File.Exists(PendingSyncFlagPath))
        {
            return;
        }

        try
        {
            File.Delete(PendingSyncFlagPath);
        }
        catch (IOException ioException)
        {
            Debug.LogWarning($"Could not delete sync flag '{PendingSyncFlagRelativePath}': {ioException.Message}");
        }

        Sync();
    }

    [MenuItem("Tools/Animation/Sync PrepReWork Movement Clips", false, 1000)]
    public static void Sync()
    {
        bool changedAnyClip = false;

        for (int i = 0; i < ClipImports.Length; i++)
        {
            changedAnyClip |= SyncClipAsset(ClipImports[i]);
        }

        for (int i = 0; i < ExtractedClipRepairs.Length; i++)
        {
            changedAnyClip |= RepairExtractedClipAsset(ExtractedClipRepairs[i]);
        }

        RebuildMaleAnimatorController.Rebuild();
        EnsureDedicatedMotionsOnController();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(changedAnyClip
            ? "PrepReWork movement clips synced and controller updated."
            : "PrepReWork movement clips were already in sync; controller synced.");
    }

    public static void RunFromBatchMode()
    {
        Sync();
    }

    private static bool SyncClipAsset(ClipImportDefinition definition)
    {
        AnimationClip existingTargetClip = LoadFirstExistingClip(CombineClipPaths(definition.TargetClipPath, definition.AdditionalTargetClipPaths));
        if (existingTargetClip != null)
        {
            bool loopChanged = ApplyLoopTime(existingTargetClip, definition.LoopTime);
            if (loopChanged)
            {
                EditorUtility.SetDirty(existingTargetClip);
            }

            return loopChanged;
        }

        string sourcePath = $"{SourceFolder}/{definition.SourceFileName}";
        if (AssetDatabase.LoadMainAssetAtPath(sourcePath) == null)
        {
            Debug.LogWarning($"PrepReWork clip sync skipped missing source FBX: {sourcePath}");
            return false;
        }

        ModelImporter importer = AssetImporter.GetAtPath(sourcePath) as ModelImporter;
        if (importer == null)
        {
            Debug.LogWarning($"PrepReWork clip sync could not read importer for: {sourcePath}");
            return false;
        }

        bool importerChanged = ConfigureImporter(importer, definition.TargetClipName, definition.LoopTime, definition.GlobalScale);
        if (importerChanged)
        {
            importer.SaveAndReimport();
        }

        AnimationClip sourceClip = LoadImportedClip(sourcePath, definition.TargetClipName);
        if (sourceClip == null)
        {
            Debug.LogWarning($"PrepReWork clip sync could not find animation clip in: {sourcePath}");
            return false;
        }

        AnimationClip targetClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(definition.TargetClipPath);
        bool createdAsset = false;
        if (targetClip == null)
        {
            targetClip = Object.Instantiate(sourceClip);
            targetClip.name = definition.TargetClipName;
            AssetDatabase.CreateAsset(targetClip, definition.TargetClipPath);
            createdAsset = true;
        }
        else
        {
            EditorUtility.CopySerialized(sourceClip, targetClip);
            targetClip.name = definition.TargetClipName;
        }

        ApplyLoopTime(targetClip, definition.LoopTime);
        EditorUtility.SetDirty(targetClip);

        return importerChanged || createdAsset;
    }

    private static string[] CombineClipPaths(string primaryPath, params string[] additionalPaths)
    {
        int additionalCount = additionalPaths?.Length ?? 0;
        string[] combined = new string[additionalCount + 1];
        combined[0] = primaryPath;
        for (int i = 0; i < additionalCount; i++)
        {
            combined[i + 1] = additionalPaths[i];
        }

        return combined;
    }

    private static bool RepairExtractedClipAsset(ExtractedClipDefinition definition)
    {
        AnimationClip clip = LoadFirstExistingClip(definition.CandidateClipPaths);
        if (clip == null)
        {
            return false;
        }

        bool changed = false;
        if (NeedsPositionCurveRepair(clip))
        {
            changed |= ScalePositionCurves(clip, PositionCurveRepairScale);
        }

        changed |= ApplyLoopTime(clip, definition.LoopTime);
        if (changed)
        {
            EditorUtility.SetDirty(clip);
        }

        return changed;
    }

    private static bool ConfigureImporter(ModelImporter importer, string clipName, bool loopTime, float globalScale)
    {
        bool changed = false;

        if (!Mathf.Approximately(importer.globalScale, globalScale))
        {
            importer.globalScale = globalScale;
            changed = true;
        }

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = importer.defaultClipAnimations;
        }

        if (clips == null || clips.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i].name != clipName)
            {
                clips[i].name = clipName;
                changed = true;
            }

            if (clips[i].loopTime != loopTime)
            {
                clips[i].loopTime = loopTime;
                changed = true;
            }
        }

        if (changed)
        {
            importer.clipAnimations = clips;
        }

        return changed;
    }

    private static AnimationClip LoadImportedClip(string sourcePath, string clipName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(sourcePath);
        for (int i = 0; i < assets.Length; i++)
        {
            AnimationClip clip = assets[i] as AnimationClip;
            if (clip == null || clip.name.StartsWith("__preview__"))
            {
                continue;
            }

            if (clip.name == clipName)
            {
                return clip;
            }
        }

        return assets.OfType<AnimationClip>().FirstOrDefault(clip => clip != null && !clip.name.StartsWith("__preview__"));
    }

    private static AnimationClip LoadFirstExistingClip(params string[] assetPaths)
    {
        if (assetPaths == null)
        {
            return null;
        }

        for (int i = 0; i < assetPaths.Length; i++)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPaths[i]);
            if (clip != null)
            {
                return clip;
            }
        }

        return null;
    }

    private static bool ApplyLoopTime(AnimationClip clip, bool loopTime)
    {
        if (clip == null)
        {
            return false;
        }

        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty settings = serializedClip.FindProperty("m_AnimationClipSettings");
        if (settings == null)
        {
            return false;
        }

        SerializedProperty loopProperty = settings.FindPropertyRelative("m_LoopTime");
        if (loopProperty == null)
        {
            return false;
        }

        if (loopProperty.boolValue == loopTime)
        {
            return false;
        }

        loopProperty.boolValue = loopTime;
        serializedClip.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    private static bool NeedsPositionCurveRepair(AnimationClip clip)
    {
        return GetLargestPositionMagnitude(clip) > PositionCurveRepairThreshold;
    }

    private static float GetLargestPositionMagnitude(AnimationClip clip)
    {
        if (clip == null)
        {
            return 0f;
        }

        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty positionCurves = serializedClip.FindProperty("m_PositionCurves");
        if (positionCurves == null || !positionCurves.isArray)
        {
            return 0f;
        }

        float maxMagnitude = 0f;
        for (int curveIndex = 0; curveIndex < positionCurves.arraySize; curveIndex++)
        {
            SerializedProperty keyframes = GetPositionCurveKeyframes(positionCurves.GetArrayElementAtIndex(curveIndex));
            if (keyframes == null || !keyframes.isArray)
            {
                continue;
            }

            for (int keyIndex = 0; keyIndex < keyframes.arraySize; keyIndex++)
            {
                SerializedProperty value = keyframes.GetArrayElementAtIndex(keyIndex).FindPropertyRelative("value");
                maxMagnitude = Mathf.Max(maxMagnitude, GetMaxAbsVector3(value));
            }
        }

        return maxMagnitude;
    }

    private static bool ScalePositionCurves(AnimationClip clip, float scale)
    {
        if (clip == null || Mathf.Approximately(scale, 1f))
        {
            return false;
        }

        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty positionCurves = serializedClip.FindProperty("m_PositionCurves");
        if (positionCurves == null || !positionCurves.isArray)
        {
            return false;
        }

        bool changed = false;
        for (int curveIndex = 0; curveIndex < positionCurves.arraySize; curveIndex++)
        {
            SerializedProperty keyframes = GetPositionCurveKeyframes(positionCurves.GetArrayElementAtIndex(curveIndex));
            if (keyframes == null || !keyframes.isArray)
            {
                continue;
            }

            for (int keyIndex = 0; keyIndex < keyframes.arraySize; keyIndex++)
            {
                SerializedProperty keyframe = keyframes.GetArrayElementAtIndex(keyIndex);
                changed |= ScaleVector3Property(keyframe.FindPropertyRelative("value"), scale);
                changed |= ScaleVector3Property(keyframe.FindPropertyRelative("inSlope"), scale);
                changed |= ScaleVector3Property(keyframe.FindPropertyRelative("outSlope"), scale);
            }
        }

        if (changed)
        {
            serializedClip.ApplyModifiedPropertiesWithoutUndo();
        }

        return changed;
    }

    private static SerializedProperty GetPositionCurveKeyframes(SerializedProperty positionCurve)
    {
        SerializedProperty curve = positionCurve?.FindPropertyRelative("curve");
        return curve?.FindPropertyRelative("m_Curve");
    }

    private static float GetMaxAbsVector3(SerializedProperty vectorProperty)
    {
        if (vectorProperty == null)
        {
            return 0f;
        }

        SerializedProperty x = vectorProperty.FindPropertyRelative("x");
        SerializedProperty y = vectorProperty.FindPropertyRelative("y");
        SerializedProperty z = vectorProperty.FindPropertyRelative("z");
        if (x == null || y == null || z == null)
        {
            return 0f;
        }

        return Mathf.Max(Mathf.Abs(x.floatValue), Mathf.Abs(y.floatValue), Mathf.Abs(z.floatValue));
    }

    private static bool ScaleVector3Property(SerializedProperty vectorProperty, float scale)
    {
        if (vectorProperty == null)
        {
            return false;
        }

        SerializedProperty x = vectorProperty.FindPropertyRelative("x");
        SerializedProperty y = vectorProperty.FindPropertyRelative("y");
        SerializedProperty z = vectorProperty.FindPropertyRelative("z");
        if (x == null || y == null || z == null)
        {
            return false;
        }

        bool changed = false;
        changed |= ScaleFloatProperty(x, scale);
        changed |= ScaleFloatProperty(y, scale);
        changed |= ScaleFloatProperty(z, scale);
        return changed;
    }

    private static bool ScaleFloatProperty(SerializedProperty property, float scale)
    {
        if (property == null)
        {
            return false;
        }

        float scaledValue = property.floatValue * scale;
        if (Mathf.Approximately(property.floatValue, scaledValue))
        {
            return false;
        }

        property.floatValue = scaledValue;
        return true;
    }

    private static void EnsureDedicatedMotionsOnController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null || controller.layers == null || controller.layers.Length == 0)
        {
            return;
        }

        AnimatorStateMachine baseStateMachine = controller.layers[0].stateMachine;
        if (baseStateMachine == null)
        {
            return;
        }

        SetStateMotion(baseStateMachine, "WalkingLeft", AssetDatabase.LoadAssetAtPath<AnimationClip>(DestinationFolder + "/LeftStrafeWalking.anim"));
        SetStateMotion(baseStateMachine, "WalkingRight", LoadFirstExistingClip(RightStrafeClipPaths));
        SetStateMotion(baseStateMachine, "WalkingBackWards", AssetDatabase.LoadAssetAtPath<AnimationClip>(DestinationFolder + "/WalkingBackwards.anim"));
        SetStateMotion(baseStateMachine, "RunningJump", AssetDatabase.LoadAssetAtPath<AnimationClip>(DestinationFolder + "/RunningJump.anim"));
        SetStateMotion(baseStateMachine, "Emote", AssetDatabase.LoadAssetAtPath<AnimationClip>(DestinationFolder + "/Emote.anim"));

        EditorUtility.SetDirty(controller);
    }

    private static void SetStateMotion(AnimatorStateMachine stateMachine, string stateName, Motion motion)
    {
        if (stateMachine == null || motion == null)
        {
            return;
        }

        ChildAnimatorState[] states = stateMachine.states;
        for (int i = 0; i < states.Length; i++)
        {
            AnimatorState state = states[i].state;
            if (state != null && state.name == stateName)
            {
                state.motion = motion;
                EditorUtility.SetDirty(state);
                return;
            }
        }
    }

    private readonly struct ClipImportDefinition
    {
        public ClipImportDefinition(string sourceFileName, string targetClipName, string targetClipPath, bool loopTime, float globalScale, params string[] additionalTargetClipPaths)
        {
            SourceFileName = sourceFileName;
            TargetClipName = targetClipName;
            TargetClipPath = targetClipPath;
            LoopTime = loopTime;
            GlobalScale = globalScale;
            AdditionalTargetClipPaths = additionalTargetClipPaths;
        }

        public string SourceFileName { get; }
        public string TargetClipName { get; }
        public string TargetClipPath { get; }
        public bool LoopTime { get; }
        public float GlobalScale { get; }
        public string[] AdditionalTargetClipPaths { get; }
    }

    private readonly struct ExtractedClipDefinition
    {
        public ExtractedClipDefinition(string[] candidateClipPaths, bool loopTime)
        {
            CandidateClipPaths = candidateClipPaths;
            LoopTime = loopTime;
        }

        public string[] CandidateClipPaths { get; }
        public bool LoopTime { get; }
    }
}
