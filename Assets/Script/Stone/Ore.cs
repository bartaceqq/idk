using UnityEngine;

[System.Serializable]
// Controls Ore behavior.
public class Ore
{
    public string oreName;
    public Material material;
    public Sprite sprite;

    public Ore(string oreName, Material material, Sprite sprite)
    {
        this.oreName = oreName;
        this.material = material;
        this.sprite = sprite;
    }
}
