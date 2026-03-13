using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Controls gun equip, shooting and reload-magazine visuals.
public class GunItem : MonoBehaviour
{
    [Header("Input")]
    public KeyCode key = KeyCode.Alpha4;
    [SerializeField] private KeyCode reloadKey = KeyCode.R;
    [SerializeField] private KeyCode shootKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode aimKey = KeyCode.Mouse1;

    [Header("References")]
    [Tooltip("Magazine object currently attached to the weapon.")]
    public GameObject MagToRealod;
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private ActionScript actionScript;
    [SerializeField] private RayScript rayScript;
    [SerializeField] private FPSController fpsController;
    [SerializeField] private Transform leftHandSocket;
    [SerializeField] private GameObject leftHandMagPrefab;

    [Header("Ammo")]
    [SerializeField, Min(1)] private int magazineSize = 30;
    [SerializeField, Min(0)] private int ammoInMagazine = 30;
    [SerializeField] private bool autoReloadWhenEmpty = true;

    [Header("Shoot")]
    [SerializeField, Min(0.01f)] private float shootCooldown = 0.12f;
    [SerializeField, Min(0.01f)] private float shootStateDuration = 0.18f;
    [SerializeField] private bool useShootAnimation = false;

    [Header("Projectile")]
    [SerializeField] private bool spawnProjectileOnShoot = true;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 65f;
    [SerializeField, Min(0f)] private float projectileGravityForce = 0f;
    [SerializeField, Min(0.1f)] private float projectileLifetime = 8f;
    [SerializeField] private bool ignoreOwnerCollision = true;
    [SerializeField] private Vector3 projectileRotationOffset = new Vector3(90f, 0f, 0f);

    [Header("Procedural Recoil")]
    [SerializeField] private bool useProceduralRecoil = true;
    [SerializeField, Min(0f)] private float recoilPitchPerShot = 2.2f;
    [SerializeField, Min(0f)] private float maxRecoilPitch = 7f;
    [SerializeField, Min(0f)] private float recoilReturnSpeed = 18f;

    [Header("Reload")]
    [SerializeField, Min(0.05f)] private float reloadDuration = 1.7f;
    [SerializeField, Min(0f)] private float magazineEjectForce = 1.5f;
    [SerializeField, Min(0f)] private float magazineUpwardForce = 0.45f;
    [SerializeField, Min(0f)] private float magazineTorque = 0.6f;
    [SerializeField, Min(0.25f)] private float droppedMagazineLifetime = 8f;
    [SerializeField] private Vector3 leftHandMagLocalPosition = new Vector3(0.03f, 0.02f, 0.05f);
    [SerializeField] private Vector3 leftHandMagLocalEulerAngles = new Vector3(0f, 90f, 0f);
    [SerializeField] private Vector3 leftHandMagLocalScale = Vector3.one;

    [Header("Animator")]
    [SerializeField] private string upperBodyLayerName = "UpperBody";
    [SerializeField] private string upperBodyIdleStateName = "UpperBodyIdle";
    [SerializeField] private string aimStateName = "ARAim";
    [SerializeField] private string shootStateName = "Shoot";
    [SerializeField] private string reloadStateName = "ARReload";
    [SerializeField] private string gunEquippedBoolName = "GunEquipped";
    [SerializeField] private string gunShootTriggerName = "GunShoot";
    [SerializeField] private string gunReloadTriggerName = "GunReload";
    [SerializeField, Min(0f)] private float stateBlendTime = 0.03f;

    [Header("Visibility")]
    [SerializeField] private bool hideGunOnStart = true;
    [SerializeField] private bool disableCollidersWhenHolstered = true;

    [Header("Upper Body Aim")]
    [SerializeField] private bool rotateUpperBodyToLook = true;
    [SerializeField] private Transform upperBodyAimSource;
    [SerializeField] private bool includePitch = true;
    [SerializeField, Range(-90f, 90f)] private float upperBodyYawOffset = 0f;
    [SerializeField, Range(-45f, 45f)] private float upperBodyPitchOffset = 0f;
    [SerializeField, Range(0f, 120f)] private float maxUpperBodyYaw = 70f;
    [SerializeField, Range(0f, 80f)] private float maxUpperBodyPitch = 35f;
    [SerializeField, Min(0f)] private float upperBodyAimSmoothing = 14f;
    [SerializeField, Range(0f, 1f)] private float spineYawWeight = 0.35f;
    [SerializeField, Range(0f, 1f)] private float chestYawWeight = 0.45f;
    [SerializeField, Range(0f, 1f)] private float upperChestYawWeight = 0.2f;
    [SerializeField, Range(0f, 1f)] private float spinePitchWeight = 0.25f;
    [SerializeField, Range(0f, 1f)] private float chestPitchWeight = 0.45f;
    [SerializeField, Range(0f, 1f)] private float upperChestPitchWeight = 0.3f;
    [SerializeField] private bool rotateShouldersAndArmsToLook = true;
    [SerializeField, Range(0f, 1f)] private float shoulderYawWeight = 0.12f;
    [SerializeField, Range(0f, 1f)] private float shoulderPitchWeight = 0.12f;
    [SerializeField, Range(0f, 1f)] private float upperArmYawWeight = 0.08f;
    [SerializeField, Range(0f, 1f)] private float upperArmPitchWeight = 0.08f;

