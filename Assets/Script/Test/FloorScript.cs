using System;
using UnityEngine;

public class FloorScript : MonoBehaviour
{
    public SnapPoint[] snapPoints = new SnapPoint[4];

    private void OnValidate()
    {
        if (snapPoints == null || snapPoints.Length != 4)
        {
            Array.Resize(ref snapPoints, 4);
        }
    }
}
