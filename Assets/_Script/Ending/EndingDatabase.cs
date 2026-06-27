using UnityEngine;

[CreateAssetMenu(fileName = "EndingDatabase", menuName = "Ending/Ending Database")]
public class EndingDatabase : ScriptableObject
{
    public EndingData[] endings;

    public EndingData GetEnding(string endingId)
    {
        if (endings == null)
            return null;

        foreach (EndingData ending in endings)
        {
            if (ending != null && ending.endingId == endingId)
                return ending;
        }

        Debug.LogError($"EndingData를 찾을 수 없습니다: {endingId}");
        return null;
    }
}