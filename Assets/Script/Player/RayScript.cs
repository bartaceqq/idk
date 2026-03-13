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
    public float cutDelaySeconds = 0.13f;
    public float axeHitDelaySeconds = 0f;
    public bool useDelayedAxeHit = false;
    public float pickaxeHitDelaySeconds = 0f;
    public bool useDelayedPickaxeHit = false;
    public float swingCooldownSeconds = 1f;
    public float swordAttackCooldownSeconds = 2.5f;
    public float swordHitDelaySeconds = 1.10f;
    public float swordHeavyAttackCooldownSeconds = 3.3f;
    public float swordHeavyHitDelaySeconds = 1.45f;
    public float unarmedAttackCooldownSeconds = 0.55f;
    public float unarmedHitDelaySeconds = 0.25f;

    [Header("Proximity Interaction")]
    public Transform interactionOrigin;
    public float axeInteractionRadius = 3f;
    public float pickaxeInteractionRadius = 3f;
    public LayerMask proximityMask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

    [Header("Weapon Sounds")]
    public AudioSource axeaudiosource;
    public AudioSource pickaxeAudioSource;
    public AudioSource swordAudioSource;

    [Header("Sound Delays")]
    public float pickaxeSoundDelaySeconds = 0.1f;
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

        if (IsUiBlockingGameplay())
        {
            return;
        }

        if (blockAttackInput)
        {
            return;
        }

        bool leftClick = Input.GetMouseButtonDown(0);
        bool rightClick = Input.GetMouseButtonDown(1);

        if ((!leftClick && !rightClick) || Time.time < _nextSwingTime)
        {
            return;
        }

        float cooldown = HandleCurrentItemAction(leftClick, rightClick);
        if (cooldown > 0f)
        {
            _nextSwingTime = Time.time + cooldown;
        }
    }

    // Handle Current Item Action.
    private float HandleCurrentItemAction(bool leftClick, bool rightClick)
    {
        int currentItemId = itemSwitchScript != null ? itemSwitchScript.currentitemid : 0;
        switch (currentItemId)
        {
            case 1:
                if (!leftClick)
                {
                    return 0f;
                }
                actionScript?.ResetUnarmedPunchCombo();
                return HandleAxeAction();
            case 2:
                if (!leftClick)
                {
                    return 0f;
                }
                actionScript?.ResetUnarmedPunchCombo();
                return HandlePickaxeAction();
            case 3:
                actionScript?.ResetUnarmedPunchCombo();
                if (!IsSwordEquipped())
                {
                    return 0f;
                }
                return HandleSwordAction(leftClick, rightClick);
            default:
                return currentItemId == 0
                    ? HandleUnarmedAction(leftClick, rightClick)
                    : 0f;
        }
    }

    // Handle Axe Action.
    private float HandleAxeAction()
    {
        if (Time.time < _nextAxeSwingTime)
        {
            return _nextAxeSwingTime - Time.time;
        }

        _nextAxeSwingTime = Time.time + swingCooldownSeconds;

        if (actionScript != null)
        {
            actionScript.Chop();
            TryPlayWeaponSound(axeaudiosource, 0f, ref _nextAxeSoundAllowedTime, swingCooldownSeconds);
        }

        if (TryGetClosestTreeTarget(out ColliderScript treeTarget))
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

        return swingCooldownSeconds;
    }

    // Handle Pickaxe Action.
    private float HandlePickaxeAction()
    {
        if (Time.time < _nextPickaxeSwingTime)
        {
            return _nextPickaxeSwingTime - Time.time;
        }

        _nextPickaxeSwingTime = Time.time + swingCooldownSeconds;

        if (actionScript != null)
        {
            actionScript.Mine();
            TryPlayWeaponSound(pickaxeAudioSource, pickaxeSoundDelaySeconds, ref _nextPickaxeSoundAllowedTime, swingCooldownSeconds);
        }

        if (TryGetClosestStoneTarget(out MineStone stoneTarget))
        {
            float mineDelay = pickaxeHitDelaySeconds > 0f ? pickaxeHitDelaySeconds : cutDelaySeconds;
            if (!useDelayedPickaxeHit || mineDelay <= 0f)
            {
                stoneTarget.Mine();
            }
            else
            {
                StartCoroutine(TriggerAfterDelayPickaxe(stoneTarget, mineDelay));
            }
        }

        return swingCooldownSeconds;
    }

    // Handle Sword Action.
    private float HandleSwordAction(bool leftClick, bool rightClick)
    {
        if (!leftClick && !rightClick)
        {
            return 0f;
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

        bool heavyAttack = rightClick;
        if (actionScript != null)
        {
            if (heavyAttack)
            {
                actionScript.AttackHeavy();
            }
            else
            {
                actionScript.AttackLight();
            }
        }

        if (heavyAttack)
        {
            StartCoroutine(TriggerMeleeAttackAfterDelay(swordHeavyHitDelaySeconds));
            TryPlayWeaponSound(swordAudioSource, swordSoundDelaySeconds, ref _nextSwordSoundAllowedTime, swordHeavyAttackCooldownSeconds);
            return swordHeavyAttackCooldownSeconds;
        }

        StartCoroutine(TriggerMeleeAttackAfterDelay(swordHitDelaySeconds));
        TryPlayWeaponSound(swordAudioSource, swordSoundDelaySeconds, ref _nextSwordSoundAllowedTime, swordAttackCooldownSeconds);
        return swordAttackCooldownSeconds;
    }

    // Handle Unarmed Action.
    private float HandleUnarmedAction(bool leftClick, bool rightClick)
    {
        if (!leftClick)
        {
            return 0f;
        }

        if (actionScript != null)
        {
            actionScript.UnarmedPunchCombo();
        }

        StartCoroutine(TriggerMeleeAttackAfterDelay(unarmedHitDelaySeconds));
        return unarmedAttackCooldownSeconds;
    }

    // Handle Is Sword Equipped.
    private bool IsSwordEquipped()
    {
        if (itemSwitchScript == null)
        {
            return false;
        }

        string equippedName = itemSwitchScript.currentitemname;
        if (!string.IsNullOrWhiteSpace(equippedName))
        {
            return string.Equals(equippedName.Trim(), "Sword", System.StringComparison.OrdinalIgnoreCase);
        }

        return itemSwitchScript.currentitemid == 3;
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
            bestObject = hit.attachedRigidbody != null ? hit.attachedRigidbody.gameObject : hit.gameObject;
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
            return TryGetClosestStoneTargetInternal(QueryTriggerInteraction.Collide, out closestStone);
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

            StoneColliderScript stoneCollider = hit.GetComponent<StoneColliderScript>();
            if (stoneCollider == null)
            {
                stoneCollider = hit.GetComponentInParent<StoneColliderScript>();
            }

            if (stoneCollider == null || stoneCollider.mineStone == null)
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
            closestStone = stoneCollider.mineStone;
        }

        return closestStone != null;
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

    // Handle Trigger After Delay Pickaxe.
    private IEnumerator TriggerAfterDelayPickaxe(MineStone mineStone, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (mineStone != null)
        {
            mineStone.Mine();
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
        return InventoryController.IsInventoryOpen || InventoryManager.IsInventoryOpen || CraftingManager.IsCraftingOpen || DialogueState.IsConversationRunning;
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
