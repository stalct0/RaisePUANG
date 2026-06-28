using System;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
    [SerializeField] private CourseRegistrationMinigameController courseRegistrationMinigame;

    private CampusLifeStats semesterStartStats = new CampusLifeStats();
    private CampusLifeStatDelta lastSemesterDelta;
    private string lastSemesterSummaryText;
    
    private string finalEndingName;
    private string finalEndingDescription;
    private EndingType finalEndingType;

    public string FinalEndingName => finalEndingName;
    public string FinalEndingDescription => finalEndingDescription;
    public EndingType FinalEndingType => finalEndingType;
    
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
    [SerializeField] private bool isDialogueWarning;
    
    public CampusLifeStatDelta LastSemesterDelta => lastSemesterDelta;
    public string LastSemesterSummaryText => lastSemesterSummaryText;
    
    private GamePhase currentPhase = GamePhase.Playing;
    private GamePhase previousPhaseBeforeMiniGame = GamePhase.Playing;

    public event Action OnGameStateChanged;

    public int CurrentSemester => currentSemester;
    public int MaxSemester => maxSemester;
    public float CurrentTime => currentTime;
    public float SemesterDuration => semesterDuration;
    public CampusLifeStats Stats => currentStats;
    public string Dialogue => dialogue;
    public bool IsDialogueWarning => isDialogueWarning;
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
        semesterStartStats = currentStats.Clone();
        
        SetDialogue("1-1 시작. 푸앙이가 청룡탕에서 깨어났다.");

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
            SetDialogue($"{activityName} 실패: {failReason}", true);
            NotifyChanged();
            return false;
        }

        currentStats.Apply(delta);
        SetDialogue(BuildActivityDialogue(activityName, delta));
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
            SetDialogue($"{activityName} 실패: {failReason}", true);
            NotifyChanged();
            return false;
        }

        currentStats.Apply(delta);
        SetDialogue(BuildActivityDialogue(activityName, delta));
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
        HeartSpawnManager heartManager = FindFirstObjectByType<HeartSpawnManager>();
        if (heartManager != null)
            heartManager.NotifySemesterEnded();
        
        lastSemesterDelta = CalculateSemesterDelta();
        lastSemesterSummaryText = BuildSemesterSummaryText(lastSemesterDelta);
        currentTime = semesterDuration;
        currentPhase = GamePhase.SemesterResult;

        SetDialogue(
            $"{GetSemesterName(currentSemester)} 종료\n" +
            $"현재 돈: {currentStats.money}\n" +
            $"컨디션: {currentStats.condition}\n" +
            $"성적: {currentStats.grades}\n" +
            $"친구관계: {currentStats.relationship}\n\n" +
            "SPACE를 눌러 계속하기");

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
        
        if (SemesterBuffManager.Instance != null)
            SemesterBuffManager.Instance.ClearSemesterBuffs();

        semesterStartStats = currentStats.Clone();

        SetDialogue($"{GetSemesterName(currentSemester)} 시작.");

        Time.timeScale = 1f;
        NotifyChanged();

        if (courseRegistrationMinigame == null)
            courseRegistrationMinigame = FindFirstObjectByType<CourseRegistrationMinigameController>();

        if (courseRegistrationMinigame != null)
            courseRegistrationMinigame.Open();
        
        if (DatingProgressManager.Instance != null)
            DatingProgressManager.Instance.StartNextSemester();
    }

    private void FinishGame()
    {
        if (EndingManager.Instance != null)
        {
            EndingManager.Instance.TriggerNormalEnding();
            return;
        }

        currentPhase = GamePhase.Finished;
        Time.timeScale = 0f;
        NotifyChanged();
    }
    public void SetFinishedByEnding()
    {
        currentPhase = GamePhase.Finished;
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
    
    private CampusLifeStatDelta CalculateSemesterDelta()
    {
        return new CampusLifeStatDelta
        {
            money = currentStats.money - semesterStartStats.money,
            condition = currentStats.condition - semesterStartStats.condition,
            grades = currentStats.grades - semesterStartStats.grades,
            relationship = currentStats.relationship - semesterStartStats.relationship
        };
    }

    private string BuildSemesterSummaryText(CampusLifeStatDelta delta)
    {
        int bestValue = delta.money;
        string bestName = "돈";

        if (delta.condition > bestValue)
        {
            bestValue = delta.condition;
            bestName = "컨디션";
        }

        if (delta.grades > bestValue)
        {
            bestValue = delta.grades;
            bestName = "성적";
        }

        if (delta.relationship > bestValue)
        {
            bestValue = delta.relationship;
            bestName = "친구관계";
        }

        if (bestValue > 0)
            return $"이번 학기에는 {bestName}이 가장 많이 올랐다.";

        if (bestValue == 0)
            return "이번 학기는 큰 변화 없이 지나갔다.";

        return "이번 학기는 전반적으로 손해가 컸다.";
    }
    
    public void TriggerEnding(EndingType type, string endingName, string description)
    {
        currentPhase = GamePhase.Finished;

        finalEndingType = type;
        finalEndingName = endingName;
        finalEndingDescription = description;

        SetDialogue(
            $"{endingName}\n\n" +
            $"{description}\n\n" +
            "SPACE를 눌러 다시 시작");

        Time.timeScale = 0f;
        NotifyChanged();
    }

    private void SetDialogue(string message, bool warning = false)
    {
        dialogue = message;
        isDialogueWarning = warning;
    }
    private void DebugSetZoneLevel(ZoneType zoneType, int level)
    {
        ZoneSpriteSwitcher[] switchers = FindObjectsByType<ZoneSpriteSwitcher>(
            FindObjectsSortMode.None);

        foreach (ZoneSpriteSwitcher switcher in switchers)
        {
            if (switcher.ZoneType == zoneType)
            {
                switcher.DebugSetLevel(level);
                return;
            }
        }
    }
    
    [ContextMenu("DEBUG -> Jump To 4-2")]
    public void DebugJumpTo42()
    {
        currentSemester = 8;
        currentTime = 0f;
        currentPhase = GamePhase.Playing;

// 원하는 테스트용 스탯
        currentStats.money = 200;
        currentStats.condition = 80;
        currentStats.grades = 120;
        currentStats.relationship = 60;

// 구역 레벨도 테스트용으로
        DebugSetZoneLevel(ZoneType.Classroom, 3);
        DebugSetZoneLevel(ZoneType.Drink, 2);
        DebugSetZoneLevel(ZoneType.TeamProjectRoom, 1);
        DebugSetZoneLevel(ZoneType.Work, 3);

        semesterStartStats = currentStats.Clone();

        NotifyChanged();
    }
}
