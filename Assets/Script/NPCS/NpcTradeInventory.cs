using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Yarn.Unity;
public static class NpcTradeInventory
{
    private static readonly Dictionary<string, InventoryItem> RuntimeItems = new Dictionary<string, InventoryItem>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Sprite> RuntimeSprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
    private const string QuestIconResourcesPath = "QuestItems/";
    private const string LumberQuestRequirements = "Wood:60|Stick:25|iron:12|flaming_ore:4";
    private const string MinerQuestRequirements = "diamond:15|radium:10|plasma:6|gold:10";
    private const string DevF10ExtraItems = "iron:5|gold:5|diamond:5|radium:5|plasma:5|flaming_ore:5|Stick:20|LittleStone:20";
    private const string EndSceneName = "End";
    private static bool PendingEndSceneLoad;
    private static float pendingEndSceneLoadAt;
    [YarnFunction("has_items")]
    public static bool HasItems(string requirements)
    {
        Dictionary<string, int> requiredItems = ParseRequirements(requirements);
        if (requiredItems.Count == 0) { return true; }
        if (TryGetInventoryManagerAvailable(out Dictionary<string, int> managerItems) && HasRequiredItems(managerItems, requiredItems)) { return true; }
        if (TryGetSlotManagerAvailable(out Dictionary<string, int> slotItems) && HasRequiredItems(slotItems, requiredItems)) { return true; }
        return false;
    }
    [YarnFunction("has_item")]
    public static bool HasItem(string itemName, int amount)
    {
        if (string.IsNullOrWhiteSpace(itemName)) { return false; }
        return HasItems($"{itemName}:{Mathf.Max(1, amount)}");
    }
    [YarnCommand("give_item")]
    public static void GiveItem(string itemName, int amount)
    {
        InventoryItem item = FindOrCreateItemDefinition(itemName); if (item == null) { return; }
        int resolvedAmount = Mathf.Max(1, amount);
        if (!TryAddItem(item, resolvedAmount)) { Debug.LogWarning($"NpcTradeInventory: Could not add '{itemName}' to inventory."); return; }
        ShowQuestInfo($"Received {resolvedAmount} {NormalizeItemName(itemName)}.", item.inventorysprite);
    }
    [YarnCommand("take_items")]
    public static void TakeItems(string requirements)
    {
        if (!TryConsumeItems(ParseRequirements(requirements))) { Debug.LogWarning($"NpcTradeInventory: Could not consume required items '{requirements}'."); }
    }
    [YarnCommand("trade_items")]
    public static void TradeItems(string requirements, string rewardItemName, int rewardAmount)
    {
        Dictionary<string, int> requiredItems = ParseRequirements(requirements);
        InventoryItem rewardItem = FindOrCreateItemDefinition(rewardItemName);
        if (rewardItem == null || !CanAddItem(rewardItem))
        {
            Debug.LogWarning($"NpcTradeInventory: Trade failed, inventory cannot accept reward '{rewardItemName}'.");
            return;
        }
        if (!TryConsumeItems(requiredItems))
        {
            Debug.LogWarning($"NpcTradeInventory: Trade failed, missing required items '{requirements}'.");
            return;
        }
        int resolvedRewardAmount = Mathf.Max(1, rewardAmount);
        if (!TryAddItem(rewardItem, resolvedRewardAmount))
        {
            Debug.LogWarning($"NpcTradeInventory: Trade consumed items but could not add reward '{rewardItemName}'.");
            return;
        }
        ShowQuestInfo($"Received {resolvedRewardAmount} {NormalizeItemName(rewardItemName)}.", rewardItem.inventorysprite);
    }
    [YarnCommand("complete_game")]
    public static void CompleteGame()
    {
        if (!HasItem("SailBoat", 1))
        {
            Debug.LogWarning("NpcTradeInventory: final trade did not complete, SailBoat was not added.");
            return;
        }
        const string message = "YOU WON - Fisherman accepted the Spyglass and Anchor.";
        Debug.Log(message);
        InventoryItem boatItem = FindOrCreateItemDefinition("SailBoat");
        ShowQuestInfo("YOU WON! The fisherman accepted the Anchor and Spyglass.", boatItem != null ? boatItem.inventorysprite : null);
        QueueEndSceneLoad();
    }
    [YarnCommand("load_end_scene")]
    public static void QueueEndSceneLoad()
    {
        PendingEndSceneLoad = true;
        pendingEndSceneLoadAt = Time.unscaledTime + 0.25f;
    }
    public static void UpdatePendingEndSceneLoad()
    {
        if (!PendingEndSceneLoad || Time.unscaledTime < pendingEndSceneLoadAt || DialogueState.IsConversationRunning) { return; }
        PendingEndSceneLoad = false;
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded(EndSceneName))
        {
            SceneManager.LoadScene(EndSceneName);
            return;
        }
        Debug.LogWarning("NpcTradeInventory: End scene is not in Build Settings, cannot load ending.");
    }
    [YarnCommand("dev_grant_final_quest_items")]
    public static void DevGrantFinalQuestItems()
    {
        GiveDevItem("Spyglass", 1);
        GiveDevItem("Anchor", 1);
        GiveDevItems(DevF10ExtraItems);
        InventoryItem iconItem = FindOrCreateItemDefinition("Spyglass");
        ShowQuestInfo("DEV: Added Spyglass, Anchor, ores, sticks, and stones.", iconItem != null ? iconItem.inventorysprite : null);
    }
    [YarnCommand("dev_grant_lumber_miner_quest_items")]
    public static void DevGrantLumberMinerQuestItems()
    {
        Dictionary<string, int> itemsToGrant = ParseRequirements($"{LumberQuestRequirements}|{MinerQuestRequirements}");
        int grantedItemTypes = 0;
        foreach (KeyValuePair<string, int> itemToGrant in itemsToGrant)
        {
            InventoryItem item = FindOrCreateItemDefinition(itemToGrant.Key);
            if (item == null || !TryAddItem(item, itemToGrant.Value))
            {
                Debug.LogWarning($"NpcTradeInventory: DEV grant could not add '{itemToGrant.Key}'.");
                continue;
            }
            grantedItemTypes++;
        }
        Debug.Log("DEV QUEST GRANT: Added Lumber and Miner quest materials to inventory.");
        InventoryItem iconItem = FindOrCreateItemDefinition("Spyglass");
        ShowQuestInfo($"DEV: Added {grantedItemTypes} quest material stacks for Lumber and Miner.", iconItem != null ? iconItem.inventorysprite : null);
    }
    private static void GiveDevItem(string itemName, int amount)
    {
        InventoryItem item = FindOrCreateItemDefinition(itemName);
        if (item == null || !TryAddItem(item, Mathf.Max(1, amount))) { Debug.LogWarning($"NpcTradeInventory: DEV grant could not add '{itemName}'."); }
    }
    private static void GiveDevItems(string requirements)
    {
        Dictionary<string, int> itemsToGrant = ParseRequirements(requirements);
        foreach (KeyValuePair<string, int> itemToGrant in itemsToGrant)
        {
            GiveDevItem(itemToGrant.Key, itemToGrant.Value);
        }
    }
    public static InventoryItem FindOrCreateItemDefinition(string itemName)
    {
        string normalizedName = NormalizeItemName(itemName);
        if (string.IsNullOrEmpty(normalizedName)) { return null; }
        InventoryItem sceneItem = FindInventoryItemInScene(normalizedName);
        if (sceneItem != null) { return sceneItem; }
        if (RuntimeItems.TryGetValue(normalizedName, out InventoryItem runtimeItem) && runtimeItem != null)
        {
            Sprite refreshedIcon = GetOrCreateIcon(normalizedName);
            if (refreshedIcon != null) { runtimeItem.inventorysprite = refreshedIcon; }
            return runtimeItem;
        }
        GameObject itemObject = new GameObject($"RuntimeQuestItem_{normalizedName}");
        itemObject.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(itemObject);
        runtimeItem = itemObject.AddComponent<InventoryItem>(); runtimeItem.name = normalizedName;
        runtimeItem.nameofitem = normalizedName;
        runtimeItem.inventorysprite = GetOrCreateIcon(normalizedName);
        runtimeItem.itemType = InventoryItemType.Usable; runtimeItem.itemPrefab = null;
        runtimeItem.mingain = 1; runtimeItem.maxgain = 1; runtimeItem.ResolveReferences();
        RuntimeItems[normalizedName] = runtimeItem; return runtimeItem;
    }
    private static bool CanAddItem(InventoryItem item)
    {
        if (item == null) { return false; }
        InventoryManager inventoryManager = UnitySceneSearch.FindFirst<InventoryManager>();
        if (inventoryManager != null && CanInventoryManagerAddItem(inventoryManager, item)) { return true; }
        SlotManager slotManager = ResolveSlotManager(item);
        return slotManager != null && slotManager.CanAddItem(item);
    }
    private static bool CanInventoryManagerAddItem(InventoryManager manager, InventoryItem item)
    {
        if (manager == null || item == null || manager.slotlist == null) { return false; }
        string itemName = NormalizeItemName(!string.IsNullOrWhiteSpace(item.nameofitem) ? item.nameofitem : item.name);
        for (int i = 0; i < manager.slotlist.Count; i++)
        {
            SlotInsideUI slot = manager.slotlist[i]; if (slot == null) { continue; }
            if (!slot.occupied) { return true; }
            if (string.Equals(GetSlotItemName(slot), itemName, StringComparison.OrdinalIgnoreCase)) { return true; }
        }
        return false;
    }
    private static bool TryAddItem(InventoryItem item, int amount)
    {
        if (item == null || amount <= 0) { return false; }
        InventoryManager inventoryManager = UnitySceneSearch.FindFirst<InventoryManager>();
        if (inventoryManager != null && inventoryManager.AddItem(item, amount)) { return true; }
        SlotManager slotManager = ResolveSlotManager(item);
        return slotManager != null && slotManager.AddItem(item, amount);
    }
    private static bool TryConsumeItems(Dictionary<string, int> requiredItems)
    {
        if (requiredItems == null || requiredItems.Count == 0) { return true; }
        InventoryManager inventoryManager = UnitySceneSearch.FindFirst<InventoryManager>();
        if (inventoryManager != null &&
        TryGetInventoryManagerAvailable(out Dictionary<string, int> managerItems) &&
        HasRequiredItems(managerItems, requiredItems)) { return ConsumeFromInventoryManager(inventoryManager, requiredItems); }
        SlotManager slotManager = UnitySceneSearch.FindFirst<SlotManager>();
        if (slotManager != null &&
        TryGetSlotManagerAvailable(out Dictionary<string, int> slotItems) &&
        HasRequiredItems(slotItems, requiredItems)) { return slotManager.TryConsumeResources(requiredItems); }
        return false;
    }
    private static bool ConsumeFromInventoryManager(InventoryManager manager, Dictionary<string, int> requiredItems)
    {
        if (manager == null || manager.slotlist == null) { return false; }
        foreach (KeyValuePair<string, int> requirement in requiredItems)
        {
            int remaining = requirement.Value; if (remaining <= 0) { continue; }
            for (int i = 0; i < manager.slotlist.Count && remaining > 0; i++)
            {
                SlotInsideUI slot = manager.slotlist[i];
                if (slot == null || !slot.occupied || slot.count <= 0) { continue; }
                if (!string.Equals(GetSlotItemName(slot), requirement.Key, StringComparison.OrdinalIgnoreCase)) { continue; }
                int consumed = Mathf.Min(slot.count, remaining); slot.count -= consumed;
                remaining -= consumed;
                if (slot.count <= 0) { ClearSlot(slot); } else { RefreshSlot(slot, manager.UIShown); }
            }
            if (remaining > 0) { return false; }
        }
        return true;
    }
    private static bool TryGetInventoryManagerAvailable(out Dictionary<string, int> availableItems)
    {
        availableItems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        InventoryManager inventoryManager = UnitySceneSearch.FindFirst<InventoryManager>();
        if (inventoryManager == null || inventoryManager.slotlist == null) { return false; }
        for (int i = 0; i < inventoryManager.slotlist.Count; i++)
        {
            SlotInsideUI slot = inventoryManager.slotlist[i];
            if (slot == null || !slot.occupied || slot.count <= 0) { continue; }
            string itemName = GetSlotItemName(slot); if (string.IsNullOrEmpty(itemName)) { continue; }
            AddAvailableAmount(availableItems, itemName, slot.count);
        }
        return availableItems.Count > 0;
    }
    private static bool TryGetSlotManagerAvailable(out Dictionary<string, int> availableItems)
    {
        availableItems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        SlotManager slotManager = UnitySceneSearch.FindFirst<SlotManager>();
        if (slotManager == null || slotManager.slots == null) { return false; }
        slotManager.PrepareSlots(); for (int i = 0; i < slotManager.slots.Count; i++)
        {
            Slot slot = slotManager.slots[i]; if (slot == null || slot.IsEmpty()) { continue; }
            string itemName = NormalizeItemName(slot.itemName);
            if (string.IsNullOrEmpty(itemName)) { continue; }
            AddAvailableAmount(availableItems, itemName, slot.count);
        }
        return availableItems.Count > 0;
    }
    private static bool HasRequiredItems(Dictionary<string, int> availableItems, Dictionary<string, int> requiredItems)
    {
        if (availableItems == null || requiredItems == null) { return false; }
        foreach (KeyValuePair<string, int> requiredItem in requiredItems)
        {
            if (!availableItems.TryGetValue(requiredItem.Key, out int availableAmount) || availableAmount < requiredItem.Value) { return false; }
        }
        return true;
    }
    private static Dictionary<string, int> ParseRequirements(string requirements)
    {
        Dictionary<string, int> parsedItems = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(requirements)) { return parsedItems; }
        string[] entries = requirements.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < entries.Length; i++)
        {
            if (!TryParseRequirement(entries[i], out string itemName, out int amount)) { continue; }
            AddAvailableAmount(parsedItems, itemName, amount);
        }
        return parsedItems;
    }
    private static bool TryParseRequirement(string rawRequirement, out string itemName, out int amount)
    {
        itemName = string.Empty; amount = 1; string trimmed = NormalizeItemName(rawRequirement);
        if (string.IsNullOrEmpty(trimmed)) { return false; }
        int separator = trimmed.LastIndexOf(':');
        if (separator <= 0 || separator >= trimmed.Length - 1)
        {
            itemName = trimmed;
            return true;
        }
        itemName = NormalizeItemName(trimmed.Substring(0, separator));
        string amountText = trimmed.Substring(separator + 1).Trim();
        if (string.IsNullOrEmpty(itemName) || !int.TryParse(amountText, out amount)) { return false; }
        amount = Mathf.Max(1, amount); return true;
    }
    private static void AddAvailableAmount(Dictionary<string, int> target, string itemName, int amount)
    {
        string normalizedName = NormalizeItemName(itemName);
        if (target == null || string.IsNullOrEmpty(normalizedName) || amount <= 0) { return; }
        if (target.TryGetValue(normalizedName, out int currentAmount)) { target[normalizedName] = currentAmount + amount; } else { target[normalizedName] = amount; }
    }
    private static InventoryItem FindInventoryItemInScene(string itemName)
    {
        string normalizedName = NormalizeItemName(itemName);
        if (string.IsNullOrEmpty(normalizedName)) { return null; }
        InventoryItem[] items = UnitySceneSearch.FindAll<InventoryItem>();
        for (int i = 0; i < items.Length; i++)
        {
            InventoryItem item = items[i];
            if (item == null) { continue; }
            if (string.Equals(NormalizeItemName(item.nameofitem), normalizedName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NormalizeItemName(item.name), normalizedName, StringComparison.OrdinalIgnoreCase)) { return item; }
        }
        return null;
    }
    private static SlotManager ResolveSlotManager(InventoryItem item)
    {
        if (item != null)
        {
            item.ResolveReferences();
            if (item.slotManager != null) { return item.slotManager; }
        }
        return UnitySceneSearch.FindFirst<SlotManager>();
    }
    private static string GetSlotItemName(SlotInsideUI slot)
    {
        if (slot == null) { return string.Empty; }
        string slotName = NormalizeItemName(slot.nameofslot);
        if (!string.IsNullOrEmpty(slotName)) { return slotName; }
        if (slot.Item == null) { return string.Empty; }
        string itemName = NormalizeItemName(slot.Item.nameofitem);
        if (!string.IsNullOrEmpty(itemName)) { return itemName; }
        return NormalizeItemName(slot.Item.name);
    }
    private static void ClearSlot(SlotInsideUI slot)
    {
        if (slot == null) { return; }
        slot.Item = null; slot.nameofslot = string.Empty; slot.count = 0; slot.occupied = false;
        RefreshSlot(slot, slot.inventoryManager != null && slot.inventoryManager.UIShown);
    }
    private static void RefreshSlot(SlotInsideUI slot, bool uiShown)
    {
        if (slot == null) { return; }
        if (slot.image != null)
        {
            slot.image.sprite = slot.Item != null ? slot.Item.inventorysprite : null;
            slot.image.enabled = uiShown && slot.occupied && slot.Item != null;
        }
        if (slot.text != null)
        {
            slot.text.text = slot.count > 0 ? slot.count.ToString() : "0";
            slot.text.enabled = uiShown;
        }
    }
    private static Sprite GetOrCreateIcon(string itemName)
    {
        string normalizedName = NormalizeItemName(itemName);
        Sprite sprite = FindSceneOreIcon(normalizedName);
        if (sprite != null)
        {
            RuntimeSprites[normalizedName] = sprite;
            return sprite;
        }
        sprite = LoadQuestIconSprite(normalizedName);
        if (sprite != null)
        {
            RuntimeSprites[normalizedName] = sprite;
            return sprite;
        }
        if (RuntimeSprites.TryGetValue(normalizedName, out sprite) && sprite != null) { return sprite; }
        if (sprite == null)
        {
            Texture2D texture = CreateIconTexture(normalizedName);
            texture.hideFlags = HideFlags.HideAndDontSave;
            sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = $"{normalizedName}_icon";
        }
        RuntimeSprites[normalizedName] = sprite;
        return sprite;
    }
    private static Sprite FindSceneOreIcon(string itemName)
    {
        string token = ToResourceToken(itemName);
        GetRandomOreType oreTypeProvider = UnitySceneSearch.FindFirst<GetRandomOreType>();
        if (oreTypeProvider == null) { return null; }
        switch (token)
        {
            case "iron": return oreTypeProvider.ironsprite;
            case "gold": return oreTypeProvider.goldsprite;
            case "diamond": return oreTypeProvider.diamondsprite;
            case "radium": return oreTypeProvider.radiumsprite;
            case "plasma": return oreTypeProvider.plasmapsprite;
            case "flamingore": return oreTypeProvider.flaming_oresprite;
            default: return null;
        }
    }
    private static Sprite LoadQuestIconSprite(string itemName)
    {
        string resourceName = NormalizeResourceName(itemName);
        if (string.IsNullOrEmpty(resourceName)) { return null; }
        Sprite sprite = Resources.Load<Sprite>(QuestIconResourcesPath + resourceName);
        if (sprite != null) { return sprite; }
        Texture2D texture = Resources.Load<Texture2D>(QuestIconResourcesPath + resourceName);
        if (texture == null) { return null; }
        sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        sprite.name = $"{itemName}_icon"; return sprite;
    }
    private static string NormalizeResourceName(string itemName)
    {
        string normalizedName = NormalizeItemName(itemName);
        if (string.IsNullOrEmpty(normalizedName)) { return string.Empty; }
        string token = ToResourceToken(normalizedName);
        if (token.Contains("spyglass")) { return "Spyglass"; }
        if (token.Contains("anchor")) { return "Anchor"; }
        if (token == "iron") { return "Iron"; }
        if (token == "gold") { return "Gold"; }
        if (token == "diamond") { return "Diamond"; }
        if (token == "radium") { return "Radium"; }
        if (token == "plasma") { return "Plasma"; }
        if (token == "flamingore") { return "FlamingOre"; }
        return normalizedName;
    }
    private static string ToResourceToken(string itemName)
    {
        return NormalizeItemName(itemName).Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
    }
    private static void ShowQuestInfo(string message, Sprite icon)
    {
        InfoHandler infoHandler = UnitySceneSearch.FindFirst<InfoHandler>();
        if (infoHandler != null) { infoHandler.ShowInfoNow(message, icon); }
    }
    private static Texture2D CreateIconTexture(string itemName)
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color clear = new Color(0f, 0f, 0f, 0f); Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) { pixels[i] = clear; }
        texture.SetPixels(pixels);
        string token = itemName.Replace(" ", string.Empty).Replace("_", string.Empty).ToLowerInvariant();
        if (token.Contains("spyglass")) { DrawSpyglassIcon(texture); } else if (token.Contains("anchor")) { DrawAnchorIcon(texture); } else if (token.Contains("sailboat") || token.Contains("boat")) { DrawBoatIcon(texture); } else { DrawFallbackIcon(texture); }
        texture.Apply(false, false); return texture;
    }
    private static void DrawSpyglassIcon(Texture2D texture)
    {
        Color brass = new Color(0.78f, 0.48f, 0.16f, 1f);
        Color dark = new Color(0.2f, 0.14f, 0.08f, 1f);
        DrawThickLine(texture, 16, 46, 47, 20, 7, dark);
        DrawThickLine(texture, 18, 44, 45, 22, 5, brass); DrawCircle(texture, 47, 20, 8, dark);
        DrawCircle(texture, 47, 20, 5, new Color(0.52f, 0.72f, 0.8f, 1f));
    }
    private static void DrawAnchorIcon(Texture2D texture)
    {
        Color metal = new Color(0.35f, 0.38f, 0.42f, 1f);
        Color shadow = new Color(0.1f, 0.12f, 0.14f, 1f);
        DrawThickLine(texture, 32, 12, 32, 46, 5, shadow);
        DrawThickLine(texture, 32, 12, 32, 46, 3, metal);
        DrawThickLine(texture, 20, 24, 44, 24, 4, metal); DrawCircle(texture, 32, 11, 6, metal);
        DrawThickLine(texture, 32, 46, 18, 34, 4, metal);
        DrawThickLine(texture, 32, 46, 46, 34, 4, metal);
        DrawThickLine(texture, 18, 34, 14, 42, 4, metal);
        DrawThickLine(texture, 46, 34, 50, 42, 4, metal);
    }
    private static void DrawBoatIcon(Texture2D texture)
    {
        Color hull = new Color(0.42f, 0.23f, 0.11f, 1f);
        Color sail = new Color(0.82f, 0.78f, 0.64f, 1f);
        Color mast = new Color(0.22f, 0.14f, 0.08f, 1f);
        DrawFilledRect(texture, 16, 42, 48, 50, hull);
        DrawThickLine(texture, 32, 15, 32, 43, 3, mast);
        DrawTriangle(texture, 33, 18, 33, 38, 49, 38, sail);
        DrawTriangle(texture, 31, 20, 31, 39, 17, 39, new Color(0.7f, 0.66f, 0.55f, 1f));
    }
    private static void DrawFallbackIcon(Texture2D texture)
    {
        DrawCircle(texture, 32, 32, 20, new Color(0.62f, 0.48f, 0.24f, 1f));
        DrawCircle(texture, 32, 32, 14, new Color(0.8f, 0.68f, 0.42f, 1f));
    }
    private static void DrawThickLine(Texture2D texture, int x0, int y0, int x1, int y1, int thickness, Color color)
    {
        int dx = Mathf.Abs(x1 - x0); int dy = Mathf.Abs(y1 - y0); int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1; int error = dx - dy; while (true)
        {
            DrawCircle(texture, x0, y0, thickness, color); if (x0 == x1 && y0 == y1) { break; }
            int error2 = error * 2; if (error2 > -dy) { error -= dy; x0 += sx; }
            if (error2 < dx) { error += dx; y0 += sy; }
        }
    }
    private static void DrawCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
    {
        int radiusSquared = radius * radius;
        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                int dx = x - centerX;
                int dy = y - centerY;
                if (dx * dx + dy * dy <= radiusSquared) { SetPixelSafe(texture, x, y, color); }
            }
        }
    }
    private static void DrawFilledRect(Texture2D texture, int minX, int minY, int maxX, int maxY, Color color)
    {
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++) { SetPixelSafe(texture, x, y, color); }
        }
    }
    private static void DrawTriangle(Texture2D texture, int x0, int y0, int x1, int y1, int x2, int y2, Color color)
    {
        int minX = Mathf.Min(x0, Mathf.Min(x1, x2)); int maxX = Mathf.Max(x0, Mathf.Max(x1, x2));
        int minY = Mathf.Min(y0, Mathf.Min(y1, y2)); int maxY = Mathf.Max(y0, Mathf.Max(y1, y2));
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (PointInTriangle(x, y, x0, y0, x1, y1, x2, y2)) { SetPixelSafe(texture, x, y, color); }
            }
        }
    }
    private static bool PointInTriangle(int px, int py, int x0, int y0, int x1, int y1, int x2, int y2)
    {
        float d1 = Sign(px, py, x0, y0, x1, y1); float d2 = Sign(px, py, x1, y1, x2, y2);
        float d3 = Sign(px, py, x2, y2, x0, y0); bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f; return !(hasNegative && hasPositive);
    }
    private static float Sign(float px, float py, float x0, float y0, float x1, float y1)
    {
        return (px - x1) * (y0 - y1) - (x0 - x1) * (py - y1);
    }
    private static void SetPixelSafe(Texture2D texture, int x, int y, Color color)
    {
        if (texture == null || x < 0 || y < 0 || x >= texture.width || y >= texture.height) { return; }
        texture.SetPixel(x, y, color);
    }
    private static string NormalizeItemName(string itemName)
    {
        return string.IsNullOrWhiteSpace(itemName) ? string.Empty : itemName.Trim();
    }
}
