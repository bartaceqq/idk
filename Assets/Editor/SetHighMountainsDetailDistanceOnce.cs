using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SetHighMountainsDetailDistanceOnce
{
    private const string TargetName = "HighMountains";
    private const float TargetDistance = 100f;
    private const string MarkerPath = "Temp/SetHighMountainsDetailDistanceOnce.done";
    private const string LogPath = "Temp/SetHighMountainsDetailDistanceOnce.log";

    [InitializeOnLoadMethod]
    private static void ScheduleApply()
    {
        EditorApplication.delayCall -= Apply;
        EditorApplication.delayCall += Apply;
    }

    private static void Apply()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Apply;
            return;
        }

        try
        {
            ApplyQualitySettings();

            Terrain target = FindTerrain(TargetName);
            if (target == null)
            {
                WriteResult("Could not find loaded Terrain named HighMountains.");
                Debug.LogWarning("SetHighMountainsDetailDistanceOnce: Could not find loaded Terrain named HighMountains.");
                return;
            }

            Undo.RecordObject(target, "Set HighMountains Detail Distance");
            target.detailObjectDistance = TargetDistance;
            EditorUtility.SetDirty(target);
            EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
            SceneView.RepaintAll();

            string message = $"Set {target.name} detailObjectDistance to {target.detailObjectDistance:0.###} in loaded scene {target.gameObject.scene.path}. Terrain quality detail-distance override was disabled for the current editor session.";
            WriteResult(message);
            Debug.Log("SetHighMountainsDetailDistanceOnce: " + message);
        }
        catch (Exception ex)
        {
            WriteResult(ex.ToString());
            Debug.LogException(ex);
        }
    }

    private static void ApplyQualitySettings()
    {
        PropertyInfo overrideProperty = typeof(QualitySettings).GetProperty("terrainQualityOverrides", BindingFlags.Public | BindingFlags.Static);
        if (overrideProperty != null && overrideProperty.CanWrite)
        {
            object disabledValue = overrideProperty.PropertyType.IsEnum
                ? Enum.ToObject(overrideProperty.PropertyType, 0)
                : Convert.ChangeType(0, overrideProperty.PropertyType);
            overrideProperty.SetValue(null, disabledValue, null);
        }

        PropertyInfo distanceProperty = typeof(QualitySettings).GetProperty("terrainDetailDistance", BindingFlags.Public | BindingFlags.Static);
        if (distanceProperty != null && distanceProperty.CanWrite)
        {
            distanceProperty.SetValue(null, TargetDistance, null);
        }
    }

    private static Terrain FindTerrain(string terrainName)
    {
        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            Scene scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded)
            {
                continue;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
            {
                Terrain[] terrains = roots[rootIndex].GetComponentsInChildren<Terrain>(true);
                for (int terrainIndex = 0; terrainIndex < terrains.Length; terrainIndex++)
                {
                    Terrain terrain = terrains[terrainIndex];
                    if (terrain != null && terrain.name == terrainName)
                    {
                        return terrain;
                    }
                }
            }
        }

        return null;
    }

    private static void WriteResult(string message)
    {
        Directory.CreateDirectory("Temp");
        File.WriteAllText(LogPath, message);
        File.WriteAllText(MarkerPath, DateTime.Now.ToString("O"));
    }
}
