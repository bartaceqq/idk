using UnityEngine;
using System.Collections.Generic;

public class RayCastScriptTest : MonoBehaviour
{
    public Camera camera;
    public float range = 100f;
    public float sphereRadius = 0.25f;
    public float scrollRotateStep = 10f;
    public float snapDistance = 1.25f;
    public LayerMask hitMask = ~0;
    public GameObject wall;

    private GameObject _previewWall;
    private float _previewYRotation;
    private readonly List<GameObject> _placedWalls = new List<GameObject>();

    void Start()
    {
        if (camera == null)
        {
            camera = Camera.main;
        }

        if (wall != null)
        {
            _previewWall = Instantiate(wall);
            _previewYRotation = _previewWall.transform.eulerAngles.y;
            SetPreviewMode(_previewWall, true);
        }
    }

    void Update()
    {
        if (camera == null)
        {
            camera = Camera.main;
        }

        if (camera == null || wall == null || _previewWall == null)
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
            PlaceWall();
        }
    }

    public void RayCheck()
    {
        Ray ray = camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            _previewWall.transform.rotation = Quaternion.Euler(0f, _previewYRotation, 0f);
            _previewWall.transform.position = GetSnappedPosition(hit.point, _previewWall.transform.rotation);
        }
    }

    private void PlaceWall()
    {
        GameObject placedWall = Instantiate(wall, _previewWall.transform.position, _previewWall.transform.rotation);
        SetPreviewMode(placedWall, false);
        _placedWalls.Add(placedWall);
    }

    private void SetPreviewMode(GameObject target, bool isPreview)
    {
        Collider[] colliders = target.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = !isPreview;
        }
    }

    private Vector3 GetSnappedPosition(Vector3 rawPosition, Quaternion previewRotation)
    {
        if (_placedWalls.Count == 0)
        {
            return rawPosition;
        }

        // Evaluate preview snap points at the candidate raw position/rotation.
        Vector3 previewLocalA;
        Vector3 previewLocalB;
        if (!TryGetLocalSnapPoints(_previewWall, out previewLocalA, out previewLocalB))
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
            if (!TryGetWorldSnapPoints(placed, out placedEndA, out placedEndB))
            {
                continue;
            }

            // Evaluate all 4 pairings and choose the truly closest snap.
            TrySnapPair(previewEndA, placedEndA, ref found, ref bestDistance, ref bestOffset);
            TrySnapPair(previewEndA, placedEndB, ref found, ref bestDistance, ref bestOffset);
            TrySnapPair(previewEndB, placedEndA, ref found, ref bestDistance, ref bestOffset);
            TrySnapPair(previewEndB, placedEndB, ref found, ref bestDistance, ref bestOffset);
        }

        return found ? rawPosition + bestOffset : rawPosition;
    }

    private void TrySnapPair(
        Vector3 previewEnd,
        Vector3 placedEnd,
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
    }

    private bool TryGetWorldSnapPoints(GameObject target, out Vector3 worldA, out Vector3 worldB)
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

    private bool TryGetLocalSnapPoints(GameObject target, out Vector3 localA, out Vector3 localB)
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
}
