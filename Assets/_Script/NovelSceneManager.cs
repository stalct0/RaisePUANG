using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

    private void Awake()
    {
        if (applyKoreanFontOnStart)
        {
            ApplyKoreanFontToSceneTexts();
        }
    }

    private void Start()
    {
        HideChoices();
        StartDialogue(dialogueData);
    }

    public void OnClickDialogWindow()
    {
        if (isChoiceTime) return;

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

    public void StartDialogue(DialogueData data)
    {
        dialogueData = data;
        currentLineIndex = 0;
        HideChoices();

        if (dialogueData == null || dialogueData.lines == null || dialogueData.lines.Length == 0)
        {
            ShowEndMessage();
            return;
        }

        ShowNextDialogue();
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

        StopAllCoroutines();
        StartCoroutine(TypeSentence(completeSentence));
    }

    private void TriggerChoiceSituation()
    {
        DialogueData choiceA = dialogueData != null && dialogueData.nextDialogueA != null ? dialogueData.nextDialogueA : nextDialogueA;
        DialogueData choiceB = dialogueData != null && dialogueData.nextDialogueB != null ? dialogueData.nextDialogueB : nextDialogueB;

        if (choiceA == null && choiceB == null)
        {
            ShowEndMessage();
            return;
        }

        isChoiceTime = true;
        choicePanel.SetActive(true);
        ConfigureChoice(choiceButton1, choiceText1, dialogueData != null ? dialogueData.choiceTextA : string.Empty, choiceA);
        ConfigureChoice(choiceButton2, choiceText2, dialogueData != null ? dialogueData.choiceTextB : string.Empty, choiceB);
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
        if (selectedDialogue == null)
        {
            Debug.LogError("연결된 다음 대사 파일(DialogueData)이 없습니다!");
            return;
        }

        ApplyDialogueReward(selectedDialogue);
        StartDialogue(selectedDialogue);
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

    private void HideChoices()
    {
        isChoiceTime = false;
        if (choicePanel != null) choicePanel.SetActive(false);
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

        TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            text.font = fontAsset;
            text.SetAllDirty();
        }
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
