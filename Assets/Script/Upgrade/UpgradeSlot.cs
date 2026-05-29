using UnityEngine;
using UnityEngine.UI;
public class UpgradeSlot : MonoBehaviour
{
    public UpgradeManager upgradeManager; public bool unlocked = false; public Sprite sprite;
    public Image image; public int id; void Start()
    {
        if (upgradeManager != null && !upgradeManager.upgradeSlots.Contains(this)) { upgradeManager.upgradeSlots.Add(this); }
    }
    public void SetVisible(bool visible)
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null) { continue; }
            graphic.enabled = visible;
        }
    }
}
