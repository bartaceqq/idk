using UnityEngine;
using UnityEngine.InputSystem; // nov˝ Input System

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    public ActionScript actionScript;
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float runSpeed = 10f;
    public float jumpSpeed = 8f;
    public float gravity = 20f;
    public float groundedStickForce = 2f; // malÈ p¯itlaËenÌ k zemi
    public bool disableExtraCapsuleCollider = true;

    

    [Header("Look")]
    public float mouseSensitivity = 0.1f; // n·sobÌ delta myöi
    public float minPitch = -90f;
    public float maxPitch = 90f;
    public Transform playerCamera; // odkaz na kameru (child) ñ nastav v Inspectoru

    // Input System (p¯es PlayerInput)
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;
    private InputAction _runAction;

    private CharacterController _cc;
    private CapsuleCollider _extraCapsuleCollider;
    private float _pitch;         // akumulovan· vertik·lnÌ rotace (X)
    private Vector3 _velocity;    // vnit¯nÌ rychlost (vË. Y)
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
            // CharacterController already provides collision. Extra collider can cause jitter.
            _extraCapsuleCollider.enabled = false;
        }

        if (_playerInput == null)
        {
            Debug.LogError("P¯idej na Player komponentu PlayerInput a p¯i¯aÔ akce (Action Map: Player).");
        }
        if (playerCamera == null)
        {
            Debug.LogWarning("ChybÌ reference na kameru");
        }
    }

    void OnEnable()
    {
        if (_playerInput != null && _playerInput.actions != null)
        {
            _moveAction = _playerInput.actions.FindAction("Move");
            _lookAction = _playerInput.actions.FindAction("Look");
            _jumpAction = _playerInput.actions.FindAction("Jump");
            _runAction = _playerInput.actions.FindAction("Run");

            // BezpeËÌ, aù neh·ûe NRE, kdyû akce neexistuje
            _moveAction?.Enable();
            _lookAction?.Enable();
            _jumpAction?.Enable();
            _runAction?.Enable();
        }

        Cursor.lockState = CursorLockMode.Locked;
    }

    void OnDisable()
    {
        _moveAction?.Disable();
        _lookAction?.Disable();
        _jumpAction?.Disable();
        _runAction?.Disable();
        Cursor.lockState = CursorLockMode.None;
    }

    void Update()
    {
        if (_playerInput == null || _moveAction == null || _lookAction == null) return;

        // --- LOOK (myö/gamepad) ---
        Vector2 look = _lookAction.ReadValue<Vector2>();


        // U myöi je look v pixelech per-frame; ök·luj citlivostÌ a neVIS Time.deltaTime
        float yaw = look.x * mouseSensitivity;
        float pitchDelta = -look.y * mouseSensitivity;

        _pitch = Mathf.Clamp(_pitch + pitchDelta, minPitch, maxPitch);
        // otoËenÌ tÏla (yaw)
        transform.Rotate(0f, yaw, 0f);
        // otoËenÌ kamery (pitch)
        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

        // --- MOVE (WASD) ---
        Vector2 moveInput = _moveAction.ReadValue<Vector2>(); // x=strafe, y=forward
        bool runPressed = _runAction != null ? _runAction.IsPressed() : (Keyboard.current?.leftShiftKey?.isPressed ?? false);
        bool canSprint = actionScript != null && actionScript.staminaScript != null ? actionScript.staminaScript.enoughstamina : true;

        if (_sprintLocked)
        {
            if (!runPressed)
            {
                _sprintLocked = false; // must release shift before sprinting again
            }
        }
        if (!canSprint && runPressed)
        {
            _sprintLocked = true;
        }

        bool isRunning = runPressed && canSprint && !_sprintLocked;
        float currentSpeed = isRunning ? runSpeed : moveSpeed;
        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y) * currentSpeed;

        UpdateMovementFlags(moveInput, isRunning);

        // --- GRAVITY + JUMP ---
        if (_cc.isGrounded)
        {
            // drû lehce z·pornou Y, aby Ñneplavalì na hran·ch
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

        // kombinace horizont·lnÌho pohybu + vertik·lnÌ rychlosti
        Vector3 finalVelocity = new Vector3(move.x, _velocity.y, move.z);
        _cc.Move(finalVelocity * Time.deltaTime);
    }

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

    private void RunCallbacks()
    {
        if (actionScript == null)
        {
            return;
        }

        bool anyWalk = _isForwardWalk || _isBackwardWalk || _isLeftWalk || _isRightWalk;
        bool anyRun = _isForwardRun || _isBackwardRun || _isLeftRun || _isRightRun;

        OnJump(_isJumping);
        actionScript.Idle(_isIdle);
        actionScript.Walk(anyWalk);
        actionScript.Sprint(anyRun);
    }

    public void OnJump(bool active)
    {
        if (active)
        {
        }
        else
        {
        }
    }

    public void OnIdle(bool active)
    {
        if (active)
        {
            
            actionScript.Idle(true);
        }
        else
        {
            actionScript.Idle(false);
        }
    }

    public void OnForwardWalk(bool active)
    {
        if (active)
        {
            actionScript.Walk(true);
        }
        else
        {
            actionScript.Walk(false);
        }
    }
    public void OnForwardRun(bool active)
    {
        if (active)
        {
            actionScript.Sprint(true);
        }
        else
        {
            actionScript.Sprint(false);
        }
    }
    public void OnBackwardWalk(bool active)
    {
        if (active)
        {
        }
        else
        {
        }
    }
    public void OnBackwardRun(bool active)
    {
        if (active)
        {
        }
        else
        {
        }
    }
    public void OnLeftWalk(bool active)
    {
        if (active)
        {
        }
        else
        {
        }
    }
    public void OnLeftRun(bool active)
    {
        if (active)
        {
        }
        else
        {
        }
    }
    public void OnRightWalk(bool active)
    {
        if (active)
        {
        }
        else
        {
        }
    }
    public void OnRightRun(bool active)
    {
        if (active)
        {
        }
        else
        {
        }
    }
}
