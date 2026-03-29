using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Controls Mine Stone behavior.
public class MineStone : MonoBehaviour
{
    private const string DefaultStoneItemName = "stone";

    private static InfoHandler cachedInfoHandler;
    private static GetRandomOreType cachedRandomOreType;

    public string type;
    public Material greymat;
    public string texttoshow;
    public Sprite sprite;
    public InfoHandler infoHandler;
    public List<GameObject> parts = new List<GameObject>();
    public GameObject fullstone;
    public List<int> chosen = new List<int>();
    public Material blackmaterial;
    public int counter = 8;
    public GameObject[] mainstoneparts;
    [SerializeField] private float destroyDelaySeconds = 1f;
    [SerializeField] private float rebuildDelaySeconds = 30f;
    public InventoryItem inventoryItem;
    public List<GameObject> orelist = new List<GameObject>();
    public GetRandomOreType getRandomOreType;
    public string currentore;

    [Header("Auto Detect")]
    [SerializeField] private bool autoPopulateFromChildren = true;
    [SerializeField] private bool includeInactiveChildrenForAutoPopulate = true;
    [SerializeField] private string oreChildNameContains = "Icosphere";
    [SerializeField] private string stonePartNameContains = "ST_Stone1_cell";

    [Header("Break Physics")]
    [SerializeField, Min(0f)] private float breakExplosionForce = 2.5f;
    [SerializeField, Min(0.01f)] private float breakExplosionRadius = 2f;
    [SerializeField, Min(0f)] private float breakUpwardModifier = 0.25f;
    [SerializeField, Min(0.01f)] private float breakFragmentMass = 0.2f;
    [SerializeField, Min(0f)] private float breakLinearDamping = 0.2f;
    [SerializeField, Min(0f)] private float breakAngularDamping = 0.9f;
    [SerializeField, Min(0f)] private float breakRandomTorque = 1.2f;

    private Ore selectedOre;
    private int initialCounter;
    private bool isRebuilding;
    private readonly List<GameObject> breakFragments = new List<GameObject>();
    private readonly Dictionary<Transform, TransformSnapshot> initialFragmentTransforms = new Dictionary<Transform, TransformSnapshot>();
    private readonly Dictionary<MeshCollider, bool> cachedBreakMeshColliderConvex = new Dictionary<MeshCollider, bool>();
    private readonly Dictionary<Rigidbody, RigidbodySnapshot> cachedExistingRigidbodies = new Dictionary<Rigidbody, RigidbodySnapshot>();
    private readonly List<Rigidbody> addedRigidbodies = new List<Rigidbody>();
    private readonly List<Collider> addedColliders = new List<Collider>();

