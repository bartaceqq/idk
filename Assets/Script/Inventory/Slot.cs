using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Controls Slot behavior.
public class Slot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
{
    public Sprite sprite;
    public int count;
    public SlotManager slotManager;
    public InventoryItem inventoryItemReference;
    public string itemName;
    public Image image;
    public TMP_Text counttext;

    private static Slot currentDragSource;
    private static GameObject dragIconObject;
    private static Canvas dragCanvas;

    public static Slot CurrentDragSource => currentDragSource;

    void Start()
    {
        if (slotManager != null)
        {
            slotManager.RegisterSlot(this);
        }

        UpdateUI();
    }

    // Handle Add Item.
    public void AddItem(InventoryItem inventoryItem, InventoryListHandler inventoryListHandler, int explicitAmount = -1)
    {
        if (inventoryItem == null)
        {
            return;
        }

        if (sprite == null)
        {
            inventoryItemReference = inventoryItem;
            sprite = inventoryItem.inventorysprite;
            itemName = inventoryItem.name;
        }
        else if (inventoryItemReference == null)
        {
            inventoryItemReference = inventoryItem;
        }

        int addedAmount = explicitAmount > 0 ? explicitAmount : GetCount(inventoryItem);
        if (addedAmount <= 0)
        {
            return;
        }

        count += addedAmount;

        if (inventoryListHandler != null)
        {
            inventoryListHandler.AddItem(inventoryItem, addedAmount);
        }
        else if (slotManager != null)
        {
            slotManager.RefreshInventoryList();
        }

        UpdateUI();
    }

    // Handle Get Count.
    public int GetCount(InventoryItem inventoryItem)
    {
        if (inventoryItem.mingain == inventoryItem.maxgain)
        {
            return inventoryItem.mingain;
        }

        int min = Mathf.Min(inventoryItem.mingain, inventoryItem.maxgain);
        int max = Mathf.Max(inventoryItem.mingain, inventoryItem.maxgain);
        return Random.Range(min, max + 1);
    }

    // Handle Matches Item.
    public bool MatchesItem(InventoryItem inventoryItem)
    {
        if (inventoryItem == null)
        {
            return false;
        }

        if (inventoryItemReference != null && inventoryItemReference == inventoryItem)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(inventoryItem.name))
        {
            return false;
        }

