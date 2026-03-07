using System.Collections.Generic;
using UnityEngine;

// Controls Ray Cast Script Test behavior.
public class RayCastScriptTest : MonoBehaviour
{
    private enum BuildType { Wall, Floor, Stair }
    private static readonly Vector3 CenterViewportPoint = new Vector3(0.5f, 0.5f, 0f);
    private const float TinyValue = 0.000001f;
    private const float MinSize = 0.01f;
    private const float MinTolerance = 0.001f;
    private const float RotationToleranceDegrees = 1f;

    [Header("Placement")]
    public Camera camera;
    public LayerMask hitMask = ~0;
    public float range = 100f;
    public GameObject wall;
    public GameObject floor;
    public GameObject stair;

    [Header("Raycast Filtering")]
    public bool ignorePlayerColliderInBuildRaycast = true;
    public string playerTag = "Player";
    public Transform playerRoot;

    [Header("Input")]
    public KeyCode rotateKey = KeyCode.R;
    public KeyCode destroyModeKey = KeyCode.X;

    [Header("Rotation")]
    public float rotateStep = 90f;

    [Header("Visuals")]
    public Material buildingMaterial;
    public Material doneBuildMaterial;
    public Material destroyBuildMaterial;

    [Header("Placed Build Visuals")]
    public bool usePrefabMaterialsOnPlacedObjects = true;

    [Header("Build Capsule")]
    public bool autoSwitchToBuildingCapsule = true;
    public LookingController lookingController;
    public bool requireBuildingCapsuleForBuildControls = true;

    [Header("Table Right Click")]
    public bool enableTableRightClickShortcut = true;
    public string tableTag = "Table";
    public string tableNameContains = "table";
    public GameObject tableRightClickBuildPrefab;
    public string tableRightClickBuildName = "Capsule";
    public Vector3 tableBuildRotationEuler = Vector3.zero;
    public Vector3 tableBuildScale = Vector3.one;

    [Header("Snapping")]
    public bool enableTwoPointSnap = true;
    public float snapEngageDistance = 0.8f;
    public float snapReleaseDistance = 1.2f;
    public float snapYOffset = 0f;
    public bool alignRotationFromPairs = true;
    public float pairLengthTolerance = 0.15f;

    [Header("Floor Height Assist")]
    public bool floorPreferNearestPointY = true;
    public Transform floorHeightReference;
    public float floorNearestPointRadius = 8f;
    public bool floorNearestPointUseXZ = true;

    [Header("Extrude")]
    public KeyCode extrudeKey = KeyCode.G;
    public bool extrudeModeStaysOn = true;
    public bool wallExtrudeUseFloorSpacing = true;
    public float wallExtrudeSpacingScale = 1f;
    public bool extrudePreventDuplicates = true;
    public float wallExtrudeOccupyToleranceXZ = 0.02f;
    public float floorExtrudeOccupyToleranceXZ = 0.05f;
    public float stairExtrudeOccupyToleranceXZ = 0.05f;
    public float extrudeOccupyToleranceY = 0.05f;

    [Header("Debug")]
    public bool logClosestPair = true;
    public bool logDetectionState = true;
    public float logDistanceDelta = 0.005f;
    public float logInterval = 0.15f;
    public Color debugLineColorA = Color.red;
    public Color debugLineColorB = Color.yellow;

    private GameObject _previewObject;
    private float _previewYRotation;
    private float _bottomOffset;
    private float _lastLoggedDistanceA = -1f;
    private float _lastLoggedDistanceB = -1f;
    private float _lastLogTime = -10f;
    private BuildType _buildType = BuildType.Wall;
    private GameObject _inventoryBuildPrefab;
    private string _inventoryBuildItemName = string.Empty;
    private Vector3 _inventoryBuildRotationEuler = Vector3.zero;
    private Vector3 _inventoryBuildScale = Vector3.one;
    private readonly List<BuildType> _availableBuildTypes = new List<BuildType>(3);

    private bool _isSnapLocked;
    private SnapPoint _lockedPreviewA;
    private SnapPoint _lockedPreviewB;
    private SnapPoint _lockedTargetA;
    private SnapPoint _lockedTargetB;
    private Vector3 _lockedPreviewOffsetA;
    private Vector3 _lockedPreviewOffsetB;

    private bool _isExtrudeMode;
    private bool _isExtruding;
    private BuildType _extrudeBuildType;
    private GameObject _extrudePrefab;
    private Vector3 _extrudeStartPosition;
    private Quaternion _extrudeStartRotation;
    private Vector3 _extrudeRight;
    private Vector3 _extrudeForward;
    private Vector2 _extrudeCellSize = Vector2.one;
    private Vector2Int _extrudeCellOffset;
    private readonly Dictionary<Vector2Int, GameObject> _extrudeGhostByCell = new Dictionary<Vector2Int, GameObject>(64);
    private readonly RaycastHit[] _placementHits = new RaycastHit[64];
    private readonly List<MeshRenderer> _destroyHighlightedRenderers = new List<MeshRenderer>(16);
    private readonly List<Material[]> _destroyOriginalMaterials = new List<Material[]>(16);
    private GameObject _destroyTarget;
    private bool _isDestroyMode;

    private readonly List<PairCandidate> _candidates = new List<PairCandidate>(64);

    private struct PairCandidate
    {
        public SnapPoint preview;
        public SnapPoint target;
        public Vector3 previewWorld;
        public Vector3 targetWorld;
        public float distance;
    }

    // Run setup once before the first frame.
    private void Start()
    {
        EnsureCamera();
        InitializeBuildType();
        CreatePreviewObject();
    }

    // Run this logic every frame.
    private void Update()
    {
        if (IsUiBlockingGameplay())
        {
            HidePreviewAndCancelBuildInteraction();
            return;
        }

        if (!CanUseBuildControls())
        {
            HidePreviewAndCancelBuildInteraction();
            return;
        }

        // 1) Handle global mode input, 2) update preview, 3) place/extrude.
        if (!EnsureCamera()) return;

        HandleDestroyModeInput();
        if (_isDestroyMode)
        {
            HandleDestroyMode();
            return;
        }

        if (!TryPrepareBuildPreview(out GameObject activePrefab))
        {
            return;
        }

        HandleBuildTypeCycleInput(ref activePrefab);
        if (activePrefab == null)
        {
            return;
        }

        HandleExtrudeAndRotationInput();
        if (_isExtruding)
        {
            HandleExtrudeDrag();
            return;
        }

        MovePreviewObject();
        if (_isExtrudeMode)
        {
            HandleExtrudeIdleInput();
            return;
        }

        TryPlaceSingleObject(activePrefab);
    }

    // Handle Ensure Camera.
    private bool EnsureCamera()
    {
        if (camera == null)
        {
            camera = Camera.main;
        }

        return camera != null;
    }

    // Handle Can Use Build Controls.
    private bool CanUseBuildControls()
    {
        if (!requireBuildingCapsuleForBuildControls)
        {
            return true;
        }

        LookingController controller = ResolveLookingController();
        if (controller == null)
        {
            return true;
        }

        return controller.switched;
    }

    // Handle Resolve Looking Controller.
    private LookingController ResolveLookingController()
    {
        if (lookingController != null)
        {
            return lookingController;
        }

#if UNITY_2023_1_OR_NEWER
        lookingController = FindFirstObjectByType<LookingController>(FindObjectsInactive.Include);
#else
        lookingController = FindObjectOfType<LookingController>(true);
#endif
        return lookingController;
    }

    // Handle Hide Preview And Cancel Build Interaction.
    private void HidePreviewAndCancelBuildInteraction()
    {
        if (_isDestroyMode)
        {
            _isDestroyMode = false;
            ClearDestroyTargetHighlight();
        }

        _isExtrudeMode = false;
        if (_isExtruding)
        {
            CancelExtrudeState();
        }
        else
        {
            ClearExtrudeGhosts();
        }

        if (_previewObject != null && _previewObject.activeSelf)
        {
            _previewObject.SetActive(false);
        }

        ClearSnapLock();
    }

