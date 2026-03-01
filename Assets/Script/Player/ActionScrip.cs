using UnityEngine;

// Controls Action Script behavior.
public class ActionScript : MonoBehaviour
{
    public bool enoughstamina;
    public StaminaScript staminaScript; 
    public string currentutil;
    public MovementAnimationScript movementAnimationScript;
    public AxeAnimationScript axeAnimationScript;
    public PickaxeAnimationScript pickaxeAnimationScript;
    public SwordAnimationScript swordAnimationScript;
   
    
    // Handle Chop.
    public void Chop()
    {
        axeAnimationScript.ChopAnimation();
    }
    // Handle Walk.
    public void Walk(bool status)
    {
        
        
         movementAnimationScript.WalkAnimation_Foreward(status);
        
    }
    // Handle Sprint.
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
    // Handle Idle.
    public void Idle(bool status)
    {
        movementAnimationScript.IdleAnimation(status);
    }
    // Handle Mine.
    public void Mine()
    {
        pickaxeAnimationScript.Mine();
    }
    // Handle Attack.
    public void Attack()
    {
        swordAnimationScript.Attack();
    }
    

}

