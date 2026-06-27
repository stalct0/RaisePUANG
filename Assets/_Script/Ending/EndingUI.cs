using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndingUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject endingPanel;

    [Header("Images")]
    [SerializeField] private Image endingImage;
    [SerializeField] private Image girlfriendImage;

    [Header("Text")]
    [SerializeField] private TMP_Text endingNameText;
    [SerializeField] private TMP_Text descriptionText;

    [Header("Button")]
    [SerializeField] private Button titleButton;
    [SerializeField] private string titleSceneName = "TitleScene";

    [Header("Database")]
    [SerializeField] private EndingGirlfriendDatabase girlfriendDatabase;

    private void Awake()
    {
        if (endingPanel != null)
            endingPanel.SetActive(false);

        if (titleButton != null)
            titleButton.onClick.AddListener(GoToTitleScene);
    }

    public void ShowEnding(EndingData endingData)
    {
        if (endingData == null)
            return;

        if (endingPanel != null)
            endingPanel.SetActive(true);

        if (endingImage != null)
        {
            endingImage.sprite = endingData.endingImage;
            endingImage.gameObject.SetActive(endingData.endingImage != null);
            endingImage.preserveAspect = true;
        }

        if (endingNameText != null)
            endingNameText.text = endingData.endingName;

        if (descriptionText != null)
            descriptionText.text = endingData.description;

        ShowGirlfriendOverlayIfNeeded(endingData);

        Time.timeScale = 0f;
    }

    private void ShowGirlfriendOverlayIfNeeded(EndingData endingData)
    {
        if (girlfriendImage == null)
            return;

        girlfriendImage.gameObject.SetActive(false);

        if (!endingData.showGirlfriendOverlay)
            return;

        if (endingData.endingType != EndingType.Normal)
            return;

        if (DatingProgressManager.Instance == null)
            return;

        if (girlfriendDatabase == null)
            return;

        DatingCharacter girlfriend = DatingProgressManager.Instance.SelectedGirlfriend;
        Sprite sprite = girlfriendDatabase.GetSprite(girlfriend);

        if (sprite == null)
            return;

        girlfriendImage.sprite = sprite;
        girlfriendImage.gameObject.SetActive(true);
        girlfriendImage.preserveAspect = true;
    }

    private void GoToTitleScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }
}