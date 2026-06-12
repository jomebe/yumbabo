using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsMenu : MonoBehaviour
{
    private static SettingsMenu instance;
    public static bool IsOpen { get; private set; }

    private GameObject rootObject;
    private GameObject blockerObject;
    private GameObject panelObject;
    private Slider volumeSlider;
    private Slider sensitivitySlider;
    private Text volumeValueText;
    private Text sensitivityValueText;
    private Text titleText;

    private float minSensitivity = 0.5f;
    private float maxSensitivity = 2f;

    public static SettingsMenu EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject existing = GameObject.Find("RuntimeSettingsMenu");
        if (existing != null)
        {
            instance = existing.GetComponent<SettingsMenu>();
            if (instance != null)
            {
                instance.EnsureBuilt();
                return instance;
            }
        }

        GameObject root = new GameObject("RuntimeSettingsMenu");
        instance = root.AddComponent<SettingsMenu>();
        instance.EnsureBuilt();
        return instance;
    }

    public static void ToggleMenu()
    {
        EnsureInstance().Toggle();
    }

    public static void OpenMenu()
    {
        EnsureInstance().Open();
    }

    public static void CloseMenu()
    {
        if (instance != null)
        {
            instance.Close();
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        GameSettings.Apply();
        EnsureBuilt();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void EnsureBuilt()
    {
        if (rootObject != null)
        {
            return;
        }

        EnsureEventSystem();

        rootObject = new GameObject("SettingsCanvasRoot");
        rootObject.transform.SetParent(transform, false);

        Canvas canvas = rootObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        rootObject.AddComponent<CanvasScaler>();
        rootObject.AddComponent<GraphicRaycaster>();

        blockerObject = BuildBlocker(rootObject.transform);
        panelObject = BuildPanel(rootObject.transform);
        panelObject.SetActive(false);
        blockerObject.SetActive(false);

        SyncUI();
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    private GameObject BuildPanel(Transform parent)
    {
        GameObject panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(parent, false);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.82f);
        panelImage.raycastTarget = true;

        Button panelButton = panel.AddComponent<Button>();
        panelButton.transition = Selectable.Transition.None;
        panelButton.targetGraphic = panelImage;
        panelButton.onClick.AddListener(CloseMenu);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(640f, 460f);
        panelRect.anchoredPosition = Vector2.zero;

        titleText = CreateText(panel.transform, "설정", 44, TextAnchor.MiddleCenter, new Vector2(0f, 175f), new Vector2(520f, 60f));

        CreateLabel(panel.transform, "게임 소리", new Vector2(0f, 95f));
        volumeSlider = CreateSlider(panel.transform, new Vector2(0f, 55f));
        volumeValueText = CreateValueText(panel.transform, new Vector2(240f, 55f));

        CreateLabel(panel.transform, "감도", new Vector2(0f, -10f));
        sensitivitySlider = CreateSlider(panel.transform, new Vector2(0f, -50f));
        sensitivityValueText = CreateValueText(panel.transform, new Vector2(240f, -50f));

        Button closeButton = CreateButton(panel.transform, "닫기", new Vector2(0f, -165f), new Vector2(220f, 56f));
        closeButton.onClick.AddListener(Close);

        Button closeXButton = CreateButton(panel.transform, "X", new Vector2(285f, 195f), new Vector2(52f, 52f));
        closeXButton.onClick.AddListener(CloseMenu);
        closeXButton.transform.SetAsLastSibling();

        ConfigureSliders();
        return panel;
    }

    private GameObject BuildBlocker(Transform parent)
    {
        GameObject blocker = new GameObject("SettingsBlocker");
        blocker.transform.SetParent(parent, false);

        Image blockerImage = blocker.AddComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0f);
        blockerImage.raycastTarget = true;

        RectTransform blockerRect = blocker.GetComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;

        return blocker;
    }

    private void ConfigureSliders()
    {
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = minSensitivity;
            sensitivitySlider.maxValue = maxSensitivity;
            sensitivitySlider.onValueChanged.AddListener(HandleSensitivityChanged);
        }
    }

    public void Open()
    {
        EnsureBuilt();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (panelObject != null)
        {
            panelObject.SetActive(true);
        }

        if (blockerObject != null)
        {
            blockerObject.SetActive(true);
        }

        IsOpen = true;

        SyncUI();
    }

    public void Close()
    {
        if (panelObject != null)
        {
            panelObject.SetActive(false);
        }

        if (blockerObject != null)
        {
            blockerObject.SetActive(false);
        }

        IsOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Toggle()
    {
        EnsureBuilt();
        if (panelObject == null)
        {
            return;
        }

        bool nextState = !panelObject.activeSelf;
        panelObject.SetActive(nextState);
        if (blockerObject != null)
        {
            blockerObject.SetActive(nextState);
        }

        IsOpen = nextState;
        if (nextState)
        {
            SyncUI();
        }
    }

    private void SyncUI()
    {
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(GameSettings.Volume);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.SetValueWithoutNotify(GameSettings.SensitivityMultiplier);
        }

        UpdateValueLabels();
    }

    private void HandleVolumeChanged(float value)
    {
        GameSettings.SetVolume(value);
        UpdateValueLabels();
    }

    private void HandleSensitivityChanged(float value)
    {
        GameSettings.SetSensitivity(value);
        UpdateValueLabels();
    }

    private void LateUpdate()
    {
        if (blockerObject != null)
        {
            blockerObject.transform.SetAsLastSibling();
        }

        if (panelObject != null)
        {
            panelObject.transform.SetAsLastSibling();
            Transform xButton = panelObject.transform.Find("X");
            if (xButton != null)
            {
                xButton.SetAsLastSibling();
            }
        }
    }

    private void UpdateValueLabels()
    {
        if (volumeValueText != null)
        {
            volumeValueText.text = Mathf.RoundToInt(GameSettings.Volume * 100f) + "%";
        }

        if (sensitivityValueText != null)
        {
            sensitivityValueText.text = GameSettings.SensitivityMultiplier.ToString("0.00");
        }
    }

    private static Text CreateText(Transform parent, string text, int fontSize, TextAnchor alignment, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject textObject = new GameObject(text);
        textObject.transform.SetParent(parent, false);
        Text uiText = textObject.AddComponent<Text>();
        uiText.font = GetBuiltinFont();
        uiText.fontSize = fontSize;
        uiText.fontStyle = FontStyle.Bold;
        uiText.color = Color.white;
        uiText.alignment = alignment;
        uiText.text = text;
        uiText.raycastTarget = false;

        RectTransform rect = uiText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return uiText;
    }

    private void CreateLabel(Transform parent, string text, Vector2 anchoredPosition)
    {
        CreateText(parent, text, 28, TextAnchor.MiddleLeft, anchoredPosition, new Vector2(320f, 42f));
    }

    private Text CreateValueText(Transform parent, Vector2 anchoredPosition)
    {
        return CreateText(parent, "100%", 26, TextAnchor.MiddleRight, anchoredPosition, new Vector2(120f, 42f));
    }

    private Slider CreateSlider(Transform parent, Vector2 anchoredPosition)
    {
        GameObject sliderObject = new GameObject("Slider");
        sliderObject.transform.SetParent(parent, false);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.direction = Slider.Direction.LeftToRight;

        RectTransform rootRect = slider.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = anchoredPosition;
        rootRect.sizeDelta = new Vector2(420f, 34f);

        Image background = CreateSliderImage(sliderObject.transform, "Background", new Color(0.2f, 0.2f, 0.2f, 1f), Vector2.zero, new Vector2(420f, 18f));
        Image fillArea = CreateSliderImage(sliderObject.transform, "FillArea", new Color(0f, 0f, 0f, 0f), Vector2.zero, Vector2.zero);
        Image fill = CreateSliderImage(fillArea.transform, "Fill", new Color(0.95f, 0.82f, 0.15f, 1f), Vector2.zero, new Vector2(420f, 18f));
        Image handle = CreateSliderImage(sliderObject.transform, "Handle", Color.white, Vector2.zero, new Vector2(28f, 28f));

        RectTransform fillAreaRect = fillArea.rectTransform;
        fillAreaRect.anchorMin = new Vector2(0f, 0.5f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.5f);
        fillAreaRect.offsetMin = new Vector2(12f, -9f);
        fillAreaRect.offsetMax = new Vector2(-12f, 9f);

        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        RectTransform handleRect = handle.rectTransform;
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(28f, 28f);

        slider.targetGraphic = background;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.transition = Selectable.Transition.ColorTint;
        slider.colors = ColorBlock.defaultColorBlock;

        return slider;
    }

    private Image CreateSliderImage(Transform parent, string name, Color color, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;

        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        return image;
    }

    private Button CreateButton(Transform parent, string label, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject buttonObject = new GameObject(label);
        buttonObject.transform.SetParent(parent, false);

        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.95f, 0.45f, 0.12f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.6f, 0.2f, 1f);
        colors.pressedColor = new Color(0.75f, 0.25f, 0.05f, 1f);
        button.colors = colors;

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Text buttonText = CreateText(buttonObject.transform, label, 28, TextAnchor.MiddleCenter, Vector2.zero, sizeDelta);
        buttonText.color = Color.white;
        return button;
    }

    private static Font GetBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
    }
}
