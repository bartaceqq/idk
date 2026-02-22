using System.Collections;
using UnityEngine;

public class RayScript : MonoBehaviour
{
    public ParticleSystem stoneparticle;
    public ItemSwitchScript itemSwitchScript;
    public ActionScript actionScript;
    public Camera camera;
    public float range = 100f;
    public float sphereRadius = 0.25f;
    public LayerMask hitMask = ~0;
    public float cutDelaySeconds = 0.13f;
    public float swingCooldownSeconds = 1f;
    public float swordAttackCooldownSeconds = 2.5f;
    public float swordHitDelaySeconds = 1.10f;
    private float _nextSwingTime;
    public RadiusForAttackScript radiusForAttackScript;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= _nextSwingTime)
        {
            float cooldown = RayCheck();
            if (cooldown > 0f)
            {
                _nextSwingTime = Time.time + cooldown;
            }
        }
    }
    public float RayCheck()
    {
        int currentItemId = itemSwitchScript != null ? itemSwitchScript.currentitemid : 0;

        // Sword attack should be responsive even without a raycast hit.
        if (currentItemId == 3)
        {
            if (actionScript != null)
            {
                if (actionScript.staminaScript.SwordSwing())
                {
                    actionScript.Attack();
                    StartCoroutine(TriggerSwordAttackAfterDelay(swordHitDelaySeconds));
                }
            }

            return swordAttackCooldownSeconds;
        }

        if (camera == null)
        {
            camera = Camera.main;
            if (camera == null) return 0f;
        }

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool hitSomething = Physics.SphereCast(ray, sphereRadius, out hit, range, hitMask, QueryTriggerInteraction.Ignore);
        if (hitSomething)
        {
            Debug.Log(hit.collider.gameObject.name);
            switch (currentItemId)
            {
                case 1:
                    if (actionScript != null)
                    {
                        actionScript.Chop();
                    }

                    if (hit.collider.CompareTag("Tree") || hit.collider.transform.root.CompareTag("Tree"))
                    {

                        ColliderScript colliderScript = hit.collider.gameObject.GetComponent<ColliderScript>();
                        if (colliderScript == null)
                        {
                            colliderScript = hit.collider.gameObject.GetComponentInParent<ColliderScript>();
                        }
                        if (colliderScript != null)
                        {
                            Debug.Log("proslo");

                        }
                        StartCoroutine(TriggerAfterDelayAxe(colliderScript, cutDelaySeconds));


                    }

                    return swingCooldownSeconds;
                case 2:
                    if (actionScript != null)
                    {
                        actionScript.Mine();
                    }
                    if (hit.collider.CompareTag("Stone") || hit.collider.transform.root.CompareTag("Stone"))
                    {

                        StoneColliderScript stoneColliderScript = hit.collider.GetComponent<StoneColliderScript>();
                        MineStone mineStone = stoneColliderScript.mineStone;
                        if (mineStone != null)
                        {
                            Debug.Log("proslooooooo volle");
                        }

                        StartCoroutine(TriggerAfterDelayPixkaxe(mineStone, cutDelaySeconds));



                    }

                    return swingCooldownSeconds;

            }


        }

        return 0f;
    }


    private IEnumerator TriggerAfterDelayAxe(ColliderScript colliderScript, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (colliderScript != null)
        {
            colliderScript.Trigger();
        }
    }

    private IEnumerator TriggerAfterDelayPixkaxe(MineStone mineStone, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (mineStone != null)
        {
            mineStone.Mine();
        }
    }

    private IEnumerator TriggerSwordAttackAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (radiusForAttackScript != null)
        {
            radiusForAttackScript.Attack();
        }
    }
}