    // Handle Handle Destroy Mode Input.
    private void HandleDestroyModeInput()
    {
        if (Input.GetKeyDown(destroyModeKey))
        {
            ToggleDestroyMode();
        }
    }

    // Handle Try Select Inventory Building Prefab.
    public bool TrySelectInventoryBuildingPrefab(GameObject prefab, string sourceItemName)
    {
        return TrySelectBuildPrefabDirect(
            prefab,
            sourceItemName,
            Vector3.zero,
            Vector3.one,
            autoSwitchToBuildingCapsule);
    }

    // Handle Try Select Inventory Building Item.
    public bool TrySelectInventoryBuildingItem(InventoryItem inventoryItem)
    {
        if (inventoryItem == null || inventoryItem.itemPrefab == null)
        {
            return false;
        }

        return TrySelectBuildPrefabDirect(
            inventoryItem.itemPrefab,
            inventoryItem.name,
            inventoryItem.buildRotationEuler,
            inventoryItem.buildScale,
            autoSwitchToBuildingCapsule);
    }

    // Handle Try Select Build Prefab Direct.
    private bool TrySelectBuildPrefabDirect(
        GameObject prefab,
        string sourceItemName,
        Vector3 buildRotationEuler,
        Vector3 buildScale,
        bool switchToBuildingCapsule)
    {
        if (prefab == null)
        {
            return false;
        }

        _inventoryBuildPrefab = prefab;
        _inventoryBuildItemName = string.IsNullOrWhiteSpace(sourceItemName) ? prefab.name : sourceItemName.Trim();
        _inventoryBuildRotationEuler = buildRotationEuler;
        _inventoryBuildScale = SanitizeScale(buildScale);
        _buildType = ResolveBuildTypeForPrefab(prefab, _buildType);

        if (switchToBuildingCapsule)
        {
            EnsureBuildingCapsuleActive();
        }

        if (_isDestroyMode)
        {
            ToggleDestroyMode();
        }

        _isExtrudeMode = false;
        CancelExtrudeState();
        CreatePreviewObject();

        if (_previewObject == null)
        {
            return false;
        }

        _previewObject.SetActive(true);
        LogDetectionState("Build mode: " + _inventoryBuildItemName);
        return true;
    }

    // Handle Try Prepare Build Preview.
    private bool TryPrepareBuildPreview(out GameObject activePrefab)
    {
        // We always need both: selected prefab + preview instance in scene.
        activePrefab = GetActivePrefab();
        if (activePrefab == null)
        {
            return false;
        }

        if (_previewObject != null)
        {
            if (!_previewObject.activeSelf)
            {
                _previewObject.SetActive(true);
            }

            return true;
        }

        CreatePreviewObject();
        return _previewObject != null;
    }

    // Handle Handle Build Type Cycle Input.
    private void HandleBuildTypeCycleInput(ref GameObject activePrefab)
    {
        if (!Input.GetMouseButtonDown(1))
        {
            return;
        }

        if (TryHandleTableRightClickShortcut(ref activePrefab))
        {
            return;
        }

        if (_isExtruding)
        {
            CancelExtrudeState();
        }

        if (_inventoryBuildPrefab != null)
        {
            ClearInventoryBuildSelection();
        }

        ToggleBuildType();
        activePrefab = GetActivePrefab();
    }

    // Handle Handle Extrude And Rotation Input.
    private void HandleExtrudeAndRotationInput()
    {
        if (Input.GetKeyDown(extrudeKey))
        {
            ToggleExtrudeMode();
        }

        if (!_isExtruding && Input.GetKeyDown(rotateKey))
        {
            _previewYRotation += rotateStep;
        }
    }

    // Handle Try Place Single Object.
    private void TryPlaceSingleObject(GameObject activePrefab)
    {
        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        GameObject created = Instantiate(activePrefab, _previewObject.transform.position, _previewObject.transform.rotation);
        ApplyScaleMultiplier(created, GetActiveBuildScale());
        ApplyPlacedObjectVisuals(created);
    }

    // Handle Initialize Build Type.
    private void InitializeBuildType()
    {
        RefreshAvailableBuildTypes();
        if (_availableBuildTypes.Count > 0) _buildType = _availableBuildTypes[0];
    }

    // Handle Get Active Prefab.
    private GameObject GetActivePrefab()
    {
        if (_inventoryBuildPrefab != null)
        {
            return _inventoryBuildPrefab;
        }

        return _buildType switch
        {
            BuildType.Wall => wall,
            BuildType.Floor => floor,
            BuildType.Stair => stair,
            _ => null,
        };
    }

    // Handle Toggle Build Type.
    private void ToggleBuildType()
    {
        RefreshAvailableBuildTypes();
        if (_availableBuildTypes.Count < 2)
        {
            LogDetectionState("Assign at least 2 prefabs (wall/floor/stair) to switch build mode.");
            return;
        }

        int currentIndex = _availableBuildTypes.IndexOf(_buildType);
        if (currentIndex < 0) currentIndex = 0;
        _buildType = _availableBuildTypes[(currentIndex + 1) % _availableBuildTypes.Count];

        CreatePreviewObject();
        LogDetectionState("Build mode: " + _buildType);
    }

    // Handle Clear Inventory Build Selection.
    private void ClearInventoryBuildSelection()
    {
        _inventoryBuildPrefab = null;
        _inventoryBuildItemName = string.Empty;
        _inventoryBuildRotationEuler = Vector3.zero;
        _inventoryBuildScale = Vector3.one;
    }

    // Handle Resolve Build Type For Prefab.
    private static BuildType ResolveBuildTypeForPrefab(GameObject prefab, BuildType fallback)
    {
        if (prefab == null)
        {
            return fallback;
        }

        if (prefab.GetComponentInChildren<FloorScript>(true) != null)
        {
            return BuildType.Floor;
        }

        if (prefab.GetComponentInChildren<StairScript>(true) != null)
        {
            return BuildType.Stair;
        }

        if (prefab.GetComponentInChildren<WallSnapPoints>(true) != null)
        {
            return BuildType.Wall;
        }

        return fallback;
    }

    // Handle Try Handle Table Right Click Shortcut.
    private bool TryHandleTableRightClickShortcut(ref GameObject activePrefab)
    {
        if (!enableTableRightClickShortcut)
        {
            return false;
        }

        Ray ray = GetCenterRay();
        if (!TryGetClosestTableHit(ray, out _))
        {
            return false;
        }

        bool changed = false;
        if (autoSwitchToBuildingCapsule)
        {
            EnsureBuildingCapsuleActive();
            changed = true;
        }

        if (tableRightClickBuildPrefab != null)
        {
            changed |= TrySelectBuildPrefabDirect(
                tableRightClickBuildPrefab,
                tableRightClickBuildName,
                tableBuildRotationEuler,
                tableBuildScale,
                false);
            activePrefab = GetActivePrefab();
        }

        return changed;
    }

    // Handle Try Get Closest Table Hit.
    private bool TryGetClosestTableHit(Ray ray, out RaycastHit closestTableHit)
    {
        closestTableHit = default;
        int hitCount = Physics.RaycastNonAlloc(ray, _placementHits, range, hitMask, QueryTriggerInteraction.Ignore);
        if (hitCount <= 0)
        {
            return false;
        }

        float closestDistance = float.MaxValue;
        bool found = false;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _placementHits[i];
            Collider collider = hit.collider;
            if (ShouldSkipPlacementCollider(collider))
            {
                continue;
            }

            if (!IsTableTransform(collider.transform))
            {
                continue;
            }

            if (hit.distance >= closestDistance)
            {
                continue;
            }

            closestDistance = hit.distance;
            closestTableHit = hit;
            found = true;
        }