        return string.Equals(itemName, inventoryItem.name, System.StringComparison.OrdinalIgnoreCase);
    }

    // Handle Update UI.
    public void UpdateUI()
    {
        if (image != null)
        {
            image.sprite = sprite;
        }

        if (counttext != null)
        {
            counttext.text = count > 0 ? count.ToString() : "0";
        }
    }

    // Handle Is Empty.
    public bool IsEmpty()
    {
        return sprite == null || count <= 0;
    }

    // Handle Can Stack With.
    private bool CanStackWith(Slot other)
    {
        if (other == null || IsEmpty() || other.IsEmpty())
        {
            return false;
        }

        if (inventoryItemReference != null &&
            other.inventoryItemReference != null &&
            inventoryItemReference == other.inventoryItemReference)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(itemName) || string.IsNullOrWhiteSpace(other.itemName))
        {
            return false;
        }

        return string.Equals(itemName, other.itemName, System.StringComparison.OrdinalIgnoreCase);
    }

    // Handle Clear Data.
    private void ClearData()
    {
        inventoryItemReference = null;
        sprite = null;
        itemName = string.Empty;
        count = 0;
    }

    // Handle On Begin Drag.
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (IsEmpty())
        {
            currentDragSource = null;
            return;
        }

        currentDragSource = this;
        CreateDragIcon();
        UpdateDragIconPosition(eventData);
    }

    // Handle On Drag.
    public void OnDrag(PointerEventData eventData)
    {
        if (currentDragSource != this || dragIconObject == null)
        {
            return;
        }

        UpdateDragIconPosition(eventData);
    }

    // Handle On End Drag.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentDragSource == this)
        {
            currentDragSource = null;
        }

        DestroyDragIcon();
    }

    // Handle On Drop.
    public void OnDrop(PointerEventData eventData)
    {
        if (currentDragSource == null || currentDragSource == this)
        {
            return;
        }

        Slot from = currentDragSource;
        Slot to = this;

        if (to.IsEmpty())
        {
            to.inventoryItemReference = from.inventoryItemReference;
            to.sprite = from.sprite;
            to.itemName = from.itemName;
            to.count = from.count;
            from.ClearData();
        }
        else if (to.CanStackWith(from))
        {
            if (to.inventoryItemReference == null)
            {
                to.inventoryItemReference = from.inventoryItemReference;
            }

            to.count += from.count;
            from.ClearData();
        }
        else
        {
            InventoryItem tempItemReference = to.inventoryItemReference;
            Sprite tempSprite = to.sprite;
            string tempName = to.itemName;
            int tempCount = to.count;

            to.inventoryItemReference = from.inventoryItemReference;
            to.sprite = from.sprite;
            to.itemName = from.itemName;
            to.count = from.count;

            from.inventoryItemReference = tempItemReference;
            from.sprite = tempSprite;
            from.itemName = tempName;
            from.count = tempCount;
        }

        from.UpdateUI();
        to.UpdateUI();

        if (from.slotManager != null)
        {
            from.slotManager.RefreshInventoryList();
        }

        if (to.slotManager != null && to.slotManager != from.slotManager)
        {
            to.slotManager.RefreshInventoryList();
        }
    }

    // Handle On Pointer Click.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Right)
        {
            return;
        }

        TryActivateInventoryBuilding();
    }

    // Handle Create Drag Icon.
    private void CreateDragIcon()
    {
        DestroyDragIcon();
        EnsureDragCanvas();
        if (dragCanvas == null)
        {
            return;
        }

        dragIconObject = new GameObject("InventoryDragIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragIconObject.transform.SetParent(dragCanvas.transform, false);
        dragIconObject.transform.SetAsLastSibling();

        CanvasGroup canvasGroup = dragIconObject.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0.85f;

        Image dragIconImage = dragIconObject.GetComponent<Image>();
        dragIconImage.raycastTarget = false;
        dragIconImage.sprite = sprite;
        dragIconImage.preserveAspect = true;
        dragIconImage.enabled = sprite != null;

        RectTransform dragRect = dragIconObject.GetComponent<RectTransform>();
        if (image != null)
        {
            RectTransform sourceRect = image.rectTransform;
            dragRect.sizeDelta = sourceRect.rect.size;
        }
        else
        {
            dragRect.sizeDelta = new Vector2(64f, 64f);
        }
    }

    // Handle Ensure Drag Canvas.
    private void EnsureDragCanvas()
    {
        if (dragCanvas != null)
        {
            return;
        }

        Canvas selfCanvas = GetComponentInParent<Canvas>();
        if (selfCanvas == null)
        {
            return;
        }

        dragCanvas = selfCanvas.rootCanvas != null ? selfCanvas.rootCanvas : selfCanvas;
    }

    // Handle Update Drag Icon Position.
    private static void UpdateDragIconPosition(PointerEventData eventData)
    {
        if (dragIconObject == null || dragCanvas == null)
        {
            return;
        }

        RectTransform dragRect = dragIconObject.GetComponent<RectTransform>();
        RectTransform canvasRect = dragCanvas.transform as RectTransform;
        Camera eventCamera = dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            dragRect.localPosition = localPoint;
        }
    }

    // Handle Destroy Drag Icon.
    private static void DestroyDragIcon()
    {
        if (dragIconObject != null)
        {
            Destroy(dragIconObject);
            dragIconObject = null;
        }
    }

    // Handle Try Activate Inventory Building.
    private void TryActivateInventoryBuilding()
    {
        if (IsEmpty())
        {
            return;
        }

        InventoryItem inventoryItem = ResolveInventoryItemReference();
        if (inventoryItem == null || inventoryItem.itemType != InventoryItemType.Building)
        {
            return;
        }

        if (inventoryItem.itemPrefab == null)
        {
            Debug.LogWarning($"Slot: Building item '{inventoryItem.name}' is missing itemPrefab.", this);
            return;
        }

        RayCastScriptTest buildController = FindBuildController();
        if (buildController == null)
        {
            Debug.LogWarning("Slot: RayCastScriptTest was not found, cannot enter build mode from inventory.", this);
            return;
        }

        if (!buildController.TrySelectInventoryBuildingItem(inventoryItem))
        {
            return;
        }

        InventoryController inventoryController = FindInventoryController();
        if (inventoryController != null)
        {
            inventoryController.CloseInventory();
        }
    }

    // Handle Resolve Inventory Item Reference.
    private InventoryItem ResolveInventoryItemReference()
    {
        if (inventoryItemReference != null)
        {
            return inventoryItemReference;
        }

        string normalizedItemName = NormalizeItemName(itemName);
        if (string.IsNullOrEmpty(normalizedItemName))
        {
            return null;
        }

#if UNITY_2023_1_OR_NEWER
        InventoryItem[] allItems = Object.FindObjectsByType<InventoryItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        InventoryItem[] allItems = Object.FindObjectsOfType<InventoryItem>(true);
#endif

        for (int i = 0; i < allItems.Length; i++)
        {
            InventoryItem candidate = allItems[i];
            if (candidate == null)
            {
                continue;
            }

            if (!string.Equals(NormalizeItemName(candidate.name), normalizedItemName, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            inventoryItemReference = candidate;
            return candidate;
        }

        return null;
    }

    // Handle Find Build Controller.
    private static RayCastScriptTest FindBuildController()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<RayCastScriptTest>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<RayCastScriptTest>(true);
#endif
    }

    // Handle Find Inventory Controller.
    private InventoryController FindInventoryController()
    {
        InventoryController controller = GetComponentInParent<InventoryController>();
        if (controller != null)
        {
            return controller;
        }

#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<InventoryController>(FindObjectsInactive.Include);
#else
        return Object.FindObjectOfType<InventoryController>(true);
#endif
    }

    // Handle Normalize Item Name.
    private static string NormalizeItemName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        return rawName.Trim();
    }
}
