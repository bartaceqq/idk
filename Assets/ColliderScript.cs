using UnityEngine;

public class ColliderScript : MonoBehaviour
{
    public CutTree cutTree;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Trigger()
    {
        cutTree.CutPart();
    }
}
