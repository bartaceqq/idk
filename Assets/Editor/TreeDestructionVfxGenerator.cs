using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TreeDestructionVfxGenerator
{
    private const string VfxFolderPath = "Assets/VFX/Resources";
    private const string VfxPrefabPath = VfxFolderPath + "/VFX_TreeDestructionBurst.prefab";
    private const string SmokeMaterialPath = "Assets/VFXPACK_IMPACT_WALLCOEUR_FreeVersion/01_Material/Smoke 4.mat";
    private const string WoodMaterialPath = "Assets/Props_WoodPack_MyUniverseStudio/Wood Pack/Built-in/Material/Base Material.mat";

    private static readonly string[] WoodMeshPaths =
    {
        "Assets/Props_WoodPack_MyUniverseStudio/Wood Pack/Meshes/Stick_8.fbx",
        "Assets/Props_WoodPack_MyUniverseStudio/Wood Pack/Meshes/Stick_17.fbx",
        "Assets/Props_WoodPack_MyUniverseStudio/Wood Pack/Meshes/Stick_32.fbx",
        "Assets/Props_WoodPack_MyUniverseStudio/Wood Pack/Meshes/Small Log_12.fbx"
    };

    [MenuItem("Tools/VFX/Generate Tree Destruction Burst")]
    public static void GenerateFromMenu()
    {
        GenerateInternal();
    }

    public static void GenerateFromCommandLine()
    {
        GenerateInternal();
    }

    private static void GenerateInternal()
    {
        EnsureFolderExists(VfxFolderPath);

        Material smokeMaterial = AssetDatabase.LoadAssetAtPath<Material>(SmokeMaterialPath);
        Material woodMaterial = AssetDatabase.LoadAssetAtPath<Material>(WoodMaterialPath);
        Mesh[] woodMeshes = LoadWoodMeshes();

        if (smokeMaterial == null)
        {
            smokeMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        }

        if (smokeMaterial == null)
        {
            throw new System.InvalidOperationException("Unable to load a particle material for the tree destruction VFX.");
        }

        GameObject root = new GameObject("VFX_TreeDestructionBurst");
        try
        {
            ConfigureDustCloud(CreateParticleChild(root.transform, "DustCloud"), smokeMaterial);
            ConfigureSawdustSpray(CreateParticleChild(root.transform, "SawdustSpray"), smokeMaterial);

            if (woodMaterial != null && woodMeshes.Length > 0)
            {
                ConfigureWoodChunks(CreateParticleChild(root.transform, "WoodChunks"), woodMaterial, woodMeshes);
            }

            PrefabUtility.SaveAsPrefabAsset(root, VfxPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        AssignVfxToAllCutTreePrefabs(AssetDatabase.LoadAssetAtPath<GameObject>(VfxPrefabPath));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Generated tree destruction VFX at '{VfxPrefabPath}'.");
    }

    private static ParticleSystem CreateParticleChild(Transform parent, string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        return child.AddComponent<ParticleSystem>();
    }

    private static void ConfigureDustCloud(ParticleSystem particleSystem, Material smokeMaterial)
    {
        var main = particleSystem.main;
        main.duration = 2.2f;
        main.loop = false;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.25f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.25f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.9f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.30f, 0.20f, 0.10f, 0.68f),
            new Color(0.48f, 0.34f, 0.18f, 0.8f));
        main.gravityModifier = 0.08f;
        main.maxParticles = 64;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 26, 34) });

        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.45f;

        var noise = particleSystem.noise;
        noise.enabled = true;
        noise.strength = 0.55f;
        noise.frequency = 0.6f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;

        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGradient = new Gradient();
        colorGradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.62f, 0.45f, 0.24f), 0f),
                new GradientColorKey(new Color(0.42f, 0.29f, 0.15f), 0.45f),
                new GradientColorKey(new Color(0.25f, 0.18f, 0.10f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.78f, 0.12f),
                new GradientAlphaKey(0.48f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);

        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.EaseInOut(0f, 0.35f, 1f, 1.3f));

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.material = smokeMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
    }

    private static void ConfigureSawdustSpray(ParticleSystem particleSystem, Material smokeMaterial)
    {
        var main = particleSystem.main;
        main.duration = 1.6f;
        main.loop = false;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.8f, 7.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.58f, 0.42f, 0.20f, 0.85f),
            new Color(0.36f, 0.25f, 0.12f, 0.95f));
        main.gravityModifier = 0.7f;
        main.maxParticles = 120;

        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 45, 60) });

        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 32f;
        shape.radius = 0.18f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;

        var limitVelocity = particleSystem.limitVelocityOverLifetime;
        limitVelocity.enabled = true;
        limitVelocity.dampen = 0.3f;
        limitVelocity.limit = new ParticleSystem.MinMaxCurve(5.5f);

        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGradient = new Gradient();
        colorGradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.62f, 0.46f, 0.22f), 0f),
                new GradientColorKey(new Color(0.43f, 0.30f, 0.14f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.45f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(colorGradient);

        var rotationOverLifetime = particleSystem.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-12f, 12f);

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.material = smokeMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.55f;
        renderer.lengthScale = 0.35f;
        renderer.sortMode = ParticleSystemSortMode.Distance;
    }

    private static void ConfigureWoodChunks(ParticleSystem particleSystem, Material woodMaterial, Mesh[] woodMeshes)
    {
        var main = particleSystem.main;
        main.duration = 1.8f;
        main.loop = false;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.4f, 5.6f);
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = 1.1f;
        main.maxParticles = 24;

        var emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 16) });

        var shape = particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.22f;

        var rotationOverLifetime = particleSystem.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.separateAxes = true;
        rotationOverLifetime.x = new ParticleSystem.MinMaxCurve(-9f, 9f);
        rotationOverLifetime.y = new ParticleSystem.MinMaxCurve(-11f, 11f);
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-13f, 13f);

        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.separateAxes = true;
        sizeOverLifetime.x = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.7f));
        sizeOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.85f));
        sizeOverLifetime.z = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.85f));

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.material = woodMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.meshDistribution = ParticleSystemMeshDistribution.NonUniformRandom;
        renderer.enableGPUInstancing = true;
        renderer.SetMeshes(woodMeshes, woodMeshes.Length);
    }

    private static Mesh[] LoadWoodMeshes()
    {
        List<Mesh> meshes = new List<Mesh>();
        foreach (string path in WoodMeshPaths)
        {
            Mesh mesh = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault();
            if (mesh != null)
            {
                meshes.Add(mesh);
            }
        }

        return meshes.ToArray();
    }

    private static void AssignVfxToAllCutTreePrefabs(GameObject vfxPrefab)
    {
        if (vfxPrefab == null)
        {
            throw new System.InvalidOperationException($"Missing generated VFX prefab at '{VfxPrefabPath}'.");
        }

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        foreach (string prefabGuid in prefabGuids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            bool changed = false;

            try
            {
                MonoBehaviour[] behaviours = prefabRoot.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    MonoBehaviour behaviour = behaviours[i];
                    if (behaviour == null || behaviour.GetType().Name != "CutTree")
                    {
                        continue;
                    }

                    SerializedObject serializedObject = new SerializedObject(behaviour);
                    SerializedProperty vfxProperty = serializedObject.FindProperty("destructionVfxPrefab");
                    if (vfxProperty != null && vfxProperty.objectReferenceValue != vfxPrefab)
                    {
                        vfxProperty.objectReferenceValue = vfxPrefab;
                        serializedObject.ApplyModifiedPropertiesWithoutUndo();
                        changed = true;
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
    }

    private static void EnsureFolderExists(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string currentPath = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string nextPath = $"{currentPath}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, parts[i]);
            }

            currentPath = nextPath;
        }
    }
}
