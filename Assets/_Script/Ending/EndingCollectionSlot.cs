using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingCollectionSlot : MonoBehaviour
{
    [Header("Ending ID")]
    [SerializeField] private string endingId;

    [Header("UI")]
    [SerializeField] private Image endingImage;
    [SerializeField] private TMP_Text endingNameText;

    [Header("Locked")]
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private string lockedName = "???";

    public void Refresh(EndingDatabase database)
    {
        if (database == null)
            return;

        EndingData endingData = database.GetEnding(endingId);

        if (endingData == null)
            return;

        bool seen = EndingCollectionManager.HasSeenEnding(endingId);

        if (endingImage != null)
        {
            endingImage.sprite = seen ? endingData.endingImage : lockedSprite;
            endingImage.preserveAspect = true;
        }

        if (endingNameText != null)
        {
            endingNameText.text = seen ? endingData.endingName : lockedName;
        }
    }
}