using System.Collections.Generic;
using UnityEngine;

// Controls Wall Snap Points behavior.
public class WallSnapPoints : MonoBehaviour
{
    private static readonly List<WallSnapPoints> InstancesInternal = new List<WallSnapPoints>(128);

    public static IReadOnlyList<WallSnapPoints> Instances => InstancesInternal;
    public static int Version { get; private set; }

    public SnapPoint[] snapPoints;

    private void OnEnable()
    {
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
}
