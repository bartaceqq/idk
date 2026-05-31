using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public class RayScript : MonoBehaviour
{
    private static InfoHandler cachedInfoHandler;
    private static InventoryAddHandler cachedInventoryAddHandler;
    public ParticleSystem stoneparticle; public ItemSwitchScript itemSwitchScript;
    public ActionScript actionScript; public TMP_Text pickuptext;
    public string pickupPromptMessage = "Press (E)"; public InfoHandler infoHandler;
    public InventoryAddHandler inventoryAddHandler;
    [Header("Legacy Raycast (unused by proximity mode)")] public Camera camera;
    public float range = 100f; public float sphereRadius = 0.25f;
    public LayerMask hitMask = ~0; [Header("Timing")] public float cutDelaySeconds = 0.65f;
    public float axeHitDelaySeconds = 0.58f; public bool useDelayedAxeHit = true;
    public float pickaxeHitDelaySeconds = 0.65f; public bool useDelayedPickaxeHit = true;
    public float swingCooldownSeconds = 1f; public float axeSwingCooldownSeconds = 0.22f;
    public float pickaxeSwingCooldownSeconds = 0.28f;
    public float swordAttackCooldownSeconds = 2.5f; public float swordHitDelaySeconds = 1.10f;
    public float swordHeavyAttackCooldownSeconds = 3.3f;
    public float swordHeavyHitDelaySeconds = 1.45f;
    public float unarmedAttackCooldownSeconds = 0.55f;
    public float unarmedHitDelaySeconds = 0.25f; [Header("Stone Impact VFX")]
    [SerializeField, Range(0f, 1f)] private float stoneImpactBetweenFactor = 0.35f;
    [SerializeField] private Vector3 stoneImpactOffset = new Vector3(0f, 0.15f, 0f);
    [SerializeField] private bool flattenStoneImpactFacingToHorizontal = true;
    [SerializeField] private float destroyStoneImpactAfterSeconds = 3f;
    [Header("Proximity Interaction")] public Transform interactionOrigin;
    public float axeInteractionRadius = 3f; public float pickaxeInteractionRadius = 3f;
    public LayerMask proximityMask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    public bool allowTreeHandlerWithoutColliderFallback = true;
    public bool allowStoneWithoutColliderFallback = true;
    [Header("Weapon Sounds")] public AudioSource axeaudiosource;
    public AudioSource pickaxeAudioSource; public AudioSource swordAudioSource;
    [Header("Sound Delays")] public float axeSoundDelaySeconds = 0.58f;
    public float pickaxeSoundDelaySeconds = 0.65f; public float swordSoundDelaySeconds = 0.1f;
    [Header("Pickup Detection")] public string pickableLayerName = "Pickable";
    public float pickableDetectionRange = 3f;
    public QueryTriggerInteraction pickableTriggerInteraction = QueryTriggerInteraction.Collide;
    [Range(0.01f, 0.25f)] public float pickableScanInterval = 0.05f;
    public bool runPickableMethodEveryFrameInRange = false;
    public bool allowPickableWithoutColliderFallback = true;
    public GameObject nearestPickableObject;
    public RadiusForAttackScript radiusForAttackScript;
    [HideInInspector] public bool blockAttackInput; private float _nextSwingTime;
    private float _nextAxeSwingTime; private float _nextPickaxeSwingTime;
    private float _nextAxeSoundAllowedTime; private float _nextPickaxeSoundAllowedTime;
    private float _nextSwordSoundAllowedTime;
    private readonly Collider[] _proximityHits = new Collider[128];
    private int _pickableLayer = -1; private float _nextPickableScanTime;
    private void Awake()
    {
        ResolveInteractionOrigin(); CachePickableLayer();
        ResolveInfoHandler(); ResolveInventoryAddHandler(); SetPickupTextVisible(false, null);
    }
    private void OnValidate() { if (!Application.isPlaying) { CachePickableLayer(); } }
    private void Update()
    {
        UpdateNearestPickable(); bool swordEquipped = IsSwordEquipped();
        bool rightMouseHeld = Input.GetMouseButton(1);
        if (GameplayUiState.IsGameplayInputBlocked)
        {
            UpdateSwordBlockState(false, false);
            return;
        }
        if (blockAttackInput) { UpdateSwordBlockState(false, false); return; }
        UpdateSwordBlockState(swordEquipped, rightMouseHeld);
        if (actionScript != null && actionScript.IsSwordBlockActive()) { return; }
        if (actionScript != null && actionScript.IsGameplayInputLocked()) { return; }
        bool leftClick = GameSettings.GetMouseButtonDown(GameSettings.Key.Attack, 0);
        bool rightClick = Input.GetMouseButtonDown(1);
        int swordSpecialIndex = GetSwordSpecialHotkeyIndex();
        if ((!leftClick && !rightClick && swordSpecialIndex < 0) || Time.time < _nextSwingTime) { return; }
        float cooldown = HandleCurrentItemAction(leftClick, rightClick, swordSpecialIndex);
        if (cooldown > 0f) { _nextSwingTime = Time.time + cooldown; }
    }
    private void UpdateSwordBlockState(bool swordEquipped, bool rightMouseHeld)
    {
        if (actionScript == null) { return; }
        if (!swordEquipped || !rightMouseHeld) { actionScript.StopSwordBlock(); return; }
        actionScript.TryBeginSwordBlock();
    }
    private float HandleCurrentItemAction(bool leftClick, bool rightClick, int swordSpecialIndex)
    {
        if (IsAxeEquipped())
        {
            if (!leftClick) { return 0f; }
            actionScript?.ResetUnarmedPunchCombo(); return HandleAxeAction();
        }
        if (IsPickaxeEquipped())
        {
            if (!leftClick) { return 0f; }
            actionScript?.ResetUnarmedPunchCombo(); return HandlePickaxeAction();
        }
        if (IsSwordEquipped())
        {
            actionScript?.ResetUnarmedPunchCombo();
            return HandleSwordAction(leftClick, rightClick, swordSpecialIndex);
        }
        return HasEquippedItem() ? 0f : HandleUnarmedAction(leftClick, rightClick);
    }
    private float HandleAxeAction()
    {
        float swingCooldown = ResolveToolSwingCooldown(axeSwingCooldownSeconds);
        if (actionScript != null)
        {
            if (!actionScript.CanTryChop())
            {
                return Mathf.Max(
        actionScript.GetRemainingChopCooldown(),
        actionScript.GetRemainingUpperBodyActionLockSeconds());
            }
        }
        if (Time.time < _nextAxeSwingTime) { return _nextAxeSwingTime - Time.time; }
        if (actionScript != null && actionScript.staminaScript != null &&
        !actionScript.staminaScript.AxeSwing()) { return 0f; }
        if (actionScript != null)
        {
            if (!actionScript.TryChop())
            {
                return Mathf.Max(actionScript.GetRemainingChopCooldown(),
            actionScript.GetRemainingUpperBodyActionLockSeconds());
            }
            swingCooldown = Mathf.Max(swingCooldown, actionScript.GetChopRepeatDelaySeconds());
            TryPlayWeaponSound(axeaudiosource, axeSoundDelaySeconds, ref _nextAxeSoundAllowedTime, swingCooldown);
        }
        _nextAxeSwingTime = Time.time + swingCooldown;
        if (TryGetClosestTreeHandlerTarget(out TreeHandler treeHandlerTarget))
        {
            if (!useDelayedAxeHit || axeHitDelaySeconds <= 0f)
            {
                treeHandlerTarget.Chop(interactionOrigin);
            }
            else
            {
                StartCoroutine(TriggerAfterDelayTreeHandler(treeHandlerTarget, interactionOrigin, axeHitDelaySeconds));
            }
        }
        else if (TryGetClosestTreeTarget(out ColliderScript treeTarget))
        {
            if (!useDelayedAxeHit || axeHitDelaySeconds <= 0f) { treeTarget.Trigger(); } else { StartCoroutine(TriggerAfterDelayAxe(treeTarget, axeHitDelaySeconds)); }
        }
        return swingCooldown;
    }
    private bool TryGetClosestTreeHandlerTarget(out TreeHandler closestTreeHandler)
    {
        if (TryGetClosestTreeHandlerTargetInternal(triggerInteraction, out closestTreeHandler)) { return true; }
        if (triggerInteraction == QueryTriggerInteraction.Ignore) { if (TryGetClosestTreeHandlerTargetInternal(QueryTriggerInteraction.Collide, out closestTreeHandler)) { return true; } }
        if (allowTreeHandlerWithoutColliderFallback) { return TryGetClosestTreeHandlerWithoutCollider(out closestTreeHandler); }
        return false;
    }
    private bool TryGetClosestTreeHandlerTargetInternal(QueryTriggerInteraction queryMode, out TreeHandler closestTreeHandler)
    {
        return TryGetClosestTargetFromColliders(axeInteractionRadius, queryMode, ResolveTreeHandlerFromCollider, out closestTreeHandler);
    }
    private bool TryGetClosestTreeHandlerWithoutCollider(out TreeHandler closestTreeHandler)
    {
        return TryGetClosestSceneTarget(axeInteractionRadius, out closestTreeHandler);
    }
    private float HandlePickaxeAction()
    {
        float swingCooldown = ResolveToolSwingCooldown(pickaxeSwingCooldownSeconds);
        if (Time.time < _nextPickaxeSwingTime) { return _nextPickaxeSwingTime - Time.time; }
        if (actionScript != null && actionScript.IsUpperBodyActionLocked()) { return actionScript.GetRemainingUpperBodyActionLockSeconds(); }
        if (actionScript != null && actionScript.staminaScript != null &&
        !actionScript.staminaScript.PickaxeSwing()) { return 0f; }
        if (actionScript != null)
        {
            if (!actionScript.TryMine()) { return actionScript.GetRemainingUpperBodyActionLockSeconds(); }
            TryPlayWeaponSound(pickaxeAudioSource, pickaxeSoundDelaySeconds, ref _nextPickaxeSoundAllowedTime, swingCooldown);
        }
        _nextPickaxeSwingTime = Time.time + swingCooldown;
        if (TryGetClosestStoneTarget(out MineStone stoneTarget))
        {
            int requiredHitsToBreak = ResolveEquippedToolRequiredHits();
            float mineDelay = pickaxeHitDelaySeconds > 0f ? pickaxeHitDelaySeconds : cutDelaySeconds;
            if (!useDelayedPickaxeHit || mineDelay <= 0f)
            {
                SpawnStoneImpact(stoneTarget, interactionOrigin);
                stoneTarget.Mine(requiredHitsToBreak);
            }
            else { StartCoroutine(TriggerAfterDelayPickaxe(stoneTarget, interactionOrigin, mineDelay, requiredHitsToBreak)); }
        }
        return swingCooldown;
    }
    private float HandleSwordAction(bool leftClick, bool rightClick, int specialAttackIndex)
    {
        bool specialAttackRequested = specialAttackIndex >= 0;
        bool lightAttackRequested = leftClick && !specialAttackRequested;
        if (!lightAttackRequested && !specialAttackRequested) { return 0f; }
        if (actionScript != null && actionScript.IsUpperBodyActionLocked()) { return actionScript.GetRemainingUpperBodyActionLockSeconds(); }
        bool canSwing = true;
        if (actionScript != null && actionScript.staminaScript != null) { canSwing = actionScript.staminaScript.SwordSwing(); }
        if (!canSwing) { return 0f; }
        if (actionScript != null)
        {
            bool startedAction = specialAttackRequested
            ? actionScript.TryAttackSpecial(specialAttackIndex) : actionScript.TryAttackLight();
            if (!startedAction)
            {
                return Mathf.Max(
            actionScript.GetRemainingGameplayInputLockSeconds(),
            actionScript.GetRemainingUpperBodyActionLockSeconds());
            }
        }
        if (specialAttackRequested)
        {
            float cooldown = ResolveSwordActionCooldown(swordHeavyAttackCooldownSeconds);
            TryPlayWeaponSound(swordAudioSource, swordSoundDelaySeconds, ref _nextSwordSoundAllowedTime, cooldown);
            return cooldown;
        }
        float lightCooldown = ResolveSwordActionCooldown(swordAttackCooldownSeconds);
        TryPlayWeaponSound(swordAudioSource, swordSoundDelaySeconds, ref _nextSwordSoundAllowedTime, lightCooldown);
        return lightCooldown;
    }
    private float HandleUnarmedAction(bool leftClick, bool rightClick)
    {
        if (!leftClick) { return 0f; }
        if (actionScript != null && actionScript.IsUpperBodyActionLocked()) { return actionScript.GetRemainingUpperBodyActionLockSeconds(); }
        if (actionScript != null && !actionScript.TryUnarmedPunchCombo()) { return actionScript.GetRemainingUpperBodyActionLockSeconds(); }
        StartCoroutine(TriggerMeleeAttackAfterDelay(unarmedHitDelaySeconds));
        return unarmedAttackCooldownSeconds;
    }
    private float ResolveToolSwingCooldown(float dedicatedCooldown)
    {
        if (dedicatedCooldown > 0f) { return dedicatedCooldown; }
        return Mathf.Max(0.01f, swingCooldownSeconds);
    }
    private int ResolveEquippedToolRequiredHits()
    {
        return ToolTierUtility.ResolveRequiredHitsToBreak(ResolveEquippedItemName(), 0);
    }
    private float ResolveSwordActionCooldown(float fallbackCooldown)
    {
        float resolvedFallbackCooldown = ResolveSwordFallbackCooldown(fallbackCooldown);
        if (actionScript == null) { return resolvedFallbackCooldown; }
        float lockSeconds = actionScript.GetRemainingGameplayInputLockSeconds();
        if (lockSeconds > 0f) { return Mathf.Max(0.01f, lockSeconds); }
        return resolvedFallbackCooldown;
    }
    private float ResolveSwordFallbackCooldown(float fallbackCooldown)
    {
        float resolvedCooldown = Mathf.Max(0.01f, fallbackCooldown);
        if (itemSwitchScript == null || !itemSwitchScript.TryGetEquippedSword(out Sword equippedSword)) { return resolvedCooldown; }
        return Mathf.Max(0.01f, resolvedCooldown / equippedSword.GetResolvedAnimationSpeed());
    }
    private static int GetSwordSpecialHotkeyIndex()
    {
        if (GameSettings.GetKeyDown(GameSettings.Key.SwordSpecial1, KeyCode.Alpha3)) { return 0; }
        if (GameSettings.GetKeyDown(GameSettings.Key.SwordSpecial2, KeyCode.Alpha4)) { return 1; }
        if (GameSettings.GetKeyDown(GameSettings.Key.SwordSpecial3, KeyCode.Alpha5)) { return 2; }
        return -1;
    }
    private bool IsSwordEquipped() { return itemSwitchScript != null && itemSwitchScript.IsSwordEquipped(); }
    private bool IsAxeEquipped() { return IsEquippedWeaponType("Axe", 1); }
    private bool IsPickaxeEquipped() { return IsEquippedWeaponType("Pickaxe", 2); }
    private bool IsEquippedWeaponType(string expectedWeaponName, int legacyItemId)
    {
        if (itemSwitchScript == null) { return false; }
        string equippedName = ResolveEquippedItemName();
        if (!string.IsNullOrEmpty(equippedName))
        {
            string mappedName = MapCommonWeaponName(equippedName);
            if (!string.IsNullOrEmpty(mappedName)) { return string.Equals(mappedName, expectedWeaponName, System.StringComparison.OrdinalIgnoreCase); }
            return string.Equals(equippedName, expectedWeaponName, System.StringComparison.OrdinalIgnoreCase);
        }
        return itemSwitchScript.currentitemid == legacyItemId;
    }
    private string ResolveEquippedItemName()
    {
        if (itemSwitchScript == null) { return string.Empty; }
        string currentName = NormalizeItemName(itemSwitchScript.currentitemname);
        if (!string.IsNullOrEmpty(currentName)) { return currentName; }
        if (itemSwitchScript.item != null) { return NormalizeItemName(itemSwitchScript.item.name); }
        return string.Empty;
    }
    private bool HasEquippedItem()
    {
        if (itemSwitchScript == null) { return false; }
        return itemSwitchScript.item != null || itemSwitchScript.currentitemid != 0 ||
        !string.IsNullOrEmpty(ResolveEquippedItemName());
    }
    private static string NormalizeItemName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) { return string.Empty; }
        string normalized = rawName.Trim();
        if (normalized.EndsWith("(Clone)", System.StringComparison.OrdinalIgnoreCase)) { normalized = normalized.Substring(0, normalized.Length - "(Clone)".Length).Trim(); }
        return normalized;
    }
    private static string MapCommonWeaponName(string rawName)
    {
        string normalized = NormalizeItemName(rawName);
        if (string.IsNullOrEmpty(normalized)) { return string.Empty; }
        string token = normalized.Replace(" ", string.Empty).ToLowerInvariant();
        if (token.Contains("pickaxe") || token.Contains("pick")) { return "Pickaxe"; }
        if (token.Contains("sword")) { return "Sword"; }
        if (token.Contains("axe")) { return "Axe"; }
        return string.Empty;
    }
    private void CachePickableLayer()
    {
        _pickableLayer = LayerMask.NameToLayer(pickableLayerName);
        if (_pickableLayer < 0) { Debug.LogWarning($"RayScript: Layer '{pickableLayerName}' does not exist.", this); }
    }
    private void UpdateNearestPickable()
    {
        bool shouldRescan = Time.time >= _nextPickableScanTime || nearestPickableObject == null;
        bool changed = false; if (shouldRescan)
        {
            _nextPickableScanTime = Time.time + Mathf.Max(0.01f, pickableScanInterval);
            GameObject nearest = FindNearestPickableInRange();
            changed = nearest != nearestPickableObject; nearestPickableObject = nearest;
            SetPickupTextVisible(nearestPickableObject != null, nearestPickableObject);
        }
        if (nearestPickableObject == null) { return; }
        if (changed || runPickableMethodEveryFrameInRange || GameSettings.GetKeyDown(GameSettings.Key.Interact, KeyCode.E)) { OnPickableInRange(nearestPickableObject); }
    }
    private void OnPickableInRange(GameObject objectik)
    {
        if (objectik == null) { return; }
        if (GameSettings.GetKeyDown(GameSettings.Key.Interact, KeyCode.E))
        {
            switch (objectik.tag)
            {
                case "Stick":
                case "LittleStone":
                case "Bamboo":
                case "MushRoom":
                case "Mushroom":
                case "MushRoomRed":
                case "RedFlower":
                case "BlueFlower":
                    TryPickupInventoryItem(objectik, 1); break;
            }
        }
    }
    private void TryPickupInventoryItem(GameObject pickableObject, int amount)
    {
        if (pickableObject == null || amount <= 0) { return; }
        InventoryItem inventoryItem = pickableObject.GetComponent<InventoryItem>();
        if (inventoryItem == null) { inventoryItem = pickableObject.GetComponentInParent<InventoryItem>(); }
        if (inventoryItem == null) { inventoryItem = pickableObject.GetComponentInChildren<InventoryItem>(true); }
        if (inventoryItem == null)
        {
            Debug.LogWarning($"RayScript: Pickable object '{pickableObject.name}' has no InventoryItem component.", this);
            return;
        }
        ResolveInventoryAddHandler(); if (inventoryAddHandler == null)
        {
            Debug.LogWarning("RayScript: No InventoryAddHandler found for pickups.", this); return;
        }
        if (!inventoryAddHandler.AddItemToInventoryAmount(inventoryItem, amount)) { return; }
        XPRewards.GrantPickupXP(amount);
        ShowPickupInfo(inventoryItem, amount);
        DetailPickupMarker marker = pickableObject.GetComponent<DetailPickupMarker>();
        if (marker == null && inventoryItem != null) { marker = inventoryItem.GetComponent<DetailPickupMarker>(); }
        if (marker != null) { marker.MarkCollected(); }
        if (nearestPickableObject == pickableObject || nearestPickableObject == inventoryItem.gameObject)
        {
            nearestPickableObject = null; SetPickupTextVisible(false, null);
        }
        Destroy(ResolvePickupDestroyTarget(pickableObject, inventoryItem));
    }
    private void ShowPickupInfo(InventoryItem inventoryItem, int amount)
    {
        if (inventoryItem == null) { return; }
        ResolveInfoHandler(); if (infoHandler == null) { return; }
        string displayName = ToDisplayName(inventoryItem.name); string message = amount > 1
        ? $"Picked up ({amount}) {displayName}" : $"Picked up {displayName}";
        infoHandler.QueueInfo(message, inventoryItem.inventorysprite);
    }
    private void SetPickupTextVisible(bool visible, GameObject pickableObject)
    {
        if (pickuptext == null) { return; }
        pickuptext.enabled = visible; if (!visible) { return; }
        string prompt = string.IsNullOrWhiteSpace(pickupPromptMessage) ? "Press (E)" : pickupPromptMessage.Trim();
        pickuptext.text = pickableObject != null ? $"{prompt}" : prompt;
    }
    private GameObject FindNearestPickableInRange()
    {
        ResolveInteractionOrigin();
        if (interactionOrigin == null) { return null; }
        if (_pickableLayer < 0) { return null; }
        float radius = Mathf.Max(0.01f, pickableDetectionRange);
        Vector3 origin = interactionOrigin.position;
        Transform playerRoot = interactionOrigin.root; float bestDistanceSqr = float.MaxValue;
        GameObject bestObject = null; int layerMask = 1 << _pickableLayer;
        int hitCount = Physics.OverlapSphereNonAlloc(origin, radius, _proximityHits, layerMask,
        pickableTriggerInteraction); for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _proximityHits[i]; if (hit == null) { continue; }
            if (playerRoot != null && hit.transform.IsChildOf(playerRoot)) { continue; }
            Vector3 closestPoint = hit.ClosestPoint(origin);
            float distanceSqr = (closestPoint - origin).sqrMagnitude;
            if (distanceSqr >= bestDistanceSqr) { continue; }
            bestDistanceSqr = distanceSqr;
            GameObject hitObject = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.gameObject;
            bestObject = ResolvePickableRoot(hitObject);
        }
        if (bestObject != null || !allowPickableWithoutColliderFallback) { return bestObject; }
        return FindNearestPickableWithoutCollider(origin, radius * radius, playerRoot);
    }
    private static GameObject ResolvePickupDestroyTarget(GameObject pickableObject, InventoryItem inventoryItem)
    {
        if (inventoryItem == null) { return pickableObject; }
        if (pickableObject == null) { return inventoryItem.gameObject; }
        if (pickableObject == inventoryItem.gameObject) { return pickableObject; }
        if (pickableObject.transform.IsChildOf(inventoryItem.transform)) { return inventoryItem.gameObject; }
        if (inventoryItem.transform.IsChildOf(pickableObject.transform)) { return pickableObject; }
        return inventoryItem.gameObject;
    }
    private static GameObject ResolvePickableRoot(GameObject candidate)
    {
        if (candidate == null) { return null; }
        InventoryItem inventoryItem = candidate.GetComponent<InventoryItem>();
        if (inventoryItem == null) { inventoryItem = candidate.GetComponentInParent<InventoryItem>(); }
        if (inventoryItem == null) { inventoryItem = candidate.GetComponentInChildren<InventoryItem>(true); }
        return inventoryItem != null ? inventoryItem.gameObject : candidate;
    }
    private GameObject FindNearestPickableWithoutCollider(Vector3 origin, float radiusSqr, Transform playerRoot)
    {
        InventoryItem[] allItems = UnitySceneSearch.FindAllCached<InventoryItem>(0.5f);
        GameObject bestObject = null; float bestDistanceSqr = float.MaxValue;
        for (int i = 0; i < allItems.Length; i++)
        {
            InventoryItem item = allItems[i];
            if (item == null || item.gameObject == null) { continue; }
            GameObject candidate = item.gameObject;
            if (candidate.layer != _pickableLayer) { continue; }
            if (!candidate.activeInHierarchy) { continue; }
            if (playerRoot != null && candidate.transform.IsChildOf(playerRoot)) { continue; }
            float distanceSqr = (candidate.transform.position - origin).sqrMagnitude;
            if (distanceSqr > radiusSqr || distanceSqr >= bestDistanceSqr) { continue; }
            bestDistanceSqr = distanceSqr; bestObject = candidate;
        }
        return bestObject;
    }
    private void ResolveInteractionOrigin()
    {
        if (interactionOrigin == null)
        {
            Transform root = transform.root; interactionOrigin = root != null ? root : transform;
        }
    }
    private void ResolveInfoHandler()
    {
        if (infoHandler == null)
        {
            if (cachedInfoHandler == null) { cachedInfoHandler = FindInfoHandlerInScene(); }
            infoHandler = cachedInfoHandler;
        }
        else { cachedInfoHandler = infoHandler; }
    }
    private void ResolveInventoryAddHandler()
    {
        if (inventoryAddHandler == null)
        {
            if (cachedInventoryAddHandler == null) { cachedInventoryAddHandler = FindInventoryAddHandlerInScene(); }
            inventoryAddHandler = cachedInventoryAddHandler;
        }
        else { cachedInventoryAddHandler = inventoryAddHandler; }
    }
    private static InventoryAddHandler FindInventoryAddHandlerInScene()
    {
        InventoryAddHandler[] handlers = UnitySceneSearch.FindAll<InventoryAddHandler>();
        if (handlers == null || handlers.Length == 0) { return null; }
        InventoryAddHandler fallback = null; for (int i = 0; i < handlers.Length; i++)
        {
            InventoryAddHandler handler = handlers[i]; if (handler == null) { continue; }
            if (fallback == null) { fallback = handler; }
            if (handler.inventoryManager != null) { return handler; }
        }
        return fallback;
    }
    private static InfoHandler FindInfoHandlerInScene()
    {
        return UnitySceneSearch.FindFirst<InfoHandler>();
    }
    private bool TryGetClosestTreeTarget(out ColliderScript closestTree)
    {
        if (TryGetClosestTreeTargetInternal(triggerInteraction, out closestTree)) { return true; }
        if (triggerInteraction == QueryTriggerInteraction.Ignore) { return TryGetClosestTreeTargetInternal(QueryTriggerInteraction.Collide, out closestTree); }
        return false;
    }
    private bool TryGetClosestTreeTargetInternal(QueryTriggerInteraction queryMode, out ColliderScript closestTree)
    {
        return TryGetClosestTargetFromColliders(axeInteractionRadius, queryMode, ResolveTreeColliderFromCollider, out closestTree);
    }
    private bool TryGetClosestStoneTarget(out MineStone closestStone)
    {
        if (TryGetClosestStoneTargetInternal(triggerInteraction, out closestStone)) { return true; }
        if (triggerInteraction == QueryTriggerInteraction.Ignore) { if (TryGetClosestStoneTargetInternal(QueryTriggerInteraction.Collide, out closestStone)) { return true; } }
        if (allowStoneWithoutColliderFallback) { return TryGetClosestStoneTargetWithoutCollider(out closestStone); }
        return false;
    }
    private bool TryGetClosestStoneTargetInternal(QueryTriggerInteraction queryMode, out MineStone closestStone)
    {
        return TryGetClosestTargetFromColliders(pickaxeInteractionRadius, queryMode, ResolveMineStoneFromCollider, out closestStone);
    }
    private bool TryGetClosestStoneTargetWithoutCollider(out MineStone closestStone)
    {
        return TryGetClosestSceneTarget(pickaxeInteractionRadius, out closestStone);
    }
    private delegate T TargetFromCollider<T>(Collider hit) where T : Component;
    private bool TryGetClosestTargetFromColliders<T>(float searchRadius,
    QueryTriggerInteraction queryMode, TargetFromCollider<T> resolveTarget,
    out T closestTarget) where T : Component
    {
        closestTarget = null;
        ResolveInteractionOrigin(); if (interactionOrigin == null) { return false; }
        float radius = Mathf.Max(0.01f, searchRadius);
        Vector3 origin = interactionOrigin.position;
        Transform playerRoot = interactionOrigin.root; float bestDistanceSqr = float.MaxValue;
        int hitCount = Physics.OverlapSphereNonAlloc(origin, radius, _proximityHits, proximityMask, queryMode);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _proximityHits[i];
            if (hit == null || (playerRoot != null && hit.transform.IsChildOf(playerRoot))) { continue; }
            T target = resolveTarget(hit); if (target == null) { continue; }
            float distanceSqr = (hit.ClosestPoint(origin) - origin).sqrMagnitude;
            if (distanceSqr >= bestDistanceSqr) { continue; }
            bestDistanceSqr = distanceSqr;
            closestTarget = target;
        }
        return closestTarget != null;
    }
    private bool TryGetClosestSceneTarget<T>(float searchRadius, out T closestTarget) where T : Component
    {
        closestTarget = null; ResolveInteractionOrigin();
        if (interactionOrigin == null) { return false; }
        float radiusSqr = Mathf.Max(0.01f, searchRadius); radiusSqr *= radiusSqr;
        Vector3 origin = interactionOrigin.position;
        Transform playerRoot = interactionOrigin.root; float bestDistanceSqr = float.MaxValue;
        T[] candidates = UnitySceneSearch.FindAllCached<T>(0.5f, false);
        for (int i = 0; i < candidates.Length; i++)
        {
            T candidate = candidates[i];
            if (candidate == null || (playerRoot != null && candidate.transform.IsChildOf(playerRoot))) { continue; }
            float distanceSqr = (candidate.transform.position - origin).sqrMagnitude;
            if (distanceSqr > radiusSqr || distanceSqr >= bestDistanceSqr) { continue; }
            bestDistanceSqr = distanceSqr; closestTarget = candidate;
        }
        return closestTarget != null;
    }
    private static TreeHandler ResolveTreeHandlerFromCollider(Collider hit)
    {
        TreeHandler treeHandler = hit.GetComponent<TreeHandler>();
        return treeHandler != null ? treeHandler : hit.GetComponentInParent<TreeHandler>();
    }
    private static ColliderScript ResolveTreeColliderFromCollider(Collider hit)
    {
        ColliderScript treeCollider = hit.GetComponent<ColliderScript>();
        return treeCollider != null ? treeCollider : hit.GetComponentInParent<ColliderScript>();
    }
    private static MineStone ResolveMineStoneFromCollider(Collider hit)
    {
        if (hit == null) { return null; }
        StoneColliderScript stoneCollider = hit.GetComponent<StoneColliderScript>();
        if (stoneCollider == null) { stoneCollider = hit.GetComponentInParent<StoneColliderScript>(); }
        if (stoneCollider != null && stoneCollider.mineStone != null) { return stoneCollider.mineStone; }
        MineStone mineStone = hit.GetComponent<MineStone>();
        if (mineStone == null) { mineStone = hit.GetComponentInParent<MineStone>(); }
        return mineStone;
    }
    private IEnumerator TriggerAfterDelayAxe(ColliderScript colliderScript, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (colliderScript != null) { colliderScript.Trigger(); }
    }
    private IEnumerator TriggerAfterDelayTreeHandler(TreeHandler treeHandler, Transform attacker, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (treeHandler != null) { treeHandler.Chop(attacker); }
    }
    private IEnumerator TriggerAfterDelayPickaxe(MineStone mineStone, Transform attacker, float delaySeconds, int requiredHitsToBreak)
    {
        yield return new WaitForSeconds(delaySeconds); if (mineStone != null)
        {
            SpawnStoneImpact(mineStone, attacker); mineStone.Mine(requiredHitsToBreak);
        }
    }
    private void SpawnStoneImpact(MineStone mineStone, Transform attacker)
    {
        if (stoneparticle == null || mineStone == null) { return; }
        Transform stoneTransform = mineStone.fullstone != null ? mineStone.fullstone.transform
        : mineStone.transform;
        Vector3 fallbackStonePoint = ResolveStoneFallbackPoint(stoneTransform);
        Vector3 attackerPosition = ResolveAttackerPosition(attacker, fallbackStonePoint);
        Vector3 stoneImpactPoint = ResolveStoneImpactPoint(stoneTransform, attackerPosition);
        Vector3 spawnPosition = Vector3.Lerp(stoneImpactPoint, attackerPosition,
        Mathf.Clamp01(stoneImpactBetweenFactor)) + stoneImpactOffset;
        Vector3 lookDirection = attackerPosition - spawnPosition;
        if (flattenStoneImpactFacingToHorizontal) { lookDirection.y = 0f; }
        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            lookDirection = attackerPosition - stoneImpactPoint;
            if (flattenStoneImpactFacingToHorizontal) { lookDirection.y = 0f; }
        }
        if (lookDirection.sqrMagnitude <= 0.0001f) { lookDirection = transform.forward; }
        Quaternion rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        ParticleSystem impactInstance = Instantiate(stoneparticle, spawnPosition, rotation);
        PlayImpactParticleSystems(impactInstance.gameObject);
        float destroyDelay = Mathf.Max(0f, destroyStoneImpactAfterSeconds);
        if (impactInstance != null && destroyDelay > 0f) { Destroy(impactInstance.gameObject, destroyDelay); }
    }
    private Vector3 ResolveStoneImpactPoint(Transform stoneTransform, Vector3 attackerPosition)
    {
        if (TryResolveClosestImpactPoint(stoneTransform, attackerPosition, out Vector3 closestImpactPoint)) { return closestImpactPoint; }
        if (TryGetBounds(stoneTransform, out Bounds stoneBounds))
        {
            float impactY = Mathf.Lerp(stoneBounds.min.y, stoneBounds.max.y, 0.5f);
            return new Vector3(stoneBounds.center.x, impactY, stoneBounds.center.z);
        }
        return ResolveStoneFallbackPoint(stoneTransform);
    }
    private static Vector3 ResolveStoneFallbackPoint(Transform stoneTransform)
    {
        if (stoneTransform != null) { return stoneTransform.position + Vector3.up * 0.35f; }
        return Vector3.zero;
    }
    private static Vector3 ResolveAttackerPosition(Transform attacker, Vector3 fallbackTargetPoint)
    {
        if (attacker != null) { return attacker.position; }
        if (Camera.main != null) { return Camera.main.transform.position; }
        return fallbackTargetPoint + Vector3.forward;
    }
    private static bool TryGetBounds(Transform target, out Bounds bounds)
    {
        bounds = default;
        if (target == null) { return false; }
        bool hasBounds = false;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer currentRenderer = renderers[i];
            if (currentRenderer == null) { continue; }
            if (!hasBounds)
            {
                bounds = currentRenderer.bounds;
                hasBounds = true;
            }
            else { bounds.Encapsulate(currentRenderer.bounds); }
        }
        if (hasBounds) { return true; }
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider currentCollider = colliders[i];
            if (currentCollider == null) { continue; }
            if (!hasBounds)
            {
                bounds = currentCollider.bounds;
                hasBounds = true;
            }
            else { bounds.Encapsulate(currentCollider.bounds); }
        }
        return hasBounds;
    }
    private static bool TryResolveClosestImpactPoint(Transform target, Vector3 attackerPosition, out Vector3 closestPoint)
    {
        closestPoint = default; if (target == null) { return false; }
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        float bestDistanceSqr = float.MaxValue; bool foundPoint = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider currentCollider = colliders[i];
            if (currentCollider == null || !currentCollider.enabled) { continue; }
            Vector3 currentClosestPoint = currentCollider.ClosestPoint(attackerPosition);
            float distanceSqr = (currentClosestPoint - attackerPosition).sqrMagnitude;
            if (distanceSqr >= bestDistanceSqr) { continue; }
            bestDistanceSqr = distanceSqr;
            closestPoint = currentClosestPoint; foundPoint = true;
        }
        return foundPoint;
    }
    private static void PlayImpactParticleSystems(GameObject impactInstance)
    {
        if (impactInstance == null) { return; }
        ParticleSystem[] particleSystems = impactInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem currentParticleSystem = particleSystems[i];
            if (currentParticleSystem == null) { continue; }
            currentParticleSystem.Clear(true);
            currentParticleSystem.Play(true);
        }
    }
    private IEnumerator TriggerMeleeAttackAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (radiusForAttackScript != null) { radiusForAttackScript.Attack(); }
    }
    private void TryPlayWeaponSound(AudioSource source, float delaySeconds, ref float nextAllowedTime, float minBlockSeconds)
    {
        if (source == null) { return; }
        float now = Time.time; if (now < nextAllowedTime) { return; }
        float delay = Mathf.Max(0f, delaySeconds); float clipDuration = 0f;
        if (source.clip != null)
        {
            float pitch = Mathf.Abs(source.pitch);
            if (pitch < 0.01f) { pitch = 0.01f; }
            clipDuration = source.clip.length / pitch;
        }
        float blockDuration = Mathf.Max(minBlockSeconds, delay + clipDuration);
        if (blockDuration <= 0f) { blockDuration = 0.01f; }
        nextAllowedTime = now + blockDuration;
        if (delay > 0f) { source.PlayDelayed(delay); } else { source.Play(); }
    }
    private static string ToDisplayName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) { return "Item"; }
        string normalized = rawName.Trim().Replace('_', ' ');
        string[] parts = normalized.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(parts[i])) { continue; }
            string lower = parts[i].ToLowerInvariant();
            parts[i] = char.ToUpperInvariant(lower[0]) + lower.Substring(1);
        }
        return string.Join(" ", parts);
    }
}
