using UnityEngine;

public class ColliderScript : MonoBehaviour
{
    public CutTree cutTree;

    private void Awake()
    {
        if (cutTree == null)
        {
            cutTree = GetComponentInParent<CutTree>();
        }
    }

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
