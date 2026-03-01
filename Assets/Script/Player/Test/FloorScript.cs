using System;
using UnityEngine;

// Controls Floor Script behavior.
public class FloorScript : MonoBehaviour
{
    public SnapPoint[] snapPoints = new SnapPoint[4];

    // Run in the editor when values change in Inspector.
    private void OnValidate()
    {
        if (snapPoints == null || snapPoints.Length != 4)
        {
            Array.Resize(ref snapPoints, 4);
        }
    }
}
