using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Controls Inventory Controller behavior.
public class InventoryController : MonoBehaviour
{
    public Slider slider;

    public static bool IsInventoryOpen { get; private set; }

    public bool UIshown = false;
    public KeyCode keycode;
    public GameObject inventoryobject;

    void Start()
    {
        
        ApplyUIState();
       
    }

    void Update()
    {
        if (Input.GetKeyDown(keycode))
        {
            SetInventoryShown(!UIshown);
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ApplyUIState();
        }
    }

    void OnDisable()
    {
        IsInventoryOpen = false;
        ApplyCursorState();
    }

    // Handle Set Inventory Shown.
    public void SetInventoryShown(bool shown)
    {
        UIshown = shown;
        ApplyUIState();
    }

    // Handle Close Inventory.
    public void CloseInventory()
    {
        SetInventoryShown(false);
    }

    // Handle Apply UIState.
    private void ApplyUIState()
    {
        slider.enabled = UIshown;
        IsInventoryOpen = UIshown;
        ApplyCursorState();
        if (inventoryobject == null)
        {
            Debug.LogWarning("InventoryController: inventoryobject is not assigned.", this);
            return;
        }

        Image[] images = inventoryobject.GetComponentsInChildren<Image>(true);
        TMP_Text[] tmpTexts = inventoryobject.GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < images.Length; i++)
        {
            images[i].enabled = UIshown;
        }

        for (int i = 0; i < tmpTexts.Length; i++)
        {
            tmpTexts[i].enabled = UIshown;
        }

        XPHandler[] xpHandlers = GetXPHandlersForInventoryUI();
        for (int i = 0; i < xpHandlers.Length; i++)
        {
            if (xpHandlers[i] != null)
            {
                xpHandlers[i].SetVisible(UIshown);
            }
        }

        // Re-apply per-slot visibility rules after global UI toggle.
        Slot[] slots = inventoryobject.GetComponentsInChildren<Slot>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].UpdateUI();
            }
        }

        WeaponSlot[] weaponSlots = inventoryobject.GetComponentsInChildren<WeaponSlot>(true);
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] != null)
            {
                weaponSlots[i].RefreshVisual();
            }
        }
    }

    // Handle Apply Cursor State.
    private static void ApplyCursorState()
    {
        GameplayUiState.ApplyCursorState();
    }

    // Handle Get XPHandlers For Inventory UI.
    private XPHandler[] GetXPHandlersForInventoryUI()
    {
        if (inventoryobject == null)
        {
            return System.Array.Empty<XPHandler>();
        }

        Canvas rootCanvas = inventoryobject.GetComponentInParent<Canvas>();
        if (rootCanvas != null)
        {
            return rootCanvas.GetComponentsInChildren<XPHandler>(true);
        }

        return inventoryobject.GetComponentsInChildren<XPHandler>(true);
    }
}
