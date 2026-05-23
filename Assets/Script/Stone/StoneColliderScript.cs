using UnityEngine;

// Controls Stone Collider Script behavior.
public class StoneColliderScript : MonoBehaviour
{
    public MineStone mineStone;

    // Initialize references before gameplay starts.
    private void Awake()
    {
        ResolveReference();
    }

    // Run in the editor when values change in Inspector.
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ResolveReference();
        }
    }

    // Handle Resolve Reference.
    private void ResolveReference()
    {
        if (mineStone == null)
        {
            mineStone = GetComponentInParent<MineStone>();
        }
    }

    // Handle Trigger.
    public void Trigger()
    {
        ResolveReference();
        if (mineStone != null)
        {
            mineStone.Mine();
        }
        else
        {
            Debug.LogWarning($"{name}: Missing MineStone reference.", this);
        }
    }
}
