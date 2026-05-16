using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public static bool IsCraftingOpen { get; private set; }

    public List<CraftableSlot> slots = new List<CraftableSlot>();
    public List<CraftableItem> items = new List<CraftableItem>();
    public LevelingManager levelingManager;
    public KeyCode toggleKey = KeyCode.T;
    public bool menuShown = false;
    public GameObject craftingMenuRoot;

    private bool _checkQueued;
    private CanvasGroup _menuCanvasGroup;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MigrateLegacyToggleKey();
        EnsureMenuCanvasGroup();
        ApplyMenuVisibility();
        RefreshLists();
        ResetRuntimePlacementState();
        UpdateSlotVisibility();
        QueueCheck();
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameSettings.GetKeyDown(GameSettings.Key.Crafting, toggleKey))
        {
            return;
        }

        if (menuShown)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            MigrateLegacyToggleKey();
        }
    }

    private void OnDisable()
    {
        menuShown = false;
        IsCraftingOpen = false;
        ApplyCursorState();
    }

    // Handle Migrate Legacy Toggle Key.
    private void MigrateLegacyToggleKey()
    {
        if (toggleKey == KeyCode.None || toggleKey == KeyCode.R)
        {
            toggleKey = KeyCode.T;
        }
    }

    // Handle Open Menu.
    private void OpenMenu()
    {
        Check();
        menuShown = true;
        ApplyMenuVisibility();
    }

    // Handle Close Menu.
    private void CloseMenu()
    {
        menuShown = false;
        ApplyMenuVisibility();
    }

    private void RefreshLists()
    {
        slots.Clear();
        items.Clear();

        slots.AddRange(GetComponentsInChildren<CraftableSlot>(true));
        items.AddRange(GetComponentsInChildren<CraftableItem>(true));

        slots.RemoveAll(slot => slot == null);
        items.RemoveAll(item => item == null);
        slots.Sort(CompareSlotsForPlacement);
    }

    private void QueueCheck()
    {
        if (_checkQueued)
        {
            return;
        }

        _checkQueued = true;
        StartCoroutine(DelayedCheck());
    }

    private System.Collections.IEnumerator DelayedCheck()
    {
        yield return null;
        _checkQueued = false;
        Check();
    }

    public void Check()
    {
        RefreshLists();

        if (items.Count == 0)
        {
            Debug.LogWarning("CraftingManager: No craftable items assigned or found.");
            UpdateSlotVisibility();
            return;
        }

        if (slots.Count == 0)
        {
            Debug.LogWarning("CraftingManager: No craftable slots assigned or found.");
            return;
        }

        RebuildVisibleCraftables();
    }

    // Handle Rebuild Visible Craftables.
    private void RebuildVisibleCraftables()
    {
        ResetRuntimePlacementState();

        for (int i = 0; i < items.Count; i++)
        {
            CraftableItem item = items[i];
            if (item == null)
            {
                continue;
            }

            CraftableSlot slot = GetLowestAvailableSlot();
            if (slot == null)
            {
                break;
            }

            item.placed = true;
            slot.AddCraftableItem(item);
            item.slotnumber = slot.slotnumber;
        }

        UpdateSlotVisibility();
    }

    private CraftableSlot GetLowestAvailableSlot()
    {
        slots.Sort(CompareSlotsForPlacement);

        foreach (CraftableSlot slot in slots)
        {
            if (slot == null || slot.occupied)
            {
                continue;
            }

            return slot;
        }

        return null;
    }

    // Handle Compare Slots For Placement.
    private static int CompareSlotsForPlacement(CraftableSlot a, CraftableSlot b)
    {
        if (a == b) return 0;
        if (a == null) return 1;
        if (b == null) return -1;

        int slotNumberCompare = a.slotnumber.CompareTo(b.slotnumber);
        if (slotNumberCompare != 0)
        {
            return slotNumberCompare;
        }

        RectTransform rectA = a.transform as RectTransform;
        RectTransform rectB = b.transform as RectTransform;

        Vector2 posA = rectA != null
            ? rectA.anchoredPosition
            : new Vector2(a.transform.position.x, a.transform.position.y);
        Vector2 posB = rectB != null
            ? rectB.anchoredPosition
            : new Vector2(b.transform.position.x, b.transform.position.y);

        // Higher Y means visually higher in UI; then lower X means more left.
        int yCompare = posB.y.CompareTo(posA.y);
        if (yCompare != 0)
        {
            return yCompare;
        }

        int xCompare = posA.x.CompareTo(posB.x);
        if (xCompare != 0)
        {
            return xCompare;
        }

        return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
    }

    // Handle Reset Runtime Placement State.
    private void ResetRuntimePlacementState()
    {
        foreach (CraftableSlot slot in slots)
        {
            if (slot != null)
            {
                slot.ResetRuntimeState();
            }
        }

        foreach (CraftableItem item in items)
        {
            if (item == null)
            {
                continue;
            }

            item.placed = false;
            item.slotnumber = -1;
        }
    }

    // Handle Update Slot Visibility.
    private void UpdateSlotVisibility()
    {
        foreach (CraftableSlot slot in slots)
        {
            if (slot == null)
            {
                continue;
            }

            slot.SetVisualVisible(slot.occupied);
        }
    }

    // Handle Ensure Menu Canvas Group.
    private void EnsureMenuCanvasGroup()
    {
        if (craftingMenuRoot == null)
        {
            craftingMenuRoot = gameObject;
        }

        if (craftingMenuRoot == null)
        {
            return;
        }

        _menuCanvasGroup = craftingMenuRoot.GetComponent<CanvasGroup>();
        if (_menuCanvasGroup == null)
        {
            _menuCanvasGroup = craftingMenuRoot.AddComponent<CanvasGroup>();
        }
    }

    // Handle Apply Menu Visibility.
    private void ApplyMenuVisibility()
    {
        if (_menuCanvasGroup == null)
        {
            return;
        }

        IsCraftingOpen = menuShown;
        _menuCanvasGroup.alpha = menuShown ? 1f : 0f;
        _menuCanvasGroup.interactable = menuShown;
        _menuCanvasGroup.blocksRaycasts = menuShown;
        ApplyCursorState();
    }

    // Handle Apply Cursor State.
    private static void ApplyCursorState()
    {
        GameplayUiState.ApplyCursorState();
    }
}
