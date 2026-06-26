using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("HUD Text")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private TMP_Text gradesText;
    [SerializeField] private TMP_Text relationshipText;
    [SerializeField] private Image timeBar;
    [SerializeField] private TMP_Text semesterText;

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
        {
            semesterResultPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (gameManager == null) return;

        RefreshTime();

        if (Keyboard.current == null)
            return;

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
        {
            gameManager.OnGameStateChanged -= RefreshAll;
        }
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

        moneyText.text = $"돈: {stats.money}";
        conditionText.text = $"컨디션: {stats.condition}";
        gradesText.text = $"성적: {stats.grades}";
        relationshipText.text = $"친구관계: {stats.relationship}";
        semesterText.text = $"학기: {GetSemesterName(gameManager.CurrentSemester)}";

        RefreshTime();
    }

    private void RefreshDialogue()
    {
        if (dialogueText != null)
        {
            dialogueText.text = gameManager.Dialogue;
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(gameManager.IsPlaying);
        }
    }

    private void RefreshSemesterResultPanel()
    {
        if (semesterResultPanel == null || semesterResultText == null)
            return;

        bool shouldShow =
            gameManager.IsShowingSemesterResult ||
            gameManager.IsFinished;

        semesterResultPanel.SetActive(shouldShow);

        if (!shouldShow)
            return;

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
            semesterResultText.text =
                $"{GetSemesterName(gameManager.CurrentSemester)} 종료\n\n" +
                $"현재 돈: {stats.money}\n" +
                $"컨디션: {stats.condition}\n" +
                $"성적: {stats.grades}\n" +
                $"친구관계: {stats.relationship}\n\n" +
                "SPACE를 눌러서 계속하기";
        }
    }

    private void RefreshTime()
    {
        if (gameManager == null || timeBar == null)
            return;

        float progress = gameManager.CurrentTime / gameManager.SemesterDuration;

        timeBar.fillAmount = Mathf.Clamp01(progress);
    }

    private string GetSemesterName(int semester)
    {
        int grade = ((semester - 1) / 2) + 1;
        int term = ((semester - 1) % 2) + 1;

        return $"{grade}-{term}";
    }
}