    [Header("Scope Camera")]
    [SerializeField] private bool overrideAimCameraSettings = true;
    [SerializeField] private Transform aimCameraAnchor;
    [SerializeField] private Vector3 aimCameraAnchorLocalOffset = Vector3.zero;
    [SerializeField] private Vector3 aimCameraPivotOffset = new Vector3(0f, 1.6f, 0.12f);
    [SerializeField, Min(0f)] private float aimCameraDistance = 0f;
    [SerializeField, Range(1f, 120f)] private float aimCameraFieldOfView = 20f;
    [SerializeField, Min(0f)] private float aimCameraTransitionSpeed = 18f;
    [SerializeField, Min(0f)] private float aimFieldOfViewTransitionSpeed = 18f;

    [Header("Aim Visibility")]
    [SerializeField] private bool autoHideHeadAndNeckWhileAiming = true;
    [SerializeField] private Renderer[] renderersHiddenWhileAiming;
    [SerializeField] private GameObject[] rendererParentObjectsHiddenWhileAiming;

    [Header("Crosshair")]
    public Image crosshairimage;
    [SerializeField] private Image aimCrosshairImage;
    private Renderer[] _gunRenderers;
    private Collider[] _gunColliders;
    private Transform _originalMagParent;
    private Vector3 _originalMagLocalPosition;
    private Quaternion _originalMagLocalRotation;
    private Vector3 _originalMagLocalScale;
    private GameObject _leftHandMagInstance;
    private Coroutine _reloadRoutine;
    private bool _gunVisible;
    private bool _isReloading;
    private bool _isAiming;
    public bool crossvisible = false;
    private float _nextShootTime;
    private int _upperBodyLayerIndex = -1;
    private int _shootStateToken;
    private Transform _spineBone;
    private Transform _chestBone;
    private Transform _upperChestBone;
    private Transform _leftShoulderBone;
    private Transform _rightShoulderBone;
    private Transform _leftUpperArmBone;
    private Transform _rightUpperArmBone;
    private float _smoothedAimYaw;
    private float _smoothedAimPitch;
    private float _currentRecoilPitch;
    private Collider[] _ownerColliders;
    private Renderer[] _resolvedAimHiddenRenderers;
    private bool[] _aimHiddenRendererPreviousStates;

    private void OnValidate()
    {
        magazineSize = Mathf.Max(1, magazineSize);
        ammoInMagazine = Mathf.Clamp(ammoInMagazine, 0, magazineSize);
        shootCooldown = Mathf.Max(0.01f, shootCooldown);
        shootStateDuration = Mathf.Max(0.01f, shootStateDuration);
        projectileSpeed = Mathf.Max(0.01f, projectileSpeed);
        projectileLifetime = Mathf.Max(0.1f, projectileLifetime);
        reloadDuration = Mathf.Max(0.05f, reloadDuration);
        droppedMagazineLifetime = Mathf.Max(0.25f, droppedMagazineLifetime);
    }

    private void Awake()
    {
        CacheComponents();
        CacheMagazineAttachPose();
        ResolveLeftHandSocket();
        RefreshAnimatorLayerIndex();
        CacheAimHiddenRenderers();

        bool startVisible = !hideGunOnStart;
        crossvisible = startVisible;
        SetGunVisible(startVisible);
        ApplyCombatInputBlock(startVisible);
        ApplyUpperBodyHold(startVisible);
        ApplyGunAnimatorVisibility(startVisible);
        ToggleCrossHair();
    }

    private void OnDisable()
    {
        SetAimMode(false);
        StopReloadAndRestoreMagazine();
        ApplyGunAnimatorVisibility(false);
        ApplyCombatInputBlock(false);
        ApplyUpperBodyHold(false);
        SetHoldShoulderCamera(false);
        ClearShotShoulderCamera();
    }
    public void ToggleCrossHair()
    {
        bool showAimCrosshair = crossvisible && _gunVisible && _isAiming && aimCrosshairImage != null;
        bool showDefaultCrosshair = crossvisible && _gunVisible && (!_isAiming || aimCrosshairImage == null);

        if (crosshairimage != null)
        {
            crosshairimage.enabled = showDefaultCrosshair;
        }

        if (aimCrosshairImage != null)
        {
            aimCrosshairImage.enabled = showAimCrosshair;
        }
    }
    private void Update()
    {
        if (IsUiBlockingGameplay())
        {
            SetAimMode(false);
            return;
        }

        if (Input.GetKeyDown(key) && !_isReloading)
        {
            ToggleGunVisible();
            ToggleCrossHair();
        }

        if (!_gunVisible)
        {
            SetAimMode(false);
            return;
        }

        SetAimMode(IsAimHeld() && !_isReloading);

        if (Input.GetKeyDown(reloadKey))
        {
            TryBeginReload();
            return;
        }

        if (DidPressShoot())
        {
            TryShoot();
        }
    }

