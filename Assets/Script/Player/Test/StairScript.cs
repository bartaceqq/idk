using System; using System.Collections.Generic; using UnityEngine;
public class StairScript : MonoBehaviour {
    private static readonly List<StairScript> InstancesInternal = new List<StairScript>(64);

    public static IReadOnlyList<StairScript> Instances => InstancesInternal;

    public SnapPoint[] snapPoints = new SnapPoint[4];

    private void OnEnable() {
        if (!InstancesInternal.Contains(this)) { InstancesInternal.Add(this); } }

    private void OnDisable() { InstancesInternal.Remove(this); }

    // Run in the editor when values change in Inspector.
    private void OnValidate() {
        if (snapPoints == null || snapPoints.Length != 4) { Array.Resize(ref snapPoints, 4); } } }
