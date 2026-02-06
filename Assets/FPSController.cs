using UnityEngine;
using UnityEngine.InputSystem; // nový Input System

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
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

    private CharacterController _cc;
    private float _pitch;         // akumulovaná vertikální rotace (X)
    private Vector3 _velocity;    // vnitřní rychlost (vč. Y)

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

            // Bezpečí, ať neháže NRE, když akce neexistuje
            _moveAction?.Enable();
            _lookAction?.Enable();
            _jumpAction?.Enable();
        }

        Cursor.lockState = CursorLockMode.Locked;
    }

    void OnDisable()
    {
        _moveAction?.Disable();
        _lookAction?.Disable();
        _jumpAction?.Disable();
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
        Vector3 move = (transform.right * moveInput.x + transform.forward * moveInput.y) * moveSpeed;

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

        // kombinace horizontálního pohybu + vertikální rychlosti
        Vector3 finalVelocity = new Vector3(move.x, _velocity.y, move.z);
        _cc.Move(finalVelocity * Time.deltaTime);
    }
}
