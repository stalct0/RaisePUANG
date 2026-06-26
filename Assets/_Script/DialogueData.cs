using UnityEngine;

[System.Serializable]
public struct DialogueLine
{
    public string name;       
    [TextArea(3, 5)] 
    public string sentence;   
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Create New Dialogue")]
public class DialogueData : ScriptableObject
{
    [Header("--- 이 스토리가 시작될 때 변화할 스탯 값 ---")]
    public int moneyChange;
    public int conditionChange;
    public int gradeChange;
    public int friendshipChange;

    [Header("--- 대사 리스트 ---")]
    public DialogueLine[] lines; 
}