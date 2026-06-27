using System.Collections.Generic;
using UnityEngine;

public sealed class DatingProgressManager : MonoBehaviour
{
    public static DatingProgressManager Instance { get; private set; }

    private const int MaxDateIndex = 10;
    
    [SerializeField] private DatingRouteDatabase routeDatabase;
    [Header("Route")]
    [SerializeField] private DatingCharacter selectedGirlfriend = DatingCharacter.ChildhoodFriend;
    [SerializeField] private int currentDateIndex = 1;

    [Header("Progress")]
    [SerializeField] private int totalDateCount;
    [SerializeField] private bool datingEnded;

    private readonly Dictionary<DatingCharacter, int> affectionByCharacter = new();

    public DatingCharacter SelectedGirlfriend => selectedGirlfriend;
    public int CurrentDateIndex => currentDateIndex;
    public int TotalDateCount => totalDateCount;
    public bool DatingEnded => datingEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureAffectionKeys();
    }

    public bool CanStartDate(out string reason)
    {
        if (datingEnded)
        {
            reason = "이미 모든 데이트를 진행했습니다.";
            return false;
        }

        if (currentDateIndex > MaxDateIndex)
        {
            reason = "진행할 데이트가 더 이상 없습니다.";
            return false;
        }

        reason = "";
        return true;
    }

    public void SetGirlfriend(DatingCharacter character)
    {
        if (character == DatingCharacter.None)
            return;

        selectedGirlfriend = character;
    }

    public void CompleteDate(int affectionDelta)
    {
        if (datingEnded)
            return;

        AddAffection(selectedGirlfriend, affectionDelta);

        totalDateCount++;
        currentDateIndex++;

        if (currentDateIndex > MaxDateIndex)
        {
            datingEnded = true;

            if (EndingManager.Instance != null)
                EndingManager.Instance.TriggerTrueLoveEnding();
        }
    }

    public int GetAffection(DatingCharacter character)
    {
        EnsureAffectionKeys();

        if (affectionByCharacter.TryGetValue(character, out int affection))
            return affection;

        return 0;
    }

    public string GetTodayDateResultText(int todayAffectionDelta)
    {
        if (todayAffectionDelta >= 1)
            return "오늘 데이트는 성공적이었던 것 같다.";

        if (todayAffectionDelta == 0)
            return "오늘 데이트는 무난하게 지나간 것 같다.";

        return "오늘 데이트는 조금 아쉬웠던 것 같다.";
    }

    public void ResetDatingProgress()
    {
        currentDateIndex = 1;
        totalDateCount = 0;
        datingEnded = false;

        affectionByCharacter.Clear();
        EnsureAffectionKeys();
    }

    private void AddAffection(DatingCharacter character, int delta)
    {
        if (character == DatingCharacter.None)
            return;

        EnsureAffectionKeys();
        affectionByCharacter[character] += delta;
    }

    private void EnsureAffectionKeys()
    {
        EnsureAffectionKey(DatingCharacter.ChildhoodFriend);
        EnsureAffectionKey(DatingCharacter.HonorStudent);
        EnsureAffectionKey(DatingCharacter.Tsundere);
    }

    private void EnsureAffectionKey(DatingCharacter character)
    {
        if (!affectionByCharacter.ContainsKey(character))
        {
            affectionByCharacter.Add(character, 0);
        }
    }
    public DialogueData GetCurrentDialogue()
    {
        if (routeDatabase == null)
            return null;

        return routeDatabase.GetDialogue(
            selectedGirlfriend,
            currentDateIndex);
    }
}