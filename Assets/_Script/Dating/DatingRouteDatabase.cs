using System;
using UnityEngine;

[Serializable]
public class DatingRouteEntry
{
    public DatingCharacter character;
    public int dateIndex; // 1~10
    public DialogueData dialogueData;
}

[CreateAssetMenu(fileName = "DatingRouteDatabase", menuName = "Dating/Dating Route Database")]
public class DatingRouteDatabase : ScriptableObject
{
    public DatingRouteEntry[] entries;

    public DialogueData GetDialogue(DatingCharacter character, int dateIndex)
    {
        foreach (DatingRouteEntry entry in entries)
        {
            if (entry.character == character && entry.dateIndex == dateIndex)
                return entry.dialogueData;
        }

        Debug.LogError($"데이트 Dialogue 없음: {character}, {dateIndex}회차");
        return null;
    }
}