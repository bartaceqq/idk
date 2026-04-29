using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainMenuSceneRepair
{
    private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    private static bool autoRepairAttempted;

    [InitializeOnLoadMethod]
    private static void AutoRepairOpenMainMenuScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        if (autoRepairAttempted)
        {
            return;
        }

        autoRepairAttempted = true;
        EditorApplication.delayCall += TryAutoRepair;
    }

    [MenuItem("Tools/One More Night/Repair Main Menu Scene")]
    public static void RepairMainMenuScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Main menu scene repair is disabled during Play Mode.");
            return;
        }

        RebuildSceneKeepingContext();
    }

    private static void TryAutoRepair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.path != MenuScenePath)
        {
            return;
        }

        if (!NeedsReferenceLayout(activeScene))
        {
            return;
        }

        RebuildSceneKeepingContext();
    }

    private static bool NeedsReferenceLayout(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return false;
        }

        bool hasMenuShell = HasObject(scene, "Menu Shell");
        bool hasMainTitleText = HasObject(scene, "Main Title");
        bool hasSettingsTabs = HasObject(scene, "Settings Tabs");
        bool hasMainButtonColumn = HasObject(scene, "Main Button Column");

        return !(hasMenuShell && hasMainTitleText && hasSettingsTabs && hasMainButtonColumn);
    }

    private static void RebuildSceneKeepingContext()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene previousScene = EditorSceneManager.GetActiveScene();
        string previousScenePath = previousScene.path;

        MainMenuSceneBuilder.Build();

        if (!string.IsNullOrWhiteSpace(previousScenePath) &&
            previousScenePath != MenuScenePath &&
            File.Exists(previousScenePath))
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        }

        Debug.Log("Main menu rebuilt with low-poly right-aligned layout.");
    }

    private static bool HasObject(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (FindByName(roots[i].transform, objectName) != null)
            {
                return true;
            }
        }

        return false;
    }

    private static Transform FindByName(Transform root, string objectName)
    {
        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindByName(root.GetChild(i), objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
