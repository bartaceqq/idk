using System; using Unity.Jobs.LowLevel.Unsafe; using UnityEngine; using UnityEngine.Rendering;
public class Optimalization : MonoBehaviour {
    [Header("When To Apply")]
    [Tooltip("If true, apply optimization settings automatically when the scene starts.")]
    [SerializeField] private bool applyOnStart = true;

    [Header("Performance")]
    [Tooltip("If true, removes frame cap by setting Application.targetFrameRate = -1.")]
    [SerializeField] private bool unlockFrameRate = true;
    [Tooltip("Set to 0 to disable frame cap. Example: 60 for 60 FPS target.")]
    [SerializeField] private int targetFrameRate = 0;

    [Tooltip("If true, VSync is disabled so targetFrameRate can control FPS.")]
    [SerializeField] private bool disableVSync = true;

    [Header("Application")]
    [Tooltip("If true, game keeps running when not focused (useful while testing).")]
    [SerializeField] private bool runInBackground = true;
    [SerializeField] private UnityEngine.ThreadPriority backgroundLoadingPriority = UnityEngine.ThreadPriority.High;

    [Header("CPU Usage")]
    [Tooltip("Lets Unity's job system use the maximum worker thread count available on this PC.")]
    [SerializeField] private bool maximizeJobWorkerThreads = true;
    [SerializeField, Range(0, 128)] private int jobWorkerCountOverride;
    [SerializeField] private bool raiseThreadPoolMinimums = true;

    [Header("Frame Diagnostics")]
    [SerializeField] private bool enforceRuntimeSettings = true;
    [SerializeField, Min(0.05f)] private float enforceInterval = 0.5f;
    [SerializeField] private bool showFrameOverlay = true;
    [SerializeField, Min(0.1f)] private float overlayRefreshInterval = 0.5f;

    private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
    private float _nextEnforceTime;
    private float _overlayTimer;
    private int _overlayFrames;
    private float _fps;
    private float _frameMs;
    private double _cpuFrameMs;
    private double _gpuFrameMs;
    private GUIStyle _overlayStyle;

    private void Start() {
        if (applyOnStart) { ApplyOptimizationSettings(); } }

    private void Update() {
        if (showFrameOverlay) {
            FrameTimingManager.CaptureFrameTimings();
            _overlayTimer += Time.unscaledDeltaTime;
            _overlayFrames++;

            if (_overlayTimer >= overlayRefreshInterval) {
                _fps = _overlayFrames / Mathf.Max(0.0001f, _overlayTimer);
                _frameMs = 1000f / Mathf.Max(0.01f, _fps);
                _overlayTimer = 0f;
                _overlayFrames = 0;
                if (FrameTimingManager.GetLatestTimings(1, _frameTimings) > 0) {
                    _cpuFrameMs = _frameTimings[0].cpuFrameTime;
                    _gpuFrameMs = _frameTimings[0].gpuFrameTime; } } }

        if (enforceRuntimeSettings && Time.unscaledTime >= _nextEnforceTime) {
            ApplyOptimizationSettings();
            _nextEnforceTime = Time.unscaledTime + enforceInterval; } }

    /// <summary>
    /// Applies simple performance-related settings.
    /// You can also call this manually from another script if needed.
    /// </summary>
    public void ApplyOptimizationSettings() {
        // Let targetFrameRate control FPS by turning VSync off.
        if (disableVSync) { QualitySettings.vSyncCount = 0; }

        // Limit or unlock FPS depending on the value.
        if (unlockFrameRate) {
            Application.targetFrameRate = -1; } else { Application.targetFrameRate = targetFrameRate <= 0 ? -1 : targetFrameRate; }

        Time.captureFramerate = 0;
        if (OnDemandRendering.renderFrameInterval != 1) { OnDemandRendering.renderFrameInterval = 1; }

        // Keep app running when window loses focus (optional).
        Application.runInBackground = runInBackground;
        Application.backgroundLoadingPriority = backgroundLoadingPriority;

        if (maximizeJobWorkerThreads) {
            int maxWorkers = Mathf.Max(1, JobsUtility.JobWorkerMaximumCount);
            JobsUtility.JobWorkerCount = jobWorkerCountOverride > 0
                ? Mathf.Clamp(jobWorkerCountOverride, 1, maxWorkers)
                : maxWorkers;
            if (raiseThreadPoolMinimums) {
                System.Threading.ThreadPool.GetMinThreads(out int minWorkers, out int minIo);
                int wantedWorkers = Mathf.Max(minWorkers, Mathf.Max(Environment.ProcessorCount, JobsUtility.JobWorkerCount + 1));
                System.Threading.ThreadPool.SetMinThreads(wantedWorkers, minIo); } } }

    private void OnGUI() {
        if (!showFrameOverlay) { return; }
        _overlayStyle ??= new GUIStyle(GUI.skin.box) {
            alignment = TextAnchor.UpperLeft,
            fontSize = 14,
            normal = { textColor = Color.white },
            padding = new RectOffset(10, 10, 8, 8) };
        GUI.Box(new Rect(12, 12, 390, 112),
            $"FPS {_fps:0}  frame {_frameMs:0.0} ms  {GetBoundText()}\n" +
            $"CPU frame {_cpuFrameMs:0.0} ms  GPU frame {_gpuFrameMs:0.0} ms\n" +
            $"Cap target={Application.targetFrameRate} vsync={QualitySettings.vSyncCount} renderInterval={OnDemandRendering.renderFrameInterval} capture={Time.captureFramerate}\n" +
            $"Job workers {JobsUtility.JobWorkerCount}/{JobsUtility.JobWorkerMaximumCount}",
            _overlayStyle); }

    private string GetBoundText() {
        if (_cpuFrameMs <= 0.01 || _gpuFrameMs <= 0.01) { return "timing warmup"; }
        if (_cpuFrameMs > _gpuFrameMs * 1.2) { return "CPU-bound"; }
        if (_gpuFrameMs > _cpuFrameMs * 1.2) { return "GPU-bound"; }
        return "balanced"; } }