    private void LateUpdate()
    {
        if (!rotateUpperBodyToLook || !_gunVisible || _isReloading || characterAnimator == null)
        {
            return;
        }

        UpdateProceduralRecoil(Time.deltaTime);
        ApplyUpperBodyAimRotation();
    }

    // Handle Toggle Gun Visible.
    private void ToggleGunVisible()
    {
        bool nextVisible = !_gunVisible;
        crossvisible = nextVisible;
        SetGunVisible(nextVisible);
        ApplyCombatInputBlock(nextVisible);
        ApplyUpperBodyHold(nextVisible);
        ApplyGunAnimatorVisibility(nextVisible);
    }

    // Handle Try Shoot.
    private void TryShoot()
    {
        if (_isReloading || Time.time < _nextShootTime)
        {
            return;
        }

        if (ammoInMagazine <= 0)
        {
            if (autoReloadWhenEmpty)
            {
                TryBeginReload();
            }
            return;
        }

        ammoInMagazine--;
        _nextShootTime = Time.time + shootCooldown;
        TriggerShotShoulderCameraPulse();
        AddRecoilKick();
        SpawnProjectile();

        if (useShootAnimation)
        {
            if (!TrySetAnimatorTrigger(gunShootTriggerName))
            {
                _shootStateToken++;
                StartCoroutine(PlayShootStateThenReturnToAim(_shootStateToken));
            }
        }
        else
        {
            // Keep aim pose stable when using procedural recoil-only firing.
            TryPlayUpperBodyState(aimStateName);
        }

        if (ammoInMagazine <= 0 && autoReloadWhenEmpty)
        {
            TryBeginReload();
        }
    }

    // Handle Try Begin Reload.
    private void TryBeginReload()
    {
        if (!_gunVisible || _isReloading)
        {
            return;
        }

        if (ammoInMagazine >= magazineSize)
        {
            return;
        }

        SetAimMode(false);
        _reloadRoutine = StartCoroutine(ReloadRoutine());
    }

    // Handle Play Shoot State Then Return To Aim.
    private IEnumerator PlayShootStateThenReturnToAim(int stateToken)
    {
        TryPlayUpperBodyState(shootStateName);
        yield return new WaitForSeconds(shootStateDuration);

        if (stateToken != _shootStateToken || !_gunVisible || _isReloading)
        {
            yield break;
        }

        TryPlayUpperBodyState(aimStateName);
    }

    // Handle Reload Routine.
    private IEnumerator ReloadRoutine()
    {
        _isReloading = true;
        _shootStateToken++;

        if (!TrySetAnimatorTrigger(gunReloadTriggerName))
        {
            TryPlayUpperBodyState(reloadStateName);
        }
        EjectMagazine();
        SpawnLeftHandMagazine();

        yield return new WaitForSeconds(reloadDuration);

        FinishReloadVisuals();
        ammoInMagazine = magazineSize;
        _isReloading = false;
        _reloadRoutine = null;

        if (!HasAnimatorParameter(gunEquippedBoolName, AnimatorControllerParameterType.Bool))
        {
            if (_gunVisible)
            {
                TryPlayUpperBodyState(aimStateName);
            }
            else
            {
                TryPlayUpperBodyState(upperBodyIdleStateName);
            }
        }
    }

    // Handle Stop Reload And Restore Magazine.
    private void StopReloadAndRestoreMagazine()
    {
        if (_reloadRoutine != null)
        {
            StopCoroutine(_reloadRoutine);
            _reloadRoutine = null;
        }

        _isReloading = false;
        _currentRecoilPitch = 0f;
        FinishReloadVisuals();
    }

    // Handle Eject Magazine.
    private void EjectMagazine()
    {
        if (MagToRealod == null || !MagToRealod.activeInHierarchy)
        {
            return;
        }

        GameObject droppedMag = Instantiate(
            MagToRealod,
            MagToRealod.transform.position,
            MagToRealod.transform.rotation);

        droppedMag.name = $"{MagToRealod.name}_Dropped";
        droppedMag.transform.localScale = MagToRealod.transform.lossyScale;

        Rigidbody rigidbody = droppedMag.GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = droppedMag.AddComponent<Rigidbody>();
        }

        if (!HasAnyCollider(droppedMag))
        {
            droppedMag.AddComponent<BoxCollider>();
        }

        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Vector3 ejectDirection = (-transform.right + (transform.up * 0.35f)).normalized;
        Vector3 impulse = (ejectDirection * magazineEjectForce) + (Vector3.up * magazineUpwardForce);
        rigidbody.AddForce(impulse, ForceMode.Impulse);
        rigidbody.AddTorque(Random.insideUnitSphere * magazineTorque, ForceMode.Impulse);

        Destroy(droppedMag, droppedMagazineLifetime);

