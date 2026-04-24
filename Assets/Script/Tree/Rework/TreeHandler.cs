using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeHandler : MonoBehaviour
{   
    private const float InitialFallRotationX = 3f;

    public InventoryAddHandler inventoryAddHandler;
    public InventoryItem inventoryItem;
    public int placeinlist = 0;
    public int idk;
    private static PhysicsMaterial fallbackNoRollMaterial;
    private static InventoryAddHandler cachedInventoryAddHandler;
    public List<Material> materials = new List<Material>();

	public int test;    
    [Header("Parts")]
    public float timetowaittodestroy = 8f;
    public GameObject toppart;
    public GameObject bottompart;
    public int counttochop = 3;
    [Header("Fall Physics")]
    [SerializeField, Min(0f)] private float fallTiltDegrees = 3f;
    [SerializeField, Min(0f)] private float fallLinearDamping = 0.2f;
    [SerializeField, Min(0f)] private float fallAngularDamping = 6f;
    [SerializeField, Min(0.1f)] private float fallMaxAngularVelocity = 2f;
    [SerializeField, Min(0f)] private float topPartGroundDespawnDelaySeconds = 1f;
    [SerializeField, Range(0f, 1f)] private float groundHitContactNormalY = 0.35f;
    [Header("Chop")]
    [SerializeField] private int chopsToFall = 4;
    [SerializeField] private float hitCooldownSeconds = 0.12f;
    public GameObject chopImpactParticlePrefab;
    [SerializeField, Range(0f, 1f)] private float chopImpactBetweenFactor = 0.35f;
    [SerializeField] private Vector3 chopImpactOffset = new Vector3(0f, 0.25f, 0f);
    [SerializeField] private bool flattenImpactFacingToHorizontal = true;
    [SerializeField] private float destroyImpactParticleAfterSeconds = 3f;

   
    private int chopCount;
    private float nextChopAllowedTime;
    private bool hasFallen;

    public void Start()
    {
         ResolveReferences();
         GenerateMaterial();
    }
    private void OnValidate()
    {
       if (fallTiltDegrees <= 0f)
       {
           fallTiltDegrees = InitialFallRotationX;
       }
    }
    public void GenerateMaterial()
    {
        MeshRenderer renderer = toppart.GetComponent<MeshRenderer>();
        Material[] rendmatlist = (Material[])renderer.materials.Clone();
        rendmatlist[placeinlist] = materials[Random.Range(0, materials.Count)];
        renderer.materials = rendmatlist;

        
    }
    public void Chop()
    {
        Chop(null);
    }

    public void Chop(Transform attacker)
    {
        ResolveReferences();

        if (hasFallen)
        {
            return;
        }

        if (Time.time < nextChopAllowedTime)
        {
            return;
        }

        nextChopAllowedTime = Time.time + Mathf.Max(0f, hitCooldownSeconds);

        if (counttochop > 0)
        {
            chopCount++;
            SpawnChopImpact(attacker);
            counttochop--;
        }
        else
        {
            hasFallen = true;
            if (inventoryAddHandler != null && inventoryItem != null)
            {
                inventoryAddHandler.AddItemToInventory(inventoryItem);
            }
            else
            {
                Debug.LogWarning($"{name}: Missing InventoryAddHandler or InventoryItem reference.", this);
            }

            TreeFall();
            StartCoroutine(destroyaftertime());
        }
    }

    private void ResolveReferences()
    {
        if (inventoryItem == null)
        {
            inventoryItem = GetComponent<InventoryItem>();
        }

        if (inventoryItem == null)
        {
            inventoryItem = GetComponentInChildren<InventoryItem>(true);
        }

        if (inventoryItem != null)
        {
            inventoryItem.ResolveReferences();
        }

        if (inventoryAddHandler != null)
        {
            cachedInventoryAddHandler = inventoryAddHandler;
            return;
        }

        if (cachedInventoryAddHandler == null)
        {
#if UNITY_2023_1_OR_NEWER
            cachedInventoryAddHandler = FindFirstObjectByType<InventoryAddHandler>(FindObjectsInactive.Include);
#else
            cachedInventoryAddHandler = FindObjectOfType<InventoryAddHandler>(true);
#endif
        }

        inventoryAddHandler = cachedInventoryAddHandler;
    }
    public IEnumerator destroyaftertime()
    {
         yield return new WaitForSeconds(timetowaittodestroy);
         Destroy(toppart);
    }
public void TreeFall()
    {
        if (toppart == null)
        {
            return;
        }

        Vector3 localEulerAngles = toppart.transform.localEulerAngles;
        localEulerAngles.x = InitialFallRotationX;
        toppart.transform.localEulerAngles = localEulerAngles;

        Rigidbody topRigidbody = toppart.GetComponent<Rigidbody>();
        
        if (topRigidbody == null)
        {
            
            topRigidbody = toppart.AddComponent<Rigidbody>();
        }

        ConfigureGroundDespawnOnTopPart();
    }
    private void SpawnChopImpact(Transform attacker)
    {
        if (chopImpactParticlePrefab == null)
        {
            return;
        }

        Vector3 fallbackTreePoint = ResolveTreeFallbackPoint();
        Vector3 attackerPosition = ResolveAttackerPosition(attacker, fallbackTreePoint);
        Vector3 treeImpactPoint = ResolveTreeImpactPoint(attackerPosition);
        Vector3 spawnPosition = Vector3.Lerp(treeImpactPoint, attackerPosition, Mathf.Clamp01(chopImpactBetweenFactor)) + chopImpactOffset;

        Vector3 lookDirection = attackerPosition - spawnPosition;
        if (flattenImpactFacingToHorizontal)
        {
            lookDirection.y = 0f;
        }

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            lookDirection = attackerPosition - treeImpactPoint;
            if (flattenImpactFacingToHorizontal)
            {
                lookDirection.y = 0f;
            }
        }

        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            lookDirection = transform.forward;
        }

        Quaternion rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        GameObject impactInstance = Instantiate(chopImpactParticlePrefab, spawnPosition, rotation);
        PlayImpactParticleSystems(impactInstance);

        float destroyDelay = Mathf.Max(0f, destroyImpactParticleAfterSeconds);
        if (impactInstance != null && destroyDelay > 0f)
        {
            Destroy(impactInstance, destroyDelay);
        }
    }

    private Vector3 ResolveTreeImpactPoint(Vector3 attackerPosition)
    {
        if (bottompart != null && TryResolveClosestImpactPoint(bottompart.transform, attackerPosition, out Vector3 closestImpactPoint))
        {
            return closestImpactPoint;
        }

        if (bottompart != null && TryGetBounds(bottompart.transform, out Bounds bottomBounds))
        {
            float impactY = Mathf.Lerp(bottomBounds.min.y, bottomBounds.max.y, 0.55f);
            return new Vector3(bottomBounds.center.x, impactY, bottomBounds.center.z);
        }

        return ResolveTreeFallbackPoint();
    }

    private Vector3 ResolveTreeFallbackPoint()
    {
        return transform.position + Vector3.up * 0.5f;
    }

    private static Vector3 ResolveAttackerPosition(Transform attacker, Vector3 fallbackTreePoint)
    {
        if (attacker != null)
        {
            return attacker.position;
        }

        if (Camera.main != null)
        {
            return Camera.main.transform.position;
        }

        return fallbackTreePoint + Vector3.forward;
    }

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

    private void PrepareTopPartCollidersForFall(Transform topTransform)
    {
        if (topTransform == null)
        {
            return;
        }

        PhysicsMaterial noRollMaterial = ResolveNoRollMaterial();
        CapsuleCollider[] capsuleColliders = topTransform.GetComponentsInChildren<CapsuleCollider>(true);
        for (int i = 0; i < capsuleColliders.Length; i++)
        {
            ReplaceCapsuleColliderForFall(capsuleColliders[i], noRollMaterial);
        }

        Collider[] colliders = topTransform.GetComponentsInChildren<Collider>(true);
        bool hasSolidCollider = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider currentCollider = colliders[i];
            if (currentCollider == null || !currentCollider.enabled || currentCollider.isTrigger)
            {
                continue;
            }

            if (currentCollider is MeshCollider meshCollider)
            {
                meshCollider.convex = true;
                meshCollider.providesContacts = true;
            }

            currentCollider.material = noRollMaterial;
            hasSolidCollider = true;
        }

        if (!hasSolidCollider)
        {
            TryEnsureFallbackBoxCollider(topTransform, noRollMaterial);
        }
    }

    private static void ReplaceCapsuleColliderForFall(CapsuleCollider capsuleCollider, PhysicsMaterial noRollMaterial)
    {
        if (capsuleCollider == null || !capsuleCollider.enabled || capsuleCollider.isTrigger)
        {
            return;
        }

        bool replacementCreated = TryEnsureConvexMeshCollider(capsuleCollider.gameObject, noRollMaterial);
        if (!replacementCreated)
        {
            replacementCreated = TryEnsureFallbackBoxCollider(capsuleCollider.transform, noRollMaterial);
        }

        if (replacementCreated)
        {
            capsuleCollider.enabled = false;
            return;
        }

        capsuleCollider.material = noRollMaterial;
    }

    private static bool TryEnsureConvexMeshCollider(GameObject target, PhysicsMaterial noRollMaterial)
    {
        if (target == null)
        {
            return false;
        }

        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return false;
        }

        MeshCollider meshCollider = target.GetComponent<MeshCollider>();
        if (meshCollider == null)
        {
            meshCollider = target.AddComponent<MeshCollider>();
        }

        meshCollider.sharedMesh = meshFilter.sharedMesh;
        meshCollider.convex = true;
        meshCollider.providesContacts = true;
        meshCollider.material = noRollMaterial;
        meshCollider.enabled = true;
        return true;
    }

    private static bool TryEnsureFallbackBoxCollider(Transform target, PhysicsMaterial noRollMaterial)
    {
        if (target == null || !TryGetLocalBounds(target, out Bounds localBounds))
        {
            return false;
        }

        Vector3 size = localBounds.size;
        size.x = Mathf.Max(size.x, 0.05f);
        size.y = Mathf.Max(size.y, 0.05f);
        size.z = Mathf.Max(size.z, 0.05f);

        BoxCollider boxCollider = target.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = target.gameObject.AddComponent<BoxCollider>();
        }

        boxCollider.center = localBounds.center;
        boxCollider.size = size;
        boxCollider.material = noRollMaterial;
        boxCollider.enabled = true;
        return true;
    }

    private void ConfigureTopPartRigidbody(Rigidbody topRigidbody)
    {
        if (topRigidbody == null)
        {
            return;
        }

        topRigidbody.linearVelocity = Vector3.zero;
        topRigidbody.angularVelocity = Vector3.zero;
        topRigidbody.useGravity = true;
        topRigidbody.isKinematic = false;
        topRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        topRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        topRigidbody.linearDamping = Mathf.Max(0f, fallLinearDamping);
        topRigidbody.angularDamping = Mathf.Max(0f, fallAngularDamping);
        topRigidbody.maxAngularVelocity = Mathf.Max(0.1f, fallMaxAngularVelocity);
        topRigidbody.ResetCenterOfMass();
        topRigidbody.ResetInertiaTensor();
    }

    private void ConfigureGroundDespawnOnTopPart()
    {
        if (toppart == null)
        {
            return;
        }

        TreeTopGroundDespawn groundDespawn = toppart.GetComponent<TreeTopGroundDespawn>();
        if (groundDespawn == null)
        {
            groundDespawn = toppart.AddComponent<TreeTopGroundDespawn>();
        }

        groundDespawn.Configure(transform, topPartGroundDespawnDelaySeconds, groundHitContactNormalY);
    }

    private static PhysicsMaterial ResolveNoRollMaterial()
    {
        if (fallbackNoRollMaterial != null)
        {
            return fallbackNoRollMaterial;
        }

        fallbackNoRollMaterial = new PhysicsMaterial("TreeHandler_NoRoll")
        {
            dynamicFriction = 1f,
            staticFriction = 1f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Maximum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        fallbackNoRollMaterial.hideFlags = HideFlags.HideAndDontSave;
        return fallbackNoRollMaterial;
    }

    private static bool TryGetLocalBounds(Transform target, out Bounds localBounds)
    {
        localBounds = default;
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

            EncapsulateWorldBoundsInLocalSpace(target, currentRenderer.bounds, ref localBounds, ref hasBounds);
        }

        if (hasBounds)
        {
            return true;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider currentCollider = colliders[i];
            if (currentCollider == null || !currentCollider.enabled)
            {
                continue;
            }

            EncapsulateWorldBoundsInLocalSpace(target, currentCollider.bounds, ref localBounds, ref hasBounds);
        }

        return hasBounds;
    }

    private static void EncapsulateWorldBoundsInLocalSpace(Transform target, Bounds worldBounds, ref Bounds localBounds, ref bool hasBounds)
    {
        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 localPoint = target.InverseTransformPoint(center + Vector3.Scale(extents, new Vector3(x, y, z)));
                    if (!hasBounds)
                    {
                        localBounds = new Bounds(localPoint, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        localBounds.Encapsulate(localPoint);
                    }
                }
            }
        }
    }

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

}

