using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotInsideUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private static SlotInsideUI draggedSlot;

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
    private Image dragGhostImage;
   
    void Awake()
    {
        if (inventoryManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            inventoryManager = FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);
#else
            inventoryManager = FindObjectOfType<InventoryManager>(true);
#endif
        }

        rootCanvas = GetComponentInParent<Canvas>();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (inventoryManager != null && !inventoryManager.slotlist.Contains(this))
        {
            inventoryManager.slotlist.Add(this);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Handle On Begin Drag.
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

    // Handle On Drag.
    public void OnDrag(PointerEventData eventData)
    {
        if (draggedSlot != this || dragGhostRect == null)
        {
            return;
        }

        UpdateDragGhostPosition(eventData);
    }

    // Handle On End Drag.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedSlot == this)
        {
            draggedSlot = null;
        }

        DestroyDragGhost();
    }

    // Handle On Drop.
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

    // Handle Has Item.
    private bool HasItem()
    {
        return occupied && Item != null && count > 0;
    }

    // Handle Move From.
    private void MoveFrom(SlotInsideUI source)
    {
        Item = source.Item;
        nameofslot = source.nameofslot;
        count = source.count;
        occupied = source.occupied;
        RefreshView();
        source.ClearSlot();
    }

    // Handle Swap With.
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

    // Handle Clear Slot.
    private void ClearSlot()
    {
        Item = null;
        nameofslot = string.Empty;
        count = 0;
        occupied = false;
        RefreshView();
    }

    // Handle Is Same Item.
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
        if (string.IsNullOrWhiteSpace(thisName) || string.IsNullOrWhiteSpace(otherName))
        {
            return false;
        }

        return string.Equals(thisName.Trim(), otherName.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    // Handle Refresh View.
    private void RefreshView()
    {
        if (image != null)
        {
            image.sprite = Item != null ? Item.inventorysprite : null;
            image.enabled = inventoryManager != null && inventoryManager.UIShown && occupied && Item != null;
        }

        if (text != null)
        {
            text.text = count > 0 ? count.ToString() : "0";
            text.enabled = inventoryManager != null && inventoryManager.UIShown;
        }
    }

    // Handle Create Drag Ghost.
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

        CanvasGroup cg = dragGhost.GetComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0.9f;

        dragGhostImage = dragGhost.GetComponent<Image>();
        dragGhostImage.sprite = Item.inventorysprite;
        dragGhostImage.raycastTarget = false;
        dragGhostImage.preserveAspect = true;
    }

    // Handle Update Drag Ghost Position.
    private void UpdateDragGhostPosition(PointerEventData eventData)
    {
        if (dragGhostRect == null || rootCanvas == null)
        {
            return;
        }

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null)
        {
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPos))
        {
            dragGhostRect.anchoredPosition = localPos;
        }
    }

    // Handle Destroy Drag Ghost.
    private void DestroyDragGhost()
    {
        if (dragGhost != null)
        {
            Destroy(dragGhost);
        }

        dragGhost = null;
        dragGhostRect = null;
        dragGhostImage = null;
    }
}
