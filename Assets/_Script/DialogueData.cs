using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public string speaker;

    [TextArea(2, 5)]
    public string text;

    [Header("Visual IDs")]
    public string backgroundId;
    public string puangAppearanceId;
    public string centerAppearanceId;
    public string leftAppearanceId;
    public string rightAppearanceId;
}

[Serializable]
public class DialogueScene
{
    public int sceneId;
    public DialogueLine[] lines;

    public bool hasChoice;

    public string choiceTextA;
    public int nextSceneA = -1;
    public int affectionA;

    public string choiceTextB;
    public int nextSceneB = -1;
    public int affectionB;

    public int moneyChange;
    public int conditionChange;
    public int gradeChange;
    public int friendshipChange;

    public bool completesDate;
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Create Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public string dialogueId;

    public NovelStoryKind storyKind = NovelStoryKind.Normal;
    public DatingCharacter datingCharacter = DatingCharacter.None;
    public DatingLocation datingLocation = DatingLocation.None;

    public int startSceneId = 0;
    public DialogueScene[] scenes;

    public DialogueScene GetScene(int sceneId)
    {
        if (scenes == null) return null;

        foreach (DialogueScene scene in scenes)
        {
            if (scene.sceneId == sceneId)
                return scene;
        }

        return null;
    }
}