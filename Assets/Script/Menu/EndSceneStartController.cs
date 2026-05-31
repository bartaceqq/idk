using UnityEngine;

public sealed class EndSceneStartController : MonoBehaviour
{
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private string entryStateName = "mixamo_com";
    [SerializeField] private bool forceAlwaysAnimate = true;

    private void Start()
    {
        StartEntryAnimation();
    }

    private void StartEntryAnimation()
    {
        Animator animator = ResolveAnimator();
        if (animator == null) { return; }

        animator.enabled = true;
        animator.speed = 1f;
        if (forceAlwaysAnimate) { animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; }

        animator.Rebind();
        animator.Update(0f);

        int fullPathHash = Animator.StringToHash("Base Layer." + entryStateName);
        int shortNameHash = Animator.StringToHash(entryStateName);
        if (animator.HasState(0, fullPathHash)) { animator.Play(fullPathHash, 0, 0f); }
        else if (animator.HasState(0, shortNameHash)) { animator.Play(shortNameHash, 0, 0f); }

        animator.Update(0f);
    }

    private Animator ResolveAnimator()
    {
        if (characterAnimator != null) { return characterAnimator; }
        Animator[] animators = FindObjectsByType<Animator>(FindObjectsInactive.Include);
        for (int i = 0; i < animators.Length; i++)
        {
            Animator animator = animators[i];
            if (animator != null && animator.runtimeAnimatorController != null) { return animator; }
        }
        return null;
    }
}
