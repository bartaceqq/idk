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

    [Header("Station Filtering")]
    public Transform playerTransform;
    public string handCraftingStationId = "HandCrafting";
    public float defaultStationRange = 5f;
    public bool closeMenuWhenLeavingStation = true;
    public bool logActiveStation = false;

    private bool _checkQueued;
    private CanvasGroup _menuCanvasGroup;
    private CraftingStation _activeStation;
    private string _activeStationId = string.Empty;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MigrateLegacyToggleKey();
        EnsureMenuCanvasGroup();
        ResolvePlayerTransform();
        EnsureActiveContextInitialized();
        ApplyMenuVisibility();
        RefreshLists();
        ResetRuntimePlacementState();
        UpdateSlotVisibility();
        QueueCheck();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (menuShown)
            {
                CloseMenu();
            }
            else
            {
                TryOpenMenuForCurrentContext();
            }
        }

        if (menuShown && closeMenuWhenLeavingStation && !IsActiveContextStillValid())
        {
            CloseMenu();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            MigrateLegacyToggleKey();
            defaultStationRange = Mathf.Max(0.1f, defaultStationRange);
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

    // Handle Try Open Menu For Current Context.
    private void TryOpenMenuForCurrentContext()
    {
        ResolvePlayerTransform();

        CraftingStation station = FindClosestStationInRange();
        string stationId = station != null
            ? station.GetNormalizedStationId()
            : NormalizeStationId(handCraftingStationId);

        if (string.IsNullOrEmpty(stationId))
        {
            return;
        }

        SetActiveCraftingContext(station, stationId);
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

    // Handle Is Active Context Still Valid.
    private bool IsActiveContextStillValid()
    {
        if (_activeStation == null)
        {
            return true;
        }

        ResolvePlayerTransform();
        if (playerTransform == null || !_activeStation.gameObject.activeInHierarchy)
        {
            return false;
        }

        return _activeStation.IsInRange(playerTransform, defaultStationRange);
    }

    // Handle Resolve Player Transform.
    private void ResolvePlayerTransform()
    {
        if (playerTransform != null)
        {
            return;
        }

        if (Camera.main != null)
        {
            playerTransform = Camera.main.transform;
            return;
        }

#if UNITY_2023_1_OR_NEWER
        LookingController lookingController = FindFirstObjectByType<LookingController>(FindObjectsInactive.Include);
#else
        LookingController lookingController = FindObjectOfType<LookingController>(true);
#endif

        if (lookingController != null)
        {
            playerTransform = lookingController.transform;
            return;
        }

        GameObject taggedPlayer = FindPlayerTaggedObject();
        if (taggedPlayer != null)
        {
            playerTransform = taggedPlayer.transform;
        }
    }

    // Handle Find Player Tagged Object.
    private static GameObject FindPlayerTaggedObject()
    {
        try
        {
            return GameObject.FindWithTag("Player");
        }
        catch (UnityException)
        {
            return null;
        }
    }

    // Handle Find Closest Station In Range.
    private CraftingStation FindClosestStationInRange()
    {
        if (playerTransform == null)
        {
            return null;
        }

#if UNITY_2023_1_OR_NEWER
        CraftingStation[] stations = FindObjectsByType<CraftingStation>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        CraftingStation[] stations = FindObjectsOfType<CraftingStation>(true);
#endif

        float bestDistance = float.MaxValue;
        CraftingStation bestStation = null;

        for (int i = 0; i < stations.Length; i++)
        {
            CraftingStation station = stations[i];
            if (station == null || !station.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (string.IsNullOrEmpty(station.GetNormalizedStationId()))
            {
                continue;
            }

            if (!station.IsInRange(playerTransform, defaultStationRange))
            {
                continue;
            }

            float distance = station.GetDistanceSqrTo(playerTransform);
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestStation = station;
        }

        return bestStation;
    }

    // Handle Set Active Crafting Context.
    private void SetActiveCraftingContext(CraftingStation station, string stationId)
    {
        _activeStation = station;
        _activeStationId = NormalizeStationId(stationId);

        if (!logActiveStation)
        {
            return;
        }

        if (_activeStation == null)
        {
            Debug.Log("Crafting station: " + _activeStationId);
            return;
        }

        Debug.Log("Crafting station: " + _activeStationId + " (" + _activeStation.name + ")");
    }

    // Handle Ensure Active Context Initialized.
    private void EnsureActiveContextInitialized()
    {
        if (!string.IsNullOrEmpty(_activeStationId))
        {
            return;
        }

        SetActiveCraftingContext(null, NormalizeStationId(handCraftingStationId));
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
        EnsureActiveContextInitialized();
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
            if (!IsItemVisibleInCurrentContext(item))
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

    // Handle Is Item Visible In Current Context.
    private bool IsItemVisibleInCurrentContext(CraftableItem item)
    {
        if (item == null)
        {
            return false;
        }

        string itemStationId = NormalizeStationId(item.craftingStationId);
        if (string.IsNullOrEmpty(itemStationId))
        {
            itemStationId = NormalizeStationId(handCraftingStationId);
        }

        if (string.Equals(itemStationId, "Any", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(itemStationId, _activeStationId, System.StringComparison.OrdinalIgnoreCase);
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
        bool uiOpen = InventoryController.IsInventoryOpen || InventoryManager.IsInventoryOpen || IsCraftingOpen;
        Cursor.lockState = uiOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = uiOpen;
    }

    // Handle Normalize Station Id.
    private static string NormalizeStationId(string rawId)
    {
        if (string.IsNullOrWhiteSpace(rawId))
        {
            return string.Empty;
        }

        return rawId.Trim();
    }
}
