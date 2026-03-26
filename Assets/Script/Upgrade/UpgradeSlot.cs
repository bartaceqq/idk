using UnityEngine;
using UnityEngine.UI;

public class UpgradeSlot : MonoBehaviour
{
    public UpgradeManager upgradeManager;
    public bool unlocked = false;
    public Sprite sprite;
    public Image image;
    public int id;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        upgradeManager.upgradeSlots.Add(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
