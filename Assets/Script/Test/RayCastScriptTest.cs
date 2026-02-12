using UnityEngine;
using System.Collections.Generic;

public class RayCastScriptTest : MonoBehaviour
{
    private enum SnapSide
    {
        None,
        A,
        B
    }

    private enum BuildMode
    {
        Wall,
        Stair,
        Floor
    }

    public Camera camera;
    public float range = 100f;
    public float sphereRadius = 0.25f;
    public float scrollRotateStep = 10f;
    public float snapDistance = 1.25f;
    public float stairRotationTolerance = 10f;
    public LayerMask hitMask = ~0;
    public GameObject wall;
    public GameObject stair;
    public GameObject floor;

    private BuildMode _buildMode = BuildMode.Wall;
    private GameObject _previewObject;
    private float _previewYRotation;
    private readonly List<GameObject> _placedWalls = new List<GameObject>();
    private readonly List<GameObject> _placedFloors = new List<GameObject>();
    private SnapSide _currentSnapSide = SnapSide.None;

    void Start()
    {
        if (camera == null)
        {
            camera = Camera.main;
        }

        RebuildPreview();
    }

    void Update()
    {
        if (camera == null)
        {
            camera = Camera.main;
        }

        if (Input.GetMouseButtonDown(1))
        {
            ToggleBuildMode();
        }

        if (camera == null || _previewObject == null)
        {
            return;
        }

        float scroll = Mathf.Clamp(Input.mouseScrollDelta.y, -1f, 1f);
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _previewYRotation += scroll * scrollRotateStep;
        }

        RayCheck();

