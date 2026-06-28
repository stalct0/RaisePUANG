using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CourseRegistrationMinigameController : MonoBehaviour
{
    public static CourseRegistrationMinigameController Instance { get; private set; }
    [Header("Reward Selection")]
    [SerializeField] private SemesterBuffSelectionUI buffSelectionUI;
    
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

    [Header("Cell Sprites")]
    [Tooltip("평소 대기 상태 3x3 칸 이미지")]
    [SerializeField] private Sprite waitSprite;
    [Tooltip("클릭해야 하는 활성 칸 이미지")]
    [SerializeField] private Sprite clickSprite;
    [Tooltip("성공했을 때 이미지. 비워두면 Click Sprite를 그대로 씁니다.")]
    [SerializeField] private Sprite successSprite;
    [Tooltip("잘못 누른 칸 이미지. 비워두면 Wait Sprite를 그대로 씁니다.")]
    [SerializeField] private Sprite wrongSprite;
    [Tooltip("시간 초과/놓친 칸 이미지. 비워두면 Click Sprite를 그대로 씁니다.")]
    [SerializeField] private Sprite missedSprite;

    [Tooltip("3x3 버튼 안이나 주변에 남아 있는 기본 'Button' 텍스트를 자동으로 숨깁니다.")]
    [SerializeField] private bool hideCellButtonLabels = true;

    [Header("Result UI")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button rewardButton1;
    [SerializeField] private Button rewardButton2;
    [SerializeField] private Button rewardButton3;
    [SerializeField] private TMP_Text rewardButtonText1;
    [SerializeField] private TMP_Text rewardButtonText2;
    [SerializeField] private TMP_Text rewardButtonText3;
    [Header("Debug")]
    [SerializeField] private Key debugOpenKey = Key.R;
    [SerializeField] private Key closeKey = Key.Escape;

    [Header("Rules")]
    [SerializeField] private int roundsPerSession = 7;
    [SerializeField] private float minCueDelay = 0.45f;
    [SerializeField] private float maxCueDelay = 1.25f;
    [SerializeField] private float reactionWindow = 0.75f;
    [SerializeField] private float resultPause = 0.38f;

    [Header("Difficulty")]
    [Tooltip("켜면 라운드가 진행될수록 클릭 제한 시간이 finalReactionWindow까지 줄어듭니다.")]
    [SerializeField] private bool useProgressiveDifficulty = true;
    [Tooltip("마지막 라운드의 클릭 제한 시간입니다. reactionWindow보다 작을수록 뒤로 갈수록 어려워집니다.")]
    [SerializeField] private float finalReactionWindow = 0.38f;
    [Tooltip("켜면 실제 클릭 칸이 나오기 전에 가짜 클릭 칸이 잠깐 깜빡입니다. 누르면 실패 처리됩니다.")]
    [SerializeField] private bool enableFakeCue = true;
    [Range(0f, 1f)]
    [SerializeField] private float fakeCueChance = 0.35f;
    [SerializeField] private float fakeCueDuration = 0.16f;
    [SerializeField] private float fakeCueGapMin = 0.10f;
    [SerializeField] private float fakeCueGapMax = 0.28f;
    [Tooltip("켜면 같은 칸이 연속으로 나오는 빈도를 줄입니다.")]
    [SerializeField] private bool avoidRepeatingSameCell = true;

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
    private int lastActiveCellIndex = -1;
    private int roundsPlayed;
    private int successCount;
    private int missCount;

    private float cueShownAt;
    private float totalReactionTime;
    private float bestReactionTime = float.MaxValue;
    
    private SemesterBuff[] currentRewardChoices;
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
        if (hideCellButtonLabels)
            HideCellButtonLabels();

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
            if (cellButtons[i] == null)
                continue;

            cellImages[i] = cellButtons[i].GetComponent<Image>();

            if (cellImages[i] == null)
                Debug.LogError($"Cell Button {i}에 Image 컴포넌트가 없습니다.", cellButtons[i]);
        }
    }

    private void BindButtons()
    {
        if (startButton != null)
            startButton.onClick.AddListener(StartSession);

        HideCellButtonLabels();

        for (int i = 0; i < cellButtons.Length; i++)
        {
            int index = i;

            if (cellButtons[i] != null)
            {
                DisableLabelsUnder(cellButtons[i].transform);

                cellButtons[i].onClick.RemoveAllListeners();
                cellButtons[i].onClick.AddListener(() => OnCellClicked(index));
            }
        }
    }

    private void HideCellButtonLabels()
    {
        if (!hideCellButtonLabels || cellButtons == null)
            return;

        Transform commonParent = null;

        for (int i = 0; i < cellButtons.Length; i++)
        {
            if (cellButtons[i] == null)
                continue;

            DisableLabelsUnder(cellButtons[i].transform);

            if (commonParent == null)
                commonParent = cellButtons[i].transform.parent;
        }

        if (commonParent != null)
            DisableDefaultButtonLabelsUnder(commonParent);
    }

    private void DisableLabelsUnder(Transform root)
    {
        if (root == null)
            return;

        TMP_Text[] tmpLabels = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tmpLabels.Length; i++)
        {
            if (tmpLabels[i] != null)
                tmpLabels[i].gameObject.SetActive(false);
        }

        Text[] legacyLabels = root.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < legacyLabels.Length; i++)
        {
            if (legacyLabels[i] != null)
                legacyLabels[i].gameObject.SetActive(false);
        }
    }

    private void DisableDefaultButtonLabelsUnder(Transform root)
    {
        if (root == null)
            return;

        TMP_Text[] tmpLabels = root.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < tmpLabels.Length; i++)
        {
            if (tmpLabels[i] != null && IsDefaultButtonLabel(tmpLabels[i].text))
                tmpLabels[i].gameObject.SetActive(false);
        }

        Text[] legacyLabels = root.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < legacyLabels.Length; i++)
        {
            if (legacyLabels[i] != null && IsDefaultButtonLabel(legacyLabels[i].text))
                legacyLabels[i].gameObject.SetActive(false);
        }
    }

    private bool IsDefaultButtonLabel(string labelText)
    {
        if (string.IsNullOrWhiteSpace(labelText))
            return false;

        string trimmed = labelText.Trim();
        return trimmed == "Button" || trimmed == "CellButton";
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
        
        SetCellsInteractable(false);
        HideCellButtonLabels();
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

        SetCellsInteractable(true);
        ResetCells();
        HideCellButtonLabels();

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

        if (ShouldShowFakeCue())
        {
            int fakeCueIndex = PickCellIndex(lastActiveCellIndex);
            SetCellSprite(fakeCueIndex, clickSprite);

            if (statusText != null)
                statusText.text = $"{attempt}/{roundsPerSession}: 낚시 조심!";

            float fakeCueEndsAt = Time.unscaledTime + fakeCueDuration;
            while (!roundResolved && Time.unscaledTime < fakeCueEndsAt)
            {
                yield return null;
            }

            if (roundResolved)
                yield break;

            SetCellSprite(fakeCueIndex, waitSprite);

            float fakeCueGap = Random.Range(fakeCueGapMin, fakeCueGapMax);
            yield return new WaitForSecondsRealtime(fakeCueGap);

            if (!isOpen || sessionComplete)
                yield break;

            activeCellIndex = PickCellIndex(fakeCueIndex);
        }
        else
        {
            activeCellIndex = PickCellIndex(lastActiveCellIndex);
        }

        lastActiveCellIndex = activeCellIndex;
        SetCellSprite(activeCellIndex, clickSprite);

        waitingForCue = false;
        waitingForClick = true;
        cueShownAt = Time.unscaledTime;

        float currentReactionWindow = GetCurrentReactionWindow();

        if (statusText != null)
            statusText.text = $"{attempt}/{roundsPerSession}: 지금 클릭!";

        float timeoutAt = cueShownAt + currentReactionWindow;

        while (!roundResolved && Time.unscaledTime < timeoutAt)
        {
            yield return null;
        }

        if (!roundResolved)
        {
            if (activeCellIndex >= 0)
                SetCellSprite(activeCellIndex, GetSpriteOrFallback(missedSprite, clickSprite));

            ResolveRound(false, "늦었습니다. 자리가 찼습니다.", 0f);
        }
    }

    private bool ShouldShowFakeCue()
    {
        return enableFakeCue &&
               cellImages != null &&
               cellImages.Length > 1 &&
               Random.value < fakeCueChance;
    }

    private int PickCellIndex(int avoidIndex)
    {
        if (cellImages == null || cellImages.Length == 0)
            return -1;

        if (!avoidRepeatingSameCell || cellImages.Length == 1 || avoidIndex < 0)
            return Random.Range(0, cellImages.Length);

        int picked = Random.Range(0, cellImages.Length);
        for (int i = 0; i < 8 && picked == avoidIndex; i++)
        {
            picked = Random.Range(0, cellImages.Length);
        }

        return picked;
    }

    private float GetCurrentReactionWindow()
    {
        float maxWindow = Mathf.Max(0.05f, reactionWindow);

        if (!useProgressiveDifficulty || roundsPerSession <= 1)
            return maxWindow;

        float minWindow = Mathf.Clamp(finalReactionWindow, 0.05f, maxWindow);
        float progress = Mathf.Clamp01((float)roundsPlayed / Mathf.Max(1, roundsPerSession - 1));

        return Mathf.Lerp(maxWindow, minWindow, progress);
    }

    private void OnCellClicked(int index)
    {
        if (!isOpen || !sessionStarted || sessionComplete || roundResolved)
            return;

        int attempt = roundsPlayed + 1;

        if (waitingForCue)
        {
            SetCellSprite(index, GetSpriteOrFallback(wrongSprite, waitSprite));

            ResolveRound(false, $"{attempt}: 너무 빨리 눌렀습니다.", 0f);
            return;
        }

        if (!waitingForClick)
            return;

        if (index != activeCellIndex)
        {
            SetCellSprite(index, GetSpriteOrFallback(wrongSprite, waitSprite));

            if (activeCellIndex >= 0)
                SetCellSprite(activeCellIndex, GetSpriteOrFallback(missedSprite, clickSprite));

            ResolveRound(false, $"{attempt}: 잘못된 칸입니다.", 0f);
            return;
        }

        float reaction = Time.unscaledTime - cueShownAt;

        SetCellSprite(index, GetSpriteOrFallback(successSprite, clickSprite));

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

        BuffGrade grade = GetResultGrade();

        if (introView != null) introView.SetActive(false);
        if (gameView != null) gameView.SetActive(false);
        if (resultView != null) resultView.SetActive(true);

        if (resultText != null)
        {
            resultText.text =
                $"수강신청 결과\n\n" +
                $"성공: {successCount}/{roundsPerSession}\n" +
                $"실패: {missCount}\n\n" +
                GetPerformanceText() + "\n\n" +
                "하나를 선택하세요.";
        }

        if (buffSelectionUI != null)
            buffSelectionUI.Open(grade, this);
    }
    private CampusLifeStatDelta BuildRewardDelta()
    {
        float successRate = GetSuccessRate();

        if (successRate >= 0.8f)
        {
            return new CampusLifeStatDelta
            {
                grades = excellentGradesReward,
                condition = excellentConditionCost
            };
        }

        if (successRate >= 0.55f)
        {
            return new CampusLifeStatDelta
            {
                grades = strongGradesReward,
                condition = strongConditionCost
            };
        }

        if (successRate >= 0.3f)
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
        float successRate = GetSuccessRate();

        if (successRate >= 0.8f)
            return "올클에 가까운 시간표를 얻었습니다.";

        if (successRate >= 0.55f)
            return "괜찮은 시간표를 얻었습니다.";

        if (successRate >= 0.3f)
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
            $"{averageText}\n" +
            $"다음 제한시간: {GetCurrentReactionWindow() * 1000f:0}ms";
    }

    private void ResetSession()
    {
        sessionStarted = false;
        sessionComplete = false;
        waitingForCue = false;
        waitingForClick = false;
        roundResolved = false;

        activeCellIndex = -1;
        lastActiveCellIndex = -1;
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
            SetCellSprite(i, waitSprite);
        }
    }

    private void SetCellSprite(int index, Sprite sprite)
    {
        if (cellImages == null)
            return;

        if (index < 0 || index >= cellImages.Length)
            return;

        Image image = cellImages[index];
        if (image == null)
            return;

        if (sprite != null)
            image.sprite = sprite;

        image.color = Color.white;
        image.preserveAspect = true;
    }

    private Sprite GetSpriteOrFallback(Sprite preferred, Sprite fallback)
    {
        return preferred != null ? preferred : fallback;
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
        HideCellButtonLabels();

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
    }
    
    private float GetSuccessRate()
    {
        if (roundsPerSession <= 0)
            return 0f;

        return (float)successCount / roundsPerSession;
    }

    private BuffGrade GetResultGrade()
    {
        float successRate = GetSuccessRate();

        if (successRate >= 0.8f)
            return BuffGrade.Good;

        if (successRate >= 0.55f)
            return BuffGrade.MidBuff;

        if (successRate >= 0.3f)
            return BuffGrade.MidDebuff;

        return BuffGrade.Bad;
    }

    public void CloseAfterRewardSelected()
    {
        CloseImmediate();

        if (CampusLifeGameManager.Instance != null &&
            CampusLifeGameManager.Instance.IsMiniGame)
        {
            CampusLifeGameManager.Instance.ExitMiniGame();
        }
    }
    
}
