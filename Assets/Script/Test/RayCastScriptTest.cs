using System.Collections.Generic;
using UnityEngine;

public class RayCastScriptTest : MonoBehaviour
{
    private enum BuildType { Wall, Floor, Stair }

    [Header("Placement")]
    public Camera camera;
    public LayerMask hitMask = ~0;
    public float range = 100f;
    public GameObject wall;
    public GameObject floor;
    public GameObject stair;

    [Header("Input")]
    public KeyCode rotateKey = KeyCode.R;

    [Header("Rotation")]
    public float rotateStep = 90f;

    [Header("Visuals")]
    public Material buildingMaterial;
    public Material doneBuildMaterial;

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

    private GameObject _previewWall;
    private float _previewYRotation;
    private float _bottomOffset;
    private float _lastLoggedDistanceA = -1f;
    private float _lastLoggedDistanceB = -1f;
    private float _lastLogTime = -10f;
    private BuildType _buildType = BuildType.Wall;
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

    private readonly List<PairCandidate> _candidates = new List<PairCandidate>(64);

    private struct PairCandidate
    {
        public SnapPoint preview;
        public SnapPoint target;
        public Vector3 previewWorld;
        public Vector3 targetWorld;
        public float distance;
    }

    private void Start()
    {
        if (camera == null) camera = Camera.main;
        InitializeBuildType();
        CreatePreviewObject();
    }

    private void Update()
    {
        if (camera == null) camera = Camera.main;

        GameObject activePrefab = GetActivePrefab();
        if (camera == null || activePrefab == null) return;

        if (_previewWall == null)
        {
            CreatePreviewObject();
            if (_previewWall == null) return;
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (_isExtruding) CancelExtrudeState();
            ToggleBuildType();
            activePrefab = GetActivePrefab();
            if (activePrefab == null) return;
        }

        if (Input.GetKeyDown(extrudeKey)) ToggleExtrudeMode();
        if (!_isExtruding && Input.GetKeyDown(rotateKey)) _previewYRotation += rotateStep;

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

        if (Input.GetMouseButtonDown(0))
        {
            GameObject created = Instantiate(activePrefab, _previewWall.transform.position, _previewWall.transform.rotation);
            ApplyMaterialToMeshRenderers(created, doneBuildMaterial);
        }
    }

    private void InitializeBuildType()
    {
        RefreshAvailableBuildTypes();
        if (_availableBuildTypes.Count > 0) _buildType = _availableBuildTypes[0];
    }

    private GameObject GetActivePrefab()
    {
        return _buildType switch
        {
            BuildType.Wall => wall,
            BuildType.Floor => floor,
            BuildType.Stair => stair,
            _ => null,
        };
    }

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

    private void RefreshAvailableBuildTypes()
    {
        _availableBuildTypes.Clear();
        if (wall != null) _availableBuildTypes.Add(BuildType.Wall);
        if (floor != null) _availableBuildTypes.Add(BuildType.Floor);
        if (stair != null) _availableBuildTypes.Add(BuildType.Stair);
    }

    private void CreatePreviewObject()
    {
        GameObject activePrefab = GetActivePrefab();
        if (activePrefab == null) return;

        if (_previewWall != null) Destroy(_previewWall);

        _previewWall = Instantiate(activePrefab, Vector3.zero, Quaternion.identity);
        ApplyMaterialToMeshRenderers(_previewWall, buildingMaterial);
        _previewYRotation = _previewWall.transform.eulerAngles.y;
        SetPreviewMode(_previewWall, true);
        _bottomOffset = GetBottomOffset(_previewWall);
        ClearSnapLock();
        CancelExtrudeState();
    }

