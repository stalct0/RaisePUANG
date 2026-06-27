using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GamePhase
{
    Playing,
    MiniGame,
    SemesterResult,
    Finished
}

public class CampusLifeGameManager : MonoBehaviour
{
    public static CampusLifeGameManager Instance { get; private set; }

    [Header("Semester")]
    [SerializeField] private int maxSemester = 8;
    [SerializeField] private int currentSemester = 1;

    [Header("Time")]
    [SerializeField] private float semesterDuration = 180f;
    [SerializeField] private float currentTime = 0f;

    [Header("Stats")]
    [SerializeField] private CampusLifeStats startingStats = new CampusLifeStats();
    [SerializeField] private CampusLifeStats currentStats = new CampusLifeStats();

    [Header("Dialogue")]
    [SerializeField] private string dialogue = "푸앙이가 대학생활을 시작했다.";

    private GamePhase currentPhase = GamePhase.Playing;
    private GamePhase previousPhaseBeforeMiniGame = GamePhase.Playing;

    public event Action OnGameStateChanged;

    public int CurrentSemester => currentSemester;
    public int MaxSemester => maxSemester;
    public float CurrentTime => currentTime;
    public float SemesterDuration => semesterDuration;
    public CampusLifeStats Stats => currentStats;
    public string Dialogue => dialogue;
    public GamePhase CurrentPhase => currentPhase;

    public bool IsPlaying => currentPhase == GamePhase.Playing;
    public bool IsMiniGame => currentPhase == GamePhase.MiniGame;
    public bool IsShowingSemesterResult => currentPhase == GamePhase.SemesterResult;
    public bool IsFinished => currentPhase == GamePhase.Finished;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        StartNewGame();
    }

    private void Update()
    {
        HandleDebugInput();

        if (currentPhase != GamePhase.Playing)
            return;

        currentTime += Time.deltaTime;

        if (currentTime >= semesterDuration)
        {
            ShowSemesterResult();
        }
    }

    private void HandleDebugInput()
    {
#if UNITY_EDITOR
        if (Keyboard.current == null) return;

        if (Keyboard.current.f1Key.wasPressedThisFrame && currentPhase == GamePhase.Playing)
        {
            ShowSemesterResult();
        }
#endif
    }

    public void StartNewGame()
    {
        currentSemester = 1;
        currentTime = 0f;
        currentPhase = GamePhase.Playing;

        currentStats = startingStats.Clone();
        currentStats.Clamp();

        dialogue = "1-1 시작. 푸앙이가 청룡탕에서 깨어났다.";

        Time.timeScale = 1f;
        NotifyChanged();
    }

    public bool TryApplyActivity(string activityName, CampusLifeStatDelta delta)
    {
        if (currentPhase != GamePhase.Playing && currentPhase != GamePhase.MiniGame)
            return false;

        if (!CanApplyDelta(delta, out string failReason))
        {
            Debug.Log(
                $"[CampusLifeGameManager] Activity '{activityName}' failed before applying stats: {failReason}. " +
                $"Delta was {FormatDeltaForLog(delta)}.",
                this);
            dialogue = $"{activityName} 실패: {failReason}";
            NotifyChanged();
            return false;
        }

        currentStats.Apply(delta);
        dialogue = BuildActivityDialogue(activityName, delta);
        Debug.Log($"[CampusLifeGameManager] Activity '{activityName}' applied: {FormatDeltaForLog(delta)}.", this);
        NotifyChanged();

        return true;
    }

    public bool TryApplyContinuousActivity(string activityName, CampusLifeStatDelta delta)
    {
        if (currentPhase != GamePhase.Playing && currentPhase != GamePhase.MiniGame)
        {
            Debug.Log($"[CampusLifeGameManager] Continuous activity '{activityName}' ignored because phase is {currentPhase}.", this);
            return false;
        }

        if (delta.IsZero)
        {
            Debug.Log($"[CampusLifeGameManager] Continuous activity '{activityName}' ignored because delta is zero.", this);
            return false;
        }

        if (!CanApplyDelta(delta, out string failReason))
        {
            Debug.Log(
                $"[CampusLifeGameManager] Continuous activity '{activityName}' failed before applying stats: {failReason}. " +
                $"Delta was {FormatDeltaForLog(delta)}.",
                this);
            dialogue = $"{activityName} 실패: {failReason}";
            NotifyChanged();
            return false;
        }

        currentStats.Apply(delta);
        dialogue = BuildActivityDialogue(activityName, delta);
        Debug.Log($"[CampusLifeGameManager] Continuous activity '{activityName}' applied: {FormatDeltaForLog(delta)}.", this);
        NotifyChanged();

        return true;
    }

    public bool CanApplyDelta(CampusLifeStatDelta delta, out string failReason)
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

    public void EnterMiniGame()
    {
        if (currentPhase != GamePhase.Playing)
            return;

        previousPhaseBeforeMiniGame = currentPhase;
        currentPhase = GamePhase.MiniGame;
        Time.timeScale = 0f;
        NotifyChanged();
    }

    public void ExitMiniGame()
    {
        if (currentPhase != GamePhase.MiniGame)
            return;

        currentPhase = previousPhaseBeforeMiniGame;
        Time.timeScale = 1f;
        NotifyChanged();
    }

    private void ShowSemesterResult()
    {
        currentTime = semesterDuration;
        currentPhase = GamePhase.SemesterResult;

        dialogue =
            $"{GetSemesterName(currentSemester)} 종료\n" +
            $"현재 돈: {currentStats.money}\n" +
            $"컨디션: {currentStats.condition}\n" +
            $"성적: {currentStats.grades}\n" +
            $"친구관계: {currentStats.relationship}\n\n" +
            "SPACE를 눌러 계속하기";

        Time.timeScale = 0f;
        NotifyChanged();
    }

    public void ContinueAfterSemesterResult()
    {
        if (currentPhase != GamePhase.SemesterResult)
            return;

        if (currentSemester >= maxSemester)
        {
            FinishGame();
            return;
        }

        currentSemester++;
        currentTime = 0f;
        currentPhase = GamePhase.Playing;

        dialogue = $"{GetSemesterName(currentSemester)} 시작.";

        Time.timeScale = 1f;
        NotifyChanged();
    }

    private void FinishGame()
    {
        currentPhase = GamePhase.Finished;

        dialogue =
            "8학기 종료. 대학생활이 끝났다.\n" +
            $"최종 결과: {GetEndingName()}\n\n" +
            "SPACE를 눌러 다시 시작";

        Time.timeScale = 0f;
        NotifyChanged();
    }

    public void RestartIfFinished()
    {
        if (currentPhase != GamePhase.Finished)
            return;

        StartNewGame();
    }

    public string GetSemesterName()
    {
        return GetSemesterName(currentSemester);
    }

    private string GetSemesterName(int semester)
    {
        int grade = ((semester - 1) / 2) + 1;
        int term = ((semester - 1) % 2) + 1;
        return $"{grade}-{term}";
    }

    private string GetEndingName()
    {
        if (currentStats.grades >= 90)
            return "대학원생 엔딩";

        if (currentStats.grades >= 80 && currentStats.condition >= 40)
            return "하닉 취업 엔딩";

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

    private string FormatDeltaForLog(CampusLifeStatDelta delta)
    {
        return $"money={delta.money}, condition={delta.condition}, grades={delta.grades}, relationship={delta.relationship}";
    }

    private void NotifyChanged()
    {
        OnGameStateChanged?.Invoke();
    }
}
