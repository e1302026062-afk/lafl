using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class B11PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float gravity = -15f;
    public float turnSpeed = 12f;

    [Header("Look")]
    public float lookSensitivity = 1f;
    public Transform cameraTransform;
    public Transform cameraPivot;

    // ★追加：割り当てられたデバイスを外部から参照するためのプロパティ
    public InputDevice PrimaryDevice { get; private set; }

    private CharacterController controller;
    private PlayerControls inputActions;

    private Vector3 velocity;
    private float xRotation = 0f;
    private float cameraYaw = 0f;

    public float speedMultiplier = 1f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputActions = new PlayerControls();
    }

    // デバイス（キーボードやコントローラー）を個別割り当てするメソッド
    public void AssignDevices(params InputDevice[] devices)
    {
        if (inputActions == null) inputActions = new PlayerControls();

        inputActions.devices = devices;

        // ★追加：1つ目の入力デバイス（キーボードまたはコントローラー）を保持
        if (devices != null && devices.Length > 0)
        {
            PrimaryDevice = devices[0];
        }

        inputActions.Enable();
    }

    void OnEnable()
    {
        // 入力デバイスの割り当てが完了するまで、全デバイス入力を受け付けない。
        if (inputActions != null && PrimaryDevice != null) inputActions.Enable();
    }

    void OnDisable()
    {
        if (inputActions != null) inputActions.Disable();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraPivot != null)
        {
            Vector3 pivotAngles = cameraPivot.localEulerAngles;
            cameraYaw = NormalizeAngle(pivotAngles.y);
            xRotation = NormalizeAngle(pivotAngles.x);
        }
    }

    void Update()
    {
        if (inputActions == null) return;

        Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        Vector2 lookInput = inputActions.Player.Look.ReadValue<Vector2>();
        bool jumpPressed = inputActions.Player.Jump.triggered;

        HandleLook(lookInput);
        HandleAllMovement(moveInput, jumpPressed);
    }

    void HandleLook(Vector2 lookInput)
    {
        float currentLookSensitivity = PrimaryDevice is Gamepad
            ? lookSensitivity * 1.5f
            : lookSensitivity;

        // スティック入力はフレームごとの値なので、60fps基準の時間補正を入れて
        // 実行環境のフレームレートによって視点感度が変わらないようにする。
        float controllerTimeScale = PrimaryDevice is Gamepad
            ? Time.unscaledDeltaTime * 60f
            : 1f;

        cameraYaw += lookInput.x * currentLookSensitivity * controllerTimeScale;
        float lookY = lookInput.y * currentLookSensitivity * controllerTimeScale;

        xRotation -= lookY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);

        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(xRotation, cameraYaw, 0f);
        }
        else if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

    }

    void HandleAllMovement(Vector2 moveInput, bool jumpPressed)
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float safeGravity = gravity > 0 ? -gravity : gravity;

        if (jumpPressed && controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * safeGravity);
        }

        velocity.y += safeGravity * Time.deltaTime;

        // ★ Vector3 move を定義してから finalVelocity の計算に使用します
        Vector3 cameraForward = cameraPivot != null ? cameraPivot.forward : transform.forward;
        cameraForward = Vector3.ProjectOnPlane(cameraForward, Vector3.up).normalized;
        if (cameraForward.sqrMagnitude < 0.001f) cameraForward = transform.forward;

        Vector3 cameraRight = Vector3.Cross(Vector3.up, cameraForward).normalized;
        Vector3 move = cameraRight * moveInput.x + cameraForward * moveInput.y;
        move = Vector3.ClampMagnitude(move, 1f);
        Vector3 horizontalMove = new Vector3(move.x, 0f, move.z);
        if (horizontalMove.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalMove.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        Vector3 finalVelocity = (move * (moveSpeed * speedMultiplier)) + velocity;

        controller.Move(finalVelocity * Time.deltaTime);
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    public void SetInputActive(bool isActive)
    {
        if (inputActions == null) return;

        if (isActive)
        {
            inputActions.Enable();  // 操作を受け付ける
        }
        else
        {
            inputActions.Disable(); // 操作を完全に無視する
        }
    }
}
