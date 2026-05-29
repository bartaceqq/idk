using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
[DisallowMultipleComponent]
public class SwordTrailEffect : MonoBehaviour
{
    [Header("Trail")][Min(0.02f)] public float sampleLifetime = 0.18f;
    [Min(0.001f)] public float minimumSampleDistance = 0.025f;
    [Min(0.001f)] public float minimumSampleInterval = 0.01f;
    public Color trailColor = new Color(1f, 1f, 1f, 0.8f);
    [Header("Blade Anchors")] public Transform bladeBaseAnchor;
    public Transform bladeTipAnchor;
    private readonly List<TrailSample> _samples = new List<TrailSample>(32);
    private readonly List<Vector3> _vertices = new List<Vector3>(64);
    private readonly List<Color> _colors = new List<Color>(64);
    private readonly List<Vector2> _uvs = new List<Vector2>(64);
    private readonly List<Vector3> _normals = new List<Vector3>(64);
    private readonly List<int> _triangles = new List<int>(192);
    private ActionScript _actionScript; private ItemSwitchScript _itemSwitchScript;
    private GameObject _trailObject; private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer; private Mesh _mesh; private Material _material;
    private Vector3 _lastBasePosition; private Vector3 _lastTipPosition;
    private float _lastSampleTime = float.NegativeInfinity; private bool _hasLastSample;
    private struct TrailSample
    {
        public Vector3 basePosition; public Vector3 tipPosition;
        public float time;
    }
    private void OnEnable()
    {
        ResolveReferences(); EnsureBladeAnchors();
        EnsureTrailObject(); ClearTrail();
    }
    private void LateUpdate()
    {
        ResolveReferences();
        if (!EnsureBladeAnchors()) { ClearTrail(); return; }
        EnsureTrailObject(); UpdateTrail();
    }
    private void OnDisable() { ClearTrail(); }
    private void OnDestroy() { DestroyTrailResources(); }
    private void ResolveReferences()
    {
        if (_actionScript == null)
        {
            _actionScript = GetComponentInParent<ActionScript>();
            if (_actionScript == null)
            {
                _actionScript = UnitySceneSearch.FindFirst<ActionScript>();
            }
        }
        if (_itemSwitchScript == null)
        {
            _itemSwitchScript = GetComponentInParent<ItemSwitchScript>();
            if (_itemSwitchScript == null)
            {
                _itemSwitchScript = UnitySceneSearch.FindFirst<ItemSwitchScript>();
            }
        }
    }
    private bool EnsureBladeAnchors()
    {
        if (bladeBaseAnchor != null && bladeTipAnchor != null) { return true; }
        if (bladeBaseAnchor == null) { bladeBaseAnchor = transform.Find("BladeTrailBase"); }
        if (bladeTipAnchor == null) { bladeTipAnchor = transform.Find("BladeTrailTip"); }
        if (bladeBaseAnchor != null && bladeTipAnchor != null) { return true; }
        if (!TryCreateDefaultBladeAnchors(out Transform createdBase, out Transform createdTip)) { return false; }
        bladeBaseAnchor = createdBase; bladeTipAnchor = createdTip; return true;
    }
    private bool TryCreateDefaultBladeAnchors(out Transform createdBase, out Transform createdTip)
    {
        createdBase = null; createdTip = null;
        if (!TryGetRenderableLocalBounds(out Bounds localBounds)) { return false; }
        Vector3 baseLocal; Vector3 tipLocal;
        DetermineBladeEndpoints(localBounds, out baseLocal, out tipLocal);
        GameObject baseAnchorObject = new GameObject("BladeTrailBase");
        baseAnchorObject.transform.SetParent(transform, false);
        baseAnchorObject.transform.localPosition = baseLocal;
        baseAnchorObject.transform.localRotation = Quaternion.identity;
        baseAnchorObject.transform.localScale = Vector3.one;
        GameObject tipAnchorObject = new GameObject("BladeTrailTip");
        tipAnchorObject.transform.SetParent(transform, false);
        tipAnchorObject.transform.localPosition = tipLocal;
        tipAnchorObject.transform.localRotation = Quaternion.identity;
        tipAnchorObject.transform.localScale = Vector3.one;
        createdBase = baseAnchorObject.transform; createdTip = tipAnchorObject.transform;
        return true;
    }
    private bool TryGetRenderableLocalBounds(out Bounds localBounds)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true); bool hasBounds = false;
        localBounds = default; for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererTarget = renderers[i];
            if (rendererTarget == null || rendererTarget is ParticleSystemRenderer) { continue; }
            Bounds worldBounds = rendererTarget.bounds; Vector3 boundsMin = worldBounds.min;
            Vector3 boundsMax = worldBounds.max; Vector3[] corners = {
new Vector3(boundsMin.x, boundsMin.y, boundsMin.z),
new Vector3(boundsMin.x, boundsMin.y, boundsMax.z),
new Vector3(boundsMin.x, boundsMax.y, boundsMin.z),
new Vector3(boundsMin.x, boundsMax.y, boundsMax.z),
new Vector3(boundsMax.x, boundsMin.y, boundsMin.z),
new Vector3(boundsMax.x, boundsMin.y, boundsMax.z),
new Vector3(boundsMax.x, boundsMax.y, boundsMin.z),
new Vector3(boundsMax.x, boundsMax.y, boundsMax.z) };
            for (int cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
            {
                Vector3 localPoint = transform.InverseTransformPoint(corners[cornerIndex]);
                if (!hasBounds)
                {
                    localBounds = new Bounds(localPoint, Vector3.zero);
                    hasBounds = true;
                }
                else { localBounds.Encapsulate(localPoint); }
            }
        }
        return hasBounds;
    }
    private static void DetermineBladeEndpoints(Bounds localBounds, out Vector3 baseLocal, out Vector3 tipLocal)
    {
        Vector3 min = localBounds.min; Vector3 max = localBounds.max;
        Vector3 center = localBounds.center; Vector3 size = localBounds.size;
        int dominantAxis = 0; float dominantSize = size.x; if (size.y > dominantSize)
        {
            dominantAxis = 1; dominantSize = size.y;
        }
        if (size.z > dominantSize) { dominantAxis = 2; }
        Vector3 endpointA = center;
        Vector3 endpointB = center; if (dominantAxis == 0)
        {
            endpointA.x = min.x;
            endpointB.x = max.x;
        }
        else if (dominantAxis == 1)
        {
            endpointA.y = min.y;
            endpointB.y = max.y;
        }
        else { endpointA.z = min.z; endpointB.z = max.z; }
        if (endpointA.sqrMagnitude <= endpointB.sqrMagnitude)
        {
            baseLocal = endpointA;
            tipLocal = endpointB; return;
        }
        baseLocal = endpointB; tipLocal = endpointA;
    }
    private void EnsureTrailObject()
    {
        if (_trailObject != null) { return; }
        _trailObject = new GameObject($"{name}_SwordTrail");
        _trailObject.layer = gameObject.layer; _trailObject.transform.position = Vector3.zero;
        _trailObject.transform.rotation = Quaternion.identity;
        _trailObject.transform.localScale = Vector3.one;
        _meshFilter = _trailObject.AddComponent<MeshFilter>();
        _meshRenderer = _trailObject.AddComponent<MeshRenderer>(); _mesh = new Mesh
        {
            name = $"{name}_SwordTrailMesh"
        }; _mesh.MarkDynamic(); _meshFilter.sharedMesh = _mesh;
        _material = CreateTrailMaterial(); _meshRenderer.sharedMaterial = _material;
        _meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _meshRenderer.receiveShadows = false;
        _meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        _meshRenderer.lightProbeUsage = LightProbeUsage.Off;
        _meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        _meshRenderer.enabled = false;
    }
    private Material CreateTrailMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) { shader = Shader.Find("Particles/Standard Unlit"); }
        if (shader == null) { shader = Shader.Find("Unlit/Transparent"); }
        if (shader == null) { shader = Shader.Find("Standard"); }
        Material createdMaterial = new Material(shader)
        {
            name = $"{name}_SwordTrailMaterial",
            hideFlags = HideFlags.DontSave
        };
        if (createdMaterial.HasProperty("_MainTex")) { createdMaterial.SetTexture("_MainTex", Texture2D.whiteTexture); }
        if (createdMaterial.HasProperty("_BaseMap")) { createdMaterial.SetTexture("_BaseMap", Texture2D.whiteTexture); }
        if (createdMaterial.HasProperty("_Color")) { createdMaterial.SetColor("_Color", trailColor); }
        if (createdMaterial.HasProperty("_BaseColor")) { createdMaterial.SetColor("_BaseColor", trailColor); }
        if (createdMaterial.HasProperty("_Cull")) { createdMaterial.SetInt("_Cull", (int)CullMode.Off); }
        createdMaterial.renderQueue = 3000; return createdMaterial;
    }
    private void UpdateTrail()
    {
        float now = Time.time; PruneExpiredSamples(now); if (ShouldEmitTrail())
        {
            Vector3 currentBasePosition = bladeBaseAnchor.position;
            Vector3 currentTipPosition = bladeTipAnchor.position;
            if (ShouldCaptureSample(now, currentBasePosition, currentTipPosition)) { AddSample(currentBasePosition, currentTipPosition, now); }
        }
        else { _hasLastSample = false; }
        RebuildTrailMesh(now);
    }
    private bool ShouldEmitTrail()
    {
        if (_actionScript == null || _itemSwitchScript == null) { return false; }
        if (_itemSwitchScript.item == null || _itemSwitchScript.item.itemobject != gameObject) { return false; }
        if (!_actionScript.TryGetActiveSwordAttackStateInfo(out _, out _)) { return false; }
        return HasVisibleRenderer();
    }
    private bool HasVisibleRenderer()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rendererTarget = renderers[i];
            if (rendererTarget != null && rendererTarget.enabled) { return true; }
        }
        return false;
    }
    private bool ShouldCaptureSample(float now, Vector3 currentBasePosition, Vector3 currentTipPosition)
    {
        if (!_hasLastSample) { return true; }
        if ((now - _lastSampleTime) < minimumSampleInterval) { return false; }
        float tipDistance = Vector3.Distance(_lastTipPosition, currentTipPosition);
        float baseDistance = Vector3.Distance(_lastBasePosition, currentBasePosition);
        return tipDistance >= minimumSampleDistance || baseDistance >= minimumSampleDistance;
    }
    private void AddSample(Vector3 currentBasePosition, Vector3 currentTipPosition, float timeStamp)
    {
        TrailSample sample = new TrailSample
        {
            basePosition = currentBasePosition,
            tipPosition = currentTipPosition,
            time = timeStamp
        }; _samples.Add(sample);
        _lastBasePosition = currentBasePosition; _lastTipPosition = currentTipPosition;
        _lastSampleTime = timeStamp; _hasLastSample = true;
    }
    private void PruneExpiredSamples(float now)
    {
        float lifetime = Mathf.Max(0.02f, sampleLifetime);
        for (int i = _samples.Count - 1; i >= 0; i--)
        {
            if ((now - _samples[i].time) <= lifetime) { continue; }
            _samples.RemoveAt(i);
        }
    }
    private void RebuildTrailMesh(float now)
    {
        if (_mesh == null || _meshRenderer == null) { return; }
        if (_samples.Count < 2) { _mesh.Clear(); _meshRenderer.enabled = false; return; }
        _vertices.Clear(); _colors.Clear(); _uvs.Clear(); _normals.Clear(); _triangles.Clear();
        float lifetime = Mathf.Max(0.02f, sampleLifetime); int segmentCount = _samples.Count - 1;
        for (int i = 0; i < _samples.Count; i++)
        {
            TrailSample sample = _samples[i];
            float age = Mathf.Clamp01((now - sample.time) / lifetime); float alpha = (1f - age);
            alpha *= alpha; Color sampleColor = trailColor; sampleColor.a *= alpha;
            _vertices.Add(sample.basePosition); _vertices.Add(sample.tipPosition);
            _colors.Add(sampleColor); _colors.Add(sampleColor);
            float v = segmentCount <= 0 ? 0f : (float)i / segmentCount; _uvs.Add(new Vector2(0f, v));
            _uvs.Add(new Vector2(1f, v)); _normals.Add(Vector3.forward);
            _normals.Add(Vector3.forward);
        }
        for (int i = 0; i < segmentCount; i++)
        {
            int start = i * 2; int next = start + 2; _triangles.Add(start); _triangles.Add(next);
            _triangles.Add(start + 1); _triangles.Add(next); _triangles.Add(next + 1);
            _triangles.Add(start + 1); _triangles.Add(start + 1); _triangles.Add(next);
            _triangles.Add(start); _triangles.Add(start + 1); _triangles.Add(next + 1);
            _triangles.Add(next);
        }
        _mesh.Clear(); _mesh.SetVertices(_vertices);
        _mesh.SetColors(_colors); _mesh.SetUVs(0, _uvs); _mesh.SetNormals(_normals);
        _mesh.SetTriangles(_triangles, 0, true); _mesh.RecalculateBounds();
        _meshRenderer.enabled = true;
    }
    private void ClearTrail()
    {
        _samples.Clear();
        _hasLastSample = false; _lastSampleTime = float.NegativeInfinity;
        if (_mesh != null) { _mesh.Clear(); }
        if (_meshRenderer != null) { _meshRenderer.enabled = false; }
    }
    private void DestroyTrailResources()
    {
        if (_trailObject != null)
        {
            Destroy(_trailObject);
            _trailObject = null;
        }
        if (_material != null) { Destroy(_material); _material = null; }
        if (_mesh != null) { Destroy(_mesh); _mesh = null; }
    }
}
