using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
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

    private void Update()
    {
        if (isChoiceTime)
        {
            if (TrySelectChoiceFromPointer())
            {
                return;
            }

            return;
        }

        if (WasAdvancePressed())
        {
            AdvanceDialogue();
        }
    }

    public void OnClickDialogWindow()
    {
        if (isChoiceTime) return;
        AdvanceDialogue();
    }

    public void StartDialogue(DialogueData data)
    {
        dialogueData = data;
        currentLineIndex = 0;
        HideChoices();
        UpdateCharacterStage();

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
        currentChoiceA = dialogueData != null && dialogueData.nextDialogueA != null ? dialogueData.nextDialogueA : nextDialogueA;
        currentChoiceB = dialogueData != null && dialogueData.nextDialogueB != null ? dialogueData.nextDialogueB : nextDialogueB;

        if (currentChoiceA == null && currentChoiceB == null)
        {
            ShowEndMessage();
            return;
        }

        isChoiceTime = true;
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
        builder.Append("LOG AUTO SKIP 선택지 A 선택지 B 클릭해서 대화를 진행하세요.");

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
