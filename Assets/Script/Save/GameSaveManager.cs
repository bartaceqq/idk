using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameSaveManager
{
    private const int SaveVersion = 2;
    private const string SaveFileName = "savegame.json";
    private const string SavedScenePlayerPrefsKey = "onemorenight.save.scene";

    public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public static bool HasSave()
    {
        return File.Exists(SavePath) || PlayerPrefs.HasKey(SavedScenePlayerPrefsKey);
    }

    public static bool TryGetSavedSceneName(out string sceneName)
    {
        sceneName = string.Empty;
        if (TryReadSave(out GameSaveData data, out _) && !string.IsNullOrWhiteSpace(data.sceneName))
        {
            sceneName = data.sceneName;
            return true;
        }

        sceneName = PlayerPrefs.GetString(SavedScenePlayerPrefsKey, string.Empty);
        return !string.IsNullOrWhiteSpace(sceneName);
    }

    public static bool SaveCurrentGame(out string message)
    {
        message = string.Empty;
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || string.IsNullOrWhiteSpace(activeScene.name))
        {
            message = "Save failed: no active gameplay scene.";
            return false;
        }

        GameSaveData data = new GameSaveData
        {
            version = SaveVersion,
            sceneName = activeScene.name,
            savedAtUtc = DateTime.UtcNow.ToString("O"),
            player = CapturePlayer(),
            inventory = CaptureInventory()
        };

        try
        {
            string directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            PlayerPrefs.SetString(SavedScenePlayerPrefsKey, data.sceneName);
            PlayerPrefs.Save();
            message = $"Game saved. Inventory slots: {data.inventory.Count}.";
            return true;
        }
        catch (Exception exception)
        {
            message = $"Save failed: {exception.Message}";
            return false;
        }
    }

    public static bool LoadSavedGameIntoActiveScene(out string message)
    {
        if (!TryReadSave(out GameSaveData data, out message))
        {
            return false;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        if (!string.Equals(activeScene.name, data.sceneName, StringComparison.Ordinal))
        {
            message = $"Save is for scene '{data.sceneName}', but active scene is '{activeScene.name}'.";
            return false;
        }

        RestorePlayer(data.player);
        int restoredInventorySlots = RestoreInventory(data.inventory);
        message = $"Loaded save. Inventory slots: {restoredInventorySlots}.";
        return true;
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
        }

        PlayerPrefs.DeleteKey(SavedScenePlayerPrefsKey);
        PlayerPrefs.Save();
    }

    private static bool TryReadSave(out GameSaveData data, out string message)
    {
        data = null;
        message = string.Empty;
        if (!File.Exists(SavePath))
        {
            message = "No save file found.";
            return false;
        }

        try
        {
            data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(SavePath));
            if (data == null || string.IsNullOrWhiteSpace(data.sceneName))
            {
                message = "Save file is empty or invalid.";
                return false;
            }

            if (data.inventory == null)
            {
                data.inventory = new List<InventorySlotSaveData>();
            }

            return true;
        }
        catch (Exception exception)
        {
            message = $"Could not read save: {exception.Message}";
            return false;
        }
    }

    private static PlayerSaveData CapturePlayer()
    {
        Transform playerTransform = TryGetActivePlayerTransform();
        if (playerTransform == null)
        {
            return new PlayerSaveData { valid = false };
        }

        return new PlayerSaveData
        {
            valid = true,
            position = playerTransform.position,
            rotation = playerTransform.rotation
        };
    }

    private static Transform TryGetActivePlayerTransform()
    {
        LookingController lookingController = FindLookingController();
        if (lookingController != null)
        {
            if (lookingController.normalcapsule != null)
            {
                return lookingController.normalcapsule.transform;
            }

            return lookingController.transform;
        }

        GameObject taggedPlayer;
        try
        {
            taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            taggedPlayer = null;
        }

        return taggedPlayer != null ? taggedPlayer.transform : null;
    }

    private static void RestorePlayer(PlayerSaveData player)
    {
        if (!player.valid)
        {
            return;
        }

        LookingController lookingController = FindLookingController();
        if (lookingController != null)
        {
            SetGameObjectTransform(lookingController.normalcapsule, player.position, player.rotation);
            SetGameObjectTransform(lookingController.buildingcapsule, player.position, player.rotation);
            lookingController.SwitchToNormalMode();
            return;
        }

        GameObject taggedPlayer;
        try
        {
            taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            taggedPlayer = null;
        }

        SetGameObjectTransform(taggedPlayer, player.position, player.rotation);
    }

    private static List<InventorySlotSaveData> CaptureInventory()
    {
        List<InventorySlotSaveData> inventory = new List<InventorySlotSaveData>();
        InventoryManager manager = FindInventoryManager();
        if (manager == null)
        {
            return inventory;
        }

        List<SlotInsideUI> slots = GetInventorySlots(manager);
        for (int i = 0; i < slots.Count; i++)
        {
            SlotInsideUI slot = slots[i];
            if (slot == null || !slot.occupied || slot.Item == null || slot.count <= 0)
            {
                continue;
            }

            string itemName = !string.IsNullOrWhiteSpace(slot.Item.nameofitem) ? slot.Item.nameofitem : slot.Item.name;
            inventory.Add(new InventorySlotSaveData
            {
                slotId = slot.id,
                itemName = itemName,
                count = slot.count
            });
        }

        return inventory;
    }

    private static int RestoreInventory(List<InventorySlotSaveData> inventory)
    {
        InventoryManager manager = FindInventoryManager();
        if (manager == null)
        {
            return 0;
        }

        List<SlotInsideUI> slots = GetInventorySlots(manager);
        for (int i = 0; i < slots.Count; i++)
        {
            ApplyInventorySlot(slots[i], null, string.Empty, 0, false, manager.UIShown);
        }

        if (inventory == null)
        {
            return 0;
        }

        int restored = 0;
        for (int i = 0; i < inventory.Count; i++)
        {
            InventorySlotSaveData savedSlot = inventory[i];
            if (string.IsNullOrWhiteSpace(savedSlot.itemName) || savedSlot.count <= 0)
            {
                continue;
            }

            SlotInsideUI targetSlot = FindInventorySlotById(slots, savedSlot.slotId);
            InventoryItem item = FindInventoryItem(savedSlot.itemName);
            if (targetSlot == null || item == null)
            {
                continue;
            }

            ApplyInventorySlot(targetSlot, item, savedSlot.itemName, savedSlot.count, true, manager.UIShown);
            restored++;
        }

        return restored;
    }

    private static void SetGameObjectTransform(GameObject target, Vector3 position, Quaternion rotation)
    {
        if (target == null)
        {
            return;
        }

        CharacterController characterController = target.GetComponent<CharacterController>();
        bool controllerWasEnabled = characterController != null && characterController.enabled;
        if (controllerWasEnabled)
        {
            characterController.enabled = false;
        }

        target.transform.SetPositionAndRotation(position, rotation);

        if (controllerWasEnabled)
        {
            characterController.enabled = true;
        }
    }

    private static LookingController FindLookingController()
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindAnyObjectByType<LookingController>(FindObjectsInactive.Include);
#else
        return UnityEngine.Object.FindObjectOfType<LookingController>(true);
