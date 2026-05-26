using System.Collections.Generic; using UnityEngine;
public class WallSnapPoints : MonoBehaviour {
    private static readonly List<WallSnapPoints> InstancesInternal = new List<WallSnapPoints>(128);

    public static IReadOnlyList<WallSnapPoints> Instances => InstancesInternal;

    public SnapPoint[] snapPoints;

    private void OnEnable() {
        if (!InstancesInternal.Contains(this)) { InstancesInternal.Add(this); } }

    private void OnDisable() { InstancesInternal.Remove(this); } }
