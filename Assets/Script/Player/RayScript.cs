using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Controls attack, chop, and mine interactions.
public class RayScript : MonoBehaviour
{
    private static InfoHandler cachedInfoHandler;
    private static InventoryAddHandler cachedInventoryAddHandler;

    public ParticleSystem stoneparticle;
    public ItemSwitchScript itemSwitchScript;
    public ActionScript actionScript;
    public TMP_Text pickuptext;
    public string pickupPromptMessage = "Press (E)";
    public InfoHandler infoHandler;
    public InventoryAddHandler inventoryAddHandler;

    [Header("Legacy Raycast (unused by proximity mode)")]
    public Camera camera;
    public float range = 100f;
    public float sphereRadius = 0.25f;
    public LayerMask hitMask = ~0;

    [Header("Timing")]
    public float cutDelaySeconds = 0.65f;
    public float axeHitDelaySeconds = 0.58f;
    public bool useDelayedAxeHit = true;
    public float pickaxeHitDelaySeconds = 0.65f;
    public bool useDelayedPickaxeHit = true;
    public float swingCooldownSeconds = 1f;
    public float axeSwingCooldownSeconds = 0.22f;
    public float pickaxeSwingCooldownSeconds = 0.28f;
    public float swordAttackCooldownSeconds = 2.5f;
    public float swordHitDelaySeconds = 1.10f;
    public float swordHeavyAttackCooldownSeconds = 3.3f;
    public float swordHeavyHitDelaySeconds = 1.45f;
    public float unarmedAttackCooldownSeconds = 0.55f;
    public float unarmedHitDelaySeconds = 0.25f;
    [Header("Stone Impact VFX")]
    [SerializeField, Range(0f, 1f)] private float stoneImpactBetweenFactor = 0.35f;
    [SerializeField] private Vector3 stoneImpactOffset = new Vector3(0f, 0.15f, 0f);
    [SerializeField] private bool flattenStoneImpactFacingToHorizontal = true;
    [SerializeField] private float destroyStoneImpactAfterSeconds = 3f;

    [Header("Proximity Interaction")]
    public Transform interactionOrigin;
    public float axeInteractionRadius = 3f;
    public float pickaxeInteractionRadius = 3f;
    public LayerMask proximityMask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
    public bool allowTreeHandlerWithoutColliderFallback = true;
    public bool allowStoneWithoutColliderFallback = true;

    [Header("Weapon Sounds")]
    public AudioSource axeaudiosource;
    public AudioSource pickaxeAudioSource;
    public AudioSource swordAudioSource;

    [Header("Sound Delays")]
    public float axeSoundDelaySeconds = 0.58f;
    public float pickaxeSoundDelaySeconds = 0.65f;
    public float swordSoundDelaySeconds = 0.1f;

    [Header("Pickup Detection")]
    public string pickableLayerName = "Pickable";
    public float pickableDetectionRange = 3f;
    public QueryTriggerInteraction pickableTriggerInteraction = QueryTriggerInteraction.Collide;
    [Range(0.01f, 0.25f)] public float pickableScanInterval = 0.05f;
    public bool runPickableMethodEveryFrameInRange = false;
    public bool allowPickableWithoutColliderFallback = true;
    public GameObject nearestPickableObject;

    public RadiusForAttackScript radiusForAttackScript;
    [HideInInspector] public bool blockAttackInput;

    private float _nextSwingTime;
    private float _nextAxeSwingTime;
    private float _nextPickaxeSwingTime;
    private float _nextAxeSoundAllowedTime;
    private float _nextPickaxeSoundAllowedTime;
    private float _nextSwordSoundAllowedTime;
    private readonly Collider[] _proximityHits = new Collider[128];
    private int _pickableLayer = -1;
    private float _nextPickableScanTime;

