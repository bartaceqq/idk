using UnityEngine; using UnityEngine.UI;
public class StaminaScript : MonoBehaviour {
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

    void Start() {
        if (valueadd < 0f) { valueadd = valuereduce; }

        UpdateEnoughStaminaState(); }
    public void AddStamina() {
        if (image == null) {
            enoughstamina = true;
            return; }

        if (Time.time < _staminaRechargeBlockedUntil) {
            UpdateEnoughStaminaState();
            return; }

        float delta = Mathf.Abs(valueadd) * Time.deltaTime;
        image.fillAmount = Mathf.Clamp01(image.fillAmount + delta);
        UpdateEnoughStaminaState();
        
    }
    public void ReduceStamina() {
        if (image == null) {
            enoughstamina = true;
            return; }

        float delta = Mathf.Abs(valuereduce) * Time.deltaTime;
        image.fillAmount = Mathf.Clamp01(image.fillAmount - delta);
        UpdateEnoughStaminaState();

    }
    public bool SwordSwing() { return TryConsumeStamina(swordSwingCost); }
    public bool AxeSwing() { return TryConsumeStamina(axeSwingCost); }
    public bool PickaxeSwing() { return TryConsumeStamina(pickaxeSwingCost); }
    public bool TryConsumeStamina(float amount) { return TryConsumeStamina(amount, staminaRechargeDelaySeconds); }
    public bool TryConsumeStamina(float amount, float rechargeDelaySeconds) {
        if (image == null) {
            enoughstamina = true;
            return true; }

        float clampedAmount = Mathf.Max(0f, amount);
        if (clampedAmount <= 0f) {
            BlockStaminaRegeneration(rechargeDelaySeconds);
            UpdateEnoughStaminaState();
            return true; }

        if ((image.fillAmount + minimumStaminaThreshold) < clampedAmount) {
            UpdateEnoughStaminaState();
            return false; }

        image.fillAmount = Mathf.Clamp01(image.fillAmount - clampedAmount);
        BlockStaminaRegeneration(rechargeDelaySeconds);
        UpdateEnoughStaminaState();
        return true; }
    public void BlockStaminaRegeneration(float delaySeconds) {
        if (image == null) {
            enoughstamina = true;
            return; }

        float blockedUntil = Time.time + Mathf.Max(0f, delaySeconds);
        if (blockedUntil > _staminaRechargeBlockedUntil) { _staminaRechargeBlockedUntil = blockedUntil; } }
    private void UpdateEnoughStaminaState() { enoughstamina = image == null || image.fillAmount > minimumStaminaThreshold; } }

