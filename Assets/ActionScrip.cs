using UnityEngine;

public class ActionScript : MonoBehaviour
{
    public bool enoughstamina;
    public StaminaScript staminaScript; 
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
    public void Sprint(bool status)
    {
        
        
         movementAnimationScript.RunAnimation_Foreward(status);

        if (status)
        {
            staminaScript.ReduceStamina();
        }
        else
        {
            staminaScript.AddStamina();
        }
        
    }
    public void Idle(bool status)
    {
        movementAnimationScript.IdleAnimation(status);
    }
    

}

