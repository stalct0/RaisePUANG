using System;
using UnityEngine;

[Serializable]
public class BackgroundEntry
{
    public string backgroundId;
    public Sprite sprite;
}

[Serializable]
public class AppearanceEntry
{
    public string appearanceId;
    public Sprite sprite;
}

[CreateAssetMenu(fileName = "NovelVisualDatabase", menuName = "Dialogue/Novel Visual Database")]
public class NovelVisualDatabase : ScriptableObject
{
    public BackgroundEntry[] backgrounds;
    public AppearanceEntry[] appearances;

    public Sprite GetBackground(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        foreach (BackgroundEntry entry in backgrounds)
        {
            if (entry.backgroundId == id)
                return entry.sprite;
        }

        Debug.LogWarning($"배경 ID 없음: {id}");
        return null;
    }

    public Sprite GetAppearance(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        foreach (AppearanceEntry entry in appearances)
        {
            if (entry.appearanceId == id)
                return entry.sprite;
        }

        Debug.LogWarning($"외형 ID 없음: {id}");
        return null;
    }
}