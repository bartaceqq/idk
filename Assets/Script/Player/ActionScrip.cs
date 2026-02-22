using UnityEngine;

public class ActionScript : MonoBehaviour
{
    public bool enoughstamina;
    public StaminaScript staminaScript; 
    public string currentutil;
    public MovementAnimationScript movementAnimationScript;
    public AxeAnimationScript axeAnimationScript;
    public PickaxeAnimationScript pickaxeAnimationScript;
    public SwordAnimationScript swordAnimationScript;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
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
    public void Mine()
    {
        pickaxeAnimationScript.Mine();
    }
    public void Attack()
    {
        swordAnimationScript.Attack();
    }
    

}
