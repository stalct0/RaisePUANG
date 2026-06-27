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

    [Header("Visual Database")]
    [SerializeField] private NovelVisualDatabase visualDatabase;

    [Header("Panel")]
    [SerializeField] private GameObject datingPanel;
    [SerializeField] private GameObject dimPanel;

    [Header("Visual Images")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image centerCharacterImage;
    [SerializeField] private Image leftCharacterImage;
    [SerializeField] private Image rightCharacterImage;

    [Header("Dialogue UI")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dialogueText;

    [Header("Choices")]
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private Button choiceButton1;
    [SerializeField] private Button choiceButton2;
    [SerializeField] private TextMeshProUGUI choiceText1;
    [SerializeField] private TextMeshProUGUI choiceText2;

    [Header("Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button logButton;
    [SerializeField] private Button autoButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button closeButton;

    [Header("Log")]
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

    private DialogueData currentDialogueData;
    private DialogueScene currentScene;

    private int currentLineIndex;
    private int pendingAffection;

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

    private void Start()
    {
        if (datingPanel != null) datingPanel.SetActive(false);
        if (choicePanel != null) choicePanel.SetActive(false);
        if (logPanel != null) logPanel.SetActive(false);

        BindButtons();
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (!isOpen && Keyboard.current != null && Keyboard.current[debugOpenKey].wasPressedThisFrame)
            OpenDating();
#endif

        if (!isOpen) return;

        if (Keyboard.current != null && Keyboard.current[closeKey].wasPressedThisFrame)
        {
            CloseDating();
            return;
        }

        if (isAutoMode)
            UpdateAutoMode();
    }

    private void BindButtons()
    {
        if (nextButton != null) nextButton.onClick.AddListener(AdvanceDialogue);
        if (logButton != null) logButton.onClick.AddListener(ToggleLog);
        if (autoButton != null) autoButton.onClick.AddListener(ToggleAuto);
        if (skipButton != null) skipButton.onClick.AddListener(SkipCurrentScene);
        if (closeButton != null) closeButton.onClick.AddListener(CloseDating);
        if (logCloseButton != null) logCloseButton.onClick.AddListener(CloseLog);
    }

    public void OpenDating()
    {
        OpenDating(startDialogueData);
    }

    public void OpenDating(DialogueData data)
    {
        if (data == null || CampusLifeGameManager.Instance == null) return;
        if (!CampusLifeGameManager.Instance.IsPlaying) return;

        currentDialogueData = data;
        pendingAffection = 0;
        isOpen = true;

        dialogueLog.Clear();
        CloseLog();
        StopAuto();

        CampusLifeGameManager.Instance.EnterMiniGame();

        if (dimPanel != null) dimPanel.SetActive(true);
        if (datingPanel != null) datingPanel.SetActive(true);

        StartScene(data.startSceneId);
    }

    private void StartScene(int sceneId)
    {
        currentScene = currentDialogueData.GetScene(sceneId);

        if (currentScene == null)
        {
            ShowEndMessage();
            return;
        }

        currentLineIndex = 0;
        isTyping = false;
        isChoiceTime = false;
        isEnd = false;

        HideChoices();
        ApplySceneStatChange(currentScene);
        ShowNextDialogue();
    }

    private void ShowNextDialogue()
    {
        if (currentScene == null ||
            currentScene.lines == null ||
            currentLineIndex >= currentScene.lines.Length)
        {
            TriggerChoiceOrEnd();
            return;
        }

        DialogueLine line = currentScene.lines[currentLineIndex];

        ApplyLineVisuals(line);

        if (nameText != null)
            nameText.text = line.speaker;

        completeSentence = line.text;
        AddLog(line.speaker, line.text);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeSentence(completeSentence));
    }

    private void ApplyLineVisuals(DialogueLine line)
    {
        if (visualDatabase == null || line == null) return;

        if (!string.IsNullOrWhiteSpace(line.backgroundId))
            ApplyBackground(line.backgroundId);

        ApplyCharacter(centerCharacterImage, line.centerAppearanceId);
        ApplyCharacter(leftCharacterImage, line.leftAppearanceId);
        ApplyCharacter(rightCharacterImage, line.rightAppearanceId);
    }

    private void ApplyBackground(string backgroundId)
    {
        if (backgroundImage == null || visualDatabase == null) return;

        Sprite sprite = visualDatabase.GetBackground(backgroundId);
        if (sprite == null) return;

        backgroundImage.sprite = sprite;
        backgroundImage.gameObject.SetActive(true);
    }

    private void ApplyCharacter(Image target, string appearanceId)
    {
        if (target == null || visualDatabase == null)
            return;

        if (string.IsNullOrWhiteSpace(appearanceId))
        {
            target.sprite = null;
            target.color = Color.clear;
            target.gameObject.SetActive(false);
            return;
        }

        string id = appearanceId.Trim().ToLower();

        if (id == "hide" || id == "none" || id == "off")
        {
            target.sprite = null;
            target.color = Color.clear;
            target.gameObject.SetActive(false);
            return;
        }

        Sprite sprite = visualDatabase.GetAppearance(appearanceId.Trim());

        if (sprite == null)
        {
            target.sprite = null;
            target.color = Color.clear;
            target.gameObject.SetActive(false);
            return;
        }

        target.sprite = sprite;
        target.color = Color.white;
        target.gameObject.SetActive(true);
    }

    public void AdvanceDialogue()
    {
        if (!isOpen || isLogOpen || isChoiceTime) return;

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

    private void TriggerChoiceOrEnd()
    {
        if (currentScene.completesDate)
        {
            CompleteDate();
            return;
        }

        if (!currentScene.hasChoice)
        {
            ShowEndMessage();
            return;
        }

        StopAuto();
        isChoiceTime = true;

        if (choicePanel != null)
            choicePanel.SetActive(true);

        ConfigureChoice(choiceButton1, choiceText1, currentScene.choiceTextA, currentScene.nextSceneA, currentScene.affectionA);
        ConfigureChoice(choiceButton2, choiceText2, currentScene.choiceTextB, currentScene.nextSceneB, currentScene.affectionB);
    }

    private void ConfigureChoice(Button button, TextMeshProUGUI label, string text, int nextSceneId, int affection)
    {
        if (button == null || label == null) return;

        bool available = nextSceneId >= 0;
        button.gameObject.SetActive(available);

        if (!available) return;

        label.text = string.IsNullOrWhiteSpace(text) ? "선택" : text;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            pendingAffection += affection;
            StartScene(nextSceneId);
        });
    }

    private void CompleteDate()
    {
        string resultText = "오늘 데이트는 무난하게 지나간 것 같다.";

        if (DatingProgressManager.Instance != null)
        {
            DatingProgressManager.Instance.CompleteDate(pendingAffection);
            resultText = DatingProgressManager.Instance.GetTodayDateResultText(pendingAffection);
        }

        ShowSystemMessage("System", resultText);
        isEnd = true;
    }

    private string GetTodayDateResultText(int affection)
    {
        if (affection >= 1)
            return "오늘 데이트는 성공적이었던 것 같다.";

        if (affection == 0)
            return "오늘 데이트는 무난하게 지나간 것 같다.";

        return "오늘 데이트는 조금 아쉬웠던 것 같다.";
    }

    private void ApplySceneStatChange(DialogueScene scene)
    {
        CampusLifeStatDelta delta = new CampusLifeStatDelta
        {
            money = scene.moneyChange,
            condition = scene.conditionChange,
            grades = scene.gradeChange,
            relationship = scene.friendshipChange
        };

        if (!delta.IsZero && CampusLifeGameManager.Instance != null)
            CampusLifeGameManager.Instance.TryApplyActivity("연애", delta);
    }

    private void ShowSystemMessage(string speaker, string message)
    {
        HideChoices();
        StopAuto();

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (nameText != null) nameText.text = speaker;
        if (dialogueText != null) dialogueText.text = message;

        completeSentence = message;
        AddLog(speaker, message);

        isTyping = false;
    }

    private void ShowEndMessage()
    {
        ShowSystemMessage("System", "오늘의 이야기는 여기까지다.");
        isEnd = true;
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

    private void FinishTypingImmediately()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        if (dialogueText != null)
            dialogueText.text = completeSentence;

        isTyping = false;
    }

    private void HideChoices()
    {
        isChoiceTime = false;
        if (choicePanel != null) choicePanel.SetActive(false);
    }

    private void ToggleLog()
    {
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
        if (logPanel != null) logPanel.SetActive(false);
    }

    private void AddLog(string speaker, string text)
    {
        dialogueLog.Add($"{speaker}\n{text}");

        if (dialogueLog.Count > 80)
            dialogueLog.RemoveAt(0);
    }

    private void RefreshLogText()
    {
        if (logText == null) return;

        StringBuilder builder = new StringBuilder();

        foreach (string log in dialogueLog)
        {
            builder.AppendLine(log);
            builder.AppendLine();
        }

        logText.text = builder.ToString();
    }

    private void ToggleAuto()
    {
        if (isChoiceTime || isEnd || isLogOpen) return;

        isAutoMode = !isAutoMode;
        autoTimer = 0f;
    }

    private void StopAuto()
    {
        isAutoMode = false;
        autoTimer = 0f;
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

    private void SkipCurrentScene()
    {
        if (isChoiceTime || isLogOpen) return;

        StopAuto();

        if (isTyping)
            FinishTypingImmediately();

        currentLineIndex = currentScene.lines.Length;
        ShowNextDialogue();
    }

    public void CloseDating()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        HideChoices();
        CloseLog();
        StopAuto();

        if (datingPanel != null) datingPanel.SetActive(false);
        if (dimPanel != null) dimPanel.SetActive(false);

        if (CampusLifeGameManager.Instance != null &&
            CampusLifeGameManager.Instance.IsMiniGame)
        {
            CampusLifeGameManager.Instance.ExitMiniGame();
        }

        isOpen = false;
        isTyping = false;
        isChoiceTime = false;
        isEnd = false;
        pendingAffection = 0;
    }
    public void OpenDatingFromIntro(DialogueData data)
    {
        if (data == null) return;

        currentDialogueData = data;
        pendingAffection = 0;
        isOpen = true;

        dialogueLog.Clear();
        CloseLog();
        StopAuto();

        if (datingPanel != null)
            datingPanel.SetActive(true);

        StartScene(data.startSceneId);
    }
}