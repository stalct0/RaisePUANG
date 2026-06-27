using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EntrySceneController : MonoBehaviour
{
    private const string VolumePrefsKey = "Entry_MasterVolume";

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "gh";

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel = null;
    [SerializeField] private GameObject settingsPanel = null;
    [SerializeField] private GameObject creditsPanel = null;

    [Header("Main Buttons")]
    [SerializeField] private Button startButton = null;
    [SerializeField] private Button settingsButton = null;
    [SerializeField] private Button creditsButton = null;

    [Header("Settings")]
    [SerializeField] private Slider volumeSlider = null;
    [SerializeField] private TextMeshProUGUI volumeValueText = null;
    [SerializeField] private Button settingsExitButton = null;

    [Header("Credits")]
    [SerializeField] private TextMeshProUGUI creditsNamesText = null;
    [SerializeField] private Button creditsExitButton = null;
    [SerializeField] private string creditsNames =
        "\uAE40\uC2B9\uD658   \uAE40\uC5F0\uC6B1   \uB958\uC900\uC131   \uBC15\uD61C\uC724   \uD0DC\uAC15\uD638";

    private void Awake()
    {
        ValidateReferences();
        RegisterButtonEvents();
    }

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat(VolumePrefsKey, AudioListener.volume);
        ApplyVolume(savedVolume, false);
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        UnregisterButtonEvents();
    }

    public void StartGame()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError("[EntrySceneController] Game Scene Name is empty.");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true, nameof(mainMenuPanel));
        SetPanelActive(settingsPanel, false, nameof(settingsPanel));
        SetPanelActive(creditsPanel, false, nameof(creditsPanel));
    }

    public void ShowSettings()
    {
        SetPanelActive(mainMenuPanel, false, nameof(mainMenuPanel));
        SetPanelActive(settingsPanel, true, nameof(settingsPanel));
        SetPanelActive(creditsPanel, false, nameof(creditsPanel));
    }

    public void ShowCredits()
    {
        SetPanelActive(mainMenuPanel, false, nameof(mainMenuPanel));
        SetPanelActive(settingsPanel, false, nameof(settingsPanel));
        SetPanelActive(creditsPanel, true, nameof(creditsPanel));

        if (creditsNamesText != null)
            creditsNamesText.text = creditsNames;
    }

    public void SetVolume(float volume)
    {
        ApplyVolume(volume, true);
    }

    private void RegisterButtonEvents()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartGame);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(ShowSettings);

        if (creditsButton != null)
            creditsButton.onClick.AddListener(ShowCredits);

        if (settingsExitButton != null)
            settingsExitButton.onClick.AddListener(ShowMainMenu);

        if (creditsExitButton != null)
            creditsExitButton.onClick.AddListener(ShowMainMenu);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(SetVolume);
    }

    private void UnregisterButtonEvents()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(StartGame);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(ShowSettings);

        if (creditsButton != null)
            creditsButton.onClick.RemoveListener(ShowCredits);

        if (settingsExitButton != null)
            settingsExitButton.onClick.RemoveListener(ShowMainMenu);

        if (creditsExitButton != null)
            creditsExitButton.onClick.RemoveListener(ShowMainMenu);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
    }

    private void ApplyVolume(float volume, bool save)
    {
        float clampedVolume = Mathf.Clamp01(volume);
        AudioListener.volume = clampedVolume;

        if (volumeSlider != null)
            volumeSlider.SetValueWithoutNotify(clampedVolume);

        if (volumeValueText != null)
            volumeValueText.text = $"{Mathf.RoundToInt(clampedVolume * 100f)}%";

        if (!save)
            return;

        PlayerPrefs.SetFloat(VolumePrefsKey, clampedVolume);
        PlayerPrefs.Save();
    }

    private void SetPanelActive(GameObject panel, bool isActive, string fieldName)
    {
        if (panel == null)
        {
            Debug.LogError($"[EntrySceneController] {fieldName} is not assigned.");
            return;
        }

        panel.SetActive(isActive);
    }

    private void ValidateReferences()
    {
        LogMissing(mainMenuPanel, nameof(mainMenuPanel));
        LogMissing(settingsPanel, nameof(settingsPanel));
        LogMissing(creditsPanel, nameof(creditsPanel));
        LogMissing(startButton, nameof(startButton));
        LogMissing(settingsButton, nameof(settingsButton));
        LogMissing(creditsButton, nameof(creditsButton));
        LogMissing(volumeSlider, nameof(volumeSlider));
        LogMissing(settingsExitButton, nameof(settingsExitButton));
        LogMissing(creditsNamesText, nameof(creditsNamesText));
        LogMissing(creditsExitButton, nameof(creditsExitButton));
    }

    private void LogMissing(Object reference, string fieldName)
    {
        if (reference == null)
            Debug.LogError($"[EntrySceneController] {fieldName} is not assigned in the Inspector.");
    }
}
