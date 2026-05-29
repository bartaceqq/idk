using System.Collections; using System.Collections.Generic; using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class TerrainTreePrefabStreamer : MonoBehaviour {
    private const float BaseActivateRadius = 60f;
    private const float BaseDeactivateRadius = 90f;

    [SerializeField] private Terrain targetTerrain;
    [SerializeField] private Transform convertedRoot;
    [SerializeField] private Transform player;
    [SerializeField] private string convertedRootName = "ConvertedTerrainTrees";

    [Header("Streaming Range")]
    [SerializeField, Min(1f)] private float activateRadius = 60f;
    [SerializeField, Min(1f)] private float deactivateRadius = 90f;
    [SerializeField, Range(0.05f, 2f)] private float updateInterval = 0.22f;
    [SerializeField, Min(16)] private int recordsCheckedPerTick = 1024;
    [SerializeField, Range(0.1f, 2f)] private float terrainRefreshInterval = 1f;

    [Header("Runtime Safety")]
    [SerializeField] private bool cloneTerrainDataInPlayMode = true;
    [SerializeField] private bool includeInactiveConvertedObjects = true;
    [SerializeField] private bool logSummary = true;

    private readonly List<StreamRecord> _records = new List<StreamRecord>(8192);
    private TerrainData _runtimeTerrainData;
    private TreeInstance[] _baseTreeInstances = new TreeInstance[0];
    private bool _initialized;
    private bool _terrainDirty;
    private float _nextUpdateTime;
    private float _nextTerrainRefreshTime;
    private int _scanIndex;
    private float _activateRadiusSqr;
    private float _deactivateRadiusSqr;

    private sealed class StreamRecord {
        public GameObject gameObject;
        public Transform transform;
        public TreeInstance treeInstance;
        public bool prefabActive;
        public bool hiddenByStreamer;
        public bool removed; }

    private IEnumerator Start() {
        if (!Application.isPlaying) { yield break; }

        ApplyViewDistanceMultiplier(GameSettings.ViewDistance);
        yield return null;
        Initialize(); }

    public void ApplyViewDistanceMultiplier(float multiplier) {
        float viewDistance = Mathf.Clamp(multiplier, GameSettings.ViewDistanceMin, GameSettings.ViewDistanceMax);
        activateRadius = Mathf.Max(1f, BaseActivateRadius * viewDistance);
        deactivateRadius = Mathf.Max(activateRadius + 1f, BaseDeactivateRadius * viewDistance);
        RefreshDistanceSquares();

        if (_initialized) {
            _terrainDirty = true;
            _nextUpdateTime = 0f;
            _nextTerrainRefreshTime = 0f; } }

    private void Update() {
        if (!_initialized || targetTerrain == null || player == null) { return; }

        if (Time.time >= _nextUpdateTime) {
            _nextUpdateTime = Time.time + Mathf.Max(0.05f, updateInterval);
            ProcessStreamingBatch(); }

        if (_terrainDirty && Time.time >= _nextTerrainRefreshTime) {
            _nextTerrainRefreshTime = Time.time + Mathf.Max(0.1f, terrainRefreshInterval);
            RebuildTerrainTreeInstances(); } }

    private void OnDisable() {
        if (!Application.isPlaying || !_initialized) { return; }

        for (int i = 0; i < _records.Count; i++) {
            StreamRecord record = _records[i];
            if (record?.gameObject != null && record.hiddenByStreamer) { record.gameObject.SetActive(true); } }

        if (targetTerrain != null && _runtimeTerrainData != null) {
            _runtimeTerrainData.treeInstances = _baseTreeInstances;
            targetTerrain.Flush(); } }

    [ContextMenu("Initialize Streamer")]
    public void Initialize() {
        ResolveReferences();
        if (targetTerrain == null || targetTerrain.terrainData == null || convertedRoot == null || player == null) {
            Debug.LogWarning("TerrainTreePrefabStreamer: missing terrain, converted root, or player.", this);
            return; }

        _runtimeTerrainData = PrepareRuntimeTerrainData(targetTerrain);
        if (_runtimeTerrainData == null) { return; }

        _baseTreeInstances = _runtimeTerrainData.treeInstances ?? new TreeInstance[0];
        RefreshDistanceSquares();
        BuildRecords();
        _initialized = true;
        _terrainDirty = true;
        _nextTerrainRefreshTime = 0f;

        if (logSummary) {
            Debug.Log($"TerrainTreePrefabStreamer: streaming {_records.Count} converted terrain objects.", this); } }

    private void ResolveReferences() {
        if (targetTerrain == null) {
            targetTerrain = GetComponent<Terrain>();
            if (targetTerrain == null) { targetTerrain = UnitySceneSearch.FindFirst<Terrain>(); } }

        if (convertedRoot == null && !string.IsNullOrWhiteSpace(convertedRootName)) {
            GameObject rootObject = GameObject.Find(convertedRootName);
            if (rootObject != null) { convertedRoot = rootObject.transform; } }

        if (player == null) {
            GameObject taggedPlayer = null;
            try { taggedPlayer = GameObject.FindGameObjectWithTag("Player"); } catch (UnityException) { }

            if (taggedPlayer != null) { player = taggedPlayer.transform; }
            else if (Camera.main != null) { player = Camera.main.transform; } } }

    private TerrainData PrepareRuntimeTerrainData(Terrain terrain) {
        TerrainData data = terrain.terrainData;
#if UNITY_EDITOR
        if (Application.isPlaying && cloneTerrainDataInPlayMode) {
            data = Instantiate(data);
            data.name = $"{terrain.terrainData.name}_StreamingRuntime";
            terrain.terrainData = data;
            TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
            if (terrainCollider != null) { terrainCollider.terrainData = data; } }
#endif
        return data; }

    private void BuildRecords() {
        _records.Clear();

        TreePrototype[] prototypes = _runtimeTerrainData.treePrototypes;
        Dictionary<string, int> prototypeByName = BuildPrototypeNameMap(prototypes);
        int childCount = convertedRoot.childCount;

        for (int i = 0; i < childCount; i++) {
            Transform child = convertedRoot.GetChild(i);
            if (child == null || (!includeInactiveConvertedObjects && !child.gameObject.activeSelf)) { continue; }

            int prototypeIndex = ResolvePrototypeIndex(child.gameObject, prototypes, prototypeByName);
            if (prototypeIndex < 0) { continue; }

            if (!TryCreateTreeInstance(child, targetTerrain, prototypes, prototypeIndex, out TreeInstance treeInstance)) { continue; }

            StreamRecord record = new StreamRecord {
                gameObject = child.gameObject,
                transform = child,
                treeInstance = treeInstance,
                prefabActive = child.gameObject.activeSelf,
                hiddenByStreamer = false
            };

            bool shouldBePrefab = IsInsideRadius(child.position, _activateRadiusSqr);
            SetPrefabActive(record, shouldBePrefab);
            _records.Add(record); } }

    private void ProcessStreamingBatch() {
        if (_records.Count == 0) { return; }

        int budget = Mathf.Min(Mathf.Max(16, recordsCheckedPerTick), _records.Count);
        for (int i = 0; i < budget; i++) {
            if (_scanIndex >= _records.Count) { _scanIndex = 0; }

            StreamRecord record = _records[_scanIndex++];
            ProcessRecord(record); } }

    private void ProcessRecord(StreamRecord record) {
        if (record == null || record.removed) { return; }

        if (record.gameObject == null || record.transform == null) {
            record.removed = true;
            _terrainDirty = true;
            return; }

        if (!record.hiddenByStreamer && !record.gameObject.activeSelf) {
            record.removed = true;
            _terrainDirty = true;
            return; }

        if (record.prefabActive) {
            if (!IsInsideRadius(record.transform.position, _deactivateRadiusSqr)) { SetPrefabActive(record, false); } }
        else if (IsInsideRadius(record.transform.position, _activateRadiusSqr)) { SetPrefabActive(record, true); }
        else if (record.gameObject.activeSelf) { SetPrefabActive(record, false); } }

    private void SetPrefabActive(StreamRecord record, bool active) {
        if (record == null || record.gameObject == null) { return; }

        bool changed = record.prefabActive != active || record.hiddenByStreamer == active || record.gameObject.activeSelf != active;
        if (record.gameObject.activeSelf != active) { record.gameObject.SetActive(active); }
        record.prefabActive = active;
        record.hiddenByStreamer = !active;
        if (changed) { _terrainDirty = true; } }

    private void RebuildTerrainTreeInstances() {
        if (_runtimeTerrainData == null) { return; }

        List<TreeInstance> result = new List<TreeInstance>(_baseTreeInstances.Length + _records.Count);
        result.AddRange(_baseTreeInstances);

        for (int i = 0; i < _records.Count; i++) {
            StreamRecord record = _records[i];
            if (record == null || record.removed || record.prefabActive) { continue; }

            result.Add(record.treeInstance); }

        _runtimeTerrainData.treeInstances = result.ToArray();
        targetTerrain.Flush();
        _terrainDirty = false; }

    private bool IsInsideRadius(Vector3 position, float radiusSqr) {
        if (player == null) { return true; }

        Vector3 delta = position - player.position;
        delta.y = 0f;
        return delta.sqrMagnitude <= radiusSqr; }

    private void RefreshDistanceSquares() {
        _activateRadiusSqr = activateRadius * activateRadius;
        float safeDeactivateRadius = Mathf.Max(deactivateRadius, activateRadius);
        _deactivateRadiusSqr = safeDeactivateRadius * safeDeactivateRadius; }

    private static Dictionary<string, int> BuildPrototypeNameMap(TreePrototype[] prototypes) {
        Dictionary<string, int> map = new Dictionary<string, int>();
        if (prototypes == null) { return map; }

        for (int i = 0; i < prototypes.Length; i++) {
            GameObject prefab = prototypes[i]?.prefab;
            if (prefab == null) { continue; }

            string key = NormalizeName(prefab.name);
            if (!map.ContainsKey(key)) { map.Add(key, i); } }

        return map; }

    private static int ResolvePrototypeIndex(GameObject candidate, TreePrototype[] prototypes, Dictionary<string, int> prototypeByName) {
        if (candidate == null || prototypes == null) { return -1; }

#if UNITY_EDITOR
        GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(candidate);
        if (source != null) {
            for (int i = 0; i < prototypes.Length; i++) {
                if (prototypes[i] != null && prototypes[i].prefab == source) { return i; } } }
#endif

        string normalized = NormalizeName(candidate.name);
        return prototypeByName.TryGetValue(normalized, out int mappedIndex) ? mappedIndex : -1; }

    private static string NormalizeName(string rawName) {
        if (string.IsNullOrWhiteSpace(rawName)) { return string.Empty; }

        string name = rawName.Trim().ToLowerInvariant();
        name = name.Replace("(clone)", string.Empty).Trim();
        name = name.Replace(" variant", string.Empty).Trim();
        return name; }

    private static bool TryCreateTreeInstance(
        Transform source,
        Terrain terrain,
        TreePrototype[] prototypes,
        int prototypeIndex,
        out TreeInstance treeInstance) {
        treeInstance = default;
        if (source == null || terrain == null || terrain.terrainData == null || prototypes == null ||
            prototypeIndex < 0 || prototypeIndex >= prototypes.Length) { return false; }

        TerrainData data = terrain.terrainData;
        Vector3 local = source.position - terrain.transform.position;
        Vector3 normalized = new Vector3(
            data.size.x > 0f ? local.x / data.size.x : 0f,
            data.size.y > 0f ? local.y / data.size.y : 0f,
            data.size.z > 0f ? local.z / data.size.z : 0f);

        if (normalized.x < 0f || normalized.x > 1f || normalized.z < 0f || normalized.z > 1f) { return false; }

        Vector3 sourceScale = source.localScale;
        Vector3 prefabScale = prototypes[prototypeIndex]?.prefab != null
            ? prototypes[prototypeIndex].prefab.transform.localScale
            : Vector3.one;
        float widthScaleX = Mathf.Abs(prefabScale.x) > 0.0001f ? sourceScale.x / prefabScale.x : 1f;
        float widthScaleZ = Mathf.Abs(prefabScale.z) > 0.0001f ? sourceScale.z / prefabScale.z : widthScaleX;
        float widthScale = Mathf.Max(0.01f, (widthScaleX + widthScaleZ) * 0.5f);
        float heightScale = Mathf.Abs(prefabScale.y) > 0.0001f ? Mathf.Max(0.01f, sourceScale.y / prefabScale.y) : 1f;

        treeInstance = new TreeInstance {
            position = new Vector3(Mathf.Clamp01(normalized.x), Mathf.Clamp01(normalized.y), Mathf.Clamp01(normalized.z)),
            widthScale = widthScale,
            heightScale = heightScale,
            rotation = source.eulerAngles.y * Mathf.Deg2Rad,
            prototypeIndex = prototypeIndex,
            color = Color.white,
            lightmapColor = Color.white
        };
        return true; }
}
