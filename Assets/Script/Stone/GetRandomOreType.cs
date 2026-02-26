using System.Collections.Generic;
using UnityEngine;

public class GetRandomOreType : MonoBehaviour
{
    private readonly List<Ore> orelist = new List<Ore>();
    private Ore noore;
    [Header("Ore Chances (Percent)")]
    [SerializeField, Range(0f, 100f)] private float ironChance = 12f;
    [SerializeField, Range(0f, 100f)] private float goldChance = 6f;
    [SerializeField, Range(0f, 100f)] private float diamondChance = 3f;
    [SerializeField, Range(0f, 100f)] private float radiumChance = 1.5f;
    [SerializeField, Range(0f, 100f)] private float plasmaChance = 0.5f;
    [SerializeField, Range(0f, 100f)] private float flamingOreChance = 0.2f;
    public Material ironmaterial;
    public Material goldmaterial;
    public Material diamondmaterial;
    public Material radiummaterial;
    public Material plasmamaterial;
    public Material flaming_ore_material;
    public Sprite ironsprite;
    public Sprite goldsprite;
    public Sprite diamondsprite;
    public Sprite radiumsprite;
    public Sprite plasmapsprite;
    public Sprite flaming_oresprite;
    public Sprite basicstonesprite;

    private void Awake()
    {
        InitializeOreList();
    }

    private void InitializeOreList()
    {
        orelist.Clear();
        orelist.Add(new Ore("iron", ironmaterial, ironsprite));
        orelist.Add(new Ore("gold", goldmaterial, goldsprite));
        orelist.Add(new Ore("diamond", diamondmaterial, diamondsprite));
        orelist.Add(new Ore("radium", radiummaterial, radiumsprite));
        orelist.Add(new Ore("plasma", plasmamaterial, plasmapsprite));
        orelist.Add(new Ore("flaming_ore", flaming_ore_material, flaming_oresprite));
        noore = new Ore("noore", null, basicstonesprite);
    }

    private Ore GetOreByName(string oreName)
    {
        foreach (Ore ore in orelist)
        {
            if (ore.oreName == oreName)
            {
                return ore;
            }
        }

        return noore;
    }

    public Ore GetOreType()
    {
        if (orelist.Count == 0 || noore == null)
        {
            InitializeOreList();
        }

        float clampedIronChance = Mathf.Clamp(ironChance, 0f, 100f);
        float clampedGoldChance = Mathf.Clamp(goldChance, 0f, 100f);
        float clampedDiamondChance = Mathf.Clamp(diamondChance, 0f, 100f);
        float clampedRadiumChance = Mathf.Clamp(radiumChance, 0f, 100f);
        float clampedPlasmaChance = Mathf.Clamp(plasmaChance, 0f, 100f);
        float clampedFlamingOreChance = Mathf.Clamp(flamingOreChance, 0f, 100f);

        float totalOreChance = clampedIronChance + clampedGoldChance + clampedDiamondChance +
                               clampedRadiumChance + clampedPlasmaChance + clampedFlamingOreChance;

        if (totalOreChance <= 0f)
        {
            return noore;
        }

        if (totalOreChance > 100f)
        {
            float normalizeScale = 100f / totalOreChance;
            clampedIronChance *= normalizeScale;
            clampedGoldChance *= normalizeScale;
            clampedDiamondChance *= normalizeScale;
            clampedRadiumChance *= normalizeScale;
            clampedPlasmaChance *= normalizeScale;
            clampedFlamingOreChance *= normalizeScale;
        }

        float roll = Random.Range(0f, 100f);

        if (roll < clampedIronChance)
        {
            return GetOreByName("iron");
        }

        roll -= clampedIronChance;
        if (roll < clampedGoldChance)
        {
            return GetOreByName("gold");
        }

        roll -= clampedGoldChance;
        if (roll < clampedDiamondChance)
        {
            return GetOreByName("diamond");
        }

        roll -= clampedDiamondChance;
        if (roll < clampedRadiumChance)
        {
            return GetOreByName("radium");
        }

        roll -= clampedRadiumChance;
        if (roll < clampedPlasmaChance)
        {
            return GetOreByName("plasma");
        }

        roll -= clampedPlasmaChance;
        if (roll < clampedFlamingOreChance)
        {
            return GetOreByName("flaming_ore");
        }

        return noore;
    }
}
