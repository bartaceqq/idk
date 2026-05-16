using UnityEngine;

public class StoneColliderScript : MonoBehaviour
{
    public MineStone mineStone;

    // Initialize references before gameplay starts.
    private void Awake()
    {
        ResolveReference();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ResolveReference();
        }
    }

    private void ResolveReference()
    {
        if (mineStone == null)
        {
            mineStone = GetComponentInParent<MineStone>();
        }
    }

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
