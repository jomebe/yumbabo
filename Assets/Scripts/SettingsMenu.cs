using UnityEngine;
using UnityEngine.UI;

public sealed class SettingsMenu : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Text volumeValueText;
    [SerializeField] private Text sensitivityValueText;
    [SerializeField] private float minSensitivity = 0.5f;
    [SerializeField] private float maxSensitivity = 2f;

    private void Awake()
    {
        GameSettings.Apply();
        ConfigureSliders();
        SyncUI();
    }

    private void OnEnable()
    {
        BindEvents();
    }

    private void OnDisable()
    {
        UnbindEvents();
    }

    public void Open()
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }

        SyncUI();
    }

    public void Close()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    public void Toggle()
    {
        if (panel == null)
        {
            return;
        }

        bool nextState = !panel.activeSelf;
        panel.SetActive(nextState);
        if (nextState)
        {
            SyncUI();
        }
    }

    private void ConfigureSliders()
    {
        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = minSensitivity;
            sensitivitySlider.maxValue = maxSensitivity;
        }
    }

    private void BindEvents()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(HandleVolumeChanged);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.AddListener(HandleSensitivityChanged);
        }
    }

    private void UnbindEvents()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(HandleVolumeChanged);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.RemoveListener(HandleSensitivityChanged);
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
}