        if (Input.GetMouseButtonDown(0))
        {
            PlaceCurrent();
        }
    }

    public void RayCheck()
    {
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            Quaternion previewRotation = Quaternion.Euler(0f, _previewYRotation, 0f);
            _previewObject.transform.rotation = previewRotation;
            if (_buildMode == BuildMode.Wall)
            {
                _previewObject.transform.position = GetWallSnappedPosition(hit.point, previewRotation);
            }
            else if (_buildMode == BuildMode.Stair)
            {
                _previewObject.transform.position = GetStairSnappedPosition(hit.point, previewRotation);
            }
            else
            {
                _previewObject.transform.position = GetFloorSnappedPosition(hit.point, previewRotation);
            }
        }
    }

    private void PlaceCurrent()
    {
        GameObject prefab = GetCurrentPrefab();
        if (prefab == null || _previewObject == null)
        {
            return;
        }

        GameObject placedObject = Instantiate(prefab, _previewObject.transform.position, _previewObject.transform.rotation);
        SetPreviewMode(placedObject, false);

        if (_buildMode == BuildMode.Floor)
        {
            _placedFloors.Add(placedObject);
            return;
        }

        if (_buildMode != BuildMode.Wall)
        {
            return;
        }

        _placedWalls.Add(placedObject);

        WallSnapPoints placedSnapPoints = placedObject.GetComponent<WallSnapPoints>();
        if (placedSnapPoints == null)
        {
            return;
        }

        if (_currentSnapSide == SnapSide.A && placedSnapPoints.logA != null)
        {
            Destroy(placedSnapPoints.logA);
        }
        else if (_currentSnapSide == SnapSide.B && placedSnapPoints.logB != null)
        {
            Destroy(placedSnapPoints.logB);
        }
    }

    private void SetPreviewMode(GameObject target, bool isPreview)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = !isPreview;
        }
    }

    private void ToggleBuildMode()
    {
        if (_buildMode == BuildMode.Wall)
        {
            _buildMode = BuildMode.Stair;
        }
        else if (_buildMode == BuildMode.Stair)
        {
            _buildMode = BuildMode.Floor;
        }
        else
        {
            _buildMode = BuildMode.Wall;
        }
        RebuildPreview();
    }

    private void RebuildPreview()
    {
        Vector3 spawnPosition = _previewObject != null ? _previewObject.transform.position : Vector3.zero;
        Quaternion spawnRotation = _previewObject != null ? _previewObject.transform.rotation : Quaternion.identity;

        if (_previewObject != null)
        {
            Destroy(_previewObject);
            _previewObject = null;
        }

        GameObject prefab = GetCurrentPrefab();
        if (prefab == null)
        {
            return;
        }

        _previewObject = Instantiate(prefab, spawnPosition, spawnRotation);
        _previewYRotation = _previewObject.transform.eulerAngles.y;
        SetPreviewMode(_previewObject, true);
        SetLogVisibility(true, true);
    }

    private GameObject GetCurrentPrefab()
    {
        if (_buildMode == BuildMode.Wall)
        {
            return wall;
        }

        if (_buildMode == BuildMode.Stair)
        {
            return stair;
        }

        return floor;
    }

    private Vector3 GetWallSnappedPosition(Vector3 rawPosition, Quaternion previewRotation)
    {
        _currentSnapSide = SnapSide.None;
        SetLogVisibility(true, true);

        if (_placedWalls.Count == 0)
        {
            return rawPosition;
        }

        // Evaluate preview snap points at the candidate raw position/rotation.
        Vector3 previewLocalA;
        Vector3 previewLocalB;
        if (!TryGetWallLocalSnapPoints(_previewObject, out previewLocalA, out previewLocalB))
        {
            return rawPosition;
        }
        Vector3 previewEndA = rawPosition + (previewRotation * previewLocalA);
        Vector3 previewEndB = rawPosition + (previewRotation * previewLocalB);

        bool found = false;
        float bestDistance = float.MaxValue;
        Vector3 bestOffset = Vector3.zero;

        for (int i = 0; i < _placedWalls.Count; i++)
        {
            GameObject placed = _placedWalls[i];
            if (placed == null) continue;

            Vector3 placedEndA;
            Vector3 placedEndB;
            if (!TryGetWallWorldSnapPoints(placed, out placedEndA, out placedEndB))
            {
                continue;
            }

            // Evaluate all 4 pairings and choose the truly closest snap.
            TrySnapPair(previewEndA, placedEndA, true, ref found, ref bestDistance, ref bestOffset);
            TrySnapPair(previewEndA, placedEndB, true, ref found, ref bestDistance, ref bestOffset);
            TrySnapPair(previewEndB, placedEndA, false, ref found, ref bestDistance, ref bestOffset);
            TrySnapPair(previewEndB, placedEndB, false, ref found, ref bestDistance, ref bestOffset);
        }

        if (!found)
        {
            SetLogVisibility(true, true);
        }

        return found ? rawPosition + bestOffset : rawPosition;
    }

    private Vector3 GetStairSnappedPosition(Vector3 rawPosition, Quaternion previewRotation)
    {
        SetLogVisibility(true, true);
        float stairBaseY = GetStairBaseY();

        Vector3 previewLocalA;
        Vector3 previewLocalB;
        if (_placedWalls.Count == 0 || !TryGetStairLocalSnapPoints(_previewObject, out previewLocalA, out previewLocalB))
        {
            return new Vector3(rawPosition.x, stairBaseY, rawPosition.z);
        }

        Vector3 previewEndA = rawPosition + (previewRotation * previewLocalA);
        Vector3 previewEndB = rawPosition + (previewRotation * previewLocalB);

        bool found = false;
        float bestDistance = float.MaxValue;
        Vector3 bestOffset = Vector3.zero;

        for (int i = 0; i < _placedWalls.Count; i++)
        {
            GameObject placedWall = _placedWalls[i];
            if (placedWall == null)
            {
                continue;
            }

            if (!HasMatchingYRotation(previewRotation, placedWall.transform.rotation, stairRotationTolerance))
            {
                continue;
            }

            Vector3 wallEndA;
            Vector3 wallEndB;
            if (!TryGetWallWorldSnapPoints(placedWall, out wallEndA, out wallEndB))
            {
                continue;
            }

            // Stair only snaps when BOTH endpoints are close to wall endpoints.
            TrySnapDualPairXZ(previewEndA, previewEndB, wallEndA, wallEndB, ref found, ref bestDistance, ref bestOffset);
            TrySnapDualPairXZ(previewEndA, previewEndB, wallEndB, wallEndA, ref found, ref bestDistance, ref bestOffset);
        }

        if (!found)
        {
            return new Vector3(rawPosition.x, stairBaseY, rawPosition.z);
        }

        return new Vector3(rawPosition.x + bestOffset.x, stairBaseY, rawPosition.z + bestOffset.z);
    }

    private Vector3 GetFloorSnappedPosition(Vector3 rawPosition, Quaternion previewRotation)
    {
        SetLogVisibility(true, true);

        Vector3[] previewLocalPoints;
        if (!TryGetFloorLocalSnapPoints(_previewObject, out previewLocalPoints))
        {
            return rawPosition;
        }

        bool found = false;
        float bestDistance = float.MaxValue;
        Vector3 bestOffset = Vector3.zero;

        for (int i = 0; i < _placedWalls.Count; i++)
        {
            GameObject placedWall = _placedWalls[i];
            if (placedWall == null)
            {
                continue;
            }

            Vector3 wallFloorA;
            Vector3 wallFloorB;
            if (!TryGetWallFloorWorldSnapPoints(placedWall, out wallFloorA, out wallFloorB))
            {
                continue;
            }

            Vector3[] wallTargets = { wallFloorA, wallFloorB };
            for (int p = 0; p < previewLocalPoints.Length; p++)
            {
                Vector3 previewWorld = rawPosition + (previewRotation * previewLocalPoints[p]);
                for (int t = 0; t < wallTargets.Length; t++)
                {
                    TrySnapPair(previewWorld, wallTargets[t], false, ref found, ref bestDistance, ref bestOffset);
                }
            }
        }

        for (int i = 0; i < _placedFloors.Count; i++)
        {
            GameObject placedFloor = _placedFloors[i];
            if (placedFloor == null)
            {
                continue;
            }

            Vector3[] floorTargets;
            if (!TryGetFloorWorldSnapPoints(placedFloor, out floorTargets))
            {
                continue;
            }

            for (int p = 0; p < previewLocalPoints.Length; p++)
            {
                Vector3 previewWorld = rawPosition + (previewRotation * previewLocalPoints[p]);
                for (int t = 0; t < floorTargets.Length; t++)
                {
                    TrySnapPair(previewWorld, floorTargets[t], false, ref found, ref bestDistance, ref bestOffset);
                }
            }
        }

        return found ? rawPosition + bestOffset : rawPosition;
    }

    private void TrySnapPair(
        Vector3 previewEnd,
        Vector3 placedEnd,
        bool isPreviewA,
        ref bool found,
        ref float bestDistance,
        ref Vector3 bestOffset)
    {
        float distance = Vector3.Distance(previewEnd, placedEnd);
        if (distance > snapDistance || distance >= bestDistance)
        {
            return;
        }

        found = true;
        bestDistance = distance;
        bestOffset = placedEnd - previewEnd;
        if (isPreviewA)
        {
            _currentSnapSide = SnapSide.A;
            SetLogVisibility(false, true);
        }
        else
        {
            _currentSnapSide = SnapSide.B;
            SetLogVisibility(true, false);
        }
    }

    private void TrySnapDualPairXZ(
        Vector3 previewA,
        Vector3 previewB,
        Vector3 wallA,
        Vector3 wallB,
        ref bool found,
        ref float bestDistance,
        ref Vector3 bestOffset)
    {
        float deltaAx = wallA.x - previewA.x;
        float deltaAz = wallA.z - previewA.z;
        float deltaBx = wallB.x - previewB.x;
        float deltaBz = wallB.z - previewB.z;

        float distA = Mathf.Sqrt((deltaAx * deltaAx) + (deltaAz * deltaAz));
        float distB = Mathf.Sqrt((deltaBx * deltaBx) + (deltaBz * deltaBz));
        if (distA > snapDistance || distB > snapDistance)
        {
            return;
        }

        // Use the worse of the two distances as the score so both ends must be good.
        float pairScore = Mathf.Max(distA, distB);
        if (pairScore >= bestDistance)
        {
            return;
        }

        found = true;
        bestDistance = pairScore;
        bestOffset = new Vector3((deltaAx + deltaBx) * 0.5f, 0f, (deltaAz + deltaBz) * 0.5f);
    }

    private void SetLogVisibility(bool showLogA, bool showLogB)
    {
        WallSnapPoints snapPoints = _previewObject != null ? _previewObject.GetComponent<WallSnapPoints>() : null;
        if (snapPoints == null)
        {
            return;
        }

        if (snapPoints.logA != null)
        {
            snapPoints.logA.SetActive(showLogA);
        }

        if (snapPoints.logB != null)
        {
            snapPoints.logB.SetActive(showLogB);
        }
    }

    private bool TryGetWallWorldSnapPoints(GameObject target, out Vector3 worldA, out Vector3 worldB)
    {
        WallSnapPoints snapPoints = target.GetComponent<WallSnapPoints>();
        if (snapPoints != null && snapPoints.snapA != null && snapPoints.snapB != null)
        {
            worldA = snapPoints.snapA.position;
            worldB = snapPoints.snapB.position;
            return true;
        }

        worldA = Vector3.zero;
        worldB = Vector3.zero;
        return false;
    }

    private bool TryGetWallLocalSnapPoints(GameObject target, out Vector3 localA, out Vector3 localB)
    {
        WallSnapPoints snapPoints = target.GetComponent<WallSnapPoints>();
        if (snapPoints != null && snapPoints.snapA != null && snapPoints.snapB != null)
        {
            localA = target.transform.InverseTransformPoint(snapPoints.snapA.position);
            localB = target.transform.InverseTransformPoint(snapPoints.snapB.position);
            return true;
        }

        localA = Vector3.zero;
        localB = Vector3.zero;
        return false;
    }

    private bool TryGetStairLocalSnapPoints(GameObject target, out Vector3 localA, out Vector3 localB)
    {
        StairScript stairScript = target.GetComponent<StairScript>();
        if (stairScript != null && stairScript.snaptopa != null && stairScript.snaptopb != null)
        {
            localA = target.transform.InverseTransformPoint(stairScript.snaptopa.transform.position);
            localB = target.transform.InverseTransformPoint(stairScript.snaptopb.transform.position);
            return true;
        }

        localA = Vector3.zero;
        localB = Vector3.zero;
        return false;
    }

    private bool TryGetFloorLocalSnapPoints(GameObject target, out Vector3[] localPoints)
    {
        FloorScript floorScript = target.GetComponent<FloorScript>();
        if (floorScript != null &&
            floorScript.snapA != null &&
            floorScript.snapB != null &&
            floorScript.snapC != null &&
            floorScript.snapD != null)
        {
            localPoints = new Vector3[4];
            localPoints[0] = target.transform.InverseTransformPoint(floorScript.snapA.position);
            localPoints[1] = target.transform.InverseTransformPoint(floorScript.snapB.position);
            localPoints[2] = target.transform.InverseTransformPoint(floorScript.snapC.position);
            localPoints[3] = target.transform.InverseTransformPoint(floorScript.snapD.position);
            return true;
        }

        localPoints = null;
        return false;
    }

    private bool TryGetFloorWorldSnapPoints(GameObject target, out Vector3[] worldPoints)
    {
        FloorScript floorScript = target.GetComponent<FloorScript>();
        if (floorScript != null &&
            floorScript.snapA != null &&
            floorScript.snapB != null &&
            floorScript.snapC != null &&
            floorScript.snapD != null)
        {
            worldPoints = new Vector3[4];
            worldPoints[0] = floorScript.snapA.position;
            worldPoints[1] = floorScript.snapB.position;
            worldPoints[2] = floorScript.snapC.position;
            worldPoints[3] = floorScript.snapD.position;
            return true;
        }

        worldPoints = null;
        return false;
    }

    private bool TryGetWallFloorWorldSnapPoints(GameObject target, out Vector3 worldA, out Vector3 worldB)
    {
        WallSnapPoints snapPoints = target.GetComponent<WallSnapPoints>();
        if (snapPoints != null && snapPoints.snapfloorA != null && snapPoints.snapFloor != null)
        {
            worldA = snapPoints.snapfloorA.position;
            worldB = snapPoints.snapFloor.position;
            return true;
        }

        worldA = Vector3.zero;
        worldB = Vector3.zero;
        return false;
    }

    private float GetStairBaseY()
    {
        return stair != null ? stair.transform.position.y : 0f;
    }

    private bool HasMatchingYRotation(Quaternion a, Quaternion b, float toleranceDegrees)
    {
        float yA = a.eulerAngles.y;
        float yB = b.eulerAngles.y;
        float delta = Mathf.Abs(Mathf.DeltaAngle(yA, yB));
        return delta <= toleranceDegrees;
    }
}
