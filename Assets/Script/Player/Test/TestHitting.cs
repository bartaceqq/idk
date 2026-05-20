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
    [SerializeField, Range(0.01f, 1f)] private float insideTimeScale = 0.3f;
    [SerializeField] private bool smoothTimeScaleTransitions = true;
    [SerializeField, Min(0.01f)] private float timeScaleBlendSpeed = 8f;

    [Header("Camera Focus")]
    [SerializeField] private bool focusCameraOnHit = false;
    [SerializeField] private Camera focusCamera;
    [SerializeField] private float cameraFocusRotateSpeed = 9f;
    [SerializeField, Min(0.01f)] private float cameraFocusPointSmoothSpeed = 16f;
    [SerializeField] private bool changeFovWhileFocusing = true;
    [SerializeField, Range(1f, 179f)] private float focusedCameraFov = 45f;
    [SerializeField] private float cameraFovLerpSpeed = 10f;
    [SerializeField] private float cameraReturnRotateSpeed = 6f;
    [SerializeField] private Vector3 focusPointOffset = new Vector3(0f, 0.08f, 0f);

    [Header("Hit Trail Tint")]
    [SerializeField] private bool tintHitTrails = true;
    [SerializeField] private Color hitTrailColor = new Color(0.35f, 0f, 0f, 0.9f);

    [Header("Hit Stroke")]
    [SerializeField] private bool drawHitStroke = true;
    [SerializeField] private Color hitStrokeColor = new Color(0.32f, 0.02f, 0.02f, 0.95f);
    [SerializeField, Min(0.001f)] private float hitStrokeWidth = 0.04f;
    [SerializeField, Min(0.05f)] private float hitStrokeLifetime = 1.1f;
    [SerializeField, Min(0.001f)] private float hitStrokePointSpacing = 0.01f;
    [SerializeField, Min(0f)] private float hitStrokeSurfaceOffset = 0.018f;
    [SerializeField, Min(0f)] private float hitStrokeRestartGap = 0.16f;

    private Collider selfCollider;
    private Material originalMaterial;
    private float defaultFixedDeltaTime;
    private float timeScaleBeforeSlow = 1f;
    private bool wasInside;
    private bool timeSlowActive;
    private float nextSwordScanTime;
    private float nextActionScriptScanTime;
    private readonly HashSet<Collider> touchingSwordColliders = new HashSet<Collider>();
    private readonly Dictionary<SwordTrailEffect, Color> originalTrailColors = new Dictionary<SwordTrailEffect, Color>();
    private readonly List<Vector3> strokePoints = new List<Vector3>();
    private readonly List<float> strokeTimes = new List<float>();
    private LineRenderer strokeRenderer;
    private Material strokeMaterial;
    private Vector3 smoothedFocusPoint;
    private Quaternion cameraBaseRotation;
    private float cameraBaseFov;
    private bool cameraWasFocusing;

    private void Awake()
    {
        selfCollider = GetComponent<Collider>();
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        ResolveMeshRenderer();
        originalMaterial = meshRenderer != null ? meshRenderer.sharedMaterial : null;
    }

    private void Update()
    {
        bool isInside = TryFindSwordContact(out Component swordSource, out Vector3 contactPoint);
        UpdateTimers(isInside);
        UpdateMaterial(isInside);
        UpdateSlowMotion(isInside);
        UpdateTrailTint(isInside, swordSource);
        UpdateHitStroke(isInside, contactPoint);
        UpdateCameraFocus(isInside, contactPoint);
        wasInside = isInside;
    }

    private void OnDisable()
    {
        RestoreMaterial();
        RestoreSlowMotion();
        RestoreTrailColors();
        ClearStroke();
        wasInside = false;
        currentInsideSeconds = 0f;
    }

    private void OnDestroy()
    {
        if (strokeMaterial != null)
        {
            Destroy(strokeMaterial);
        }
    }

    private void OnTriggerEnter(Collider other) => TrackCollider(other, true);
    private void OnTriggerStay(Collider other) => TrackCollider(other, true);
    private void OnTriggerExit(Collider other) => TrackCollider(other, false);

    public void ConfigureHitTarget(Renderer targetRenderer, Material hitMaterial)
    {
        if (targetRenderer != null) meshRenderer = targetRenderer;
        if (hitMaterial != null) hitmat = hitMaterial;
        ResolveMeshRenderer();
        originalMaterial = meshRenderer != null ? meshRenderer.sharedMaterial : null;
    }

    public void ApplySettingsFrom(TestHitting template)
    {
        if (template == null || template == this) return;

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
        if (template.hitmat != null) hitmat = template.hitmat;
    }

    public static TestHitting FindPracticeTemplate()
    {
#if UNITY_2023_1_OR_NEWER
        TestHitting[] targets = FindObjectsByType<TestHitting>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        TestHitting[] targets = FindObjectsOfType<TestHitting>(true);
#endif
        foreach (TestHitting target in targets)
        {
            if (target != null && target.GetComponentInParent<NPCDemageScript>() == null)
            {
                return target;
            }
        }

        return null;
    }

    public static TestHitting EnsureEnemyHitFeedback(GameObject enemy, TestHitting template, Material fallbackHitMaterial)
    {
        if (enemy == null || !enemy.scene.IsValid()) return null;

        Collider targetCollider = enemy.GetComponent<Collider>() ?? enemy.GetComponentInChildren<Collider>(true);
        if (targetCollider == null) return null;

        TestHitting feedback = targetCollider.GetComponent<TestHitting>() ?? enemy.GetComponentInChildren<TestHitting>(true);
        if (feedback == null) feedback = targetCollider.gameObject.AddComponent<TestHitting>();
        if (template != null) feedback.ApplySettingsFrom(template);

        feedback.ConfigureHitTarget(ResolveEnemyRenderer(enemy), ResolveEnemyHitMaterial(enemy, template, fallbackHitMaterial));
        return feedback;
    }

    private static Renderer ResolveEnemyRenderer(GameObject enemy)
    {
        NPCDemageScript damage = enemy.GetComponent<NPCDemageScript>() ?? enemy.GetComponentInChildren<NPCDemageScript>(true);
        if (damage != null && damage.meshRenderer != null) return damage.meshRenderer;
        return enemy.GetComponentInChildren<Renderer>(true);
    }

    private static Material ResolveEnemyHitMaterial(GameObject enemy, TestHitting template, Material fallback)
    {
        if (template != null && template.hitmat != null) return template.hitmat;
        if (fallback != null) return fallback;
        NPCDemageScript damage = enemy.GetComponent<NPCDemageScript>() ?? enemy.GetComponentInChildren<NPCDemageScript>(true);
        return damage != null ? damage.demagemat : null;
    }

    private void TrackCollider(Collider other, bool touching)
    {
        if (other == null || !IsSwordCollider(other)) return;
        if (touching) touchingSwordColliders.Add(other);
        else touchingSwordColliders.Remove(other);
    }

    private bool TryFindSwordContact(out Component swordSource, out Vector3 contactPoint)
    {
        swordSource = null;
        contactPoint = transform.position;

        foreach (Collider swordCollider in touchingSwordColliders)
        {
            if (IsValidSwordContact(swordCollider, out swordSource, out contactPoint)) return true;
        }

        if (!usePollingFallback || Time.time < nextSwordScanTime || selfCollider == null)
        {
            return false;
        }

        nextSwordScanTime = Time.time + Mathf.Max(0.02f, swordScanInterval);
        Bounds bounds = selfCollider.bounds;
        Collider[] overlaps = Physics.OverlapBox(bounds.center, bounds.extents, transform.rotation, ~0, QueryTriggerInteraction.Collide);
        foreach (Collider overlap in overlaps)
        {
            if (IsValidSwordContact(overlap, out swordSource, out contactPoint)) return true;
        }

        return false;
    }

    private bool IsValidSwordContact(Collider candidate, out Component swordSource, out Vector3 contactPoint)
    {
        swordSource = null;
        contactPoint = transform.position;
        if (candidate == null || candidate == selfCollider || (!includeInactiveSwordObjects && !candidate.gameObject.activeInHierarchy)) return false;
        if (!IsSwordCollider(candidate)) return false;

        swordSource = candidate.GetComponentInParent<Sword>() as Component ?? candidate.transform;
        if (!IsAttackAllowed(swordSource)) return false;

        contactPoint = selfCollider != null ? selfCollider.ClosestPoint(candidate.bounds.center) : candidate.transform.position;
        if (selfCollider != null && contactPoint == selfCollider.transform.position)
        {
            contactPoint = candidate.ClosestPoint(transform.position);
        }

        return true;
    }

    private bool IsSwordCollider(Collider candidate)
    {
        if (candidate == null) return false;
        if (candidate.GetComponentInParent<Sword>() != null || candidate.GetComponentInParent<SwordTrailEffect>() != null) return true;
        if (!useNameFallback) return false;

        string objectName = candidate.name.ToLowerInvariant();
        string rootName = candidate.transform.root != null ? candidate.transform.root.name.ToLowerInvariant() : string.Empty;
        return objectName.Contains("sword") || rootName.Contains("sword");
    }

    private bool IsAttackAllowed(Component swordSource)
    {
        if (!requireActiveSwordAttackAnimation) return true;
        if (Time.time >= nextActionScriptScanTime || attackingActionScript == null)
        {
            attackingActionScript = FindActionScript(swordSource);
            nextActionScriptScanTime = Time.time + Mathf.Max(0.02f, actionScriptScanInterval);
        }

        return attackingActionScript != null && attackingActionScript.TryGetActiveSwordAttackStateInfo(out _, out _);
    }

    private static ActionScript FindActionScript(Component source)
    {
        ActionScript action = source != null ? source.GetComponentInParent<ActionScript>() : null;
        if (action != null) return action;
#if UNITY_2023_1_OR_NEWER
        return FindAnyObjectByType<ActionScript>(FindObjectsInactive.Exclude);
#else
        return FindObjectOfType<ActionScript>(false);
#endif
    }

    private void UpdateTimers(bool isInside)
    {
        if (!isInside)
        {
            if (wasInside && currentInsideSeconds > 0f)
            {
                insideHistorySeconds.Add(currentInsideSeconds);
                while (insideHistorySeconds.Count > maxHistoryEntries) insideHistorySeconds.RemoveAt(0);
            }

            currentInsideSeconds = 0f;
            return;
        }

        currentInsideSeconds += Time.deltaTime;
        totalInsideSeconds += Time.deltaTime;
    }

    private void UpdateMaterial(bool isInside)
    {
        if (isInside && meshRenderer != null && hitmat != null)
        {
            meshRenderer.material = hitmat;
        }
        else if (!isInside)
        {
            RestoreMaterial();
        }
    }

    private void RestoreMaterial()
    {
        if (meshRenderer != null && originalMaterial != null)
        {
            meshRenderer.material = originalMaterial;
        }
    }

    private void UpdateSlowMotion(bool isInside)
    {
        if (!slowTimeWhileInside)
        {
            if (timeSlowActive) RestoreSlowMotion();
            return;
        }

        if (isInside && !timeSlowActive)
        {
            timeScaleBeforeSlow = Time.timeScale;
            timeSlowActive = true;
        }

        if (!timeSlowActive) return;
        float targetScale = isInside ? Mathf.Clamp(insideTimeScale, 0.01f, 1f) : timeScaleBeforeSlow;
        Time.timeScale = smoothTimeScaleTransitions ? Mathf.Lerp(Time.timeScale, targetScale, Time.unscaledDeltaTime * timeScaleBlendSpeed) : targetScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;

        if (!isInside && Mathf.Abs(Time.timeScale - timeScaleBeforeSlow) < 0.01f)
        {
            RestoreSlowMotion();
        }
    }

    private void RestoreSlowMotion()
    {
        Time.timeScale = timeScaleBeforeSlow > 0f ? timeScaleBeforeSlow : 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
        timeSlowActive = false;
    }

    private void UpdateTrailTint(bool isInside, Component source)
    {
        if (!tintHitTrails)
        {
            RestoreTrailColors();
            return;
        }

        if (!isInside)
        {
            RestoreTrailColors();
            return;
        }

        SwordTrailEffect trail = source != null ? source.GetComponentInParent<SwordTrailEffect>() : null;
        if (trail == null) return;
        if (!originalTrailColors.ContainsKey(trail)) originalTrailColors[trail] = trail.trailColor;
        trail.trailColor = hitTrailColor;
    }

    private void RestoreTrailColors()
    {
        foreach (KeyValuePair<SwordTrailEffect, Color> entry in originalTrailColors)
        {
            if (entry.Key != null) entry.Key.trailColor = entry.Value;
        }

        originalTrailColors.Clear();
    }

    private void UpdateHitStroke(bool isInside, Vector3 point)
    {
        if (!drawHitStroke)
        {
            ClearStroke();
            return;
        }

        EnsureStrokeRenderer();
        if (isInside)
        {
            AddStrokePoint(point + transform.up * hitStrokeSurfaceOffset);
        }

        for (int i = strokeTimes.Count - 1; i >= 0; i--)
        {
            if (Time.time - strokeTimes[i] > hitStrokeLifetime)
            {
                strokeTimes.RemoveAt(i);
                strokePoints.RemoveAt(i);
            }
        }

        strokeRenderer.positionCount = strokePoints.Count;
        strokeRenderer.SetPositions(strokePoints.ToArray());
    }

    private void AddStrokePoint(Vector3 point)
    {
        if (strokePoints.Count == 0 || Vector3.Distance(strokePoints[strokePoints.Count - 1], point) >= hitStrokePointSpacing || Time.time - strokeTimes[strokeTimes.Count - 1] >= hitStrokeRestartGap)
        {
            strokePoints.Add(point);
            strokeTimes.Add(Time.time);
        }
    }

    private void EnsureStrokeRenderer()
    {
        if (strokeRenderer != null) return;

        GameObject strokeObject = new GameObject("HitStroke");
        strokeObject.transform.SetParent(transform, false);
        strokeRenderer = strokeObject.AddComponent<LineRenderer>();
        strokeRenderer.useWorldSpace = true;
        strokeRenderer.widthMultiplier = hitStrokeWidth;
        strokeRenderer.numCapVertices = 4;
        strokeMaterial = new Material(Shader.Find("Sprites/Default"));
        strokeMaterial.color = hitStrokeColor;
        strokeRenderer.material = strokeMaterial;
        strokeRenderer.startColor = hitStrokeColor;
        strokeRenderer.endColor = hitStrokeColor;
    }

    private void ClearStroke()
    {
        strokePoints.Clear();
        strokeTimes.Clear();
        if (strokeRenderer != null) strokeRenderer.positionCount = 0;
    }

    private void UpdateCameraFocus(bool isInside, Vector3 point)
    {
        if (!focusCameraOnHit)
        {
            return;
        }

        Camera cameraTarget = focusCamera != null ? focusCamera : Camera.main;
        if (cameraTarget == null) return;

        if (isInside && !cameraWasFocusing)
        {
            cameraBaseRotation = cameraTarget.transform.rotation;
            cameraBaseFov = cameraTarget.fieldOfView;
            smoothedFocusPoint = point + focusPointOffset;
            cameraWasFocusing = true;
        }

        if (isInside)
        {
            smoothedFocusPoint = Vector3.Lerp(smoothedFocusPoint, point + focusPointOffset, Time.deltaTime * cameraFocusPointSmoothSpeed);
            Quaternion targetRotation = Quaternion.LookRotation(smoothedFocusPoint - cameraTarget.transform.position, Vector3.up);
            cameraTarget.transform.rotation = Quaternion.Slerp(cameraTarget.transform.rotation, targetRotation, Time.deltaTime * cameraFocusRotateSpeed);
            if (changeFovWhileFocusing) cameraTarget.fieldOfView = Mathf.Lerp(cameraTarget.fieldOfView, focusedCameraFov, Time.deltaTime * cameraFovLerpSpeed);
            return;
        }

        if (!cameraWasFocusing) return;
        cameraTarget.transform.rotation = Quaternion.Slerp(cameraTarget.transform.rotation, cameraBaseRotation, Time.deltaTime * cameraReturnRotateSpeed);
        if (changeFovWhileFocusing) cameraTarget.fieldOfView = Mathf.Lerp(cameraTarget.fieldOfView, cameraBaseFov, Time.deltaTime * cameraFovLerpSpeed);
    }

    private void ResolveMeshRenderer()
    {
        if (meshRenderer == null)
        {
            meshRenderer = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
        }
    }
}
