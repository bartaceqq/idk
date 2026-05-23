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
    public float axeSwingCost = 0.12f;
    public float pickaxeSwingCost = 0.15f;
    public float staminaRechargeDelaySeconds = 0.35f;
    public float minimumStaminaThreshold = 0.001f;

    private float _staminaRechargeBlockedUntil;

    void Start()
    {
        if (valueadd < 0f)
        {
            valueadd = valuereduce;
        }

        UpdateEnoughStaminaState();
    }
// Handle Add Stamina.
    public void AddStamina()
    {
        if (image == null)
        {
            enoughstamina = true;
            return;
        }

        if (Time.time < _staminaRechargeBlockedUntil)
        {
            UpdateEnoughStaminaState();
            return;
        }

        float delta = Mathf.Abs(valueadd) * Time.deltaTime;
        image.fillAmount = Mathf.Clamp01(image.fillAmount + delta);
        UpdateEnoughStaminaState();
        
    }
    // Handle Reduce Stamina.
    public void ReduceStamina()
    {
        if (image == null)
        {
            enoughstamina = true;
            return;
        }

        float delta = Mathf.Abs(valuereduce) * Time.deltaTime;
        image.fillAmount = Mathf.Clamp01(image.fillAmount - delta);
        UpdateEnoughStaminaState();

    }
    // Handle Sword Swing.
    public bool SwordSwing()
    {
        return TryConsumeStamina(swordSwingCost);
    }

    // Handle Axe Swing.
    public bool AxeSwing()
    {
        return TryConsumeStamina(axeSwingCost);
    }

    // Handle Pickaxe Swing.
    public bool PickaxeSwing()
    {
        return TryConsumeStamina(pickaxeSwingCost);
    }

    // Handle Try Consume Stamina.
    public bool TryConsumeStamina(float amount)
    {
        return TryConsumeStamina(amount, staminaRechargeDelaySeconds);
    }

    // Handle Try Consume Stamina With Delay.
    public bool TryConsumeStamina(float amount, float rechargeDelaySeconds)
    {
        if (image == null)
        {
            enoughstamina = true;
            return true;
        }

        float clampedAmount = Mathf.Max(0f, amount);
        if (clampedAmount <= 0f)
        {
            BlockStaminaRegeneration(rechargeDelaySeconds);
            UpdateEnoughStaminaState();
            return true;
        }

        if ((image.fillAmount + minimumStaminaThreshold) < clampedAmount)
        {
            UpdateEnoughStaminaState();
            return false;
        }

        image.fillAmount = Mathf.Clamp01(image.fillAmount - clampedAmount);
        BlockStaminaRegeneration(rechargeDelaySeconds);
        UpdateEnoughStaminaState();
        return true;
    }

    // Handle Block Stamina Regeneration.
    public void BlockStaminaRegeneration(float delaySeconds)
    {
        if (image == null)
        {
            enoughstamina = true;
            return;
        }

        float blockedUntil = Time.time + Mathf.Max(0f, delaySeconds);
        if (blockedUntil > _staminaRechargeBlockedUntil)
        {
            _staminaRechargeBlockedUntil = blockedUntil;
        }
    }

    // Handle Update Enough Stamina State.
    private void UpdateEnoughStaminaState()
    {
        enoughstamina = image == null || image.fillAmount > minimumStaminaThreshold;
    }
}

