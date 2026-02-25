using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutTree : MonoBehaviour
{
    public string texttoshow;
    public Sprite sprite;
    public InfoHandler infoHandler;
    public List<GameObject> treeparts = new List<GameObject>();
    public GameObject topofthetree;
    [SerializeField] private float destroyDelaySeconds = 1f;
    [SerializeField] private float rebuildDelaySeconds = 30f;
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

    private struct TransformSnapshot
    {
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
    }

    private void Awake()
    {
        CacheInitialState();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    private void CacheInitialState()
    {
        initialTreeParts.Clear();
        initialTransforms.Clear();

        foreach (GameObject treePart in treeparts)
        {
            if (treePart == null)
            {
                continue;
            }

            initialTreeParts.Add(treePart);
            CacheTransform(treePart.transform);
        }

        if (topofthetree != null)
        {
            CacheTransform(topofthetree.transform);
            topRigidbody = topofthetree.GetComponent<Rigidbody>();
            topMeshCollider = topofthetree.GetComponent<MeshCollider>();

            if (topRigidbody != null)
            {
                topInitialUseGravity = topRigidbody.useGravity;
                topInitialIsKinematic = topRigidbody.isKinematic;
            }

            if (topMeshCollider != null)
            {
                topInitialConvex = topMeshCollider.convex;
                topInitialProvidesContacts = topMeshCollider.providesContacts;
            }
        }

        treeparts.Clear();
        treeparts.AddRange(initialTreeParts);
    }

    private void CacheTransform(Transform targetTransform)
    {
        if (targetTransform == null || initialTransforms.ContainsKey(targetTransform))
        {
            return;
        }

        initialTransforms[targetTransform] = new TransformSnapshot
        {
            localPosition = targetTransform.localPosition,
            localRotation = targetTransform.localRotation,
            localScale = targetTransform.localScale
        };
    }

    private void RestoreTransform(Transform targetTransform)
    {
        if (targetTransform == null || !initialTransforms.TryGetValue(targetTransform, out TransformSnapshot snapshot))
        {
            return;
        }

        targetTransform.localPosition = snapshot.localPosition;
        targetTransform.localRotation = snapshot.localRotation;
        targetTransform.localScale = snapshot.localScale;
    }

    private IEnumerator SetActiveAfterSeconds(GameObject target, float delaySeconds, bool active)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private IEnumerator RebuildTreeAfterSeconds(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        RebuildTree();
    }

    private void RebuildTree()
    {
        broken = false;
        isRebuilding = false;

        treeparts.Clear();
        treeparts.AddRange(initialTreeParts);

        foreach (GameObject treePart in initialTreeParts)
        {
            if (treePart == null)
            {
                continue;
            }

            RestoreTransform(treePart.transform);
            treePart.SetActive(true);

            Renderer renderer = treePart.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = treePart.GetComponentInChildren<MeshRenderer>(true);
            }
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }

        if (topofthetree != null)
        {
            RestoreTransform(topofthetree.transform);
            topofthetree.SetActive(true);
        }

        if (topRigidbody != null)
        {
            topRigidbody.linearVelocity = Vector3.zero;
            topRigidbody.angularVelocity = Vector3.zero;
            topRigidbody.useGravity = topInitialUseGravity;
            topRigidbody.isKinematic = topInitialIsKinematic;
        }

        if (topMeshCollider != null)
        {
            topMeshCollider.convex = topInitialConvex;
            topMeshCollider.providesContacts = topInitialProvidesContacts;
        }
    }

    public void CutPart()
    {
        if (broken || isRebuilding)
        {
            return;
        }

        if (treeparts.Count == 0)
        {
            if (topMeshCollider != null)
            {
                topMeshCollider.convex = true;
                topMeshCollider.providesContacts = true;
            }

            if (topRigidbody != null)
            {
                topRigidbody.useGravity = true;
                topRigidbody.isKinematic = false;
            }

            StartCoroutine(SetActiveAfterSeconds(topofthetree, destroyDelaySeconds, false));
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
                infoHandler.ShowInfoNow(texttoshow, sprite);
            }
            else
            {
                Debug.LogWarning($"{name}: Missing InfoHandler reference.", this);
            }

            broken = true;
            isRebuilding = true;
            StartCoroutine(RebuildTreeAfterSeconds(destroyDelaySeconds + rebuildDelaySeconds));
        }
        else
        {
            GameObject treepart = treeparts[treeparts.Count - 1];
            if (treepart == null)
            {
                treeparts.RemoveAt(treeparts.Count - 1);
                return;
            }

            Renderer renderer = treepart.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                renderer = treepart.GetComponentInChildren<MeshRenderer>();
            }
            if (renderer == null)
            {
                Debug.LogWarning($"CutTree: No Renderer found on {treepart.name}");
            }
            else
            {
                renderer.enabled = false;
            }
            StartCoroutine(SetActiveAfterSeconds(treepart, destroyDelaySeconds, false));
            treeparts.RemoveAt(treeparts.Count - 1);
        }
    }
}
