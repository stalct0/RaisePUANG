using UnityEngine;
using UnityEngine.UI;

public class EndingCollectionUI : MonoBehaviour
{
    [Header("Database")]
    [SerializeField] private EndingDatabase endingDatabase;

    [Header("Panel")]
    [SerializeField] private GameObject collectionPanel;

    [Header("Slots")]
    [SerializeField] private EndingCollectionSlot[] slots;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (collectionPanel != null)
            collectionPanel.SetActive(false);
    }

    public void Open()
    {
        if (collectionPanel != null)
            collectionPanel.SetActive(true);

        Refresh();
    }

    public void Close()
    {
        if (collectionPanel != null)
            collectionPanel.SetActive(false);
    }

    private void Refresh()
    {
        if (slots == null)
            return;

        foreach (EndingCollectionSlot slot in slots)
        {
            if (slot == null)
                continue;

            slot.Refresh(endingDatabase);
        }
    }
}