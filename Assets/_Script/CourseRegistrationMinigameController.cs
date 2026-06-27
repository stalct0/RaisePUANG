using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CourseRegistrationMinigameController : MonoBehaviour
{
    public static CourseRegistrationMinigameController Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject dimPanel;
    [SerializeField] private GameObject courseRegistrationPanel;

    [Header("Views")]
    [SerializeField] private GameObject introView;
    [SerializeField] private GameObject gameView;
    [SerializeField] private GameObject resultView;

    [Header("Intro UI")]
    [SerializeField] private Button startButton;

    [Header("Game UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Button[] cellButtons;

    [Header("Result UI")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button closeButton;

    [Header("Debug")]
    [SerializeField] private Key debugOpenKey = Key.R;
    [SerializeField] private Key closeKey = Key.Escape;

    [Header("Rules")]
    [SerializeField] private int roundsPerSession = 7;
    [SerializeField] private float minCueDelay = 0.65f;
    [SerializeField] private float maxCueDelay = 1.8f;
    [SerializeField] private float reactionWindow = 0.9f;
    [SerializeField] private float resultPause = 0.55f;

    [Header("Rewards")]
    [SerializeField] private int excellentGradesReward = 15;
    [SerializeField] private int strongGradesReward = 10;
    [SerializeField] private int decentGradesReward = 6;
    [SerializeField] private int scrapeGradesReward = 2;

    [SerializeField] private int excellentConditionCost = -3;
    [SerializeField] private int strongConditionCost = -4;
    [SerializeField] private int decentConditionCost = -5;
    [SerializeField] private int scrapeConditionCost = -6;

    private Image[] cellImages;

    private Coroutine roundCoroutine;
    private Coroutine transitionCoroutine;

    private bool isOpen;
    private bool sessionStarted;
    private bool sessionComplete;
    private bool waitingForCue;
    private bool waitingForClick;
    private bool roundResolved;

    private int activeCellIndex = -1;
    private int roundsPlayed;
    private int successCount;
    private int missCount;

    private float cueShownAt;
    private float totalReactionTime;
    private float bestReactionTime = float.MaxValue;

    private readonly Color idleColor = new Color(0.18f, 0.22f, 0.31f, 1f);
    private readonly Color activeColor = new Color(0.95f, 0.79f, 0.27f, 1f);
    private readonly Color successColor = new Color(0.25f, 0.68f, 0.37f, 1f);
    private readonly Color wrongColor = new Color(0.75f, 0.29f, 0.29f, 1f);
    private readonly Color missedColor = new Color(0.91f, 0.52f, 0.30f, 1f);

    public bool IsOpen => isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CacheCellImages();

        CloseImmediate();
    }

    private void Start()
    {
        BindButtons();
        CloseImmediate();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

#if UNITY_EDITOR
        if (!isOpen && keyboard[debugOpenKey].wasPressedThisFrame)
        {
            Open();
        }
#endif

        if (isOpen &&
            sessionComplete &&
            keyboard[closeKey].wasPressedThisFrame)
        {
            Close();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (startButton != null)
            startButton.onClick.RemoveListener(StartSession);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    private void CacheCellImages()
    {
        if (cellButtons == null)
        {
            cellImages = new Image[0];
            return;
        }

        cellImages = new Image[cellButtons.Length];

        for (int i = 0; i < cellButtons.Length; i++)
        {
            if (cellButtons[i] != null)
                cellImages[i] = cellButtons[i].GetComponent<Image>();
        }
    }

    private void BindButtons()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartSession);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        for (int i = 0; i < cellButtons.Length; i++)
        {
            int index = i;

            if (cellButtons[i] != null)
            {
                cellButtons[i].onClick.RemoveAllListeners();
                cellButtons[i].onClick.AddListener(() => OnCellClicked(index));
            }
        }
    }

    public void Open()
    {
        if (isOpen) return;

        if (CampusLifeGameManager.Instance == null)
        {
            Debug.LogError("CampusLifeGameManager가 없습니다.");
            return;
        }

        if (!CampusLifeGameManager.Instance.IsPlaying)
            return;

        isOpen = true;
        CampusLifeGameManager.Instance.EnterMiniGame();

        if (dimPanel != null)
            dimPanel.SetActive(true);

        if (courseRegistrationPanel != null)
            courseRegistrationPanel.SetActive(true);

        ShowIntroView();
    }

    private void ShowIntroView()
    {
        StopCoroutines();
        ResetSession();
        ResetCells();

        if (introView != null) introView.SetActive(true);
        if (gameView != null) gameView.SetActive(false);
        if (resultView != null) resultView.SetActive(false);

        if (closeButton != null)
            closeButton.gameObject.SetActive(false);
        
        SetCellsInteractable(false);
    }

    private void StartSession()
    {
        if (!isOpen) return;
        if (sessionStarted) return;

        if (cellButtons == null || cellButtons.Length == 0)
        {
            Debug.LogError("Cell Buttons가 연결되지 않았습니다.");
            return;
        }

        sessionStarted = true;
        sessionComplete = false;

        if (introView != null) introView.SetActive(false);
        if (gameView != null) gameView.SetActive(true);
        if (resultView != null) resultView.SetActive(false);

        if (closeButton != null)
            closeButton.gameObject.SetActive(false);

        SetCellsInteractable(true);
        ResetCells();

        if (statusText != null)
            statusText.text = "수강신청 전쟁 시작.";

        if (progressText != null)
            progressText.text = BuildProgressText();

        StartNextRound();
    }

    private void StartNextRound()
    {
        if (roundCoroutine != null)
            StopCoroutine(roundCoroutine);

        roundCoroutine = StartCoroutine(RunRound());
    }

    private IEnumerator RunRound()
    {
        ResetCells();

        activeCellIndex = -1;
        roundResolved = false;
        waitingForCue = true;
        waitingForClick = false;

        int attempt = roundsPlayed + 1;

        if (statusText != null)
            statusText.text = $"{attempt}/{roundsPerSession}: 대기 중...";

        if (progressText != null)
            progressText.text = BuildProgressText();

        float delay = Random.Range(minCueDelay, maxCueDelay);
        yield return new WaitForSecondsRealtime(delay);

        if (!isOpen || sessionComplete)
            yield break;

        activeCellIndex = Random.Range(0, cellImages.Length);

        if (cellImages[activeCellIndex] != null)
            cellImages[activeCellIndex].color = activeColor;

        waitingForCue = false;
        waitingForClick = true;
        cueShownAt = Time.unscaledTime;

        if (statusText != null)
            statusText.text = $"{attempt}/{roundsPerSession}: 지금 클릭!";

        float timeoutAt = cueShownAt + reactionWindow;

        while (!roundResolved && Time.unscaledTime < timeoutAt)
        {
            yield return null;
        }

        if (!roundResolved)
        {
            if (activeCellIndex >= 0 && cellImages[activeCellIndex] != null)
                cellImages[activeCellIndex].color = missedColor;

            ResolveRound(false, "늦었습니다. 자리가 찼습니다.", 0f);
        }
    }

    private void OnCellClicked(int index)
    {
        if (!isOpen || !sessionStarted || sessionComplete || roundResolved)
            return;

        int attempt = roundsPlayed + 1;

        if (waitingForCue)
        {
            if (cellImages[index] != null)
                cellImages[index].color = wrongColor;

            ResolveRound(false, $"{attempt}: 너무 빨리 눌렀습니다.", 0f);
            return;
        }

        if (!waitingForClick)
            return;

        if (index != activeCellIndex)
        {
            if (cellImages[index] != null)
                cellImages[index].color = wrongColor;

            if (activeCellIndex >= 0 && cellImages[activeCellIndex] != null)
                cellImages[activeCellIndex].color = missedColor;

            ResolveRound(false, $"{attempt}: 잘못된 칸입니다.", 0f);
            return;
        }

        float reaction = Time.unscaledTime - cueShownAt;

        if (cellImages[index] != null)
            cellImages[index].color = successColor;

        ResolveRound(true, $"{attempt}: 성공! {reaction * 1000f:0}ms", reaction);
    }

    private void ResolveRound(bool success, string message, float reactionTime)
    {
        if (roundResolved) return;

        roundResolved = true;
        waitingForCue = false;
        waitingForClick = false;

        roundsPlayed++;

        if (success)
        {
            successCount++;
            totalReactionTime += reactionTime;
            bestReactionTime = Mathf.Min(bestReactionTime, reactionTime);
        }
        else
        {
            missCount++;
        }

        if (statusText != null)
            statusText.text = message;

        if (progressText != null)
            progressText.text = BuildProgressText();

        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);

        transitionCoroutine = StartCoroutine(AdvanceAfterRound());
    }

    private IEnumerator AdvanceAfterRound()
    {
        yield return new WaitForSecondsRealtime(resultPause);

        if (!isOpen)
            yield break;

        if (roundsPlayed >= roundsPerSession)
        {
            FinishSession();
            yield break;
        }

        StartNextRound();
    }

    private void FinishSession()
    {
        StopCoroutines();

        sessionStarted = false;
        sessionComplete = true;

        SetCellsInteractable(false);

        CampusLifeStatDelta reward = BuildRewardDelta();

        bool applied = CampusLifeGameManager.Instance != null &&
                       CampusLifeGameManager.Instance.TryApplyActivity("수강신청", reward);

        if (introView != null) introView.SetActive(false);
        if (gameView != null) gameView.SetActive(false);
        if (resultView != null) resultView.SetActive(true);

        if (closeButton != null)
            closeButton.gameObject.SetActive(true);

        if (resultText != null)
        {
            resultText.text =
                $"수강신청 결과\n\n" +
                $"성공: {successCount}/{roundsPerSession}\n" +
                $"실패: {missCount}\n" +
                $"성적: +{reward.grades}\n" +
                $"컨디션: {reward.condition}\n\n" +
                GetPerformanceText() + "\n\n" +
                (applied ? "결과가 적용되었습니다." : "결과 적용 실패.") + "\n\n" +
                "닫기 버튼을 눌러 돌아가기";
        }
    }

    private CampusLifeStatDelta BuildRewardDelta()
    {
        if (successCount >= 6)
        {
            return new CampusLifeStatDelta
            {
                grades = excellentGradesReward,
                condition = excellentConditionCost
            };
        }

        if (successCount >= 4)
        {
            return new CampusLifeStatDelta
            {
                grades = strongGradesReward,
                condition = strongConditionCost
            };
        }

        if (successCount >= 2)
        {
            return new CampusLifeStatDelta
            {
                grades = decentGradesReward,
                condition = decentConditionCost
            };
        }

        return new CampusLifeStatDelta
        {
            grades = scrapeGradesReward,
            condition = scrapeConditionCost
        };
    }

    private string GetPerformanceText()
    {
        if (successCount >= 6)
            return "올클에 가까운 시간표를 얻었습니다.";

        if (successCount >= 4)
            return "괜찮은 시간표를 얻었습니다.";

        if (successCount >= 2)
            return "애매한 시간표입니다.";

        return "수강신청을 망했습니다.";
    }

    private string BuildProgressText()
    {
        string averageText = successCount > 0
            ? $"{(totalReactionTime / successCount) * 1000f:0}ms 평균"
            : "성공 기록 없음";

        return
            $"진행: {roundsPlayed}/{roundsPerSession}\n" +
            $"성공: {successCount} / 실패: {missCount}\n" +
            $"{averageText}";
    }

    private void ResetSession()
    {
        sessionStarted = false;
        sessionComplete = false;
        waitingForCue = false;
        waitingForClick = false;
        roundResolved = false;

        activeCellIndex = -1;
        roundsPlayed = 0;
        successCount = 0;
        missCount = 0;

        cueShownAt = 0f;
        totalReactionTime = 0f;
        bestReactionTime = float.MaxValue;
    }

    private void ResetCells()
    {
        if (cellImages == null) return;

        for (int i = 0; i < cellImages.Length; i++)
        {
            if (cellImages[i] != null)
                cellImages[i].color = idleColor;
        }
    }

    private void SetCellsInteractable(bool value)
    {
        if (cellButtons == null) return;

        for (int i = 0; i < cellButtons.Length; i++)
        {
            if (cellButtons[i] != null)
                cellButtons[i].interactable = value;
        }
    }

    private void StopCoroutines()
    {
        if (roundCoroutine != null)
        {
            StopCoroutine(roundCoroutine);
            roundCoroutine = null;
        }

        if (transitionCoroutine != null)
        {
            StopCoroutine(transitionCoroutine);
            transitionCoroutine = null;
        }
    }

    public void Close()
    {
        if (!isOpen) return;
        if (!sessionComplete) return;

        CloseImmediate();

        if (CampusLifeGameManager.Instance != null &&
            CampusLifeGameManager.Instance.IsMiniGame)
        {
            CampusLifeGameManager.Instance.ExitMiniGame();
        }
    }

    private void CloseImmediate()
    {
        StopCoroutines();

        isOpen = false;

        ResetSession();
        ResetCells();

        if (dimPanel != null)
            dimPanel.SetActive(false);

        if (courseRegistrationPanel != null)
            courseRegistrationPanel.SetActive(false);

        if (introView != null)
            introView.SetActive(false);

        if (gameView != null)
            gameView.SetActive(false);

        if (resultView != null)
            resultView.SetActive(false);

        if (closeButton != null)
            closeButton.gameObject.SetActive(false);
    }
}