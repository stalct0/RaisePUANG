using System.Collections.Generic;
using UnityEngine;

public static class EndingCollectionManager
{
    private const string SeenEndingPrefix = "SeenEnding_";

    public static void UnlockEnding(string endingId)
    {
        if (string.IsNullOrWhiteSpace(endingId))
            return;

        PlayerPrefs.SetInt(SeenEndingPrefix + endingId, 1);
        PlayerPrefs.Save();

        Debug.Log($"[EndingCollection] Unlocked: {endingId}");
    }

    public static bool HasSeenEnding(string endingId)
    {
        if (string.IsNullOrWhiteSpace(endingId))
            return false;

        return PlayerPrefs.GetInt(SeenEndingPrefix + endingId, 0) == 1;
    }

    public static void DebugResetCollection()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
    }
}