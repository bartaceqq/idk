using System.Collections.Generic;
using UnityEngine;

// Controls Snap Point behavior.
public class SnapPoint : MonoBehaviour
{
    private static readonly List<SnapPoint> InstancesInternal = new List<SnapPoint>(512);

    public static IReadOnlyList<SnapPoint> Instances => InstancesInternal;
    public static int Version { get; private set; }

    private void OnEnable()
    {
        DisableMarkerColliders();

        if (!InstancesInternal.Contains(this))
        {
            InstancesInternal.Add(this);
            Version++;
        }
    }

    private void OnDisable()
    {
        if (InstancesInternal.Remove(this))
        {
            Version++;
        }
    }

    private void DisableMarkerColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }
    }
}
