using System.Collections; using System.Collections.Generic; using UnityEngine; using UnityEngine.Rendering;

// Aggressive runtime optimizer for very large scenes.
public class ResHandler : MonoBehaviour {
    private const float BaseTreeRenderDistance = 360f;
    private const float BaseTreeShadowDistance = 45f;
    private const float BaseTerrainTreeDistance = 360f;
    private const float BaseTerrainBillboardStart = 55f;
    private const float BaseCameraFarClipDistance = 700f;
    private const float BaseLightDistance = 32f;
    private const float BaseLightShadowDistance = 8f;

    [Header("Resolution")]
    [SerializeField] private bool forceResolutionOnStart = true;
    [Tooltip("If true, uses highest supported refresh rate (ignores Target Refresh Rate).")]
    [SerializeField] private bool useHighestRefreshRate = true;
    [SerializeField] private int targetWidth = 1920;
    [SerializeField] private int targetHeight = 1080;
    [Tooltip("Set 0 to use the highest supported refresh rate.")]
    [SerializeField] private int targetRefreshRate = 0;
    [SerializeField] private FullScreenMode fullscreenMode = FullScreenMode.ExclusiveFullScreen;

    [Header("Distance Culling")]
    [Tooltip("Main camera used for culling checks.")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Roots containing heavy objects (Trees&stones, Trees, etc.).")]
    [SerializeField] private Transform[] managedRoots;
    [SerializeField] private bool includeInactiveChildren;
    [SerializeField] private bool useGameObjectCulling = true;
    [SerializeField] private bool useRendererCullingFallback;
    [Tooltip("Also cull root-level tree/stone/rock objects that are not under a managed root.")]
    [SerializeField] private bool autoIncludeLooseHeavyObjects = true;
    [SerializeField, Min(40f)] private float treeRenderDistance = 360f;
    [SerializeField, Min(20f)] private float treeShadowDistance = 45f;
    [SerializeField, Range(0.03f, 1f)] private float cullingUpdateInterval = 0.2f;
    [SerializeField, Min(128)] private int maxObjectsProcessedPerTick = 450;
    [SerializeField, Min(128)] private int maxRenderersProcessedPerTick = 400;
    [SerializeField, Min(256)] private int initialCullBatchSize = 400;

    [Header("Safety Caps")]
    [SerializeField] private bool enforceDistanceCaps = true;
    [SerializeField, Min(60f)] private float hardMaxRenderDistance = 600f;
    [SerializeField, Min(20f)] private float hardMaxShadowDistance = 120f;
    [SerializeField] private bool clampCameraFarClip = true;
    [SerializeField, Min(100f)] private float cameraFarClipDistance = 700f;

    [Header("Global Quality")]
    [SerializeField] private bool applyGlobalQualityClamps = true;
    [SerializeField] private bool applyGlobalShadowDistance = true;
    [SerializeField, Min(20f)] private float globalShadowDistance = 35f;
    [SerializeField, Range(0.3f, 2f)] private float qualityLodBias = 0.65f;
    [SerializeField, Range(0.1f, 1f)] private float terrainDetailDensityScale = 0.25f;
    [SerializeField, Min(40f)] private float terrainTreeDistance = 360f;
    [SerializeField, Min(20f)] private float terrainBillboardStart = 55f;

    [Header("Lights")]
    [SerializeField] private bool optimizeRealtimeLights = true;
    [SerializeField, Range(0.05f, 1f)] private float lightsUpdateInterval = 0.35f;
    [SerializeField, Min(10f)] private float nonDirectionalLightDistance = 32f;
    [SerializeField, Min(5f)] private float nonDirectionalShadowDistance = 8f;
    [SerializeField, Min(0)] private int maxShadowedNonDirectionalLights = 0;
    [SerializeField] private bool disableShadowsOnDisabledLights = true;

    [Header("Adaptive Runtime")]
    [SerializeField] private bool adaptiveDistanceByFps = true;
    [SerializeField, Range(15f, 120f)] private float lowFpsThreshold = 58f;
    [SerializeField, Range(15f, 180f)] private float highFpsThreshold = 75f;
    [SerializeField, Range(0.2f, 3f)] private float adaptiveCheckInterval = 1f;
    [SerializeField, Min(5f)] private float adaptiveStep = 30f;
    [SerializeField, Min(50f)] private float adaptiveMinRenderDistance = 260f;
    [SerializeField, Min(80f)] private float adaptiveMaxRenderDistance = 430f;
    [SerializeField, Min(20f)] private float adaptiveMinShadowDistance = 25f;
    [SerializeField, Min(40f)] private float adaptiveMaxShadowDistance = 60f;

    private readonly List<ManagedObject> _managedObjects = new List<ManagedObject>(8192);
    private readonly List<ManagedRenderer> _managedRenderers = new List<ManagedRenderer>(4096);
    private readonly List<ManagedLight> _managedLights = new List<ManagedLight>(256);
    private readonly HashSet<int> _uniqueManagedObjectIds = new HashSet<int>();
    private readonly HashSet<int> _uniqueManagedRendererIds = new HashSet<int>();

    private float _nextCullingUpdateTime;
    private float _nextLightsUpdateTime;
    private float _nextAdaptiveCheckTime;
    private int _objectRoundRobinIndex;
    private int _rendererRoundRobinIndex;
    private int _fpsFrameCount;
    private float _fpsAccumTime;
    private bool settingsViewDistanceOverrideActive;

    private struct ManagedObject {
        public GameObject gameObject;
        public bool initialActive;
        public Vector3 cachedPosition; }

    private struct ManagedRenderer {
        public Renderer renderer;
        public bool initialEnabled;
        public ShadowCastingMode originalShadows; }

    private struct ManagedLight {
        public Light light;
        public bool initialEnabled;
        public LightShadows originalShadows;
        public LightType type; }

    private void OnValidate() { ClampRuntimeTuning(); }

    private void Awake() {
        ClampRuntimeTuning();

        if (forceResolutionOnStart) {
            int requestedRefresh = useHighestRefreshRate ? 0 : Mathf.Max(0, targetRefreshRate);
            Screen.SetResolution(targetWidth, targetHeight, fullscreenMode, requestedRefresh); }

        ResolveCamera();
        ApplyViewDistanceMultiplier(GameSettings.ViewDistance);
        if (clampCameraFarClip && targetCamera != null) { targetCamera.farClipPlane = Mathf.Min(targetCamera.farClipPlane, cameraFarClipDistance); }

        if (applyGlobalShadowDistance) { QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, globalShadowDistance); }

        if (applyGlobalQualityClamps) { ApplyQualityClamps(); }

        AutoAssignManagedRootsIfMissing();
        CollectManagedData();
        CollectLights(); }

