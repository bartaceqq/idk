using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeManager : MonoBehaviour
{
    public static bool IsUpgradeOpen { get; private set; }

    public List<UpgradeSlot> upgradeSlots = new List<UpgradeSlot>();
    public Image backgroundimage;
    public Image schemeimage;
    public bool UpgradeUIShown = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ApplyUIState();
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
        IsUpgradeOpen = false;
        GameplayUiState.ApplyCursorState();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            UpgradeUIShown = !UpgradeUIShown;
            ApplyUIState();
        }
    }
    public void ApplyUIState()
    {
        IsUpgradeOpen = UpgradeUIShown;
        GameplayUiState.ApplyCursorState();

        foreach(UpgradeSlot slot in upgradeSlots)
        {
            if (slot != null && slot.image != null)
            {
                slot.image.enabled = UpgradeUIShown;
            }
        }

        if (backgroundimage != null)
        {
            backgroundimage.enabled = UpgradeUIShown;
        }

        if (schemeimage != null)
        {
            schemeimage.enabled = UpgradeUIShown;
        }
    }
}

// Centralizes UI-open checks that should block gameplay input or unlock the cursor.
public static class GameplayUiState
{
    // Handle Is Menu Open.
    public static bool IsMenuOpen
    {
        get
        {
            return InventoryController.IsInventoryOpen ||
                   InventoryManager.IsInventoryOpen ||
                   CraftingManager.IsCraftingOpen ||
                   UpgradeManager.IsUpgradeOpen;
        }
    }

    // Handle Is Gameplay Input Blocked.
    public static bool IsGameplayInputBlocked
    {
        get
        {
            return IsMenuOpen || DialogueState.IsConversationRunning;
        }
    }

    // Handle Apply Cursor State.
    public static void ApplyCursorState()
    {
        bool uiBlocking = IsGameplayInputBlocked;
        Cursor.lockState = uiBlocking ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = uiBlocking;
    }
}
