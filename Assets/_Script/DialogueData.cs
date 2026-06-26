using UnityEngine;

// 대사 한 줄의 정보를 담는 구조체
[System.Serializable]
public struct DialogueLine
{
    public string name;       // 캐릭터 이름
    [TextArea(3, 5)] 
    public string sentence;   // 대사 본문
}

// 유니티 에디터 메뉴에 등록
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Create New Dialogue")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] lines; // 대사 배열
}