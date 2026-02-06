using System.Collections.Generic;
using UnityEngine;

public class CutTree : MonoBehaviour
{
    public List<GameObject> treeparts = new List<GameObject>();
    public GameObject topofthetree;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void CutPart()
    {
        if (treeparts.Count == 0)
        {
            Rigidbody rigidbody = topofthetree.GetComponent<Rigidbody>();
            rigidbody.useGravity = true;
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
            treeparts.RemoveAt(treeparts.Count - 1);
        }
    }
}
