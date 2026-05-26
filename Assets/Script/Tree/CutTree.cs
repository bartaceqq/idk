using System.Collections; using System.Collections.Generic; using UnityEngine;
public class CutTree : MonoBehaviour {
    private const string DestructionVfxResourcePath = "VFX_TreeDestructionBurst";

    private static InfoHandler cachedInfoHandler;
    private static SlotManager cachedSlotManager;
    private static GameObject cachedDestructionVfxPrefab;

    public string texttoshow;
    public Sprite sprite;
    public InfoHandler infoHandler;
    public List<GameObject> treeparts = new List<GameObject>();
    public GameObject topofthetree;
    [SerializeField] private float destroyDelaySeconds = 1f;
    [SerializeField] private float rebuildDelaySeconds = 30f;
    [SerializeField, Min(0f)] private float fallTiltDegrees = 15f;
    [SerializeField] private GameObject destructionVfxPrefab;
    [SerializeField] private Vector3 destructionVfxOffset = new Vector3(0f, 0.15f, 0f);
    public InventoryItem inventoryItem;
    public bool broken = false;
    private readonly List<GameObject> initialTreeParts = new List<GameObject>();
    private readonly Dictionary<Transform, TransformSnapshot> initialTransforms = new Dictionary<Transform, TransformSnapshot>();
    private Rigidbody topRigidbody;
    private MeshCollider topMeshCollider;
    private bool topInitialUseGravity;
    private bool topInitialIsKinematic;
    private bool topInitialConvex;
    private bool topInitialProvidesContacts;
    private bool isRebuilding;
    private bool topHadInitialRigidbody;

    
    private struct TransformSnapshot {
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale; }
    private void SpawnDestructionVfx() {
        GameObject vfxPrefab = ResolveDestructionVfxPrefab();
        if (vfxPrefab == null) { return; }

        Instantiate(vfxPrefab, ResolveDestructionVfxPosition(), Quaternion.identity); }
    private GameObject ResolveDestructionVfxPrefab() { if (destructionVfxPrefab != null) { return destructionVfxPrefab; }

        if (cachedDestructionVfxPrefab == null) { cachedDestructionVfxPrefab = Resources.Load<GameObject>(DestructionVfxResourcePath); }

        return cachedDestructionVfxPrefab; }
    private Vector3 ResolveDestructionVfxPosition() {
        if (TryGetPreferredVfxBounds(topofthetree, out Bounds bounds)) {
            Vector3 position = bounds.center;
            position.y = bounds.min.y + Mathf.Min(bounds.size.y * 0.2f, 1.5f);
            return position + transform.TransformVector(destructionVfxOffset); }

        return transform.position + transform.TransformVector(destructionVfxOffset); }
    private static bool TryGetPreferredVfxBounds(GameObject source, out Bounds bounds) {
        bounds = default;
        if (source == null) { return false; }

        Renderer[] renderers = source.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++) {
            Renderer currentRenderer = renderers[i];
            if (currentRenderer == null) { continue; }

            if (bounds.size == Vector3.zero) {
                bounds = currentRenderer.bounds;
                continue; }

            bounds.Encapsulate(currentRenderer.bounds); }

