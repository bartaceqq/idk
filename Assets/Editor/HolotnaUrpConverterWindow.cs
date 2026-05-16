using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class HolotnaUrpConverterWindow : EditorWindow
{
    private const string PackRoot = "Assets/Environment_MountainPack_Holotna/Mountain";
    private const string MaterialsFolder = "Assets/Environment_MountainPack_Holotna/Mountain/Materials";
    private const string UrpPackagePath = "Assets/Environment_MountainPack_Holotna/Mountain/_URP/URP.unitypackage";

    [MenuItem("Tools/Holotna/URP Converter")]
    public static void Open()
    {
        GetWindow<HolotnaUrpConverterWindow>("Holotna URP Converter");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Holotna Mountain", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Use this tool to import the URP package and fallback-convert pink materials to URP/Lit.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(!Directory.Exists(PackRoot)))
        {
            if (GUILayout.Button("1) Import URP Package", GUILayout.Height(30)))
            {
                ImportUrpPackage();
            }

            if (GUILayout.Button("2) Convert Materials To URP/Lit Fallback", GUILayout.Height(30)))
            {
                ConvertMaterialsToUrpFallback();
            }

            if (GUILayout.Button("Run Both Steps", GUILayout.Height(34)))
            {
                ImportUrpPackage();
                ConvertMaterialsToUrpFallback();
            }
        }

        if (!Directory.Exists(PackRoot))
        {
            EditorGUILayout.HelpBox("Holotna pack not found at Assets/Environment_MountainPack_Holotna/Mountain.", MessageType.Warning);
        }
    }

    private static void ImportUrpPackage()
    {
        if (!File.Exists(UrpPackagePath))
        {
            EditorUtility.DisplayDialog("Holotna URP Converter", "URP package not found:\n" + UrpPackagePath, "OK");
            return;
        }

        AssetDatabase.ImportPackage(UrpPackagePath, false);
        AssetDatabase.Refresh();
        Debug.Log("[Holotna URP Converter] Imported URP package.");
    }

    private static void ConvertMaterialsToUrpFallback()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog(
                "Holotna URP Converter",
                "URP/Lit shader not found. Make sure URP is installed and active.",
                "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { MaterialsFolder });
        int converted = 0;

        foreach (string guid in guids)
        {
            string matPath = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                continue;
            }

            if (!ShouldConvert(mat))
            {
                continue;
            }

            Dictionary<string, Texture> textures = CaptureTextures(mat);
            Color color = CaptureColor(mat);

            Undo.RecordObject(mat, "Convert material to URP Lit");
            mat.shader = urpLit;

            if (mat.HasProperty("_BaseMap"))
            {
                Texture baseMap = FirstTexture(textures, "_BaseMap", "_MainTex", "Albedo", "BaseColorMap", "_BaseColorMap");
                if (baseMap != null)
                {
                    mat.SetTexture("_BaseMap", baseMap);
                }
            }

            if (mat.HasProperty("_BumpMap"))
            {
                Texture normal = FirstTexture(textures, "_BumpMap", "Normal", "_NormalMap");
                if (normal != null)
                {
                    mat.SetTexture("_BumpMap", normal);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }

            EditorUtility.SetDirty(mat);
            converted++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Holotna URP Converter] Converted {converted} materials to URP/Lit fallback.");
        EditorUtility.DisplayDialog("Holotna URP Converter", $"Converted {converted} materials.", "OK");
    }

    private static bool ShouldConvert(Material mat)
    {
        if (mat.shader == null)
        {
            return true;
        }

        string shaderName = mat.shader.name;
        if (shaderName.StartsWith("Universal Render Pipeline/"))
        {
            return false;
        }

        if (!mat.shader.isSupported)
        {
            return true;
        }

        return shaderName.StartsWith("Shader Graphs/");
    }

    private static Dictionary<string, Texture> CaptureTextures(Material mat)
    {
        Dictionary<string, Texture> textures = new Dictionary<string, Texture>();
        foreach (string prop in mat.GetTexturePropertyNames())
        {
            textures[prop] = mat.GetTexture(prop);
        }

        return textures;
    }

    private static Color CaptureColor(Material mat)
    {
        string[] candidates = { "_BaseColor", "_Color", "Color", "PrimaryColor" };
        foreach (string key in candidates)
        {
            if (mat.HasProperty(key))
            {
                return mat.GetColor(key);
            }
        }

        return Color.white;
    }

    private static Texture FirstTexture(Dictionary<string, Texture> textures, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (textures.TryGetValue(key, out Texture texture) && texture != null)
            {
                return texture;
            }
        }

        return null;
    }
}
