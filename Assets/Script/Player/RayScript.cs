using System.Collections;
using UnityEngine;

// Controls attack, chop, and mine interactions.
public class RayScript : MonoBehaviour
{
    public ParticleSystem stoneparticle;
    public ItemSwitchScript itemSwitchScript;
    public ActionScript actionScript;

    [Header("Legacy Raycast (unused by proximity mode)")]
    public Camera camera;
    public float range = 100f;
    public float sphereRadius = 0.25f;
    public LayerMask hitMask = ~0;

    [Header("Timing")]
    public float cutDelaySeconds = 0.13f;
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

    public RadiusForAttackScript radiusForAttackScript;

    private float _nextSwingTime;
    private float _nextAxeSwingTime;
    private float _nextPickaxeSwingTime;
    private float _nextAxeSoundAllowedTime;
    private float _nextPickaxeSoundAllowedTime;
    private float _nextSwordSoundAllowedTime;
    private readonly Collider[] _proximityHits = new Collider[128];

    private void Awake()
    {
        ResolveInteractionOrigin();
    }

    private void Update()
    {
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
            StartCoroutine(TriggerAfterDelayAxe(treeTarget, cutDelaySeconds));
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
            StartCoroutine(TriggerAfterDelayPickaxe(stoneTarget, cutDelaySeconds));
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
            triggerInteraction);

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
            triggerInteraction);

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
}
