using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NovelSceneManager : MonoBehaviour
{
    [Header("Dialogue Data")]
    [SerializeField] private DialogueData startDialogueData;

    [Header("Panel")]
    [SerializeField] private GameObject datingPanel;
    [SerializeField] private GameObject dimPanel;

    [Header("Dialogue UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Choices")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button choiceButton1;
    [SerializeField] private Button choiceButton2;
    [SerializeField] private TextMeshProUGUI choiceText1;
    [SerializeField] private TextMeshProUGUI choiceText2;

    [Header("Utility Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button logButton;
    [SerializeField] private Button autoButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button closeButton;

    [Header("Log Panel")]
    [SerializeField] private GameObject logPanel;
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private Button logCloseButton;

    [Header("Debug")]
    [SerializeField] private Key debugOpenKey = Key.D;
    [SerializeField] private Key closeKey = Key.Escape;

    [Header("Typing")]
    [SerializeField] private float typingSpeed = 0.035f;

    [Header("Auto")]
    [SerializeField] private float autoDelay = 1.2f;

    [Header("End Message")]
    [SerializeField] private string defaultEndName = "System";
    [TextArea(2, 4)]
    [SerializeField] private string defaultEndSentence = "오늘의 이야기는 여기까지다.";

    private DialogueData currentDialogueData;
    private int currentLineIndex;

    private bool isOpen;
    private bool isTyping;
    private bool isChoiceTime;
    private bool isEnd;
    private bool isAutoMode;
    private bool isLogOpen;

    private float autoTimer;
    private string completeSentence = "";
    private Coroutine typingCoroutine;

    private readonly List<string> dialogueLog = new List<string>();
    private const int MaxLogCount = 80;

    private void Start()
    {
        if (datingPanel != null)
            datingPanel.SetActive(false);

        if (choicePanel != null)
            choicePanel.SetActive(false);

        if (logPanel != null)
            logPanel.SetActive(false);

        BindButtons();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!isOpen && Keyboard.current != null && Keyboard.current[debugOpenKey].wasPressedThisFrame)
        {
            OpenDating();
        }
#endif

        if (!isOpen)
            return;

        if (Keyboard.current != null && Keyboard.current[closeKey].wasPressedThisFrame)
        {
            CloseDating();
            return;
        }

        if (isAutoMode)
        {
            UpdateAutoMode();
        }
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    private void BindButtons()
    {
        if (nextButton != null)
            nextButton.onClick.AddListener(AdvanceDialogue);

        if (logButton != null)
            logButton.onClick.AddListener(ToggleLog);

        if (autoButton != null)
            autoButton.onClick.AddListener(ToggleAuto);

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipToChoiceOrEnd);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseDating);

        if (logCloseButton != null)
            logCloseButton.onClick.AddListener(CloseLog);
    }

    private void UnbindButtons()
    {
        if (nextButton != null)
            nextButton.onClick.RemoveListener(AdvanceDialogue);

        if (logButton != null)
            logButton.onClick.RemoveListener(ToggleLog);

        if (autoButton != null)
            autoButton.onClick.RemoveListener(ToggleAuto);

        if (skipButton != null)
            skipButton.onClick.RemoveListener(SkipToChoiceOrEnd);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseDating);

        if (logCloseButton != null)
            logCloseButton.onClick.RemoveListener(CloseLog);
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

        if (DatingProgressManager.Instance != null)
        {
            if (!DatingProgressManager.Instance.CanStartDate(out string reason))
            {
                Debug.Log(reason);
                return;
            }
        }

        isOpen = true;

        CampusLifeGameManager.Instance.EnterMiniGame();

        if (dimPanel != null)
            dimPanel.SetActive(true);

        if (datingPanel != null)
            datingPanel.SetActive(true);

        dialogueLog.Clear();
        CloseLog();
        StopAuto();

        StartDialogue(dialogueData);
    }

    public void StartDialogue(DialogueData dialogueData)
    {
        currentDialogueData = dialogueData;
        currentLineIndex = 0;

        isTyping = false;
        isChoiceTime = false;
        isEnd = false;

        HideChoices();

        if (currentDialogueData == null ||
            currentDialogueData.lines == null ||
            currentDialogueData.lines.Length == 0)
        {
            ShowEndMessage();
            return;
        }

        ShowNextDialogue();
    }

    public void AdvanceDialogue()
    {
        if (!isOpen)
            return;

        if (isLogOpen)
            return;

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

        currentLineIndex++;
        ShowNextDialogue();
    }

    private void ShowNextDialogue()
    {
        if (currentDialogueData == null ||
            currentLineIndex >= currentDialogueData.lines.Length)
        {
            TriggerChoiceSituation();
            return;
        }

        DialogueLine line = currentDialogueData.lines[currentLineIndex];

        if (nameText != null)
            nameText.text = line.name;

        completeSentence = line.sentence;
        AddLog(line.name, line.sentence);

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
            CompleteDatingIfNeeded();
            ShowEndMessage();
            return;
        }

        StopAuto();
        isChoiceTime = true;

        if (choicePanel != null)
            choicePanel.SetActive(true);

        ConfigureChoice(choiceButton1, choiceText1, currentDialogueData.choiceTextA, choiceA);
        ConfigureChoice(choiceButton2, choiceText2, currentDialogueData.choiceTextB, choiceB);
    }

    private void ConfigureChoice(Button button, TextMeshProUGUI label, string choiceText, DialogueData nextDialogue)
    {
        if (button == null || label == null)
            return;

        bool available = nextDialogue != null;
        button.gameObject.SetActive(available);

        if (!available)
            return;

        label.text = string.IsNullOrWhiteSpace(choiceText) ? "선택" : choiceText;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSelectChoice(nextDialogue));
    }

    public void OnSelectChoice(DialogueData selectedDialogue)
    {
        if (selectedDialogue == null)
        {
            Debug.LogError("선택지에 DialogueData가 없습니다.");
            return;
        }

        ApplyDialogueReward(selectedDialogue);
        StartDialogue(selectedDialogue);
    }

    private void ApplyDialogueReward(DialogueData data)
    {
        if (CampusLifeGameManager.Instance == null)
            return;

        CampusLifeStatDelta delta = new CampusLifeStatDelta
        {
            money = data.moneyChange,
            condition = data.conditionChange,
            grades = data.gradeChange,
            relationship = data.friendshipChange
        };

        CampusLifeGameManager.Instance.TryApplyActivity("연애", delta);
    }

    private void CompleteDatingIfNeeded()
    {
        if (DatingProgressManager.Instance == null)
            return;

        DatingProgressManager.Instance.CompleteDate(
            DatingCharacter.None,
            DatingLocation.None,
            0
        );
    }

    private void ToggleLog()
    {
        if (!isOpen)
            return;

        isLogOpen = !isLogOpen;

        if (logPanel != null)
            logPanel.SetActive(isLogOpen);

        if (isLogOpen)
        {
            StopAuto();
            RefreshLogText();
        }
    }

    private void CloseLog()
    {
        isLogOpen = false;

        if (logPanel != null)
            logPanel.SetActive(false);
    }

    private void AddLog(string speaker, string sentence)
    {
        string speakerName = string.IsNullOrWhiteSpace(speaker) ? "Narrator" : speaker;
        dialogueLog.Add($"{speakerName}\n{sentence}");

        while (dialogueLog.Count > MaxLogCount)
            dialogueLog.RemoveAt(0);

        if (isLogOpen)
            RefreshLogText();
    }

    private void RefreshLogText()
    {
        if (logText == null)
            return;

        StringBuilder builder = new StringBuilder();

        for (int i = 0; i < dialogueLog.Count; i++)
        {
            builder.AppendLine(dialogueLog[i]);
            builder.AppendLine();
        }

        logText.text = builder.Length > 0
            ? builder.ToString()
            : "아직 대화 기록이 없습니다.";
    }

    private void ToggleAuto()
    {
        if (!isOpen || isChoiceTime || isEnd || isLogOpen)
            return;

        isAutoMode = !isAutoMode;
        autoTimer = 0f;

        UpdateAutoButtonVisual();
    }

    private void StopAuto()
    {
        isAutoMode = false;
        autoTimer = 0f;
        UpdateAutoButtonVisual();
    }

    private void UpdateAutoMode()
    {
        if (isTyping || isChoiceTime || isEnd || isLogOpen)
        {
            autoTimer = 0f;
            return;
        }

        autoTimer += Time.unscaledDeltaTime;

        if (autoTimer >= autoDelay)
        {
            autoTimer = 0f;
            AdvanceDialogue();
        }
    }

    private void UpdateAutoButtonVisual()
    {
        if (autoButton == null || autoButton.targetGraphic == null)
            return;

        autoButton.targetGraphic.color = isAutoMode
            ? new Color(0.4f, 0.8f, 1f, 1f)
            : Color.white;
    }

    private void SkipToChoiceOrEnd()
    {
        if (!isOpen || isChoiceTime || isLogOpen)
            return;

        StopAuto();

        if (isTyping)
            FinishTypingImmediately();

        if (currentDialogueData == null ||
            currentDialogueData.lines == null)
        {
            ShowEndMessage();
            return;
        }

        currentLineIndex = currentDialogueData.lines.Length;
        ShowNextDialogue();
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
        StopAuto();

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (nameText != null)
            nameText.text = defaultEndName;

        completeSentence = defaultEndSentence;

        if (dialogueText != null)
            dialogueText.text = completeSentence;

        AddLog(defaultEndName, defaultEndSentence);

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

        foreach (char c in sentence)
        {
            if (dialogueText != null)
                dialogueText.text += c;

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        isTyping = false;
    }

    public void CloseDating()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        HideChoices();
        CloseLog();
        StopAuto();

        if (datingPanel != null)
            datingPanel.SetActive(false);

        if (dimPanel != null)
            dimPanel.SetActive(false);

        if (CampusLifeGameManager.Instance != null &&
            CampusLifeGameManager.Instance.IsMiniGame)
        {
            CampusLifeGameManager.Instance.ExitMiniGame();
        }

        isOpen = false;
        isTyping = false;
        isChoiceTime = false;
        isEnd = false;
    }
}