using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DatingIntroController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject dimPanel;
    [SerializeField] private GameObject datingIntroPanel;

    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("References")]
    [SerializeField] private NovelSceneManager novelSceneManager;

    private void Start()
    {
        if (yesButton != null)
            yesButton.onClick.AddListener(StartDating);

        if (noButton != null)
            noButton.onClick.AddListener(CancelDating);

        CloseImmediate();
    }

    public void OpenDatingIntro()
    {
        if (CampusLifeGameManager.Instance == null) return;
        if (!CampusLifeGameManager.Instance.IsPlaying) return;

        CampusLifeGameManager.Instance.EnterMiniGame();

        if (dimPanel != null) dimPanel.SetActive(true);
        if (datingIntroPanel != null) datingIntroPanel.SetActive(true);

        if (titleText != null)
            titleText.text = "데이트";

        if (descriptionText != null)
            descriptionText.text = "데이트를 하시겠습니까?";
    }

    private void StartDating()
    {
        if (DatingProgressManager.Instance == null) return;

        DatingCharacter character = DatingProgressManager.Instance.SelectedGirlfriend;
        int dateIndex = DatingProgressManager.Instance.CurrentDateIndex;

        DialogueData dialogue =
            DatingProgressManager.Instance.GetCurrentDialogue();        if (dialogue == null) return;

        if (datingIntroPanel != null)
            datingIntroPanel.SetActive(false);

        novelSceneManager.OpenDatingFromIntro(dialogue);
    }

    private void CancelDating()
    {
        CloseImmediate();

        if (CampusLifeGameManager.Instance != null &&
            CampusLifeGameManager.Instance.IsMiniGame)
        {
            CampusLifeGameManager.Instance.ExitMiniGame();
        }
    }

    private void CloseImmediate()
    {
        if (datingIntroPanel != null) datingIntroPanel.SetActive(false);
        if (dimPanel != null) dimPanel.SetActive(false);
    }
}