        return found;
    }

    // Handle Is Table Transform.
    private bool IsTableTransform(Transform startTransform)
    {
        if (startTransform == null)
        {
            return false;
        }

        string contains = string.IsNullOrWhiteSpace(tableNameContains) ? string.Empty : tableNameContains.Trim();
        bool hasTagFilter = !string.IsNullOrWhiteSpace(tableTag);
        bool hasNameFilter = !string.IsNullOrEmpty(contains);

        Transform current = startTransform;
        while (current != null)
        {
            if (hasTagFilter && current.CompareTag(tableTag))
            {
                return true;
            }

            if (hasNameFilter && current.name.IndexOf(contains, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    // Handle Refresh Available Build Types.
    private void RefreshAvailableBuildTypes()
    {
        _availableBuildTypes.Clear();
        if (wall != null) _availableBuildTypes.Add(BuildType.Wall);
        if (floor != null) _availableBuildTypes.Add(BuildType.Floor);
        if (stair != null) _availableBuildTypes.Add(BuildType.Stair);
    }

    // Handle Create Preview Object.
    private void CreatePreviewObject()
    {
        GameObject activePrefab = GetActivePrefab();
        if (activePrefab == null) return;

        if (_previewObject != null) Destroy(_previewObject);

        _previewObject = Instantiate(activePrefab, Vector3.zero, Quaternion.identity);
        ApplyScaleMultiplier(_previewObject, GetActiveBuildScale());
        ApplyMaterialToMeshRenderers(_previewObject, buildingMaterial);
        _previewYRotation = _previewObject.transform.eulerAngles.y;
        SetPreviewMode(_previewObject, true);
        _bottomOffset = GetBottomOffset(_previewObject);
        ClearSnapLock();
        CancelExtrudeState();
    }

    // Handle Move Preview Object.
    private void MovePreviewObject()
    {
        Ray ray = GetCenterRay();
        if (!TryGetPlacementHit(ray, out RaycastHit hit)) return;

        Vector3 rawPosition = GetRawPlacementPosition(hit);
        Quaternion baseRotation = Quaternion.Euler(0f, _previewYRotation, 0f);
        Quaternion placementRotation = ApplyBuildRotationOffset(baseRotation);
        _previewObject.transform.SetPositionAndRotation(rawPosition, placementRotation);

        if (!enableTwoPointSnap)
        {
            if (_isSnapLocked) ClearSnapLock();
            return;
        }

        HandleStickyTwoPointSnap(rawPosition, placementRotation);
    }

    // Handle Get Raw Placement Position.
    private Vector3 GetRawPlacementPosition(RaycastHit hit)
    {
        Vector3 rawPosition = hit.point;

        if (_buildType == BuildType.Floor)
        {
            rawPosition.y = GetFloorPlacementY(hit);
        }

        rawPosition.y += _bottomOffset;
        return rawPosition;
    }

    // Handle Get Center Ray.
    private Ray GetCenterRay()
    {
        return camera.ViewportPointToRay(CenterViewportPoint);
    }

    // Handle Try Get Placement Hit.
    private bool TryGetPlacementHit(Ray ray, out RaycastHit closestHit)
    {
        closestHit = default;
        int hitCount = Physics.RaycastNonAlloc(ray, _placementHits, range, hitMask, QueryTriggerInteraction.Ignore);
        if (hitCount <= 0)
        {
            return false;
        }

        float closestDistance = float.MaxValue;
        bool found = false;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit candidate = _placementHits[i];
            Collider collider = candidate.collider;
            if (ShouldSkipPlacementCollider(collider))
            {
                continue;
            }

            if (candidate.distance >= closestDistance)
            {
                continue;
            }

            closestDistance = candidate.distance;
            closestHit = candidate;
            found = true;
        }

        return found;
    }

    // Handle Should Skip Placement Collider.
    private bool ShouldSkipPlacementCollider(Collider collider)
    {
        if (collider == null)
        {
            return true;
        }

        if (IsSnapMarkerTransform(collider.transform))
        {
            return true;
        }

        if (_previewObject != null && collider.transform.IsChildOf(_previewObject.transform))
        {
            return true;
        }

        return IsPlayerCollider(collider);
    }

    // Handle Is Player Collider.
    private bool IsPlayerCollider(Collider collider)
    {
        if (!ignorePlayerColliderInBuildRaycast || collider == null)
        {
            return false;
        }

        Transform colliderTransform = collider.transform;
        if (playerRoot != null && colliderTransform.IsChildOf(playerRoot))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(playerTag) &&
            string.Equals(collider.tag, playerTag, System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (camera != null && camera.transform != null)
        {
            Transform cameraRoot = camera.transform.root;
            if (cameraRoot != null && colliderTransform.IsChildOf(cameraRoot))
            {
                return true;
            }
        }

        return false;
    }

    // Handle Apply Build Rotation Offset.
    private Quaternion ApplyBuildRotationOffset(Quaternion baseRotation)
    {
        Vector3 rotationOffset = GetActiveBuildRotationEuler();
        if (rotationOffset.sqrMagnitude < TinyValue)
        {
            return baseRotation;
        }

        return baseRotation * Quaternion.Euler(rotationOffset);
    }

    // Handle Get Active Build Rotation Euler.
    private Vector3 GetActiveBuildRotationEuler()
    {
        if (_inventoryBuildPrefab != null)
        {
            return _inventoryBuildRotationEuler;
        }

        return Vector3.zero;
    }

    // Handle Get Active Build Scale.
    private Vector3 GetActiveBuildScale()
    {
        if (_inventoryBuildPrefab != null)
        {
            return _inventoryBuildScale;
        }

        return Vector3.one;
    }

    // Handle Sanitize Scale.
    private static Vector3 SanitizeScale(Vector3 scale)
    {
        scale.x = Mathf.Abs(scale.x) < TinyValue ? 1f : scale.x;
        scale.y = Mathf.Abs(scale.y) < TinyValue ? 1f : scale.y;
        scale.z = Mathf.Abs(scale.z) < TinyValue ? 1f : scale.z;
        return scale;
    }

    // Handle Apply Scale Multiplier.
    private static void ApplyScaleMultiplier(GameObject target, Vector3 scaleMultiplier)
    {
        if (target == null)
        {
            return;
        }

        Vector3 cleanScale = SanitizeScale(scaleMultiplier);
        target.transform.localScale = Vector3.Scale(target.transform.localScale, cleanScale);
    }

    // Handle Apply Placed Object Visuals.
    private void ApplyPlacedObjectVisuals(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (usePrefabMaterialsOnPlacedObjects)
        {
            return;
        }

        ApplyMaterialToMeshRenderers(target, doneBuildMaterial);
    }

    // Handle Ensure Building Capsule Active.
    private void EnsureBuildingCapsuleActive()
    {
        if (!autoSwitchToBuildingCapsule)
        {
            return;
        }

        LookingController controller = ResolveLookingController();
        if (controller == null)
        {
            return;
        }

        controller.SwitchToBuildingMode();
    }

    // Handle On Disable.
    private void OnDisable()
    {
        HidePreviewAndCancelBuildInteraction();
    }

    // Handle On Destroy.
    private void OnDestroy()
    {
        ClearDestroyTargetHighlight();
        ClearExtrudeGhosts();

        if (_previewObject != null)
        {
            Destroy(_previewObject);
            _previewObject = null;
        }
    }

    // Handle Get Floor Placement Y.
    private float GetFloorPlacementY(RaycastHit hit)
    {
        float targetY = hit.point.y;

        // If we are aiming at a built object, snap to that object's level first.
        if (TryGetSnapOwnerFromCollider(hit.collider, out GameObject owner))
        {
            if (owner.TryGetComponent(out FloorScript _))
            {
                return GetBottomY(owner);
            }

            return GetTopY(owner);
        }

        if (floorPreferNearestPointY && TryGetNearestSnapPointYNearReference(out float nearestPointY))
        {
            return nearestPointY;
        }

        return targetY;
    }

    // Handle Toggle Destroy Mode.
    private void ToggleDestroyMode()
    {
        _isDestroyMode = !_isDestroyMode;
        if (_isDestroyMode)
        {
            _isExtrudeMode = false;
            if (_isExtruding) CancelExtrudeState();
            if (_previewObject != null) _previewObject.SetActive(false);
            ClearSnapLock();
            ClearDestroyTargetHighlight();
            LogDetectionState("Destroy mode: ON");
            return;
        }

        ClearDestroyTargetHighlight();
        if (_previewObject == null)
        {
            CreatePreviewObject();
        }
        else
        {
            _previewObject.SetActive(true);
        }

        LogDetectionState("Destroy mode: OFF");
    }

    // Handle Handle Destroy Mode.
    private void HandleDestroyMode()
    {
        Ray ray = GetCenterRay();
        if (TryGetLookedAtBuildTarget(ray, out GameObject target))
        {
            SetDestroyTargetHighlight(target);
        }
        else
        {
            ClearDestroyTargetHighlight();
        }

        if (!Input.GetMouseButtonDown(0) || _destroyTarget == null)
        {
            return;
        }

        GameObject destroyNow = _destroyTarget;
        ClearDestroyTargetHighlight();
        Destroy(destroyNow);
    }

    // Handle Try Get Looked At Build Target.
    private bool TryGetLookedAtBuildTarget(Ray ray, out GameObject target)
    {
        target = null;
        int hitCount = Physics.RaycastNonAlloc(ray, _placementHits, range, hitMask, QueryTriggerInteraction.Ignore);
        if (hitCount <= 0)
        {
            return false;
        }

        float bestDistance = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            Collider collider = _placementHits[i].collider;
            if (ShouldSkipPlacementCollider(collider))
            {
                continue;
            }

            if (!TryGetSnapOwnerFromCollider(collider, out GameObject owner))
            {
                continue;
            }

            if (owner == null || owner == _previewObject || !owner.activeInHierarchy)
            {
                continue;
            }

            if (_placementHits[i].distance >= bestDistance)
            {
                continue;
            }

            bestDistance = _placementHits[i].distance;
            target = owner;
        }

        return target != null;
    }

    // Handle Set Destroy Target Highlight.
    private void SetDestroyTargetHighlight(GameObject target)
    {
        if (_destroyTarget == target)
        {
            return;
        }

        ClearDestroyTargetHighlight();
        _destroyTarget = target;
        if (_destroyTarget == null || destroyBuildMaterial == null)
        {
            return;
        }

        MeshRenderer[] renderers = _destroyTarget.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null || IsSnapMarkerTransform(renderer.transform))
            {
                continue;
            }

            Material[] original = renderer.sharedMaterials;
            if (original == null) original = new Material[0];
            _destroyHighlightedRenderers.Add(renderer);
            _destroyOriginalMaterials.Add((Material[])original.Clone());

            if (original.Length == 0)
            {
                renderer.sharedMaterial = destroyBuildMaterial;
                continue;
            }

            Material[] highlight = new Material[original.Length];
            for (int j = 0; j < highlight.Length; j++)
            {
                highlight[j] = destroyBuildMaterial;
            }

            renderer.sharedMaterials = highlight;
        }
    }

    // Handle Clear Destroy Target Highlight.
    private void ClearDestroyTargetHighlight()
    {
        for (int i = 0; i < _destroyHighlightedRenderers.Count; i++)
        {
            MeshRenderer renderer = _destroyHighlightedRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] original = _destroyOriginalMaterials[i];
            if (original == null)
            {
                continue;
            }

            renderer.sharedMaterials = original;
        }

        _destroyHighlightedRenderers.Clear();
        _destroyOriginalMaterials.Clear();
        _destroyTarget = null;
    }

    // Handle Toggle Extrude Mode.
    private void ToggleExtrudeMode()
    {
        if (GetActivePrefab() == null)
        {
            LogDetectionState("No active prefab for extrude mode.");
            return;
        }

        _isExtrudeMode = !_isExtrudeMode;
        if (!_isExtrudeMode) CancelExtrudeState();

        LogDetectionState(_buildType + " extrude: " + (_isExtrudeMode ? "ON" : "OFF"));
    }

    // Handle Handle Extrude Idle Input.
    private void HandleExtrudeIdleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _isExtrudeMode = false;
            CancelExtrudeState();
            return;
        }

        if (Input.GetMouseButtonDown(0)) BeginExtrude();
    }

    // Handle Handle Extrude Drag.
    private void HandleExtrudeDrag()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelExtrudeState();
            return;
        }

        UpdateExtrudePreview();
        if (Input.GetMouseButtonUp(0))
        {
            CommitExtrude();
            _isExtruding = false;
            ClearExtrudeGhosts();
            if (!extrudeModeStaysOn) _isExtrudeMode = false;
        }
    }

    // Handle Begin Extrude.
    private void BeginExtrude()
    {
        _extrudePrefab = GetActivePrefab();
        if (_previewObject == null || _extrudePrefab == null) return;

        _isExtruding = true;
        _extrudeBuildType = _buildType;
        _extrudeStartPosition = _previewObject.transform.position;
        _extrudeStartRotation = _previewObject.transform.rotation;
        _extrudeCellOffset = Vector2Int.zero;

        _extrudeRight = Vector3.ProjectOnPlane(_extrudeStartRotation * Vector3.right, Vector3.up);
        _extrudeForward = Vector3.ProjectOnPlane(_extrudeStartRotation * Vector3.forward, Vector3.up);
        if (_extrudeRight.sqrMagnitude < TinyValue) _extrudeRight = Vector3.right;
        else _extrudeRight.Normalize();

        if (_extrudeForward.sqrMagnitude < TinyValue) _extrudeForward = Vector3.forward;
        else _extrudeForward.Normalize();

        _extrudeCellSize = GetExtrudeCellSize(_previewObject, _extrudeRight, _extrudeForward);
        _previewObject.transform.SetPositionAndRotation(_extrudeStartPosition, _extrudeStartRotation);
        ClearExtrudeGhosts();
    }

    // Handle Update Extrude Preview.
    private void UpdateExtrudePreview()
    {
        if (!TryGetCursorPointForExtrude(out Vector3 cursorPoint)) return;

        Vector3 delta = cursorPoint - _extrudeStartPosition;
        int xCells = Mathf.RoundToInt(Vector3.Dot(delta, _extrudeRight) / _extrudeCellSize.x);
        int zCells = Mathf.RoundToInt(Vector3.Dot(delta, _extrudeForward) / _extrudeCellSize.y);
        _extrudeCellOffset = new Vector2Int(xCells, zCells);

        Vector3 snappedPosition =
            _extrudeStartPosition
            + (_extrudeRight * (xCells * _extrudeCellSize.x))
            + (_extrudeForward * (zCells * _extrudeCellSize.y));
        snappedPosition.y = _extrudeStartPosition.y;
        _previewObject.transform.SetPositionAndRotation(snappedPosition, _extrudeStartRotation);
        UpdateExtrudeGhosts();
    }

    // Handle Try Get Cursor Point For Extrude.
    private bool TryGetCursorPointForExtrude(out Vector3 point)
    {
        point = Vector3.zero;
        if (camera == null) return false;

        Ray ray = GetCenterRay();
        if (TryGetPlacementHit(ray, out RaycastHit hit))
        {
            point = hit.point;
            return true;
        }

        if (TryGetCursorPointOnHorizontalPlane(_extrudeStartPosition.y, out point))
        {
            return true;
        }

        point = ray.GetPoint(Mathf.Max(1f, range * 0.5f));
        return true;
    }

    // Handle Commit Extrude.
    private void CommitExtrude()
    {
        if (_extrudePrefab == null) return;
        ClearExtrudeGhosts();

        int minX = Mathf.Min(0, _extrudeCellOffset.x);
        int maxX = Mathf.Max(0, _extrudeCellOffset.x);
        int minZ = Mathf.Min(0, _extrudeCellOffset.y);
        int maxZ = Mathf.Max(0, _extrudeCellOffset.y);

        float occupyToleranceXZ = GetExtrudeOccupyToleranceXZ(_extrudeBuildType);
        float occupyToleranceY = Mathf.Max(MinTolerance, extrudeOccupyToleranceY);
        List<Transform> existing = GetExistingTransformsForBuildType(_extrudeBuildType);
        int placedCount = 0;
        int skippedAsDuplicateCount = 0;

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector3 placementPosition =
                    _extrudeStartPosition
                    + (_extrudeRight * (x * _extrudeCellSize.x))
                    + (_extrudeForward * (z * _extrudeCellSize.y));

                if (extrudePreventDuplicates &&
                    HasObjectAtPosition(existing, placementPosition, _extrudeStartRotation, occupyToleranceXZ, occupyToleranceY))
                {
                    skippedAsDuplicateCount++;
                    continue;
                }

                GameObject created = Instantiate(_extrudePrefab, placementPosition, _extrudeStartRotation);
                ApplyScaleMultiplier(created, GetActiveBuildScale());
                ApplyPlacedObjectVisuals(created);
                existing.Add(created.transform);
                placedCount++;
            }
            
        }

        if (placedCount == 0 && skippedAsDuplicateCount > 0)
        {
            LogDetectionState("Extrude skipped: all cells seen as occupied. Lower wall/floor/stair extrude occupy tolerance.");
        }

        UpdateExtrudePreview();
    }

    // Handle Cancel Extrude State.
    private void CancelExtrudeState()
    {
        _isExtruding = false;
        _extrudePrefab = null;
        _extrudeCellOffset = Vector2Int.zero;
        ClearExtrudeGhosts();
    }

    // Handle Update Extrude Ghosts.
    private void UpdateExtrudeGhosts()
    {
        if (!_isExtruding || _extrudePrefab == null)
        {
            ClearExtrudeGhosts();
            return;
        }

        int minX = Mathf.Min(0, _extrudeCellOffset.x);
        int maxX = Mathf.Max(0, _extrudeCellOffset.x);
        int minZ = Mathf.Min(0, _extrudeCellOffset.y);
        int maxZ = Mathf.Max(0, _extrudeCellOffset.y);

        HashSet<Vector2Int> neededCells = new HashSet<Vector2Int>();
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Vector2Int cell = new Vector2Int(x, z);
                if (cell == _extrudeCellOffset)
                {
                    continue;
                }

                neededCells.Add(cell);
                Vector3 cellPosition = GetExtrudeCellWorldPosition(x, z);
                if (_extrudeGhostByCell.TryGetValue(cell, out GameObject ghost))
                {
                    if (ghost == null)
                    {
                        _extrudeGhostByCell[cell] = CreateExtrudeGhost(cellPosition);
                    }
                    else
                    {
                        ghost.transform.SetPositionAndRotation(cellPosition, _extrudeStartRotation);
                    }
                }
                else
                {
                    _extrudeGhostByCell[cell] = CreateExtrudeGhost(cellPosition);
                }
            }
        }

        List<Vector2Int> cellsToRemove = new List<Vector2Int>();
        foreach (KeyValuePair<Vector2Int, GameObject> pair in _extrudeGhostByCell)
        {
            if (!neededCells.Contains(pair.Key))
            {
                if (pair.Value != null)
                {
                    pair.Value.SetActive(false);
                    Destroy(pair.Value);
                }

                cellsToRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < cellsToRemove.Count; i++)
        {
            _extrudeGhostByCell.Remove(cellsToRemove[i]);
        }
    }

    // Handle Create Extrude Ghost.
    private GameObject CreateExtrudeGhost(Vector3 position)
    {
        GameObject ghost = Instantiate(_extrudePrefab, position, _extrudeStartRotation);
        ApplyScaleMultiplier(ghost, GetActiveBuildScale());
        SetPreviewMode(ghost, true);
        ApplyMaterialToMeshRenderers(ghost, buildingMaterial);
        return ghost;
    }

    // Handle Get Extrude Cell World Position.
    private Vector3 GetExtrudeCellWorldPosition(int xCell, int zCell)
    {
        return _extrudeStartPosition
            + (_extrudeRight * (xCell * _extrudeCellSize.x))
            + (_extrudeForward * (zCell * _extrudeCellSize.y));
    }

    // Handle Clear Extrude Ghosts.
    private void ClearExtrudeGhosts()
    {
        if (_extrudeGhostByCell.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<Vector2Int, GameObject> pair in _extrudeGhostByCell)
        {
            if (pair.Value == null)
            {
                continue;
            }

            pair.Value.SetActive(false);
            Destroy(pair.Value);
        }

        _extrudeGhostByCell.Clear();
    }

    // Handle Get Existing Transforms For Build Type.
    private List<Transform> GetExistingTransformsForBuildType(BuildType type)
    {
        List<Transform> transforms = new List<Transform>(128);

        switch (type)
        {
            case BuildType.Wall:
                AddActiveTransforms(FindObjectsByType<WallSnapPoints>(FindObjectsSortMode.None), transforms);
                break;
            case BuildType.Floor:
                AddActiveTransforms(FindObjectsByType<FloorScript>(FindObjectsSortMode.None), transforms);
                break;
            case BuildType.Stair:
                AddActiveTransforms(FindObjectsByType<StairScript>(FindObjectsSortMode.None), transforms);
                break;
        }

        return transforms;
    }

    private void AddActiveTransforms<T>(T[] components, List<Transform> destination) where T : Component
    {
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component == null || component.gameObject == _previewObject || !component.gameObject.activeInHierarchy)
            {
                continue;
            }

            destination.Add(component.transform);
        }
    }

    private static bool HasObjectAtPosition(
        List<Transform> existing,
        Vector3 position,
        Quaternion rotation,
        float toleranceXZ,
        float toleranceY)
    {
        float toleranceXZSq = toleranceXZ * toleranceXZ;
        for (int i = 0; i < existing.Count; i++)
        {
            Transform candidate = existing[i];
            if (candidate == null || !candidate.gameObject.activeInHierarchy) continue;

            Vector3 existingPosition = candidate.position;
            float dx = existingPosition.x - position.x;
            float dz = existingPosition.z - position.z;
            if ((dx * dx) + (dz * dz) > toleranceXZSq) continue;
            if (Mathf.Abs(existingPosition.y - position.y) > toleranceY) continue;
            if (Quaternion.Angle(candidate.rotation, rotation) > RotationToleranceDegrees) continue;

            return true;
        }

        return false;
    }

    // Handle Try Get Cursor Point On Horizontal Plane.
    private bool TryGetCursorPointOnHorizontalPlane(float planeY, out Vector3 point)
    {
        point = Vector3.zero;
        if (camera == null) return false;

        Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
        Ray ray = GetCenterRay();
        if (!plane.Raycast(ray, out float enter)) return false;

        point = ray.GetPoint(enter);
        return true;
    }

    // Handle Get Preview Cell Size.
    private Vector2 GetPreviewCellSize(GameObject target, Vector3 axisRight, Vector3 axisForward)
    {
        if (TryGetSnapPoints(target, out SnapPoint[] snapPoints) &&
            TryGetCellSizeFromSnapPoints(snapPoints, axisRight, axisForward, out Vector2 snapSize))
        {
            return snapSize;
        }

        if (TryGetProjectedBoundsSize(target, axisRight, axisForward, out Vector2 projectedSize))
        {
            return projectedSize;
        }

        return Vector2.one;
    }

    // Handle Get Extrude Cell Size.
    private Vector2 GetExtrudeCellSize(GameObject target, Vector3 axisRight, Vector3 axisForward)
    {
        Vector2 cellSize = GetPreviewCellSize(target, axisRight, axisForward);
        if (_extrudeBuildType != BuildType.Wall || !wallExtrudeUseFloorSpacing)
        {
            return cellSize;
        }

        if (TryGetWallExtrudeReferenceFloor(out GameObject referenceFloor))
        {
            Vector2 floorCell = GetPreviewCellSize(referenceFloor, axisRight, axisForward);
            if (floorCell.x > MinSize && floorCell.y > MinSize)
            {
                cellSize = floorCell;
            }
        }

        float spacingScale = Mathf.Max(MinSize, wallExtrudeSpacingScale);
        cellSize.x = Mathf.Max(MinSize, cellSize.x * spacingScale);
        cellSize.y = Mathf.Max(MinSize, cellSize.y * spacingScale);
        return cellSize;
    }

    // Handle Try Get Wall Extrude Reference Floor.
    private bool TryGetWallExtrudeReferenceFloor(out GameObject floorReference)
    {
        floorReference = null;

        FloorScript[] floors = FindObjectsByType<FloorScript>(FindObjectsSortMode.None);
        if (floors.Length == 0)
        {
            floorReference = floor;
            return floorReference != null;
        }

        float bestDistanceSqr = float.MaxValue;
        Vector3 from = _extrudeStartPosition;
        for (int i = 0; i < floors.Length; i++)
        {
            FloorScript floorScript = floors[i];
            if (floorScript == null || !floorScript.gameObject.activeInHierarchy || floorScript.gameObject == _previewObject)
            {
                continue;
            }

            Vector3 to = floorScript.transform.position;
            float dx = to.x - from.x;
            float dz = to.z - from.z;
            float distanceSqr = (dx * dx) + (dz * dz);
            if (distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            floorReference = floorScript.gameObject;
        }

        if (floorReference != null)
        {
            return true;
        }

        floorReference = floor;
        return floorReference != null;
    }

    // Handle Get Extrude Occupy Tolerance XZ.
    private float GetExtrudeOccupyToleranceXZ(BuildType type)
    {
        switch (type)
        {
            case BuildType.Wall:
                return Mathf.Max(MinTolerance, wallExtrudeOccupyToleranceXZ);
            case BuildType.Floor:
                return Mathf.Max(MinTolerance, floorExtrudeOccupyToleranceXZ);
            case BuildType.Stair:
                return Mathf.Max(MinTolerance, stairExtrudeOccupyToleranceXZ);
            default:
                return 0.02f;
        }
    }

    private static bool TryGetCellSizeFromSnapPoints(
        SnapPoint[] snapPoints,
        Vector3 axisRight,
        Vector3 axisForward,
        out Vector2 size)
    {
        size = Vector2.zero;
        if (snapPoints == null || snapPoints.Length == 0) return false;
        if (axisRight.sqrMagnitude < TinyValue || axisForward.sqrMagnitude < TinyValue) return false;

        axisRight.Normalize();
        axisForward.Normalize();

        bool hasPoint = false;
        float minRight = float.MaxValue;
        float maxRight = float.MinValue;
        float minForward = float.MaxValue;
        float maxForward = float.MinValue;

        for (int i = 0; i < snapPoints.Length; i++)
        {
            SnapPoint snapPoint = snapPoints[i];
            if (snapPoint == null) continue;

            Vector3 point = snapPoint.transform.position;
            float rightDot = Vector3.Dot(point, axisRight);
            float forwardDot = Vector3.Dot(point, axisForward);
            minRight = Mathf.Min(minRight, rightDot);
            maxRight = Mathf.Max(maxRight, rightDot);
            minForward = Mathf.Min(minForward, forwardDot);
            maxForward = Mathf.Max(maxForward, forwardDot);
            hasPoint = true;
        }

        if (!hasPoint) return false;

        float sizeRight = maxRight - minRight;
        float sizeForward = maxForward - minForward;
        if (sizeRight < MinSize || sizeForward < MinSize) return false;

        size = new Vector2(sizeRight, sizeForward);
        return true;
    }

    private static bool TryGetProjectedBoundsSize(
        GameObject target,
        Vector3 axisRight,
        Vector3 axisForward,
        out Vector2 size)
    {
        size = Vector2.zero;
        if (!TryGetRenderableBoundsWithoutSnapMarkers(target, out Bounds bounds)) return false;
        if (axisRight.sqrMagnitude < TinyValue || axisForward.sqrMagnitude < TinyValue) return false;

        axisRight.Normalize();
        axisForward.Normalize();

        Vector3 min = bounds.min;
        Vector3 max = bounds.max;
        Vector3[] corners =
        {
            new Vector3(min.x, min.y, min.z),
            new Vector3(min.x, min.y, max.z),
            new Vector3(min.x, max.y, min.z),
            new Vector3(min.x, max.y, max.z),
            new Vector3(max.x, min.y, min.z),
            new Vector3(max.x, min.y, max.z),
            new Vector3(max.x, max.y, min.z),
            new Vector3(max.x, max.y, max.z),
        };

        float minRight = float.MaxValue;
        float maxRight = float.MinValue;
        float minForward = float.MaxValue;
        float maxForward = float.MinValue;

        for (int i = 0; i < corners.Length; i++)
        {
            float rightDot = Vector3.Dot(corners[i], axisRight);
            float forwardDot = Vector3.Dot(corners[i], axisForward);
            minRight = Mathf.Min(minRight, rightDot);
            maxRight = Mathf.Max(maxRight, rightDot);
            minForward = Mathf.Min(minForward, forwardDot);
            maxForward = Mathf.Max(maxForward, forwardDot);
        }

        float sizeRight = maxRight - minRight;
        float sizeForward = maxForward - minForward;
        if (sizeRight < MinSize || sizeForward < MinSize) return false;

        size = new Vector2(sizeRight, sizeForward);
        return true;
    }

    // Handle Handle Sticky Two Point Snap.
    private void HandleStickyTwoPointSnap(Vector3 rawPosition, Quaternion rawRotation)
    {
        if (_isSnapLocked)
        {
            if (TrySolveFromLockedPairs(rawPosition, rawRotation, out Vector3 lockedPosition, out Quaternion lockedRotation, out float lockedDistanceA, out float lockedDistanceB))
            {
                _previewObject.transform.SetPositionAndRotation(lockedPosition, lockedRotation);
                DrawAndLogDistances(
                    _lockedPreviewA.transform.position,
                    _lockedTargetA.transform.position,
                    _lockedPreviewB.transform.position,
                    _lockedTargetB.transform.position,
                    lockedDistanceA,
                    lockedDistanceB,
                    "Closest pairs(solved-lock)");

                GetRawLockedPairDistances(rawPosition, rawRotation, out float rawDistanceA, out float rawDistanceB);
                if (rawDistanceA <= snapReleaseDistance && rawDistanceB <= snapReleaseDistance)
                {
                    return;
                }
            }

            ClearSnapLock();
            _previewObject.transform.SetPositionAndRotation(rawPosition, rawRotation);
        }

        if (!TryGetClosestTwoPairsOnSameObject(
            out PairCandidate pairA,
            out PairCandidate pairB,
            out int scannedTargets,
            out int validTargets,
            out int totalCandidates))
        {
            LogDetectionState($"No valid 2-point pairs. ScannedTargets={scannedTargets}, ValidTargets={validTargets}, Candidates={totalCandidates}");
            return;
        }

        DrawAndLogDistances(pairA.previewWorld, pairA.targetWorld, pairB.previewWorld, pairB.targetWorld, pairA.distance, pairB.distance);

        if (pairA.distance > snapEngageDistance || pairB.distance > snapEngageDistance)
        {
            return;
        }

        _isSnapLocked = true;
        _lockedPreviewA = pairA.preview;
        _lockedPreviewB = pairB.preview;
        _lockedTargetA = pairA.target;
        _lockedTargetB = pairB.target;
        _lockedPreviewOffsetA = GetScaledLocalPoint(_lockedPreviewA);
        _lockedPreviewOffsetB = GetScaledLocalPoint(_lockedPreviewB);

        if (TrySolveFromLockedPairs(rawPosition, rawRotation, out Vector3 snappedPosition, out Quaternion snappedRotation, out float snappedDistanceA, out float snappedDistanceB))
        {
            _previewObject.transform.SetPositionAndRotation(snappedPosition, snappedRotation);
            DrawAndLogDistances(
                _lockedPreviewA.transform.position,
                _lockedTargetA.transform.position,
                _lockedPreviewB.transform.position,
                _lockedTargetB.transform.position,
                snappedDistanceA,
                snappedDistanceB,
                "Closest pairs(solved-snap)");
        }
    }

    private void GetRawLockedPairDistances(
        Vector3 rawPosition,
        Quaternion rawRotation,
        out float rawDistanceA,
        out float rawDistanceB)
    {
        rawDistanceA = float.MaxValue;
        rawDistanceB = float.MaxValue;

        if (_lockedPreviewA == null || _lockedPreviewB == null || _lockedTargetA == null || _lockedTargetB == null)
        {
            return;
        }

        Vector3 rawWorldA = rawPosition + (rawRotation * _lockedPreviewOffsetA);
        Vector3 rawWorldB = rawPosition + (rawRotation * _lockedPreviewOffsetB);
        rawDistanceA = Vector3.Distance(rawWorldA, _lockedTargetA.transform.position);
        rawDistanceB = Vector3.Distance(rawWorldB, _lockedTargetB.transform.position);
    }

    private bool TryGetClosestTwoPairsOnSameObject(
        out PairCandidate bestA,
        out PairCandidate bestB,
        out int scannedTargets,
        out int validTargets,
        out int totalCandidates)
    {
        bestA = default;
        bestB = default;
        scannedTargets = 0;
        validTargets = 0;
        totalCandidates = 0;

        if (!TryGetSnapPoints(_previewObject, out SnapPoint[] previewSnapPoints))
        {
            LogDetectionState("Preview object has no snap points configured.");
            return false;
        }

        bool found = false;
        float bestScore = float.MaxValue;

        WallSnapPoints[] wallTargets = FindObjectsByType<WallSnapPoints>(FindObjectsSortMode.None);
        for (int i = 0; i < wallTargets.Length; i++)
        {
            EvaluateTargetPairs(
                wallTargets[i] != null ? wallTargets[i].gameObject : null,
                wallTargets[i] != null ? wallTargets[i].snapPoints : null,
                previewSnapPoints,
                ref bestA,
                ref bestB,
                ref found,
                ref bestScore,
                ref scannedTargets,
                ref validTargets,
                ref totalCandidates);
        }

        FloorScript[] floorTargets = FindObjectsByType<FloorScript>(FindObjectsSortMode.None);
        for (int i = 0; i < floorTargets.Length; i++)
        {
            EvaluateTargetPairs(
                floorTargets[i] != null ? floorTargets[i].gameObject : null,
                floorTargets[i] != null ? floorTargets[i].snapPoints : null,
                previewSnapPoints,
                ref bestA,
                ref bestB,
                ref found,
                ref bestScore,
                ref scannedTargets,
                ref validTargets,
                ref totalCandidates);
        }

        StairScript[] stairTargets = FindObjectsByType<StairScript>(FindObjectsSortMode.None);
        for (int i = 0; i < stairTargets.Length; i++)
        {
            EvaluateTargetPairs(
                stairTargets[i] != null ? stairTargets[i].gameObject : null,
                stairTargets[i] != null ? stairTargets[i].snapPoints : null,
                previewSnapPoints,
                ref bestA,
                ref bestB,
                ref found,
                ref bestScore,
                ref scannedTargets,
                ref validTargets,
                ref totalCandidates);
        }

        return found;
    }

    private void EvaluateTargetPairs(
        GameObject targetObject,
        SnapPoint[] targetSnapPoints,
        SnapPoint[] previewSnapPoints,
        ref PairCandidate bestA,
        ref PairCandidate bestB,
        ref bool found,
        ref float bestScore,
        ref int scannedTargets,
        ref int validTargets,
        ref int totalCandidates)
    {
        if (targetObject == null || targetObject == _previewObject || !targetObject.activeInHierarchy)
        {
            return;
        }

        scannedTargets++;
        if (targetSnapPoints == null || targetSnapPoints.Length < 2)
        {
            return;
        }

        validTargets++;
        _candidates.Clear();

        for (int i = 0; i < previewSnapPoints.Length; i++)
        {
            SnapPoint previewPoint = previewSnapPoints[i];
            if (previewPoint == null)
            {
                continue;
            }

            Vector3 previewWorld = previewPoint.transform.position;
            for (int j = 0; j < targetSnapPoints.Length; j++)
            {
                SnapPoint targetPoint = targetSnapPoints[j];
                if (targetPoint == null)
                {
                    continue;
                }

                PairCandidate candidate = new PairCandidate
                {
                    preview = previewPoint,
                    target = targetPoint,
                    previewWorld = previewWorld,
                    targetWorld = targetPoint.transform.position,
                    distance = Vector3.Distance(previewWorld, targetPoint.transform.position)
                };
                _candidates.Add(candidate);
            }
        }

        totalCandidates += _candidates.Count;
        if (_candidates.Count < 2)
        {
            return;
        }

        for (int i = 0; i < _candidates.Count - 1; i++)
        {
            PairCandidate candidateA = _candidates[i];
            for (int j = i + 1; j < _candidates.Count; j++)
            {
                PairCandidate candidateB = _candidates[j];
                if (candidateA.preview == candidateB.preview || candidateA.target == candidateB.target)
                {
                    continue;
                }

                float previewPairLength = Vector3.Distance(candidateA.previewWorld, candidateB.previewWorld);
                float targetPairLength = Vector3.Distance(candidateA.targetWorld, candidateB.targetWorld);
                if (Mathf.Abs(previewPairLength - targetPairLength) > pairLengthTolerance)
                {
                    continue;
                }

                float score = candidateA.distance + candidateB.distance;
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                bestA = candidateA;
                bestB = candidateB;
                found = true;
            }
        }
    }

    private bool TrySolveFromLockedPairs(
        Vector3 rawPosition,
        Quaternion rawRotation,
        out Vector3 position,
        out Quaternion rotation,
        out float distanceA,
        out float distanceB)
    {
        position = Vector3.zero;
        rotation = rawRotation;
        distanceA = float.MaxValue;
        distanceB = float.MaxValue;

        if (_lockedPreviewA == null || _lockedPreviewB == null || _lockedTargetA == null || _lockedTargetB == null)
        {
            return false;
        }

        Vector3 targetWorldA = _lockedTargetA.transform.position;
        Vector3 targetWorldB = _lockedTargetB.transform.position;

        if (alignRotationFromPairs)
        {
            Vector3 previewVector = _lockedPreviewOffsetB - _lockedPreviewOffsetA;
            Vector3 previewVectorWorld = rawRotation * previewVector;
            Vector3 targetVector = targetWorldB - targetWorldA;

            if (previewVectorWorld.sqrMagnitude > TinyValue && targetVector.sqrMagnitude > TinyValue)
            {
                Quaternion delta = Quaternion.FromToRotation(previewVectorWorld, targetVector);
                rotation = delta * rawRotation;
            }
        }

        Vector3 rotatedOffsetA = rotation * _lockedPreviewOffsetA;
        Vector3 rotatedOffsetB = rotation * _lockedPreviewOffsetB;

        position = targetWorldA - rotatedOffsetA;
        position.y += snapYOffset;

        Vector3 solvedA = position + rotatedOffsetA;
        Vector3 solvedB = position + rotatedOffsetB;
        distanceA = Vector3.Distance(solvedA, targetWorldA);
        distanceB = Vector3.Distance(solvedB, targetWorldB);
        return true;
    }

    // Handle Try Get Snap Points.
    private bool TryGetSnapPoints(GameObject target, out SnapPoint[] snapPoints)
    {
        snapPoints = null;
        if (target == null)
        {
            return false;
        }

        if (target.TryGetComponent(out WallSnapPoints wallSnapPoints) && wallSnapPoints.snapPoints != null && wallSnapPoints.snapPoints.Length > 0)
        {
            snapPoints = wallSnapPoints.snapPoints;
            return true;
        }

        if (target.TryGetComponent(out FloorScript floorSnapPoints) && floorSnapPoints.snapPoints != null && floorSnapPoints.snapPoints.Length > 0)
        {
            snapPoints = floorSnapPoints.snapPoints;
            return true;
        }

        if (target.TryGetComponent(out StairScript stairSnapPoints) && stairSnapPoints.snapPoints != null && stairSnapPoints.snapPoints.Length > 0)
        {
            snapPoints = stairSnapPoints.snapPoints;
            return true;
        }

        return false;
    }

    // Handle Get Scaled Local Point.
    private Vector3 GetScaledLocalPoint(SnapPoint point)
    {
        Vector3 localPoint = _previewObject.transform.InverseTransformPoint(point.transform.position);
        return Vector3.Scale(localPoint, _previewObject.transform.lossyScale);
    }

    // Handle Apply Material To Mesh Renderers.
    private static void ApplyMaterialToMeshRenderers(GameObject target, Material material)
    {
        if (target == null || material == null)
        {
            return;
        }

        MeshRenderer[] renderers = target.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            Material[] materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                renderer.sharedMaterial = material;
                continue;
            }

            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = material;
            }

            renderer.sharedMaterials = materials;
        }
    }

    private void DrawAndLogDistances(
        Vector3 fromA,
        Vector3 toA,
        Vector3 fromB,
        Vector3 toB,
        float distanceA,
        float distanceB,
        string label = "Closest pairs(raw)")
    {
        Debug.DrawLine(fromA, toA, debugLineColorA);
        Debug.DrawLine(fromB, toB, debugLineColorB);

        if (!logClosestPair)
        {
            return;
        }

        bool changedEnoughA = _lastLoggedDistanceA < 0f || Mathf.Abs(distanceA - _lastLoggedDistanceA) >= logDistanceDelta;
        bool changedEnoughB = _lastLoggedDistanceB < 0f || Mathf.Abs(distanceB - _lastLoggedDistanceB) >= logDistanceDelta;
        bool intervalReached = Time.time - _lastLogTime >= logInterval;
        if (!changedEnoughA && !changedEnoughB)
        {
            return;
        }

        if (!intervalReached)
        {
            return;
        }

        Debug.Log($"{label}: 1) {distanceA:F3}, 2) {distanceB:F3}, total={distanceA + distanceB:F3}");
        _lastLoggedDistanceA = distanceA;
        _lastLoggedDistanceB = distanceB;
        _lastLogTime = Time.time;
    }

    // Handle Log Detection State.
    private void LogDetectionState(string message)
    {
        if (!logDetectionState)
        {
            return;
        }

        if (Time.time - _lastLogTime < logInterval)
        {
            return;
        }

        Debug.Log(message);
        _lastLogTime = Time.time;
    }

    // Handle Try Get Snap Owner From Collider.
    private static bool TryGetSnapOwnerFromCollider(Collider hitCollider, out GameObject owner)
    {
        owner = null;
        if (hitCollider == null)
        {
            return false;
        }

        WallSnapPoints wallOwner = hitCollider.GetComponentInParent<WallSnapPoints>();
        if (wallOwner != null)
        {
            owner = wallOwner.gameObject;
            return true;
        }

        FloorScript floorOwner = hitCollider.GetComponentInParent<FloorScript>();
        if (floorOwner != null)
        {
            owner = floorOwner.gameObject;
            return true;
        }

        StairScript stairOwner = hitCollider.GetComponentInParent<StairScript>();
        if (stairOwner != null)
        {
            owner = stairOwner.gameObject;
            return true;
        }

        return false;
    }

    // Handle Get Top Y.
    private static float GetTopY(GameObject target)
    {
        return TryGetExtremeY(target, searchTop: true, out float y) ? y : target.transform.position.y;
    }

    // Handle Get Bottom Y.
    private static float GetBottomY(GameObject target)
    {
        return TryGetExtremeY(target, searchTop: false, out float y) ? y : target.transform.position.y;
    }

    // Handle Try Get Extreme Y.
    private static bool TryGetExtremeY(GameObject target, bool searchTop, out float y)
    {
        // Prefer collider bounds, then renderer bounds if colliders are missing.
        y = 0f;
        if (target == null)
        {
            return false;
        }

        if (TryGetExtremeYFromColliders(target.GetComponentsInChildren<Collider>(true), searchTop, out y))
        {
            return true;
        }

        return TryGetExtremeYFromRenderers(target.GetComponentsInChildren<Renderer>(true), searchTop, out y);
    }

    // Handle Try Get Extreme YFrom Colliders.
    private static bool TryGetExtremeYFromColliders(Collider[] colliders, bool searchTop, out float y)
    {
        y = searchTop ? float.MinValue : float.MaxValue;
        bool found = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || IsSnapMarkerTransform(collider.transform))
            {
                continue;
            }

            float candidate = searchTop ? collider.bounds.max.y : collider.bounds.min.y;
            if (!found)
            {
                y = candidate;
                found = true;
                continue;
            }

            y = searchTop ? Mathf.Max(y, candidate) : Mathf.Min(y, candidate);
        }

        return found;
    }

    // Handle Try Get Extreme YFrom Renderers.
    private static bool TryGetExtremeYFromRenderers(Renderer[] renderers, bool searchTop, out float y)
    {
        y = searchTop ? float.MinValue : float.MaxValue;
        bool found = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || IsSnapMarkerTransform(renderer.transform))
            {
                continue;
            }

            float candidate = searchTop ? renderer.bounds.max.y : renderer.bounds.min.y;
            if (!found)
            {
                y = candidate;
                found = true;
                continue;
            }

            y = searchTop ? Mathf.Max(y, candidate) : Mathf.Min(y, candidate);
        }

        return found;
    }

    // Handle Clear Snap Lock.
    private void ClearSnapLock()
    {
        _isSnapLocked = false;
        _lockedPreviewA = null;
        _lockedPreviewB = null;
        _lockedTargetA = null;
        _lockedTargetB = null;
        _lockedPreviewOffsetA = Vector3.zero;
        _lockedPreviewOffsetB = Vector3.zero;
    }

    // Handle Set Preview Mode.
    private static void SetPreviewMode(GameObject target, bool isPreview)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = !isPreview;
        }
    }

    // Handle Get Bottom Offset.
    private static float GetBottomOffset(GameObject target)
    {
        if (!TryGetRenderableBoundsWithoutSnapMarkers(target, out Bounds bounds))
        {
            return 0f;
        }

        return target.transform.position.y - bounds.min.y;
    }

    // Handle Try Get Renderable Bounds Without Snap Markers.
    private static bool TryGetRenderableBoundsWithoutSnapMarkers(GameObject target, out Bounds bounds)
    {
        bounds = default;
        if (target == null)
        {
            return false;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || IsSnapMarkerTransform(renderer.transform))
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    // Handle Is Snap Marker Transform.
    private static bool IsSnapMarkerTransform(Transform transformToCheck)
    {
        return transformToCheck != null && transformToCheck.GetComponentInParent<SnapPoint>() != null;
    }

    // Handle Is UIBlocking Gameplay.
    private static bool IsUiBlockingGameplay()
    {
        return InventoryController.IsInventoryOpen || CraftingManager.IsCraftingOpen || VisualCommunication.IsTalking;
    }

    // Handle Try Get Nearest Snap Point YNear Reference.
    private bool TryGetNearestSnapPointYNearReference(out float nearestY)
    {
        nearestY = 0f;
        Transform reference = floorHeightReference != null ? floorHeightReference : camera != null ? camera.transform : transform;
        if (reference == null)
        {
            return false;
        }

        float radius = Mathf.Max(MinSize, floorNearestPointRadius);
        float maxDistanceSqr = radius * radius;
        Vector3 referencePosition = reference.position;
        bool found = false;
        float bestDistanceSqr = float.MaxValue;

        SnapPoint[] allSnapPoints = FindObjectsByType<SnapPoint>(FindObjectsSortMode.None);
        for (int i = 0; i < allSnapPoints.Length; i++)
        {
            SnapPoint snapPoint = allSnapPoints[i];
            if (snapPoint == null || !snapPoint.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (_previewObject != null && snapPoint.transform.IsChildOf(_previewObject.transform))
            {
                continue;
            }

            Vector3 pointPosition = snapPoint.transform.position;
            float distanceSqr;
            if (floorNearestPointUseXZ)
            {
                float dx = pointPosition.x - referencePosition.x;
                float dz = pointPosition.z - referencePosition.z;
                distanceSqr = (dx * dx) + (dz * dz);
            }
            else
            {
                distanceSqr = (pointPosition - referencePosition).sqrMagnitude;
            }

            if (distanceSqr > maxDistanceSqr || distanceSqr >= bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = distanceSqr;
            nearestY = pointPosition.y;
            found = true;
        }

        return found;
    }
}

