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
    public GameObject choicePanel;         // 1단계에서 만든 ChoicePanel
    public Button choiceButton1;           // ChoiceButton_1
    public Button choiceButton2;           // ChoiceButton_2
    public TextMeshProUGUI choiceText1;    // ChoiceButton_1의 자식 텍스트
    public TextMeshProUGUI choiceText2;    // ChoiceButton_2의 자식 텍스트

    [Header("--- 선택지 분기용 대사 데이터 ---")]
    public DialogueData nextDialogueA;     // 1번 선택 시 이어질 대사 파일
    public DialogueData nextDialogueB;     // 2번 선택 시 이어질 대사 파일

    [Header("--- 연출 설정 ---")]
    public float typingSpeed = 0.05f;      

    private int currentLineIndex = 0;      
    private bool isTyping = false;         
    private string completeSentence = "";  
    private bool isChoiceTime = false;     // 현재 선택지가 켜진 상황인가?

    void Start()
    {
        choicePanel.SetActive(false); // 시작할 땐 선택지 숨기기
        if (dialogueData != null && dialogueData.lines.Length > 0)
        {
            currentLineIndex = 0;
            ShowNextDialogue();
        }
    }

    public void OnClickDialogWindow()
    {
        // 선택지가 화면에 떠있을 때는 대사창 클릭으로 안 넘어가게 막기
        if (isChoiceTime) return;

        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = completeSentence;
            isTyping = false;
        }
        else
        {
            currentLineIndex++;
            ShowNextDialogue();
        }
    }

    private void ShowNextDialogue()
    {
        // 준비된 대사가 다 끝났을 때 ➡️ 선택지를 띄운다!
        if (currentLineIndex >= dialogueData.lines.Length)
        {
            TriggerChoiceSituation();
            return;
        }

        DialogueLine currentLine = dialogueData.lines[currentLineIndex];
        nameText.text = currentLine.name;
        completeSentence = currentLine.sentence;

        StartCoroutine(TypeSentence(completeSentence));
    }

    // 대사가 끝나면 선택지 판넬을 켜는 메서드
    private void TriggerChoiceSituation()
    {
        isChoiceTime = true;
        choicePanel.SetActive(true); // 선택지 창 팝업!

        // 버튼 텍스트를 기획에 맞게 세팅 (임시 더미 텍스트)
        choiceText1.text = "1. 술자리에 끝까지 남는다 (재미+10)";
        choiceText2.text = "2. 피곤하니 먼저 집에 간다 (컨디션+20)";

        // 버튼 클릭 이벤트 연결
        choiceButton1.onClick.RemoveAllListeners();
        choiceButton1.onClick.AddListener(() => OnSelectChoice(nextDialogueA));

        choiceButton2.onClick.RemoveAllListeners();
        choiceButton2.onClick.AddListener(() => OnSelectChoice(nextDialogueB));
    }

    // 플레이어가 선택지를 클릭했을 때 실행되는 메서드
    public void OnSelectChoice(DialogueData selectedDialogue)
    {
        if (selectedDialogue == null)
        {
            Debug.LogError("연결된 다음 대사 파일(DialogueData)이 없습니다!");
            return;
        }

        // 1. 선택지 창을 다시 닫고 상태 해제
        choicePanel.SetActive(false);
        isChoiceTime = false;

        // 2. 대사 데이터를 플레이어가 선택한 새로운 데이터로 교체
        dialogueData = selectedDialogue;
        currentLineIndex = 0;

        if (GameCenter.Instance != null)
         {
            GameCenter.Instance.ChangeStatus(
                selectedDialogue.moneyChange, 
                selectedDialogue.conditionChange, 
                selectedDialogue.gradeChange, 
                selectedDialogue.friendshipChange
            );
         }

        // 3. 새로운 대사 시작
        ShowNextDialogue();
    }

    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = ""; 

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter; 
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }


}