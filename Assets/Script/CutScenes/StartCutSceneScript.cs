using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class StartCutSceneScript : MonoBehaviour
{
    [Header("Timelines")]
    public PlayableDirector playableDirectorcrashing;
    public PlayableDirector lookingoutaftercrash;

    [Header("Objects")]
    public GameObject canvas;
    public GameObject Player;
    public GameObject AirPlaneToDisable;

    [Header("Camera Fix For Timeline 2")]
    public Transform cameraToUnparentBeforeSecondTimeline;
    public GameObject normalPlayerCameraObject;
    public LookingController playerLookingController;

    private readonly List<Behaviour> disabledScripts = new List<Behaviour>();
    private bool canvasWasActiveBeforeCutscene;
    private bool isCutsceneRunning;
    private float oldTimeScale = 1f;
    private float oldFixedDeltaTime = 0.02f;

    private void Start()
    {
        StartCoroutine(PlayStartCutscene());
    }

    private void OnDisable()
    {
        if (isCutsceneRunning)
        {
            // If this object is disabled in the middle of cutscene, we still restore game state.
            RestoreGameAfterCutscene();
            isCutsceneRunning = false;
        }
    }

    private IEnumerator PlayStartCutscene()
    {
        if (isCutsceneRunning)
        {
            yield break;
        }

        isCutsceneRunning = true;
        PrepareGameForCutscene();

        // 1) Play the crash timeline and wait until it ends.
        yield return PlayTimelineAndWait(playableDirectorcrashing);

        // 2) Camera fix: remove camera parent before second timeline so parent transform does not affect it.
        UnparentCameraForSecondTimeline();

        // 3) Play second timeline and wait until it ends.
        yield return PlayTimelineAndWait(lookingoutaftercrash);

        // 4) After both timelines are done, hide/disable the airplane object.
        if (AirPlaneToDisable != null)
        {
            AirPlaneToDisable.SetActive(false);
        }

        // 5) Force camera/player back to normal gameplay view.
        SwitchToNormalGameplayView();

        RestoreGameAfterCutscene();
        isCutsceneRunning = false;
    }

    private void PrepareGameForCutscene()
    {
        disabledScripts.Clear();

        // Pause normal gameplay time so physics/update based gameplay cannot interrupt the cutscene.
        oldTimeScale = Time.timeScale;
        oldFixedDeltaTime = Time.fixedDeltaTime;
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;

        // Hide gameplay canvas during cutscene.
        canvasWasActiveBeforeCutscene = canvas != null && canvas.activeSelf;
        if (canvasWasActiveBeforeCutscene)
        {
            canvas.SetActive(false);
        }

        // Disable player control scripts so player cannot move, attack, switch items, etc.
        if (Player != null)
        {
            DisableScripts(Player.GetComponentsInChildren<PlayerInput>(true));
            DisableScripts(Player.GetComponentsInChildren<FPSController>(true));
            DisableScripts(Player.GetComponentsInChildren<LookingController>(true));
            DisableScripts(Player.GetComponentsInChildren<RayScript>(true));
            DisableScripts(Player.GetComponentsInChildren<ItemSwitchScript>(true));
            DisableScripts(Player.GetComponentsInChildren<ActionScript>(true));
        }

        // Disable enemy AI scripts so enemies cannot run during cutscene.
        DisableScripts(FindObjectsByType<RandomZombieScript>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
        DisableScripts(FindObjectsByType<RandomSkeletonScript>(FindObjectsInactive.Exclude, FindObjectsSortMode.None));
    }

    private void RestoreGameAfterCutscene()
    {
        // Re-enable only scripts that this script disabled.
        for (int i = 0; i < disabledScripts.Count; i++)
        {
            if (disabledScripts[i] != null)
            {
                disabledScripts[i].enabled = true;
            }
        }
        disabledScripts.Clear();

        // Show gameplay canvas again if it was active before cutscene.
        if (canvas != null && canvasWasActiveBeforeCutscene)
        {
            canvas.SetActive(true);
        }

        // Restore normal gameplay time.
        Time.timeScale = oldTimeScale;
        Time.fixedDeltaTime = oldFixedDeltaTime;

        // Safety: if cutscene is interrupted, stop timelines and restore normal update mode.
        if (playableDirectorcrashing != null)
        {
            playableDirectorcrashing.Stop();
            playableDirectorcrashing.timeUpdateMode = DirectorUpdateMode.GameTime;
        }

        if (lookingoutaftercrash != null)
        {
            lookingoutaftercrash.Stop();
            lookingoutaftercrash.timeUpdateMode = DirectorUpdateMode.GameTime;
        }
    }

    private IEnumerator PlayTimelineAndWait(PlayableDirector director)
    {
        if (director == null || director.playableAsset == null)
        {
            yield break;
        }

        // Use unscaled time so timeline still plays when Time.timeScale is 0.
        DirectorUpdateMode oldMode = director.timeUpdateMode;
        director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;

        director.Play();

        while (director.state == PlayState.Playing)
        {
            yield return null;
        }

        director.timeUpdateMode = oldMode;
    }

    private void SwitchToNormalGameplayView()
    {
        // Disable the cutscene camera if it exists.
        if (cameraToUnparentBeforeSecondTimeline != null)
        {
            Camera cutsceneCam = cameraToUnparentBeforeSecondTimeline.GetComponent<Camera>();
            if (cutsceneCam != null)
            {
                cutsceneCam.enabled = false;
            }

            cameraToUnparentBeforeSecondTimeline.gameObject.SetActive(false);
        }

        // Make sure normal player mode is active (normal capsule on, building capsule off).
        ForceNormalPlayerMode();

        // Turn on the normal gameplay camera.
        if (normalPlayerCameraObject != null)
        {
            normalPlayerCameraObject.SetActive(true);
            Camera normalCam = normalPlayerCameraObject.GetComponent<Camera>();
            if (normalCam != null)
            {
                normalCam.enabled = true;
            }
        }
        else
        {
            Camera fallbackCam = FindNormalPlayerCamera();
            if (fallbackCam != null)
            {
                fallbackCam.gameObject.SetActive(true);
                fallbackCam.enabled = true;
            }
        }
    }

    private void UnparentCameraForSecondTimeline()
    {
        if (cameraToUnparentBeforeSecondTimeline == null)
        {
            return;
        }

        if (cameraToUnparentBeforeSecondTimeline.parent == null)
        {
            return;
        }

        // true = keep same world position/rotation when removing parent.
        cameraToUnparentBeforeSecondTimeline.SetParent(null, true);
    }

    private void ForceNormalPlayerMode()
    {
        if (playerLookingController == null && Player != null)
        {
            playerLookingController = Player.GetComponentInChildren<LookingController>(true);
        }

        if (playerLookingController == null)
        {
            return;
        }

        GameObject normalCapsule = playerLookingController.normalcapsule;
        GameObject buildingCapsule = playerLookingController.buildingcapsule;

        if (normalCapsule == null || buildingCapsule == null)
        {
            return;
        }

        // Keep player position/rotation from whichever capsule is currently active.
        GameObject sourceCapsule = normalCapsule.activeInHierarchy ? normalCapsule : buildingCapsule;
        Vector3 sharedPosition = sourceCapsule.transform.position;
        Quaternion sharedRotation = sourceCapsule.transform.rotation;

        normalCapsule.transform.position = sharedPosition;
        normalCapsule.transform.rotation = sharedRotation;
        normalCapsule.SetActive(true);
        buildingCapsule.SetActive(false);

        playerLookingController.switched = false;
        if (playerLookingController.animator != null)
        {
            playerLookingController.animator.enabled = true;
        }
    }

    private Camera FindNormalPlayerCamera()
    {
        if (playerLookingController == null && Player != null)
        {
            playerLookingController = Player.GetComponentInChildren<LookingController>(true);
        }

        if (playerLookingController != null && playerLookingController.normalcapsule != null)
        {
            Camera cam = playerLookingController.normalcapsule.GetComponentInChildren<Camera>(true);
            if (cam != null)
            {
                return cam;
            }
        }

        if (Player != null)
        {
            return Player.GetComponentInChildren<Camera>(true);
        }

        return null;
    }

    private void DisableScripts(Behaviour[] scripts)
    {
        if (scripts == null)
        {
            return;
        }

        for (int i = 0; i < scripts.Length; i++)
        {
            Behaviour script = scripts[i];
            if (script == null || !script.enabled)
            {
                continue;
            }

            script.enabled = false;
            disabledScripts.Add(script);
        }
    }
}
