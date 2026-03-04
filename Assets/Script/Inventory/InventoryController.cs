using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Controls Inventory Controller behavior.
public class InventoryController : MonoBehaviour
{
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
            UIshown = !UIshown;
            ApplyUIState();
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

    // Handle Apply UIState.
    private void ApplyUIState()
    {
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
    }

    // Handle Apply Cursor State.
    private static void ApplyCursorState()
    {
        bool uiOpen = IsInventoryOpen || CraftingManager.IsCraftingOpen;
        Cursor.lockState = uiOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = uiOpen;
    }
}
