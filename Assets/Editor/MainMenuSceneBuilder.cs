using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MainMenuSceneBuilder
{
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameplayScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/One More Night/Rebuild Main Menu Scene")]
    public static void Build()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.031f, 0.025f);
        camera.fieldOfView = 48f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(0f, 3.2f, -9.5f);
        cameraObject.transform.rotation = Quaternion.Euler(12f, 0f, 0f);

        GameObject lightObject = new GameObject("Menu Key Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.78f, 0.87f, 1f);
        light.intensity = 1.05f;
        lightObject.transform.rotation = Quaternion.Euler(42f, -28f, 0f);

        GameObject controller = new GameObject("Fantasy Menu Controller");
        controller.AddComponent<FantasyMenuController>();

        GameObject settingsBootstrapper = new GameObject("Game Settings Bootstrapper");
        settingsBootstrapper.AddComponent<GameSettingsBootstrapper>();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), MenuScenePath);
        EnsureBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
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
