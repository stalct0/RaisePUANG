using TMPro;
using UnityEngine;

public class StatPopupSpawner : MonoBehaviour
{
    [Header("Popup Prefab")]
    [SerializeField] private FloatingStatText popupPrefab;

    [Header("Spawn Target")]
    [Tooltip("Assign the Player transform. If empty, this object's transform is used.")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.75f, 0f);

    private const float PopupLineSpacing = 0.32f;

    private void Awake()
    {
        ValidateReferences();
    }

    public void ShowDelta(CampusLifeStatDelta delta)
    {
        if (delta.IsZero)
        {
            Debug.Log("[StatPopupSpawner] ShowDelta was called with a zero delta. No popup will be spawned.", this);
            return;
        }

        if (popupPrefab == null)
        {
            Debug.LogError(
                $"[StatPopupSpawner] {gameObject.name}: Popup Prefab is not assigned. " +
                "Assign the StatPopupText prefab in the Inspector.",
                this);
            return;
        }

        Debug.Log($"[StatPopupSpawner] Showing delta popup: {FormatDeltaForLog(delta)}", this);

        float lineOffset = 0f;

        if (delta.condition != 0)
        {
            SpawnText($"컨디션 {Format(delta.condition)}", lineOffset);
            lineOffset += PopupLineSpacing;
        }

        if (delta.money != 0)
        {
            SpawnText($"돈 {Format(delta.money)}", lineOffset);
            lineOffset += PopupLineSpacing;
        }

        if (delta.grades != 0)
        {
            SpawnText($"학점 {Format(delta.grades)}", lineOffset);
            lineOffset += PopupLineSpacing;
        }

        if (delta.relationship != 0)
        {
            SpawnText($"친구관계 {Format(delta.relationship)}", lineOffset);
        }
    }

    private void SpawnText(string text, float extraY)
    {
        Transform spawnTarget = target != null ? target : transform;
        Vector3 spawnPosition = spawnTarget.position + offset + new Vector3(0f, extraY, 0f);

        FloatingStatText popup = Instantiate(popupPrefab, spawnPosition, Quaternion.identity);
        popup.Init(text);

        Debug.Log($"[StatPopupSpawner] Spawned popup '{text}' at {spawnPosition}.", popup);
    }

    private void ValidateReferences()
    {
        if (popupPrefab == null)
        {
            Debug.LogError(
                $"[StatPopupSpawner] {gameObject.name}: Popup Prefab is not assigned. " +
                "Assign a prefab that has FloatingStatText and a world-space TextMeshPro component.",
                this);
        }
        else if (popupPrefab.GetComponentInChildren<TextMeshPro>(true) == null)
        {
            Debug.LogError(
                $"[StatPopupSpawner] {popupPrefab.name}: Popup Prefab must contain a world-space TextMeshPro component. " +
                "Do not use TextMeshProUGUI for this popup.",
                popupPrefab);
        }

        if (target == null)
        {
            Debug.LogWarning(
                $"[StatPopupSpawner] {gameObject.name}: Target is not assigned. This object's transform will be used. " +
                "Assign the Player transform in the Inspector for player-following popups.",
                this);
        }
    }

    private string Format(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }

    private string FormatDeltaForLog(CampusLifeStatDelta delta)
    {
        return $"money={delta.money}, condition={delta.condition}, grades={delta.grades}, relationship={delta.relationship}";
    }
}
