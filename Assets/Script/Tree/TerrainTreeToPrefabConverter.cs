using System.Collections.Generic;
using UnityEngine;

// Controls Terrain Tree To Prefab Converter behavior.
public class TerrainTreeToPrefabConverter : MonoBehaviour
{
    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private Transform parentForSpawnedTrees;
    [Tooltip("If enabled, converter will only convert resource prefabs (CutTree/MineStone).")]
    [SerializeField] private bool convertOnlyCuttableTrees = true;
    [SerializeField] private bool includeMineableStones = true;
    [SerializeField] private bool clearConvertedTerrainTrees = true;

    [ContextMenu("Convert Painted Trees To Prefabs")]
    // Handle Convert Painted Trees To Prefabs.
    public void ConvertPaintedTreesToPrefabs()
    {
        if (targetTerrain == null)
        {
            targetTerrain = GetComponent<Terrain>();
        }

        if (targetTerrain == null || targetTerrain.terrainData == null)
        {
            Debug.LogWarning("TerrainTreeToPrefabConverter: Missing Terrain reference.", this);
            return;
        }

        if (parentForSpawnedTrees == null)
        {
            GameObject root = new GameObject("ConvertedTerrainTrees");
            root.transform.SetParent(targetTerrain.transform, false);
            parentForSpawnedTrees = root.transform;
        }

        TerrainData terrainData = targetTerrain.terrainData;
        TreePrototype[] prototypes = terrainData.treePrototypes;
        TreeInstance[] treeInstances = terrainData.treeInstances;

        if (treeInstances == null || treeInstances.Length == 0)
        {
            Debug.Log("TerrainTreeToPrefabConverter: No painted terrain trees to convert.", this);
            return;
        }

        List<TreeInstance> remainingInstances = new List<TreeInstance>(treeInstances.Length);
        int convertedCount = 0;

        for (int i = 0; i < treeInstances.Length; i++)
        {
            TreeInstance instance = treeInstances[i];

            if (instance.prototypeIndex < 0 || instance.prototypeIndex >= prototypes.Length)
            {
                remainingInstances.Add(instance);
                continue;
            }

            GameObject prototypePrefab = prototypes[instance.prototypeIndex].prefab;
            if (prototypePrefab == null)
            {
                remainingInstances.Add(instance);
                continue;
            }

            if (convertOnlyCuttableTrees)
            {
                bool hasCutTree = prototypePrefab.GetComponentInChildren<CutTree>(true) != null;
                bool hasMineStone = includeMineableStones && prototypePrefab.GetComponentInChildren<MineStone>(true) != null;

                if (!hasCutTree && !hasMineStone)
                {
                    remainingInstances.Add(instance);
                    continue;
                }
            }

            Vector3 worldPosition = targetTerrain.transform.position + Vector3.Scale(instance.position, terrainData.size);
            Quaternion worldRotation = Quaternion.Euler(0f, instance.rotation * Mathf.Rad2Deg, 0f);

            GameObject spawnedTree = Instantiate(prototypePrefab, worldPosition, worldRotation, parentForSpawnedTrees);

            Vector3 baseScale = spawnedTree.transform.localScale;
            spawnedTree.transform.localScale = new Vector3(
                baseScale.x * instance.widthScale,
                baseScale.y * instance.heightScale,
                baseScale.z * instance.widthScale
            );

            convertedCount++;

            if (!clearConvertedTerrainTrees)
            {
                remainingInstances.Add(instance);
            }
        }

        if (clearConvertedTerrainTrees)
        {
            terrainData.treeInstances = remainingInstances.ToArray();
        }

        Debug.Log($"TerrainTreeToPrefabConverter: Converted {convertedCount} tree instances.", this);
    }
}
