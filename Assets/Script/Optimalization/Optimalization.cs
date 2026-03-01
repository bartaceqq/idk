using UnityEngine;

// Controls Optimalization behavior.
public class Optimalization : MonoBehaviour
{
    [Header("When To Apply")]
    [Tooltip("If true, apply optimization settings automatically when the scene starts.")]
    [SerializeField] private bool applyOnStart = true;

    [Header("Performance")]
    [Tooltip("Set to 0 to disable frame cap. Example: 60 for 60 FPS target.")]
    [SerializeField] private int targetFrameRate = 60;

    [Tooltip("If true, VSync is disabled so targetFrameRate can control FPS.")]
    [SerializeField] private bool disableVSync = true;

    [Header("Application")]
    [Tooltip("If true, game keeps running when not focused (useful while testing).")]
    [SerializeField] private bool runInBackground = true;

    // Run setup once before the first frame.
    private void Start()
    {
        if (applyOnStart)
        {
            ApplyOptimizationSettings();
        }
    }

    /// <summary>
    /// Applies simple performance-related settings.
    /// You can also call this manually from another script if needed.
    /// </summary>
    public void ApplyOptimizationSettings()
    {
        // Let targetFrameRate control FPS by turning VSync off.
        if (disableVSync)
        {
            QualitySettings.vSyncCount = 0;
        }

        // Limit or unlock FPS depending on the value.
        Application.targetFrameRate = targetFrameRate <= 0 ? -1 : targetFrameRate;

        // Keep app running when window loses focus (optional).
        Application.runInBackground = runInBackground;
    }
}
