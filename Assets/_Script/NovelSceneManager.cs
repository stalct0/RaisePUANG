using System.Collections;
using UnityEngine;
using TMPro;

public class NovelSceneManager : MonoBehaviour
{
    [Header("--- 대사 데이터 에셋 ---")]
    public DialogueData dialogueData; // 1단계에서 만든 대사 파일을 여기 드래그앤드롭

    [Header("--- UI 컴포넌트 (TMP) ---")]
    public TextMeshProUGUI nameText;       // 이름 표시용 텍스트
    public TextMeshProUGUI dialogueText;   // 대사 표시용 텍스트

    [Header("--- 연출 설정 ---")]
    public float typingSpeed = 0.05f;      // 글자가 찍히는 속도 (초 단위)

    private int currentLineIndex = 0;      // 현재 몇 번째 대사인지
    private bool isTyping = false;         // 현재 글자가 찍히는 중인가?
    private string completeSentence = "";  // 현재 줄의 전체 대사 내용

    void Start()
    {
        if (dialogueData != null && dialogueData.lines.Length > 0)
        {
            currentLineIndex = 0;
            ShowNextDialogue();
        }
    }

    // 화면(또는 대사창 버튼)을 클릭했을 때 호출할 메서드
    public void OnClickDialogWindow()
    {
        // 1. 글자가 한 글자씩 또르륵 출력 중일 때 클릭하면 ➡️ 한 번에 전체 대사 다 보여주기
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueText.text = completeSentence;
            isTyping = false;
        }
        // 2. 대사 출력이 이미 끝난 상태에서 클릭하면 ➡️ 다음 대사로 넘어가기
        else
        {
            currentLineIndex++;
            ShowNextDialogue();
        }
    }

    // 대사를 화면에 세팅하는 메서드
    private void ShowNextDialogue()
    {
        // 모든 대사가 끝났다면 게임 오버 혹은 다른 처리
        if (currentLineIndex >= dialogueData.lines.Length)
        {
            Debug.Log("스토리가 모두 끝났습니다! 육성 씬으로 복귀하거나 이벤트를 종료합니다.");
            dialogueText.text = "[스토리 종료]";
            return;
        }

        // 현재 순서의 대사 데이터 가져오기
        DialogueLine currentLine = dialogueData.lines[currentLineIndex];

        nameText.text = currentLine.name;
        completeSentence = currentLine.sentence;

        // 한 글자씩 출력하는 코루틴 시작
        StartCoroutine(TypeSentence(completeSentence));
    }

    // 타이프라이터 효과 연출 코루틴
    private IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueText.text = ""; // 이전 대사 비우기

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueText.text += letter; // 한 글자씩 붙이기
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }
}