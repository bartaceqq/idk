using System.Collections.Generic; using UnityEngine; using UnityEngine.EventSystems; using UnityEngine.UI;
public class WeaponSlot : MonoBehaviour, IDropHandler, IPointerClickHandler {
public Image Backgroundimage; public ItemSwitchScript itemSwitchScript;
public Image iconImage; public bool allowToolItems = true;
public bool allowSwordItems = true; public bool hideIconWhenEmpty = true;
public InventoryItem equippedItemReference; public string equippedItemName;
public Sprite equippedSprite;
private static readonly List<WeaponSlot> ActiveWeaponSlots = new List<WeaponSlot>();
private void Awake() { ResolveReferences(); UpdateVisual(); }
private void OnEnable() { if (!ActiveWeaponSlots.Contains(this)) { ActiveWeaponSlots.Add(this); }
UpdateVisual(); SyncAllWeaponSlotsToItemSwitch(); } private void OnDisable() {
ActiveWeaponSlots.Remove(this); SyncAllWeaponSlotsToItemSwitch(); }
private void OnValidate() { if (!Application.isPlaying) { ResolveReferences();
UpdateVisual(); } } public void OnDrop(PointerEventData eventData) {
Slot sourceSlot = ResolveDraggedLegacySlot(eventData);
if (sourceSlot == null) { sourceSlot = Slot.CurrentDragSource; }
if (sourceSlot != null && !sourceSlot.IsEmpty()) { TryAssignFromSlot(sourceSlot, true);
return; } SlotInsideUI remakeSourceSlot = ResolveDraggedRemakeSlot(eventData);
if (remakeSourceSlot == null) { remakeSourceSlot = SlotInsideUI.CurrentDragSource; }
if (remakeSourceSlot == null || !remakeSourceSlot.HasItem()) { return; }
TryAssignFromRemakeSlot(remakeSourceSlot, true); }
public void OnPointerClick(PointerEventData eventData) { if (eventData == null) { return; }
if (eventData.button == PointerEventData.InputButton.Right) { ClearEquippedItem();
return; }
if (eventData.button == PointerEventData.InputButton.Left) { HandleLeftClickEquipToggle(); } }
public void ClearEquippedItem() { equippedItemReference = null;
equippedItemName = string.Empty; equippedSprite = null; UpdateVisual();
SyncAllWeaponSlotsToItemSwitch(); } public void RefreshVisual() { UpdateVisual(); }
public static List<WeaponSlot> GetOrderedWeaponSlots() { PruneNullWeaponSlots();
List<WeaponSlot> orderedSlots = new List<WeaponSlot>(ActiveWeaponSlots);
orderedSlots.Sort(CompareSlotsTopLeft); return orderedSlots; }
public string GetAssignedItemName() {
string normalizedName = NormalizeItemName(equippedItemName);
if (!string.IsNullOrEmpty(normalizedName)) { return normalizedName; }
if (equippedItemReference != null) {
normalizedName = NormalizeItemName(equippedItemReference.nameofitem);
if (!string.IsNullOrEmpty(normalizedName)) { return normalizedName; }
return NormalizeItemName(equippedItemReference.name); } return string.Empty; }
private bool TryAssignFromSlot(Slot sourceSlot, bool logWarnings) { if (sourceSlot == null || sourceSlot.IsEmpty()) { return false; }
InventoryItem sourceItem = ResolveInventoryItemFromSlot(sourceSlot);
string sourceName = NormalizeItemName(sourceSlot.itemName);
if (!CanAcceptItem(sourceItem, sourceName, out string resolvedEquipName, out string rejectReason)) { if (logWarnings) { Debug.LogWarning($"WeaponSlot: {rejectReason}", this); }
return false; } equippedItemReference = sourceItem; equippedItemName = resolvedEquipName;
equippedSprite = sourceSlot.sprite; UpdateVisual(); SyncAllWeaponSlotsToItemSwitch();
return true; }
private bool TryAssignFromRemakeSlot(SlotInsideUI sourceSlot, bool logWarnings) { if (sourceSlot == null || !sourceSlot.HasItem()) { return false; }
InventoryItem sourceItem = sourceSlot.Item;
string sourceName = ResolveRemakeSlotItemName(sourceSlot);
if (!CanAcceptItem(sourceItem, sourceName, out string resolvedEquipName, out string rejectReason)) { if (logWarnings) { Debug.LogWarning($"WeaponSlot: {rejectReason}", this); }
return false; } Sprite sourceSprite = sourceSlot.image != null ? sourceSlot.image.sprite
: null;
if (sourceSprite == null && sourceItem != null) { sourceSprite = sourceItem.inventorysprite; }
equippedItemReference = sourceItem; equippedItemName = resolvedEquipName;
equippedSprite = sourceSprite; UpdateVisual(); SyncAllWeaponSlotsToItemSwitch();
return true; }
private bool CanAcceptItem(InventoryItem sourceItem, string sourceItemName, out string resolvedEquipName, out string reason) {
resolvedEquipName = string.Empty; reason = string.Empty; ResolveReferences();
if (itemSwitchScript == null) { reason = "ItemSwitchScript reference is missing.";
return false; } if (string.IsNullOrEmpty(sourceItemName)) {
reason = "Dragged item name is empty."; return false; }
if (!TryResolveEquipName(sourceItem, sourceItemName, out resolvedEquipName)) {
reason = $"No Item entry with name '{sourceItemName}' exists in ItemSwitchScript.";
return false; } bool typeAllowed; if (sourceItem != null) {
bool isTool = sourceItem.itemType == InventoryItemType.Tool;
bool isSword = sourceItem.itemType == InventoryItemType.Sword;
typeAllowed = (allowToolItems && isTool) || (allowSwordItems && isSword);
if (!typeAllowed) { typeAllowed = IsAllowedByResolvedName(resolvedEquipName) || IsAllowedByResolvedName(sourceItemName); } } else { typeAllowed = IsAllowedByResolvedName(resolvedEquipName); }
if (!typeAllowed) { reason = sourceItem != null
? $"Only configured weapon types are allowed. Received type: {sourceItem.itemType}."
: "Dragged item does not match allowed weapon categories."; return false; }
if (string.IsNullOrWhiteSpace(resolvedEquipName)) {
reason = "Resolved equip name is empty."; return false; } return true; }
private static InventoryItem ResolveInventoryItemFromSlot(Slot sourceSlot) { if (sourceSlot == null) { return null; }
if (sourceSlot.inventoryItemReference != null) { return sourceSlot.inventoryItemReference; }
string sourceName = NormalizeItemName(sourceSlot.itemName);
if (string.IsNullOrEmpty(sourceName)) { return null; }
InventoryItem[] allItems = UnitySceneSearch.FindAll<InventoryItem>();
for (int i = 0; i < allItems.Length; i++) { InventoryItem candidate = allItems[i];
if (candidate == null) { continue; }
if (string.Equals(NormalizeItemName(candidate.nameofitem), sourceName, System.StringComparison.OrdinalIgnoreCase) ||
string.Equals(NormalizeItemName(candidate.name), sourceName, System.StringComparison.OrdinalIgnoreCase)) { return candidate; } }
return null; }
private void ResolveReferences() { if (Backgroundimage == null) { Backgroundimage = GetComponent<Image>(); }
if (iconImage != null && iconImage.gameObject == gameObject) { iconImage = null; }
if (iconImage == null) { Transform preferred = transform.Find("ImagePlace");
if (preferred == null) { preferred = transform.Find("WhiteInside"); }
if (preferred != null) { iconImage = preferred.GetComponent<Image>(); } }
if (iconImage == null) { Image[] images = GetComponentsInChildren<Image>(true);
for (int i = 0; i < images.Length; i++) { Image candidate = images[i];
if (candidate == null || candidate.gameObject == gameObject) { continue; }
if (candidate.name == "BlackBakground") { continue; } iconImage = candidate; break; } }
if (itemSwitchScript == null) { itemSwitchScript = GetComponentInParent<ItemSwitchScript>(); }
if (itemSwitchScript == null) {
itemSwitchScript = UnitySceneSearch.FindFirst<ItemSwitchScript>(); } }
private void UpdateVisual() { ResolveReferences();
bool inventoryVisible = IsInventoryVisible();
Sprite backgroundSprite = ResolveBackgroundSprite();
bool hasDedicatedSurface = HasDedicatedInteractionSurface();
bool hasItem = equippedSprite != null;
if (Backgroundimage != null) { if (Backgroundimage.sprite == null && backgroundSprite != null) { Backgroundimage.sprite = backgroundSprite; }
Backgroundimage.enabled = inventoryVisible && Backgroundimage.sprite != null;
Backgroundimage.raycastTarget = inventoryVisible && !hasDedicatedSurface; }
if (iconImage == null) { return; } iconImage.sprite = equippedSprite;
iconImage.color = hasItem ? Color.white : new Color(1f, 1f, 1f, 0f);
iconImage.enabled = inventoryVisible && (hasItem || hasDedicatedSurface || !hideIconWhenEmpty);
iconImage.raycastTarget = inventoryVisible && hasDedicatedSurface;
iconImage.preserveAspect = hasItem; HideExtraPlaceholderImages(); }
private void HideExtraPlaceholderImages() {
Image[] images = GetComponentsInChildren<Image>(true);
for (int i = 0; i < images.Length; i++) { Image candidate = images[i];
if (candidate == null || candidate == iconImage || candidate.gameObject == gameObject) { continue; }
if (candidate.name == "BlackBakground") { continue; }
if (candidate.sprite == null) { candidate.enabled = false; } } }
private static void SyncAllWeaponSlotsToItemSwitch() {
HashSet<string> equippedNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
HashSet<ItemSwitchScript> switchScripts = new HashSet<ItemSwitchScript>();
PruneNullWeaponSlots(); for (int i = ActiveWeaponSlots.Count - 1; i >= 0; i--) {
WeaponSlot slot = ActiveWeaponSlots[i]; slot.ResolveReferences();
if (slot.itemSwitchScript != null) { switchScripts.Add(slot.itemSwitchScript); }
string normalizedName = NormalizeItemName(slot.equippedItemName);
if (!string.IsNullOrEmpty(normalizedName)) { equippedNames.Add(normalizedName); } }
if (switchScripts.Count == 0) {
ItemSwitchScript fallback = UnitySceneSearch.FindFirst<ItemSwitchScript>();
if (fallback != null) { switchScripts.Add(fallback); } }
foreach (ItemSwitchScript switchScript in switchScripts) { if (switchScript != null) { switchScript.ApplyEquippedItemNames(equippedNames); } } }
private static string NormalizeItemName(string rawName) { if (string.IsNullOrWhiteSpace(rawName)) { return string.Empty; }
string normalized = rawName.Trim();
if (normalized.EndsWith("(Clone)", System.StringComparison.OrdinalIgnoreCase)) { normalized = normalized.Substring(0, normalized.Length - "(Clone)".Length).Trim(); }
return normalized; }
private static string ResolveRemakeSlotItemName(SlotInsideUI sourceSlot) { if (sourceSlot == null) { return string.Empty; }
string slotName = NormalizeItemName(sourceSlot.nameofslot);
if (!string.IsNullOrEmpty(slotName)) { return slotName; }
if (sourceSlot.Item == null) { return string.Empty; }
string itemName = NormalizeItemName(sourceSlot.Item.nameofitem);
if (!string.IsNullOrEmpty(itemName)) { return itemName; }
string objectName = NormalizeItemName(sourceSlot.Item.name);
if (!string.IsNullOrEmpty(objectName)) { return objectName; }
if (sourceSlot.Item.itemPrefab != null) { return NormalizeItemName(sourceSlot.Item.itemPrefab.name); }
return string.Empty; }
private bool TryResolveEquipName(InventoryItem sourceItem, string sourceItemName, out string resolvedName) {
resolvedName = string.Empty; ResolveReferences();
if (itemSwitchScript == null) { return false; }
List<string> identityCandidates = new List<string>(6);
List<string> prefabCandidates = new List<string>(2);
AddCandidateName(identityCandidates, sourceItemName); if (sourceItem != null) {
AddCandidateName(identityCandidates, sourceItem.nameofitem);
AddCandidateName(identityCandidates, sourceItem.name);
if (sourceItem.itemPrefab != null) { AddCandidateName(prefabCandidates, sourceItem.itemPrefab.name); } }
if (TryResolveExactItemName(identityCandidates, out resolvedName)) { return true; }
if (TryResolveMappedItemName(identityCandidates, out resolvedName)) { return true; }
if (TryResolveExactItemName(prefabCandidates, out resolvedName)) { return true; }
if (TryResolveMappedItemName(prefabCandidates, out resolvedName)) { return true; }
return false; }
private bool TryResolveExactItemName(List<string> candidates, out string resolvedName) {
resolvedName = string.Empty; for (int i = 0; i < candidates.Count; i++) {
string candidate = candidates[i]; if (itemSwitchScript.HasItemNamed(candidate)) {
resolvedName = candidate; return true; } } return false; }
private bool TryResolveMappedItemName(List<string> candidates, out string resolvedName) {
resolvedName = string.Empty; for (int i = 0; i < candidates.Count; i++) {
string mapped = MapCommonWeaponName(candidates[i]);
if (string.IsNullOrEmpty(mapped)) { continue; }
if (itemSwitchScript.HasItemNamed(mapped)) { resolvedName = mapped; return true; }
if (itemSwitchScript.TryResolveFirstWeaponByCategory(mapped, out resolvedName)) { return true; } }
return false; }
private static void AddCandidateName(List<string> candidates, string rawName) {
string normalized = NormalizeItemName(rawName);
if (string.IsNullOrEmpty(normalized) || candidates.Contains(normalized)) { return; }
candidates.Add(normalized); } private static string MapCommonWeaponName(string rawName) {
string normalized = NormalizeItemName(rawName);
if (string.IsNullOrEmpty(normalized)) { return string.Empty; }
string token = normalized.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
if (token.Contains("pickaxe") || token.Contains("pick")) { return "Pickaxe"; }
if (token.Contains("sword")) { return "Sword"; }
if (token.Contains("axe")) { return "Axe"; }
return string.Empty; }
private bool IsAllowedByResolvedName(string resolvedName) {
string mapped = MapCommonWeaponName(resolvedName);
if (string.IsNullOrEmpty(mapped)) { return false; }
if (string.Equals(mapped, "Sword", System.StringComparison.OrdinalIgnoreCase)) { return allowSwordItems; }
if (string.Equals(mapped, "Axe", System.StringComparison.OrdinalIgnoreCase) ||
string.Equals(mapped, "Pickaxe", System.StringComparison.OrdinalIgnoreCase)) { return allowToolItems; }
return false; }
private static Slot ResolveDraggedLegacySlot(PointerEventData eventData) { if (eventData == null || eventData.pointerDrag == null) { return null; }
Slot direct = eventData.pointerDrag.GetComponent<Slot>();
if (direct != null) { return direct; }
return eventData.pointerDrag.GetComponentInParent<Slot>(); }
private static SlotInsideUI ResolveDraggedRemakeSlot(PointerEventData eventData) { if (eventData == null || eventData.pointerDrag == null) { return null; }
SlotInsideUI direct = eventData.pointerDrag.GetComponent<SlotInsideUI>();
if (direct != null) { return direct; }
return eventData.pointerDrag.GetComponentInParent<SlotInsideUI>(); }
private void HandleLeftClickEquipToggle() {
string assignedItemName = GetAssignedItemName();
if (string.IsNullOrEmpty(assignedItemName)) { return; } ResolveReferences();
if (itemSwitchScript == null) { return; }
itemSwitchScript.ToggleItemByName(assignedItemName); }
private Sprite ResolveBackgroundSprite() { if (Backgroundimage != null && Backgroundimage.sprite != null) { return Backgroundimage.sprite; }
Sprite fallbackSprite = FindSharedBackgroundSprite(this);
if (Backgroundimage != null && fallbackSprite != null) { Backgroundimage.sprite = fallbackSprite; }
return fallbackSprite; }
private static Sprite FindSharedBackgroundSprite(WeaponSlot requestingSlot) {
PruneNullWeaponSlots(); for (int i = 0; i < ActiveWeaponSlots.Count; i++) {
WeaponSlot candidate = ActiveWeaponSlots[i];
if (candidate == null || candidate == requestingSlot) { continue; }
candidate.ResolveReferences();
if (candidate.Backgroundimage != null && candidate.Backgroundimage.sprite != null) { return candidate.Backgroundimage.sprite; } }
WeaponSlot[] allSlots = UnitySceneSearch.FindAll<WeaponSlot>();
for (int i = 0; i < allSlots.Length; i++) { WeaponSlot candidate = allSlots[i];
if (candidate == null || candidate == requestingSlot) { continue; }
candidate.ResolveReferences();
if (candidate.Backgroundimage != null && candidate.Backgroundimage.sprite != null) { return candidate.Backgroundimage.sprite; } }
return null; }
private bool HasDedicatedInteractionSurface() { return iconImage != null && iconImage.gameObject != gameObject; }
private static bool IsInventoryVisible() { if (!Application.isPlaying) { return true; }
return InventoryManager.IsInventoryOpen || InventoryController.IsInventoryOpen; }
private static void PruneNullWeaponSlots() {
for (int i = ActiveWeaponSlots.Count - 1; i >= 0; i--) { if (ActiveWeaponSlots[i] == null) { ActiveWeaponSlots.RemoveAt(i); } } }
private static int CompareSlotsTopLeft(WeaponSlot a, WeaponSlot b) { if (a == b) return 0;
if (a == null) return 1; if (b == null) return -1;
RectTransform rectA = a.transform as RectTransform;
RectTransform rectB = b.transform as RectTransform; Vector2 posA = rectA != null
? rectA.anchoredPosition : new Vector2(a.transform.position.x, a.transform.position.y);
Vector2 posB = rectB != null ? rectB.anchoredPosition
: new Vector2(b.transform.position.x, b.transform.position.y);
int yCompare = posB.y.CompareTo(posA.y); if (yCompare != 0) { return yCompare; }
int xCompare = posA.x.CompareTo(posB.x); if (xCompare != 0) { return xCompare; }
return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()); } }
