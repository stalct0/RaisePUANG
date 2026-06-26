using System;
using UnityEngine;

public class CampusLifeGameManager : MonoBehaviour
{
    public static CampusLifeGameManager Instance { get; private set; }

    [Header("Semester")]
    [SerializeField] private int maxSemester = 8;
    [SerializeField] private int currentSemester = 1;

    [Header("Time")]
    [SerializeField] private float semesterDuration = 180f; // 3분
    [SerializeField] private float currentTime = 0f;

    [Header("Stats")]
    [SerializeField] private CampusLifeStats startingStats = new CampusLifeStats();
    [SerializeField] private CampusLifeStats currentStats = new CampusLifeStats();

    [Header("State")]
    [SerializeField] private bool isGameFinished = false;

    [Header("Dialogue")]
    [SerializeField] private string dialogue = "푸앙이가 대학생활을 시작했다.";

    public event Action OnGameStateChanged;

    public int CurrentSemester => currentSemester;
    public int MaxSemester => maxSemester;
    public float CurrentTime => currentTime;
    public float SemesterDuration => semesterDuration;
    public bool IsGameFinished => isGameFinished;
    public CampusLifeStats Stats => currentStats;
    public string Dialogue => dialogue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 자동 생성 / 씬 유지 방식 안 씀
        // DontDestroyOnLoad(gameObject); 일부러 제거
    }

    private void Start()
    {
        StartNewGame();
    }

    private void Update()
    {
        if (isGameFinished) return;

        currentTime += Time.deltaTime;

        if (currentTime >= semesterDuration)
        {
            EndSemester();
        }
    }

    public void StartNewGame()
    {
        currentSemester = 1;
        currentTime = 0f;
        isGameFinished = false;

        currentStats = startingStats.Clone();
        currentStats.Clamp();

        dialogue = "1학기 시작. 푸앙이가 청룡탕에서 깨어났다.";

        NotifyChanged();
    }

    public bool TryApplyActivity(string activityName, CampusLifeStatDelta delta)
    {
        if (isGameFinished)
        {
            dialogue = "이미 모든 학기가 끝났다.";
            NotifyChanged();
            return false;
        }

        if (!CanApplyDelta(delta, out string failReason))
        {
            dialogue = $"{activityName} 실패: {failReason}";
            NotifyChanged();
            return false;
        }

        currentStats.Apply(delta);
        dialogue = BuildActivityDialogue(activityName, delta);

        NotifyChanged();
        return true;
    }

    private bool CanApplyDelta(CampusLifeStatDelta delta, out string failReason)
    {
        if (currentStats.money + delta.money < 0)
        {
            failReason = "돈이 부족하다.";
            return false;
        }

        if (currentStats.condition + delta.condition < 0)
        {
            failReason = "컨디션이 부족하다.";
            return false;
        }

        if (currentStats.grades + delta.grades < 0)
        {
            failReason = "성적이 너무 낮다.";
            return false;
        }

        if (currentStats.relationship + delta.relationship < 0)
        {
            failReason = "친구관계가 부족하다.";
            return false;
        }

        failReason = "";
        return true;
    }

    public void EndSemester()
    {
        if (isGameFinished) return;

        dialogue = $"{currentSemester}학기가 종료되었다.";

        if (currentSemester >= maxSemester)
        {
            FinishGame();
            return;
        }

        currentSemester++;
        currentTime = 0f;

        dialogue += $"\n{currentSemester}학기가 시작되었다.";

        NotifyChanged();
    }

    private void FinishGame()
    {
        isGameFinished = true;
        currentTime = semesterDuration;

        dialogue += "\n8학기 종료. 대학생활이 끝났다.";
        dialogue += $"\n최종 결과: {GetEndingName()}";

        NotifyChanged();
    }

    private string GetEndingName()
    {
        if (currentStats.grades >= 80 && currentStats.condition >= 40)
            return "하닉 취업 엔딩";

        if (currentStats.grades >= 90)
            return "대학원생 엔딩";

        if (currentStats.relationship >= 80)
            return "인싸 졸업 엔딩";

        if (currentStats.money >= 100)
            return "알바왕 엔딩";

        return "무난한 졸업 엔딩";
    }

    private string BuildActivityDialogue(string activityName, CampusLifeStatDelta delta)
    {
        string result = $"{activityName} 완료.";

        if (delta.money != 0)
            result += $"\n돈 {(delta.money > 0 ? "+" : "")}{delta.money}";

        if (delta.condition != 0)
            result += $"\n컨디션 {(delta.condition > 0 ? "+" : "")}{delta.condition}";

        if (delta.grades != 0)
            result += $"\n성적 {(delta.grades > 0 ? "+" : "")}{delta.grades}";

        if (delta.relationship != 0)
            result += $"\n친구관계 {(delta.relationship > 0 ? "+" : "")}{delta.relationship}";

        return result;
    }

    private void NotifyChanged()
    {
        OnGameStateChanged?.Invoke();
    }
}