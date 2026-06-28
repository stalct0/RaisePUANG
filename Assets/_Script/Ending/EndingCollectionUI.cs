using TMPro;
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
        if (endingDatabase == null || endingDatabase.endings == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            if (i >= endingDatabase.endings.Length)
            {
                slots[i].gameObject.SetActive(false);
                continue;
            }

            slots[i].gameObject.SetActive(true);
            slots[i].SetData(endingDatabase.endings[i]);
        }
    }
}