        return bounds.size != Vector3.zero; }

    // Initialize references before gameplay starts.
    private void Awake() {
        ResolveReferences();
        CacheInitialState(); }

    // Run in the editor when values change in Inspector.
    private void OnValidate() { if (!Application.isPlaying) { ResolveReferences(); } }
    private void ResolveReferences() { if (inventoryItem == null) { inventoryItem = GetComponent<InventoryItem>(); }

        if (infoHandler == null) { if (cachedInfoHandler == null) { cachedInfoHandler = FindInfoHandlerInScene(); }

            infoHandler = cachedInfoHandler; } else { cachedInfoHandler = infoHandler; }

        if (inventoryItem == null) { return; }

        inventoryItem.ResolveReferences();

        if (inventoryItem.slotManager == null) { if (cachedSlotManager == null) { cachedSlotManager = FindSlotManagerInScene(); }

            inventoryItem.slotManager = cachedSlotManager; } else { cachedSlotManager = inventoryItem.slotManager; } }
    private static InfoHandler FindInfoHandlerInScene() {
        return UnitySceneSearch.FindFirst<InfoHandler>();
    }
    private static SlotManager FindSlotManagerInScene() {
        return UnitySceneSearch.FindFirst<SlotManager>();
    }
    private void CacheInitialState() {
        initialTreeParts.Clear();
        initialTransforms.Clear();

        foreach (GameObject treePart in treeparts) { if (treePart == null) { continue; }

            initialTreeParts.Add(treePart);
            CacheTransform(treePart.transform); }

        if (topofthetree != null) {
            CacheTransform(topofthetree.transform);
            topRigidbody = topofthetree.GetComponent<Rigidbody>();
            topMeshCollider = topofthetree.GetComponent<MeshCollider>();
            topHadInitialRigidbody = topRigidbody != null;

            if (topRigidbody != null) {
                topInitialUseGravity = topRigidbody.useGravity;
                topInitialIsKinematic = topRigidbody.isKinematic; }

            if (topMeshCollider != null) {
                topInitialConvex = topMeshCollider.convex;
                topInitialProvidesContacts = topMeshCollider.providesContacts; } }

        treeparts.Clear();
        treeparts.AddRange(initialTreeParts); }
    private void CacheTransform(Transform targetTransform) { if (targetTransform == null || initialTransforms.ContainsKey(targetTransform)) { return; }

        initialTransforms[targetTransform] = new TransformSnapshot {
            localPosition = targetTransform.localPosition,
            localRotation = targetTransform.localRotation,
            localScale = targetTransform.localScale }; }
    private void RestoreTransform(Transform targetTransform) { if (targetTransform == null || !initialTransforms.TryGetValue(targetTransform, out TransformSnapshot snapshot)) { return; }

        targetTransform.localPosition = snapshot.localPosition;
        targetTransform.localRotation = snapshot.localRotation;
        targetTransform.localScale = snapshot.localScale; }
    private static void SetTreePartVisible(GameObject treePart, bool visible) { if (treePart == null) { return; }

        Renderer[] renderers = treePart.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++) { if (renderers[i] != null) { renderers[i].enabled = visible; } } }
    private IEnumerator SetActiveAfterSeconds(GameObject target, float delaySeconds, bool active) {
        yield return new WaitForSeconds(delaySeconds);
        if (target != null) { target.SetActive(active); } }
    private IEnumerator RebuildTreeAfterSeconds(float delaySeconds) {
        yield return new WaitForSeconds(delaySeconds);
        RebuildTree(); }
    private void RebuildTree() {
        broken = false;
        isRebuilding = false;

        treeparts.Clear();
        treeparts.AddRange(initialTreeParts);

        foreach (GameObject treePart in initialTreeParts) { if (treePart == null) { continue; }

            RestoreTransform(treePart.transform);
            treePart.SetActive(true);
            SetTreePartVisible(treePart, true); }

        if (topofthetree != null) {
            RestoreTransform(topofthetree.transform);
            topofthetree.SetActive(true); }

        if (topRigidbody != null) {
            topRigidbody.linearVelocity = Vector3.zero;
            topRigidbody.angularVelocity = Vector3.zero;
            if (topHadInitialRigidbody) {
                topRigidbody.useGravity = topInitialUseGravity;
                topRigidbody.isKinematic = topInitialIsKinematic; } else {
                Destroy(topRigidbody);
                topRigidbody = null; } }

        if (topMeshCollider != null) {
            topMeshCollider.convex = topInitialConvex;
            topMeshCollider.providesContacts = topInitialProvidesContacts; } }
    public void CutPart() {
        ResolveReferences();

        if (broken || isRebuilding) { return; }

        if (treeparts.Count == 0) {
            SpawnDestructionVfx();

            if (topofthetree != null) {
                float tiltDegrees = Mathf.Max(15f, fallTiltDegrees);
                if (tiltDegrees > 0f) { topofthetree.transform.Rotate(Vector3.right, tiltDegrees, Space.Self); } }

            if (topMeshCollider != null) {
                topMeshCollider.convex = true;
                topMeshCollider.providesContacts = true; }

            if (topRigidbody == null && topofthetree != null) {
                topRigidbody = topofthetree.GetComponent<Rigidbody>();
                if (topRigidbody == null) { topRigidbody = topofthetree.AddComponent<Rigidbody>(); } }

            if (topRigidbody != null) {
                topRigidbody.useGravity = true;
                topRigidbody.isKinematic = false;
                topRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                topRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; }

            StartCoroutine(SetActiveAfterSeconds(topofthetree, destroyDelaySeconds, false));
            if (inventoryItem != null && inventoryItem.slotManager != null) {
                inventoryItem.slotManager.AddItem(inventoryItem);
                
            } else { Debug.LogWarning($"{name}: Missing InventoryItem or SlotManager reference.", this); }

            if (infoHandler != null) { infoHandler.ShowInfoNow(texttoshow, sprite); } else { Debug.LogWarning($"{name}: Missing InfoHandler reference.", this); }

            broken = true;
            isRebuilding = true;
            StartCoroutine(RebuildTreeAfterSeconds(destroyDelaySeconds + rebuildDelaySeconds)); } else {
            GameObject treepart = treeparts[treeparts.Count - 1];
            if (treepart == null) {
                treeparts.RemoveAt(treeparts.Count - 1);
                return; }

            SetTreePartVisible(treepart, false);
            StartCoroutine(SetActiveAfterSeconds(treepart, destroyDelaySeconds, false));
            treeparts.RemoveAt(treeparts.Count - 1); } } }
