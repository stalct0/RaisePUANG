using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    public void OnClickDialogWindow()
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

        currentLineIndex++;
        ShowNextDialogue();
    }

    public void StartDialogue(DialogueData dialogueData)
    {
        currentDialogueData = dialogueData;
        currentLineIndex = 0;
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
        if (selectedDialogue == null)
        {
            Debug.LogError("선택지에 연결된 DialogueData가 없습니다.");
            return;
        }

        ApplyDialogueReward(selectedDialogue);
        StartDialogue(selectedDialogue);
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