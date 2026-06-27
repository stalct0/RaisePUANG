using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public string speaker;

    [TextArea(2, 5)]
    public string text;
}

[Serializable]
public class DialogueScene
{
    public int sceneId;

    [Header("Lines")]
    public DialogueLine[] lines;

    [Header("Choice")]
    public bool hasChoice;

    public string choiceTextA;
    public int nextSceneA = -1;
    public int affectionA;

    public string choiceTextB;
    public int nextSceneB = -1;
    public int affectionB;

    [Header("Stat Change")]
    public int moneyChange;
    public int conditionChange;
    public int gradeChange;
    public int friendshipChange;

    [Header("End")]
    public bool completesDate;
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Create Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("Basic")]
    public string dialogueId;

    public NovelStoryKind storyKind = NovelStoryKind.Normal;
    public DatingCharacter datingCharacter = DatingCharacter.None;
    public DatingLocation datingLocation = DatingLocation.None;

    [Header("Scene")]
    public int startSceneId = 0;
    public DialogueScene[] scenes;

    public DialogueScene GetScene(int sceneId)
    {
        if (scenes == null) return null;

        for (int i = 0; i < scenes.Length; i++)
        {
            if (scenes[i].sceneId == sceneId)
                return scenes[i];
        }

        return null;
    }
}