using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotInsideUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private static SlotInsideUI draggedSlot;
    public static SlotInsideUI CurrentDragSource => draggedSlot;

    public InventoryManager inventoryManager;
    public int count;
    public string nameofslot;
    public Image image;
    public Image background;
    public TMP_Text text;
    public int id;
    public bool occupied;
    public InventoryItem Item;

    private Canvas rootCanvas;
    private GameObject dragGhost;
    private RectTransform dragGhostRect;

    private void Awake()
    {
        if (inventoryManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            inventoryManager = FindAnyObjectByType<InventoryManager>(FindObjectsInactive.Include);
#else
            inventoryManager = FindObjectOfType<InventoryManager>(true);
#endif
        }

        rootCanvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        if (inventoryManager != null && !inventoryManager.slotlist.Contains(this))
        {
            inventoryManager.slotlist.Add(this);
        }

        RefreshView();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!InventoryManager.IsInventoryOpen || !HasItem())
        {
            return;
        }

        draggedSlot = this;
        CreateDragGhost();
        UpdateDragGhostPosition(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggedSlot == this && dragGhostRect != null)
        {
            UpdateDragGhostPosition(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedSlot == this)
        {
            draggedSlot = null;
        }

        DestroyDragGhost();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!InventoryManager.IsInventoryOpen)
        {
            return;
        }

        SlotInsideUI source = draggedSlot;
        if (source == null || source == this || !source.HasItem())
        {
            return;
        }

        if (!HasItem())
        {
            MoveFrom(source);
            return;
        }

        if (IsSameItem(source))
        {
            count += source.count;
            RefreshView();
            source.ClearSlot();
            return;
        }

        SwapWith(source);
    }

    public bool HasItem()
    {
        return occupied && Item != null && count > 0;
    }

    private void MoveFrom(SlotInsideUI source)
    {
        Item = source.Item;
        nameofslot = source.nameofslot;
        count = source.count;
        occupied = source.occupied;
        RefreshView();
        source.ClearSlot();
    }

    private void SwapWith(SlotInsideUI source)
    {
        InventoryItem oldItem = Item;
        string oldName = nameofslot;
        int oldCount = count;
        bool oldOccupied = occupied;

        Item = source.Item;
        nameofslot = source.nameofslot;
        count = source.count;
        occupied = source.occupied;
        RefreshView();

        source.Item = oldItem;
        source.nameofslot = oldName;
        source.count = oldCount;
        source.occupied = oldOccupied;
        source.RefreshView();
    }

    private void ClearSlot()
    {
        Item = null;
        nameofslot = string.Empty;
        count = 0;
        occupied = false;
        RefreshView();
    }

    private bool IsSameItem(SlotInsideUI other)
    {
        if (other == null || other.Item == null || Item == null)
        {
            return false;
        }

        if (Item == other.Item)
        {
            return true;
        }

        string thisName = !string.IsNullOrWhiteSpace(nameofslot) ? nameofslot : Item.nameofitem;
        string otherName = !string.IsNullOrWhiteSpace(other.nameofslot) ? other.nameofslot : other.Item.nameofitem;
        return !string.IsNullOrWhiteSpace(thisName) &&
               !string.IsNullOrWhiteSpace(otherName) &&
               string.Equals(thisName.Trim(), otherName.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshView()
    {
        bool visible = inventoryManager != null && inventoryManager.UIShown;
        if (image != null)
        {
            image.sprite = Item != null ? Item.inventorysprite : null;
            image.enabled = visible && occupied && Item != null;
        }

        if (background != null)
        {
            background.enabled = visible;
        }

        if (text != null)
        {
            text.text = count > 0 ? count.ToString() : "0";
            text.enabled = visible;
        }
    }

    private void CreateDragGhost()
    {
        if (rootCanvas == null || Item == null || Item.inventorysprite == null)
        {
            return;
        }

        dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        dragGhost.transform.SetParent(rootCanvas.transform, false);
        dragGhost.transform.SetAsLastSibling();
        dragGhostRect = dragGhost.GetComponent<RectTransform>();
        dragGhostRect.sizeDelta = new Vector2(64f, 64f);

        CanvasGroup canvasGroup = dragGhost.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.9f;

        Image ghostImage = dragGhost.GetComponent<Image>();
        ghostImage.sprite = Item.inventorysprite;
        ghostImage.raycastTarget = false;
        ghostImage.preserveAspect = true;
    }

    private void UpdateDragGhostPosition(PointerEventData eventData)
    {
        if (dragGhostRect == null || rootCanvas == null)
        {
            return;
        }

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventData.pressEventCamera, out Vector2 localPos))
        {
            dragGhostRect.anchoredPosition = localPos;
        }
    }

    private void DestroyDragGhost()
    {
        if (dragGhost != null)
        {
            Destroy(dragGhost);
        }

        dragGhost = null;
        dragGhostRect = null;
    }
}
