using UnityEngine;

[System.Serializable]
public struct DialogueLine
{
    public string name;
    [TextArea(3, 5)]
    public string sentence;

    [Header("변경할 배경 이미지 파일명 (비어있으면 이전 배경 유지)")]
    public string backgroundName;
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Create New Dialogue")]
public class DialogueData : ScriptableObject
{
    [Header("--- 이 스토리가 시작될 때 변화할 스탯 값 ---")]
    public int moneyChange;
    public int conditionChange;
    public int gradeChange;
    public int friendshipChange;

    [Header("--- 연출 정보 ---")]
    [Range(0, 3)] public int visibleCharacterCount = 1;

    [Header("--- 대사 리스트 ---")]
    public DialogueLine[] lines;

    [Header("--- 선택지 ---")]
    public string choiceTextA;
    public DialogueData nextDialogueA;
    public string choiceTextB;
    public DialogueData nextDialogueB;

    public bool HasChoices => nextDialogueA != null || nextDialogueB != null;
}
