using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Controls attack, chop, and mine interactions.
public class RayScript : MonoBehaviour
{
    public ParticleSystem stoneparticle;
    public ItemSwitchScript itemSwitchScript;
    public ActionScript actionScript;
    public TMP_Text pickuptext;
    public string pickupPromptMessage = "Press (E)";

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
    public bool runPickableMethodEveryFrameInRange = false;
    public bool allowPickableWithoutColliderFallback = true;
    public GameObject nearestPickableObject;

    public RadiusForAttackScript radiusForAttackScript;

    private float _nextSwingTime;
    private float _nextAxeSwingTime;
    private float _nextPickaxeSwingTime;
    private float _nextAxeSoundAllowedTime;
    private float _nextPickaxeSoundAllowedTime;
    private float _nextSwordSoundAllowedTime;
    private readonly Collider[] _proximityHits = new Collider[128];
    private int _pickableLayer = -1;

    private void Awake()
    {
        ResolveInteractionOrigin();
        CachePickableLayer();
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

        if (!Input.GetMouseButtonDown(0) || Time.time < _nextSwingTime)
        {
            return;
        }

        float cooldown = HandleCurrentItemAction();
        if (cooldown > 0f)
        {
            _nextSwingTime = Time.time + cooldown;
        }
    }

    // Handle Current Item Action.
    private float HandleCurrentItemAction()
    {
        int currentItemId = itemSwitchScript != null ? itemSwitchScript.currentitemid : 0;
        switch (currentItemId)
        {
            case 1:
                return HandleAxeAction();
            case 2:
                return HandlePickaxeAction();
            case 3:
                return HandleSwordAction();
            default:
                return 0f;
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
    private float HandleSwordAction()
    {
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
            actionScript.Attack();
        }

        StartCoroutine(TriggerSwordAttackAfterDelay(swordHitDelaySeconds));
        TryPlayWeaponSound(swordAudioSource, swordSoundDelaySeconds, ref _nextSwordSoundAllowedTime, swordAttackCooldownSeconds);
        return swordAttackCooldownSeconds;
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
        GameObject nearest = FindNearestPickableInRange();
        bool changed = nearest != nearestPickableObject;
        nearestPickableObject = nearest;
        SetPickupTextVisible(nearestPickableObject != null, nearestPickableObject);

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

        inventoryItem.ResolveReferences();
        if (inventoryItem.slotManager == null)
        {
            Debug.LogWarning($"RayScript: InventoryItem '{inventoryItem.name}' has no SlotManager assigned.", this);
            return;
        }

        if (!inventoryItem.slotManager.AddItem(inventoryItem, amount))
        {
            // Inventory full or add failed.
            return;
        }

        if (nearestPickableObject == pickableObject || nearestPickableObject == inventoryItem.gameObject)
        {
            nearestPickableObject = null;
            SetPickupTextVisible(false, null);
        }

        Destroy(ResolvePickupDestroyTarget(pickableObject, inventoryItem));
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

    // Handle Trigger Sword Attack After Delay.
    private IEnumerator TriggerSwordAttackAfterDelay(float delaySeconds)
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
        return InventoryController.IsInventoryOpen || CraftingManager.IsCraftingOpen;
    }
}
