using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class NovelSceneManager : MonoBehaviour
{
    [Header("--- 대사 데이터 에셋 ---")]
    public DialogueData dialogueData;

    [Header("--- UI 컴포넌트 (TMP) ---")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("--- 선택지 시스템 UI ---")]
    public GameObject choicePanel;
    public Button choiceButton1;
    public Button choiceButton2;
    public TextMeshProUGUI choiceText1;
    public TextMeshProUGUI choiceText2;

    [Header("--- 캐릭터 배치 ---")]
    public RectTransform leftCharacter;
    public RectTransform centerCharacter;
    public RectTransform rightCharacter;

    [Header("--- 선택지 분기용 대사 데이터 (기존 씬 호환용) ---")]
    public DialogueData nextDialogueA;
    public DialogueData nextDialogueB;

    [Header("--- 연출 설정 ---")]
    public float typingSpeed = 0.035f;
    public string defaultEndName = "System";
    [TextArea(2, 4)] public string defaultEndSentence = "오늘의 이야기는 여기까지다.";

    [Header("--- 한글 폰트 보정 ---")]
    public TMP_FontAsset koreanFontAsset;
    public bool applyKoreanFontOnStart = true;
    public int koreanFontSize = 32;

    [Header("--- 기능 버튼 ---")]
    public Button logButton;
    public Button autoButton;
    public Button skipButton;
    public float autoAdvanceDelay = 1.2f;

    [Header("--- 대화 로그 UI ---")]
    public GameObject logPanel;
    public TextMeshProUGUI logText;
    public ScrollRect logScrollRect;
    public int maxLogEntries = 100;

    private static readonly string[] KoreanFontNames =
    {
        "Malgun Gothic",
        "맑은 고딕",
        "Noto Sans CJK KR",
        "Noto Sans KR",
        "Apple SD Gothic Neo",
        "Arial Unicode MS"
    };

    private int currentLineIndex;
    private bool isTyping;
    private string completeSentence = string.Empty;
    private bool isChoiceTime;
    private int lastAdvanceFrame = -1;
    private int lastChoiceFrame = -1;
    private DialogueData currentChoiceA;
    private DialogueData currentChoiceB;
    private int pendingDateAffection;
    private int currentDateChoiceCount;
    private bool showingPostStoryFeedback;
    private bool isAutoMode;
    private bool isLogOpen;
    private float autoAdvanceTimer;
    private readonly List<string> dialogueLog = new List<string>();

    private void Awake()
    {
        if (applyKoreanFontOnStart)
        {
            ApplyKoreanFontToSceneTexts();
        }

        BindUtilityButtons();
        EnsureLogPanel();
    }

    private void Start()
    {
        HideChoices();
        StartDialogue(dialogueData);
    }

    private void Update()
    {
        if (isLogOpen)
        {
            return;
        }

        if (isChoiceTime)
        {
            autoAdvanceTimer = 0f;
            if (TrySelectChoiceFromPointer())
            {
                return;
            }

            return;
        }

        if (isAutoMode)
        {
            UpdateAutoAdvance();
        }

        if (WasAdvancePressed())
        {
            AdvanceDialogue();
        }
    }

    public void OnClickDialogWindow()
    {
        if (isChoiceTime || isLogOpen) return;
        AdvanceDialogue();
    }

    public void StartDialogue(DialogueData data)
    {
        DialogueData previousDialogue = dialogueData;
        bool isContinuingDate = previousDialogue != null
            && data != null
            && previousDialogue.storyKind == NovelStoryKind.Date
            && data.storyKind == NovelStoryKind.Date;

        dialogueData = data;
        currentLineIndex = 0;
        HideChoices();
        showingPostStoryFeedback = false;
        if (!isContinuingDate) pendingDateAffection = 0;
        if (!isContinuingDate) currentDateChoiceCount = 0;
        UpdateCharacterStage();

        if (!CanEnterCurrentDialogue())
        {
            return;
        }

        if (dialogueData == null || dialogueData.lines == null || dialogueData.lines.Length == 0)
        {
            ShowEndMessage();
            return;
        }

        ShowNextDialogue();
    }

    private void AdvanceDialogue()
    {
        if (lastAdvanceFrame == Time.frameCount) return;
        lastAdvanceFrame = Time.frameCount;

        if (showingPostStoryFeedback)
        {
            showingPostStoryFeedback = false;
            ShowEndMessage();
            return;
        }

        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = completeSentence;
            isTyping = false;
            return;
        }

        currentLineIndex++;
        ShowNextDialogue();
    }

    private bool WasAdvancePressed()
    {
        if (IsPointerOverUtilityUi()) return false;

#if ENABLE_INPUT_SYSTEM
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        bool touchPressed = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        bool spacePressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool enterPressed = Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame;
        return mousePressed || touchPressed || spacePressed || enterPressed;
#else
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
#endif
    }

    private bool IsPointerOverUtilityUi()
    {
        if (EventSystem.current == null) return false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return EventSystem.current.IsPointerOverGameObject();
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return EventSystem.current.IsPointerOverGameObject(Touchscreen.current.primaryTouch.touchId.ReadValue());
        }

        return false;
