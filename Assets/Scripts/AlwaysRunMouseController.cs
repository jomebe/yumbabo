using UnityEngine;
using UnityEngine.InputSystem;

public sealed class AlwaysRunMouseController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float runSpeed = 9f;
    [SerializeField] private bool disableAnimatorRootMotion = true;

    [Header("Mouse Look")]
    [SerializeField] private float turnSensitivity = 0.12f;
    [SerializeField] private bool lockCursorOnStart = true;

    private Animator animator;
    private CharacterController characterController;

    private void Awake()
    {
        GameSettings.Apply();
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        if (animator != null && disableAnimatorRootMotion)
        {
            animator.applyRootMotion = false;
        }
    }

    private void OnEnable()
    {
        if (lockCursorOnStart)
        {
            LockCursor();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && lockCursorOnStart)
        {
            LockCursor();
        }
    }

    private void Update()
    {
        if (SettingsMenu.IsOpen)
        {
            return;
        }

        UpdateCursorLock();
        TurnWithMouse();
        MoveForward();
    }

    private void UpdateCursorLock()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            LockCursor();
        }
    }

    private void TurnWithMouse()
    {
        if (Mouse.current == null || Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        float mouseX = Mouse.current.delta.ReadValue().x;
        float sensitivity = GameSettings.SensitivityMultiplier;
        float turnAmount = mouseX * turnSensitivity * sensitivity;
        transform.Rotate(Vector3.up, turnAmount, Space.World);
    }

    private void MoveForward()
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 velocity = forward * runSpeed;

        if (characterController != null)
        {
            characterController.SimpleMove(velocity);
            return;
        }

        transform.position += velocity * Time.deltaTime;
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
