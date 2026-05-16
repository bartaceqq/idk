using System.Collections.Generic;
using UnityEngine;

public sealed class RuntimeBuildPiece : MonoBehaviour
{
    private static readonly List<RuntimeBuildPiece> InstancesInternal = new List<RuntimeBuildPiece>(256);

    public static IReadOnlyList<RuntimeBuildPiece> Instances => InstancesInternal;

    public BuildPieceKind kind;

    public static RuntimeBuildPiece Mark(GameObject target, BuildPieceKind pieceKind)
    {
        if (target == null)
        {
            return null;
        }

        RuntimeBuildPiece piece = target.GetComponent<RuntimeBuildPiece>();
        if (piece == null)
        {
            piece = target.AddComponent<RuntimeBuildPiece>();
        }

        piece.kind = pieceKind;
        return piece;
    }

    private void OnEnable()
    {
        if (!InstancesInternal.Contains(this))
        {
            InstancesInternal.Add(this);
        }
    }

    private void OnDisable()
    {
        InstancesInternal.Remove(this);
    }
}