#endif
    }

    private static InventoryManager FindInventoryManager()
    {
#if UNITY_2023_1_OR_NEWER
        return UnityEngine.Object.FindAnyObjectByType<InventoryManager>(FindObjectsInactive.Include);
#else
        return UnityEngine.Object.FindObjectOfType<InventoryManager>(true);
#endif
    }

    private static List<SlotInsideUI> GetInventorySlots(InventoryManager manager)
    {
        if (manager.slotlist == null)
        {
            manager.slotlist = new List<SlotInsideUI>();
        }

        if (manager.slotlist.Count == 0)
        {
            SlotInsideUI[] discovered = manager.GetComponentsInChildren<SlotInsideUI>(true);
            for (int i = 0; i < discovered.Length; i++)
            {
                if (discovered[i] != null && !manager.slotlist.Contains(discovered[i]))
                {
                    manager.slotlist.Add(discovered[i]);
                }
            }
        }

        return manager.slotlist;
    }

    private static SlotInsideUI FindInventorySlotById(List<SlotInsideUI> slots, int slotId)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].id == slotId)
            {
                return slots[i];
            }
        }

        return null;
    }

    private static InventoryItem FindInventoryItem(string itemName)
    {
        string normalizedName = itemName.Trim();
#if UNITY_2023_1_OR_NEWER
        InventoryItem[] items = UnityEngine.Object.FindObjectsByType<InventoryItem>(FindObjectsInactive.Include);
#else
        InventoryItem[] items = UnityEngine.Object.FindObjectsOfType<InventoryItem>(true);
#endif
        for (int i = 0; i < items.Length; i++)
        {
            InventoryItem item = items[i];
            if (item == null)
            {
                continue;
            }

            if (string.Equals(item.nameofitem, normalizedName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.name, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }

    private static void ApplyInventorySlot(
        SlotInsideUI slot,
        InventoryItem item,
        string itemName,
        int count,
        bool occupied,
        bool uiShown)
    {
        if (slot == null)
        {
            return;
        }

        slot.Item = item;
        slot.nameofslot = itemName;
        slot.count = count;
        slot.occupied = occupied;
        if (slot.image != null)
        {
            slot.image.sprite = item != null ? item.inventorysprite : null;
            slot.image.enabled = uiShown && occupied && item != null;
        }

        if (slot.text != null)
        {
            slot.text.text = count > 0 ? count.ToString() : "0";
            slot.text.enabled = uiShown;
        }
    }

    [Serializable]
    private sealed class GameSaveData
    {
        public int version;
        public string sceneName;
        public string savedAtUtc;
        public PlayerSaveData player;
        public List<InventorySlotSaveData> inventory = new List<InventorySlotSaveData>();
    }

    [Serializable]
    private struct PlayerSaveData
    {
        public bool valid;
        public Vector3 position;
        public Quaternion rotation;
    }

    [Serializable]
    private struct InventorySlotSaveData
    {
        public int slotId;
        public string itemName;
        public int count;
    }
}