sealed class TreeTopGroundDespawn : MonoBehaviour
{
    private Transform owningTreeRoot;
    private float despawnDelaySeconds = 1f;
    private float minimumGroundNormalY = 0.35f;
    private bool despawnScheduled;

    public void Configure(Transform treeRoot, float delaySeconds, float groundNormalY)
    {
        owningTreeRoot = treeRoot;
        despawnDelaySeconds = Mathf.Max(0f, delaySeconds);
        minimumGroundNormalY = Mathf.Clamp01(groundNormalY);
        despawnScheduled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryScheduleDespawn(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        TryScheduleDespawn(collision);
    }

    private void TryScheduleDespawn(Collision collision)
    {
        if (despawnScheduled || !IsGroundHit(collision))
        {
            return;
        }

        despawnScheduled = true;
        Destroy(gameObject, despawnDelaySeconds);
    }

    private bool IsGroundHit(Collision collision)
    {
        if (collision == null || collision.collider == null || collision.contactCount <= 0)
        {
            return false;
        }

        if (owningTreeRoot != null && collision.collider.transform.IsChildOf(owningTreeRoot))
        {
            return false;
        }

        if (collision.collider is TerrainCollider)
        {
            return true;
        }

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y >= minimumGroundNormalY)
            {
                return true;
            }
        }

        return false;
    }
}