    private void MovePreviewObject()
    {
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore)) return;

        Vector3 rawPosition = GetRawPlacementPosition(hit);
        Quaternion rawRotation = Quaternion.Euler(0f, _previewYRotation, 0f);
        _previewWall.transform.SetPositionAndRotation(rawPosition, rawRotation);

        if (!enableTwoPointSnap)
        {
            if (_isSnapLocked) ClearSnapLock();
            return;
        }

        HandleStickyTwoPointSnap(rawPosition, rawRotation);
    }

    private Vector3 GetRawPlacementPosition(RaycastHit hit)
    {
        Vector3 rawPosition = hit.point;

        if (_buildType == BuildType.Floor)
        {
            float targetY = hit.point.y;

            // First: if we're aiming at an existing build piece, align Y from that piece type.
            if (TryGetSnapOwnerFromCollider(hit.collider, out GameObject owner))
            {
                if (owner.TryGetComponent(out FloorScript _))
                {
                    // Place floor on the same level as existing floor.
                    targetY = GetBottomY(owner);
                }
                else
                {
                    // Place floor on top of wall/stair.
                    targetY = GetTopY(owner);
                }
            }
            else if (floorPreferNearestPointY && TryGetNearestSnapPointYNearReference(out float nearestPointY))
            {
                targetY = nearestPointY;
            }

            rawPosition.y = targetY;
        }

        rawPosition.y += _bottomOffset;
        return rawPosition;
    }

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

    private void BeginExtrude()
    {
        _extrudePrefab = GetActivePrefab();
        if (_previewWall == null || _extrudePrefab == null) return;

        _isExtruding = true;
        _extrudeBuildType = _buildType;
        _extrudeStartPosition = _previewWall.transform.position;
        _extrudeStartRotation = _previewWall.transform.rotation;
        _extrudeCellOffset = Vector2Int.zero;

        _extrudeRight = Vector3.ProjectOnPlane(_extrudeStartRotation * Vector3.right, Vector3.up);
        _extrudeForward = Vector3.ProjectOnPlane(_extrudeStartRotation * Vector3.forward, Vector3.up);
        if (_extrudeRight.sqrMagnitude < 0.000001f) _extrudeRight = Vector3.right;
        else _extrudeRight.Normalize();

        if (_extrudeForward.sqrMagnitude < 0.000001f) _extrudeForward = Vector3.forward;
        else _extrudeForward.Normalize();

        _extrudeCellSize = GetExtrudeCellSize(_previewWall, _extrudeRight, _extrudeForward);
        _previewWall.transform.SetPositionAndRotation(_extrudeStartPosition, _extrudeStartRotation);
        ClearExtrudeGhosts();
    }

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
        _previewWall.transform.SetPositionAndRotation(snappedPosition, _extrudeStartRotation);
        UpdateExtrudeGhosts();
    }

    private bool TryGetCursorPointForExtrude(out Vector3 point)
    {
        point = Vector3.zero;
        if (camera == null) return false;

        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
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

    private void CommitExtrude()
    {
        if (_extrudePrefab == null) return;
        ClearExtrudeGhosts();

        int minX = Mathf.Min(0, _extrudeCellOffset.x);
        int maxX = Mathf.Max(0, _extrudeCellOffset.x);
        int minZ = Mathf.Min(0, _extrudeCellOffset.y);
        int maxZ = Mathf.Max(0, _extrudeCellOffset.y);

        float occupyToleranceXZ = GetExtrudeOccupyToleranceXZ(_extrudeBuildType);
        float occupyToleranceY = Mathf.Max(0.001f, extrudeOccupyToleranceY);
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
                ApplyMaterialToMeshRenderers(created, doneBuildMaterial);
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

    private void CancelExtrudeState()
    {
        _isExtruding = false;
        _extrudePrefab = null;
        _extrudeCellOffset = Vector2Int.zero;
        ClearExtrudeGhosts();
    }

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

    private GameObject CreateExtrudeGhost(Vector3 position)
    {
        GameObject ghost = Instantiate(_extrudePrefab, position, _extrudeStartRotation);
        SetPreviewMode(ghost, true);
        ApplyMaterialToMeshRenderers(ghost, buildingMaterial);
        return ghost;
    }

    private Vector3 GetExtrudeCellWorldPosition(int xCell, int zCell)
    {
        return _extrudeStartPosition
            + (_extrudeRight * (xCell * _extrudeCellSize.x))
            + (_extrudeForward * (zCell * _extrudeCellSize.y));
    }

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

    private List<Transform> GetExistingTransformsForBuildType(BuildType type)
    {
        List<Transform> transforms = new List<Transform>(128);

        switch (type)
        {
            case BuildType.Wall:
            {
                WallSnapPoints[] walls = FindObjectsByType<WallSnapPoints>(FindObjectsSortMode.None);
                for (int i = 0; i < walls.Length; i++)
                {
                    if (walls[i] == null || walls[i].gameObject == _previewWall || !walls[i].gameObject.activeInHierarchy) continue;
                    transforms.Add(walls[i].transform);
                }

                break;
            }
            case BuildType.Floor:
            {
                FloorScript[] floors = FindObjectsByType<FloorScript>(FindObjectsSortMode.None);
                for (int i = 0; i < floors.Length; i++)
                {
                    if (floors[i] == null || floors[i].gameObject == _previewWall || !floors[i].gameObject.activeInHierarchy) continue;
                    transforms.Add(floors[i].transform);
                }

                break;
            }
            case BuildType.Stair:
            {
                StairScript[] stairs = FindObjectsByType<StairScript>(FindObjectsSortMode.None);
                for (int i = 0; i < stairs.Length; i++)
                {
                    if (stairs[i] == null || stairs[i].gameObject == _previewWall || !stairs[i].gameObject.activeInHierarchy) continue;
                    transforms.Add(stairs[i].transform);
                }

                break;
            }
        }

        return transforms;
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
            if (Quaternion.Angle(candidate.rotation, rotation) > 1f) continue;

            return true;
        }

        return false;
    }

    private bool TryGetCursorPointOnHorizontalPlane(float planeY, out Vector3 point)
    {
        point = Vector3.zero;
        if (camera == null) return false;

        Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!plane.Raycast(ray, out float enter)) return false;

        point = ray.GetPoint(enter);
        return true;
    }

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
            if (floorCell.x > 0.01f && floorCell.y > 0.01f)
            {
                cellSize = floorCell;
            }
        }

        float spacingScale = Mathf.Max(0.01f, wallExtrudeSpacingScale);
        cellSize.x = Mathf.Max(0.01f, cellSize.x * spacingScale);
        cellSize.y = Mathf.Max(0.01f, cellSize.y * spacingScale);
        return cellSize;
    }

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
            if (floorScript == null || !floorScript.gameObject.activeInHierarchy || floorScript.gameObject == _previewWall)
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

    private float GetExtrudeOccupyToleranceXZ(BuildType type)
    {
        switch (type)
        {
            case BuildType.Wall:
                return Mathf.Max(0.001f, wallExtrudeOccupyToleranceXZ);
            case BuildType.Floor:
                return Mathf.Max(0.001f, floorExtrudeOccupyToleranceXZ);
            case BuildType.Stair:
                return Mathf.Max(0.001f, stairExtrudeOccupyToleranceXZ);
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
        if (axisRight.sqrMagnitude < 0.000001f || axisForward.sqrMagnitude < 0.000001f) return false;

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
        if (sizeRight < 0.01f || sizeForward < 0.01f) return false;

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
        if (axisRight.sqrMagnitude < 0.000001f || axisForward.sqrMagnitude < 0.000001f) return false;

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
        if (sizeRight < 0.01f || sizeForward < 0.01f) return false;

        size = new Vector2(sizeRight, sizeForward);
        return true;
    }

    private void HandleStickyTwoPointSnap(Vector3 rawPosition, Quaternion rawRotation)
    {
        if (_isSnapLocked)
        {
            if (TrySolveFromLockedPairs(rawPosition, rawRotation, out Vector3 lockedPosition, out Quaternion lockedRotation, out float lockedDistanceA, out float lockedDistanceB))
            {
                _previewWall.transform.SetPositionAndRotation(lockedPosition, lockedRotation);
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
            _previewWall.transform.SetPositionAndRotation(rawPosition, rawRotation);
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
            _previewWall.transform.SetPositionAndRotation(snappedPosition, snappedRotation);
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

        if (!TryGetSnapPoints(_previewWall, out SnapPoint[] previewSnapPoints))
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
        if (targetObject == null || targetObject == _previewWall || !targetObject.activeInHierarchy)
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

            if (previewVectorWorld.sqrMagnitude > 0.000001f && targetVector.sqrMagnitude > 0.000001f)
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

    private Vector3 GetScaledLocalPoint(SnapPoint point)
    {
        Vector3 localPoint = _previewWall.transform.InverseTransformPoint(point.transform.position);
        return Vector3.Scale(localPoint, _previewWall.transform.lossyScale);
    }

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

    private static float GetTopY(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        bool foundCollider = false;
        float topY = float.MinValue;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null || IsSnapMarkerTransform(c.transform))
            {
                continue;
            }

            if (!foundCollider)
            {
                topY = c.bounds.max.y;
                foundCollider = true;
            }
            else
            {
                topY = Mathf.Max(topY, c.bounds.max.y);
            }
        }

        if (foundCollider)
        {
            return topY;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bool foundRenderer = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || IsSnapMarkerTransform(r.transform))
            {
                continue;
            }

            if (!foundRenderer)
            {
                topY = r.bounds.max.y;
                foundRenderer = true;
            }
            else
            {
                topY = Mathf.Max(topY, r.bounds.max.y);
            }
        }

        if (foundRenderer)
        {
            return topY;
        }

        return target.transform.position.y;
    }

    private static float GetBottomY(GameObject target)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        bool foundCollider = false;
        float bottomY = float.MaxValue;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider c = colliders[i];
            if (c == null || IsSnapMarkerTransform(c.transform))
            {
                continue;
            }

            if (!foundCollider)
            {
                bottomY = c.bounds.min.y;
                foundCollider = true;
            }
            else
            {
                bottomY = Mathf.Min(bottomY, c.bounds.min.y);
            }
        }

        if (foundCollider)
        {
            return bottomY;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        bool foundRenderer = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || IsSnapMarkerTransform(r.transform))
            {
                continue;
            }

            if (!foundRenderer)
            {
                bottomY = r.bounds.min.y;
                foundRenderer = true;
            }
            else
            {
                bottomY = Mathf.Min(bottomY, r.bounds.min.y);
            }
        }

        if (foundRenderer)
        {
            return bottomY;
        }

        return target.transform.position.y;
    }

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

    private static void SetPreviewMode(GameObject target, bool isPreview)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = !isPreview;
        }
    }

    private static float GetBottomOffset(GameObject target)
    {
        if (!TryGetRenderableBoundsWithoutSnapMarkers(target, out Bounds bounds))
        {
            return 0f;
        }

        return target.transform.position.y - bounds.min.y;
    }

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

    private static bool IsSnapMarkerTransform(Transform transformToCheck)
    {
        return transformToCheck != null && transformToCheck.GetComponentInParent<SnapPoint>() != null;
    }

    private bool TryGetNearestSnapPointYNearReference(out float nearestY)
    {
        nearestY = 0f;
        Transform reference = floorHeightReference != null ? floorHeightReference : camera != null ? camera.transform : transform;
        if (reference == null)
        {
            return false;
        }

        float radius = Mathf.Max(0.01f, floorNearestPointRadius);
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

            if (_previewWall != null && snapPoint.transform.IsChildOf(_previewWall.transform))
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
