using System.Collections;
using UnityEngine;

// Controls Ray Script behavior.
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
    private float _nextAxeSwingTime;
    private float _nextPickaxeSwingTime;
    private bool _axeSoundPlayedThisSwing;
    public RadiusForAttackScript radiusForAttackScript;

    [Header("Weapon Sounds")]
    public AudioSource axeaudiosource;
    public AudioSource pickaxeAudioSource;
    public AudioSource swordAudioSource;

    [Header("Sound Delays")]
    public float pickaxeSoundDelaySeconds = 0.1f;
    public float swordSoundDelaySeconds = 0.1f;

    private void Update()
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
    // Handle Ray Check.
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
                    PlaySoundAtSwingStart(swordAudioSource, swordSoundDelaySeconds);
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
                    if (Time.time < _nextAxeSwingTime)
                    {
                        return _nextAxeSwingTime - Time.time;
                    }

                    // A new axe animation cycle starts here.
                    _nextAxeSwingTime = Time.time + swingCooldownSeconds;
                    _axeSoundPlayedThisSwing = false;

                    if (actionScript != null)
                    {
                        actionScript.Chop();
                        PlayAxeSoundOncePerSwing();
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
                    if (Time.time < _nextPickaxeSwingTime)
                    {
                        return _nextPickaxeSwingTime - Time.time;
                    }

                    _nextPickaxeSwingTime = Time.time + swingCooldownSeconds;

                    if (actionScript != null)
                    {
                        actionScript.Mine();
                        PlaySoundAtSwingStart(pickaxeAudioSource, pickaxeSoundDelaySeconds);
                    }
                    if (hit.collider.CompareTag("Stone") || hit.collider.transform.root.CompareTag("Stone"))
                    {
                        StoneColliderScript stoneColliderScript = hit.collider.GetComponent<StoneColliderScript>();
                        if (stoneColliderScript == null)
                        {
                            stoneColliderScript = hit.collider.GetComponentInParent<StoneColliderScript>();
                        }

                        if (stoneColliderScript != null && stoneColliderScript.mineStone != null)
                        {
                            Debug.Log("proslooooooo volle");
                            StartCoroutine(TriggerAfterDelayPixkaxe(stoneColliderScript.mineStone, cutDelaySeconds));
                        }
                        else
                        {
                            Debug.LogWarning("Stone hit but MineStone reference is missing on hit object/parent.");
                        }



                    }

                    return swingCooldownSeconds;

            }


        }

        return 0f;
    }


    // Handle Trigger After Delay Axe.
    private IEnumerator TriggerAfterDelayAxe(ColliderScript colliderScript, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (colliderScript != null)
        {
            colliderScript.Trigger();
        }
    }

    // Handle Trigger After Delay Pixkaxe.
    private IEnumerator TriggerAfterDelayPixkaxe(MineStone mineStone, float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (mineStone != null)
        {
            mineStone.Mine();
        }
    }

    // Handle Trigger Sword Attack After Delay.
    private IEnumerator TriggerSwordAttackAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        if (radiusForAttackScript != null)
        {
            radiusForAttackScript.Attack();
        }
    }

    // Handle Play Sound At Swing Start.
    private void PlaySoundAtSwingStart(AudioSource source, float delaySeconds)
    {
        if (source == null)
        {
            return;
        }

        // New swing should be heard from the beginning, so override previous playback.
        source.Stop();

        if (delaySeconds > 0f)
        {
            source.PlayDelayed(delaySeconds);
        }
        else
        {
            source.Play();
        }
    }

    // Play axe sound only once for the current axe animation cycle.
    // It resets when a new axe swing starts.
    private void PlayAxeSoundOncePerSwing()
    {
        if (_axeSoundPlayedThisSwing || axeaudiosource == null)
        {
            return;
        }

        _axeSoundPlayedThisSwing = true;
        PlaySoundAtSwingStart(axeaudiosource, 0f);
    }
}

