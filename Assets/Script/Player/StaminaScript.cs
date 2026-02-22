using UnityEngine;
using UnityEngine.UI;

public class StaminaScript : MonoBehaviour
{
    public bool enoughstamina;
    public Image image;
    public float valuereduce = 0.01f;
    public float swordSwingCost = 0.5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (image != null)
        {
            enoughstamina = image.fillAmount > 0f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void AddStamina()
    {
        if (image == null)
        {
            enoughstamina = true;
            return;
        }

        if (image.fillAmount < 1f)
        {
            image.fillAmount += valuereduce;
        }
        if (image.fillAmount > 0f)
        {
            enoughstamina = true;
        }
        
    }
    public void ReduceStamina()
    {
        if (image == null)
        {
            enoughstamina = true;
            return;
        }

        if (image.fillAmount > 0f)
        {
            image.fillAmount -= valuereduce;
        }

        if (image.fillAmount <= 0f)
        {
            image.fillAmount = 0f;
            enoughstamina = false; 
        }

    }
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