    private void Start() {
        if (useGameObjectCulling && _managedObjects.Count > 0) { StartCoroutine(InitialObjectCullPass()); } else if (_managedRenderers.Count > 0) { ForceRendererCullAllNow(); } }

    private void OnDisable() { RestoreManagedState(); }

    private void Update() {
        if (targetCamera == null) {
            ResolveCamera();
            if (targetCamera == null) { return; } }

        _fpsFrameCount++;
        _fpsAccumTime += Mathf.Max(0.0001f, Time.unscaledDeltaTime);

        if (Time.time >= _nextCullingUpdateTime) {
            _nextCullingUpdateTime = Time.time + Mathf.Max(0.03f, cullingUpdateInterval);

            if (useGameObjectCulling && _managedObjects.Count > 0) { ProcessObjectCullingBatch(); } else if (_managedRenderers.Count > 0) { ProcessRendererCullingBatch(); } }

        if (optimizeRealtimeLights && Time.time >= _nextLightsUpdateTime) {
            _nextLightsUpdateTime = Time.time + Mathf.Max(0.05f, lightsUpdateInterval);
            ProcessLights(); }

        if (adaptiveDistanceByFps && Time.time >= _nextAdaptiveCheckTime) {
            _nextAdaptiveCheckTime = Time.time + Mathf.Max(0.2f, adaptiveCheckInterval);
            AdaptDistancesFromFps(); } }

