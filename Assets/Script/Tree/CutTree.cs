using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

public class CutTree : MonoBehaviour
{
    public string texttoshow;
    public Sprite sprite;
    public InfoHandler infoHandler;
    public List<GameObject> treeparts = new List<GameObject>();
    public GameObject topofthetree;
    [SerializeField] private float destroyDelaySeconds = 1f;
    public InventoryItem inventoryItem;
    public bool broken = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    private IEnumerator DestroyAfterSeconds(GameObject target, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (target != null)
        {
            Destroy(target);
        }
    }
    public void CutPart()
    {
        if (!broken)
        {
            if (treeparts.Count == 0)
            {
                MeshCollider meshCollider = topofthetree.GetComponent<MeshCollider>();
                meshCollider.convex = true;
                meshCollider.providesContacts = true;
                Rigidbody rigidbody = topofthetree.GetComponent<Rigidbody>();
                rigidbody.useGravity = true;
                StartCoroutine(DestroyAfterSeconds(topofthetree, destroyDelaySeconds));
                infoHandler.texttoshow = this.texttoshow;
                infoHandler.toshowimage = this.sprite;
                inventoryItem.slotManager.AddItem(inventoryItem);
                StartCoroutine(infoHandler.showinfo());
                broken = true;

            }
            else
            {

                GameObject treepart = treeparts[treeparts.Count - 1];
                Renderer renderer = treepart.GetComponent<MeshRenderer>();
                if (renderer == null)
                {
                    renderer = treepart.GetComponentInChildren<MeshRenderer>();
                }
                if (renderer == null)
                {
                    Debug.LogWarning($"CutTree: No Renderer found on {treepart.name}");
                    return;
                }
                renderer.enabled = false;
                StartCoroutine(DestroyAfterSeconds(treepart, destroyDelaySeconds));
                treeparts.RemoveAt(treeparts.Count - 1);
            }
        }
    }
}
