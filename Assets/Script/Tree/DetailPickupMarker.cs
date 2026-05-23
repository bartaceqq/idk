using UnityEngine;

// Marks a spawned pickup as originating from a Terrain detail cell.
public class DetailPickupMarker : MonoBehaviour
{
    private Terrain terrain;
    private int detailIndex;
    private int cellX;
    private int cellZ;
    private bool suppressClear;
    private TerrainDetailPickupSpawner spawner;

    public void Initialize(Terrain sourceTerrain, int detailPrototypeIndex, int detailCellX, int detailCellZ, TerrainDetailPickupSpawner owner)
    {
        terrain = sourceTerrain;
        detailIndex = detailPrototypeIndex;
        cellX = detailCellX;
        cellZ = detailCellZ;
        spawner = owner;
        suppressClear = false;
    }

    public void MarkCollected()
    {
        if (terrain == null || terrain.terrainData == null)
        {
            return;
        }

        int[,] layer = terrain.terrainData.GetDetailLayer(cellX, cellZ, 1, 1, detailIndex);
        if (layer != null && layer.Length > 0)
        {
            layer[0, 0] = Mathf.Max(0, layer[0, 0] - 1);
            terrain.terrainData.SetDetailLayer(cellX, cellZ, detailIndex, layer);
        }

        spawner?.NotifyCollected(cellX, cellZ);
    }

    public void Despawn()
    {
        suppressClear = true;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (suppressClear)
        {
            return;
        }
    }
}
