using UnityEngine;

public sealed class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private LunchRushPlayerController player;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.8f, -5.5f);
    [SerializeField] private float followSpeed = 16f;
    [SerializeField] private float lookHeight = 1.3f;
    [SerializeField] private float normalFov = 60f;
    [SerializeField] private float fastFov = 72f;
    [SerializeField] private float fovSpeed = 5f;
    [SerializeField] private float dashFov = 78f;
    [SerializeField] private float shakeFrequency = 45f;

    private Camera followCamera;
    private float shakeTimer;
    private float shakeDuration = 1f;
    private float shakeStrength;

    private void Awake()
    {
        followCamera = GetComponent<Camera>();

        if (player == null)
        {
            player = FindFirstObjectByType<LunchRushPlayerController>();
        }

        if (target == null && player != null)
        {
            target = player.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 wantedPosition = target.position + target.TransformDirection(offset);
        Vector3 basePosition = Vector3.Lerp(transform.position, wantedPosition, Time.deltaTime * followSpeed);
        transform.position = basePosition + GetShakeOffset();

        Vector3 lookPoint = target.position + Vector3.up * lookHeight;
        transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);

        if (followCamera != null && player != null)
        {
            float speed01 = Mathf.InverseLerp(5f, 13f, player.CurrentSpeed);
            float targetFov = Mathf.Lerp(normalFov, fastFov, speed01);
            if (player.IsDashViewActive)
            {
                targetFov = dashFov;
            }

            followCamera.fieldOfView = Mathf.Lerp(followCamera.fieldOfView, targetFov, Time.deltaTime * fovSpeed);
        }
    }

    public void Shake(float duration, float strength)
    {
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeTimer = shakeDuration;
        shakeStrength = strength;
        Debug.Log($"[LunchRushCameraShake] duration={duration:0.##}, strength={strength:0.##}");
    }

    private Vector3 GetShakeOffset()
    {
        if (shakeTimer <= 0f)
        {
            return Vector3.zero;
        }

        shakeTimer -= Time.deltaTime;
        float power = shakeStrength * (shakeTimer / shakeDuration);
        float x = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0.17f) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(0.41f, Time.time * shakeFrequency) - 0.5f) * 2f;
        return transform.right * x * power + transform.up * y * power;
    }
}
