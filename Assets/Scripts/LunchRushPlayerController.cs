using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

    [Header("Game Over FX")]
    [SerializeField] private bool playGameOverFx = true;
    [SerializeField] private ParticleSystem gameOverParticles;
    [SerializeField] private Color gameOverTint = Color.white;
    [SerializeField] private float gameOverTintDuration = 0.18f;
    [SerializeField] private int gameOverFlashCount = 3;
    [SerializeField] private float gameOverFlashInterval = 0.04f;
    [SerializeField] private float gameOverPopScale = 1.35f;
    [SerializeField] private float gameOverPopDuration = 0.1f;
    [SerializeField] private float gameOverShrinkDuration = 0.2f;
    [SerializeField] private float gameOverSpinDegrees = 1080f;
    [SerializeField] private float gameOverSpinDuration = 0.35f;
    [SerializeField] private Vector3 gameOverSpinAxis = new Vector3(0f, 1f, 0f);
    [SerializeField] private float gameOverParticleScale = 4.5f;
    [SerializeField] private int gameOverParticleBursts = 3;
    [SerializeField] private float gameOverParticleBurstDelay = 0.04f;
    [SerializeField] private Vector3 gameOverParticleOffset = new Vector3(0f, 1.15f, 0f);
    [SerializeField] private bool gameOverParticleUsePlayerRotation = true;
    [SerializeField] private float gameOverDestroyDelay = 0.05f;
    [SerializeField] private bool disableCollisionsOnGameOver = true;
    [SerializeField] private bool disableAnimatorOnGameOver = true;
    [SerializeField] private bool freezeCameraOnGameOver = true;
    [SerializeField] private float freezeCameraDelay = 0.06f;
    [SerializeField] private bool lockDeathPosition = true;
    [SerializeField] private bool lockDeathRotation = false;
    [SerializeField] private bool restartOnAnyKey = true;
    [SerializeField] private float gameOverInputDelay = 0.35f;

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
    private Renderer[] renderers;
    private Color[] baseColors;
    private Collider[] colliders;
    private Coroutine gameOverRoutine;
    private Vector3 deathPosition;
    private Quaternion deathRotation;
    private float deathTime;

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
        GameSettings.Apply();
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        currentSpeed = startSpeed;
        modelRoot = modelRoot != null ? modelRoot : transform;
        normalScale = modelRoot.localScale;
        controlledYaw = transform.eulerAngles.y;
        lastYaw = transform.eulerAngles.y;
        hearts = maxHearts;
        heartUI = FindAnyObjectByType<HeartUI>();
        followCamera = FindAnyObjectByType<FollowCamera>();
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
        CacheBaseColors();

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
            if (restartOnAnyKey && Time.time - deathTime >= gameOverInputDelay && IsRestartPressed())
            {
                RestartScene();
            }

            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (SettingsMenu.IsOpen)
            {
                SettingsMenu.CloseMenu();
            }
            else
            {
                SettingsMenu.OpenMenu();
            }

            return;
        }

        if (SettingsMenu.IsOpen)
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
        if (dead)
        {
            if (lockDeathPosition)
            {
                transform.position = deathPosition;
            }

            if (lockDeathRotation)
            {
                transform.rotation = deathRotation;
            }

            return;
        }

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

        float sensitivity = GameSettings.SensitivityMultiplier;
        float turnAmount = mouseX * turnSensitivity * sensitivity;
        float maxTurn = maxTurnDegreesPerFrame * sensitivity;
        float turnDegrees = Mathf.Clamp(turnAmount, -maxTurn, maxTurn);
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
        if (!lockCursor || SettingsMenu.IsOpen)
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
            followCamera = FindAnyObjectByType<FollowCamera>();
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
        if (dead)
        {
            return;
        }

        dead = true;
        deathPosition = transform.position;
        deathRotation = transform.rotation;
        deathTime = Time.time;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        heartUI?.ShowGameOver();

        if (followCamera == null)
        {
            followCamera = FindAnyObjectByType<FollowCamera>();
        }

        followCamera?.Shake(0.9f, 1.4f);
        if (freezeCameraOnGameOver && followCamera != null)
        {
            if (freezeCameraDelay <= 0f)
            {
                followCamera.enabled = false;
            }
            else
            {
                StartCoroutine(FreezeCameraAfterDelay());
            }
        }

        Debug.Log("[LunchRushGameOver]");

        if (gameOverRoutine != null)
        {
            StopCoroutine(gameOverRoutine);
        }

        gameOverRoutine = StartCoroutine(PlayGameOverFx());
    }

    private IEnumerator FreezeCameraAfterDelay()
    {
        float delay = Mathf.Max(0f, freezeCameraDelay);
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (followCamera != null)
        {
            followCamera.enabled = false;
        }
    }

    private void CacheBaseColors()
    {
        if (renderers == null || renderers.Length == 0)
        {
            baseColors = null;
            return;
        }

        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i] != null ? renderers[i].material : null;
            if (material != null && material.HasProperty("_Color"))
            {
                baseColors[i] = material.color;
            }
            else if (material != null && material.HasProperty("_BaseColor"))
            {
                baseColors[i] = material.GetColor("_BaseColor");
            }
            else
            {
                baseColors[i] = Color.white;
            }
        }
    }

    private void ApplyTint(float t)
    {
        if (renderers == null || baseColors == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i] != null ? renderers[i].material : null;
            if (material == null)
            {
                continue;
            }

            Color baseColor = baseColors.Length > i ? baseColors[i] : Color.white;
            Color tint = Color.Lerp(baseColor, gameOverTint, t);
            if (material.HasProperty("_Color"))
            {
                material.color = tint;
            }
            else if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", tint);
            }
        }
    }

    private void DisableCollisions()
    {
        if (colliders == null)
        {
            return;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }
    }

    private void SpawnGameOverParticles()
    {
        if (gameOverParticles == null)
        {
            return;
        }

        int bursts = Mathf.Max(1, gameOverParticleBursts);
        StartCoroutine(SpawnParticleBursts(bursts));
    }

    private IEnumerator SpawnParticleBursts(int bursts)
    {
        float delay = Mathf.Max(0f, gameOverParticleBurstDelay);
        for (int i = 0; i < bursts; i++)
        {
            SpawnParticleBurst();
            if (i < bursts - 1 && delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
        }
    }

    private void SpawnParticleBurst()
    {
        Transform root = GetModelTransform();
        Vector3 position = root.TransformPoint(gameOverParticleOffset);
        Quaternion rotation = gameOverParticleUsePlayerRotation ? transform.rotation : Quaternion.identity;
        ParticleSystem particles = Instantiate(gameOverParticles, position, rotation);
        float scale = Mathf.Max(0.01f, gameOverParticleScale);
        particles.transform.localScale = particles.transform.localScale * scale;
        particles.Play();

        ParticleSystem.MainModule main = particles.main;
        float lifetime = main.duration;
        lifetime += main.startLifetime.constantMax;
        Destroy(particles.gameObject, lifetime);
    }

    private Vector3 GetModelScale()
    {
        return GetModelTransform().localScale;
    }

    private void SetModelScale(Vector3 scale)
    {
        GetModelTransform().localScale = scale;
    }

    private Transform GetModelTransform()
    {
        return modelRoot != null ? modelRoot : transform;
    }

    private Quaternion GetModelRotation()
    {
        return GetModelTransform().localRotation;
    }

    private void SetModelRotation(Quaternion rotation)
    {
        Transform root = GetModelTransform();
        if (root == transform && lockDeathRotation)
        {
            return;
        }

        root.localRotation = rotation;
    }

    private IEnumerator PlayGameOverFx()
    {
        if (disableCollisionsOnGameOver)
        {
            DisableCollisions();
        }

        if (controller != null)
        {
            controller.enabled = false;
        }

        if (animator != null && disableAnimatorOnGameOver)
        {
            animator.enabled = false;
        }

        if (!playGameOverFx)
        {
            if (gameOverDestroyDelay > 0f)
            {
                yield return new WaitForSeconds(gameOverDestroyDelay);
            }

            if (restartOnAnyKey)
            {
                SetModelScale(Vector3.zero);
                yield break;
            }

            Destroy(gameObject);
            yield break;
        }

        SpawnGameOverParticles();

        float tintTime = Mathf.Max(0.01f, gameOverTintDuration);
        float timer = 0f;
        while (timer < tintTime)
        {
            timer += Time.deltaTime;
            ApplyTint(Mathf.Clamp01(timer / tintTime));
            yield return null;
        }

        int flashes = Mathf.Max(0, gameOverFlashCount);
        float flashInterval = Mathf.Max(0.01f, gameOverFlashInterval);
        for (int i = 0; i < flashes; i++)
        {
            ApplyTint(1f);
            yield return new WaitForSeconds(flashInterval);
            ApplyTint(0f);
            yield return new WaitForSeconds(flashInterval);
        }

        ApplyTint(1f);

        Vector3 startScale = GetModelScale();
        Vector3 popScale = startScale * gameOverPopScale;
        Quaternion startRotation = GetModelRotation();
        Vector3 spinAxis = gameOverSpinAxis.sqrMagnitude > 0.001f ? gameOverSpinAxis.normalized : Vector3.up;
        float spinDuration = Mathf.Max(0.01f, gameOverSpinDuration);
        float spinTimer = 0f;

        float popTime = Mathf.Max(0.01f, gameOverPopDuration);
        timer = 0f;
        while (timer < popTime)
        {
            timer += Time.deltaTime;
            SetModelScale(Vector3.Lerp(startScale, popScale, Mathf.Clamp01(timer / popTime)));
            spinTimer += Time.deltaTime;
            float spinT = spinTimer / spinDuration;
            SetModelRotation(startRotation * Quaternion.AngleAxis(gameOverSpinDegrees * spinT, spinAxis));
            yield return null;
        }

        float shrinkTime = Mathf.Max(0.01f, gameOverShrinkDuration);
        timer = 0f;
        while (timer < shrinkTime)
        {
            timer += Time.deltaTime;
            SetModelScale(Vector3.Lerp(popScale, Vector3.zero, Mathf.Clamp01(timer / shrinkTime)));
            spinTimer += Time.deltaTime;
            float spinT = spinTimer / spinDuration;
            SetModelRotation(startRotation * Quaternion.AngleAxis(gameOverSpinDegrees * spinT, spinAxis));
            yield return null;
        }

        if (gameOverDestroyDelay > 0f)
        {
            yield return new WaitForSeconds(gameOverDestroyDelay);
        }

        if (restartOnAnyKey)
        {
            SetModelScale(Vector3.zero);
            yield break;
        }

        Destroy(gameObject);
    }

    private bool IsRestartPressed()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
        {
            return true;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame))
        {
            return true;
        }

        return false;
    }

    private void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
