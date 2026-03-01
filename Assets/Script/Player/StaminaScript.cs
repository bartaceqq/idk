using UnityEngine;
using UnityEngine.UI;

// Controls Stamina Script behavior.
public class StaminaScript : MonoBehaviour
{
    public bool enoughstamina;
    public Image image;
    public float valuereduce = 0.6f;
    public float valueadd = -1f;
    public float swordSwingCost = 0.5f;
    void Start()
    {
        if (valueadd < 0f)
        {
            valueadd = valuereduce;
        }

        if (image != null)
        {
            enoughstamina = image.fillAmount > 0f;
        }
    }
// Handle Add Stamina.
    public void AddStamina()
    {
        if (image == null)
        {
            enoughstamina = true;
            return;
        }

        float delta = valueadd * Time.deltaTime;
        image.fillAmount = Mathf.Clamp01(image.fillAmount + delta);
        enoughstamina = image.fillAmount > 0f;
        
    }
    // Handle Reduce Stamina.
    public void ReduceStamina()
    {
        if (image == null)
        {
            enoughstamina = true;
            return;
        }

        float delta = valuereduce * Time.deltaTime;
        image.fillAmount = Mathf.Clamp01(image.fillAmount - delta);
        enoughstamina = image.fillAmount > 0f;

    }
    // Handle Sword Swing.
    public bool SwordSwing()
    {
        if (image == null)
        {
            return false;
        }

        if (image.fillAmount >= swordSwingCost)
        {
            image.fillAmount -= swordSwingCost;
            if (image.fillAmount < 0f)
            {
                image.fillAmount = 0f;
            }

            enoughstamina = image.fillAmount > 0f;
            return true;
        }

        enoughstamina = image.fillAmount > 0f;
        return false;
    }
}

