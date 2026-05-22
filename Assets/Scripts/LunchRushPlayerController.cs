using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public sealed class LunchRushPlayerController : MonoBehaviour
{
    [Header("Run")]
    [SerializeField] private float startSpeed = 5f;
    [SerializeField] private float maxSpeed = 13f;
    [SerializeField] private float speedGainPerSecond = 0.18f;
    [SerializeField] private float turnSensitivity = 0.12f;
    [SerializeField] private float mouseDeadzone = 5f;
    [SerializeField] private float maxTurnDegreesPerFrame = 2f;
    [SerializeField] private bool lockCursor = true;
    [SerializeField] private bool debugMouseTurn = false;
    [SerializeField] private float debugLogInterval = 0.25f;

    [Header("Jump")]
    [SerializeField] private float jumpPower = 7f;
    [SerializeField] private float gravity = -22f;
    [SerializeField] private string runStateName = "HumanF@Sprint01_Forward";
    [SerializeField] private string jumpStateName = "HumanM@Jump01";
    [SerializeField] private float animationFadeTime = 0.06f;
    [SerializeField] private float jumpReturnDelay = 0.2f;

    [Header("Dash")]
    [SerializeField] private float dashSpeedBonus = 6f;
    [SerializeField] private float dashDuration = 0.22f;
    [SerializeField] private float dashCooldown = 0.75f;
    [SerializeField] private float dashViewHoldDuration = 0.65f;

    [Header("Hit")]
    [SerializeField] private int maxHearts = 3;
    [SerializeField] private float hitSpeedLoss = 2f;
    [SerializeField] private float hitCooldown = 0.7f;
    [SerializeField] private float slowDuration = 0.9f;
    [SerializeField] private float slowMultiplier = 0.45f;

    [Header("Slide")]
    [SerializeField] private float slideDuration = 0.55f;
    [SerializeField] private float slideScaleY = 0.5f;
    [SerializeField] private float slideScaleSpeed = 14f;
    [SerializeField] private Transform modelRoot;

    private CharacterController controller;
    private Animator animator;
    private float currentSpeed;
    private float verticalVelocity;
    private float dashTimer;
    private float dashViewTimer;
    private float dashCooldownTimer;
    private float slideTimer;
    private float jumpReturnTimer;
    private float hitCooldownTimer;
    private float slowTimer;
    private float controlledYaw;
    private float debugLogTimer;
    private float lastYaw;
    private int ignoreMouseFrames;
    private Vector3 normalScale;
    private HeartUI heartUI;
    private FollowCamera followCamera;
    private int hearts;
    private bool jumpAnimationActive;
    private bool dead;

    public float CurrentSpeed
    {
        get
        {
            float speed = currentSpeed + (dashTimer > 0f ? dashSpeedBonus : 0f);
            return slowTimer > 0f ? speed * slowMultiplier : speed;
        }
    }

    public bool IsDashViewActive => dashViewTimer > 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        currentSpeed = startSpeed;
        modelRoot = modelRoot != null ? modelRoot : transform;
        normalScale = modelRoot.localScale;
        controlledYaw = transform.eulerAngles.y;
        lastYaw = transform.eulerAngles.y;
        hearts = maxHearts;
        heartUI = FindFirstObjectByType<HeartUI>();
        followCamera = FindFirstObjectByType<FollowCamera>();

        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        Debug.Log($"[LunchRushPlayerController] Awake object={name}, controllersOnObject={GetComponents<LunchRushPlayerController>().Length}, characterController={(controller != null)}, animator={(animator != null)}");
    }

    private void Start()
    {
        if (heartUI == null)
        {
            heartUI = HeartUI.CreateRuntime();
        }

        heartUI.SetHearts(hearts, maxHearts);
    }

    private void OnEnable()
    {
        if (lockCursor)
        {
            LockCursor();
            ignoreMouseFrames = 3;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && lockCursor)
        {
            LockCursor();
            ignoreMouseFrames = 3;
        }
    }

    private void Update()
    {
        if (dead)
        {
            return;
        }

        float yawBeforeFrame = transform.eulerAngles.y;

        ApplyControlledYaw();
        ReadInput();
        UpdateTimers();
        TurnWithMouse();
        UpdateMovement();
        ApplyControlledYaw();
        UpdateAnimationState();
        UpdateSlideScale();

        LogYawChange(yawBeforeFrame);
    }

    private void LateUpdate()
    {
        ApplyControlledYaw();
    }

    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        UpdateCursorLock(keyboard, mouse);

        if (keyboard == null)
        {
            return;
        }

        if (controller.isGrounded && (keyboard.spaceKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame))
        {
            verticalVelocity = jumpPower;
            PlayJumpAnimation();
        }

        if (dashCooldownTimer <= 0f && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame))
        {
            dashTimer = dashDuration;
            dashViewTimer = dashViewHoldDuration;
            dashCooldownTimer = dashCooldown;
        }

        if (slideTimer <= 0f && controller.isGrounded && (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame))
        {
            slideTimer = slideDuration;
        }
    }

    private void UpdateTimers()
    {
        currentSpeed = Mathf.Min(maxSpeed, currentSpeed + speedGainPerSecond * Time.deltaTime);

        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
        }

        if (dashViewTimer > 0f)
        {
            dashViewTimer -= Time.deltaTime;
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (slideTimer > 0f)
        {
            slideTimer -= Time.deltaTime;
        }

        if (jumpReturnTimer > 0f)
        {
            jumpReturnTimer -= Time.deltaTime;
        }

        if (hitCooldownTimer > 0f)
        {
            hitCooldownTimer -= Time.deltaTime;
        }

        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
        }
    }

    private void TurnWithMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        if (ignoreMouseFrames > 0)
        {
            LogMouseTurn(0f, 0f, $"ignored startup frame {ignoreMouseFrames}");
            ignoreMouseFrames--;
            return;
        }

        float mouseX = mouse.delta.ReadValue().x;
        if (Mathf.Abs(mouseX) < mouseDeadzone)
        {
            LogMouseTurn(mouseX, 0f, "deadzone");
            return;
        }

        float turnDegrees = Mathf.Clamp(mouseX * turnSensitivity, -maxTurnDegreesPerFrame, maxTurnDegreesPerFrame);
        controlledYaw += turnDegrees;
        ApplyControlledYaw();
        LogMouseTurn(mouseX, turnDegrees, "applied");
    }

    private void UpdateMovement()
    {
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 forward = Quaternion.Euler(0f, controlledYaw, 0f) * Vector3.forward;
        Vector3 move = forward * CurrentSpeed;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }

    private void UpdateSlideScale()
    {
        Vector3 wantedScale = normalScale;
        if (slideTimer > 0f)
        {
            wantedScale.y = normalScale.y * slideScaleY;
        }

        modelRoot.localScale = Vector3.Lerp(modelRoot.localScale, wantedScale, Time.deltaTime * slideScaleSpeed);
    }

    private void PlayJumpAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(jumpStateName))
        {
            return;
        }

        if (PlayAnimatorState(jumpStateName))
        {
            jumpAnimationActive = true;
            jumpReturnTimer = jumpReturnDelay;
        }
    }

    private void UpdateAnimationState()
    {
        if (!jumpAnimationActive || jumpReturnTimer > 0f || !controller.isGrounded)
        {
            return;
        }

        if (PlayAnimatorState(runStateName))
        {
            jumpAnimationActive = false;
        }
    }

    private bool PlayAnimatorState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        int stateHash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, stateHash))
        {
            return false;
        }

        animator.CrossFadeInFixedTime(stateHash, animationFadeTime);
        return true;
    }

    private void UpdateCursorLock(Keyboard keyboard, Mouse mouse)
    {
        if (!lockCursor)
        {
            return;
        }

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            LockCursor();
            ignoreMouseFrames = 3;
        }
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ApplyControlledYaw()
    {
        transform.rotation = Quaternion.Euler(0f, controlledYaw, 0f);
    }

    public void HitObstacle(string obstacleName)
    {
        if (hitCooldownTimer > 0f)
        {
            return;
        }

        hitCooldownTimer = hitCooldown;
        slowTimer = slowDuration;
        currentSpeed = Mathf.Max(startSpeed, currentSpeed - hitSpeedLoss);
        hearts = Mathf.Max(0, hearts - 1);
        heartUI?.SetHearts(hearts, maxHearts);

        if (followCamera == null)
        {
            followCamera = FindFirstObjectByType<FollowCamera>();
        }

        followCamera?.Shake(0.45f, hearts <= 0 ? 1.2f : 0.75f);
        Debug.Log($"[LunchRushHit] obstacle={obstacleName}, hearts={hearts}/{maxHearts}, speed={CurrentSpeed:0.##}");

        if (hearts <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        dead = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        heartUI?.ShowGameOver();
        followCamera?.Shake(0.9f, 1.4f);
        Debug.Log("[LunchRushGameOver]");
    }

    private void LogMouseTurn(float mouseX, float turnDegrees, string reason)
    {
        if (!debugMouseTurn)
        {
            return;
        }

        debugLogTimer -= Time.deltaTime;
        if (debugLogTimer > 0f && reason != "applied")
        {
            return;
        }

        debugLogTimer = debugLogInterval;
        Debug.Log($"[LunchRushMouse] reason={reason}, mouseX={mouseX:0.###}, turn={turnDegrees:0.###}, yaw={transform.eulerAngles.y:0.###}, lock={Cursor.lockState}, visible={Cursor.visible}");
    }

    private void LogYawChange(float yawBeforeFrame)
    {
        if (!debugMouseTurn)
        {
            return;
        }

        float yawNow = transform.eulerAngles.y;
        float yawDelta = Mathf.DeltaAngle(yawBeforeFrame, yawNow);
        float yawDeltaFromLast = Mathf.DeltaAngle(lastYaw, yawNow);
        lastYaw = yawNow;

        if (Mathf.Abs(yawDelta) > 0.01f || Mathf.Abs(yawDeltaFromLast) > 0.01f)
        {
            Debug.Log($"[LunchRushYaw] frameDelta={yawDelta:0.###}, fromLast={yawDeltaFromLast:0.###}, yaw={yawNow:0.###}");
        }
    }
}
