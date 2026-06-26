using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class CourseRegistrationMinigameController : MonoBehaviour
{
    private const string SemesterActivityId = "course_registration";

    [Header("Temporary Trigger")]
    [SerializeField] private Key startKey = Key.R;
    [SerializeField] private Key closeKey = Key.Escape;

    [Header("Session Setup")]
    [SerializeField] private int roundsPerSession = 7;
    [SerializeField] private float minimumCueDelay = 0.65f;
    [SerializeField] private float maximumCueDelay = 1.8f;
    [SerializeField] private float reactionWindowSeconds = 0.9f;
    [SerializeField] private float resultPauseSeconds = 0.55f;

    [Header("Rewards")]
    [SerializeField] private int excellentGradesReward = 15;
    [SerializeField] private int strongGradesReward = 10;
    [SerializeField] private int decentGradesReward = 6;
    [SerializeField] private int scrapeGradesReward = 2;
    [SerializeField] private int excellentConditionCost = -3;
    [SerializeField] private int strongConditionCost = -4;
    [SerializeField] private int decentConditionCost = -5;
    [SerializeField] private int scrapeConditionCost = -6;

    public static CourseRegistrationMinigameController Instance { get; private set; }

    public event Action StateChanged;

    private CampusLifeGameManager manager;
    private Canvas canvas;
    private RectTransform overlayRoot;
    private RectTransform modalPanel;
    private RectTransform gridHost;
    private RectTransform progressPanel;
    private Text titleText;
    private Text statusText;
    private Text progressText;
    private Text footerText;
    private Button primaryButton;
    private Text primaryButtonLabel;
    private Button closeButton;
    private readonly Button[] cellButtons = new Button[9];
    private readonly Image[] cellImages = new Image[9];
    private Font uiFont;
    private GridLayoutGroup gridLayout;

    private Coroutine activeRoundCoroutine;
    private Coroutine activeTransitionCoroutine;
    private float storedTimeScale = 1f;
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

    public bool IsOpen => overlayRoot != null && overlayRoot.gameObject.activeSelf;

    private Color IdleCellColor => new Color(0.18f, 0.22f, 0.31f, 1f);
    private Color ActiveCellColor => new Color(0.95f, 0.79f, 0.27f, 1f);
    private Color SuccessCellColor => new Color(0.25f, 0.68f, 0.37f, 1f);
    private Color WrongCellColor => new Color(0.75f, 0.29f, 0.29f, 1f);
    private Color MissedTargetColor => new Color(0.91f, 0.52f, 0.30f, 1f);

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
        manager = CampusLifeGameManager.Instance ?? FindAnyObjectByType<CampusLifeGameManager>();
        EnsureUi();
        SetOverlayVisible(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (!IsOpen && keyboard[startKey].wasPressedThisFrame)
        {
            Open();
            return;
        }

        if (IsOpen && keyboard[closeKey].wasPressedThisFrame)
        {
            Close();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (IsOpen)
        {
            Time.timeScale = storedTimeScale;
        }
    }

    public void Open()
    {
        EnsureUi();

        if (IsOpen)
        {
            return;
        }

        manager = CampusLifeGameManager.Instance ?? FindAnyObjectByType<CampusLifeGameManager>();
        storedTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        SetOverlayVisible(true);
        PrepareSessionView();
        Canvas.ForceUpdateCanvases();
        RefreshLayout();
        RefreshGridLayout();
        NotifyStateChanged();
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        StopRunningCoroutines();
        ResetBoardVisuals();
        SetOverlayVisible(false);
        Time.timeScale = storedTimeScale;
        NotifyStateChanged();
    }

    public bool CanOpen(out string failureReason)
    {
        manager = CampusLifeGameManager.Instance ?? FindAnyObjectByType<CampusLifeGameManager>();
        if (manager == null)
        {
            failureReason = "The game manager is missing.";
            return false;
        }

        if (!manager.CanStartSemesterActivity(SemesterActivityId, out failureReason))
        {
            return false;
        }

        CampusLifeStatDelta minimumPlayableDelta = new CampusLifeStatDelta
        {
            condition = GetWorstConditionCost()
        };

        if (!manager.CanApplyActivity(minimumPlayableDelta, out string statFailureReason))
        {
            failureReason = $"Not enough resources to start. {statFailureReason}";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    private void PrepareSessionView()
    {
        StopRunningCoroutines();
        ResetSessionState();
        ResetBoardVisuals();

        titleText.text = "Course Registration";
        footerText.text = "Temporary trigger: press R in gameplay. Later this can be called by an event.";
        primaryButton.gameObject.SetActive(true);
        closeButton.gameObject.SetActive(true);
        closeButton.interactable = true;
        closeButton.GetComponentInChildren<Text>().text = "Close";

        if (!CanOpen(out string failureReason))
        {
            statusText.text = failureReason;
            progressText.text = manager != null && manager.HasFinishedRun
                ? "Restart the run to play again."
                : "This minigame can be cleared once per semester.";

            primaryButton.gameObject.SetActive(false);
            SetBoardInteractable(false);
            sessionComplete = true;
            RefreshVisibleLayout();
            return;
        }

        statusText.text = "Wait for one of the 9 squares to change color, then click it immediately.";
        progressText.text = BuildProgressText();
        primaryButton.interactable = true;
        primaryButtonLabel.text = "Start";
        SetBoardInteractable(true);
        RefreshVisibleLayout();
    }

    private void StartSession()
    {
        sessionStarted = true;
        sessionComplete = false;
        primaryButton.interactable = false;
        primaryButtonLabel.text = "Running";
        statusText.text = "Seat war begins. Stay ready.";
        progressText.text = BuildProgressText();
        RefreshVisibleLayout();
        StartNextRound();
        NotifyStateChanged();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        RefreshLayout();
        RefreshGridLayout();
    }

    private void StartNextRound()
    {
        if (activeRoundCoroutine != null)
        {
            StopCoroutine(activeRoundCoroutine);
        }

        activeRoundCoroutine = StartCoroutine(RunRound());
    }

    private IEnumerator RunRound()
    {
        ResetBoardVisuals();
        activeCellIndex = -1;
        roundResolved = false;
        waitingForCue = true;
        waitingForClick = false;

        int attemptNumber = roundsPlayed + 1;
        statusText.text = $"Attempt {attemptNumber}/{roundsPerSession}: wait for the color change.";
        progressText.text = BuildProgressText();

        float cueDelay = UnityEngine.Random.Range(minimumCueDelay, maximumCueDelay);
        yield return new WaitForSecondsRealtime(cueDelay);

        if (!IsOpen || sessionComplete)
        {
            yield break;
        }

        activeCellIndex = UnityEngine.Random.Range(0, cellImages.Length);
        cellImages[activeCellIndex].color = ActiveCellColor;
        waitingForCue = false;
        waitingForClick = true;
        cueShownAt = Time.unscaledTime;
        statusText.text = $"Attempt {attemptNumber}/{roundsPerSession}: click the highlighted square now.";

        float timeoutAt = cueShownAt + reactionWindowSeconds;
        while (!roundResolved && Time.unscaledTime < timeoutAt)
        {
            yield return null;
        }

        if (!roundResolved)
        {
            cellImages[activeCellIndex].color = MissedTargetColor;
            ResolveRound(false, $"Attempt {attemptNumber}: too slow. The class filled up.", 0f);
        }
    }

    private void ResolveRound(bool wasSuccessful, string feedback, float reactionSeconds)
    {
        if (roundResolved)
        {
            return;
        }

        roundResolved = true;
        waitingForCue = false;
        waitingForClick = false;
        roundsPlayed++;

        if (wasSuccessful)
        {
            successCount++;
            totalReactionTime += reactionSeconds;
            bestReactionTime = Mathf.Min(bestReactionTime, reactionSeconds);
        }
        else
        {
            missCount++;
        }

        statusText.text = feedback;
        progressText.text = BuildProgressText();
        RefreshVisibleLayout();

        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
        }

        activeTransitionCoroutine = StartCoroutine(AdvanceAfterRound());
        NotifyStateChanged();
    }

    private IEnumerator AdvanceAfterRound()
    {
        yield return new WaitForSecondsRealtime(resultPauseSeconds);

        if (!IsOpen)
        {
            yield break;
        }

        if (roundsPlayed >= roundsPerSession)
        {
            FinishSession();
            yield break;
        }

        StartNextRound();
    }

    private void FinishSession()
    {
        StopRunningCoroutines();
        sessionStarted = false;
        sessionComplete = true;
        waitingForCue = false;
        waitingForClick = false;

        CampusLifeStatDelta rewardDelta = BuildRewardDelta();
        string performanceTitle = GetPerformanceTitle();
        string detailSummary = BuildResultSummary(performanceTitle);
        bool applied = manager != null &&
                       manager.TryApplyActivityResult(
                           "Course Registration",
                           rewardDelta,
                           detailSummary,
                           SemesterActivityId);

        titleText.text = "Registration Result";
        statusText.text = performanceTitle;
        progressText.text = BuildFinalOverlayText(rewardDelta, applied);
        footerText.text = applied
            ? "Reward saved for this semester. Press Close to return."
            : "The result could not be saved to the semester state.";

        primaryButton.gameObject.SetActive(false);
        SetBoardInteractable(false);
        RefreshVisibleLayout();
        NotifyStateChanged();
    }

    private void OnPrimaryButtonClicked()
    {
        if (sessionComplete)
        {
            Close();
            return;
        }

        if (sessionStarted)
        {
            return;
        }

        if (!CanOpen(out string failureReason))
        {
            statusText.text = failureReason;
            primaryButton.gameObject.SetActive(false);
            SetBoardInteractable(false);
            RefreshVisibleLayout();
            NotifyStateChanged();
            return;
        }

        StartSession();
    }

    private void OnCloseButtonClicked()
    {
        Close();
    }

    private void OnCellClicked(int cellIndex)
    {
        if (!IsOpen || sessionComplete || !sessionStarted || roundResolved)
        {
            return;
        }

        int attemptNumber = roundsPlayed + 1;

        if (waitingForCue)
        {
            cellImages[cellIndex].color = WrongCellColor;
            ResolveRound(false, $"Attempt {attemptNumber}: too early. Wait for the color change.", 0f);
            return;
        }

        if (!waitingForClick)
        {
            return;
        }

        if (cellIndex != activeCellIndex)
        {
            cellImages[cellIndex].color = WrongCellColor;
            if (activeCellIndex >= 0)
            {
                cellImages[activeCellIndex].color = MissedTargetColor;
            }

            ResolveRound(false, $"Attempt {attemptNumber}: wrong square. Someone else took the seat.", 0f);
            return;
        }

        float reactionSeconds = Time.unscaledTime - cueShownAt;
        cellImages[cellIndex].color = SuccessCellColor;
        ResolveRound(
            true,
            $"Attempt {attemptNumber}: success in {reactionSeconds * 1000f:0} ms.",
            reactionSeconds);
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

    private string GetPerformanceTitle()
    {
        if (successCount >= 6)
        {
            return "Perfect timetable secured.";
        }

        if (successCount >= 4)
        {
            return "Strong registration haul.";
        }

        if (successCount >= 2)
        {
            return "Usable schedule, but not ideal.";
        }

        return "Seat war defeat. Barely salvaged the timetable.";
    }

    private string BuildProgressText()
    {
        string averageText = successCount > 0
            ? $"{(totalReactionTime / successCount) * 1000f:0} ms avg"
            : "No successful clicks yet";

        return
            $"Progress: {roundsPlayed}/{roundsPerSession}\n" +
            $"Success: {successCount}  Miss: {missCount}\n" +
            $"{averageText}";
    }

    private string BuildResultSummary(string performanceTitle)
    {
        string bestText = successCount > 0
            ? $"{bestReactionTime * 1000f:0} ms best"
            : "No seats captured";

        string averageText = successCount > 0
            ? $"{(totalReactionTime / successCount) * 1000f:0} ms avg"
            : "No successful clicks";

        return
            $"{performanceTitle}\n" +
            $"Hits {successCount}/{roundsPerSession}, Misses {missCount}\n" +
            $"{averageText}, {bestText}";
    }

    private string BuildFinalOverlayText(CampusLifeStatDelta rewardDelta, bool applied)
    {
        string bestText = successCount > 0
            ? $"{bestReactionTime * 1000f:0} ms"
            : "-";

        string averageText = successCount > 0
            ? $"{(totalReactionTime / successCount) * 1000f:0} ms"
            : "-";

        string rewardText = applied
            ? $"Grades: +{rewardDelta.grades}\nCondition: {rewardDelta.condition}"
            : $"Pending grades: +{rewardDelta.grades}\nPending condition: {rewardDelta.condition}";

        return
            $"Hits: {successCount}/{roundsPerSession}\n" +
            $"Misses: {missCount}\n" +
            $"Average: {averageText}\n" +
            $"Best: {bestText}\n" +
            $"{rewardText}";
    }

    private int GetWorstConditionCost()
    {
        return Mathf.Min(
            Mathf.Min(excellentConditionCost, strongConditionCost),
            Mathf.Min(decentConditionCost, scrapeConditionCost));
    }

    private void ResetSessionState()
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

    private void ResetBoardVisuals()
    {
        for (int i = 0; i < cellImages.Length; i++)
        {
            if (cellImages[i] == null)
            {
                continue;
            }

            cellImages[i].color = IdleCellColor;
        }
    }

    private void SetBoardInteractable(bool isInteractable)
    {
        for (int i = 0; i < cellButtons.Length; i++)
        {
            if (cellButtons[i] == null)
            {
                continue;
            }

            cellButtons[i].interactable = isInteractable;
        }
    }

    private void StopRunningCoroutines()
    {
        if (activeRoundCoroutine != null)
        {
            StopCoroutine(activeRoundCoroutine);
            activeRoundCoroutine = null;
        }

        if (activeTransitionCoroutine != null)
        {
            StopCoroutine(activeTransitionCoroutine);
            activeTransitionCoroutine = null;
        }
    }

    private void EnsureUi()
    {
        if (canvas != null)
        {
            return;
        }

        EnsureEventSystem();

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("CourseRegistrationCanvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        overlayRoot = CreatePanel(
            "Overlay",
            canvas.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            new Color(0.03f, 0.04f, 0.07f, 0.84f));

        modalPanel = CreatePanel(
            "ModalPanel",
            overlayRoot,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            new Color(0.11f, 0.15f, 0.22f, 0.98f));

        titleText = CreateText(
            "Title",
            modalPanel,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            30,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            "Course Registration");
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 16;
        titleText.resizeTextMaxSize = 30;

        statusText = CreateText(
            "Status",
            modalPanel,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            20,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            string.Empty);
        statusText.resizeTextForBestFit = true;
        statusText.resizeTextMinSize = 12;
        statusText.resizeTextMaxSize = 20;

        progressPanel = CreatePanel(
            "ProgressPanel",
            modalPanel,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            new Color(0.08f, 0.10f, 0.16f, 0.82f));

        progressText = CreateText(
            "Progress",
            progressPanel,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            20,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            string.Empty);
        progressText.lineSpacing = 0.9f;

        gridHost = CreatePanel(
            "GridHost",
            modalPanel,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            new Color(0.08f, 0.10f, 0.16f, 0.95f));

        gridLayout = gridHost.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
        gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < cellButtons.Length; i++)
        {
            int cellIndex = i;
            Button button = CreateSquareButton($"Cell_{i + 1}", gridHost, IdleCellColor);
            button.onClick.AddListener(() => OnCellClicked(cellIndex));
            cellButtons[i] = button;
            cellImages[i] = button.GetComponent<Image>();
        }

        footerText = CreateText(
            "Footer",
            modalPanel,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            15,
            FontStyle.Italic,
            TextAnchor.UpperLeft,
            string.Empty);
        footerText.resizeTextForBestFit = true;
        footerText.resizeTextMinSize = 10;
        footerText.resizeTextMaxSize = 16;
        footerText.lineSpacing = 0.92f;

        primaryButton = CreateButton(
            "PrimaryButton",
            modalPanel,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            new Color(0.20f, 0.53f, 0.34f, 1f),
            "Start");
        primaryButton.onClick.AddListener(OnPrimaryButtonClicked);
        primaryButtonLabel = primaryButton.GetComponentInChildren<Text>();
        primaryButtonLabel.resizeTextForBestFit = true;
        primaryButtonLabel.resizeTextMinSize = 12;
        primaryButtonLabel.resizeTextMaxSize = 22;

        closeButton = CreateButton(
            "CloseButton",
            modalPanel,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero,
            new Color(0.32f, 0.36f, 0.44f, 1f),
            "Close");
        closeButton.onClick.AddListener(OnCloseButtonClicked);

        Canvas.ForceUpdateCanvases();
        RefreshLayout();
        RefreshGridLayout();
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        StandaloneInputModule standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneModule != null)
        {
            standaloneModule.enabled = false;
        }

        InputSystemUIInputModule inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputSystemModule == null)
        {
            inputSystemModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        inputSystemModule.AssignDefaultActions();
    }

    private void SetOverlayVisible(bool isVisible)
    {
        if (overlayRoot == null)
        {
            return;
        }

        overlayRoot.gameObject.SetActive(isVisible);
    }

    private RectTransform CreatePanel(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Color color)
    {
        GameObject panelObject = new GameObject(objectName);
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        return rectTransform;
    }

    private Text CreateText(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor alignment,
        string initialValue)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        Text text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = initialValue;
        return text;
    }

    private Button CreateButton(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Color color,
        string label)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.2f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.18f, 0.18f, 0.18f, 0.5f);
        button.colors = colors;

        CreateText(
            "Label",
            buttonObject.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(12f, 8f),
            new Vector2(-12f, -8f),
            24,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            label);

        return button;
    }

    private Button CreateSquareButton(string objectName, Transform parent, Color color)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(150f, 150f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.05f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.08f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = color;
        button.colors = colors;

        return button;
    }

    private static void LayoutGroupPadding(GridLayoutGroup gridLayoutGroup, int padding)
    {
        gridLayoutGroup.padding = new RectOffset(padding, padding, padding, padding);
    }

    private void RefreshLayout()
    {
        if (overlayRoot == null || modalPanel == null || progressPanel == null || gridHost == null)
        {
            return;
        }

        float screenWidth = overlayRoot.rect.width;
        float screenHeight = overlayRoot.rect.height;
        if (screenWidth <= 0f || screenHeight <= 0f)
        {
            return;
        }

        float modalWidth = Mathf.Min(860f, screenWidth - 40f);
        float modalHeight = Mathf.Min(560f, screenHeight - 40f);
        modalPanel.anchorMin = modalPanel.anchorMax = new Vector2(0.5f, 0.5f);
        modalPanel.pivot = new Vector2(0.5f, 0.5f);
        modalPanel.sizeDelta = new Vector2(modalWidth, modalHeight);
        modalPanel.anchoredPosition = Vector2.zero;

        float outerPadding = Mathf.Clamp(modalWidth * 0.035f, 22f, 30f);
        float innerGap = 18f;
        float buttonWidth = 120f;
        float buttonHeight = 46f;
        float buttonGap = 16f;
        float buttonBottom = 22f;
        float minimumLeftColumnWidth = 300f;
        float boardSize = Mathf.Clamp(modalHeight * 0.48f, 220f, 280f);
        float maxBoardSizeFromWidth = modalWidth - minimumLeftColumnWidth - outerPadding * 2f - innerGap * 2f;
        boardSize = Mathf.Clamp(Mathf.Min(boardSize, maxBoardSizeFromWidth), 200f, 280f);

        float boardX = modalWidth - outerPadding - boardSize;
        float leftColumnWidth = Mathf.Max(260f, boardX - outerPadding - innerGap);
        float titleHeight = MeasurePreferredHeight(titleText, leftColumnWidth, 42f, 90f);
        float statusHeight = MeasurePreferredHeight(statusText, leftColumnWidth, 54f, 110f);
        float progressTextWidth = leftColumnWidth - 30f;
        float progressHeight = MeasurePreferredHeight(progressText, progressTextWidth, 88f, 160f) + 24f;
        float footerHeight = MeasurePreferredHeight(footerText, leftColumnWidth, 40f, 84f);
        float statusTop = outerPadding + titleHeight + 10f;
        float progressTop = statusTop + statusHeight + 14f;
        float footerTop = modalHeight - buttonBottom - buttonHeight - footerHeight - 18f;

        SetTopLeftRect(titleText.rectTransform, outerPadding, outerPadding, leftColumnWidth, titleHeight);
        SetTopLeftRect(statusText.rectTransform, outerPadding, statusTop, leftColumnWidth, statusHeight);
        SetTopLeftRect(progressPanel, outerPadding, progressTop, leftColumnWidth, progressHeight);
        SetEdgeInsets(progressText.rectTransform, 16f, 14f, 16f, 14f);
        SetTopLeftRect(footerText.rectTransform, outerPadding, footerTop, leftColumnWidth, footerHeight);

        float boardY = Mathf.Clamp((modalHeight - boardSize) * 0.5f - 18f, outerPadding + 48f, modalHeight - boardSize - 92f);
        SetTopLeftRect(gridHost, boardX, boardY, boardSize, boardSize);

        bool showPrimaryButton = primaryButton.gameObject.activeSelf;
        if (showPrimaryButton)
        {
            float buttonsTotalWidth = (buttonWidth * 2f) + buttonGap;
            float buttonsLeft = (modalWidth - buttonsTotalWidth) * 0.5f;
            SetBottomLeftRect(primaryButton.GetComponent<RectTransform>(), buttonsLeft, buttonBottom, buttonWidth, buttonHeight);
            SetBottomLeftRect(closeButton.GetComponent<RectTransform>(), buttonsLeft + buttonWidth + buttonGap, buttonBottom, buttonWidth, buttonHeight);
        }
        else
        {
            float closeLeft = (modalWidth - buttonWidth) * 0.5f;
            SetBottomLeftRect(closeButton.GetComponent<RectTransform>(), closeLeft, buttonBottom, buttonWidth, buttonHeight);
        }
    }

    private void RefreshGridLayout()
    {
        if (gridHost == null || gridLayout == null)
        {
            return;
        }

        float boardSize = Mathf.Min(gridHost.rect.width, gridHost.rect.height, 360f);
        if (boardSize <= 0f)
        {
            return;
        }

        int padding = Mathf.RoundToInt(Mathf.Clamp(boardSize * 0.05f, 14f, 22f));
        float spacing = Mathf.Clamp(boardSize * 0.03f, 8f, 14f);
        float availableSize = boardSize - (padding * 2f) - (spacing * 2f);
        float cellSize = Mathf.Floor(availableSize / 3f);

        if (cellSize < 1f)
        {
            return;
        }

        LayoutGroupPadding(gridLayout, padding);
        gridLayout.spacing = new Vector2(spacing, spacing);
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
    }

    private static void SetTopLeftRect(RectTransform rectTransform, float left, float top, float width, float height)
    {
        rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.anchoredPosition = new Vector2(left, -top);
        rectTransform.sizeDelta = new Vector2(width, height);
    }

    private static void SetBottomLeftRect(RectTransform rectTransform, float left, float bottom, float width, float height)
    {
        rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0f, 0f);
        rectTransform.pivot = new Vector2(0f, 0f);
        rectTransform.anchoredPosition = new Vector2(left, bottom);
        rectTransform.sizeDelta = new Vector2(width, height);
    }

    private static void SetEdgeInsets(RectTransform rectTransform, float left, float right, float top, float bottom)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.offsetMin = new Vector2(left, bottom);
        rectTransform.offsetMax = new Vector2(-right, -top);
    }

    private float MeasurePreferredHeight(Text text, float width, float minimumHeight, float maximumHeight)
    {
        if (text == null || width <= 0f)
        {
            return minimumHeight;
        }

        TextGenerationSettings settings = text.GetGenerationSettings(new Vector2(width, 0f));
        float preferredHeight = text.cachedTextGeneratorForLayout.GetPreferredHeight(text.text, settings) / text.pixelsPerUnit;
        return Mathf.Clamp(Mathf.Ceil(preferredHeight) + 4f, minimumHeight, maximumHeight);
    }

    private void RefreshVisibleLayout()
    {
        Canvas.ForceUpdateCanvases();
        RefreshLayout();
        RefreshGridLayout();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
