using UnityEngine;

public class XPTest : MonoBehaviour
{
    public XPHandler xPHandler;

    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            xPHandler.AddXP(20);
        }    
    }
}
