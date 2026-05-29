using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class InventoryController : MonoBehaviour
{
    public Slider slider;
    public static bool IsInventoryOpen { get; private set; }
    public bool UIshown = false;
    public KeyCode keycode; public GameObject inventoryobject; void Start()
    {
        ApplyUIState();
    }
    void Update()
    {
        if (GameSettings.GetKeyDown(GameSettings.Key.Inventory, keycode)) { SetInventoryShown(!UIshown); }
    }
    void OnApplicationFocus(bool hasFocus) { if (hasFocus) { ApplyUIState(); } }
    void OnDisable() { IsInventoryOpen = false; ApplyCursorState(); }
    public void SetInventoryShown(bool shown) { UIshown = shown; ApplyUIState(); }
    public void CloseInventory() { SetInventoryShown(false); }
    private void ApplyUIState()
    {
        slider.enabled = UIshown; IsInventoryOpen = UIshown; ApplyCursorState();
        if (inventoryobject == null)
        {
            Debug.LogWarning("InventoryController: inventoryobject is not assigned.", this); return;
        }
        Image[] images = inventoryobject.GetComponentsInChildren<Image>(true);
        TMP_Text[] tmpTexts = inventoryobject.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < images.Length; i++) { images[i].enabled = UIshown; }
        for (int i = 0; i < tmpTexts.Length; i++) { tmpTexts[i].enabled = UIshown; }
        XPHandler[] xpHandlers = GetXPHandlersForInventoryUI();
        for (int i = 0; i < xpHandlers.Length; i++)
        {
            if (xpHandlers[i] != null) { xpHandlers[i].SetVisible(UIshown); }
        }
        Slot[] slots = inventoryobject.GetComponentsInChildren<Slot>(true);
        for (int i = 0; i < slots.Length; i++) { if (slots[i] != null) { slots[i].UpdateUI(); } }
        WeaponSlot[] weaponSlots = inventoryobject.GetComponentsInChildren<WeaponSlot>(true);
        for (int i = 0; i < weaponSlots.Length; i++)
        {
            if (weaponSlots[i] != null) { weaponSlots[i].RefreshVisual(); }
        }
    }
    private static void ApplyCursorState() { GameplayUiState.ApplyCursorState(); }
    private XPHandler[] GetXPHandlersForInventoryUI()
    {
        if (inventoryobject == null) { return System.Array.Empty<XPHandler>(); }
        Canvas rootCanvas = inventoryobject.GetComponentInParent<Canvas>();
        if (rootCanvas != null) { return rootCanvas.GetComponentsInChildren<XPHandler>(true); }
        return inventoryobject.GetComponentsInChildren<XPHandler>(true);
    }
}
