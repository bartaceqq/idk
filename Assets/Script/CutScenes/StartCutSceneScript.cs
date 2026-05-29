using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
public class StartCutSceneScript : MonoBehaviour
{
    [Header("Cutscenes")] public PlayableDirector playableDirectorcrashing;
    public PlayableDirector lookingoutaftercrash; public bool skipAllCutscenes;
    public bool skipFirstCutscene; public bool skipSecondCutscene;
    [Header("Camera Setup")] public Transform cameraToUnparentBeforeSecondTimeline;
    public GameObject cutsceneCameraObject; public GameObject normalPlayerCameraObject;
    [Header("After Cutscene")] public GameObject AirPlaneToDisable;
    public UnityEvent onCutscenesFinished; public LookingController lookingController;
    public GameObject capsnorm; public GameObject capsbuild; public GameObject canvas;
    public AudioSource backgroundMusic; private void Start()
    {
        if (skipAllCutscenes)
        {
            OnCutscenesFinished(); return;
        }
        SetCutsceneMode();
        StartCoroutine(PlayCutscenesInOrder());
    }
    private IEnumerator PlayCutscenesInOrder()
    {
        if (!skipFirstCutscene) { yield return PlayCutscene(playableDirectorcrashing); }
        if (!skipSecondCutscene)
        {
            UnparentCameraForSecondCutscene();
            yield return PlayCutscene(lookingoutaftercrash);
        }
        OnCutscenesFinished();
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
        if (AirPlaneToDisable != null) { AirPlaneToDisable.SetActive(false); }
        if (cutsceneCameraObject != null) { cutsceneCameraObject.SetActive(false); }
        if (normalPlayerCameraObject != null) { normalPlayerCameraObject.SetActive(true); }
        onCutscenesFinished?.Invoke(); SetGameplayMode();
        if (backgroundMusic != null && !backgroundMusic.isPlaying) { backgroundMusic.Play(); }
    }
    private void SetCutsceneMode()
    {
        if (capsnorm != null) { capsnorm.SetActive(false); }
        if (capsbuild != null) { capsbuild.SetActive(false); }
        if (lookingController != null) { lookingController.enabled = false; }
    }
    private void SetGameplayMode()
    {
        if (capsnorm != null) { capsnorm.SetActive(true); }
        if (capsbuild != null) { capsbuild.SetActive(false); }
        if (lookingController != null)
        {
            lookingController.enabled = true; lookingController.switched = false;
        }
    }
}
