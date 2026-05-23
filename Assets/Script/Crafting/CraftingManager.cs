using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public static bool IsCraftingOpen { get; private set; }

    public List<CraftableSlot> slots = new List<CraftableSlot>();
    public List<CraftableItem> items = new List<CraftableItem>();
    public LevelingManager levelingManager;
    public KeyCode toggleKey = KeyCode.T;
    public bool menuShown;
    public GameObject craftingMenuRoot;

    private bool checkQueued;
    private CanvasGroup menuCanvasGroup;

    private void Start()
    {
        MigrateLegacyToggleKey();
        EnsureMenuCanvasGroup();
        ApplyMenuVisibility();
        Check();
        QueueCheck();
    }

    private void Update()
    {
        if (GameSettings.GetKeyDown(GameSettings.Key.Crafting, toggleKey))
        {
            SetMenuShown(!menuShown);
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
        GameplayUiState.ApplyCursorState();
    }

    public void SetMenuShown(bool shown)
    {
        menuShown = shown;
        if (shown)
        {
            Check();
        }

        ApplyMenuVisibility();
    }

    public void Check()
    {
        RefreshLists();
        ResetRuntimePlacementState();

        if (items.Count == 0)
        {
            Debug.LogWarning("CraftingManager: No craftable items assigned or found.", this);
            UpdateSlotVisibility();
            return;
        }

        if (slots.Count == 0)
        {
            Debug.LogWarning("CraftingManager: No craftable slots assigned or found.", this);
            return;
        }

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
            item.slotnumber = slot.slotnumber;
            slot.AddCraftableItem(item);
        }

        UpdateSlotVisibility();
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
        if (checkQueued)
        {
            return;
        }

        checkQueued = true;
        StartCoroutine(DelayedCheck());
    }

    private System.Collections.IEnumerator DelayedCheck()
    {
        yield return null;
        checkQueued = false;
        Check();
    }

    private CraftableSlot GetLowestAvailableSlot()
    {
        slots.Sort(CompareSlotsForPlacement);
        for (int i = 0; i < slots.Count; i++)
        {
            CraftableSlot slot = slots[i];
            if (slot != null && !slot.occupied)
            {
                return slot;
            }
        }

        return null;
    }

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
        Vector2 posA = rectA != null ? rectA.anchoredPosition : new Vector2(a.transform.position.x, a.transform.position.y);
        Vector2 posB = rectB != null ? rectB.anchoredPosition : new Vector2(b.transform.position.x, b.transform.position.y);

        int yCompare = posB.y.CompareTo(posA.y);
        return yCompare != 0 ? yCompare : posA.x.CompareTo(posB.x);
    }

    private void ResetRuntimePlacementState()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i]?.ResetRuntimeState();
        }

        for (int i = 0; i < items.Count; i++)
        {
            CraftableItem item = items[i];
            if (item == null)
            {
                continue;
            }

            item.placed = false;
            item.slotnumber = -1;
        }
    }

    private void UpdateSlotVisibility()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            CraftableSlot slot = slots[i];
            if (slot != null)
            {
                slot.SetVisualVisible(slot.occupied);
            }
        }
    }

    private void EnsureMenuCanvasGroup()
    {
        if (craftingMenuRoot == null)
        {
            craftingMenuRoot = gameObject;
        }

        menuCanvasGroup = craftingMenuRoot.GetComponent<CanvasGroup>();
        if (menuCanvasGroup == null)
        {
            menuCanvasGroup = craftingMenuRoot.AddComponent<CanvasGroup>();
        }
    }

    private void ApplyMenuVisibility()
    {
        if (menuCanvasGroup == null)
        {
            EnsureMenuCanvasGroup();
        }

        IsCraftingOpen = menuShown;
        menuCanvasGroup.alpha = menuShown ? 1f : 0f;
        menuCanvasGroup.interactable = menuShown;
        menuCanvasGroup.blocksRaycasts = menuShown;
        GameplayUiState.ApplyCursorState();
    }

    private void MigrateLegacyToggleKey()
    {
        if (toggleKey == KeyCode.None || toggleKey == KeyCode.R)
        {
            toggleKey = KeyCode.T;
        }
    }
}
