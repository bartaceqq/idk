using UnityEngine;

public class GetRandomOreType : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)] private float stoneChance = 60f;
    private const float BaseIronChance = 25f;
    private const float BaseGoldChance = 10f;
    private const float BaseDiamondChance = 10f;
    private const float BaseRadiumChance = 5f;
    private const float BasePlasmaChance = 5f;
    private const float BaseFlamingOreChance = 5f;
    private const float BaseOreTotalChance = 60f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public string GetOreType()
    {
        float clampedStoneChance = Mathf.Clamp(stoneChance, 0f, 100f);
        float remainingChance = 100f - clampedStoneChance;
        if (remainingChance <= 0f)
        {
            return string.Empty;
        }

        float scale = remainingChance / BaseOreTotalChance;
        float ironChance = BaseIronChance * scale;
        float goldChance = BaseGoldChance * scale;
        float diamondChance = BaseDiamondChance * scale;
        float radiumChance = BaseRadiumChance * scale;
        float plasmaChance = BasePlasmaChance * scale;
        float flamingOreChance = BaseFlamingOreChance * scale;

        float roll = Random.Range(0f, 100f);

        if (roll < ironChance) return "iron";
        roll -= ironChance;
        if (roll < goldChance) return "gold";
        roll -= goldChance;
        if (roll < diamondChance) return "diamond";
        roll -= diamondChance;
        if (roll < radiumChance) return "radium";
        roll -= radiumChance;
        if (roll < plasmaChance) return "plasma";
        roll -= plasmaChance;
        if (roll < flamingOreChance) return "flaming_ore";

        return string.Empty;
    }
}