    [ContextMenu("Refresh Managed Data")] public void RefreshManagedData() { CollectManagedData(); }

    [ContextMenu("Refresh Lights")] public void RefreshLights() { CollectLights(); }

    public void ApplyViewDistanceMultiplier(float multiplier) {
        float viewDistance = Mathf.Clamp(multiplier, GameSettings.ViewDistanceMin, GameSettings.ViewDistanceMax);
        float scaledRoot = Mathf.Sqrt(viewDistance);
        settingsViewDistanceOverrideActive = true;

        treeRenderDistance = Mathf.Clamp(BaseTreeRenderDistance * viewDistance, 40f, 3000f);
        treeShadowDistance = Mathf.Clamp(BaseTreeShadowDistance * scaledRoot, 10f, 300f);
        terrainTreeDistance = Mathf.Clamp(BaseTerrainTreeDistance * viewDistance, 40f, 3000f);
        terrainBillboardStart = Mathf.Clamp(BaseTerrainBillboardStart * scaledRoot, 15f, 700f);
        cameraFarClipDistance = Mathf.Clamp(BaseCameraFarClipDistance * viewDistance, 120f, 5000f);
        nonDirectionalLightDistance = Mathf.Clamp(BaseLightDistance * scaledRoot, 8f, 260f);
        nonDirectionalShadowDistance = Mathf.Clamp(BaseLightShadowDistance * scaledRoot, 3f, 120f);

        hardMaxRenderDistance = Mathf.Max(hardMaxRenderDistance, treeRenderDistance);
        hardMaxShadowDistance = Mathf.Max(hardMaxShadowDistance, treeShadowDistance);
        adaptiveMinRenderDistance = Mathf.Min(adaptiveMinRenderDistance, treeRenderDistance);
        adaptiveMaxRenderDistance = Mathf.Max(adaptiveMaxRenderDistance, treeRenderDistance);
        adaptiveMinShadowDistance = Mathf.Min(adaptiveMinShadowDistance, treeShadowDistance);
        adaptiveMaxShadowDistance = Mathf.Max(adaptiveMaxShadowDistance, treeShadowDistance);

        ClampRuntimeTuning();
        QualitySettings.lodBias = viewDistance;
        ApplyTerrainDistanceSettings();
        ResolveCamera();
        if (clampCameraFarClip && targetCamera != null) { targetCamera.farClipPlane = Mathf.Max(targetCamera.nearClipPlane + 10f, cameraFarClipDistance); }

        _nextCullingUpdateTime = 0f;
        _nextLightsUpdateTime = 0f; }

    public void RegisterManagedRoot(Transform root) { if (root == null || IsConvertedTerrainObject(root)) { return; }

        if (managedRoots == null) { managedRoots = new Transform[0]; }

        for (int i = 0; i < managedRoots.Length; i++) { if (managedRoots[i] == root) { return; } }

        Transform[] expandedRoots = new Transform[managedRoots.Length + 1];
        for (int i = 0; i < managedRoots.Length; i++) { expandedRoots[i] = managedRoots[i]; }

        expandedRoots[managedRoots.Length] = root;
        managedRoots = expandedRoots; }

    private void ClampRuntimeTuning() { if (!enforceDistanceCaps) { return; }

        treeRenderDistance = Mathf.Min(treeRenderDistance, hardMaxRenderDistance);
        treeShadowDistance = Mathf.Min(treeShadowDistance, hardMaxShadowDistance);
        nonDirectionalLightDistance = Mathf.Min(nonDirectionalLightDistance, hardMaxRenderDistance);
        nonDirectionalShadowDistance = Mathf.Min(nonDirectionalShadowDistance, hardMaxShadowDistance); }

    private void ResolveCamera() { if (targetCamera == null) { targetCamera = Camera.main; } }

    private void AutoAssignManagedRootsIfMissing() {
        List<Transform> found = new List<Transform>(6);
        if (managedRoots != null) {
            for (int i = 0; i < managedRoots.Length; i++) { AddRootIfMissing(found, managedRoots[i]); } }

        TryAddRootByName(found, "Trees&stones");
        TryAddRootByName(found, "Trees");
        TryAddRootByName(found, "Stones");
        TryAddRootByName(found, "Rocks");
        TryAddRootByName(found, "Lamps");

        managedRoots = found.Count > 0 ? found.ToArray() : new Transform[0]; }

