using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Controls Cut Tree behavior.
public class CutTree : MonoBehaviour
{
    private static InfoHandler cachedInfoHandler;
    private static SlotManager cachedSlotManager;

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

    // Initialize references before gameplay starts.
    private void Awake()
    {
        ResolveReferences();
        CacheInitialState();
    }

    // Run in the editor when values change in Inspector.
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ResolveReferences();
        }
    }

    // Handle Resolve References.
    private void ResolveReferences()
    {
        if (inventoryItem == null)
        {
            inventoryItem = GetComponent<InventoryItem>();
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

        if (inventoryItem == null)
        {
            return;
        }

        inventoryItem.ResolveReferences();

        if (inventoryItem.slotManager == null)
        {
            if (cachedSlotManager == null)
            {
                cachedSlotManager = FindSlotManagerInScene();
            }

            inventoryItem.slotManager = cachedSlotManager;
        }
        else
        {
            cachedSlotManager = inventoryItem.slotManager;
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

    // Handle Find Slot Manager In Scene.
    private static SlotManager FindSlotManagerInScene()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<SlotManager>(FindObjectsInactive.Include);
#else
        return FindObjectOfType<SlotManager>(true);
#endif
    }

    // Handle Cache Initial State.
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

    // Handle Cache Transform.
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

    // Handle Restore Transform.
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

    // Handle Set Tree Part Visible.
    private static void SetTreePartVisible(GameObject treePart, bool visible)
    {
        if (treePart == null)
        {
            return;
        }

        Renderer[] renderers = treePart.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = visible;
            }
        }
    }

    // Handle Set Active After Seconds.
    private IEnumerator SetActiveAfterSeconds(GameObject target, float delaySeconds, bool active)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    // Handle Rebuild Tree After Seconds.
    private IEnumerator RebuildTreeAfterSeconds(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        RebuildTree();
    }

    // Handle Rebuild Tree.
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
            SetTreePartVisible(treePart, true);
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

    // Handle Cut Part.
    public void CutPart()
    {
        ResolveReferences();

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

            SetTreePartVisible(treepart, false);
            StartCoroutine(SetActiveAfterSeconds(treepart, destroyDelaySeconds, false));
            treeparts.RemoveAt(treeparts.Count - 1);
        }
    }
}
