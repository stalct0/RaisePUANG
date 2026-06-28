using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private TMP_Text gradesText;
    [SerializeField] private TMP_Text relationshipText;
    [SerializeField] private TMP_Text semesterText;
    [SerializeField] private Image timeBarFill;

    [Header("Dialogue")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Color dialogueWarningColor = Color.red;

    [Header("Semester Result")]
    [SerializeField] private GameObject semesterResultPanel;
    [SerializeField] private TMP_Text semesterResultText;

    private CampusLifeGameManager gameManager;
    private Color defaultDialogueColor = Color.white;

    private void Start()
    {
        gameManager = CampusLifeGameManager.Instance;
        if (dialogueText != null)
            defaultDialogueColor = dialogueText.color;

        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += RefreshAll;
            RefreshAll();
        }

        if (semesterResultPanel != null)
            semesterResultPanel.SetActive(false);
    }

    private void Update()
    {
        if (gameManager == null) return;

        RefreshTimeBar();

        if (Keyboard.current == null) return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (gameManager.IsShowingSemesterResult)
            {
                gameManager.ContinueAfterSemesterResult();
            }
        }
    }

    private void OnDestroy()
    {
        if (gameManager != null)
            gameManager.OnGameStateChanged -= RefreshAll;
    }

    private void RefreshAll()
    {
        if (gameManager == null) return;

        RefreshHud();
        RefreshDialogue();
        RefreshSemesterResultPanel();
    }

    private void RefreshHud()
    {
        CampusLifeStats stats = gameManager.Stats;

        moneyText.text = $"{stats.money}";
        conditionText.text = $"{stats.condition}";
        gradesText.text = $"{stats.grades}";
        relationshipText.text = $"{stats.relationship}";
        semesterText.text = $"학기: {gameManager.GetSemesterName()}";

        RefreshTimeBar();
    }

    private void RefreshTimeBar()
    {
        if (timeBarFill == null || gameManager == null) return;

        float progress = gameManager.CurrentTime / gameManager.SemesterDuration;
        timeBarFill.fillAmount = Mathf.Clamp01(progress);
    }

    private void RefreshDialogue()
    {
        if (dialogueText != null)
        {
            dialogueText.text = gameManager.Dialogue;
            dialogueText.color = gameManager.IsDialogueWarning ? dialogueWarningColor : defaultDialogueColor;
        }

        if (dialoguePanel != null)
            dialoguePanel.SetActive(gameManager.IsPlaying);
    }

    private void RefreshSemesterResultPanel()
    {
        if (semesterResultPanel == null || semesterResultText == null)
            return;

        bool showPanel = gameManager.IsShowingSemesterResult;
        semesterResultPanel.SetActive(showPanel);

        if (!showPanel) return;

        CampusLifeStatDelta delta = gameManager.LastSemesterDelta;

        semesterResultText.text =
            $"{gameManager.GetSemesterName()} 종료\n\n" +
            $"이번 학기 변화\n" +
            $"돈: {FormatDelta(delta.money)}\n" +
            $"컨디션: {FormatDelta(delta.condition)}\n" +
            $"성적: {FormatDelta(delta.grades)}\n" +
            $"친구관계: {FormatDelta(delta.relationship)}\n\n" +
            $"{gameManager.LastSemesterSummaryText}\n\n" +
            "SPACE를 눌러 다음 학기로";
    }
    
    private string FormatDelta(int value)
    {
        if (value > 0)
            return $"+{value}";

        return value.ToString();
    }
}
