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

    [Header("Semester Result")]
    [SerializeField] private GameObject semesterResultPanel;
    [SerializeField] private TMP_Text semesterResultText;

    private CampusLifeGameManager gameManager;

    private void Start()
    {
        gameManager = CampusLifeGameManager.Instance;

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
            else if (gameManager.IsFinished)
            {
                gameManager.RestartIfFinished();
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
            dialogueText.text = gameManager.Dialogue;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(gameManager.IsPlaying);
    }

    private void RefreshSemesterResultPanel()
    {
        if (semesterResultPanel == null || semesterResultText == null)
            return;

        bool showPanel = gameManager.IsShowingSemesterResult || gameManager.IsFinished;
        semesterResultPanel.SetActive(showPanel);

        if (!showPanel) return;

        CampusLifeStats stats = gameManager.Stats;

        if (gameManager.IsFinished)
        {
            semesterResultText.text =
                "대학생활 종료\n\n" +
                $"최종 돈: {stats.money}\n" +
                $"최종 컨디션: {stats.condition}\n" +
                $"최종 성적: {stats.grades}\n" +
                $"최종 친구관계: {stats.relationship}\n\n" +
                "SPACE를 눌러 다시 시작";
        }
        else
        {
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
    }
    private string FormatDelta(int value)
    {
        if (value > 0)
            return $"+{value}";

        return value.ToString();
    }
}