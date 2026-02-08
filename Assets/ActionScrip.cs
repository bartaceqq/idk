using UnityEngine;

public class ActionScript : MonoBehaviour
{
    
    public string currentutil;
    public MovementAnimationScript movementAnimationScript;
    public AxeAnimationScript axeAnimationScript;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Input.GetMouseButton(0))
        {
           
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
   
    
    public void Chop()
    {
        axeAnimationScript.ChopAnimation();
    }
    public void Walk(bool status)
    {
        
        
         movementAnimationScript.WalkAnimation_Foreward(status);
        
    }
    public void Idle(bool status)
    {
        movementAnimationScript.IdleAnimation(status);
    }

}

