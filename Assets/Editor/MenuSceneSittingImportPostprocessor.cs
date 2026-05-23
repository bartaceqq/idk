using System;
using UnityEditor;

public sealed class MenuSceneSittingImportPostprocessor : AssetPostprocessor
{
    private const string TargetAssetPath = "Assets/Animations/MenuScene/Sitting.fbx";

    void OnPreprocessModel()
    {
        if (!assetPath.Replace('\\', '/').Equals(TargetAssetPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!(assetImporter is ModelImporter importer))
        {
            return;
        }

        var sourceClips = importer.defaultClipAnimations;
        if (sourceClips == null || sourceClips.Length == 0)
        {
            return;
        }

        var updatedClips = new ModelImporterClipAnimation[sourceClips.Length];
        var changed = false;

        for (var i = 0; i < sourceClips.Length; i++)
        {
            var clip = sourceClips[i];

            if (!clip.keepOriginalOrientation ||
                !clip.keepOriginalPositionY ||
                !clip.keepOriginalPositionXZ ||
                clip.heightFromFeet)
            {
                changed = true;
            }

            clip.keepOriginalOrientation = true;
            clip.keepOriginalPositionY = true;
            clip.keepOriginalPositionXZ = true;
            clip.heightFromFeet = false;

            updatedClips[i] = clip;
        }

        if (changed)
        {
            importer.clipAnimations = updatedClips;
        }
    }
}
