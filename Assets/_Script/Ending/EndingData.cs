using UnityEngine;

public enum EndingType
{
    Normal,
    Hidden
}

[CreateAssetMenu(fileName = "NewEndingData", menuName = "Ending/Ending Data")]
public class EndingData : ScriptableObject
{
    public string endingId;
    public string endingName;

    [TextArea(2, 5)]
    public string description;

    public EndingType endingType;

    [Header("Images")]
    public Sprite endingImage;

    [Tooltip("노말 엔딩에서 여자친구 이미지를 같이 띄울지")]
    public bool showGirlfriendOverlay;
}