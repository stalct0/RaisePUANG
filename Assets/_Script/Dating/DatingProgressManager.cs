using System.Collections.Generic;
using UnityEngine;

public sealed class DatingProgressManager : MonoBehaviour
{
    public static DatingProgressManager Instance { get; private set; }

    private const int MaxDateIndex = 3;

    [Header("Route")]
    [SerializeField] private DatingRouteDatabase routeDatabase;
    [SerializeField] private DatingCharacter selectedGirlfriend = DatingCharacter.ChildhoodFriend;
    [SerializeField] private int currentDateIndex = 1;

    [Header("Progress")]
    [SerializeField] private int totalDateCount;
    [SerializeField] private int currentSemesterDateCount;
    [SerializeField] private int previousSemesterDateCount;
    [SerializeField] private bool datingEnded;

    [Header("First Romance Event")]
    [SerializeField] private bool firstRomanceEventCompleted;
    [SerializeField] private bool romanceSystemLocked;

    [Header("First Meeting")]
    [SerializeField] private DialogueData firstMeetingDialogue;

    private readonly Dictionary<DatingCharacter, int> affectionByCharacter = new();

    public DialogueData FirstMeetingDialogue => firstMeetingDialogue;

    public DatingCharacter SelectedGirlfriend => selectedGirlfriend;
    public int CurrentDateIndex => currentDateIndex;
    public int TotalDateCount => totalDateCount;
    public int CurrentSemesterDateCount => currentSemesterDateCount;
    public int PreviousSemesterDateCount => previousSemesterDateCount;

    public bool DatingEnded => datingEnded;
    public bool FirstRomanceEventCompleted => firstRomanceEventCompleted;
    public bool RomanceSystemLocked => romanceSystemLocked;

    public int CompletedRegularDateCount => Mathf.Max(0, currentDateIndex - 1);

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

    public DialogueData GetCurrentDialogue()
    {
        if (routeDatabase == null)
            return null;

        return routeDatabase.GetDialogue(selectedGirlfriend, currentDateIndex);
    }

    public void SetGirlfriend(DatingCharacter character)
    {
        if (character == DatingCharacter.None)
            return;

        selectedGirlfriend = character;
    }

    public void CompleteFirstRomanceEvent(DatingCharacter girlfriend)
    {
        if (firstRomanceEventCompleted)
            return;

        if (girlfriend == DatingCharacter.None)
            girlfriend = DatingCharacter.ChildhoodFriend;

        selectedGirlfriend = girlfriend;
        firstRomanceEventCompleted = true;
        romanceSystemLocked = false;
    }

    public void LockRomanceSystem()
    {
        if (firstRomanceEventCompleted)
            return;

        romanceSystemLocked = true;
    }

    public void CompleteDate(int affectionDelta)
    {
        if (datingEnded)
            return;

        AddAffection(selectedGirlfriend, affectionDelta);

        totalDateCount++;
        currentSemesterDateCount++;
        currentDateIndex++;

        if (currentDateIndex > MaxDateIndex)
        {
            datingEnded = true;

            if (EndingManager.Instance != null)
                EndingManager.Instance.TriggerTrueLoveEnding();
        }
    }

    public void StartNextSemester()
    {
        previousSemesterDateCount = currentSemesterDateCount;
        currentSemesterDateCount = 0;
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
        currentSemesterDateCount = 0;
        previousSemesterDateCount = 0;
        datingEnded = false;
        firstRomanceEventCompleted = false;
        romanceSystemLocked = false;

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
            affectionByCharacter.Add(character, 0);
    }
}