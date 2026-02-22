using UnityEngine;

public class ZombieAnimationScript : MonoBehaviour
{
    public Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ThrowAnim()
    {
        animator.SetTrigger("Throw");
    }
    public void MoveAnim(bool status)
    {
        animator.SetBool("Walking", status);
    }
}