    private static void TryAddRootByName(List<Transform> found, string objectName) {
        GameObject go = GameObject.Find(objectName);
        if (go != null) { AddRootIfMissing(found, go.transform); } }

    private static void AddRootIfMissing(List<Transform> found, Transform root) {
        if (root == null) { return; }

        for (int i = 0; i < found.Count; i++) { if (found[i] == root) { return; } }

        found.Add(root); }

    private void CollectManagedData() {
        _managedObjects.Clear();
        _managedRenderers.Clear();
        _uniqueManagedObjectIds.Clear();
        _uniqueManagedRendererIds.Clear();

        if (managedRoots != null) {
            for (int i = 0; i < managedRoots.Length; i++) {
                Transform root = managedRoots[i];
                if (root == null) { continue; }

                if (IsConvertedTerrainObject(root)) { continue; }

                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactiveChildren);
                for (int r = 0; r < renderers.Length; r++) {
                    Renderer renderer = renderers[r];
                    if (!CanManageRenderer(renderer)) { continue; }

                    AddManagedRenderer(renderer);

                    if (useGameObjectCulling) {
                        Transform top = GetTopObjectUnderRoot(renderer.transform, root);
                        AddManagedObject(top); } } } }

        if (autoIncludeLooseHeavyObjects) { CollectLooseHeavyObjects(); }

