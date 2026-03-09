using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
// Controls player movement and third-person camera behavior.
public class FPSController : MonoBehaviour
{
    public ActionScript actionScript;

    [Header("Movement")]
    public float moveSpeed = 6f;
    public float runSpeed = 10f;
    public float jumpSpeed = 8f;
    public float jumpInputCooldownSeconds = 0.2f;
    public float jumpAnimationLockSeconds = 0.18f;
    public float groundedAnimationGraceSeconds = 0.1f;
    public float gravity = 20f;
    public float groundedStickForce = 2f;
    public float coyoteTimeSeconds = 0.12f;
    public float jumpBufferSeconds = 0.12f;
    public bool disableExtraCapsuleCollider = true;

    [Header("Look")]
    public float mouseSensitivity = 0.1f;
    public float minPitch = -60f;
    public float maxPitch = 75f;
    public Transform playerCamera;

    [Header("Third Person Camera")]
    public Vector3 cameraPivotOffset = new Vector3(0f, 1.6f, 0f);
    public float cameraDistance = 4.5f;
    public float minCameraDistance = 1.2f;
    public float cameraCollisionRadius = 0.2f;
    public float cameraSmoothSpeed = 20f;
    public float cameraReturnSpeed = 6f;
    public float cameraSnapInSpeed = 25f;
    public LayerMask cameraCollisionMask = ~0;

    [Header("Camera Shake")]
    public bool enableCameraShake = false;
    public float cameraShakeAmplitude = 0.04f;
    public float cameraShakeFrequency = 20f;

    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;
    private InputAction _runAction;

    private CharacterController _cc;
    private CapsuleCollider _extraCapsuleCollider;
    private readonly RaycastHit[] _cameraHits = new RaycastHit[8];

    private float _pitch;
    private float _yaw;
    private Vector3 _velocity;
    private Vector3 _cameraVelocity;
    private float _currentCameraDistance;
    private bool _cameraDistanceInitialized;
    private float _cameraShakeSeed;

    private bool _isJumping;
    private bool _isIdle;
    private bool _isForwardWalk;
    private bool _isForwardRun;
    private bool _isBackwardWalk;
    private bool _isBackwardRun;
    private bool _isLeftWalk;
    private bool _isLeftRun;
    private bool _isRightWalk;
    private bool _isRightRun;
    private bool _isForwardLeftWalk;
    private bool _isForwardLeftRun;
    private bool _isForwardRightWalk;
    private bool _isForwardRightRun;
    private bool _sprintLocked;
    private bool _jumpTriggeredThisFrame;
    private float _nextJumpAllowedTime;
    private float _lastGroundedTime = -100f;
    private float _lastJumpPressedTime = -100f;
    private float _jumpAnimationLockUntil;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _extraCapsuleCollider = GetComponent<CapsuleCollider>();
        _playerInput = GetComponent<PlayerInput>();

        if (disableExtraCapsuleCollider && _extraCapsuleCollider != null)
        {
            // CharacterController already handles collisions for this player.
            _extraCapsuleCollider.enabled = false;
        }

        if (_playerInput == null)
        {
            Debug.LogError("Missing PlayerInput component.");
        }

        if (playerCamera == null)
        {
            Debug.LogWarning("Missing player camera reference.");
        }

