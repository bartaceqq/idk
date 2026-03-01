using System.Collections.Generic;
using UnityEngine;

// Controls Axe Animation Script behavior.
public class AxeAnimationScript : MonoBehaviour
{
   
    public Animator axeanimator;
    // Handle Chop Animation.
    public void ChopAnimation()
    {
        
        axeanimator.SetTrigger("Swing");
    }
}

