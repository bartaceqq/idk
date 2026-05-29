using TMPro; using UnityEngine; using UnityEngine.EventSystems; using UnityEngine.UI;

public class SlotInsideUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler {
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
    private Image dragGhostImage;
   
    void Awake() {
        if (inventoryManager == null) {
            inventoryManager = UnitySceneSearch.FindFirst<InventoryManager>();
        }

        rootCanvas = GetComponentInParent<Canvas>(); }
    void Start() { if (inventoryManager != null && !inventoryManager.slotlist.Contains(this)) { inventoryManager.slotlist.Add(this); } }
    public void OnBeginDrag(PointerEventData eventData) { if (!InventoryManager.IsInventoryOpen || !HasItem()) { return; }

        draggedSlot = this;
        CreateDragGhost();
        UpdateDragGhostPosition(eventData); }
    public void OnDrag(PointerEventData eventData) { if (draggedSlot != this || dragGhostRect == null) { return; }

        UpdateDragGhostPosition(eventData); }
    public void OnEndDrag(PointerEventData eventData) { if (draggedSlot == this) { draggedSlot = null; }

        DestroyDragGhost(); }
    public void OnDrop(PointerEventData eventData) { if (!InventoryManager.IsInventoryOpen) { return; }

        SlotInsideUI source = draggedSlot;
        if (source == null || source == this || !source.HasItem()) { return; }

        if (!HasItem()) {
            MoveFrom(source);
            return; }

        if (IsSameItem(source)) {
            count += source.count;
            RefreshView();
            source.ClearSlot();
            return; }

        SwapWith(source); }
    public void OnPointerClick(PointerEventData eventData) { if (eventData == null || eventData.button != PointerEventData.InputButton.Right) { return; }

        TryActivateInventoryBuilding(); }
    public bool HasItem() { return occupied && Item != null && count > 0; }
    private void MoveFrom(SlotInsideUI source) {
        Item = source.Item;
        nameofslot = source.nameofslot;
        count = source.count;
        occupied = source.occupied;
        RefreshView();
        source.ClearSlot(); }
    private void SwapWith(SlotInsideUI source) {
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
        source.RefreshView(); }
    private void ClearSlot() {
        Item = null;
        nameofslot = string.Empty;
        count = 0;
        occupied = false;
        RefreshView(); }
    private bool IsSameItem(SlotInsideUI other) { if (other == null || other.Item == null || Item == null) { return false; }

        string thisName = !string.IsNullOrWhiteSpace(nameofslot) ? nameofslot : Item.nameofitem;
        string otherName = !string.IsNullOrWhiteSpace(other.nameofslot) ? other.nameofslot : other.Item.nameofitem;
        if (string.IsNullOrWhiteSpace(thisName) || string.IsNullOrWhiteSpace(otherName)) { return Item == other.Item; }

        return string.Equals(thisName.Trim(), otherName.Trim(), System.StringComparison.OrdinalIgnoreCase); }
    private void TryActivateInventoryBuilding() { if (!InventoryManager.IsInventoryOpen || !HasItem()) { return; }

        Item.ResolveReferences();
        if (Item.itemType != InventoryItemType.Building) { return; }

        if (Item.itemPrefab == null) {
            Debug.LogWarning($"SlotInsideUI: Building item '{GetItemDisplayName(Item)}' is missing itemPrefab.", this);
            return; }

        RayCastScriptTest buildController = FindBuildController();
        if (buildController == null) {
            Debug.LogWarning("SlotInsideUI: RayCastScriptTest was not found, cannot enter build mode from inventory.", this);
            return; }

        if (!buildController.TrySelectInventoryBuildingItem(Item)) { return; }

        if (inventoryManager != null) { inventoryManager.EnableInventory(false); } }
    private static RayCastScriptTest FindBuildController() {
        return UnitySceneSearch.FindFirst<RayCastScriptTest>();
    }
    private static string GetItemDisplayName(InventoryItem item) { if (item == null) { return string.Empty; }

        if (!string.IsNullOrWhiteSpace(item.nameofitem)) { return item.nameofitem; }

        return item.name; }
    private void RefreshView() {
        if (image != null) {
            image.sprite = Item != null ? Item.inventorysprite : null;
            image.enabled = inventoryManager != null && inventoryManager.UIShown && occupied && Item != null; }

        if (text != null) {
            text.text = count > 0 ? count.ToString() : "0";
            text.enabled = inventoryManager != null && inventoryManager.UIShown; } }
    private void CreateDragGhost() { if (rootCanvas == null || Item == null || Item.inventorysprite == null) { return; }

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
        dragGhostImage.preserveAspect = true; }
    private void UpdateDragGhostPosition(PointerEventData eventData) { if (dragGhostRect == null || rootCanvas == null) { return; }

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null) { return; }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPos)) { dragGhostRect.anchoredPosition = localPos; } }
    private void DestroyDragGhost() { if (dragGhost != null) { Destroy(dragGhost); }

        dragGhost = null;
        dragGhostRect = null;
        dragGhostImage = null; } }