        _cameraShakeSeed = Random.Range(0f, 1000f);
    }

    void OnEnable()
    {
        if (_playerInput != null && _playerInput.actions != null)
        {
            _playerInput.ActivateInput();
            _moveAction = _playerInput.actions.FindAction("Move");
            _lookAction = _playerInput.actions.FindAction("Look");
            _jumpAction = _playerInput.actions.FindAction("Jump");
            _runAction = _playerInput.actions.FindAction("Sprint");
            if (_runAction == null)
            {
                _runAction = _playerInput.actions.FindAction("Run");
            }
        }

        SyncLookAnglesFromTransforms();
        UpdateThirdPersonCamera(0f);
        _nextJumpAllowedTime = 0f;
        _jumpAnimationLockUntil = 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        _playerInput?.DeactivateInput();
        _moveAction = null;
        _lookAction = null;
        _jumpAction = null;
        _runAction = null;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        if (_playerInput == null || _moveAction == null || _lookAction == null)
        {
            return;
        }

        if (IsUiBlockingGameplay())
        {
            if (_cc.isGrounded)
            {
                _velocity.y = -groundedStickForce;
                _lastGroundedTime = Time.time;
            }
            else
            {
                _velocity.y -= gravity * Time.deltaTime;
            }

            _jumpTriggeredThisFrame = false;
            _isJumping = !_cc.isGrounded;
            _isIdle = true;
            _isForwardWalk = false;
            _isForwardRun = false;
            _isBackwardWalk = false;
            _isBackwardRun = false;
            _isLeftWalk = false;
            _isLeftRun = false;
            _isRightWalk = false;
            _isRightRun = false;
            _isForwardLeftWalk = false;
            _isForwardLeftRun = false;
            _isForwardRightWalk = false;
            _isForwardRightRun = false;

            RunCallbacks();
            _cc.Move(new Vector3(0f, _velocity.y, 0f) * Time.deltaTime);
            return;
        }

        if (!InventoryController.IsInventoryOpen && !InventoryManager.IsInventoryOpen)
        {
            Vector2 look = _lookAction.ReadValue<Vector2>();
            _yaw += look.x * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - (look.y * mouseSensitivity), minPitch, maxPitch);
        }

        Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
        transform.rotation = yawRotation;

        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        bool runPressed = _runAction != null
            ? _runAction.IsPressed()
            : (Keyboard.current?.leftShiftKey?.isPressed ?? false);
        bool canSprint = actionScript != null && actionScript.staminaScript != null
            ? actionScript.staminaScript.enoughstamina
            : true;

        if (_sprintLocked && !runPressed)
        {
            _sprintLocked = false;
        }

        if (!canSprint && runPressed)
        {
            _sprintLocked = true;
        }

        bool isRunning = runPressed && canSprint && !_sprintLocked;
        float currentSpeed = isRunning ? runSpeed : moveSpeed;
        Vector3 moveDirection = yawRotation * new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 move = moveDirection * currentSpeed;

        UpdateMovementFlags(moveInput, isRunning);

        _jumpTriggeredThisFrame = false;
        bool jumpPressedThisFrame = _jumpAction != null && _jumpAction.triggered;
        if (jumpPressedThisFrame)
        {
            _lastJumpPressedTime = Time.time;
        }

        bool isGroundedBeforeMove = _cc.isGrounded;
        if (isGroundedBeforeMove)
        {
            _lastGroundedTime = Time.time;
        }

        bool jumpBuffered = (Time.time - _lastJumpPressedTime) <= Mathf.Max(0f, jumpBufferSeconds);
        bool jumpWithGroundGrace = isGroundedBeforeMove || (Time.time - _lastGroundedTime) <= Mathf.Max(0f, coyoteTimeSeconds);
        bool canStartJump = jumpBuffered && jumpWithGroundGrace && Time.time >= _nextJumpAllowedTime;

        if (canStartJump)
        {
            _velocity.y = jumpSpeed;
            _jumpTriggeredThisFrame = true;
            _nextJumpAllowedTime = Time.time + Mathf.Max(0f, jumpInputCooldownSeconds);
            _lastJumpPressedTime = -100f;
            _lastGroundedTime = -100f;
            _jumpAnimationLockUntil = Time.time + Mathf.Max(0f, jumpAnimationLockSeconds);
        }
        else if (isGroundedBeforeMove)
        {
            _velocity.y = -groundedStickForce;
        }
        else
        {
            _velocity.y -= gravity * Time.deltaTime;
        }

        Vector3 finalVelocity = new Vector3(move.x, _velocity.y, move.z);
        _cc.Move(finalVelocity * Time.deltaTime);

        if (_cc.isGrounded)
        {
            _lastGroundedTime = Time.time;
        }

        bool animationGrounded = _cc.isGrounded ||
            (Time.time - _lastGroundedTime) <= Mathf.Max(0f, groundedAnimationGraceSeconds);
        _isJumping = !animationGrounded;
        RunCallbacks();
    }

    void LateUpdate()
    {
        UpdateThirdPersonCamera(Time.deltaTime);
    }

    // Handle Update Third Person Camera.
    private void UpdateThirdPersonCamera(float deltaTime)
    {
        if (playerCamera == null)
        {
            return;
        }

        Quaternion lookRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 pivot = transform.position + cameraPivotOffset;
        float requestedDistance = Mathf.Max(minCameraDistance, cameraDistance);
        float resolvedDistance = requestedDistance;
        Vector3 backward = -(lookRotation * Vector3.forward);

        int hitCount = Physics.SphereCastNonAlloc(
            pivot,
            cameraCollisionRadius,
            backward,
            _cameraHits,
            requestedDistance,
            cameraCollisionMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            Collider hitCollider = _cameraHits[i].collider;
            if (hitCollider == null || hitCollider.transform.IsChildOf(transform))
            {
                continue;
            }

            if (_cameraHits[i].distance > 0.001f && _cameraHits[i].distance < resolvedDistance)
            {
                resolvedDistance = _cameraHits[i].distance;
            }
        }

        float targetDistance = Mathf.Max(minCameraDistance, resolvedDistance - 0.05f);
        if (!_cameraDistanceInitialized || deltaTime <= 0f)
        {
            _currentCameraDistance = targetDistance;
            _cameraDistanceInitialized = true;
        }
        else
        {
            float distanceSpeed = targetDistance < _currentCameraDistance ? cameraSnapInSpeed : cameraReturnSpeed;
            _currentCameraDistance = Mathf.MoveTowards(_currentCameraDistance, targetDistance, distanceSpeed * deltaTime);
        }

        Vector3 desiredPosition = pivot + (backward * _currentCameraDistance);
        desiredPosition += GetCameraShakeOffset();

        if (deltaTime > 0f && cameraSmoothSpeed > 0f)
        {
            float smoothTime = 1f / cameraSmoothSpeed;
            playerCamera.position = Vector3.SmoothDamp(
                playerCamera.position,
                desiredPosition,
                ref _cameraVelocity,
                smoothTime,
                Mathf.Infinity,
                deltaTime);
        }
        else
        {
            _cameraVelocity = Vector3.zero;
            playerCamera.position = desiredPosition;
        }

        playerCamera.rotation = lookRotation;
    }

    // Handle Sync Look Angles From Transforms.
    private void SyncLookAnglesFromTransforms()
    {
        if (playerCamera != null)
        {
            _yaw = playerCamera.eulerAngles.y;
            _pitch = Mathf.Clamp(NormalizeAngle(playerCamera.eulerAngles.x), minPitch, maxPitch);
        }
        else
        {
            _yaw = transform.eulerAngles.y;
            _pitch = 0f;
        }

        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        _cameraDistanceInitialized = false;
        _currentCameraDistance = Mathf.Max(minCameraDistance, cameraDistance);
        _cameraVelocity = Vector3.zero;
    }

    // Handle Normalize Angle.
    private static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    // Handle Get Camera Shake Offset.
    private Vector3 GetCameraShakeOffset()
    {
        if (!enableCameraShake || cameraShakeAmplitude <= 0f || cameraShakeFrequency <= 0f)
        {
            return Vector3.zero;
        }

        float time = Time.time * cameraShakeFrequency;
        float x = (Mathf.PerlinNoise(_cameraShakeSeed, time) * 2f) - 1f;
        float y = (Mathf.PerlinNoise(_cameraShakeSeed + 1f, time) * 2f) - 1f;
        float z = (Mathf.PerlinNoise(_cameraShakeSeed + 2f, time) * 2f) - 1f;
        return new Vector3(x, y, z) * cameraShakeAmplitude;
    }

    // Handle Update Movement Flags.
    private void UpdateMovementFlags(Vector2 moveInput, bool isRunning)
    {
        const float deadzone = 0.1f;
        bool xPositive = moveInput.x > deadzone;
        bool xNegative = moveInput.x < -deadzone;
        bool yPositive = moveInput.y > deadzone;
        bool yNegative = moveInput.y < -deadzone;
        bool hasInput = xPositive || xNegative || yPositive || yNegative;

        bool forwardOnly = yPositive && !xPositive && !xNegative;
        bool backwardAny = yNegative;
        bool leftOnly = xNegative && !yPositive && !yNegative;
        bool rightOnly = xPositive && !yPositive && !yNegative;
        bool forwardLeft = yPositive && xNegative;
        bool forwardRight = yPositive && xPositive;

        _isIdle = !hasInput;
        _isForwardWalk = forwardOnly && !isRunning;
        _isForwardRun = forwardOnly && isRunning;
        _isBackwardWalk = backwardAny && !isRunning;
        _isBackwardRun = backwardAny && isRunning;
        _isLeftWalk = leftOnly && !isRunning;
        _isLeftRun = leftOnly && isRunning;
        _isRightWalk = rightOnly && !isRunning;
        _isRightRun = rightOnly && isRunning;
        _isForwardLeftWalk = forwardLeft && !isRunning;
        _isForwardLeftRun = forwardLeft && isRunning;
        _isForwardRightWalk = forwardRight && !isRunning;
        _isForwardRightRun = forwardRight && isRunning;
    }

    // Handle Run Callbacks.
    private void RunCallbacks()
    {
        if (actionScript == null)
        {
            return;
        }

        if (_jumpTriggeredThisFrame)
        {
            actionScript.Jump();
        }

        bool jumpLocked = Time.time < _jumpAnimationLockUntil;
        if (jumpLocked)
        {
            actionScript.Idle(false);
            actionScript.Walk(false);
            actionScript.WalkBackwards(false);
            actionScript.WalkLeft(false);
            actionScript.WalkRight(false);
            actionScript.WalkForwardLeft(false);
            actionScript.WalkForwardRight(false);
            actionScript.Sprint(false, false);
            actionScript.SprintForwardLeft(false);
            actionScript.SprintForwardRight(false);
            return;
        }

        if (actionScript.IsMovementAnimationLocked())
        {
            actionScript.Idle(false);
            actionScript.Walk(false);
            actionScript.WalkBackwards(false);
            actionScript.WalkLeft(false);
            actionScript.WalkRight(false);
            actionScript.WalkForwardLeft(false);
            actionScript.WalkForwardRight(false);
            actionScript.Sprint(false, false);
            actionScript.SprintForwardLeft(false);
            actionScript.SprintForwardRight(false);
            return;
        }

        bool forwardWalk = _isForwardWalk;
        bool forwardRun = _isForwardRun;
        bool backward = _isBackwardWalk || _isBackwardRun;
        bool anyRun = _isForwardRun || _isBackwardRun || _isLeftRun || _isRightRun || _isForwardLeftRun || _isForwardRightRun;

        if (_isJumping)
        {
            actionScript.Idle(false);
            actionScript.Walk(false);
            actionScript.WalkBackwards(false);
            actionScript.WalkLeft(false);
            actionScript.WalkRight(false);
            actionScript.WalkForwardLeft(false);
            actionScript.WalkForwardRight(false);
            actionScript.Sprint(false, false);
            actionScript.SprintForwardLeft(false);
            actionScript.SprintForwardRight(false);
            return;
        }

        OnJump(_isJumping);
        actionScript.Idle(_isIdle);
        actionScript.Walk(forwardWalk);
        actionScript.WalkBackwards(backward);
        actionScript.WalkLeft(_isLeftWalk || _isLeftRun);
        actionScript.WalkRight(_isRightWalk || _isRightRun);
        actionScript.WalkForwardLeft(_isForwardLeftWalk);
        actionScript.WalkForwardRight(_isForwardRightWalk);
        actionScript.Sprint(anyRun, forwardRun);
        actionScript.SprintForwardLeft(_isForwardLeftRun);
        actionScript.SprintForwardRight(_isForwardRightRun);
    }

    // Handle On Jump.
    public void OnJump(bool active)
    {
    }

    // Keep these handlers so PlayerInput Send Messages mode does not throw.
    public void OnJump(InputValue value)
    {
    }

    // Handle On Move.
    public void OnMove(InputValue value)
    {
    }

    // Handle On Look.
    public void OnLook(InputValue value)
    {
    }

    // Handle On Sprint.
    public void OnSprint(InputValue value)
    {
    }

    // Handle On Idle.
    public void OnIdle(bool active)
    {
        if (actionScript == null)
        {
            return;
        }

        actionScript.Idle(active);
    }

    // Handle On Forward Walk.
    public void OnForwardWalk(bool active)
    {
        if (actionScript == null)
        {
            return;
        }

        actionScript.Walk(active);
    }

    // Handle On Forward Run.
    public void OnForwardRun(bool active)
    {
        if (actionScript == null)
        {
            return;
        }

        actionScript.Sprint(active, active);
    }

    // Handle On Backward Walk.
    public void OnBackwardWalk(bool active)
    {
        if (actionScript == null)
        {
            return;
        }

        actionScript.WalkBackwards(active);
    }

    // Handle On Backward Run.
    public void OnBackwardRun(bool active)
    {
        if (actionScript == null)
        {
            return;
        }

        actionScript.WalkBackwards(active);
        actionScript.Sprint(active, false);
    }

    // Handle On Left Walk.
    public void OnLeftWalk(bool active)
    {
    }

    // Handle On Left Run.
    public void OnLeftRun(bool active)
    {
    }

    // Handle On Right Walk.
    public void OnRightWalk(bool active)
    {
    }

    // Handle On Right Run.
    public void OnRightRun(bool active)
    {
    }

    // Handle Is UIBlocking Gameplay.
    private static bool IsUiBlockingGameplay()
    {
        return InventoryController.IsInventoryOpen || InventoryManager.IsInventoryOpen || CraftingManager.IsCraftingOpen || VisualCommunication.IsTalking;
    }
}