#else
        return Input.GetMouseButtonDown(0) && EventSystem.current.IsPointerOverGameObject();
#endif
    }

    private void ShowNextDialogue()
    {
        if (dialogueData == null || currentLineIndex >= dialogueData.lines.Length)
        {
            TriggerChoiceSituation();
            return;
        }

        DialogueLine currentLine = dialogueData.lines[currentLineIndex];
        nameText.text = currentLine.name;
        completeSentence = currentLine.sentence;
        AddDialogueLog(currentLine.name, currentLine.sentence);

        StopAllCoroutines();
        StartCoroutine(TypeSentence(completeSentence));
    }

    private void TriggerChoiceSituation()
    {
        if (TryCompleteStory())
        {
            return;
        }

        if (dialogueData != null && dialogueData.storyKind == NovelStoryKind.Date && currentDateChoiceCount >= 3)
        {
            CompleteCurrentDateAndShowFeedback();
            return;
        }

        currentChoiceA = dialogueData != null && dialogueData.nextDialogueA != null ? dialogueData.nextDialogueA : nextDialogueA;
        currentChoiceB = dialogueData != null && dialogueData.nextDialogueB != null ? dialogueData.nextDialogueB : nextDialogueB;

        if (currentChoiceA == null && currentChoiceB == null)
        {
            ShowEndMessage();
            return;
        }

        isChoiceTime = true;
        isAutoMode = false;
        autoAdvanceTimer = 0f;
        choicePanel.SetActive(true);
        ConfigureChoice(choiceButton1, choiceText1, dialogueData != null ? dialogueData.choiceTextA : string.Empty, currentChoiceA);
        ConfigureChoice(choiceButton2, choiceText2, dialogueData != null ? dialogueData.choiceTextB : string.Empty, currentChoiceB);
    }

    private void ConfigureChoice(Button button, TextMeshProUGUI label, string text, DialogueData nextDialogue)
    {
        bool isAvailable = nextDialogue != null;
        button.gameObject.SetActive(isAvailable);
        if (!isAvailable) return;

        label.text = string.IsNullOrWhiteSpace(text) ? nextDialogue.name : text;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSelectChoice(nextDialogue));
    }

    public void OnSelectChoice(DialogueData selectedDialogue)
    {
        if (lastChoiceFrame == Time.frameCount) return;
        lastChoiceFrame = Time.frameCount;

        if (selectedDialogue == null)
        {
            Debug.LogError("연결된 다음 대사 파일(DialogueData)이 없습니다!");
            return;
        }

        ApplyChoiceResult(selectedDialogue);
        ApplyDialogueReward(selectedDialogue);
        StartDialogue(selectedDialogue);
    }

    private bool TrySelectChoiceFromPointer()
    {
        if (!TryGetPointerPressPosition(out Vector2 screenPosition)) return false;

        if (IsPointerInsideButton(choiceButton1, screenPosition))
        {
            OnSelectChoice(currentChoiceA);
            return true;
        }

        if (IsPointerInsideButton(choiceButton2, screenPosition))
        {
            OnSelectChoice(currentChoiceB);
            return true;
        }

        return false;
    }

    private bool TryGetPointerPressPosition(out Vector2 screenPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPosition = Mouse.current.position.ReadValue();
            return true;
        }

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            return true;
        }

        screenPosition = Vector2.zero;
        return false;
