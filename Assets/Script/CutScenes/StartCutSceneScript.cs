using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
public class StartCutSceneScript : MonoBehaviour
{
    public static bool IntroCutsceneFinished { get; private set; } = true;
    public static bool IsIntroCutsceneBlockingGameplay => activeIntroCutscene != null && !IntroCutsceneFinished;
    private static StartCutSceneScript activeIntroCutscene;
    private static bool skipNextIntroCutscenes;
    [Header("Cutscenes")] public PlayableDirector playableDirectorcrashing;
    public PlayableDirector lookingoutaftercrash; public bool skipAllCutscenes;
    public bool skipFirstCutscene; public bool skipSecondCutscene;
    [Header("Camera Setup")] public Transform cameraToUnparentBeforeSecondTimeline;
    public GameObject cutsceneCameraObject; public GameObject normalPlayerCameraObject;
    [Header("After Cutscene")] public GameObject AirPlaneToDisable;
    public UnityEvent onCutscenesFinished; public LookingController lookingController;
    public GameObject capsnorm; public GameObject capsbuild; public GameObject canvas;
    [Header("Persistent Crash VFX")] public Transform crashVfxRoot;
    public bool restartCrashVfxAfterTimeline = true;
    public AudioSource backgroundMusic;
    private bool canvasWasActive;
    private bool cutsceneModeApplied;

    private void Awake()
    {
        activeIntroCutscene = this;
        IntroCutsceneFinished = false;
    }
    private void OnDestroy()
    {
        if (activeIntroCutscene == this) { activeIntroCutscene = null; }
    }
    private void Start()
    {
        Time.timeScale = 1f;
        canvasWasActive = canvas != null && canvas.activeSelf;

        if (skipNextIntroCutscenes)
        {
            skipNextIntroCutscenes = false;
            skipAllCutscenes = true;
        }

        if (skipAllCutscenes)
        {
            OnCutscenesFinished(); return;
        }

        SetCutsceneMode();
        StartCoroutine(PlayCutscenesInOrder());
    }
    private IEnumerator PlayCutscenesInOrder()
    {
        if (!skipFirstCutscene) { yield return PlayCutscene(playableDirectorcrashing); ResumeCrashVfx(); }
        if (!skipSecondCutscene)
        {
            UnparentCameraForSecondCutscene();
            yield return PlayCutscene(lookingoutaftercrash);
            ResumeCrashVfx();
        }
        OnCutscenesFinished();
    }
    public static void SkipNextIntroCutscenes()
    {
        skipNextIntroCutscenes = true;
        IntroCutsceneFinished = true;
    }
    private IEnumerator PlayCutscene(PlayableDirector director)
    {
        if (director == null || director.playableAsset == null) { yield break; }
        director.Play();
        while (director.state == PlayState.Playing) { yield return null; }
    }
    private void UnparentCameraForSecondCutscene()
    {
        if (cameraToUnparentBeforeSecondTimeline == null || cameraToUnparentBeforeSecondTimeline.parent == null) { return; }
        cameraToUnparentBeforeSecondTimeline.SetParent(null, true);
    }
    public void OnCutscenesFinished()
    {
        IntroCutsceneFinished = true;
        ResumeCrashVfx();
        if (AirPlaneToDisable != null) { AirPlaneToDisable.SetActive(false); }
        if (cutsceneCameraObject != null) { cutsceneCameraObject.SetActive(false); }
        if (normalPlayerCameraObject != null) { normalPlayerCameraObject.SetActive(true); }
        onCutscenesFinished?.Invoke(); SetGameplayMode();
        if (backgroundMusic != null && !backgroundMusic.isPlaying) { backgroundMusic.Play(); }
    }
    private void SetCutsceneMode()
    {
        cutsceneModeApplied = true;
        if (cutsceneCameraObject != null) { cutsceneCameraObject.SetActive(true); }
        if (normalPlayerCameraObject != null) { normalPlayerCameraObject.SetActive(false); }
        if (capsnorm != null) { capsnorm.SetActive(false); }
        if (capsbuild != null) { capsbuild.SetActive(false); }
        if (canvas != null) { canvas.SetActive(false); }
        if (lookingController != null) { lookingController.enabled = false; }
    }
    private void SetGameplayMode()
    {
        if (capsnorm != null) { capsnorm.SetActive(true); }
        if (capsbuild != null) { capsbuild.SetActive(false); }
        if (canvas != null && cutsceneModeApplied) { canvas.SetActive(canvasWasActive); }
        if (lookingController != null)
        {
            lookingController.enabled = true; lookingController.switched = false;
        }
    }
    private void ResumeCrashVfx()
    {
        if (!restartCrashVfxAfterTimeline) { return; }
        Transform root = crashVfxRoot;
        if (root == null)
        {
            GameObject crashSite = GameObject.Find("CrashSiteDefaultPlane");
            if (crashSite != null) { root = crashSite.transform; }
        }
        ParticleSystem[] particles = root != null
            ? root.GetComponentsInChildren<ParticleSystem>(true)
            : FindObjectsByType<ParticleSystem>(FindObjectsInactive.Include);
        for (int i = 0; i < particles.Length; i++)
        {
            ParticleSystem particle = particles[i];
            if (particle == null || !IsCrashVfxParticle(particle)) { continue; }
            particle.gameObject.SetActive(true);
            ParticleSystem.MainModule main = particle.main;
            main.loop = true;
            if (main.simulationSpeed <= 0f) { main.simulationSpeed = 1f; }
            particle.Clear(true);
            particle.Play(true);
        }
    }
    private static bool IsCrashVfxParticle(ParticleSystem particle)
    {
        string objectName = particle.name;
        return objectName.StartsWith("VFX_Fire") || objectName.StartsWith("VFX_Smoke") ||
        objectName.StartsWith("VFX_BlackSmoke");
    }
}
