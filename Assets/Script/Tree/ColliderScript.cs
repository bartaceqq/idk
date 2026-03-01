using UnityEngine;

// Controls Collider Script behavior.
public class ColliderScript : MonoBehaviour
{
    public CutTree cutTree;

    // Initialize references before gameplay starts.
    private void Awake()
    {
        if (cutTree == null)
        {
            cutTree = GetComponentInParent<CutTree>();
        }
    }

    // Handle Trigger.
    public void Trigger()
    {
        if (cutTree != null)
        {
            cutTree.CutPart();
            return;
        }

        Debug.LogWarning($"{name}: Missing CutTree reference.", this);
    }
}
