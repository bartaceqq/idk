using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TestHitting : MonoBehaviour
{
    [Header("Time Tracking")]
    public float currentInsideSeconds;
    public float totalInsideSeconds;
    public List<float> insideHistorySeconds = new List<float>();
    [SerializeField] private int maxHistoryEntries = 100;

    [Header("Visuals")]
    public Renderer meshRenderer;
    public Material hitmat;

    [Header("Detection")]
    [SerializeField] private bool usePollingFallback = true;
    [SerializeField] private bool includeInactiveSwordObjects = false;
    [SerializeField] private float swordScanInterval = 0.35f;
    [SerializeField] private bool useNameFallback = true;

    [Header("Attack Animation Gate")]
    [SerializeField] private bool requireActiveSwordAttackAnimation = true;
    [SerializeField] private ActionScript attackingActionScript;
    [SerializeField] private float actionScriptScanInterval = 0.5f;

    [Header("Slow Motion")]
    [SerializeField] private bool slowTimeWhileInside = false;
    [SerializeField] [Range(0.01f, 1f)] private float insideTimeScale = 0.3f;
    [SerializeField] private bool smoothTimeScaleTransitions = true;
    [SerializeField] [Min(0.01f)] private float timeScaleBlendSpeed = 8f;

    [Header("Camera Focus")]
    [SerializeField] private bool focusCameraOnHit = false;
    [SerializeField] private Camera focusCamera;
    [SerializeField] private float cameraFocusRotateSpeed = 9f;
    [SerializeField] [Min(0.01f)] private float cameraFocusPointSmoothSpeed = 16f;
    [SerializeField] private bool changeFovWhileFocusing = true;
    [SerializeField] [Range(1f, 179f)] private float focusedCameraFov = 45f;
    [SerializeField] private float cameraFovLerpSpeed = 10f;
    [SerializeField] private float cameraReturnRotateSpeed = 6f;
    [SerializeField] private Vector3 focusPointOffset = new Vector3(0f, 0.08f, 0f);

    [Header("Hit Trail Tint")]
    [SerializeField] private bool tintHitTrails = true;
    [SerializeField] private Color hitTrailColor = new Color(0.35f, 0f, 0f, 0.9f);

    [Header("Hit Stroke")]
    [SerializeField] private bool drawHitStroke = true;
    [SerializeField] private Color hitStrokeColor = new Color(0.32f, 0.02f, 0.02f, 0.95f);
    [SerializeField] [Min(0.001f)] private float hitStrokeWidth = 0.04f;
    [SerializeField] [Min(0.05f)] private float hitStrokeLifetime = 1.1f;
    [SerializeField] [Min(0.001f)] private float hitStrokePointSpacing = 0.01f;
    [SerializeField] [Min(0f)] private float hitStrokeSurfaceOffset = 0.018f;
    [SerializeField] [Min(0f)] private float hitStrokeRestartGap = 0.16f;

    private Material _originalMaterial;
    private Collider _selfCollider;
    private readonly List<Collider> _swordColliders = new List<Collider>();
    private readonly List<Renderer> _swordRenderers = new List<Renderer>();
    private readonly HashSet<Collider> _touchingSwordColliders = new HashSet<Collider>();
    private readonly HashSet<Renderer> _overlappingSwordRenderers = new HashSet<Renderer>();
    private float _nextSwordScanTime;
    private float _nextActionScriptScanTime;
    private bool _isUsingHitMaterial;
    private bool _wasSwordInsideLastFrame;
    private readonly List<ActionScript> _knownActionScripts = new List<ActionScript>();
    private int _anyAttackStateFrame = -1;
    private bool _anySwordAttackActiveThisFrame;
    private float _defaultFixedDeltaTime;
    private float _timeScaleBeforeSlow = 1f;
    private bool _timeSlowActive;
    private bool _swordInsideThisFrame;
    private bool _hasFocusPointThisFrame;
    private Vector3 _focusPointThisFrame;
    private bool _cameraWasFocusing;
    private float _cameraBaseFov;
    private Quaternion _cameraBaseRotation;
    private readonly Dictionary<SwordTrailEffect, Color> _originalTrailColors = new Dictionary<SwordTrailEffect, Color>();
    private readonly HashSet<SwordTrailEffect> _desiredTintTrails = new HashSet<SwordTrailEffect>();
    private readonly List<SwordTrailEffect> _trailRestoreBuffer = new List<SwordTrailEffect>();
    private readonly List<Vector3> _hitStrokePoints = new List<Vector3>();
    private GameObject _hitStrokeObject;
    private LineRenderer _hitStrokeRenderer;
    private Material _hitStrokeMaterial;
    private bool _hitStrokeActive;
    private float _lastHitStrokeContactTime = float.NegativeInfinity;
    private readonly List<float> _hitStrokePointTimes = new List<float>();
    private bool _hasSmoothedFocusPoint;
    private Vector3 _smoothedFocusPoint;

    private void Awake()
    {
        _selfCollider = GetComponent<Collider>();
        _defaultFixedDeltaTime = Time.fixedDeltaTime;
        ResolveMeshRendererIfNeeded();
        CaptureOriginalMaterial();
    }

    public void ConfigureHitTarget(Renderer targetRenderer, Material hitMaterial)
    {
        if (targetRenderer != null)
        {
            meshRenderer = targetRenderer;
        }

        ResolveMeshRendererIfNeeded();
        CaptureOriginalMaterial();

        if (hitMaterial != null)
        {
            hitmat = hitMaterial;
        }
    }

    public void ApplySettingsFrom(TestHitting template)
    {
        if (template == null || template == this)
        {
            return;
        }

        maxHistoryEntries = template.maxHistoryEntries;
        usePollingFallback = template.usePollingFallback;
        includeInactiveSwordObjects = template.includeInactiveSwordObjects;
        swordScanInterval = template.swordScanInterval;
        useNameFallback = template.useNameFallback;
        requireActiveSwordAttackAnimation = template.requireActiveSwordAttackAnimation;
        attackingActionScript = template.attackingActionScript;
        actionScriptScanInterval = template.actionScriptScanInterval;
        slowTimeWhileInside = template.slowTimeWhileInside;
        insideTimeScale = template.insideTimeScale;
        smoothTimeScaleTransitions = template.smoothTimeScaleTransitions;
        timeScaleBlendSpeed = template.timeScaleBlendSpeed;
        focusCameraOnHit = template.focusCameraOnHit;
        focusCamera = template.focusCamera;
        cameraFocusRotateSpeed = template.cameraFocusRotateSpeed;
        cameraFocusPointSmoothSpeed = template.cameraFocusPointSmoothSpeed;
        changeFovWhileFocusing = template.changeFovWhileFocusing;
        focusedCameraFov = template.focusedCameraFov;
        cameraFovLerpSpeed = template.cameraFovLerpSpeed;
        cameraReturnRotateSpeed = template.cameraReturnRotateSpeed;
        focusPointOffset = template.focusPointOffset;
        tintHitTrails = template.tintHitTrails;
        hitTrailColor = template.hitTrailColor;
        drawHitStroke = template.drawHitStroke;
        hitStrokeColor = template.hitStrokeColor;
        hitStrokeWidth = template.hitStrokeWidth;
        hitStrokeLifetime = template.hitStrokeLifetime;
        hitStrokePointSpacing = template.hitStrokePointSpacing;
        hitStrokeSurfaceOffset = template.hitStrokeSurfaceOffset;
        hitStrokeRestartGap = template.hitStrokeRestartGap;

        if (template.hitmat != null)
        {
            hitmat = template.hitmat;
        }
    }

    public static TestHitting FindPracticeTemplate()
    {
#if UNITY_2023_1_OR_NEWER
        TestHitting[] hitTargets = FindObjectsByType<TestHitting>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        TestHitting[] hitTargets = FindObjectsOfType<TestHitting>(true);
#endif

        for (int i = 0; i < hitTargets.Length; i++)
        {
            TestHitting candidate = hitTargets[i];
            if (candidate == null)
            {
                continue;
            }

            if (candidate.GetComponentInParent<NPCDemageScript>() != null)
            {
                continue;
            }

            return candidate;
        }

        return null;
    }

    public static TestHitting EnsureEnemyHitFeedback(GameObject enemy, TestHitting template, Material fallbackHitMaterial)
    {
        if (enemy == null || !enemy.scene.IsValid())
        {
            return null;
        }

        Collider targetCollider = enemy.GetComponent<Collider>();
        if (targetCollider == null)
        {
            targetCollider = enemy.GetComponentInChildren<Collider>(true);
        }

        if (targetCollider == null)
        {
            return null;
        }

        TestHitting hitFeedback = targetCollider.GetComponent<TestHitting>();
        if (hitFeedback == null)
        {
            hitFeedback = enemy.GetComponentInChildren<TestHitting>(true);
        }

        if (hitFeedback == null)
        {
            hitFeedback = targetCollider.gameObject.AddComponent<TestHitting>();
        }

        if (template != null)
        {
            hitFeedback.ApplySettingsFrom(template);
        }

        Material hitMaterial = ResolveEnemyHitMaterial(enemy, template, fallbackHitMaterial);
        hitFeedback.ConfigureHitTarget(ResolveEnemyRenderer(enemy), hitMaterial);
        return hitFeedback;
    }

    private static Material ResolveEnemyHitMaterial(GameObject enemy, TestHitting template, Material fallbackHitMaterial)
    {
        if (template != null && template.hitmat != null)
        {
            return template.hitmat;
        }

        if (fallbackHitMaterial != null)
        {
            return fallbackHitMaterial;
        }

        NPCDemageScript damageScript = enemy.GetComponent<NPCDemageScript>();
        if (damageScript == null)
        {
            damageScript = enemy.GetComponentInChildren<NPCDemageScript>(true);
        }

        return damageScript != null ? damageScript.demagemat : null;
    }

    private static Renderer ResolveEnemyRenderer(GameObject enemy)
    {
        if (enemy == null)
        {
            return null;
        }

        NPCDemageScript damageScript = enemy.GetComponent<NPCDemageScript>();
        if (damageScript == null)
        {
            damageScript = enemy.GetComponentInChildren<NPCDemageScript>(true);
        }

        if (damageScript != null && damageScript.meshRenderer != null)
        {
            return damageScript.meshRenderer;
        }

        return enemy.GetComponentInChildren<Renderer>(true);
    }

    private void ResolveMeshRendererIfNeeded()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<Renderer>();
        }

        if (meshRenderer == null)
        {
            meshRenderer = GetComponentInChildren<Renderer>(true);
        }
    }

    private void CaptureOriginalMaterial()
    {
        if (meshRenderer != null)
        {
            _originalMaterial = meshRenderer.material;
        }
    }

    private void Start()
    {
        swordScanInterval = Mathf.Max(0.05f, swordScanInterval);
        actionScriptScanInterval = Mathf.Max(0.1f, actionScriptScanInterval);
        RefreshSwordColliders();
        RefreshActionScripts();
        _nextSwordScanTime = Time.time + swordScanInterval;
        _nextActionScriptScanTime = Time.time + actionScriptScanInterval;
    }

    private void Update()
    {
        if (usePollingFallback && _selfCollider != null)
        {
            if (Time.time >= _nextSwordScanTime)
            {
                RefreshSwordColliders();
                _nextSwordScanTime = Time.time + swordScanInterval;
            }

            UpdateSwordOverlapStateFromPolling();
        }

        if (requireActiveSwordAttackAnimation && Time.time >= _nextActionScriptScanTime)
        {
            RefreshActionScripts();
            _nextActionScriptScanTime = Time.time + actionScriptScanInterval;
        }

        bool swordInside = IsSwordInsideAndAllowedThisFrame();
        _swordInsideThisFrame = swordInside;
        _hasFocusPointThisFrame = swordInside && TryGetFocusPoint(out _focusPointThisFrame);
        UpdateSmoothedFocusPointState();

        ApplyTimeScaleState(swordInside);

        float timeDeltaForCounters = Time.unscaledDeltaTime;
        if (swordInside)
        {
            currentInsideSeconds += timeDeltaForCounters;
            totalInsideSeconds += timeDeltaForCounters;
        }
        else
        {
            CommitCurrentSessionToHistoryIfNeeded();
            currentInsideSeconds = 0f;
        }

        _wasSwordInsideLastFrame = swordInside;

        ApplyMaterialState(swordInside);
        UpdateHitTrailColors();
        if (swordInside && _hasFocusPointThisFrame)
        {
            AppendHitStrokePoint(_focusPointThisFrame);
        }

        UpdateHitStrokeVisual();
    }

    private void LateUpdate()
    {
        ApplyCameraFocusState();
    }

    private void OnDisable()
    {
        CommitCurrentSessionToHistoryIfNeeded();
        currentInsideSeconds = 0f;
        _wasSwordInsideLastFrame = false;
        RestoreTimeScaleIfNeeded(true);
        RestoreCameraStateImmediate();
        RestoreAllTrailColors();
        ResetHitStroke(clearPoints: true);
        _hasSmoothedFocusPoint = false;
        _touchingSwordColliders.Clear();
        _overlappingSwordRenderers.Clear();
        ApplyMaterialState(false);
    }

    private void OnDestroy()
    {
        RestoreAllTrailColors();
        DestroyHitStrokeResources();
    }

    public void ClearInsideHistory()
    {
        insideHistorySeconds.Clear();
    }

    private bool IsSwordInsideAndAllowedThisFrame()
    {
        _touchingSwordColliders.RemoveWhere(c => c == null);
        _overlappingSwordRenderers.RemoveWhere(r => r == null);

        foreach (Collider collider in _touchingSwordColliders)
        {
            if (IsSwordAttackAnimationActive(collider))
            {
                return true;
            }
        }

        foreach (Renderer renderer in _overlappingSwordRenderers)
        {
            if (IsSwordAttackAnimationActive(renderer))
            {
                return true;
            }
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsSwordCollider(other))
        {
            _touchingSwordColliders.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other != null)
        {
            _touchingSwordColliders.Remove(other);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Collider other = GetOtherCollider(collision);
        if (IsSwordCollider(other))
        {
            _touchingSwordColliders.Add(other);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        Collider other = GetOtherCollider(collision);
        if (other != null)
        {
            _touchingSwordColliders.Remove(other);
        }
    }

    private void UpdateSwordOverlapStateFromPolling()
    {
        for (int i = _swordColliders.Count - 1; i >= 0; i--)
        {
            Collider swordCollider = _swordColliders[i];
            if (swordCollider == null)
            {
                _swordColliders.RemoveAt(i);
                continue;
            }

            if (IsTouchingSwordCollider(swordCollider))
            {
                _touchingSwordColliders.Add(swordCollider);
            }
            else
            {
                _touchingSwordColliders.Remove(swordCollider);
            }
        }

        for (int i = _swordRenderers.Count - 1; i >= 0; i--)
        {
            Renderer swordRenderer = _swordRenderers[i];
            if (swordRenderer == null)
            {
                _swordRenderers.RemoveAt(i);
                continue;
            }

            if (IsSwordRendererOverlapping(swordRenderer))
            {
                _overlappingSwordRenderers.Add(swordRenderer);
            }
            else
            {
                _overlappingSwordRenderers.Remove(swordRenderer);
            }
        }
    }

    private void ApplyMaterialState(bool swordInside)
    {
        if (meshRenderer == null || hitmat == null)
        {
            return;
        }

        if (swordInside)
        {
            if (!_isUsingHitMaterial)
            {
                meshRenderer.material = hitmat;
                _isUsingHitMaterial = true;
            }
        }
        else if (_isUsingHitMaterial)
        {
            meshRenderer.material = _originalMaterial;
            _isUsingHitMaterial = false;
        }
    }

    private void UpdateHitTrailColors()
    {
        if (!tintHitTrails)
        {
            RestoreAllTrailColors();
            return;
        }

        _desiredTintTrails.Clear();

        foreach (Collider swordCollider in _touchingSwordColliders)
        {
            if (swordCollider == null || !IsSwordAttackAnimationActive(swordCollider))
            {
                continue;
            }

            TryAddDesiredTrail(swordCollider);
        }

        foreach (Renderer swordRenderer in _overlappingSwordRenderers)
        {
            if (swordRenderer == null || !IsSwordAttackAnimationActive(swordRenderer))
            {
                continue;
            }

            TryAddDesiredTrail(swordRenderer);
        }

        _trailRestoreBuffer.Clear();
        foreach (KeyValuePair<SwordTrailEffect, Color> entry in _originalTrailColors)
        {
            SwordTrailEffect trail = entry.Key;
            if (trail == null || !_desiredTintTrails.Contains(trail))
            {
                _trailRestoreBuffer.Add(trail);
            }
        }

        for (int i = 0; i < _trailRestoreBuffer.Count; i++)
        {
            SwordTrailEffect trail = _trailRestoreBuffer[i];
            if (trail != null && _originalTrailColors.TryGetValue(trail, out Color originalColor))
            {
                trail.trailColor = originalColor;
            }

            _originalTrailColors.Remove(trail);
        }

        foreach (SwordTrailEffect trail in _desiredTintTrails)
        {
            if (trail == null)
            {
                continue;
            }

            if (!_originalTrailColors.ContainsKey(trail))
            {
                _originalTrailColors.Add(trail, trail.trailColor);
            }

            trail.trailColor = hitTrailColor;
        }
    }

    private void TryAddDesiredTrail(Component source)
    {
        if (source == null)
        {
            return;
        }

        SwordTrailEffect trail = source.GetComponentInParent<SwordTrailEffect>();
        if (trail != null)
        {
            _desiredTintTrails.Add(trail);
        }
    }

    private void RestoreAllTrailColors()
    {
        foreach (KeyValuePair<SwordTrailEffect, Color> entry in _originalTrailColors)
        {
            if (entry.Key != null)
            {
                entry.Key.trailColor = entry.Value;
            }
        }

        _originalTrailColors.Clear();
        _desiredTintTrails.Clear();
        _trailRestoreBuffer.Clear();
    }

    private void CommitCurrentSessionToHistoryIfNeeded()
    {
        if (!_wasSwordInsideLastFrame || currentInsideSeconds <= 0f)
        {
            return;
        }

        insideHistorySeconds.Add(currentInsideSeconds);

        if (maxHistoryEntries > 0 && insideHistorySeconds.Count > maxHistoryEntries)
        {
            int removeCount = insideHistorySeconds.Count - maxHistoryEntries;
            insideHistorySeconds.RemoveRange(0, removeCount);
        }

        _wasSwordInsideLastFrame = false;
    }

    private void ApplyTimeScaleState(bool swordInside)
    {
        if (!slowTimeWhileInside)
        {
            RestoreTimeScaleIfNeeded();
            return;
        }

        float targetScale = Mathf.Clamp(insideTimeScale, 0.01f, 1f);

        if (swordInside)
        {
            if (!_timeSlowActive)
            {
                _timeScaleBeforeSlow = Time.timeScale;
                _timeSlowActive = true;
            }

            ApplyTargetTimeScale(targetScale);
        }
        else
        {
            RestoreTimeScaleIfNeeded();
        }
    }

    private void RestoreTimeScaleIfNeeded(bool immediate = false)
    {
        if (!_timeSlowActive)
        {
            return;
        }

        float restoreScale = _timeScaleBeforeSlow > 0f ? _timeScaleBeforeSlow : 1f;
        if (immediate)
        {
            SetGlobalTimeScale(restoreScale);
            _timeSlowActive = false;
            return;
        }

        ApplyTargetTimeScale(restoreScale);
        if (Mathf.Abs(Time.timeScale - restoreScale) <= 0.01f)
        {
            SetGlobalTimeScale(restoreScale);
            _timeSlowActive = false;
        }
    }

    private void ApplyTargetTimeScale(float targetScale)
    {
        float clampedTarget = Mathf.Clamp(targetScale, 0.01f, 100f);
        if (!smoothTimeScaleTransitions)
        {
            SetGlobalTimeScale(clampedTarget);
            return;
        }

        float blend = 1f - Mathf.Exp(-Mathf.Max(0.01f, timeScaleBlendSpeed) * Time.unscaledDeltaTime);
        float nextScale = Mathf.Lerp(Time.timeScale, clampedTarget, blend);
        if (Mathf.Abs(nextScale - clampedTarget) <= 0.001f)
        {
            nextScale = clampedTarget;
        }

        SetGlobalTimeScale(nextScale);
    }

    private void SetGlobalTimeScale(float scale)
    {
        float clampedScale = Mathf.Clamp(scale, 0.01f, 100f);
        Time.timeScale = clampedScale;
        Time.fixedDeltaTime = _defaultFixedDeltaTime * clampedScale;
    }

    private void ApplyCameraFocusState()
    {
        if (!focusCameraOnHit)
        {
            RestoreCameraStateIfNeeded();
            return;
        }

        Camera activeCamera = ResolveFocusCamera();
        if (activeCamera == null)
        {
            return;
        }

        Transform cameraTransform = activeCamera.transform;
        float unscaledDelta = Time.unscaledDeltaTime;

        if (_swordInsideThisFrame && _hasFocusPointThisFrame)
        {
            Vector3 targetPoint = _hasSmoothedFocusPoint ? _smoothedFocusPoint : _focusPointThisFrame;
            if (!_cameraWasFocusing)
            {
                _cameraBaseFov = activeCamera.fieldOfView;
                _cameraBaseRotation = cameraTransform.rotation;
                _cameraWasFocusing = true;
            }

            Vector3 direction = targetPoint - cameraTransform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                float rotationLerp = 1f - Mathf.Exp(-Mathf.Max(0.01f, cameraFocusRotateSpeed) * unscaledDelta);
                cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, targetRotation, rotationLerp);
            }

            if (changeFovWhileFocusing)
            {
                float targetFov = Mathf.Clamp(focusedCameraFov, 1f, 179f);
                float fovLerp = 1f - Mathf.Exp(-Mathf.Max(0.01f, cameraFovLerpSpeed) * unscaledDelta);
                activeCamera.fieldOfView = Mathf.Lerp(activeCamera.fieldOfView, targetFov, fovLerp);
            }
        }
        else
        {
            RestoreCameraStateIfNeeded();
        }
    }

    private void RestoreCameraStateIfNeeded()
    {
        if (!_cameraWasFocusing)
        {
            return;
        }

        Camera activeCamera = ResolveFocusCamera();
        if (activeCamera == null)
        {
            _cameraWasFocusing = false;
            return;
        }

        if (changeFovWhileFocusing)
        {
            float fovLerp = 1f - Mathf.Exp(-Mathf.Max(0.01f, cameraFovLerpSpeed) * Time.unscaledDeltaTime);
            activeCamera.fieldOfView = Mathf.Lerp(activeCamera.fieldOfView, _cameraBaseFov, fovLerp);
            if (Mathf.Abs(activeCamera.fieldOfView - _cameraBaseFov) <= 0.05f)
            {
                activeCamera.fieldOfView = _cameraBaseFov;
            }
        }

        Transform cameraTransform = activeCamera.transform;
        float rotationLerp = 1f - Mathf.Exp(-Mathf.Max(0.01f, cameraReturnRotateSpeed) * Time.unscaledDeltaTime);
        cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, _cameraBaseRotation, rotationLerp);

        bool fovRestored = !changeFovWhileFocusing || Mathf.Abs(activeCamera.fieldOfView - _cameraBaseFov) <= 0.05f;
        bool rotationRestored = Quaternion.Angle(cameraTransform.rotation, _cameraBaseRotation) <= 0.15f;
        if (fovRestored && rotationRestored)
        {
            _cameraWasFocusing = false;
        }
    }

    private void RestoreCameraStateImmediate()
    {
        if (!_cameraWasFocusing)
        {
            return;
        }

        Camera activeCamera = ResolveFocusCamera();
        if (activeCamera != null)
        {
            if (changeFovWhileFocusing)
            {
                activeCamera.fieldOfView = _cameraBaseFov;
            }

            activeCamera.transform.rotation = _cameraBaseRotation;
        }

        _cameraWasFocusing = false;
        _hasSmoothedFocusPoint = false;
    }

    private Camera ResolveFocusCamera()
    {
        if (focusCamera != null)
        {
            return focusCamera;
        }

        return Camera.main;
    }

    private void UpdateSmoothedFocusPointState()
    {
        if (!_swordInsideThisFrame || !_hasFocusPointThisFrame)
        {
            _hasSmoothedFocusPoint = false;
            return;
        }

        if (!_hasSmoothedFocusPoint)
        {
            _smoothedFocusPoint = _focusPointThisFrame;
            _hasSmoothedFocusPoint = true;
            return;
        }

        float smooth = 1f - Mathf.Exp(-Mathf.Max(0.01f, cameraFocusPointSmoothSpeed) * Time.unscaledDeltaTime);
        _smoothedFocusPoint = Vector3.Lerp(_smoothedFocusPoint, _focusPointThisFrame, smooth);
    }

    private void AppendHitStrokePoint(Vector3 worldHitPoint)
    {
        if (!drawHitStroke)
        {
            return;
        }

        float now = Time.unscaledTime;
        float restartGap = Mathf.Max(0f, hitStrokeRestartGap);
        if (_hitStrokePoints.Count > 0 && now - _lastHitStrokeContactTime > restartGap)
        {
            ResetHitStroke(clearPoints: true);
        }

        _lastHitStrokeContactTime = now;
        _hitStrokeActive = true;

        Vector3 strokePoint = ComputeHitStrokePoint(worldHitPoint);
        float minSpacing = Mathf.Max(0.001f, hitStrokePointSpacing);
        float minSpacingSqr = minSpacing * minSpacing;

        if (_hitStrokePoints.Count > 0)
        {
            int lastIndex = _hitStrokePoints.Count - 1;
            Vector3 lastPoint = _hitStrokePoints[lastIndex];
            if ((strokePoint - lastPoint).sqrMagnitude < minSpacingSqr)
            {
                _hitStrokePointTimes[lastIndex] = now;
                return;
            }
        }

        _hitStrokePoints.Add(strokePoint);
        _hitStrokePointTimes.Add(now);
        EnsureHitStrokeRenderer();
    }

    private Vector3 ComputeHitStrokePoint(Vector3 worldHitPoint)
    {
        if (_selfCollider == null)
        {
            return worldHitPoint;
        }

        Vector3 surfacePoint = _selfCollider.ClosestPoint(worldHitPoint);
        Vector3 normal = worldHitPoint - surfacePoint;
        if (normal.sqrMagnitude < 0.000001f)
        {
            normal = surfacePoint - _selfCollider.bounds.center;
            if (normal.sqrMagnitude < 0.000001f)
            {
                normal = Vector3.up;
            }
        }

        return surfacePoint + normal.normalized * Mathf.Max(0f, hitStrokeSurfaceOffset);
    }

    private void UpdateHitStrokeVisual()
    {
        if (!drawHitStroke)
        {
            ResetHitStroke(clearPoints: true);
            return;
        }

        if (_hitStrokePoints.Count == 0)
        {
            if (_hitStrokeRenderer != null)
            {
                _hitStrokeRenderer.positionCount = 0;
                _hitStrokeRenderer.enabled = false;
            }

            _hitStrokeActive = false;
            return;
        }

        float life = Mathf.Max(0.05f, hitStrokeLifetime);
        float now = Time.unscaledTime;
        while (_hitStrokePointTimes.Count > 0 && now - _hitStrokePointTimes[0] > life)
        {
            _hitStrokePointTimes.RemoveAt(0);
            _hitStrokePoints.RemoveAt(0);
        }

        if (_hitStrokePoints.Count == 0)
        {
            ResetHitStroke(clearPoints: true);
            return;
        }

        EnsureHitStrokeRenderer();
        _hitStrokeRenderer.enabled = true;
        _hitStrokeRenderer.positionCount = _hitStrokePoints.Count;
        for (int i = 0; i < _hitStrokePoints.Count; i++)
        {
            _hitStrokeRenderer.SetPosition(i, _hitStrokePoints[i]);
        }

        float contactFade = Mathf.Clamp01(1f - ((now - _lastHitStrokeContactTime) / life));
        ApplyHitStrokeStyle(contactFade);
    }

    private void EnsureHitStrokeRenderer()
    {
        if (_hitStrokeRenderer != null)
        {
            return;
        }

        _hitStrokeObject = new GameObject($"{name}_HitStroke");
        _hitStrokeObject.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        _hitStrokeRenderer = _hitStrokeObject.AddComponent<LineRenderer>();
        _hitStrokeRenderer.useWorldSpace = true;
        _hitStrokeRenderer.alignment = LineAlignment.View;
        _hitStrokeRenderer.textureMode = LineTextureMode.Stretch;
        _hitStrokeRenderer.numCornerVertices = 6;
        _hitStrokeRenderer.numCapVertices = 6;
        _hitStrokeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _hitStrokeRenderer.receiveShadows = false;
        _hitStrokeRenderer.positionCount = 0;

        Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (lineShader == null)
        {
            lineShader = Shader.Find("Sprites/Default");
        }

        if (lineShader == null)
        {
            lineShader = Shader.Find("Unlit/Color");
        }

        if (lineShader != null)
        {
            _hitStrokeMaterial = new Material(lineShader);
            _hitStrokeMaterial.name = $"{name}_HitStrokeMat";
            _hitStrokeRenderer.material = _hitStrokeMaterial;
        }

        ApplyHitStrokeStyle(1f);
    }

    private void ApplyHitStrokeStyle(float intensity)
    {
        if (_hitStrokeRenderer == null)
        {
            return;
        }

        float clampedIntensity = Mathf.Clamp01(intensity);
        float width = Mathf.Max(0.001f, hitStrokeWidth * Mathf.Lerp(0.6f, 1f, clampedIntensity));
        _hitStrokeRenderer.startWidth = width;
        _hitStrokeRenderer.endWidth = width * 0.82f;

        Color tint = hitStrokeColor;
        tint.a *= Mathf.Lerp(0.3f, 1f, clampedIntensity);
        _hitStrokeRenderer.startColor = tint;
        _hitStrokeRenderer.endColor = tint;

        if (_hitStrokeMaterial != null)
        {
            if (_hitStrokeMaterial.HasProperty("_BaseColor"))
            {
                _hitStrokeMaterial.SetColor("_BaseColor", tint);
            }

            if (_hitStrokeMaterial.HasProperty("_Color"))
            {
                _hitStrokeMaterial.SetColor("_Color", tint);
            }
        }
    }

    private void ResetHitStroke(bool clearPoints)
    {
        _hitStrokeActive = false;
        if (clearPoints)
        {
            _hitStrokePoints.Clear();
            _hitStrokePointTimes.Clear();
        }

        if (_hitStrokeRenderer != null)
        {
            _hitStrokeRenderer.positionCount = _hitStrokePoints.Count;
            _hitStrokeRenderer.enabled = _hitStrokePoints.Count > 0;
        }
    }

    private void DestroyHitStrokeResources()
    {
        ResetHitStroke(clearPoints: true);

        if (_hitStrokeObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_hitStrokeObject);
            }
            else
            {
                DestroyImmediate(_hitStrokeObject);
            }

            _hitStrokeObject = null;
            _hitStrokeRenderer = null;
        }

        if (_hitStrokeMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_hitStrokeMaterial);
            }
            else
            {
                DestroyImmediate(_hitStrokeMaterial);
            }

            _hitStrokeMaterial = null;
        }
    }

    private bool IsSwordAttackAnimationActive(Component swordSource)
    {
        if (!requireActiveSwordAttackAnimation)
        {
            return true;
        }

        if (TryResolveAttackingActionScript(swordSource, out ActionScript actionScript) &&
            IsActionScriptInSwordAttack(actionScript))
        {
            return true;
        }

        return IsAnyKnownActionScriptInSwordAttack();
    }

    private bool TryResolveAttackingActionScript(Component swordSource, out ActionScript actionScript)
    {
        actionScript = attackingActionScript;
        if (actionScript != null)
        {
            return true;
        }

        if (swordSource == null)
        {
            return false;
        }

        actionScript = swordSource.GetComponentInParent<ActionScript>();
        if (actionScript != null)
        {
            return true;
        }

        Sword sword = swordSource.GetComponentInParent<Sword>();
        if (sword != null)
        {
            actionScript = sword.GetComponentInParent<ActionScript>();
            if (actionScript != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsActionScriptInSwordAttack(ActionScript actionScript)
    {
        return actionScript != null &&
               actionScript.isActiveAndEnabled &&
               actionScript.TryGetActiveSwordAttackStateInfo(out _, out _);
    }

    private bool IsAnyKnownActionScriptInSwordAttack()
    {
        if (_anyAttackStateFrame == Time.frameCount)
        {
            return _anySwordAttackActiveThisFrame;
        }

        _anyAttackStateFrame = Time.frameCount;
        _anySwordAttackActiveThisFrame = false;

        for (int i = _knownActionScripts.Count - 1; i >= 0; i--)
        {
            ActionScript candidate = _knownActionScripts[i];
            if (candidate == null)
            {
                _knownActionScripts.RemoveAt(i);
                continue;
            }

            if (IsActionScriptInSwordAttack(candidate))
            {
                _anySwordAttackActiveThisFrame = true;
                break;
            }
        }

        return _anySwordAttackActiveThisFrame;
    }

    private bool TryGetFocusPoint(out Vector3 focusPoint)
    {
        focusPoint = transform.position;

        Camera activeCamera = ResolveFocusCamera();
        Vector3 cameraPosition = activeCamera != null ? activeCamera.transform.position : transform.position;
        bool found = false;
        float bestDistanceSqr = float.MaxValue;
        Vector3 selfCenter = _selfCollider != null ? _selfCollider.bounds.center : transform.position;

        foreach (Collider swordCollider in _touchingSwordColliders)
        {
            if (swordCollider == null || !IsSwordAttackAnimationActive(swordCollider))
            {
                continue;
            }

            Vector3 candidate = ComputeFocusPointFromCollider(swordCollider, selfCenter);
            float distanceSqr = (candidate - cameraPosition).sqrMagnitude;
            if (!found || distanceSqr < bestDistanceSqr)
            {
                focusPoint = candidate;
                bestDistanceSqr = distanceSqr;
                found = true;
            }
        }

        foreach (Renderer swordRenderer in _overlappingSwordRenderers)
        {
            if (swordRenderer == null || !IsSwordAttackAnimationActive(swordRenderer))
            {
                continue;
            }

            Vector3 candidate = ComputeFocusPointFromRenderer(swordRenderer, selfCenter);
            float distanceSqr = (candidate - cameraPosition).sqrMagnitude;
            if (!found || distanceSqr < bestDistanceSqr)
            {
                focusPoint = candidate;
                bestDistanceSqr = distanceSqr;
                found = true;
            }
        }

        return found;
    }

    private Vector3 ComputeFocusPointFromCollider(Collider swordCollider, Vector3 selfCenter)
    {
        if (_selfCollider == null || swordCollider == null)
        {
            return selfCenter + focusPointOffset;
        }

        Vector3 selfPoint = _selfCollider.ClosestPoint(swordCollider.bounds.center);
        Vector3 swordPoint = swordCollider.ClosestPoint(selfPoint);
        return (selfPoint + swordPoint) * 0.5f + focusPointOffset;
    }

    private Vector3 ComputeFocusPointFromRenderer(Renderer swordRenderer, Vector3 selfCenter)
    {
        if (_selfCollider == null || swordRenderer == null)
        {
            return selfCenter + focusPointOffset;
        }

        Vector3 selfPoint = _selfCollider.ClosestPoint(swordRenderer.bounds.center);
        Vector3 swordPoint = swordRenderer.bounds.ClosestPoint(selfPoint);
        return (selfPoint + swordPoint) * 0.5f + focusPointOffset;
    }

    private Collider GetOtherCollider(Collision collision)
    {
        if (collision == null)
        {
            return null;
        }

        if (collision.collider != null && collision.collider.gameObject != gameObject)
        {
            return collision.collider;
        }

        return null;
    }

    private bool IsSwordCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (LooksLikeSwordPart(other.transform))
        {
            return true;
        }

        Sword sword = other.GetComponent<Sword>() ?? other.GetComponentInParent<Sword>();
        if (sword != null && LooksLikeSwordPart(sword.transform))
        {
            return true;
        }

        return false;
    }

    private bool IsTouchingSwordCollider(Collider swordCollider)
    {
        if (_selfCollider == null || swordCollider == null || !_selfCollider.enabled || !swordCollider.enabled)
        {
            return false;
        }

        if (!_selfCollider.gameObject.activeInHierarchy || !swordCollider.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (!_selfCollider.bounds.Intersects(swordCollider.bounds))
        {
            return false;
        }

        return Physics.ComputePenetration(
            _selfCollider,
            _selfCollider.transform.position,
            _selfCollider.transform.rotation,
            swordCollider,
            swordCollider.transform.position,
            swordCollider.transform.rotation,
            out _,
            out _);
    }

    private bool IsSwordRendererOverlapping(Renderer swordRenderer)
    {
        if (_selfCollider == null || swordRenderer == null || !swordRenderer.enabled)
        {
            return false;
        }

        if (!_selfCollider.gameObject.activeInHierarchy || !swordRenderer.gameObject.activeInHierarchy)
        {
            return false;
        }

        return _selfCollider.bounds.Intersects(swordRenderer.bounds);
    }

    private void RefreshSwordColliders()
    {
        _swordColliders.Clear();
        _swordRenderers.Clear();
        _touchingSwordColliders.RemoveWhere(c => c == null);
        _overlappingSwordRenderers.RemoveWhere(r => r == null);
        HashSet<Collider> seenColliders = new HashSet<Collider>();
        HashSet<Renderer> seenRenderers = new HashSet<Renderer>();

#if UNITY_2023_1_OR_NEWER
        Sword[] swords = FindObjectsByType<Sword>(
            includeInactiveSwordObjects ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
#else
        Sword[] swords = FindObjectsOfType<Sword>(includeInactiveSwordObjects);
#endif

        for (int i = 0; i < swords.Length; i++)
        {
            Sword sword = swords[i];
            if (sword == null)
            {
                continue;
            }

            AddCandidateColliders(sword.gameObject, seenColliders);
            AddCandidateRenderers(sword.gameObject, seenRenderers);
        }

        try
        {
            GameObject[] taggedSwordObjects = GameObject.FindGameObjectsWithTag("Sword");
            for (int i = 0; i < taggedSwordObjects.Length; i++)
            {
                AddCandidateColliders(taggedSwordObjects[i], seenColliders);
                AddCandidateRenderers(taggedSwordObjects[i], seenRenderers);
            }
        }
        catch (UnityException)
        {
            // Sword tag may not exist in Tag Manager; component/name lookup still works.
        }

        if (useNameFallback && _swordColliders.Count == 0 && _swordRenderers.Count == 0)
        {
#if UNITY_2023_1_OR_NEWER
            Collider[] allColliders = FindObjectsByType<Collider>(
                includeInactiveSwordObjects ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            Renderer[] allRenderers = FindObjectsByType<Renderer>(
                includeInactiveSwordObjects ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
#else
            Collider[] allColliders = FindObjectsOfType<Collider>(includeInactiveSwordObjects);
            Renderer[] allRenderers = FindObjectsOfType<Renderer>(includeInactiveSwordObjects);
#endif

            for (int i = 0; i < allColliders.Length; i++)
            {
                Collider candidate = allColliders[i];
                if (candidate == null || candidate == _selfCollider || candidate.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (!NameLooksLikeSword(candidate.transform))
                {
                    continue;
                }

                if (seenColliders.Add(candidate))
                {
                    _swordColliders.Add(candidate);
                }
            }

            for (int i = 0; i < allRenderers.Length; i++)
            {
                Renderer candidate = allRenderers[i];
                if (candidate == null || candidate.transform.IsChildOf(transform))
                {
                    continue;
                }

                if (!NameLooksLikeSword(candidate.transform))
                {
                    continue;
                }

                if (seenRenderers.Add(candidate))
                {
                    _swordRenderers.Add(candidate);
                }
            }
        }
    }

    private void RefreshActionScripts()
    {
        _knownActionScripts.Clear();

#if UNITY_2023_1_OR_NEWER
        ActionScript[] actions = FindObjectsByType<ActionScript>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
        ActionScript[] actions = FindObjectsOfType<ActionScript>(false);
#endif

        for (int i = 0; i < actions.Length; i++)
        {
            ActionScript candidate = actions[i];
            if (candidate == null)
            {
                continue;
            }

            _knownActionScripts.Add(candidate);
        }

        if (attackingActionScript == null && _knownActionScripts.Count > 0)
        {
            attackingActionScript = _knownActionScripts[0];
        }
    }

    private void AddCandidateColliders(GameObject root, HashSet<Collider> seen)
    {
        if (root == null || root == gameObject || root.transform.IsChildOf(transform))
        {
            return;
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(includeInactiveSwordObjects);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider candidate = colliders[i];
            if (candidate == null || candidate == _selfCollider || candidate.transform.IsChildOf(transform))
            {
                continue;
            }

            if (!CandidateBelongsToSwordHierarchy(candidate.transform, root.transform))
            {
                continue;
            }

            if (seen.Add(candidate))
            {
                _swordColliders.Add(candidate);
            }
        }
    }

    private void AddCandidateRenderers(GameObject root, HashSet<Renderer> seen)
    {
        if (root == null || root == gameObject || root.transform.IsChildOf(transform))
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(includeInactiveSwordObjects);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer candidate = renderers[i];
            if (candidate == null || candidate.transform.IsChildOf(transform))
            {
                continue;
            }

            if (!CandidateBelongsToSwordHierarchy(candidate.transform, root.transform))
            {
                continue;
            }

            if (seen.Add(candidate))
            {
                _swordRenderers.Add(candidate);
            }
        }
    }

    private bool CandidateBelongsToSwordHierarchy(Transform candidate, Transform swordRoot)
    {
        if (candidate == null)
        {
            return false;
        }

        if (LooksLikeSwordPart(candidate))
        {
            return true;
        }

        return swordRoot != null &&
               candidate.IsChildOf(swordRoot) &&
               LooksLikeSwordPart(swordRoot);
    }

    private bool LooksLikeSwordPart(Transform candidateTransform)
    {
        if (candidateTransform == null)
        {
            return false;
        }

        if (HasSwordTagInHierarchy(candidateTransform))
        {
            return true;
        }

        return useNameFallback && NameLooksLikeSword(candidateTransform);
    }

    private static bool HasSwordTagInHierarchy(Transform candidateTransform)
    {
        Transform current = candidateTransform;
        while (current != null)
        {
            if (HasSwordTag(current.gameObject))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static bool HasSwordTag(GameObject obj)
    {
        if (obj == null)
        {
            return false;
        }

        try
        {
            return obj.CompareTag("Sword");
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private static bool NameLooksLikeSword(Transform candidateTransform)
    {
        while (candidateTransform != null)
        {
            string name = candidateTransform.name;
            if (!string.IsNullOrEmpty(name))
            {
                if (name.IndexOf("sword", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("blade", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("katana", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            candidateTransform = candidateTransform.parent;
        }

        return false;
    }
}
