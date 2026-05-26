using System.Collections.Generic; using UnityEngine;

// Replaces painted terrain proxy prefabs with runtime gameplay prefabs when the scene starts.
public class TerrainTreeProxySpawner : MonoBehaviour {
    [System.Serializable] public class ProxyMapping {
        public GameObject terrainProxyPrefab;
        public GameObject runtimePrefab; }

    [Header("Runtime Conversion")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool removePaintedProxyInstances = true;
    [SerializeField] private bool cloneTerrainDataDuringPlayInEditor = true;
    [SerializeField] private bool logConversionSummary = true;

    [Header("Spawn Root")]
    [SerializeField] private Transform spawnParent;
    [SerializeField] private string spawnParentName = "ConvertedTerrainTrees";
    [SerializeField] private bool createSpawnParentIfMissing = true;

    [Header("Placement")]
    [SerializeField] private bool snapSpawnedObjectsToTerrain = true;
    [SerializeField] private bool alignSpawnedObjectsToTerrainNormal = false;
    [SerializeField] private float terrainSurfaceOffset = 0f;
    [SerializeField, Min(0f)] private float mineStoneEmbedDepth = 0.2f;

    [Header("Mappings")]
    [SerializeField] private List<ProxyMapping> mappings = new List<ProxyMapping>();

    private readonly Dictionary<Terrain, TerrainData> _runtimeTerrainData = new Dictionary<Terrain, TerrainData>();

    private void Start() { if (!Application.isPlaying || !spawnOnStart) { return; }

        SpawnMappedRuntimePrefabs(); }

    [ContextMenu("Spawn Mapped Runtime Prefabs")] public void SpawnMappedRuntimePrefabs() {
        Dictionary<GameObject, GameObject> mappingLookup = BuildMappingLookup();
        if (mappingLookup.Count == 0) { if (logConversionSummary) { Debug.Log("TerrainTreeProxySpawner: No valid proxy mappings configured.", this); }

            return; }

        Transform resolvedSpawnParent = ResolveSpawnParent();
        if (resolvedSpawnParent == null) {
            Debug.LogWarning("TerrainTreeProxySpawner: Failed to resolve spawn parent.", this);
            return; }

        int terrainsConverted = 0;
        int instancesConverted = 0;

        Terrain[] terrains = Terrain.activeTerrains;
        for (int terrainIndex = 0; terrainIndex < terrains.Length; terrainIndex++) {
            Terrain terrain = terrains[terrainIndex];
            if (terrain == null || terrain.terrainData == null) { continue; }

            if (!TryResolveMappedRuntimePrefabs(terrain.terrainData, mappingLookup, out GameObject[] runtimePrefabsByPrototypeIndex)) { continue; }

            TerrainData terrainData = terrain.terrainData;
            TreeInstance[] treeInstances = terrainData.treeInstances;
            if (treeInstances == null || treeInstances.Length == 0) { continue; }

            if (removePaintedProxyInstances) {
                terrainData = EnsureRuntimeTerrainData(terrain);
                treeInstances = terrainData.treeInstances;
                if (!TryResolveMappedRuntimePrefabs(terrainData, mappingLookup, out runtimePrefabsByPrototypeIndex)) { continue; } }

            List<TreeInstance> remainingInstances = removePaintedProxyInstances
                ? new List<TreeInstance>(treeInstances.Length)
                : null;
            int terrainConvertedCount = 0;

            for (int instanceIndex = 0; instanceIndex < treeInstances.Length; instanceIndex++) {
                TreeInstance instance = treeInstances[instanceIndex];
                if (instance.prototypeIndex < 0 || instance.prototypeIndex >= runtimePrefabsByPrototypeIndex.Length) { if (removePaintedProxyInstances) { remainingInstances.Add(instance); }

                    continue; }

                GameObject runtimePrefab = runtimePrefabsByPrototypeIndex[instance.prototypeIndex];
                if (runtimePrefab == null) { if (removePaintedProxyInstances) { remainingInstances.Add(instance); }

                    continue; }

                SpawnRuntimePrefabForInstance(terrain, terrainData, instance, runtimePrefab, resolvedSpawnParent);
                terrainConvertedCount++; }

            if (terrainConvertedCount <= 0) { continue; }

            terrainsConverted++;
            instancesConverted += terrainConvertedCount;

            if (removePaintedProxyInstances) { terrainData.treeInstances = remainingInstances.ToArray(); } }

        RefreshRuntimeOptimizer(resolvedSpawnParent);

        if (logConversionSummary) {
            Debug.Log(
                $"TerrainTreeProxySpawner: Converted {instancesConverted} painted proxy instances across {terrainsConverted} terrains.",
                this); } }

    private Dictionary<GameObject, GameObject> BuildMappingLookup() {
        Dictionary<GameObject, GameObject> lookup = new Dictionary<GameObject, GameObject>();
        for (int i = 0; i < mappings.Count; i++) {
            ProxyMapping mapping = mappings[i];
            if (mapping == null || mapping.terrainProxyPrefab == null || mapping.runtimePrefab == null) { continue; }

            lookup[mapping.terrainProxyPrefab] = mapping.runtimePrefab; }

        return lookup; }

    private bool TryResolveMappedRuntimePrefabs(
        TerrainData terrainData,
        Dictionary<GameObject, GameObject> mappingLookup,
        out GameObject[] runtimePrefabsByPrototypeIndex) {
        runtimePrefabsByPrototypeIndex = null;
        if (terrainData == null) { return false; }

        TreePrototype[] prototypes = terrainData.treePrototypes;
        if (prototypes == null || prototypes.Length == 0) { return false; }

        runtimePrefabsByPrototypeIndex = new GameObject[prototypes.Length];
        bool hasMappedPrototype = false;

        for (int i = 0; i < prototypes.Length; i++) {
            TreePrototype prototype = prototypes[i];
            GameObject proxyPrefab = prototype != null ? prototype.prefab : null;
            if (proxyPrefab == null) { continue; }

            if (!mappingLookup.TryGetValue(proxyPrefab, out GameObject runtimePrefab) || runtimePrefab == null) { continue; }

            runtimePrefabsByPrototypeIndex[i] = runtimePrefab;
            hasMappedPrototype = true; }

        return hasMappedPrototype; }

    private TerrainData EnsureRuntimeTerrainData(Terrain terrain) { if (terrain == null || terrain.terrainData == null) { return null; }

        if (_runtimeTerrainData.TryGetValue(terrain, out TerrainData cachedRuntimeData) && cachedRuntimeData != null) { return cachedRuntimeData; }

        TerrainData sourceData = terrain.terrainData;
        TerrainData runtimeData = sourceData;

#if UNITY_EDITOR
        if (cloneTerrainDataDuringPlayInEditor) {
            runtimeData = Instantiate(sourceData);
            runtimeData.name = $"{sourceData.name} Runtime";
            terrain.terrainData = runtimeData;

            TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
            if (terrainCollider != null) { terrainCollider.terrainData = runtimeData; } }
#endif

        _runtimeTerrainData[terrain] = runtimeData;
        return runtimeData; }

    private void SpawnRuntimePrefabForInstance(
        Terrain terrain,
        TerrainData terrainData,
        TreeInstance instance,
        GameObject runtimePrefab,
        Transform parent) { if (terrain == null || terrainData == null || runtimePrefab == null || parent == null) { return; }

        Vector3 worldPosition = terrain.transform.position + Vector3.Scale(instance.position, terrainData.size);
        Quaternion worldRotation = Quaternion.Euler(0f, instance.rotation * Mathf.Rad2Deg, 0f);

        GameObject spawned = Instantiate(runtimePrefab, worldPosition, worldRotation, parent);
        if (spawned == null) { return; }

        Vector3 baseScale = spawned.transform.localScale;
        spawned.transform.localScale = new Vector3(
            baseScale.x * instance.widthScale,
            baseScale.y * instance.heightScale,
            baseScale.z * instance.widthScale);

        ApplyTerrainPlacement(spawned.transform, terrain);

        InventoryItem inventoryItem = spawned.GetComponentInChildren<InventoryItem>(true);
        if (inventoryItem != null) { inventoryItem.ResolveReferences(); } }

    private Transform ResolveSpawnParent() { if (spawnParent != null) { return spawnParent; }

        if (!string.IsNullOrWhiteSpace(spawnParentName)) {
            GameObject existingRoot = GameObject.Find(spawnParentName);
            if (existingRoot != null) {
                spawnParent = existingRoot.transform;
                return spawnParent; } }

        if (!createSpawnParentIfMissing) { return null; }

        string rootName = string.IsNullOrWhiteSpace(spawnParentName) ? "TerrainRuntimeProxies" : spawnParentName;
        GameObject createdRoot = new GameObject(rootName);
        spawnParent = createdRoot.transform;
        return spawnParent; }

    private void RefreshRuntimeOptimizer(Transform resolvedSpawnParent) { if (resolvedSpawnParent == null) { return; }

        ResHandler optimizer = UnitySceneSearch.FindFirst<ResHandler>();

        if (optimizer == null) { return; }

        optimizer.RegisterManagedRoot(resolvedSpawnParent);
        optimizer.RefreshManagedData(); }

    private void ApplyTerrainPlacement(Transform target, Terrain terrain) { if (target == null || terrain == null || terrain.terrainData == null) { return; }

        if (alignSpawnedObjectsToTerrainNormal) { AlignObjectToTerrainNormal(target, terrain); }

        if (snapSpawnedObjectsToTerrain) { SnapObjectBaseToTerrain(target, terrain); } }

    private void SnapObjectBaseToTerrain(Transform target, Terrain terrain) {
        Vector3 position = target.position;
        float terrainHeight = terrain.SampleHeight(position) + terrain.transform.position.y + terrainSurfaceOffset;
        float groundEmbedDepth = GetAdditionalGroundEmbedDepth(target);

        if (TryGetGroundingBounds(target, out Bounds bounds)) { position.y += terrainHeight - bounds.min.y - groundEmbedDepth; } else { position.y = terrainHeight - groundEmbedDepth; }

        target.position = position; }

    private float GetAdditionalGroundEmbedDepth(Transform target) { if (mineStoneEmbedDepth <= 0f || target == null) { return 0f; }

        return target.GetComponentInChildren<MineStone>(true) != null
            ? mineStoneEmbedDepth
            : 0f; }

    private static bool TryGetGroundingBounds(Transform target, out Bounds bounds) { if (TryGetMineStoneBounds(target, out bounds)) { return true; }

        if (TryGetObjectBounds(target, includeInactive: false, out bounds)) { return true; }

        return TryGetObjectBounds(target, includeInactive: true, out bounds); }

    private static bool TryGetMineStoneBounds(Transform target, out Bounds bounds) {
        bounds = default;
        if (target == null) { return false; }

        MineStone mineStone = target.GetComponentInChildren<MineStone>(true);
        if (mineStone == null) { return false; }

        bool hasBounds = false;

        if (mineStone.mainstoneparts != null && mineStone.mainstoneparts.Length > 0) {
            for (int i = 0; i < mineStone.mainstoneparts.Length; i++) {
                GameObject part = mineStone.mainstoneparts[i];
                if (part == null) { continue; }

                if (TryEncapsulateRenderers(part.transform, includeInactive: false, ref bounds, ref hasBounds)) { continue; }

                TryEncapsulateColliders(part.transform, includeInactive: false, ref bounds, ref hasBounds); } }

        return hasBounds; }

    private static bool TryGetObjectBounds(Transform target, bool includeInactive, out Bounds bounds) {
        bounds = default;
        if (target == null) { return false; }

        bool hasBounds = false;
        TryEncapsulateRenderers(target, includeInactive, ref bounds, ref hasBounds);

        if (hasBounds) { return true; }

        TryEncapsulateColliders(target, includeInactive, ref bounds, ref hasBounds);

        return hasBounds; }

    private static bool TryEncapsulateRenderers(Transform target, bool includeInactive, ref Bounds bounds, ref bool hasBounds) { if (target == null) { return hasBounds; }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(includeInactive);
        for (int i = 0; i < renderers.Length; i++) {
            Renderer renderer = renderers[i];
            if (renderer == null) { continue; }

            if (!includeInactive && (!renderer.enabled || !renderer.gameObject.activeInHierarchy)) { continue; }

            if (!hasBounds) {
                bounds = renderer.bounds;
                hasBounds = true; } else { bounds.Encapsulate(renderer.bounds); } }

        return hasBounds; }

    private static bool TryEncapsulateColliders(Transform target, bool includeInactive, ref Bounds bounds, ref bool hasBounds) { if (target == null) { return hasBounds; }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(includeInactive);
        for (int i = 0; i < colliders.Length; i++) {
            Collider collider = colliders[i];
            if (collider == null) { continue; }

            if (!includeInactive && (!collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger)) { continue; }

            if (!hasBounds) {
                bounds = collider.bounds;
                hasBounds = true; } else { bounds.Encapsulate(collider.bounds); } }

        return hasBounds; }

    private static void AlignObjectToTerrainNormal(Transform target, Terrain terrain) {
        TerrainData terrainData = terrain.terrainData;
        Vector3 localPosition = target.position - terrain.transform.position;
        float normalizedX = terrainData.size.x > 0f ? Mathf.Clamp01(localPosition.x / terrainData.size.x) : 0f;
        float normalizedZ = terrainData.size.z > 0f ? Mathf.Clamp01(localPosition.z / terrainData.size.z) : 0f;
        Vector3 terrainNormal = terrainData.GetInterpolatedNormal(normalizedX, normalizedZ);

        if (terrainNormal.sqrMagnitude < 0.0001f) { return; }

        Vector3 projectedForward = Vector3.ProjectOnPlane(target.forward, terrainNormal);
        if (projectedForward.sqrMagnitude < 0.0001f) { projectedForward = Vector3.ProjectOnPlane(target.right, terrainNormal); }

        if (projectedForward.sqrMagnitude < 0.0001f) { projectedForward = Vector3.Cross(terrainNormal, Vector3.right); }

        target.rotation = Quaternion.LookRotation(projectedForward.normalized, terrainNormal.normalized); } }