    private void Awake()
    {
        ResolveInteractionOrigin();
        CachePickableLayer();
        ResolveInfoHandler();
        ResolveInventoryAddHandler();
        SetPickupTextVisible(false, null);
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            CachePickableLayer();
        }
    }

    private void Update()
    {
        UpdateNearestPickable();
        bool swordEquipped = IsSwordEquipped();
        bool rightMouseHeld = Input.GetMouseButton(1);

        if (IsUiBlockingGameplay())
        {
            UpdateSwordBlockState(false, false);
            return;
        }

        if (blockAttackInput)
        {
            UpdateSwordBlockState(false, false);
            return;
        }

        UpdateSwordBlockState(swordEquipped, rightMouseHeld);

        if (actionScript != null && actionScript.IsSwordBlockActive())
        {
            return;
        }

        if (actionScript != null && actionScript.IsGameplayInputLocked())
        {
            return;
        }

        bool leftClick = Input.GetMouseButtonDown(0);
        bool rightClick = Input.GetMouseButtonDown(1);
        int swordSpecialIndex = GetSwordSpecialHotkeyIndex();

        if ((!leftClick && !rightClick && swordSpecialIndex < 0) || Time.time < _nextSwingTime)
        {
            return;
        }

        float cooldown = HandleCurrentItemAction(leftClick, rightClick, swordSpecialIndex);
        if (cooldown > 0f)
        {
            _nextSwingTime = Time.time + cooldown;
        }
    }

    // Handle Update Sword Block State.
    private void UpdateSwordBlockState(bool swordEquipped, bool rightMouseHeld)
    {
        if (actionScript == null)
        {
            return;
        }

        if (!swordEquipped || !rightMouseHeld)
        {
            actionScript.StopSwordBlock();
            return;
        }

        actionScript.TryBeginSwordBlock();
    }

    // Handle Current Item Action.
    private float HandleCurrentItemAction(bool leftClick, bool rightClick, int swordSpecialIndex)
    {
        if (IsAxeEquipped())
        {
            if (!leftClick)
            {
                return 0f;
            }

            actionScript?.ResetUnarmedPunchCombo();
            return HandleAxeAction();
        }

        if (IsPickaxeEquipped())
        {
            if (!leftClick)
            {
                return 0f;
            }

            actionScript?.ResetUnarmedPunchCombo();
            return HandlePickaxeAction();
        }

        if (IsSwordEquipped())
        {
            actionScript?.ResetUnarmedPunchCombo();
            return HandleSwordAction(leftClick, rightClick, swordSpecialIndex);
        }

        return HasEquippedItem()
            ? 0f
            : HandleUnarmedAction(leftClick, rightClick);
    }

    // Handle Axe Action.
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

        if (Time.time < _nextAxeSwingTime)
        {
            return _nextAxeSwingTime - Time.time;
        }

        if (actionScript != null &&
            actionScript.staminaScript != null &&
            !actionScript.staminaScript.AxeSwing())
        {
            return 0f;
        }

        if (actionScript != null)
        {
            if (!actionScript.TryChop())
            {
                return Mathf.Max(
                    actionScript.GetRemainingChopCooldown(),
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
            if (!useDelayedAxeHit || axeHitDelaySeconds <= 0f)
            {
                treeTarget.Trigger();
            }
            else
            {
                StartCoroutine(TriggerAfterDelayAxe(treeTarget, axeHitDelaySeconds));
            }
        }

        return swingCooldown;
    }

    // Handle Try Get Closest Tree Handler Target.
    private bool TryGetClosestTreeHandlerTarget(out TreeHandler closestTreeHandler)
    {
        if (TryGetClosestTreeHandlerTargetInternal(triggerInteraction, out closestTreeHandler))
        {
            return true;
        }

        // Fallback: some tree colliders may be marked as trigger colliders.
        if (triggerInteraction == QueryTriggerInteraction.Ignore)
        {
            if (TryGetClosestTreeHandlerTargetInternal(QueryTriggerInteraction.Collide, out closestTreeHandler))
            {
                return true;
            }
        }

        if (allowTreeHandlerWithoutColliderFallback)
        {
            return TryGetClosestTreeHandlerWithoutCollider(out closestTreeHandler);
        }

        return false;
    }

    // Handle Try Get Closest Tree Handler Target Internal.
    private bool TryGetClosestTreeHandlerTargetInternal(QueryTriggerInteraction queryMode, out TreeHandler closestTreeHandler)
    {
        closestTreeHandler = null;
        ResolveInteractionOrigin();
        if (interactionOrigin == null)
        {
            return false;
        }

        float radius = Mathf.Max(0.01f, axeInteractionRadius);
        Vector3 origin = interactionOrigin.position;
        Transform playerRoot = interactionOrigin.root;
        float bestDistanceSqr = float.MaxValue;

        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            radius,
            _proximityHits,
            proximityMask,
            queryMode);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _proximityHits[i];
            if (hit == null)
            {
                continue;
            }

            if (playerRoot != null && hit.transform.IsChildOf(playerRoot))
            {
                continue;
            }

            TreeHandler treeHandlerTarget = hit.GetComponent<TreeHandler>();
            if (treeHandlerTarget == null)
            {
                treeHandlerTarget = hit.GetComponentInParent<TreeHandler>();
            }

            if (treeHandlerTarget == null)
            {
                continue;
            }

            Vector3 closestPoint = hit.ClosestPoint(origin);
            float distanceSqr = (closestPoint - origin).sqrMagnitude;
            if (distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            closestTreeHandler = treeHandlerTarget;
        }

        return closestTreeHandler != null;
    }

    // Handle Try Get Closest Tree Handler Without Collider.
    private bool TryGetClosestTreeHandlerWithoutCollider(out TreeHandler closestTreeHandler)
    {
        closestTreeHandler = null;
        ResolveInteractionOrigin();
        if (interactionOrigin == null)
        {
            return false;
        }

        float radiusSqr = Mathf.Max(0.01f, axeInteractionRadius);
        radiusSqr *= radiusSqr;
        Vector3 origin = interactionOrigin.position;
        Transform playerRoot = interactionOrigin.root;
        float bestDistanceSqr = float.MaxValue;

#if UNITY_2023_1_OR_NEWER
        TreeHandler[] allTreeHandlers = FindObjectsByType<TreeHandler>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        TreeHandler[] allTreeHandlers = FindObjectsOfType<TreeHandler>(false);
#endif

        for (int i = 0; i < allTreeHandlers.Length; i++)
        {
            TreeHandler candidate = allTreeHandlers[i];
            if (candidate == null)
            {
                continue;
            }

            if (playerRoot != null && candidate.transform.IsChildOf(playerRoot))
            {
                continue;
            }

            float distanceSqr = (candidate.transform.position - origin).sqrMagnitude;
            if (distanceSqr > radiusSqr || distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            closestTreeHandler = candidate;
        }

        return closestTreeHandler != null;
    }

    // Handle Pickaxe Action.
    private float HandlePickaxeAction()
    {
        float swingCooldown = ResolveToolSwingCooldown(pickaxeSwingCooldownSeconds);
        if (Time.time < _nextPickaxeSwingTime)
        {
            return _nextPickaxeSwingTime - Time.time;
        }

        if (actionScript != null && actionScript.IsUpperBodyActionLocked())
        {
            return actionScript.GetRemainingUpperBodyActionLockSeconds();
        }

        if (actionScript != null &&
            actionScript.staminaScript != null &&
            !actionScript.staminaScript.PickaxeSwing())
        {
            return 0f;
        }

        if (actionScript != null)
        {
            if (!actionScript.TryMine())
            {
                return actionScript.GetRemainingUpperBodyActionLockSeconds();
            }

            TryPlayWeaponSound(pickaxeAudioSource, pickaxeSoundDelaySeconds, ref _nextPickaxeSoundAllowedTime, swingCooldown);
        }

        _nextPickaxeSwingTime = Time.time + swingCooldown;

        if (TryGetClosestStoneTarget(out MineStone stoneTarget))
        {
            float mineDelay = pickaxeHitDelaySeconds > 0f ? pickaxeHitDelaySeconds : cutDelaySeconds;
            if (!useDelayedPickaxeHit || mineDelay <= 0f)
            {
                SpawnStoneImpact(stoneTarget, interactionOrigin);
                stoneTarget.Mine();
            }
            else
            {
                StartCoroutine(TriggerAfterDelayPickaxe(stoneTarget, interactionOrigin, mineDelay));
            }
        }

        return swingCooldown;
    }

    // Handle Sword Action.
    private float HandleSwordAction(bool leftClick, bool rightClick, int specialAttackIndex)
    {
        bool specialAttackRequested = specialAttackIndex >= 0;
        bool lightAttackRequested = leftClick && !specialAttackRequested;
        if (!lightAttackRequested && !specialAttackRequested)
        {
            return 0f;
        }

        if (actionScript != null && actionScript.IsUpperBodyActionLocked())
        {
            return actionScript.GetRemainingUpperBodyActionLockSeconds();
        }

        bool canSwing = true;
        if (actionScript != null && actionScript.staminaScript != null)
        {
            canSwing = actionScript.staminaScript.SwordSwing();
        }

        if (!canSwing)
        {
            return 0f;
        }

        if (actionScript != null)
        {
            bool startedAction = specialAttackRequested
                ? actionScript.TryAttackSpecial(specialAttackIndex)
                : actionScript.TryAttackLight();
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

    // Handle Unarmed Action.
    private float HandleUnarmedAction(bool leftClick, bool rightClick)
    {
        if (!leftClick)
        {
            return 0f;
        }

        if (actionScript != null && actionScript.IsUpperBodyActionLocked())
        {
            return actionScript.GetRemainingUpperBodyActionLockSeconds();
        }

        if (actionScript != null && !actionScript.TryUnarmedPunchCombo())
        {
            return actionScript.GetRemainingUpperBodyActionLockSeconds();
        }

        StartCoroutine(TriggerMeleeAttackAfterDelay(unarmedHitDelaySeconds));
        return unarmedAttackCooldownSeconds;
    }

    // Handle Resolve Tool Swing Cooldown.
    private float ResolveToolSwingCooldown(float dedicatedCooldown)
    {
        if (dedicatedCooldown > 0f)
        {
            return dedicatedCooldown;
        }

        return Mathf.Max(0.01f, swingCooldownSeconds);
    }

    // Handle Resolve Sword Action Cooldown.
    private float ResolveSwordActionCooldown(float fallbackCooldown)
    {
        float resolvedFallbackCooldown = ResolveSwordFallbackCooldown(fallbackCooldown);
        if (actionScript == null)
        {
            return resolvedFallbackCooldown;
        }

        float lockSeconds = actionScript.GetRemainingGameplayInputLockSeconds();
        if (lockSeconds > 0f)
        {
            return Mathf.Max(0.01f, lockSeconds);
        }

        return resolvedFallbackCooldown;
    }

    // Handle Resolve Sword Fallback Cooldown.
    private float ResolveSwordFallbackCooldown(float fallbackCooldown)
    {
        float resolvedCooldown = Mathf.Max(0.01f, fallbackCooldown);
        if (itemSwitchScript == null || !itemSwitchScript.TryGetEquippedSword(out Sword equippedSword))
        {
            return resolvedCooldown;
        }

        return Mathf.Max(0.01f, resolvedCooldown / equippedSword.GetResolvedAnimationSpeed());
    }

    // Handle Get Sword Special Hotkey Index.
    private static int GetSwordSpecialHotkeyIndex()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            return 0;
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            return 1;
        }

        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            return 2;
        }

        return -1;
    }

    // Handle Is Sword Equipped.
    private bool IsSwordEquipped()
    {
        return itemSwitchScript != null && itemSwitchScript.IsSwordEquipped();
    }

    // Handle Is Axe Equipped.
    private bool IsAxeEquipped()
    {
        return IsEquippedWeaponType("Axe", 1);
    }

    // Handle Is Pickaxe Equipped.
    private bool IsPickaxeEquipped()
    {
        return IsEquippedWeaponType("Pickaxe", 2);
    }

    // Handle Is Equipped Weapon Type.
    private bool IsEquippedWeaponType(string expectedWeaponName, int legacyItemId)
    {
        if (itemSwitchScript == null)
        {
            return false;
        }

        string equippedName = ResolveEquippedItemName();
        if (!string.IsNullOrEmpty(equippedName))
        {
            string mappedName = MapCommonWeaponName(equippedName);
            if (!string.IsNullOrEmpty(mappedName))
            {
                return string.Equals(mappedName, expectedWeaponName, System.StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(equippedName, expectedWeaponName, System.StringComparison.OrdinalIgnoreCase);
        }

        return itemSwitchScript.currentitemid == legacyItemId;
    }

    // Handle Resolve Equipped Item Name.
    private string ResolveEquippedItemName()
    {
        if (itemSwitchScript == null)
        {
            return string.Empty;
        }

        string currentName = NormalizeItemName(itemSwitchScript.currentitemname);
        if (!string.IsNullOrEmpty(currentName))
        {
            return currentName;
        }

        if (itemSwitchScript.item != null)
        {
            return NormalizeItemName(itemSwitchScript.item.name);
        }

        return string.Empty;
    }

    // Handle Has Equipped Item.
    private bool HasEquippedItem()
    {
        if (itemSwitchScript == null)
        {
            return false;
        }

        return itemSwitchScript.item != null ||
               itemSwitchScript.currentitemid != 0 ||
               !string.IsNullOrEmpty(ResolveEquippedItemName());
    }

    // Handle Normalize Item Name.
    private static string NormalizeItemName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        string normalized = rawName.Trim();
        if (normalized.EndsWith("(Clone)", System.StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - "(Clone)".Length).Trim();
        }

        return normalized;
    }

    // Handle Map Common Weapon Name.
    private static string MapCommonWeaponName(string rawName)
    {
        string normalized = NormalizeItemName(rawName);
        if (string.IsNullOrEmpty(normalized))
        {
            return string.Empty;
        }

        string token = normalized.Replace(" ", string.Empty).ToLowerInvariant();
        if (token.Contains("pickaxe") || token.Contains("pick"))
        {
            return "Pickaxe";
        }

        if (token.Contains("sword"))
        {
            return "Sword";
        }

        if (token.Contains("axe"))
        {
            return "Axe";
        }

        return string.Empty;
    }

    // Handle Cache Pickable Layer.
    private void CachePickableLayer()
    {
        _pickableLayer = LayerMask.NameToLayer(pickableLayerName);
        if (_pickableLayer < 0)
        {
            Debug.LogWarning($"RayScript: Layer '{pickableLayerName}' does not exist.", this);
        }
    }

    // Handle Update Nearest Pickable.
    private void UpdateNearestPickable()
    {
        bool shouldRescan = Time.time >= _nextPickableScanTime || nearestPickableObject == null;
        bool changed = false;

        if (shouldRescan)
        {
            _nextPickableScanTime = Time.time + Mathf.Max(0.01f, pickableScanInterval);
            GameObject nearest = FindNearestPickableInRange();
            changed = nearest != nearestPickableObject;
            nearestPickableObject = nearest;
            SetPickupTextVisible(nearestPickableObject != null, nearestPickableObject);
        }

        if (nearestPickableObject == null)
        {
            return;
        }

        if (changed || runPickableMethodEveryFrameInRange || Input.GetKeyDown(KeyCode.E))
        {
            OnPickableInRange(nearestPickableObject);
        }
    }

    // Handle On Pickable In Range.
    // Runs when the nearest pickable in 3f range is found or changes.
    private void OnPickableInRange(GameObject objectik)
    {
        if (objectik == null)
        {
            return;
        }
        if (Input.GetKeyDown(KeyCode.E))
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
                    TryPickupInventoryItem(objectik, 1);
                    break;

            }
        }
    }

    // Handle Try Pickup Inventory Item.
    private void TryPickupInventoryItem(GameObject pickableObject, int amount)
    {
        if (pickableObject == null || amount <= 0)
        {
            return;
        }

        InventoryItem inventoryItem = pickableObject.GetComponent<InventoryItem>();
        if (inventoryItem == null)
        {
            inventoryItem = pickableObject.GetComponentInParent<InventoryItem>();
        }
        if (inventoryItem == null)
        {
            inventoryItem = pickableObject.GetComponentInChildren<InventoryItem>(true);
        }

        if (inventoryItem == null)
        {
            Debug.LogWarning($"RayScript: Pickable object '{pickableObject.name}' has no InventoryItem component.", this);
            return;
        }

        ResolveInventoryAddHandler();
        if (inventoryAddHandler == null)
        {
            Debug.LogWarning("RayScript: No InventoryAddHandler found for pickups.", this);
            return;
        }

        if (!inventoryAddHandler.AddItemToInventoryAmount(inventoryItem, amount))
        {
            // Inventory full or add failed.
            return;
        }

        ShowPickupInfo(inventoryItem, amount);

        DetailPickupMarker marker = pickableObject.GetComponent<DetailPickupMarker>();
        if (marker == null && inventoryItem != null)
        {
            marker = inventoryItem.GetComponent<DetailPickupMarker>();
        }

        if (marker != null)
        {
            marker.MarkCollected();
        }

        if (nearestPickableObject == pickableObject || nearestPickableObject == inventoryItem.gameObject)
        {
            nearestPickableObject = null;
            SetPickupTextVisible(false, null);
        }

        Destroy(ResolvePickupDestroyTarget(pickableObject, inventoryItem));
    }

    // Handle Show Pickup Info.
    private void ShowPickupInfo(InventoryItem inventoryItem, int amount)
    {
        if (inventoryItem == null)
        {
            return;
        }

        ResolveInfoHandler();
        if (infoHandler == null)
        {
            return;
        }

        string displayName = ToDisplayName(inventoryItem.name);
        string message = amount > 1
            ? $"Picked up ({amount}) {displayName}"
            : $"Picked up {displayName}";

        infoHandler.QueueInfo(message, inventoryItem.inventorysprite);
    }

    // Handle Set Pickup Text Visible.
    private void SetPickupTextVisible(bool visible, GameObject pickableObject)
    {
        if (pickuptext == null)
        {
            return;
        }

        pickuptext.enabled = visible;
        if (!visible)
        {
            return;
        }

        string prompt = string.IsNullOrWhiteSpace(pickupPromptMessage) ? "Press (E)" : pickupPromptMessage.Trim();
        pickuptext.text = pickableObject != null ? $"{prompt}" : prompt;
    }

    // Handle Find Nearest Pickable In Range.
    private GameObject FindNearestPickableInRange()
    {
        ResolveInteractionOrigin();
        if (interactionOrigin == null)
        {
            return null;
        }

        if (_pickableLayer < 0)
        {
            return null;
        }

        float radius = Mathf.Max(0.01f, pickableDetectionRange);
        Vector3 origin = interactionOrigin.position;
        Transform playerRoot = interactionOrigin.root;
        float bestDistanceSqr = float.MaxValue;
        GameObject bestObject = null;

        int layerMask = 1 << _pickableLayer;
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            radius,
            _proximityHits,
            layerMask,
            pickableTriggerInteraction);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _proximityHits[i];
            if (hit == null)
            {
                continue;
            }

            if (playerRoot != null && hit.transform.IsChildOf(playerRoot))
            {
                continue;
            }

            Vector3 closestPoint = hit.ClosestPoint(origin);
            float distanceSqr = (closestPoint - origin).sqrMagnitude;
            if (distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            GameObject hitObject = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.gameObject;
            bestObject = ResolvePickableRoot(hitObject);
        }

        if (bestObject != null || !allowPickableWithoutColliderFallback)
        {
            return bestObject;
        }

        return FindNearestPickableWithoutCollider(origin, radius * radius, playerRoot);
    }

    // Handle Resolve Pickup Destroy Target.
    private static GameObject ResolvePickupDestroyTarget(GameObject pickableObject, InventoryItem inventoryItem)
    {
        if (inventoryItem == null)
        {
            return pickableObject;
        }

        if (pickableObject == null)
        {
            return inventoryItem.gameObject;
        }

        if (pickableObject == inventoryItem.gameObject)
        {
            return pickableObject;
        }

        if (pickableObject.transform.IsChildOf(inventoryItem.transform))
        {
            return inventoryItem.gameObject;
        }

        if (inventoryItem.transform.IsChildOf(pickableObject.transform))
        {
            return pickableObject;
        }

        return inventoryItem.gameObject;
    }

    // Handle Resolve Pickable Root.
    private static GameObject ResolvePickableRoot(GameObject candidate)
    {
        if (candidate == null)
        {
            return null;
        }

        InventoryItem inventoryItem = candidate.GetComponent<InventoryItem>();
        if (inventoryItem == null)
        {
            inventoryItem = candidate.GetComponentInParent<InventoryItem>();
        }

        if (inventoryItem == null)
        {
            inventoryItem = candidate.GetComponentInChildren<InventoryItem>(true);
        }

        return inventoryItem != null ? inventoryItem.gameObject : candidate;
    }

    // Handle Find Nearest Pickable Without Collider.
    private GameObject FindNearestPickableWithoutCollider(Vector3 origin, float radiusSqr, Transform playerRoot)
    {
#if UNITY_2023_1_OR_NEWER
        InventoryItem[] allItems = FindObjectsByType<InventoryItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        InventoryItem[] allItems = FindObjectsOfType<InventoryItem>(true);
#endif

        GameObject bestObject = null;
        float bestDistanceSqr = float.MaxValue;

        for (int i = 0; i < allItems.Length; i++)
        {
            InventoryItem item = allItems[i];
            if (item == null || item.gameObject == null)
            {
                continue;
            }

            GameObject candidate = item.gameObject;
            if (candidate.layer != _pickableLayer)
            {
                continue;
            }

            if (!candidate.activeInHierarchy)
            {
                continue;
            }

            if (playerRoot != null && candidate.transform.IsChildOf(playerRoot))
            {
                continue;
            }

            float distanceSqr = (candidate.transform.position - origin).sqrMagnitude;
            if (distanceSqr > radiusSqr || distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            bestObject = candidate;
        }

        return bestObject;
    }

    // Handle Resolve Interaction Origin.
    private void ResolveInteractionOrigin()
    {
        if (interactionOrigin == null)
        {
            Transform root = transform.root;
            interactionOrigin = root != null ? root : transform;
        }
    }

    // Handle Resolve Info Handler.
    private void ResolveInfoHandler()
    {
        if (infoHandler == null)
        {
            if (cachedInfoHandler == null)
            {
                cachedInfoHandler = FindInfoHandlerInScene();
            }

            infoHandler = cachedInfoHandler;
        }
        else
        {
            cachedInfoHandler = infoHandler;
        }
    }

    // Handle Resolve Inventory Add Handler.
    private void ResolveInventoryAddHandler()
    {
        if (inventoryAddHandler == null)
        {
            if (cachedInventoryAddHandler == null)
            {
                cachedInventoryAddHandler = FindInventoryAddHandlerInScene();
            }

            inventoryAddHandler = cachedInventoryAddHandler;
        }
        else
        {
            cachedInventoryAddHandler = inventoryAddHandler;
        }
    }

    // Handle Find Inventory Add Handler In Scene.
    private static InventoryAddHandler FindInventoryAddHandlerInScene()
    {
#if UNITY_2023_1_OR_NEWER
        InventoryAddHandler[] handlers = FindObjectsByType<InventoryAddHandler>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
#else
        InventoryAddHandler[] handlers = FindObjectsOfType<InventoryAddHandler>(true);
#endif
        if (handlers == null || handlers.Length == 0)
        {
            return null;
        }

        InventoryAddHandler fallback = null;
        for (int i = 0; i < handlers.Length; i++)
        {
            InventoryAddHandler handler = handlers[i];
            if (handler == null)
            {
                continue;
            }

            if (fallback == null)
            {
                fallback = handler;
            }

            if (handler.inventoryManager != null)
            {
                return handler;
            }
        }

        return fallback;
    }

    // Handle Find Info Handler In Scene.
    private static InfoHandler FindInfoHandlerInScene()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<InfoHandler>(FindObjectsInactive.Include);
#else
        return FindObjectOfType<InfoHandler>(true);
#endif
    }

    // Handle Try Get Closest Tree Target.
    private bool TryGetClosestTreeTarget(out ColliderScript closestTree)
    {
        if (TryGetClosestTreeTargetInternal(triggerInteraction, out closestTree))
        {
            return true;
        }

        // Fallback: some tree colliders may be marked as trigger colliders.
        if (triggerInteraction == QueryTriggerInteraction.Ignore)
        {
            return TryGetClosestTreeTargetInternal(QueryTriggerInteraction.Collide, out closestTree);
        }

        return false;
    }

    // Handle Try Get Closest Tree Target Internal.
    private bool TryGetClosestTreeTargetInternal(QueryTriggerInteraction queryMode, out ColliderScript closestTree)
    {
        closestTree = null;
        ResolveInteractionOrigin();
        if (interactionOrigin == null)
        {
            return false;
        }

        float radius = Mathf.Max(0.01f, axeInteractionRadius);
        Vector3 origin = interactionOrigin.position;
        Transform playerRoot = interactionOrigin.root;
        float bestDistanceSqr = float.MaxValue;

        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            radius,
            _proximityHits,
            proximityMask,
            queryMode);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _proximityHits[i];
            if (hit == null)
            {
                continue;
            }

            if (playerRoot != null && hit.transform.IsChildOf(playerRoot))
            {
                continue;
            }

            ColliderScript treeTarget = hit.GetComponent<ColliderScript>();
            if (treeTarget == null)
            {
                treeTarget = hit.GetComponentInParent<ColliderScript>();
            }

            if (treeTarget == null)
            {
                continue;
            }

            Vector3 closestPoint = hit.ClosestPoint(origin);
            float distanceSqr = (closestPoint - origin).sqrMagnitude;
            if (distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            closestTree = treeTarget;
        }

        return closestTree != null;
    }

    // Handle Try Get Closest Stone Target.
    private bool TryGetClosestStoneTarget(out MineStone closestStone)
    {
        if (TryGetClosestStoneTargetInternal(triggerInteraction, out closestStone))
        {
            return true;
        }

        // Fallback: some stone colliders may be marked as trigger colliders.
        if (triggerInteraction == QueryTriggerInteraction.Ignore)
        {
            if (TryGetClosestStoneTargetInternal(QueryTriggerInteraction.Collide, out closestStone))
            {
                return true;
            }
        }

        if (allowStoneWithoutColliderFallback)
        {
            return TryGetClosestStoneTargetWithoutCollider(out closestStone);
        }

        return false;
    }

    // Handle Try Get Closest Stone Target Internal.
    private bool TryGetClosestStoneTargetInternal(QueryTriggerInteraction queryMode, out MineStone closestStone)
    {
        closestStone = null;
        ResolveInteractionOrigin();
        if (interactionOrigin == null)
        {
            return false;
        }

        float radius = Mathf.Max(0.01f, pickaxeInteractionRadius);
        Vector3 origin = interactionOrigin.position;
        Transform playerRoot = interactionOrigin.root;
        float bestDistanceSqr = float.MaxValue;

        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            radius,
            _proximityHits,
            proximityMask,
            queryMode);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _proximityHits[i];
            if (hit == null)
            {
                continue;
            }

            if (playerRoot != null && hit.transform.IsChildOf(playerRoot))
            {
                continue;
            }

            MineStone mineStone = ResolveMineStoneFromCollider(hit);
            if (mineStone == null)
            {
                continue;
            }

            Vector3 closestPoint = hit.ClosestPoint(origin);
            float distanceSqr = (closestPoint - origin).sqrMagnitude;
            if (distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            closestStone = mineStone;
        }

        return closestStone != null;
    }

    // Handle Try Get Closest Stone Target Without Collider.
    private bool TryGetClosestStoneTargetWithoutCollider(out MineStone closestStone)
    {
        closestStone = null;
        ResolveInteractionOrigin();
        if (interactionOrigin == null)
        {
            return false;
        }

        float radiusSqr = Mathf.Max(0.01f, pickaxeInteractionRadius);
        radiusSqr *= radiusSqr;
        Vector3 origin = interactionOrigin.position;
        Transform playerRoot = interactionOrigin.root;
        float bestDistanceSqr = float.MaxValue;

#if UNITY_2023_1_OR_NEWER
        MineStone[] allMineStones = FindObjectsByType<MineStone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        MineStone[] allMineStones = FindObjectsOfType<MineStone>(false);
#endif

        for (int i = 0; i < allMineStones.Length; i++)
        {
            MineStone candidate = allMineStones[i];
            if (candidate == null)
            {
                continue;
            }

            if (playerRoot != null && candidate.transform.IsChildOf(playerRoot))
            {
                continue;
            }

            float distanceSqr = (candidate.transform.position - origin).sqrMagnitude;
            if (distanceSqr > radiusSqr || distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            closestStone = candidate;
        }

        return closestStone != null;
    }

    // Handle Resolve Mine Stone From Collider.
    private static MineStone ResolveMineStoneFromCollider(Collider hit)
    {
        if (hit == null)
        {
            return null;
        }

        StoneColliderScript stoneCollider = hit.GetComponent<StoneColliderScript>();
        if (stoneCollider == null)
        {
            stoneCollider = hit.GetComponentInParent<StoneColliderScript>();
        }

        if (stoneCollider != null && stoneCollider.mineStone != null)
        {
            return stoneCollider.mineStone;
        }

        MineStone mineStone = hit.GetComponent<MineStone>();
        if (mineStone == null)
        {
            mineStone = hit.GetComponentInParent<MineStone>();
        }

        return mineStone;
    }

    // Handle Trigger After Delay Axe.
    private IEnumerator TriggerAfterDelayAxe(ColliderScript colliderScript, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (colliderScript != null)
        {
            colliderScript.Trigger();
        }
    }

    // Handle Trigger After Delay Tree Handler.
    private IEnumerator TriggerAfterDelayTreeHandler(TreeHandler treeHandler, Transform attacker, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (treeHandler != null)
        {
            treeHandler.Chop(attacker);
        }
    }

    // Handle Trigger After Delay Pickaxe.
    private IEnumerator TriggerAfterDelayPickaxe(MineStone mineStone, Transform attacker, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (mineStone != null)
        {
            SpawnStoneImpact(mineStone, attacker);
            mineStone.Mine();
        }
    }

    // Handle Spawn Stone Impact.
    private void SpawnStoneImpact(MineStone mineStone, Transform attacker)
    {
        if (stoneparticle == null || mineStone == null)
        {
            return;
        }

        Transform stoneTransform = mineStone.fullstone != null
            ? mineStone.fullstone.transform
            : mineStone.transform;
        Vector3 fallbackStonePoint = ResolveStoneFallbackPoint(stoneTransform);
        Vector3 attackerPosition = ResolveAttackerPosition(attacker, fallbackStonePoint);
        Vector3 stoneImpactPoint = ResolveStoneImpactPoint(stoneTransform, attackerPosition);
        Vector3 spawnPosition = Vector3.Lerp(
            stoneImpactPoint,
            attackerPosition,
            Mathf.Clamp01(stoneImpactBetweenFactor)) + stoneImpactOffset;

        Vector3 lookDirection = attackerPosition - spawnPosition;
        if (flattenStoneImpactFacingToHorizontal)
        {
            lookDirection.y = 0f;
        }

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            lookDirection = attackerPosition - stoneImpactPoint;
            if (flattenStoneImpactFacingToHorizontal)
            {
                lookDirection.y = 0f;
            }
        }

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            lookDirection = transform.forward;
        }

        Quaternion rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        ParticleSystem impactInstance = Instantiate(stoneparticle, spawnPosition, rotation);
        PlayImpactParticleSystems(impactInstance.gameObject);

        float destroyDelay = Mathf.Max(0f, destroyStoneImpactAfterSeconds);
        if (impactInstance != null && destroyDelay > 0f)
        {
            Destroy(impactInstance.gameObject, destroyDelay);
        }
    }

    // Handle Resolve Stone Impact Point.
    private Vector3 ResolveStoneImpactPoint(Transform stoneTransform, Vector3 attackerPosition)
    {
        if (TryResolveClosestImpactPoint(stoneTransform, attackerPosition, out Vector3 closestImpactPoint))
        {
            return closestImpactPoint;
        }

        if (TryGetBounds(stoneTransform, out Bounds stoneBounds))
        {
            float impactY = Mathf.Lerp(stoneBounds.min.y, stoneBounds.max.y, 0.5f);
            return new Vector3(stoneBounds.center.x, impactY, stoneBounds.center.z);
        }

        return ResolveStoneFallbackPoint(stoneTransform);
    }

    // Handle Resolve Stone Fallback Point.
    private static Vector3 ResolveStoneFallbackPoint(Transform stoneTransform)
    {
        if (stoneTransform != null)
        {
            return stoneTransform.position + Vector3.up * 0.35f;
        }

        return Vector3.zero;
    }

    // Handle Resolve Attacker Position.
    private static Vector3 ResolveAttackerPosition(Transform attacker, Vector3 fallbackTargetPoint)
    {
        if (attacker != null)
        {
            return attacker.position;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform.position;
        }

        return fallbackTargetPoint + Vector3.forward;
    }

    // Handle Try Get Bounds.
    private static bool TryGetBounds(Transform target, out Bounds bounds)
    {
        bounds = default;
        if (target == null)
        {
            return false;
        }

        bool hasBounds = false;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer currentRenderer = renderers[i];
            if (currentRenderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = currentRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(currentRenderer.bounds);
            }
        }

        if (hasBounds)
        {
            return true;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider currentCollider = colliders[i];
            if (currentCollider == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = currentCollider.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(currentCollider.bounds);
            }
        }

        return hasBounds;
    }

    // Handle Try Resolve Closest Impact Point.
    private static bool TryResolveClosestImpactPoint(Transform target, Vector3 attackerPosition, out Vector3 closestPoint)
    {
        closestPoint = default;
        if (target == null)
        {
            return false;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        float bestDistanceSqr = float.MaxValue;
        bool foundPoint = false;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider currentCollider = colliders[i];
            if (currentCollider == null || !currentCollider.enabled)
            {
                continue;
            }

            Vector3 currentClosestPoint = currentCollider.ClosestPoint(attackerPosition);
            float distanceSqr = (currentClosestPoint - attackerPosition).sqrMagnitude;
            if (distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            closestPoint = currentClosestPoint;
            foundPoint = true;
        }

        return foundPoint;
    }

    // Handle Play Impact Particle Systems.
    private static void PlayImpactParticleSystems(GameObject impactInstance)
    {
        if (impactInstance == null)
        {
            return;
        }

        ParticleSystem[] particleSystems = impactInstance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem currentParticleSystem = particleSystems[i];
            if (currentParticleSystem == null)
            {
                continue;
            }

            currentParticleSystem.Clear(true);
            currentParticleSystem.Play(true);
        }
    }

    // Handle Trigger Melee Attack After Delay.
    private IEnumerator TriggerMeleeAttackAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (radiusForAttackScript != null)
        {
            radiusForAttackScript.Attack();
        }
    }

    // Handle Play Sound At Swing Start.
    private void TryPlayWeaponSound(AudioSource source, float delaySeconds, ref float nextAllowedTime, float minBlockSeconds)
    {
        if (source == null)
        {
            return;
        }

        float now = Time.time;
        if (now < nextAllowedTime)
        {
            return;
        }

        float delay = Mathf.Max(0f, delaySeconds);
        float clipDuration = 0f;
        if (source.clip != null)
        {
            float pitch = Mathf.Abs(source.pitch);
            if (pitch < 0.01f)
            {
                pitch = 0.01f;
            }

            clipDuration = source.clip.length / pitch;
        }

        float blockDuration = Mathf.Max(minBlockSeconds, delay + clipDuration);
        if (blockDuration <= 0f)
        {
            blockDuration = 0.01f;
        }

        nextAllowedTime = now + blockDuration;

        if (delay > 0f)
        {
            source.PlayDelayed(delay);
        }
        else
        {
            source.Play();
        }
    }

    // Handle Is UIBlocking Gameplay.
    private static bool IsUiBlockingGameplay()
    {
        return GameplayUiState.IsGameplayInputBlocked;
    }

    // Handle To Display Name.
    private static string ToDisplayName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return "Item";
        }

        string normalized = rawName.Trim().Replace('_', ' ');
        string[] parts = normalized.Split(' ');
        for (int i = 0; i < parts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(parts[i]))
            {
                continue;
            }

            string lower = parts[i].ToLowerInvariant();
            parts[i] = char.ToUpperInvariant(lower[0]) + lower.Substring(1);
        }

        return string.Join(" ", parts);
    }
}
