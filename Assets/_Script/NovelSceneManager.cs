using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class NovelSceneManager : MonoBehaviour
{
    [Header("Dialogue Data")]
    [SerializeField] private DialogueData startDialogueData;

    [Header("Panel")]
    [SerializeField] private GameObject datingPanel;
    [SerializeField] private GameObject dimPanel;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Choices")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button choiceButton1;
    [SerializeField] private Button choiceButton2;
    [SerializeField] private TextMeshProUGUI choiceText1;
    [SerializeField] private TextMeshProUGUI choiceText2;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    [Header("Typing")]
    [SerializeField] private float typingSpeed = 0.035f;

    [Header("End Message")]
    [SerializeField] private string defaultEndName = "System";
    [TextArea(2, 4)]
    [SerializeField] private string defaultEndSentence = "오늘의 이야기는 여기까지다.";

    private DialogueData currentDialogueData;
    private int currentLineIndex;
    private bool isTyping;
    private bool isChoiceTime;
    private bool isEnd;
    private string completeSentence = "";

    private Coroutine typingCoroutine;

    private void Start()
    {
        if (datingPanel != null)
            datingPanel.SetActive(false);

        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseDating);
    }

    private void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseDating);
    }

    public void OpenDating()
    {
        OpenDating(startDialogueData);
    }

    public void OpenDating(DialogueData dialogueData)
    {
        if (dialogueData == null)
        {
            Debug.LogError("DialogueData가 없습니다.");
            return;
        }

        if (CampusLifeGameManager.Instance == null)
        {
            Debug.LogError("CampusLifeGameManager가 없습니다.");
            return;
        }

        if (!CampusLifeGameManager.Instance.IsPlaying)
            return;

        CampusLifeGameManager.Instance.EnterMiniGame();

        if (dimPanel != null)
            dimPanel.SetActive(true);

        if (datingPanel != null)
            datingPanel.SetActive(true);

        StartDialogue(dialogueData);
    }

    private void Update()
    {
        if (isChoiceTime)
            return;

        if (isEnd)
        {
            CloseDating();
            return;
        }

        if (isTyping)
        {
            FinishTypingImmediately();
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

    public void StartDialogue(DialogueData dialogueData)
    {
        currentDialogueData = dialogueData;
        currentLineIndex = 0;
        isEnd = false;

        HideChoices();
        showingPostStoryFeedback = false;
        if (!isContinuingDate) pendingDateAffection = 0;
        if (!isContinuingDate) currentDateChoiceCount = 0;
        UpdateCharacterStage();

        if (currentDialogueData == null ||
            currentDialogueData.lines == null ||
            currentDialogueData.lines.Length == 0)
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
        if (currentDialogueData == null ||
            currentLineIndex >= currentDialogueData.lines.Length)
        {
            TriggerChoiceSituation();
            return;
        }

        DialogueLine currentLine = currentDialogueData.lines[currentLineIndex];

        if (nameText != null)
            nameText.text = currentLine.name;

        completeSentence = currentLine.sentence;
        AddDialogueLog(currentLine.name, currentLine.sentence);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeSentence(completeSentence));
    }

    private void TriggerChoiceSituation()
    {
        if (currentDialogueData == null)
        {
            ShowEndMessage();
            return;
        }

        DialogueData choiceA = currentDialogueData.nextDialogueA;
        DialogueData choiceB = currentDialogueData.nextDialogueB;

        if (choiceA == null && choiceB == null)
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

        if (choicePanel != null)
            choicePanel.SetActive(true);

        ConfigureChoice(choiceButton1, choiceText1, currentDialogueData.choiceTextA, choiceA);
        ConfigureChoice(choiceButton2, choiceText2, currentDialogueData.choiceTextB, choiceB);
    }

    private void ConfigureChoice(
        Button button,
        TextMeshProUGUI label,
        string choiceText,
        DialogueData nextDialogue)
    {
        if (button == null || label == null)
            return;

        bool available = nextDialogue != null;

        button.gameObject.SetActive(available);

        if (!available)
            return;

        label.text = string.IsNullOrWhiteSpace(choiceText)
            ? "선택"
            : choiceText;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSelectChoice(nextDialogue));
    }

    public void OnSelectChoice(DialogueData selectedDialogue)
    {
        if (lastChoiceFrame == Time.frameCount) return;
        lastChoiceFrame = Time.frameCount;

        if (selectedDialogue == null)
        {
            Debug.LogError("선택지에 연결된 DialogueData가 없습니다.");
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
        if (CampusLifeGameManager.Instance == null)
            return;

        CampusLifeStatDelta delta = new CampusLifeStatDelta
        {
            money = selectedDialogue.moneyChange,
            condition = selectedDialogue.conditionChange,
            grades = selectedDialogue.gradeChange,
            relationship = selectedDialogue.friendshipChange
        };

        CampusLifeGameManager.Instance.TryApplyActivity("연애", delta);
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

        if (choicePanel != null)
            choicePanel.SetActive(false);
    }

    private void ShowEndMessage()
    {
        HideChoices();

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (nameText != null)
            nameText.text = defaultEndName;

        completeSentence = defaultEndSentence;

        if (dialogueText != null)
            dialogueText.text = defaultEndSentence;

        isTyping = false;
        isEnd = true;
    }

    private void FinishTypingImmediately()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (dialogueText != null)
            dialogueText.text = completeSentence;

        isTyping = false;
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;

        if (dialogueText != null)
            dialogueText.text = "";

        foreach (char letter in sentence)
        {
            if (dialogueText != null)
                dialogueText.text += letter;

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    public void CloseDating()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        HideChoices();

        if (datingPanel != null)
            datingPanel.SetActive(false);

        if (dimPanel != null)
            dimPanel.SetActive(false);

        if (CampusLifeGameManager.Instance != null &&
            CampusLifeGameManager.Instance.IsMiniGame)
        {
            CampusLifeGameManager.Instance.ExitMiniGame();
        }

        isTyping = false;
        isChoiceTime = false;
        isEnd = false;
    }
}