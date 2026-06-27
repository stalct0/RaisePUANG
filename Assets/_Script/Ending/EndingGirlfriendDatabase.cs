using System;
using UnityEngine;

[Serializable]
public class GirlfriendEndingImageEntry
{
    public DatingCharacter character;
    public Sprite endingSprite;
}

[CreateAssetMenu(fileName = "EndingGirlfriendDatabase", menuName = "Ending/Girlfriend Database")]
public class EndingGirlfriendDatabase : ScriptableObject
{
    public GirlfriendEndingImageEntry[] entries;

    public Sprite GetSprite(DatingCharacter character)
    {
        if (entries == null)
            return null;

        foreach (GirlfriendEndingImageEntry entry in entries)
        {
            if (entry.character == character)
                return entry.endingSprite;
        }

        return null;
    }
}