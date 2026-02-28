#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DigitalRuby.RainMaker
{
    public static class RainMakerUrpConverter
    {
        private const string PrefabFolder = "Assets/RainMaker/Prefab";
        private static readonly string[] MaterialPaths =
        {
            "Assets/RainMaker/Prefab/RainMaterial.mat",
            "Assets/RainMaker/Prefab/RainExplosionMaterial.mat",
            "Assets/RainMaker/Prefab/RainMistMaterial.mat",
            "Assets/RainMaker/Prefab/RainMaterial2D.mat",
            "Assets/RainMaker/Prefab/RainExplosionMaterial2D.mat",
            "Assets/RainMaker/Prefab/RainMistMaterial2D.mat"
        };

        [MenuItem("Tools/RainMaker/Convert Materials To URP")]
        public static void ConvertMaterialsToUrp()
        {
            Shader urpParticleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (urpParticleShader == null)
            {
                Debug.LogError("RainMaker: URP Particles Unlit shader was not found. Ensure URP is installed and active.");
                return;
            }

            int convertedCount = 0;
            foreach (string materialPath in MaterialPaths)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    Debug.LogWarning($"RainMaker: Could not find material at '{materialPath}'.");
                    continue;
                }

                Texture mainTexture = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : material.mainTexture;
                Color baseColor = GetBestColor(material);

                Undo.RecordObject(material, "Convert RainMaker Material To URP");
                material.shader = urpParticleShader;

                if (mainTexture != null)
                {
                    SetTextureIfExists(material, "_BaseMap", mainTexture);
                    SetTextureIfExists(material, "_MainTex", mainTexture);
                }

                SetColorIfExists(material, "_BaseColor", baseColor);
                SetColorIfExists(material, "_Color", baseColor);

                SetFloatIfExists(material, "_Surface", 1.0f);
                SetFloatIfExists(material, "_Blend", 0.0f);
                SetFloatIfExists(material, "_SrcBlend", (float)BlendMode.SrcAlpha);
                SetFloatIfExists(material, "_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                SetFloatIfExists(material, "_SrcBlendAlpha", (float)BlendMode.One);
                SetFloatIfExists(material, "_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                SetFloatIfExists(material, "_ZWrite", 0.0f);
                SetFloatIfExists(material, "_AlphaClip", 0.0f);

                material.renderQueue = (int)RenderQueue.Transparent;

                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

                EditorUtility.SetDirty(material);
                convertedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"RainMaker: Converted {convertedCount} material(s) to URP Particles/Unlit.");
        }

        [MenuItem("Tools/RainMaker/Convert Materials To URP", true)]
        private static bool ValidateConvertMaterialsToUrp()
        {
            return Directory.Exists(PrefabFolder);
        }

        private static Color GetBestColor(Material material)
        {
            if (material.HasProperty("_TintColor"))
            {
                return material.GetColor("_TintColor");
            }

            if (material.HasProperty("_Color"))
            {
                return material.GetColor("_Color");
            }

            return Color.white;
        }

        private static void SetTextureIfExists(Material material, string propertyName, Texture value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetTexture(propertyName, value);
            }
        }

        private static void SetColorIfExists(Material material, string propertyName, Color value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetColor(propertyName, value);
            }
        }

        private static void SetFloatIfExists(Material material, string propertyName, float value)
        {
            if (material.HasProperty(propertyName))
            {
                material.SetFloat(propertyName, value);
            }
        }
    }
}
#endif
