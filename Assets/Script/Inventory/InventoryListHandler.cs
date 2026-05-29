using System.Collections.Generic; using UnityEngine;
public class InventoryListHandler : MonoBehaviour {
public Dictionary<InventoryItem, int> itemlist = new Dictionary<InventoryItem, int>();
private readonly Dictionary<string, InventoryItem> itemKeyByName = new Dictionary<string, InventoryItem>(System.StringComparer.OrdinalIgnoreCase);
private static readonly System.Reflection.FieldInfo SlotItemReferenceField = typeof(Slot).GetField("inventoryItemReference");
public void AddItem(InventoryItem inventoryItem, int amount) {
if (inventoryItem == null || amount <= 0) { return; }
InventoryItem itemKey = ResolveOrRegisterItemKey(GetInventoryItemName(inventoryItem), inventoryItem);
AddAmount(itemKey, amount); } public void RebuildFromSlots(List<Slot> slots) {
itemlist.Clear(); if (slots == null) { return; } for (int i = 0; i < slots.Count; i++) {
Slot slot = slots[i]; if (slot == null || slot.IsEmpty()) { continue; }
InventoryItem itemKey = ResolveItemKeyFromSlot(slot);
int safeCount = Mathf.Max(0, slot.count);
if (itemKey == null || safeCount <= 0) { continue; } AddAmount(itemKey, safeCount); } }
private void AddAmount(InventoryItem itemKey, int amount) {
if (itemKey == null || amount <= 0) { return; }
if (itemlist.TryGetValue(itemKey, out int currentAmount)) {
itemlist[itemKey] = currentAmount + amount; return; } itemlist[itemKey] = amount; }
private InventoryItem ResolveItemKeyFromSlot(Slot slot) {
if (slot == null) { return null; } string slotName = NormalizeItemName(slot.itemName);
if (SlotItemReferenceField != null) {
InventoryItem slotReference = SlotItemReferenceField.GetValue(slot) as InventoryItem;
if (slotReference != null) { string referenceName = GetInventoryItemName(slotReference);
if (string.IsNullOrEmpty(slotName) || string.Equals(slotName, referenceName, System.StringComparison.OrdinalIgnoreCase)) { return ResolveOrRegisterItemKey(referenceName, slotReference); } } }
return ResolveOrRegisterItemKey(slotName, null); }
private InventoryItem ResolveOrRegisterItemKey(string itemName, InventoryItem preferredItem) {
string normalizedName = NormalizeItemName(itemName);
if (string.IsNullOrEmpty(normalizedName)) { return preferredItem; }
if (preferredItem != null) { itemKeyByName[normalizedName] = preferredItem;
return preferredItem; }
if (itemKeyByName.TryGetValue(normalizedName, out InventoryItem cachedItem) && cachedItem != null) { return cachedItem; }
InventoryItem foundItem = FindInventoryItemInScene(normalizedName);
if (foundItem != null) { itemKeyByName[normalizedName] = foundItem; } return foundItem; }
private static string GetInventoryItemName(InventoryItem inventoryItem) { if (inventoryItem == null) { return string.Empty; }
string itemName = NormalizeItemName(inventoryItem.nameofitem);
if (!string.IsNullOrEmpty(itemName)) { return itemName; }
return NormalizeItemName(inventoryItem.name); }
private static string NormalizeItemName(string itemName) {
if (string.IsNullOrWhiteSpace(itemName)) { return string.Empty; }
return itemName.Trim(); }
private static InventoryItem FindInventoryItemInScene(string normalizedItemName) {
if (string.IsNullOrEmpty(normalizedItemName)) { return null; }
InventoryItem[] allItems = UnitySceneSearch.FindAll<InventoryItem>();
for (int i = 0; i < allItems.Length; i++) { InventoryItem candidate = allItems[i];
if (candidate == null) { continue; }
if (string.Equals(NormalizeItemName(candidate.nameofitem), normalizedItemName, System.StringComparison.OrdinalIgnoreCase) ||
string.Equals(NormalizeItemName(candidate.name), normalizedItemName, System.StringComparison.OrdinalIgnoreCase)) { return candidate; } }
return null; } }
