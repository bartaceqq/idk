using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerXPState))]
[RequireComponent(typeof(XPLevelTableLoader))]
public class XPHandler : MonoBehaviour
{
    public Slider xpSlider;
    public TMP_Text levelText;
    public TMP_Text xpAmountText;

    private PlayerXPState playerXPState;
    private XPLevelTableLoader xpLevelTableLoader;
    private Graphic[] cachedGraphics;
    private InventoryManager inventoryManager;
    private InventoryController inventoryController;
    private bool hasVisibilityState;
    private bool currentVisibilityState;

    void Awake()
    {
        CacheDependencies();
        EnsureReady();
        CacheGraphics();
        RefreshUI();
    }

    void Start()
    {
        RefreshUI();
        SetVisible(ResolveInventoryVisibility());
    }

    void Update()
    {
        SetVisible(ResolveInventoryVisibility());
    }

    // Handle Add XP.
    public void AddXP(int count)
    {
        if (count <= 0)
        {
            return;
        }

        CacheDependencies();
        if (!EnsureReady())
        {
            return;
        }

        playerXPState.AddXP(count, xpLevelTableLoader);
        RefreshUI();
    }

    // Handle Refresh UI.
    public void RefreshUI()
    {
        CacheDependencies();
        if (!EnsureReady())
        {
            return;
        }

        int currentLevel = playerXPState.CurrentLevel;
        int currentXP = playerXPState.CurrentXP;
        int maxXP = xpLevelTableLoader.GetRequiredXPForLevel(currentLevel);

        if (xpSlider != null)
        {
            xpSlider.minValue = 0f;
            xpSlider.maxValue = maxXP;
            xpSlider.wholeNumbers = true;
            xpSlider.value = currentXP;
        }

        if (levelText != null)
        {
            levelText.text = "LEVEL " + currentLevel;
        }

        if (xpAmountText != null)
        {
            xpAmountText.text = currentXP + "/" + maxXP;
        }
    }

    // Handle Get Current Level.
    public int GetCurrentLevel()
    {
        CacheDependencies();
        return playerXPState != null ? playerXPState.CurrentLevel : 1;
    }

    // Handle Get Current XP.
    public int GetCurrentXP()
    {
        CacheDependencies();
        return playerXPState != null ? playerXPState.CurrentXP : 0;
    }

    // Handle Get Max XP For Current Level.
    public int GetCurrentLevelMaxXP()
    {
        CacheDependencies();
        return xpLevelTableLoader != null
            ? xpLevelTableLoader.GetRequiredXPForLevel(GetCurrentLevel())
            : 100;
    }

    // Handle Cache Dependencies.
    private void CacheDependencies()
    {
        if (playerXPState == null)
        {
            playerXPState = GetComponent<PlayerXPState>();
        }

        if (xpLevelTableLoader == null)
        {
            xpLevelTableLoader = GetComponent<XPLevelTableLoader>();
        }

        if (inventoryManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            inventoryManager = FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);
#else
            inventoryManager = FindObjectOfType<InventoryManager>(true);
#endif
        }

        if (inventoryController == null)
        {
#if UNITY_2023_1_OR_NEWER
            inventoryController = FindFirstObjectByType<InventoryController>(FindObjectsInactive.Include);
#else
            inventoryController = FindObjectOfType<InventoryController>(true);
#endif
        }
    }

    // Handle Cache Graphics.
    private void CacheGraphics()
    {
        if (cachedGraphics == null || cachedGraphics.Length == 0)
        {
            cachedGraphics = GetComponentsInChildren<Graphic>(true);
        }
    }

    // Handle Ensure Ready.
    private bool EnsureReady()
    {
        if (playerXPState == null || xpLevelTableLoader == null)
        {
            Debug.LogWarning("XPHandler: Missing PlayerXPState or XPLevelTableLoader component.", this);
            return false;
        }

        xpLevelTableLoader.EnsureLoaded();
        playerXPState.NormalizeProgress(xpLevelTableLoader);
        return true;
    }

    // Handle Set Visible.
    public void SetVisible(bool visible)
    {
        if (hasVisibilityState && currentVisibilityState == visible)
        {
            return;
        }

        CacheGraphics();

        if (cachedGraphics != null)
        {
            for (int i = 0; i < cachedGraphics.Length; i++)
            {
                if (cachedGraphics[i] != null)
                {
                    cachedGraphics[i].enabled = visible;
                }
            }
        }

        if (xpSlider != null)
        {
            xpSlider.enabled = visible;
        }

        currentVisibilityState = visible;
        hasVisibilityState = true;
    }

    // Handle Resolve Inventory Visibility.
    private bool ResolveInventoryVisibility()
    {
        CacheDependencies();

        bool visible = false;
        bool foundSource = false;

        if (inventoryManager != null)
        {
            visible |= inventoryManager.UIShown;
            foundSource = true;
        }

        if (inventoryController != null)
        {
            visible |= inventoryController.UIshown;
            foundSource = true;
        }

        if (!foundSource)
        {
            visible = InventoryManager.IsInventoryOpen || InventoryController.IsInventoryOpen;
        }

        return visible;
    }
}
