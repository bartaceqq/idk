using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
// Controls test movement and third-person camera behavior.
public class FPSControllerTest : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float runSpeed = 10f;
    public float jumpSpeed = 8f;
    public float gravity = 20f;
    public float groundedStickForce = 2f;
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
    private bool _sprintLocked;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _extraCapsuleCollider = GetComponent<CapsuleCollider>();
        _playerInput = GetComponent<PlayerInput>();

        if (disableExtraCapsuleCollider && _extraCapsuleCollider != null)
        {
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

        Vector2 look = _lookAction.ReadValue<Vector2>();
        _yaw += look.x * mouseSensitivity;
        _pitch = Mathf.Clamp(_pitch - (look.y * mouseSensitivity), minPitch, maxPitch);

        Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
        transform.rotation = yawRotation;

        Vector2 moveInput = _moveAction.ReadValue<Vector2>();
        bool runPressed = _runAction != null
            ? _runAction.IsPressed()
            : (Keyboard.current?.leftShiftKey?.isPressed ?? false);
        bool canSprint = true;

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

        if (_cc.isGrounded)
        {
            _velocity.y = -groundedStickForce;

            if (_jumpAction != null && _jumpAction.triggered)
            {
                _velocity.y = jumpSpeed;
            }
        }
        else
        {
            _velocity.y -= gravity * Time.deltaTime;
        }

        _isJumping = _jumpAction != null && _jumpAction.triggered;
        RunCallbacks();

        Vector3 finalVelocity = new Vector3(move.x, _velocity.y, move.z);
        _cc.Move(finalVelocity * Time.deltaTime);
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

    // Handle Update Movement Flags.
    private void UpdateMovementFlags(Vector2 moveInput, bool isRunning)
    {
        const float deadzone = 0.1f;
        bool hasInput = moveInput.sqrMagnitude >= deadzone * deadzone;
        bool forwardDominant = Mathf.Abs(moveInput.y) >= Mathf.Abs(moveInput.x);

        bool forward = hasInput && forwardDominant && moveInput.y > 0f;
        bool backward = hasInput && forwardDominant && moveInput.y < 0f;
        bool right = hasInput && !forwardDominant && moveInput.x > 0f;
        bool left = hasInput && !forwardDominant && moveInput.x < 0f;

        _isIdle = !hasInput;
        _isForwardWalk = forward && !isRunning;
        _isForwardRun = forward && isRunning;
        _isBackwardWalk = backward && !isRunning;
        _isBackwardRun = backward && isRunning;
        _isLeftWalk = left && !isRunning;
        _isLeftRun = left && isRunning;
        _isRightWalk = right && !isRunning;
        _isRightRun = right && isRunning;
    }

    // Handle Run Callbacks.
    private void RunCallbacks()
    {
        OnJump(_isJumping);
        OnIdle(_isIdle);
        OnForwardWalk(_isForwardWalk);
        OnForwardRun(_isForwardRun);
        OnBackwardWalk(_isBackwardWalk);
        OnBackwardRun(_isBackwardRun);
        OnLeftWalk(_isLeftWalk);
        OnLeftRun(_isLeftRun);
        OnRightWalk(_isRightWalk);
        OnRightRun(_isRightRun);
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

    // Handle On Jump.
    public void OnJump(bool active)
    {
    }

    // Handle On Idle.
    public void OnIdle(bool active)
    {
    }

    // Handle On Forward Walk.
    public void OnForwardWalk(bool active)
    {
    }

    // Handle On Forward Run.
    public void OnForwardRun(bool active)
    {
    }

    // Handle On Backward Walk.
    public void OnBackwardWalk(bool active)
    {
    }

    // Handle On Backward Run.
    public void OnBackwardRun(bool active)
    {
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
}
