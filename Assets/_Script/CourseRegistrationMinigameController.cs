using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CourseRegistrationMinigameController : MonoBehaviour
{
    public static CourseRegistrationMinigameController Instance { get; private set; }

    [Header("Open Settings")]
    [SerializeField] private Key debugOpenKey = Key.R;
    [SerializeField] private Key closeKey = Key.Escape;

    [Header("UI")]
    [SerializeField] private GameObject dimPanel;
    [SerializeField] private GameObject miniGamePanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button startButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button[] cellButtons;

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

    private Color idleColor = new Color(0.18f, 0.22f, 0.31f, 1f);
    private Color activeColor = new Color(0.95f, 0.79f, 0.27f, 1f);
    private Color successColor = new Color(0.25f, 0.68f, 0.37f, 1f);
    private Color wrongColor = new Color(0.75f, 0.29f, 0.29f, 1f);
    private Color missedColor = new Color(0.91f, 0.52f, 0.30f, 1f);

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

        if (isOpen && keyboard[closeKey].wasPressedThisFrame)
        {
            Close();
        }
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
                cellButtons[i].onClick.AddListener(() => OnCellClicked(index));
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

        if (miniGamePanel != null)
            miniGamePanel.SetActive(true);

        PrepareSession();
    }

    public void Close()
    {
        if (!isOpen) return;

        StopCoroutines();

        isOpen = false;

        if (dimPanel != null)
            dimPanel.SetActive(false);

        if (miniGamePanel != null)
            miniGamePanel.SetActive(false);

        if (CampusLifeGameManager.Instance != null)
            CampusLifeGameManager.Instance.ExitMiniGame();
    }

    private void CloseImmediate()
    {
        isOpen = false;

        if (dimPanel != null)
            dimPanel.SetActive(false);

        if (miniGamePanel != null)
            miniGamePanel.SetActive(false);
    }

    private void PrepareSession()
    {
        ResetSession();
        ResetCells();

        if (titleText != null)
            titleText.text = "수강신청";

        if (statusText != null)
            statusText.text = "색이 바뀌는 칸을 빠르게 클릭하세요.";

        if (resultText != null)
            resultText.text = "";

        if (progressText != null)
            progressText.text = BuildProgressText();

        if (startButton != null)
        {
            startButton.gameObject.SetActive(true);
            startButton.interactable = true;
        }

        SetCellsInteractable(false);
    }

    private void StartSession()
    {
        if (sessionStarted) return;

        if (cellButtons == null || cellButtons.Length == 0)
        {
            Debug.LogError("cellButtons가 연결되지 않았습니다.");
            return;
        }

        sessionStarted = true;
        sessionComplete = false;

        if (startButton != null)
            startButton.gameObject.SetActive(false);

        SetCellsInteractable(true);

        if (statusText != null)
            statusText.text = "수강신청 전쟁 시작.";

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
            if (cellImages[activeCellIndex] != null)
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

        if (titleText != null)
            titleText.text = "수강신청 결과";

        if (statusText != null)
            statusText.text = GetPerformanceText();

        if (resultText != null)
        {
            resultText.text =
                $"성공: {successCount}/{roundsPerSession}\n" +
                $"실패: {missCount}\n" +
                $"성적: +{reward.grades}\n" +
                $"컨디션: {reward.condition}\n\n" +
                (applied ? "결과가 적용되었습니다." : "결과 적용 실패.");
        }

        if (startButton != null)
            startButton.gameObject.SetActive(false);
    }

    private CampusLifeStatDelta BuildRewardDelta()
    {
        if (successCount >= 6)
            return new CampusLifeStatDelta { grades = excellentGradesReward, condition = excellentConditionCost };

        if (successCount >= 4)
            return new CampusLifeStatDelta { grades = strongGradesReward, condition = strongConditionCost };

        if (successCount >= 2)
            return new CampusLifeStatDelta { grades = decentGradesReward, condition = decentConditionCost };

        return new CampusLifeStatDelta { grades = scrapeGradesReward, condition = scrapeConditionCost };
    }

    private string GetPerformanceText()
    {
        if (successCount >= 6)
            return "올클 성공.";

        if (successCount >= 4)
            return "괜찮은 시간표를 얻었습니다.";

        if (successCount >= 2)
            return "애매한 시간표입니다.";

        return "수강신청 망했습니다.";
    }

    private string BuildProgressText()
    {
        return
            $"진행: {roundsPlayed}/{roundsPerSession}\n" +
            $"성공: {successCount} / 실패: {missCount}";
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
}