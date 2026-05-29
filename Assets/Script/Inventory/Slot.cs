using TMPro; using UnityEngine; using UnityEngine.UI; using UnityEngine.EventSystems;
public class Slot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler {
public Sprite sprite; public int count; public SlotManager slotManager;
public InventoryItem inventoryItemReference; public string itemName; public Image image;
public TMP_Text counttext; private Image resolvedItemImage;
private static Slot currentDragSource; private static GameObject dragIconObject;
private static Canvas dragCanvas;
public static Slot CurrentDragSource => currentDragSource;
void Start() { if (slotManager != null) { slotManager.RegisterSlot(this); }
ResolveVisualReferences(); UpdateUI(); }
public void AddItem(InventoryItem inventoryItem, InventoryListHandler inventoryListHandler, int explicitAmount = -1) { if (inventoryItem == null) { return; }
if (IsEmpty()) { inventoryItemReference = inventoryItem;
sprite = inventoryItem.inventorysprite;
itemName = GetInventoryItemName(inventoryItem); } else if (inventoryItemReference == null) { inventoryItemReference = inventoryItem; }
if (string.IsNullOrWhiteSpace(itemName)) { itemName = GetInventoryItemName(inventoryItem); }
int addedAmount = explicitAmount > 0 ? explicitAmount : GetCount(inventoryItem);
if (addedAmount <= 0) { return; } count += addedAmount;
if (inventoryListHandler != null) { inventoryListHandler.AddItem(inventoryItem, addedAmount); } else if (slotManager != null) { slotManager.RefreshInventoryList(); }
UpdateUI(); }
public int GetCount(InventoryItem inventoryItem) { if (inventoryItem.mingain == inventoryItem.maxgain) { return inventoryItem.mingain; }
int min = Mathf.Min(inventoryItem.mingain, inventoryItem.maxgain);
int max = Mathf.Max(inventoryItem.mingain, inventoryItem.maxgain);
return Random.Range(min, max + 1); }
public bool MatchesItem(InventoryItem inventoryItem) { if (inventoryItem == null) { return false; }
string thisItemName = GetComparableItemName();
string otherItemName = GetInventoryItemName(inventoryItem);
if (string.IsNullOrEmpty(thisItemName) || string.IsNullOrEmpty(otherItemName)) { return inventoryItemReference != null && inventoryItemReference == inventoryItem; }
return string.Equals(thisItemName, otherItemName, System.StringComparison.OrdinalIgnoreCase); }
public void UpdateUI() { bool hasItem = !IsEmpty(); ResolveVisualReferences();
if (resolvedItemImage != null) { resolvedItemImage.sprite = sprite;
resolvedItemImage.enabled = hasItem && sprite != null; } if (counttext != null) {
counttext.text = hasItem ? count.ToString() : string.Empty; counttext.enabled = hasItem; }
HideExtraPlaceholderImages(); }
public bool IsEmpty() { return sprite == null || count <= 0; }
private void ResolveVisualReferences() { if (resolvedItemImage != null) { return; }
if (image != null && image.gameObject != gameObject) { resolvedItemImage = image;
return; } Transform preferred = transform.Find("ImagePlace");
if (preferred == null) { preferred = transform.Find("WhiteInside"); }
if (preferred != null) { resolvedItemImage = preferred.GetComponent<Image>(); }
if (resolvedItemImage == null) { Image[] images = GetComponentsInChildren<Image>(true);
for (int i = 0; i < images.Length; i++) { if (images[i] == null || images[i].gameObject == gameObject) { continue; }
if (images[i].name == "BlackBakground") { continue; } resolvedItemImage = images[i];
break; } } } private void HideExtraPlaceholderImages() {
Image[] images = GetComponentsInChildren<Image>(true);
for (int i = 0; i < images.Length; i++) { Image candidate = images[i];
if (candidate == null || candidate == resolvedItemImage || candidate.gameObject == gameObject) { continue; }
if (candidate.name == "BlackBakground") { continue; }
if (candidate.sprite == null) { candidate.enabled = false; } } }
private bool CanStackWith(Slot other) { if (other == null || IsEmpty() || other.IsEmpty()) { return false; }
if (inventoryItemReference != null && other.inventoryItemReference != null &&
inventoryItemReference == other.inventoryItemReference) { return true; }
string thisItemName = GetComparableItemName();
string otherItemName = other.GetComparableItemName();
if (string.IsNullOrEmpty(thisItemName) || string.IsNullOrEmpty(otherItemName)) { return false; }
return string.Equals(thisItemName, otherItemName, System.StringComparison.OrdinalIgnoreCase); }
private void ClearData() { inventoryItemReference = null; sprite = null;
itemName = string.Empty; count = 0; }
public void OnBeginDrag(PointerEventData eventData) { if (IsEmpty()) {
currentDragSource = null; return; } currentDragSource = this; CreateDragIcon();
UpdateDragIconPosition(eventData); }
public void OnDrag(PointerEventData eventData) { if (currentDragSource != this || dragIconObject == null) { return; }
UpdateDragIconPosition(eventData); }
public void OnEndDrag(PointerEventData eventData) { if (currentDragSource == this) { currentDragSource = null; }
DestroyDragIcon(); }
public void OnDrop(PointerEventData eventData) { if (currentDragSource == null || currentDragSource == this) { return; }
Slot from = currentDragSource; Slot to = this; if (to.IsEmpty()) {
to.inventoryItemReference = from.inventoryItemReference; to.sprite = from.sprite;
to.itemName = from.itemName; to.count = from.count;
from.ClearData(); } else if (to.CanStackWith(from)) { if (to.inventoryItemReference == null) { to.inventoryItemReference = from.inventoryItemReference; }
to.count += from.count; from.ClearData(); } else {
InventoryItem tempItemReference = to.inventoryItemReference;
Sprite tempSprite = to.sprite; string tempName = to.itemName; int tempCount = to.count;
to.inventoryItemReference = from.inventoryItemReference; to.sprite = from.sprite;
to.itemName = from.itemName; to.count = from.count;
from.inventoryItemReference = tempItemReference; from.sprite = tempSprite;
from.itemName = tempName; from.count = tempCount; } from.UpdateUI(); to.UpdateUI();
if (from.slotManager != null) { from.slotManager.RefreshInventoryList(); }
if (to.slotManager != null && to.slotManager != from.slotManager) { to.slotManager.RefreshInventoryList(); } }
public void OnPointerClick(PointerEventData eventData) { if (eventData == null || eventData.button != PointerEventData.InputButton.Right) { return; }
TryActivateInventoryBuilding(); } private void CreateDragIcon() { DestroyDragIcon();
EnsureDragCanvas(); if (dragCanvas == null) { return; }
dragIconObject = new GameObject("InventoryDragIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
dragIconObject.transform.SetParent(dragCanvas.transform, false);
dragIconObject.transform.SetAsLastSibling();
CanvasGroup canvasGroup = dragIconObject.GetComponent<CanvasGroup>();
canvasGroup.blocksRaycasts = false; canvasGroup.interactable = false;
canvasGroup.alpha = 0.85f; Image dragIconImage = dragIconObject.GetComponent<Image>();
dragIconImage.raycastTarget = false; dragIconImage.sprite = sprite;
dragIconImage.preserveAspect = true; dragIconImage.enabled = sprite != null;
RectTransform dragRect = dragIconObject.GetComponent<RectTransform>();
if (image != null) { RectTransform sourceRect = image.rectTransform;
dragRect.sizeDelta = sourceRect.rect.size; } else { dragRect.sizeDelta = new Vector2(64f, 64f); } }
private void EnsureDragCanvas() { if (dragCanvas != null) { return; }
Canvas selfCanvas = GetComponentInParent<Canvas>(); if (selfCanvas == null) { return; }
dragCanvas = selfCanvas.rootCanvas != null ? selfCanvas.rootCanvas : selfCanvas; }
private static void UpdateDragIconPosition(PointerEventData eventData) { if (dragIconObject == null || dragCanvas == null) { return; }
RectTransform dragRect = dragIconObject.GetComponent<RectTransform>();
RectTransform canvasRect = dragCanvas.transform as RectTransform;
Camera eventCamera = dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera;
if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventCamera, out Vector2 localPoint)) { dragRect.localPosition = localPoint; } }
private static void DestroyDragIcon() { if (dragIconObject != null) {
Destroy(dragIconObject); dragIconObject = null; } }
private void TryActivateInventoryBuilding() { if (IsEmpty()) { return; }
InventoryItem inventoryItem = ResolveInventoryItemReference();
if (inventoryItem == null || inventoryItem.itemType != InventoryItemType.Building) { return; }
if (inventoryItem.itemPrefab == null) {
Debug.LogWarning($"Slot: Building item '{inventoryItem.name}' is missing itemPrefab.", this);
return; } RayCastScriptTest buildController = FindBuildController();
if (buildController == null) {
Debug.LogWarning("Slot: RayCastScriptTest was not found, cannot enter build mode from inventory.", this);
return; } if (!buildController.TrySelectInventoryBuildingItem(inventoryItem)) { return; }
InventoryController inventoryController = FindInventoryController();
if (inventoryController != null) { inventoryController.CloseInventory(); } }
private InventoryItem ResolveInventoryItemReference() { if (inventoryItemReference != null) { return inventoryItemReference; }
string normalizedItemName = NormalizeItemName(itemName);
if (string.IsNullOrEmpty(normalizedItemName)) { return null; }
InventoryItem[] allItems = UnitySceneSearch.FindAll<InventoryItem>();
for (int i = 0; i < allItems.Length; i++) { InventoryItem candidate = allItems[i];
if (candidate == null) { continue; }
if (!string.Equals(GetInventoryItemName(candidate), normalizedItemName, System.StringComparison.OrdinalIgnoreCase)) { continue; }
inventoryItemReference = candidate; return candidate; } return null; }
private static RayCastScriptTest FindBuildController() {
return UnitySceneSearch.FindFirst<RayCastScriptTest>(); }
private InventoryController FindInventoryController() {
InventoryController controller = GetComponentInParent<InventoryController>();
if (controller != null) { return controller; }
return UnitySceneSearch.FindFirst<InventoryController>(); }
private static string GetInventoryItemName(InventoryItem inventoryItem) { if (inventoryItem == null) { return string.Empty; }
string itemName = NormalizeItemName(inventoryItem.nameofitem);
if (!string.IsNullOrEmpty(itemName)) { return itemName; }
return NormalizeItemName(inventoryItem.name); }
private static string NormalizeItemName(string rawName) { if (string.IsNullOrWhiteSpace(rawName)) { return string.Empty; }
return rawName.Trim(); } private string GetComparableItemName() {
string normalizedSlotName = NormalizeItemName(itemName);
if (!string.IsNullOrEmpty(normalizedSlotName)) { return normalizedSlotName; }
if (inventoryItemReference != null) { return GetInventoryItemName(inventoryItemReference); }
return string.Empty; } }
