using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MenuStartButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private string sceneName = "InGame";
    [SerializeField] private float clickDelay = 0.08f;

    [Header("Animation")]
    [SerializeField] private float idlePulseSpeed = 2.6f;
    [SerializeField] private float idlePulseAmount = 0.035f;
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float pressedScale = 0.94f;
    [SerializeField] private float idleTiltDegrees = 1.5f;
    [SerializeField] private float animationSpeed = 12f;

    private RectTransform rectTransform;
    private Button button;
    private Vector3 baseScale;
    private Quaternion baseRotation;
    private bool hovered;
    private bool pressed;
    private bool loading;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        baseScale = rectTransform.localScale;
        baseRotation = rectTransform.localRotation;
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(StartGame);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(StartGame);
        }

        hovered = false;
        pressed = false;
        loading = false;
        rectTransform.localScale = baseScale;
        rectTransform.localRotation = baseRotation;
    }

    private void Update()
    {
        float pulse = Mathf.Sin(Time.unscaledTime * idlePulseSpeed) * idlePulseAmount;
        float scale = pressed ? pressedScale : (hovered ? hoverScale : 1f) + pulse;
        float tilt = pressed ? 0f : Mathf.Sin(Time.unscaledTime * idlePulseSpeed * 0.7f) * idleTiltDegrees;
        float t = Time.unscaledDeltaTime * animationSpeed;

        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, baseScale * scale, t);
        rectTransform.localRotation = Quaternion.Lerp(rectTransform.localRotation, baseRotation * Quaternion.Euler(0f, 0f, tilt), t);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        hovered = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        hovered = false;
        pressed = false;
    }

    private void StartGame()
    {
        if (loading || string.IsNullOrEmpty(sceneName))
        {
            return;
        }

        loading = true;
        StartCoroutine(LoadSceneAfterClick());
    }

    private IEnumerator LoadSceneAfterClick()
    {
        yield return new WaitForSecondsRealtime(clickDelay);
        SceneManager.LoadScene(sceneName);
    }
}
