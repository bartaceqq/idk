using UnityEngine;
public class RandomSkeletonScript : CustomEnemyAIBase {
    [Header("Skeleton References")] public SkeletonAnimatopmScript skeletonAnimationScript;

    protected override void Awake() {
        base.Awake();

        if (skeletonAnimationScript == null) {
            skeletonAnimationScript = GetComponent<SkeletonAnimatopmScript>();
            if (skeletonAnimationScript == null) { skeletonAnimationScript = GetComponentInChildren<SkeletonAnimatopmScript>(); } } }
    public void Attack() { TriggerEnemyAttack(); }

    protected override void OnEnemyAttack() {
        if (debugRangeLogs) { Debug.Log($"[Skeleton:{name}] ATTACK triggered", this); }

        if (skeletonAnimationScript != null) { skeletonAnimationScript.ThrowAnim(); } }

    protected override void SetWalkAnimation(bool status) {
        if (skeletonAnimationScript != null) { skeletonAnimationScript.MoveAnim(status); } } }
