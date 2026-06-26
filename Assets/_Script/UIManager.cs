using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Stats Text")]
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private TMP_Text gradesText;
    [SerializeField] private TMP_Text relationshipText;

    [Header("Time Text")]
    [SerializeField] private TMP_Text timeText;

    [Header("Semester Text")]
    [SerializeField] private TMP_Text semesterText;

    [Header("Dialogue Text")]
    [SerializeField] private TMP_Text dialogueText;

    private CampusLifeGameManager gameManager;

    private void Start()
    {
        gameManager = CampusLifeGameManager.Instance;

        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += RefreshAll;
            RefreshAll();
        }
    }

    private void Update()
    {
        RefreshTime();
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

        CampusLifeStats stats = gameManager.Stats;

        moneyText.text = $"돈: {stats.money}";
        conditionText.text = $"컨디션: {stats.condition}";
        gradesText.text = $"성적: {stats.grades}";
        relationshipText.text = $"친구관계: {stats.relationship}";

        semesterText.text = $"학기: {gameManager.CurrentSemester} / {gameManager.MaxSemester}";
        dialogueText.text = gameManager.Dialogue;

        RefreshTime();
    }

    private void RefreshTime()
    {
        if (gameManager == null) return;

        float remainTime = gameManager.SemesterDuration - gameManager.CurrentTime;
        remainTime = Mathf.Max(0f, remainTime);

        int minute = Mathf.FloorToInt(remainTime / 60f);
        int second = Mathf.FloorToInt(remainTime % 60f);

        timeText.text = $"시간: {minute:00}:{second:00}";
    }
}