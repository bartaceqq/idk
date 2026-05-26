using System; using System.Collections.Generic; using UnityEngine;
public class FloorScript : MonoBehaviour {
    private static readonly List<FloorScript> InstancesInternal = new List<FloorScript>(128);

    public static IReadOnlyList<FloorScript> Instances => InstancesInternal;

    public SnapPoint[] snapPoints = new SnapPoint[4];

    private void OnEnable() {
        if (!InstancesInternal.Contains(this)) { InstancesInternal.Add(this); } }

    private void OnDisable() { InstancesInternal.Remove(this); }

    // Run in the editor when values change in Inspector.
    private void OnValidate() {
        if (snapPoints == null || snapPoints.Length != 4) { Array.Resize(ref snapPoints, 4); } } }