#else
        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            return true;
        }

        screenPosition = Vector2.zero;
        return false;
#endif
    }

    private bool IsPointerInsideButton(Button button, Vector2 screenPosition)
    {
        if (button == null || !button.gameObject.activeInHierarchy) return false;

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        return rectTransform != null && RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPosition);
    }

    private void ApplyDialogueReward(DialogueData selectedDialogue)
    {
        if (GameCenter.Instance == null) return;

        GameCenter.Instance.ChangeStatus(
            selectedDialogue.moneyChange,
            selectedDialogue.conditionChange,
            selectedDialogue.gradeChange,
            selectedDialogue.friendshipChange
        );
    }

    public void OnClickLogButton()
    {
        lastAdvanceFrame = Time.frameCount;
        ToggleLogPanel();
    }

    public void OnClickAutoButton()
    {
        lastAdvanceFrame = Time.frameCount;
        if (isChoiceTime || isLogOpen) return;

        isAutoMode = !isAutoMode;
        autoAdvanceTimer = 0f;
        UpdateAutoButtonVisual();
    }

    public void OnClickSkipButton()
    {
        lastAdvanceFrame = Time.frameCount;
        if (isChoiceTime || isLogOpen || dialogueData == null) return;

        isAutoMode = false;
        autoAdvanceTimer = 0f;
        UpdateAutoButtonVisual();

        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = completeSentence;
            isTyping = false;
        }

        currentLineIndex = dialogueData.lines != null ? dialogueData.lines.Length : 0;
        ShowNextDialogue();
    }

    private void UpdateAutoAdvance()
    {
        if (isTyping || showingPostStoryFeedback)
        {
            autoAdvanceTimer = 0f;
            return;
        }

        autoAdvanceTimer += Time.deltaTime;
        if (autoAdvanceTimer < autoAdvanceDelay) return;

        autoAdvanceTimer = 0f;
        AdvanceDialogue();
    }

    private void BindUtilityButtons()
    {
        logButton = logButton != null ? logButton : FindButtonByName("LogButton");
        autoButton = autoButton != null ? autoButton : FindButtonByName("AutoButton");
        skipButton = skipButton != null ? skipButton : FindButtonByName("SkipButton");

        if (logButton != null) logButton.onClick.AddListener(OnClickLogButton);
        if (autoButton != null) autoButton.onClick.AddListener(OnClickAutoButton);
        if (skipButton != null) skipButton.onClick.AddListener(OnClickSkipButton);
    }

    private Button FindButtonByName(string objectName)
    {
        GameObject buttonObject = GameObject.Find(objectName);
        return buttonObject != null ? buttonObject.GetComponent<Button>() : null;
    }

    private void ToggleLogPanel()
    {
        EnsureLogPanel();
        isLogOpen = !isLogOpen;
        isAutoMode = false;
        autoAdvanceTimer = 0f;
        UpdateAutoButtonVisual();

        if (logPanel != null)
        {
            logPanel.SetActive(isLogOpen);
            if (isLogOpen)
            {
                RefreshLogText();
            }
        }
    }

    private void CloseLogPanel()
    {
        lastAdvanceFrame = Time.frameCount;
        isLogOpen = false;
        if (logPanel != null) logPanel.SetActive(false);
    }

    private void EnsureLogPanel()
    {
        if (logPanel != null)
        {
            logPanel.SetActive(false);
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        logPanel = new GameObject("DialogueLogPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        logPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = logPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.08f, 0.12f);
        panelRect.anchorMax = new Vector2(0.92f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImage = logPanel.GetComponent<Image>();
        panelImage.color = new Color(0.03f, 0.035f, 0.05f, 0.94f);

        GameObject titleObject = CreateLogTextObject("Title", logPanel.transform);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);
        titleRect.sizeDelta = new Vector2(-48f, 34f);
        TextMeshProUGUI titleText = titleObject.GetComponent<TextMeshProUGUI>();
        titleText.text = "대화 로그";
        titleText.fontSize = 24f;
        titleText.alignment = TextAlignmentOptions.Center;

        Button closeButton = CreateLogButton("CloseButton", "X", logPanel.transform);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-14f, -12f);
        closeRect.sizeDelta = new Vector2(36f, 30f);
        closeButton.onClick.AddListener(CloseLogPanel);

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(logPanel.transform, false);
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = new Vector2(28f, 28f);
        viewportRect.offsetMax = new Vector2(-28f, -64f);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.12f);
        viewport.GetComponent<Mask>().showMaskGraphic = true;

        GameObject content = CreateLogTextObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(-22f, 800f);
        logText = content.GetComponent<TextMeshProUGUI>();
        logText.fontSize = 22f;
        logText.alignment = TextAlignmentOptions.TopLeft;
        logText.enableWordWrapping = true;

        logScrollRect = logPanel.AddComponent<ScrollRect>();
        logScrollRect.viewport = viewportRect;
        logScrollRect.content = contentRect;
        logScrollRect.horizontal = false;
        logScrollRect.vertical = true;
        logScrollRect.movementType = ScrollRect.MovementType.Clamped;

        logPanel.SetActive(false);
        ApplyKoreanFontToRuntimeText(titleText);
        ApplyKoreanFontToRuntimeText(logText);
        ApplyKoreanFontToRuntimeText(closeButton.GetComponentInChildren<TextMeshProUGUI>());
    }

    private GameObject CreateLogTextObject(string objectName, Transform parent)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.color = Color.white;
        return textObject;
    }

    private Button CreateLogButton(string objectName, string label, Transform parent)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.16f);

        GameObject labelObject = CreateLogTextObject("Label", buttonObject.transform);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        TextMeshProUGUI labelText = labelObject.GetComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 18f;
        labelText.alignment = TextAlignmentOptions.Center;

        return buttonObject.GetComponent<Button>();
    }

    private void AddDialogueLog(string speaker, string sentence)
    {
        if (string.IsNullOrWhiteSpace(sentence)) return;

        string safeSpeaker = string.IsNullOrWhiteSpace(speaker) ? "Narrator" : speaker;
        dialogueLog.Add($"{safeSpeaker}\n{sentence}");
        while (dialogueLog.Count > maxLogEntries)
        {
            dialogueLog.RemoveAt(0);
        }

        if (isLogOpen)
        {
            RefreshLogText();
        }
    }

    private void RefreshLogText()
    {
        if (logText == null) return;

        logText.text = dialogueLog.Count > 0
            ? string.Join("\n\n", dialogueLog)
            : "아직 표시할 대화가 없습니다.";

        logText.ForceMeshUpdate();
        RectTransform textRect = logText.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, Mathf.Max(600f, logText.preferredHeight + 28f));

        if (logScrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void UpdateAutoButtonVisual()
    {
        if (autoButton == null || autoButton.targetGraphic == null) return;

        autoButton.targetGraphic.color = isAutoMode
            ? new Color(0.58f, 0.9f, 1f, 1f)
            : Color.white;
    }

    private void ApplyKoreanFontToRuntimeText(TextMeshProUGUI text)
    {
        if (text == null) return;

        TMP_FontAsset fontAsset = koreanFontAsset != null ? koreanFontAsset : CreateKoreanFontAsset();
        if (fontAsset == null) return;

        PrepareFontAsset(fontAsset);
        text.font = fontAsset;
        text.SetAllDirty();
    }

    private bool CanEnterCurrentDialogue()
    {
        if (dialogueData == null || dialogueData.storyKind != NovelStoryKind.Date) return true;

        DatingProgressManager progressManager = EnsureDatingProgressManager();
        if (progressManager.CanStartDate(out string reason)) return true;

        ShowSystemMessage("System", reason);
        return false;
    }

    private bool TryCompleteStory()
    {
        if (dialogueData == null) return false;

        if (dialogueData.storyKind == NovelStoryKind.Date && dialogueData.completesDate)
        {
            CompleteCurrentDateAndShowFeedback();
            return true;
        }

        if (dialogueData.storyKind == NovelStoryKind.Meeting && dialogueData.completesMeeting)
        {
            DatingProgressManager progressManager = EnsureDatingProgressManager();
            progressManager.RegisterMeetingAfterSelection(dialogueData.datingCharacter);
            ShowSystemMessage("Meeting", "에프터 상대를 정했다. 이제 둘만의 시간을 이어갈 수 있다.");
            showingPostStoryFeedback = true;
            return true;
        }

        return false;
    }

    private void CompleteCurrentDateAndShowFeedback()
    {
        DatingProgressManager progressManager = EnsureDatingProgressManager();
        progressManager.CompleteDate(dialogueData.datingCharacter, dialogueData.datingLocation, pendingDateAffection);
        string feedback = progressManager.GameEnded
            ? "데이트를 10번 진행했다. 이번 학기의 이야기는 여기서 끝난다."
            : progressManager.GetDateFeedback(dialogueData.datingCharacter);
        ShowSystemMessage("Date Result", feedback);
        showingPostStoryFeedback = true;
    }

    private void ApplyChoiceResult(DialogueData selectedDialogue)
    {
        if (dialogueData == null) return;

        if (dialogueData.storyKind == NovelStoryKind.Date)
        {
            currentDateChoiceCount++;
            if (selectedDialogue == currentChoiceA) pendingDateAffection += dialogueData.choiceAffectionA;
            if (selectedDialogue == currentChoiceB) pendingDateAffection += dialogueData.choiceAffectionB;
        }
        else if (dialogueData.storyKind == NovelStoryKind.Meeting)
        {
            DatingProgressManager progressManager = EnsureDatingProgressManager();
            progressManager.RegisterMeetingAfterSelection(selectedDialogue.datingCharacter);
        }
    }

    private void ShowSystemMessage(string speaker, string message)
    {
        HideChoices();
        StopAllCoroutines();
        nameText.text = speaker;
        dialogueText.text = message;
        completeSentence = message;
        AddDialogueLog(speaker, message);
        isTyping = false;
    }

    private DatingProgressManager EnsureDatingProgressManager()
    {
        DatingProgressManager progressManager = DatingProgressManager.Instance;
        if (progressManager != null) return progressManager;

        progressManager = FindFirstObjectByType<DatingProgressManager>();
        if (progressManager != null) return progressManager;

        GameObject managerObject = new GameObject("DatingProgressManager");
        return managerObject.AddComponent<DatingProgressManager>();
    }

    private void UpdateCharacterStage()
    {
        int count = dialogueData != null ? Mathf.Clamp(dialogueData.visibleCharacterCount, 0, 3) : 0;

        SetCharacterActive(leftCharacter, count >= 2);
        SetCharacterActive(centerCharacter, count == 1 || count == 3);
        SetCharacterActive(rightCharacter, count >= 2);

        if (leftCharacter != null) leftCharacter.anchoredPosition = count == 2 ? new Vector2(-220f, -20f) : new Vector2(-360f, -20f);
        if (centerCharacter != null) centerCharacter.anchoredPosition = new Vector2(0f, 10f);
        if (rightCharacter != null) rightCharacter.anchoredPosition = count == 2 ? new Vector2(220f, -20f) : new Vector2(360f, -20f);
    }

    private void SetCharacterActive(RectTransform character, bool isActive)
    {
        if (character != null) character.gameObject.SetActive(isActive);
    }

    private void HideChoices()
    {
        isChoiceTime = false;
        if (choicePanel != null) choicePanel.SetActive(false);
        currentChoiceA = null;
        currentChoiceB = null;
    }

    private void ShowEndMessage()
    {
        HideChoices();
        StopAllCoroutines();
        nameText.text = defaultEndName;
        dialogueText.text = defaultEndSentence;
        completeSentence = defaultEndSentence;
        isTyping = false;
    }

    private void ApplyKoreanFontToSceneTexts()
    {
        TMP_FontAsset fontAsset = koreanFontAsset != null ? koreanFontAsset : CreateKoreanFontAsset();
        if (fontAsset == null)
        {
            Debug.LogWarning("한글 표시용 OS 폰트를 찾지 못했습니다. TMP Font Asset에 한글 폰트를 직접 연결해 주세요.");
            return;
        }

        PrepareFontAsset(fontAsset);

        TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            text.font = fontAsset;
            text.SetAllDirty();
        }
    }

    private void PrepareFontAsset(TMP_FontAsset fontAsset)
    {
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.isMultiAtlasTexturesEnabled = true;

        string characters = CollectDialogueCharacters();
        if (!fontAsset.TryAddCharacters(characters, out string missingCharacters) || !string.IsNullOrEmpty(missingCharacters))
        {
            Debug.LogWarning($"Maplestory Light에 추가하지 못한 글자가 있습니다: {missingCharacters}");
        }
    }

    private string CollectDialogueCharacters()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(defaultEndName);
        builder.Append(defaultEndSentence);
        builder.Append("LOG AUTO SKIP 선택지 A 선택지 B 대화 로그 아직 표시할 대화가 없습니다. 클릭해서 대화를 진행하세요.");
        builder.Append("System Date Result Meeting Narrator 에프터 상대를 정했다. 이제 둘만의 시간을 이어갈 수 있다.");
        builder.Append("데이트를 10번 진행했다. 이번 학기의 이야기는 여기서 끝난다.");

        HashSet<DialogueData> visited = new HashSet<DialogueData>();
        AppendDialogueCharacters(builder, dialogueData, visited);
        AppendDialogueCharacters(builder, nextDialogueA, visited);
        AppendDialogueCharacters(builder, nextDialogueB, visited);

        return builder.ToString();
    }

    private void AppendDialogueCharacters(StringBuilder builder, DialogueData data, HashSet<DialogueData> visited)
    {
        if (data == null || visited.Contains(data)) return;
        visited.Add(data);

        builder.Append(data.name);
        builder.Append(data.choiceTextA);
        builder.Append(data.choiceTextB);

        if (data.lines != null)
        {
            foreach (DialogueLine line in data.lines)
            {
                builder.Append(line.name);
                builder.Append(line.sentence);
            }
        }

        AppendDialogueCharacters(builder, data.nextDialogueA, visited);
        AppendDialogueCharacters(builder, data.nextDialogueB, visited);
    }

    private TMP_FontAsset CreateKoreanFontAsset()
    {
        Font osFont = Font.CreateDynamicFontFromOSFont(KoreanFontNames, koreanFontSize);
        if (osFont == null) return null;

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(osFont);
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.name = osFont.name + " TMP Dynamic";
        return fontAsset;
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = string.Empty;

        foreach (char letter in sentence)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }
}