        _objectRoundRobinIndex = 0;
        _rendererRoundRobinIndex = 0; }

    private static Transform GetTopObjectUnderRoot(Transform candidate, Transform root) { if (candidate == null || root == null || candidate == root) { return null; }

        Transform current = candidate;
        while (current != null && current.parent != null && current.parent != root) { current = current.parent; }

        return current != null && current.parent == root ? current : null; }

    private void CollectLooseHeavyObjects() {
        Renderer[] renderers = UnitySceneSearch.FindAll<Renderer>(includeInactiveChildren);
        for (int i = 0; i < renderers.Length; i++) {
            Renderer renderer = renderers[i];
            if (!CanManageRenderer(renderer) || IsUnderManagedRoot(renderer.transform) || IsConvertedTerrainObject(renderer.transform)) { continue; }

            Transform heavyRoot = FindHeavyObjectRoot(renderer.transform);
            if (heavyRoot == null) { continue; }

            AddManagedRenderer(renderer);
            if (useGameObjectCulling) { AddManagedObject(heavyRoot); } } }

    private void AddManagedRenderer(Renderer renderer) {
        if (!CanManageRenderer(renderer) || (useGameObjectCulling && !useRendererCullingFallback)) { return; }

        int id = renderer.GetInstanceID();
        if (_uniqueManagedRendererIds.Contains(id)) { return; }

        _uniqueManagedRendererIds.Add(id);
        _managedRenderers.Add(new ManagedRenderer {
            renderer = renderer,
            initialEnabled = renderer.enabled,
            originalShadows = renderer.shadowCastingMode
        }); }

    private void AddManagedObject(Transform top) {
        if (top == null) { return; }

        int id = top.GetInstanceID();
        if (_uniqueManagedObjectIds.Contains(id)) { return; }

        _uniqueManagedObjectIds.Add(id);
        _managedObjects.Add(new ManagedObject {
            gameObject = top.gameObject,
            initialActive = top.gameObject.activeSelf,
            cachedPosition = top.position
        }); }

    private static bool CanManageRenderer(Renderer renderer) {
        return renderer != null &&
               !(renderer is ParticleSystemRenderer) &&
               !(renderer is LineRenderer) &&
               !(renderer is TrailRenderer); }

    private bool IsUnderManagedRoot(Transform candidate) {
        if (candidate == null || managedRoots == null) { return false; }

        for (int i = 0; i < managedRoots.Length; i++) {
            Transform root = managedRoots[i];
            if (root != null && candidate.IsChildOf(root)) { return true; } }

        return false; }

    private static Transform FindHeavyObjectRoot(Transform candidate) {
        Transform current = candidate;
        while (current != null) {
            if (NameLooksHeavy(current.name) && !NameLooksHeavyContainer(current.name)) { return current; }

            current = current.parent; }

        return null; }

    private static bool NameLooksHeavy(string objectName) {
        if (string.IsNullOrEmpty(objectName)) { return false; }

        return objectName.IndexOf("tree", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("stone", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               objectName.IndexOf("rock", System.StringComparison.OrdinalIgnoreCase) >= 0; }

    private static bool NameLooksHeavyContainer(string objectName) {
        if (string.IsNullOrEmpty(objectName)) { return false; }

        return objectName.IndexOf("ConvertedTerrainTrees", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
               string.Equals(objectName, "Trees", System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(objectName, "Trees&stones", System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(objectName, "Stones", System.StringComparison.OrdinalIgnoreCase) ||
               string.Equals(objectName, "Rocks", System.StringComparison.OrdinalIgnoreCase); }

    private static bool IsConvertedTerrainObject(Transform candidate) {
        Transform current = candidate;
        while (current != null) {
            if (current.name.IndexOf("ConvertedTerrainTrees", System.StringComparison.OrdinalIgnoreCase) >= 0) { return true; }
            current = current.parent; }

        return false; }

    private void RestoreManagedState() {
        for (int i = 0; i < _managedObjects.Count; i++) {
            ManagedObject item = _managedObjects[i];
            if (item.gameObject != null && item.gameObject.activeSelf != item.initialActive) { item.gameObject.SetActive(item.initialActive); } }

        for (int i = 0; i < _managedRenderers.Count; i++) {
            ManagedRenderer item = _managedRenderers[i];
            if (item.renderer == null) { continue; }

            item.renderer.enabled = item.initialEnabled;
            item.renderer.shadowCastingMode = item.originalShadows; }

        for (int i = 0; i < _managedLights.Count; i++) {
            ManagedLight item = _managedLights[i];
            if (item.light == null) { continue; }

            item.light.enabled = item.initialEnabled;
            item.light.shadows = item.originalShadows; } }

    private IEnumerator InitialObjectCullPass() { if (_managedObjects.Count == 0 || targetCamera == null) { yield break; }

        Vector3 camPos = targetCamera.transform.position;
        float renderDistSqr = treeRenderDistance * treeRenderDistance;
        int batchSize = Mathf.Max(128, initialCullBatchSize);

        for (int i = 0; i < _managedObjects.Count; i++) {
            ManagedObject item = _managedObjects[i];
            if (item.gameObject == null || !item.initialActive) { continue; }

            bool shouldBeActive = (item.cachedPosition - camPos).sqrMagnitude <= renderDistSqr;
            if (item.gameObject.activeSelf != shouldBeActive) { item.gameObject.SetActive(shouldBeActive); }

            if ((i + 1) % batchSize == 0) {
                yield return null;
                if (targetCamera != null) { camPos = targetCamera.transform.position; } } } }

    private void ForceRendererCullAllNow() { if (_managedRenderers.Count == 0 || targetCamera == null) { return; }

        Vector3 camPos = targetCamera.transform.position;
        float renderDistSqr = treeRenderDistance * treeRenderDistance;
        float shadowDistSqr = treeShadowDistance * treeShadowDistance;

        for (int i = 0; i < _managedRenderers.Count; i++) {
            ManagedRenderer item = _managedRenderers[i];
            if (item.renderer == null || !item.initialEnabled) { continue; }

            float distSqr = (item.renderer.bounds.center - camPos).sqrMagnitude;
            bool shouldRender = distSqr <= renderDistSqr;
            item.renderer.enabled = shouldRender;

            if (shouldRender) {
                item.renderer.shadowCastingMode = distSqr <= shadowDistSqr
                    ? item.originalShadows
                    : ShadowCastingMode.Off; } } }

    private void ProcessObjectCullingBatch() { if (_managedObjects.Count == 0 || targetCamera == null) { return; }

        Vector3 camPos = targetCamera.transform.position;
        float renderDistSqr = treeRenderDistance * treeRenderDistance;
        int budget = Mathf.Max(1, maxObjectsProcessedPerTick);

        for (int i = 0; i < budget; i++) { if (_objectRoundRobinIndex >= _managedObjects.Count) { _objectRoundRobinIndex = 0; }

            ManagedObject item = _managedObjects[_objectRoundRobinIndex];
            _objectRoundRobinIndex++;

            if (item.gameObject == null || !item.initialActive) { continue; }

            bool shouldBeActive = (item.cachedPosition - camPos).sqrMagnitude <= renderDistSqr;
            if (item.gameObject.activeSelf != shouldBeActive) { item.gameObject.SetActive(shouldBeActive); } } }

    private void ProcessRendererCullingBatch() { if (_managedRenderers.Count == 0 || targetCamera == null) { return; }

        Vector3 camPos = targetCamera.transform.position;
        float renderDistSqr = treeRenderDistance * treeRenderDistance;
        float shadowDistSqr = treeShadowDistance * treeShadowDistance;
        int budget = Mathf.Max(1, maxRenderersProcessedPerTick);

        for (int i = 0; i < budget; i++) { if (_rendererRoundRobinIndex >= _managedRenderers.Count) { _rendererRoundRobinIndex = 0; }

            ManagedRenderer item = _managedRenderers[_rendererRoundRobinIndex];
            _rendererRoundRobinIndex++;

            if (item.renderer == null || !item.initialEnabled) { continue; }

            float distSqr = (item.renderer.bounds.center - camPos).sqrMagnitude;
            bool shouldRender = distSqr <= renderDistSqr;
            if (item.renderer.enabled != shouldRender) { item.renderer.enabled = shouldRender; }

            if (shouldRender) {
                ShadowCastingMode desired = distSqr <= shadowDistSqr
                    ? item.originalShadows
                    : ShadowCastingMode.Off;

                if (item.renderer.shadowCastingMode != desired) { item.renderer.shadowCastingMode = desired; } } } }

    private void CollectLights() {
        _managedLights.Clear();

        Light[] allLights = UnitySceneSearch.FindAll<Light>();
        for (int i = 0; i < allLights.Length; i++) {
            Light light = allLights[i];
            if (light == null) { continue; }

            _managedLights.Add(new ManagedLight {
                light = light,
                initialEnabled = light.enabled,
                originalShadows = light.shadows,
                type = light.type
            }); } }

    private void ProcessLights() {
        if (_managedLights.Count == 0) {
            CollectLights();
            if (_managedLights.Count == 0) { return; } }

        if (targetCamera == null) { return; }

        Vector3 camPos = targetCamera.transform.position;
        float lightDistSqr = nonDirectionalLightDistance * nonDirectionalLightDistance;
        float shadowDistSqr = nonDirectionalShadowDistance * nonDirectionalShadowDistance;
        int shadowBudget = Mathf.Max(0, maxShadowedNonDirectionalLights);
        int shadowedCount = 0;

        for (int i = 0; i < _managedLights.Count; i++) {
            ManagedLight item = _managedLights[i];
            Light light = item.light;
            if (light == null) { continue; }

            if (item.type == LightType.Directional) { continue; }

            if (!item.initialEnabled) { if (disableShadowsOnDisabledLights && light.shadows != LightShadows.None) { light.shadows = LightShadows.None; }
                continue; }

            bool shouldEnable = (light.transform.position - camPos).sqrMagnitude <= lightDistSqr;
            if (light.enabled != shouldEnable) { light.enabled = shouldEnable; }

            if (!shouldEnable) { if (disableShadowsOnDisabledLights && light.shadows != LightShadows.None) { light.shadows = LightShadows.None; }
                continue; }

            bool canCast = item.originalShadows != LightShadows.None &&
                           (light.transform.position - camPos).sqrMagnitude <= shadowDistSqr &&
                           shadowedCount < shadowBudget;

            LightShadows desired = canCast ? item.originalShadows : LightShadows.None;
            if (light.shadows != desired) { light.shadows = desired; }

            if (canCast) { shadowedCount++; } } }

    private void AdaptDistancesFromFps() {
        if (settingsViewDistanceOverrideActive) { return; }

        if (_fpsFrameCount <= 0 || _fpsAccumTime <= 0.0001f) {
            _fpsFrameCount = 0;
            _fpsAccumTime = 0f;
            return; }

        float fps = _fpsFrameCount / _fpsAccumTime;
        _fpsFrameCount = 0;
        _fpsAccumTime = 0f;

        float renderBefore = treeRenderDistance;
        float shadowBefore = treeShadowDistance;

        if (fps < lowFpsThreshold) {
            treeRenderDistance -= adaptiveStep;
            treeShadowDistance -= adaptiveStep * 0.5f;
            terrainTreeDistance -= adaptiveStep;
            terrainDetailDensityScale -= 0.04f;
            nonDirectionalLightDistance -= adaptiveStep * 0.4f;
            nonDirectionalShadowDistance -= adaptiveStep * 0.3f; } else if (fps > highFpsThreshold) {
            treeRenderDistance += adaptiveStep;
            treeShadowDistance += adaptiveStep * 0.5f;
            terrainTreeDistance += adaptiveStep;
            terrainDetailDensityScale += 0.04f;
            nonDirectionalLightDistance += adaptiveStep * 0.4f;
            nonDirectionalShadowDistance += adaptiveStep * 0.3f; }

        treeRenderDistance = Mathf.Clamp(treeRenderDistance, adaptiveMinRenderDistance, adaptiveMaxRenderDistance);
        treeShadowDistance = Mathf.Clamp(treeShadowDistance, adaptiveMinShadowDistance, adaptiveMaxShadowDistance);
        terrainTreeDistance = Mathf.Clamp(terrainTreeDistance, adaptiveMinRenderDistance, adaptiveMaxRenderDistance);
        terrainDetailDensityScale = Mathf.Clamp(terrainDetailDensityScale, 0.18f, 0.35f);
        nonDirectionalLightDistance = Mathf.Clamp(nonDirectionalLightDistance, 24f, 45f);
        nonDirectionalShadowDistance = Mathf.Clamp(nonDirectionalShadowDistance, 6f, adaptiveMaxShadowDistance);

        ClampRuntimeTuning();
        ApplyQualityClamps();

        if (Mathf.Abs(renderBefore - treeRenderDistance) > 0.1f || Mathf.Abs(shadowBefore - treeShadowDistance) > 0.1f) {
            _nextCullingUpdateTime = 0f;
            _nextLightsUpdateTime = 0f; } }

    private void ApplyQualityClamps() {
        QualitySettings.lodBias = settingsViewDistanceOverrideActive ? GameSettings.ViewDistance : qualityLodBias;
        QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance, globalShadowDistance);
        QualitySettings.pixelLightCount = 1;
        QualitySettings.antiAliasing = Mathf.Min(QualitySettings.antiAliasing, 2);
        QualitySettings.shadowCascades = Mathf.Min(QualitySettings.shadowCascades, 1);
        QualitySettings.shadowResolution = ShadowResolution.Low;
        QualitySettings.realtimeReflectionProbes = false;
        QualitySettings.softParticles = false;

        ApplyTerrainDistanceSettings();

        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++) {
            Terrain terrain = terrains[i];
            if (terrain == null) { continue; }

            terrain.heightmapPixelError = Mathf.Max(terrain.heightmapPixelError, 8f);
            terrain.basemapDistance = Mathf.Min(terrain.basemapDistance, 300f); } }

    private void ApplyTerrainDistanceSettings() {
        Terrain[] terrains = Terrain.activeTerrains;
        for (int i = 0; i < terrains.Length; i++) {
            Terrain terrain = terrains[i];
            if (terrain == null) { continue; }

            terrain.detailObjectDensity = Mathf.Clamp01(terrainDetailDensityScale);
            terrain.detailObjectDistance = GameSettings.LockedTerrainDetailDistance;
            terrain.treeDistance = terrainTreeDistance;
            terrain.treeBillboardDistance = terrainBillboardStart; } } }

// Backward compatibility for older components already referencing ForceFullHD.
public class ForceFullHD : ResHandler { }
