using System.Collections.Generic;
using UnityEngine;

// Controls Item behavior.
public class Item : MonoBehaviour
{
    public int ID;
    public string name;
    public KeyCode key;
    public GameObject itemobject;

    [Header("Item Presentation")]
    public bool keepVisibleWhenHolstered;
    public bool useDrawnTransformOverride;
    public Vector3 drawnLocalPosition;
    public Vector3 drawnLocalEulerAngles;
    public Vector3 drawnLocalScale = Vector3.one;
    public bool useHolsteredTransformOverride;
    public Vector3 holsteredLocalPosition;
    public Vector3 holsteredLocalEulerAngles;
    public Vector3 holsteredLocalScale = Vector3.one;
    public Transform holsteredParentOverride;

    private Transform _defaultParent;
    private int _defaultSiblingIndex;
    private Transform _resolvedHolsteredParent;

    private static readonly string[] HolsteredParentSearchNames =
    {
        "mixamorig:Spine2",
        "mixamorig:Spine1",
        "mixamorig:Spine",
        "Spine2",
        "Spine1",
        "Spine"
    };

    // Handle Should Remain Visible When Holstered.
    public bool ShouldRemainVisibleWhenHolstered()
    {
        return keepVisibleWhenHolstered && itemobject != null;
    }

    // Handle Apply Drawn Presentation.
    public void ApplyDrawnPresentation()
    {
        if (!EnsurePresentationTargets())
        {
            return;
        }

        Transform itemTransform = itemobject.transform;
        if (_defaultParent != null)
        {
            itemTransform.SetParent(_defaultParent, false);
            itemTransform.SetSiblingIndex(Mathf.Clamp(_defaultSiblingIndex, 0, itemTransform.parent.childCount - 1));
        }

        ApplyLocalTransformOverride(
            useDrawnTransformOverride,
            drawnLocalPosition,
            drawnLocalEulerAngles,
            drawnLocalScale);
    }

    // Handle Apply Holstered Presentation.
    public void ApplyHolsteredPresentation()
    {
        if (!EnsurePresentationTargets())
        {
            return;
        }

        Transform itemTransform = itemobject.transform;
        if (_resolvedHolsteredParent != null)
        {
            itemTransform.SetParent(_resolvedHolsteredParent, false);
        }

        ApplyLocalTransformOverride(
            useHolsteredTransformOverride,
            holsteredLocalPosition,
            holsteredLocalEulerAngles,
            holsteredLocalScale);
    }

    // Handle Ensure Presentation Targets.
    private bool EnsurePresentationTargets()
    {
        if (itemobject == null)
        {
            return false;
        }

        Transform itemTransform = itemobject.transform;
        if (_defaultParent == null)
        {
            _defaultParent = itemTransform.parent;
            _defaultSiblingIndex = itemTransform.GetSiblingIndex();
        }

        if (_resolvedHolsteredParent == null)
        {
            _resolvedHolsteredParent = ResolveHolsteredParent(itemTransform);
        }

        return true;
    }

    // Handle Resolve Holstered Parent.
    private Transform ResolveHolsteredParent(Transform itemTransform)
    {
        if (holsteredParentOverride != null)
        {
            return holsteredParentOverride;
        }

        Transform searchRoot = _defaultParent != null
            ? _defaultParent.root
            : itemTransform.root;
        if (searchRoot == null)
        {
            return null;
        }

        for (int i = 0; i < HolsteredParentSearchNames.Length; i++)
        {
            Transform resolved = FindDescendantByName(searchRoot, HolsteredParentSearchNames[i]);
            if (resolved != null)
            {
                return resolved;
            }
        }

        return null;
    }

    // Handle Find Descendant By Name.
    private static Transform FindDescendantByName(Transform root, string targetName)
    {
        if (root == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        Queue<Transform> queue = new Queue<Transform>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            Transform current = queue.Dequeue();
            if (current == null)
            {
                continue;
            }

            if (string.Equals(current.name, targetName, System.StringComparison.Ordinal))
            {
                return current;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                queue.Enqueue(current.GetChild(i));
            }
        }

        return null;
    }

    // Handle Apply Local Transform Override.
    private void ApplyLocalTransformOverride(bool shouldApply, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
    {
        if (!shouldApply || itemobject == null)
        {
            return;
        }

        Transform itemTransform = itemobject.transform;
        itemTransform.localPosition = localPosition;
        itemTransform.localEulerAngles = localEulerAngles;
        itemTransform.localScale = localScale;
    }
}
