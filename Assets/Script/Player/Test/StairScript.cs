using System;
using System.Collections.Generic;
using UnityEngine;

// Controls Stair Script behavior.
public class StairScript : MonoBehaviour
{
    private static readonly List<StairScript> InstancesInternal = new List<StairScript>(64);

    public static IReadOnlyList<StairScript> Instances => InstancesInternal;
    public static int Version { get; private set; }

    public SnapPoint[] snapPoints = new SnapPoint[4];

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

    // Run in the editor when values change in Inspector.
    private void OnValidate()
    {
        if (snapPoints == null || snapPoints.Length != 4)
        {
            Array.Resize(ref snapPoints, 4);
        }
    }
}
