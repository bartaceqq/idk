using UnityEngine;
public class PickaxeAnimationScript : MonoBehaviour
{
    public Animator animator; public void Mine() { animator.SetTrigger("Mine"); }
}
