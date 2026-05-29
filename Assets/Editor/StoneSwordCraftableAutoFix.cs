using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class StoneSwordCraftableAutoFix {
    private const string StoneSwordName = "stone_sword";
    private const string StoneSwordIconPath = "Assets/Images/SwordIcons/stone_sword_icon.png";
    private const string SwordPrefabPath = "Assets/melee weapons/Prefabs/sword2.prefab";

    static StoneSwordCraftableAutoFix() {
        EditorApplication.delayCall += RepairOpenScenes;
        EditorSceneManager.sceneOpened += (_, __) => EditorApplication.delayCall += RepairOpenScenes; }

    [MenuItem("Tools/Fixes/Repair Stone Sword Craftable")]
    public static void RepairOpenScenes() {
        Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(StoneSwordIconPath);
        GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPrefabPath);
        CraftableItem[] craftables = Resources.FindObjectsOfTypeAll<CraftableItem>();

        bool changedAny = false;
        for (int i = 0; i < craftables.Length; i++) {
            CraftableItem craftable = craftables[i];
            if (craftable == null || !craftable.gameObject.scene.IsValid()) { continue; }

            if (!IsStoneSwordCandidate(craftable)) { continue; }

            bool changed = false;
            if (craftable.gameObject.name != "stone_swordcraftable") {
                Undo.RecordObject(craftable.gameObject, "Repair Stone Sword Craftable");
                craftable.gameObject.name = "stone_swordcraftable";
                changed = true; }

            Undo.RecordObject(craftable, "Repair Stone Sword Craftable");
            changed |= SetCraftableValues(craftable, icon, swordPrefab);

            if (craftable.craftedInventoryItem != null) {
                Undo.RecordObject(craftable.craftedInventoryItem, "Repair Stone Sword Craftable");
                changed |= SetInventoryItemValues(craftable.craftedInventoryItem, icon, swordPrefab); }

            if (!changed) { continue; }

            EditorUtility.SetDirty(craftable.gameObject);
            EditorUtility.SetDirty(craftable);
            if (craftable.craftedInventoryItem != null) { EditorUtility.SetDirty(craftable.craftedInventoryItem); }

            EditorSceneManager.MarkSceneDirty(craftable.gameObject.scene);
            changedAny = true; }

        if (changedAny) { Debug.Log("Stone sword craftable data repaired in the open scene."); } }

    private static bool SetCraftableValues(CraftableItem craftable, Sprite icon, GameObject swordPrefab) {
        bool changed = false;
        if (craftable.name != StoneSwordName) {
            craftable.name = StoneSwordName;
            changed = true; }

        if (craftable.itemType != InventoryItemType.Sword) {
            craftable.itemType = InventoryItemType.Sword;
            changed = true; }

        if (icon != null && craftable.sprite != icon) {
            craftable.sprite = icon;
            changed = true; }

        if (swordPrefab != null && craftable.itemPrefab != swordPrefab) {
            craftable.itemPrefab = swordPrefab;
            changed = true; }

        return changed; }

    private static bool SetInventoryItemValues(InventoryItem item, Sprite icon, GameObject swordPrefab) {
        bool changed = false;
        if (item.nameofitem != StoneSwordName) {
            item.nameofitem = StoneSwordName;
            changed = true; }

        if (item.itemType != InventoryItemType.Sword) {
            item.itemType = InventoryItemType.Sword;
            changed = true; }

        if (icon != null && item.inventorysprite != icon) {
            item.inventorysprite = icon;
            changed = true; }

        if (swordPrefab != null && item.itemPrefab != swordPrefab) {
            item.itemPrefab = swordPrefab;
            changed = true; }

        return changed; }

    private static bool IsStoneSwordCandidate(CraftableItem craftable) {
        string token = (craftable.name + " " + craftable.gameObject.name).Replace("_", string.Empty).Replace(" ", string.Empty).ToLowerInvariant();
        return token.Contains("heavysword") || token.Contains("stonesword"); } }
