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
    private float _nextSwingTime;
   
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= _nextSwingTime)
        {
            _nextSwingTime = Time.time + swingCooldownSeconds;
            RayCheck();
        }
    }
    public void RayCheck()
    {
        if (camera == null)
        {
            camera = Camera.main;
            if (camera == null) return;
        }

        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        bool hitSomething = Physics.SphereCast(ray, sphereRadius, out hit, range, hitMask, QueryTriggerInteraction.Ignore);
        if (hitSomething)
        {
            Debug.Log(hit.collider.gameObject.name);
            switch (itemSwitchScript.currentitemid)
            {
                case 1:
                    actionScript.Chop();

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
                    
                    break;
                    case 2:
                    actionScript.Mine();
                     if (hit.collider.CompareTag("Stone") || hit.collider.transform.root.CompareTag("Stone"))
                    {

                        StoneColliderScript stoneColliderScript = hit.collider.GetComponent<StoneColliderScript>();
                        MineStone mineStone = stoneColliderScript.mineStone;
                        if(mineStone != null)
                        {
                            Debug.Log("proslooooooo volle");
                        }
                       
                            StartCoroutine(TriggerAfterDelayPixkaxe(mineStone, cutDelaySeconds));
                        

                        
                    }
                    break;
                    case 3:
                    actionScript.Attack();


                    break;

            }


        }
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
}