    private struct TransformSnapshot
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
    }

    private struct RigidbodySnapshot
    {
        public bool useGravity;
        public bool isKinematic;
        public RigidbodyInterpolation interpolation;
        public CollisionDetectionMode collisionDetectionMode;
        public float linearDamping;
        public float angularDamping;
        public float maxAngularVelocity;
    }

    // Initialize references before gameplay starts.
    private void Awake()
    {
        ResolveReferences();
        AutoPopulateStoneStructure();
        CacheFragmentDefaults();
    }

    // Run in the editor when values change in Inspector.
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ResolveReferences();
            AutoPopulateStoneStructure();
            CacheFragmentDefaults();
        }
    }

    private void Reset()
    {
        fullstone = gameObject;
        AutoPopulateStoneStructure();
    }

    private void Start()
    {
        ResolveReferences();
        AutoPopulateStoneStructure();
        CacheFragmentDefaults();
        initialCounter = Mathf.Max(1, counter);
        counter = initialCounter;
        chosen.Clear();
        InitializeStoneState();
    }

    // Handle Resolve References.
    private void ResolveReferences()
    {
        if (fullstone == null)
        {
            fullstone = gameObject;
        }

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

        if (getRandomOreType == null)
        {
            if (cachedRandomOreType == null)
            {
                cachedRandomOreType = FindOreTypeProviderInScene();
            }

            getRandomOreType = cachedRandomOreType;
        }
        else
        {
            cachedRandomOreType = getRandomOreType;
        }

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

    // Handle Find Ore Type Provider In Scene.
    private static GetRandomOreType FindOreTypeProviderInScene()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<GetRandomOreType>(FindObjectsInactive.Include);
#else
        return FindObjectOfType<GetRandomOreType>(true);
#endif
    }

    [ContextMenu("Auto Populate Stone From Children")]
    private void AutoPopulateStoneStructure()
    {
        if (fullstone == null)
        {
            fullstone = gameObject;
        }

        if (!autoPopulateFromChildren)
        {
            return;
        }

        Transform root = fullstone != null ? fullstone.transform : transform;
        Transform[] descendants = root.GetComponentsInChildren<Transform>(includeInactiveChildrenForAutoPopulate);
        List<GameObject> detectedStoneParts = new List<GameObject>();
        List<GameObject> detectedOreParts = new List<GameObject>();

        for (int i = 0; i < descendants.Length; i++)
        {
            Transform current = descendants[i];
            if (current == null || current == root)
            {
                continue;
            }

            if (NameContains(current.name, stonePartNameContains))
            {
                detectedStoneParts.Add(current.gameObject);
                continue;
            }

            if (NameContains(current.name, oreChildNameContains))
            {
                detectedOreParts.Add(current.gameObject);
            }
        }

        SortByName(detectedStoneParts);
        SortByName(detectedOreParts);

        parts.Clear();
        parts.AddRange(detectedStoneParts);

        orelist.Clear();
        orelist.AddRange(detectedOreParts);

        mainstoneparts = detectedStoneParts.ToArray();
        chosen.Clear();
    }

    // Handle Initialize Stone State.
    private void InitializeStoneState()
    {
        ApplyMaterialToTargets(parts, greymat);
        ApplyOreSelectionAndVisuals();
    }

    // Handle Apply Ore Selection And Visuals.
    private void ApplyOreSelectionAndVisuals()
    {
        ResolveReferences();

        string inventoryName = DefaultStoneItemName;
        Sprite inventorySprite = sprite;

        if (getRandomOreType == null)
        {
            Debug.LogWarning($"{name}: Missing GetRandomOreType reference.", this);
            selectedOre = null;
            currentore = "noore";
        }
        else
        {
            selectedOre = getRandomOreType.GetOreType();
            currentore = selectedOre != null ? selectedOre.oreName : "noore";
            inventoryName = GetInventoryItemName(selectedOre);
            if (selectedOre != null && selectedOre.sprite != null)
            {
                inventorySprite = selectedOre.sprite;
            }
        }

        if (inventoryItem != null)
        {
            inventoryItem.name = inventoryName;
            inventoryItem.nameofitem = inventoryName;
            inventoryItem.inventorysprite = inventorySprite;
        }

        texttoshow = $"{ToDisplayName(inventoryName)} gained";

        for (int i = 0; i < orelist.Count; i++)
        {
            GameObject ore = orelist[i];
            if (ore == null)
            {
                continue;
            }

            bool shouldShowOre = selectedOre != null &&
                                 selectedOre.oreName != "noore" &&
                                 selectedOre.material != null;

            SetObjectRenderersEnabled(ore, shouldShowOre);
            if (shouldShowOre)
            {
                ApplyMaterialToObject(ore, selectedOre.material);
            }
        }
    }

    // Handle Mine.
    public void Mine()
    {
        ResolveReferences();
        EnsureStoneStructureReady();

        if (isRebuilding)
        {
            return;
        }

        if (counter <= 0)
        {
            return;
        }

        counter--;

        if (parts == null || parts.Count == 0)
        {
            Debug.LogWarning($"{name}: No stone parts were auto-detected. Check child names.", this);
            return;
        }

        if (chosen.Count < parts.Count)
        {
            int selectedIndex = GetRandomAvailablePartIndex();
            if (selectedIndex >= 0)
            {
                chosen.Add(selectedIndex);
                ApplyMaterialToObject(parts[selectedIndex], blackmaterial);
            }
        }
        else
        {
            counter = 0;
        }

        if (counter == 0)
        {
            GiveStoneReward();
            StartCoroutine(HandleStoneBreakAndRebuild());
        }
    }

    // Handle Handle Stone Break And Rebuild.
    private IEnumerator HandleStoneBreakAndRebuild()
    {
        isRebuilding = true;
        BreakStoneApart();
        yield return new WaitForSeconds(Mathf.Max(0f, destroyDelaySeconds));
        SetStoneVisible(false);
        RestoreFragmentsFromBreak();
        yield return new WaitForSeconds(Mathf.Max(0f, rebuildDelaySeconds));
        RebuildStone();
        isRebuilding = false;
    }

    // Handle Rebuild Stone.
    private void RebuildStone()
    {
        ResolveReferences();
        EnsureStoneStructureReady();
        counter = initialCounter;
        chosen.Clear();
        SetStoneVisible(true);
        ApplyMaterialToTargets(parts, greymat);
        ApplyOreSelectionAndVisuals();
    }

    // Handle Give Stone Reward.
    private void GiveStoneReward()
    {
        if (inventoryItem != null && inventoryItem.slotManager != null)
        {
            inventoryItem.slotManager.AddItem(inventoryItem);
        }
        else
        {
            Debug.LogWarning($"{name}: Missing InventoryItem or SlotManager reference.", this);
        }

        if (infoHandler != null)
        {
            Sprite infoSprite = inventoryItem != null && inventoryItem.inventorysprite != null
                ? inventoryItem.inventorysprite
                : sprite;
            infoHandler.ShowInfoNow(texttoshow, infoSprite);
        }
        else
        {
            Debug.LogWarning($"{name}: Missing InfoHandler reference.", this);
        }
    }

    // Handle Break Stone Apart.
    private void BreakStoneApart()
    {
        Vector3 explosionCenter = ResolveStoneCenter();

        for (int i = 0; i < breakFragments.Count; i++)
        {
            GameObject fragment = breakFragments[i];
            if (fragment == null)
            {
                continue;
            }

            EnsureFragmentCollider(fragment);

            Rigidbody rigidbody = fragment.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = fragment.AddComponent<Rigidbody>();
                addedRigidbodies.Add(rigidbody);
            }
            else if (!cachedExistingRigidbodies.ContainsKey(rigidbody))
            {
                cachedExistingRigidbodies.Add(rigidbody, CaptureRigidbodySnapshot(rigidbody));
            }

            ConfigureFragmentRigidbody(rigidbody);

            Vector3 directionalForce = fragment.transform.position - explosionCenter;
            if (directionalForce.sqrMagnitude <= 0.0001f)
            {
                directionalForce = UnityEngine.Random.onUnitSphere;
            }

            directionalForce.y = Mathf.Abs(directionalForce.y) + 0.15f;
            rigidbody.AddExplosionForce(breakExplosionForce, explosionCenter, breakExplosionRadius, breakUpwardModifier, ForceMode.Impulse);
            rigidbody.AddForce(directionalForce.normalized * (breakExplosionForce * 0.35f), ForceMode.Impulse);

            if (breakRandomTorque > 0f)
            {
                rigidbody.AddTorque(UnityEngine.Random.insideUnitSphere * breakRandomTorque, ForceMode.Impulse);
            }
        }
    }

    // Handle Restore Fragments From Break.
    private void RestoreFragmentsFromBreak()
    {
        for (int i = 0; i < addedRigidbodies.Count; i++)
        {
            if (addedRigidbodies[i] != null)
            {
                Destroy(addedRigidbodies[i]);
            }
        }

        for (int i = 0; i < addedColliders.Count; i++)
        {
            if (addedColliders[i] != null)
            {
                Destroy(addedColliders[i]);
            }
        }

        foreach (KeyValuePair<Rigidbody, RigidbodySnapshot> entry in cachedExistingRigidbodies)
        {
            Rigidbody rigidbody = entry.Key;
            if (rigidbody == null)
            {
                continue;
            }

            RigidbodySnapshot snapshot = entry.Value;
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.useGravity = snapshot.useGravity;
            rigidbody.isKinematic = snapshot.isKinematic;
            rigidbody.interpolation = snapshot.interpolation;
            rigidbody.collisionDetectionMode = snapshot.collisionDetectionMode;
            rigidbody.linearDamping = snapshot.linearDamping;
            rigidbody.angularDamping = snapshot.angularDamping;
            rigidbody.maxAngularVelocity = snapshot.maxAngularVelocity;
        }

        foreach (KeyValuePair<MeshCollider, bool> entry in cachedBreakMeshColliderConvex)
        {
            if (entry.Key != null)
            {
                entry.Key.convex = entry.Value;
            }
        }

        foreach (KeyValuePair<Transform, TransformSnapshot> entry in initialFragmentTransforms)
        {
            Transform target = entry.Key;
            if (target == null)
            {
                continue;
            }

            TransformSnapshot snapshot = entry.Value;
            target.localPosition = snapshot.localPosition;
            target.localRotation = snapshot.localRotation;
            target.localScale = snapshot.localScale;
        }

        addedRigidbodies.Clear();
        addedColliders.Clear();
    }

    // Handle Ensure Stone Structure Ready.
    private void EnsureStoneStructureReady()
    {
        bool needsAutoPopulate = autoPopulateFromChildren &&
                                 (parts == null || parts.Count == 0 || mainstoneparts == null || mainstoneparts.Length == 0);
        if (needsAutoPopulate)
        {
            AutoPopulateStoneStructure();
        }

        if (breakFragments.Count == 0)
        {
            CacheFragmentDefaults();
        }

        if (initialCounter <= 0)
        {
            initialCounter = Mathf.Max(1, counter);
        }
    }

    // Handle Cache Fragment Defaults.
    private void CacheFragmentDefaults()
    {
        breakFragments.Clear();
        initialFragmentTransforms.Clear();
        cachedBreakMeshColliderConvex.Clear();
        cachedExistingRigidbodies.Clear();
        addedRigidbodies.Clear();
        addedColliders.Clear();

        HashSet<int> seenFragmentIds = new HashSet<int>();
        HashSet<Transform> mainStonePartTransforms = new HashSet<Transform>();

        if (mainstoneparts != null)
        {
            for (int i = 0; i < mainstoneparts.Length; i++)
            {
                GameObject fragment = mainstoneparts[i];
                if (fragment == null)
                {
                    continue;
                }

                mainStonePartTransforms.Add(fragment.transform);
                AddBreakFragment(fragment, seenFragmentIds);
            }
        }

        if (orelist != null)
        {
            for (int i = 0; i < orelist.Count; i++)
            {
                GameObject ore = orelist[i];
                if (ore == null)
                {
                    continue;
                }

                if (HasBreakableAncestor(ore.transform, mainStonePartTransforms))
                {
                    continue;
                }

                AddBreakFragment(ore, seenFragmentIds);
            }
        }
    }

    // Handle Add Break Fragment.
    private void AddBreakFragment(GameObject fragment, HashSet<int> seenFragmentIds)
    {
        if (fragment == null)
        {
            return;
        }

        int instanceId = fragment.GetInstanceID();
        if (!seenFragmentIds.Add(instanceId))
        {
            return;
        }

        breakFragments.Add(fragment);

        Transform fragmentTransform = fragment.transform;
        initialFragmentTransforms[fragmentTransform] = new TransformSnapshot
        {
            localPosition = fragmentTransform.localPosition,
            localRotation = fragmentTransform.localRotation,
            localScale = fragmentTransform.localScale
        };

        MeshCollider meshCollider = fragment.GetComponent<MeshCollider>();
        if (meshCollider != null && !cachedBreakMeshColliderConvex.ContainsKey(meshCollider))
        {
            cachedBreakMeshColliderConvex.Add(meshCollider, meshCollider.convex);
        }

        Rigidbody rigidbody = fragment.GetComponent<Rigidbody>();
        if (rigidbody != null && !cachedExistingRigidbodies.ContainsKey(rigidbody))
        {
            cachedExistingRigidbodies.Add(rigidbody, CaptureRigidbodySnapshot(rigidbody));
        }
    }

    // Handle Set Stone Visible.
    private void SetStoneVisible(bool visible)
    {
        GameObject target = fullstone != null ? fullstone : gameObject;
        if (target == null)
        {
            return;
        }

        if (target != gameObject)
        {
            target.SetActive(visible);
            return;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = visible;
            }
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = visible;
            }
        }
    }

    // Handle Ensure Fragment Collider.
    private void EnsureFragmentCollider(GameObject fragment)
    {
        if (fragment == null)
        {
            return;
        }

        Collider fragmentCollider = fragment.GetComponent<Collider>();
        if (fragmentCollider == null)
        {
            fragmentCollider = CreateFragmentCollider(fragment);
        }

        if (fragmentCollider is MeshCollider meshCollider)
        {
            if (!cachedBreakMeshColliderConvex.ContainsKey(meshCollider))
            {
                cachedBreakMeshColliderConvex.Add(meshCollider, meshCollider.convex);
            }

            meshCollider.convex = true;
            meshCollider.enabled = true;
        }
        else if (fragmentCollider != null)
        {
            fragmentCollider.enabled = true;
        }
    }

    // Handle Create Fragment Collider.
    private Collider CreateFragmentCollider(GameObject fragment)
    {
        MeshFilter meshFilter = fragment.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            MeshCollider meshCollider = fragment.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = true;
            addedColliders.Add(meshCollider);
            return meshCollider;
        }

        if (!TryGetLocalBounds(fragment.transform, out Bounds localBounds))
        {
            return null;
        }

        Vector3 size = localBounds.size;
        size.x = Mathf.Max(size.x, 0.05f);
        size.y = Mathf.Max(size.y, 0.05f);
        size.z = Mathf.Max(size.z, 0.05f);

        BoxCollider boxCollider = fragment.AddComponent<BoxCollider>();
        boxCollider.center = localBounds.center;
        boxCollider.size = size;
        addedColliders.Add(boxCollider);
        return boxCollider;
    }

    // Handle Configure Fragment Rigidbody.
    private void ConfigureFragmentRigidbody(Rigidbody rigidbody)
    {
        if (rigidbody == null)
        {
            return;
        }

        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        rigidbody.useGravity = true;
        rigidbody.isKinematic = false;
        rigidbody.mass = Mathf.Max(0.01f, breakFragmentMass);
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rigidbody.linearDamping = Mathf.Max(0f, breakLinearDamping);
        rigidbody.angularDamping = Mathf.Max(0f, breakAngularDamping);
        rigidbody.maxAngularVelocity = Mathf.Max(1f, breakRandomTorque * 4f);
        rigidbody.ResetCenterOfMass();
        rigidbody.ResetInertiaTensor();
    }

    // Handle Capture Rigidbody Snapshot.
    private static RigidbodySnapshot CaptureRigidbodySnapshot(Rigidbody rigidbody)
    {
        return new RigidbodySnapshot
        {
            useGravity = rigidbody.useGravity,
            isKinematic = rigidbody.isKinematic,
            interpolation = rigidbody.interpolation,
            collisionDetectionMode = rigidbody.collisionDetectionMode,
            linearDamping = rigidbody.linearDamping,
            angularDamping = rigidbody.angularDamping,
            maxAngularVelocity = rigidbody.maxAngularVelocity
        };
    }

    // Handle Resolve Stone Center.
    private Vector3 ResolveStoneCenter()
    {
        GameObject target = fullstone != null ? fullstone : gameObject;
        if (target != null && TryGetWorldBounds(target.transform, out Bounds bounds))
        {
            return bounds.center;
        }

        return transform.position;
    }

    // Handle Get Inventory Item Name.
    private static string GetInventoryItemName(Ore ore)
    {
        if (ore == null || ore.oreName == "noore")
        {
            return DefaultStoneItemName;
        }

        return ore.oreName;
    }

    // Handle To Display Name.
    private static string ToDisplayName(string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemName))
        {
            return "Stone";
        }

        string normalized = itemName.Replace('_', ' ');
        string[] splitParts = normalized.Split(' ');
        for (int i = 0; i < splitParts.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(splitParts[i]))
            {
                continue;
            }

            string lower = splitParts[i].ToLowerInvariant();
            splitParts[i] = char.ToUpperInvariant(lower[0]) + lower.Substring(1);
        }

        return string.Join(" ", splitParts);
    }

    // Handle Apply Material To Targets.
    private static void ApplyMaterialToTargets(List<GameObject> targets, Material material)
    {
        if (targets == null || material == null)
        {
            return;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            ApplyMaterialToObject(targets[i], material);
        }
    }

    // Handle Apply Material To Object.
    private static void ApplyMaterialToObject(GameObject target, Material material)
    {
        if (target == null || material == null)
        {
            return;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].material = material;
            }
        }
    }

    // Handle Set Object Renderers Enabled.
    private static void SetObjectRenderersEnabled(GameObject target, bool enabled)
    {
        if (target == null)
        {
            return;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = enabled;
            }
        }
    }

    // Handle Get Random Available Part Index.
    private int GetRandomAvailablePartIndex()
    {
        if (parts == null || parts.Count == 0)
        {
            return -1;
        }

        List<int> availableIndices = new List<int>();
        for (int i = 0; i < parts.Count; i++)
        {
            if (!chosen.Contains(i))
            {
                availableIndices.Add(i);
            }
        }

        if (availableIndices.Count == 0)
        {
            return -1;
        }

        return availableIndices[UnityEngine.Random.Range(0, availableIndices.Count)];
    }

    // Handle Has Breakable Ancestor.
    private static bool HasBreakableAncestor(Transform target, HashSet<Transform> mainStonePartTransforms)
    {
        if (target == null || mainStonePartTransforms == null || mainStonePartTransforms.Count == 0)
        {
            return false;
        }

        Transform current = target.parent;
        while (current != null)
        {
            if (mainStonePartTransforms.Contains(current))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    // Handle Name Contains.
    private static bool NameContains(string source, string pattern)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        return source.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // Handle Sort By Name.
    private static void SortByName(List<GameObject> targets)
    {
        if (targets == null)
        {
            return;
        }

        targets.Sort((left, right) =>
        {
            if (left == null && right == null)
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            return string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase);
        });
    }

    // Handle Try Get World Bounds.
    private static bool TryGetWorldBounds(Transform target, out Bounds bounds)
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

    // Handle Try Get Local Bounds.
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
            if (currentCollider == null)
            {
                continue;
            }

            EncapsulateWorldBoundsInLocalSpace(target, currentCollider.bounds, ref localBounds, ref hasBounds);
        }

        return hasBounds;
    }

    // Handle Encapsulate World Bounds In Local Space.
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
}
