using UnityEditor;
using UnityEngine;

public class TerrainLayerCopyTool : EditorWindow
{
    private TerrainData sourceTerrain;
    private TerrainData targetTerrain;

    private bool copyTerrainLayers = true;
    private bool copySplatmaps = true;
    private bool copyGrassSettings = true;

    [MenuItem("Tools/Terrain/Copy Terrain Layers")]
    private static void Open()
    {
        GetWindow<TerrainLayerCopyTool>("Copy Terrain Layers");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Source", EditorStyles.boldLabel);
        sourceTerrain = (TerrainData)EditorGUILayout.ObjectField(
            "TerrainData",
            sourceTerrain,
            typeof(TerrainData),
            false);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
        targetTerrain = (TerrainData)EditorGUILayout.ObjectField(
            "TerrainData",
            targetTerrain,
            typeof(TerrainData),
            false);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Copy Options", EditorStyles.boldLabel);
        copyTerrainLayers = EditorGUILayout.Toggle("Terrain Layers", copyTerrainLayers);
        copySplatmaps = EditorGUILayout.Toggle("Texture Painting (Splatmaps)", copySplatmaps);
        copyGrassSettings = EditorGUILayout.Toggle("Grass Settings", copyGrassSettings);

        EditorGUILayout.Space();

        using (new EditorGUI.DisabledScope(sourceTerrain == null || targetTerrain == null))
        {
            if (GUILayout.Button("Copy"))
            {
                CopySettings();
            }
        }
    }

    private void CopySettings()
    {
        if (sourceTerrain == null || targetTerrain == null)
        {
            Debug.LogError("Assign both source and target TerrainData assets.");
            return;
        }

        Undo.RegisterCompleteObjectUndo(targetTerrain, "Copy Terrain Layers");

        if (copyTerrainLayers)
        {
            targetTerrain.terrainLayers = sourceTerrain.terrainLayers;
        }

        if (copySplatmaps)
        {
            CopySplatmaps();
        }

        if (copyGrassSettings)
        {
            targetTerrain.wavingGrassTint = sourceTerrain.wavingGrassTint;
            targetTerrain.wavingGrassStrength = sourceTerrain.wavingGrassStrength;
            targetTerrain.wavingGrassAmount = sourceTerrain.wavingGrassAmount;
            targetTerrain.wavingGrassSpeed = sourceTerrain.wavingGrassSpeed;
        }

        EditorUtility.SetDirty(targetTerrain);
        AssetDatabase.SaveAssets();
        Debug.Log("Terrain settings copied.");
    }

    private void CopySplatmaps()
    {
        if (sourceTerrain.terrainLayers == null || sourceTerrain.terrainLayers.Length == 0)
        {
            Debug.LogWarning("Source terrain has no Terrain Layers assigned.");
            return;
        }

        if (targetTerrain.terrainLayers == null || targetTerrain.terrainLayers.Length == 0)
        {
            Debug.LogWarning("Target terrain has no Terrain Layers assigned.");
            return;
        }

        if (sourceTerrain.terrainLayers.Length != targetTerrain.terrainLayers.Length)
        {
            Debug.LogWarning("Terrain layer count mismatch. Copy Terrain Layers first, then copy splatmaps.");
            return;
        }

        if (targetTerrain.alphamapResolution != sourceTerrain.alphamapResolution)
        {
            targetTerrain.alphamapResolution = sourceTerrain.alphamapResolution;
        }

        targetTerrain.baseMapResolution = sourceTerrain.baseMapResolution;

        float[,,] splats = sourceTerrain.GetAlphamaps(
            0,
            0,
            sourceTerrain.alphamapWidth,
            sourceTerrain.alphamapHeight);

        targetTerrain.SetAlphamaps(0, 0, splats);
    }
}
