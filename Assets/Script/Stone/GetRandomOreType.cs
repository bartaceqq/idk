using System.Collections.Generic;
using UnityEngine;

public class GetRandomOreType : MonoBehaviour
{
    private readonly List<Ore> orelist = new List<Ore>();
    private Ore noore;
    [SerializeField, Range(0f, 100f)] private float stoneChance = 60f;
    private const float BaseIronChance = 25f;
    private const float BaseGoldChance = 10f;
    private const float BaseDiamondChance = 10f;
    private const float BaseRadiumChance = 5f;
    private const float BasePlasmaChance = 5f;
    private const float BaseFlamingOreChance = 5f;
    private const float BaseOreTotalChance = 60f;
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

        float clampedStoneChance = Mathf.Clamp(stoneChance, 0f, 100f);
        float remainingChance = 100f - clampedStoneChance;
        if (remainingChance <= 0f)
        {
            return noore;
        }

        float scale = remainingChance / BaseOreTotalChance;
        float ironChance = BaseIronChance * scale;
        float goldChance = BaseGoldChance * scale;
        float diamondChance = BaseDiamondChance * scale;
        float radiumChance = BaseRadiumChance * scale;
        float plasmaChance = BasePlasmaChance * scale;
        float flamingOreChance = BaseFlamingOreChance * scale;

        float roll = Random.Range(0f, 100f);

        if (roll < ironChance)
        {
            return GetOreByName("iron");
        }

        roll -= ironChance;
        if (roll < goldChance)
        {
            return GetOreByName("gold");
        }

        roll -= goldChance;
        if (roll < diamondChance)
        {
            return GetOreByName("diamond");
        }

        roll -= diamondChance;
        if (roll < radiumChance)
        {
            return GetOreByName("radium");
        }

        roll -= radiumChance;
        if (roll < plasmaChance)
        {
            return GetOreByName("plasma");
        }

        roll -= plasmaChance;
        if (roll < flamingOreChance)
        {
            return GetOreByName("flaming_ore");
        }

        return noore;
    }
}
