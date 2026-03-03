using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Controls Slot behavior.
public class Slot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public Sprite sprite;
    public int count;
    public SlotManager slotManager;
    public string itemName;
    public Image image;
    public TMP_Text counttext;

    private static Slot currentDragSource;
    private static GameObject dragIconObject;
    private static Canvas dragCanvas;

    void Start()
    {
        if (slotManager != null)
        {
            slotManager.RegisterSlot(this);
        }

        UpdateUI();
    }

    // Handle Add Item.
    public void AddItem(InventoryItem inventoryItem)
    {
        if (inventoryItem == null)
        {
            return;
        }

        if (sprite == null)
        {
            sprite = inventoryItem.inventorysprite;
            itemName = inventoryItem.name;
        }

        count += GetCount(inventoryItem);
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

        return sprite != null &&
               sprite == inventoryItem.inventorysprite &&
               itemName == inventoryItem.name;
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

        return sprite == other.sprite && itemName == other.itemName;
    }

    // Handle Clear Data.
    private void ClearData()
    {
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
            to.sprite = from.sprite;
            to.itemName = from.itemName;
            to.count = from.count;
            from.ClearData();
        }
        else if (to.CanStackWith(from))
        {
            to.count += from.count;
            from.ClearData();
        }
        else
        {
            Sprite tempSprite = to.sprite;
            string tempName = to.itemName;
            int tempCount = to.count;

            to.sprite = from.sprite;
            to.itemName = from.itemName;
            to.count = from.count;

            from.sprite = tempSprite;
            from.itemName = tempName;
            from.count = tempCount;
        }

        from.UpdateUI();
        to.UpdateUI();
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
}