        MagToRealod.SetActive(false);
    }

    // Handle Spawn Left Hand Magazine.
    private void SpawnLeftHandMagazine()
    {
        if (leftHandSocket == null)
        {
            return;
        }

        GameObject sourcePrefab = leftHandMagPrefab != null ? leftHandMagPrefab : MagToRealod;
        if (sourcePrefab == null)
        {
            return;
        }

        _leftHandMagInstance = Instantiate(sourcePrefab, leftHandSocket);
        _leftHandMagInstance.name = $"{sourcePrefab.name}_HandMag";
        _leftHandMagInstance.transform.localPosition = leftHandMagLocalPosition;
        _leftHandMagInstance.transform.localRotation = Quaternion.Euler(leftHandMagLocalEulerAngles);
        _leftHandMagInstance.transform.localScale = leftHandMagLocalScale;

        Rigidbody[] rigidbodies = _leftHandMagInstance.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            if (rigidbodies[i] == null)
            {
                continue;
            }

            rigidbodies[i].isKinematic = true;
            rigidbodies[i].useGravity = false;
        }

        Collider[] colliders = _leftHandMagInstance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }
    }

    // Handle Finish Reload Visuals.
    private void FinishReloadVisuals()
    {
        if (_leftHandMagInstance != null)
        {
            Destroy(_leftHandMagInstance);
            _leftHandMagInstance = null;
        }

        if (MagToRealod == null)
        {
            return;
        }

        Transform magTransform = MagToRealod.transform;
        if (_originalMagParent != null)
        {
            magTransform.SetParent(_originalMagParent, false);
        }

        magTransform.localPosition = _originalMagLocalPosition;
        magTransform.localRotation = _originalMagLocalRotation;
        magTransform.localScale = _originalMagLocalScale;
        MagToRealod.SetActive(true);
    }

    // Handle Cache Components.
    private void CacheComponents()
    {
        _gunRenderers = GetComponentsInChildren<Renderer>(true);
        _gunColliders = GetComponentsInChildren<Collider>(true);
        _ownerColliders = transform.root != null
            ? transform.root.GetComponentsInChildren<Collider>(true)
            : GetComponentsInParent<Collider>(true);

        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInParent<Animator>();
        }

        if (actionScript == null)
        {
            actionScript = GetComponentInParent<ActionScript>();
        }

        if (rayScript == null)
        {
            rayScript = GetComponentInParent<RayScript>();
            if (rayScript == null && transform.root != null)
            {
                rayScript = transform.root.GetComponentInChildren<RayScript>(true);
            }
        }

        if (fpsController == null)
        {
            fpsController = GetComponentInParent<FPSController>();
            if (fpsController == null && transform.root != null)
            {
                fpsController = transform.root.GetComponentInChildren<FPSController>(true);
            }
        }

        CacheUpperBodyBones();
    }

    // Handle Cache Magazine Attach Pose.
    private void CacheMagazineAttachPose()
    {
        if (MagToRealod == null)
        {
            return;
        }

        Transform magTransform = MagToRealod.transform;
        _originalMagParent = magTransform.parent;
        _originalMagLocalPosition = magTransform.localPosition;
        _originalMagLocalRotation = magTransform.localRotation;
        _originalMagLocalScale = magTransform.localScale;
    }

    // Handle Resolve Left Hand Socket.
    private void ResolveLeftHandSocket()
    {
        if (leftHandSocket != null || characterAnimator == null || !characterAnimator.isHuman)
        {
            return;
        }

        leftHandSocket = characterAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
    }

    // Handle Cache Upper Body Bones.
    private void CacheUpperBodyBones()
    {
        _spineBone = null;
        _chestBone = null;
        _upperChestBone = null;
        _leftShoulderBone = null;
        _rightShoulderBone = null;
        _leftUpperArmBone = null;
        _rightUpperArmBone = null;

        if (characterAnimator == null || !characterAnimator.isHuman)
        {
            return;
        }

        _spineBone = characterAnimator.GetBoneTransform(HumanBodyBones.Spine);
        _chestBone = characterAnimator.GetBoneTransform(HumanBodyBones.Chest);
        _upperChestBone = characterAnimator.GetBoneTransform(HumanBodyBones.UpperChest);
        _leftShoulderBone = characterAnimator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        _rightShoulderBone = characterAnimator.GetBoneTransform(HumanBodyBones.RightShoulder);
        _leftUpperArmBone = characterAnimator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        _rightUpperArmBone = characterAnimator.GetBoneTransform(HumanBodyBones.RightUpperArm);
    }

    // Handle Apply Upper Body Aim Rotation.
    private void ApplyUpperBodyAimRotation()
    {
        if (!HasAnyAimBones())
        {
            CacheUpperBodyBones();
            if (!HasAnyAimBones())
            {
                return;
            }
        }

        Transform lookSource = ResolveUpperBodyAimSource();
        if (lookSource == null)
        {
            return;
        }

        Vector3 localLookDirection = characterAnimator.transform.InverseTransformDirection(lookSource.forward);
        if (localLookDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        localLookDirection.Normalize();

        float targetYaw = Mathf.Atan2(localLookDirection.x, localLookDirection.z) * Mathf.Rad2Deg;
        targetYaw += upperBodyYawOffset;
        targetYaw = Mathf.Clamp(targetYaw, -maxUpperBodyYaw, maxUpperBodyYaw);

        float targetPitch = 0f;
        if (includePitch)
        {
            float horizontalMagnitude = new Vector2(localLookDirection.x, localLookDirection.z).magnitude;
            targetPitch = -Mathf.Atan2(localLookDirection.y, Mathf.Max(0.0001f, horizontalMagnitude)) * Mathf.Rad2Deg;
            targetPitch += upperBodyPitchOffset;
            targetPitch = Mathf.Clamp(targetPitch, -maxUpperBodyPitch, maxUpperBodyPitch);
        }

        if (upperBodyAimSmoothing <= 0f || !Application.isPlaying)
        {
            _smoothedAimYaw = targetYaw;
            _smoothedAimPitch = targetPitch;
        }
        else
        {
            float t = 1f - Mathf.Exp(-upperBodyAimSmoothing * Time.deltaTime);
            _smoothedAimYaw = Mathf.LerpAngle(_smoothedAimYaw, targetYaw, t);
            _smoothedAimPitch = Mathf.LerpAngle(_smoothedAimPitch, targetPitch, t);
        }

        float recoilPitch = useProceduralRecoil ? _currentRecoilPitch : 0f;
        float finalPitch = _smoothedAimPitch - recoilPitch;

        float yawNormalization = GetAimWeightNormalization(spineYawWeight, chestYawWeight, upperChestYawWeight);
        float pitchNormalization = GetAimWeightNormalization(spinePitchWeight, chestPitchWeight, upperChestPitchWeight);

        ApplyBoneAimRotation(
            _spineBone,
            characterAnimator.transform,
            _smoothedAimYaw * spineYawWeight * yawNormalization,
            finalPitch * spinePitchWeight * pitchNormalization);

        ApplyBoneAimRotation(
            _chestBone,
            characterAnimator.transform,
            _smoothedAimYaw * chestYawWeight * yawNormalization,
            finalPitch * chestPitchWeight * pitchNormalization);

        ApplyBoneAimRotation(
            _upperChestBone,
            characterAnimator.transform,
            _smoothedAimYaw * upperChestYawWeight * yawNormalization,
            finalPitch * upperChestPitchWeight * pitchNormalization);

        if (!rotateShouldersAndArmsToLook)
        {
            return;
        }

        ApplyBoneAimRotation(
            _leftShoulderBone,
            characterAnimator.transform,
            _smoothedAimYaw * shoulderYawWeight,
            finalPitch * shoulderPitchWeight);

        ApplyBoneAimRotation(
            _rightShoulderBone,
            characterAnimator.transform,
            _smoothedAimYaw * shoulderYawWeight,
            finalPitch * shoulderPitchWeight);

        ApplyBoneAimRotation(
            _leftUpperArmBone,
            characterAnimator.transform,
            _smoothedAimYaw * upperArmYawWeight,
            finalPitch * upperArmPitchWeight);

        ApplyBoneAimRotation(
            _rightUpperArmBone,
            characterAnimator.transform,
            _smoothedAimYaw * upperArmYawWeight,
            finalPitch * upperArmPitchWeight);
    }

    // Handle Resolve Upper Body Aim Source.
    private Transform ResolveUpperBodyAimSource()
    {
        if (upperBodyAimSource != null)
        {
            return upperBodyAimSource;
        }

        if (fpsController != null && fpsController.playerCamera != null)
        {
            return fpsController.playerCamera;
        }

        Camera mainCamera = Camera.main;
        return mainCamera != null ? mainCamera.transform : null;
    }

    // Handle Has Any Aim Bones.
    private bool HasAnyAimBones()
    {
        return _spineBone != null ||
               _chestBone != null ||
               _upperChestBone != null ||
               _leftShoulderBone != null ||
               _rightShoulderBone != null ||
               _leftUpperArmBone != null ||
               _rightUpperArmBone != null;
    }

    // Handle Apply Bone Aim Rotation.
    private static void ApplyBoneAimRotation(Transform bone, Transform characterRoot, float yawDegrees, float pitchDegrees)
    {
        if (bone == null || characterRoot == null)
        {
            return;
        }

        Quaternion yawRotation = Quaternion.AngleAxis(yawDegrees, characterRoot.up);
        Quaternion pitchRotation = Quaternion.AngleAxis(pitchDegrees, characterRoot.right);
        bone.rotation = yawRotation * pitchRotation * bone.rotation;
    }

    // Handle Get Aim Weight Normalization.
    private static float GetAimWeightNormalization(params float[] weights)
    {
        if (weights == null || weights.Length == 0)
        {
            return 1f;
        }

        float total = 0f;
        for (int i = 0; i < weights.Length; i++)
        {
            total += Mathf.Max(0f, weights[i]);
        }

        if (total <= 1f || total <= 0.0001f)
        {
            return 1f;
        }

        return 1f / total;
    }

    // Handle Refresh Animator Layer Index.
    private void RefreshAnimatorLayerIndex()
    {
        _upperBodyLayerIndex = -1;
        if (characterAnimator == null || string.IsNullOrWhiteSpace(upperBodyLayerName))
        {
            return;
        }

        _upperBodyLayerIndex = characterAnimator.GetLayerIndex(upperBodyLayerName);
    }

    // Handle Apply Combat Input Block.
    private void ApplyCombatInputBlock(bool active)
    {
        if (rayScript != null)
        {
            rayScript.blockAttackInput = active;
        }
    }

    // Handle Apply Upper Body Hold.
    private void ApplyUpperBodyHold(bool active)
    {
        if (actionScript != null)
        {
            actionScript.SetUpperBodyExternalHold(active);
        }
    }

    // Handle Apply Gun Animator Visibility.
    private void ApplyGunAnimatorVisibility(bool visible)
    {
        bool usedBool = TrySetAnimatorBool(gunEquippedBoolName, visible);

        if (!visible)
        {
            ClearShotShoulderCamera();
        }

        // Fallback for controllers that do not have the gun bool parameter yet.
        if (!usedBool)
        {
            if (visible)
            {
                TryPlayUpperBodyState(aimStateName);
            }
            else
            {
                TryPlayUpperBodyState(upperBodyIdleStateName);
            }
        }
    }

    // Handle Set Gun Visible.
    private void SetGunVisible(bool visible)
    {
        _gunVisible = visible;
        if (!visible)
        {
            SetAimMode(false);
        }

        SetHoldShoulderCamera(visible);

        if (!visible)
        {
            _currentRecoilPitch = 0f;
        }

        if (_gunRenderers != null)
        {
            for (int i = 0; i < _gunRenderers.Length; i++)
            {
                if (_gunRenderers[i] != null)
                {
                    _gunRenderers[i].enabled = visible;
                }
            }
        }

        if (disableCollidersWhenHolstered && _gunColliders != null)
        {
            for (int i = 0; i < _gunColliders.Length; i++)
            {
                if (_gunColliders[i] != null)
                {
                    _gunColliders[i].enabled = visible;
                }
            }
        }

        ToggleCrossHair();
    }

    // Handle Try Play Upper Body State.
    private bool TryPlayUpperBodyState(string stateName)
    {
        if (characterAnimator == null || string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        if (_upperBodyLayerIndex < 0)
        {
            RefreshAnimatorLayerIndex();
            if (_upperBodyLayerIndex < 0)
            {
                return false;
            }
        }

        int fullPathHash = Animator.StringToHash($"{upperBodyLayerName}.{stateName}");
        int shortNameHash = Animator.StringToHash(stateName);

        int targetStateHash;
        if (characterAnimator.HasState(_upperBodyLayerIndex, fullPathHash))
        {
            targetStateHash = fullPathHash;
        }
        else if (characterAnimator.HasState(_upperBodyLayerIndex, shortNameHash))
        {
            targetStateHash = shortNameHash;
        }
        else
        {
            return false;
        }

        if (stateBlendTime > 0f)
        {
            characterAnimator.CrossFadeInFixedTime(targetStateHash, stateBlendTime, _upperBodyLayerIndex);
        }
        else
        {
            characterAnimator.Play(targetStateHash, _upperBodyLayerIndex, 0f);
        }

        return true;
    }

    // Handle Did Press Shoot.
    private bool DidPressShoot()
    {
        if (shootKey == KeyCode.Mouse0)
        {
            return Input.GetMouseButtonDown(0);
        }

        if (shootKey == KeyCode.Mouse1)
        {
            return Input.GetMouseButtonDown(1);
        }

        if (shootKey == KeyCode.Mouse2)
        {
            return Input.GetMouseButtonDown(2);
        }

        return Input.GetKeyDown(shootKey);
    }

    // Handle Is Aim Held.
    private bool IsAimHeld()
    {
        if (aimKey == KeyCode.Mouse0)
        {
            return Input.GetMouseButton(0);
        }

        if (aimKey == KeyCode.Mouse1)
        {
            return Input.GetMouseButton(1);
        }

        if (aimKey == KeyCode.Mouse2)
        {
            return Input.GetMouseButton(2);
        }

        return Input.GetKey(aimKey);
    }

    // Handle Has Any Collider.
    private static bool HasAnyCollider(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    // Handle Has Animator Parameter.
    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (characterAnimator == null || string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = characterAnimator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].type == type && parameters[i].name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    // Handle Try Set Animator Bool.
    private bool TrySetAnimatorBool(string parameterName, bool value)
    {
        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
        {
            return false;
        }

        characterAnimator.SetBool(parameterName, value);
        return true;
    }

    // Handle Try Set Animator Trigger.
    private bool TrySetAnimatorTrigger(string parameterName)
    {
        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
        {
            return false;
        }

        characterAnimator.SetTrigger(parameterName);
        return true;
    }

    // Handle Is UIBlocking Gameplay.
    private static bool IsUiBlockingGameplay()
    {
        return InventoryController.IsInventoryOpen ||
               InventoryManager.IsInventoryOpen ||
               CraftingManager.IsCraftingOpen ||
               DialogueState.IsConversationRunning;
    }

    // Handle Spawn Projectile.
    private void SpawnProjectile()
    {
        if (!spawnProjectileOnShoot || projectilePrefab == null)
        {
            return;
        }

        Transform spawnTransform = projectileSpawnPoint != null ? projectileSpawnPoint : transform;
        Vector3 spawnPosition = spawnTransform.position;
        Vector3 shootDirection = ResolveProjectileDirection(spawnPosition);

        if (shootDirection.sqrMagnitude < 0.0001f)
        {
            shootDirection = spawnTransform.forward;
        }

        shootDirection.Normalize();

        Quaternion rotation = Quaternion.LookRotation(shootDirection) * Quaternion.Euler(projectileRotationOffset);
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, rotation);

        Rigidbody projectileRigidbody = projectile.GetComponent<Rigidbody>();
        if (projectileRigidbody != null)
        {
            projectileRigidbody.linearVelocity = shootDirection * projectileSpeed;
            if (projectileGravityForce > 0f)
            {
                projectileRigidbody.AddForce(Vector3.down * projectileGravityForce, ForceMode.Acceleration);
            }
        }

        if (ignoreOwnerCollision)
        {
            IgnoreProjectileCollisions(projectile);
        }

        Destroy(projectile, projectileLifetime);
    }

    // Handle Resolve Projectile Direction.
    private Vector3 ResolveProjectileDirection(Vector3 spawnPosition)
    {
        Transform lookSource = ResolveUpperBodyAimSource();
        if (lookSource == null)
        {
            return (projectileSpawnPoint != null ? projectileSpawnPoint.forward : transform.forward);
        }

        Ray ray = new Ray(lookSource.position, lookSource.forward);
        if (TryGetClosestNonOwnerHit(ray, out RaycastHit hit))
        {
            Vector3 toHit = hit.point - spawnPosition;
            if (toHit.sqrMagnitude > 0.0001f)
            {
                return toHit.normalized;
            }
        }

        return ray.direction.normalized;
    }

    // Handle Try Get Closest Non Owner Hit.
    private bool TryGetClosestNonOwnerHit(Ray ray, out RaycastHit closestHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, 2000f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            closestHit = default;
            return false;
        }

        float bestDistance = float.MaxValue;
        int bestIndex = -1;
        for (int i = 0; i < hits.Length; i++)
        {
            Collider collider = hits[i].collider;
            if (collider == null || collider.transform.IsChildOf(transform.root))
            {
                continue;
            }

            if (hits[i].distance < bestDistance)
            {
                bestDistance = hits[i].distance;
                bestIndex = i;
            }
        }

        if (bestIndex < 0)
        {
            closestHit = default;
            return false;
        }

        closestHit = hits[bestIndex];
        return true;
    }

    // Handle Ignore Projectile Collisions.
    private void IgnoreProjectileCollisions(GameObject projectile)
    {
        if (projectile == null || _ownerColliders == null || _ownerColliders.Length == 0)
        {
            return;
        }

        Collider[] projectileColliders = projectile.GetComponentsInChildren<Collider>(true);
        if (projectileColliders == null || projectileColliders.Length == 0)
        {
            return;
        }

        for (int i = 0; i < projectileColliders.Length; i++)
        {
            Collider projectileCollider = projectileColliders[i];
            if (projectileCollider == null)
            {
                continue;
            }

            for (int j = 0; j < _ownerColliders.Length; j++)
            {
                Collider ownerCollider = _ownerColliders[j];
                if (ownerCollider != null)
                {
                    Physics.IgnoreCollision(projectileCollider, ownerCollider, true);
                }
            }
        }
    }

    // Handle Add Recoil Kick.
    private void AddRecoilKick()
    {
        if (!useProceduralRecoil || recoilPitchPerShot <= 0f)
        {
            return;
        }

        _currentRecoilPitch = Mathf.Clamp(_currentRecoilPitch + recoilPitchPerShot, 0f, maxRecoilPitch);
    }

    // Handle Update Procedural Recoil.
    private void UpdateProceduralRecoil(float deltaTime)
    {
        if (!useProceduralRecoil || _currentRecoilPitch <= 0f)
        {
            return;
        }

        _currentRecoilPitch = Mathf.MoveTowards(
            _currentRecoilPitch,
            0f,
            Mathf.Max(0f, recoilReturnSpeed) * Mathf.Max(0f, deltaTime));
    }

    // Handle Trigger Shot Shoulder Camera Pulse.
    private void TriggerShotShoulderCameraPulse()
    {
        if (fpsController == null)
        {
            return;
        }

        float holdTime = Mathf.Max(shootCooldown, shootStateDuration);
        fpsController.TriggerShotShoulderCamera(holdTime);
    }

    // Handle Clear Shot Shoulder Camera.
    private void ClearShotShoulderCamera()
    {
        if (fpsController != null)
        {
            fpsController.ClearShotShoulderCamera();
        }
    }

    // Handle Set Hold Shoulder Camera.
    private void SetHoldShoulderCamera(bool active)
    {
        if (fpsController != null)
        {
            fpsController.SetHoldShoulderCamera(active);
        }
    }

    // Handle Set Aim Mode.
    private void SetAimMode(bool active)
    {
        bool nextAiming = active && _gunVisible && !_isReloading;
        if (_isAiming == nextAiming)
        {
            return;
        }

        _isAiming = nextAiming;
        ApplyAimHiddenRenderers(_isAiming);

        if (fpsController != null)
        {
            if (_isAiming)
            {
                ApplyAimCameraSettings();
            }
            else
            {
                ClearAimCameraSettings();
            }

            fpsController.SetAimCameraActive(_isAiming);
        }

        ToggleCrossHair();
    }

    // Handle Apply Aim Camera Settings.
    private void ApplyAimCameraSettings()
    {
        if (fpsController == null)
        {
            return;
        }

        if (!overrideAimCameraSettings)
        {
            fpsController.ClearAimCameraOverride();
            return;
        }

        fpsController.SetAimCameraOverride(
            aimCameraPivotOffset,
            aimCameraDistance,
            aimCameraFieldOfView,
            aimCameraTransitionSpeed,
            aimFieldOfViewTransitionSpeed,
            aimCameraAnchor,
            aimCameraAnchorLocalOffset);
    }

    // Handle Clear Aim Camera Settings.
    private void ClearAimCameraSettings()
    {
        if (fpsController != null)
        {
            fpsController.ClearAimCameraOverride();
        }
    }

    // Handle Cache Aim Hidden Renderers.
    private void CacheAimHiddenRenderers()
    {
        var resolvedRenderers = new List<Renderer>();
        AddUniqueRenderers(resolvedRenderers, renderersHiddenWhileAiming);
        AddUniqueRenderersFromObjects(resolvedRenderers, rendererParentObjectsHiddenWhileAiming);

        if (autoHideHeadAndNeckWhileAiming)
        {
            Transform searchRoot = characterAnimator != null ? characterAnimator.transform.root : transform.root;
            if (searchRoot != null)
            {
                Renderer[] allRenderers = searchRoot.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < allRenderers.Length; i++)
                {
                    Renderer renderer = allRenderers[i];
                    if (renderer == null || IsGunRenderer(renderer))
                    {
                        continue;
                    }

                    if (!IsHeadOrNeckRenderer(renderer))
                    {
                        continue;
                    }

                    AddUniqueRenderer(resolvedRenderers, renderer);
                }
            }
        }

        _resolvedAimHiddenRenderers = resolvedRenderers.ToArray();
        _aimHiddenRendererPreviousStates = new bool[_resolvedAimHiddenRenderers.Length];
    }

    // Handle Apply Aim Hidden Renderers.
    private void ApplyAimHiddenRenderers(bool hidden)
    {
        if (_resolvedAimHiddenRenderers == null)
        {
            CacheAimHiddenRenderers();
        }

        if (_resolvedAimHiddenRenderers == null || _resolvedAimHiddenRenderers.Length == 0)
        {
            return;
        }

        for (int i = 0; i < _resolvedAimHiddenRenderers.Length; i++)
        {
            Renderer renderer = _resolvedAimHiddenRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (hidden)
            {
                _aimHiddenRendererPreviousStates[i] = renderer.enabled;
                renderer.enabled = false;
                continue;
            }

            renderer.enabled = _aimHiddenRendererPreviousStates[i];
        }
    }

    // Handle Is Gun Renderer.
    private bool IsGunRenderer(Renderer renderer)
    {
        if (renderer == null || _gunRenderers == null)
        {
            return false;
        }

        for (int i = 0; i < _gunRenderers.Length; i++)
        {
            if (_gunRenderers[i] == renderer)
            {
                return true;
            }
        }

        return false;
    }

    // Handle Is Head Or Neck Renderer.
    private static bool IsHeadOrNeckRenderer(Renderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        Transform current = renderer.transform;
        while (current != null)
        {
            if (MatchesAimHiddenName(current.name))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    // Handle Matches Aim Hidden Name.
    private static bool MatchesAimHiddenName(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return false;
        }

        return objectName.Contains("Head", System.StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Neck", System.StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Hair", System.StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Eye", System.StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Brow", System.StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Lash", System.StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Beard", System.StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Jaw", System.StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Face", System.StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Mask", System.StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Hat", System.StringComparison.OrdinalIgnoreCase) ||
               objectName.Contains("Helmet", System.StringComparison.OrdinalIgnoreCase);
    }

    // Handle Add Unique Renderers From Objects.
    private void AddUniqueRenderersFromObjects(List<Renderer> results, GameObject[] source)
    {
        if (results == null || source == null)
        {
            return;
        }

        for (int i = 0; i < source.Length; i++)
        {
            GameObject parentObject = source[i];
            if (parentObject == null)
            {
                continue;
            }

            Renderer[] renderers = parentObject.GetComponentsInChildren<Renderer>(true);
            for (int j = 0; j < renderers.Length; j++)
            {
                Renderer renderer = renderers[j];
                if (renderer == null || IsGunRenderer(renderer))
                {
                    continue;
                }

                AddUniqueRenderer(results, renderer);
            }
        }
    }

    // Handle Add Unique Renderers.
    private static void AddUniqueRenderers(List<Renderer> results, Renderer[] source)
    {
        if (results == null || source == null)
        {
            return;
        }

        for (int i = 0; i < source.Length; i++)
        {
            AddUniqueRenderer(results, source[i]);
        }
    }

    // Handle Add Unique Renderer.
    private static void AddUniqueRenderer(List<Renderer> results, Renderer renderer)
    {
        if (results == null || renderer == null || results.Contains(renderer))
        {
            return;
        }

        results.Add(renderer);
    }
}
