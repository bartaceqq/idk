using UnityEngine;
using UnityEngine.InputSystem; // nový Input System

[RequireComponent(typeof(CharacterController))]
public class FPSControllerTest : MonoBehaviour
{
   
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float runSpeed = 10f;
    public float jumpSpeed = 8f;
    public float gravity = 20f;
    public float groundedStickForce = 2f; // malé přitlačení k zemi

    [Header("Look")]
    public float mouseSensitivity = 0.1f; // násobí delta myši
    public float minPitch = -90f;
    public float maxPitch = 90f;
    public Transform playerCamera; // odkaz na kameru (child) – nastav v Inspectoru

    // Input System (přes PlayerInput)
    private PlayerInput _playerInput;
    private InputAction _moveAction;
    private InputAction _lookAction;
    private InputAction _jumpAction;
    private InputAction _runAction;

    private CharacterController _cc;
    private float _pitch;         // akumulovaná vertikální rotace (X)
    private Vector3 _velocity;    // vnitřní rychlost (vč. Y)
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
        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("Přidej na Player komponentu PlayerInput a přiřaď akce (Action Map: Player).");
        }
        if (playerCamera == null)
        {
            Debug.LogWarning("Chybí reference na kameru");
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

            // Bezpečí, ať neháže NRE, když akce neexistuje
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
        if (_playerInput == null) return;

        // --- LOOK (myš/gamepad) ---
        Vector2 look = _lookAction.ReadValue<Vector2>();


        // U myši je look v pixelech per-frame; škáluj citlivostí a neVIS Time.deltaTime
        float yaw = look.x * mouseSensitivity;
        float pitchDelta = -look.y * mouseSensitivity;

        _pitch = Mathf.Clamp(_pitch + pitchDelta, minPitch, maxPitch);
        // otočení těla (yaw)
        transform.Rotate(0f, yaw, 0f);
        // otočení kamery (pitch)
        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

        // --- MOVE (WASD) ---
        Vector2 moveInput = _moveAction.ReadValue<Vector2>(); // x=strafe, y=forward
        bool runPressed = _runAction != null ? _runAction.IsPressed() : (Keyboard.current?.leftShiftKey?.isPressed ?? false);
        bool canSprint = true;
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
            // drž lehce zápornou Y, aby „neplaval“ na hranách
            _velocity.y = -groundedStickForce;

            if (_jumpAction.triggered)
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

        // kombinace horizontálního pohybu + vertikální rychlosti
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
        
    }

    public void OnForwardWalk(bool active)
    {
      
    }
    public void OnForwardRun(bool active)
    {
    
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
