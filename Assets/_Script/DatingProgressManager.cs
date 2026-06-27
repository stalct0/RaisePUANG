using System.Collections.Generic;
using UnityEngine;

public sealed class DatingProgressManager : MonoBehaviour
{
    public static DatingProgressManager Instance { get; private set; }

    private const int TheoreticalMaxDatesPerSemester = 14;
    private const int GameEndingDateCount = 10;
    private const int ReducedNextSemesterDateLimit = 1;
    private const int PreviousSemesterPenaltyThreshold = 2;

    [Header("--- Dating Progress ---")]
    [SerializeField] private int currentSemester = 1;
    [SerializeField] private int totalDateCount;
    [SerializeField] private int currentSemesterDateCount;
    [SerializeField] private int previousSemesterDateCount;
    [SerializeField] private bool gameEnded;

    private readonly Dictionary<DatingCharacter, int> affectionByCharacter = new Dictionary<DatingCharacter, int>();
    private DatingCharacter lastMeetingSelection = DatingCharacter.None;

    public int CurrentSemester => currentSemester;
    public int TotalDateCount => totalDateCount;
    public int CurrentSemesterDateCount => currentSemesterDateCount;
    public int CurrentSemesterDateLimit => previousSemesterDateCount >= PreviousSemesterPenaltyThreshold
        ? ReducedNextSemesterDateLimit
        : TheoreticalMaxDatesPerSemester;
    public bool GameEnded => gameEnded;
    public DatingCharacter LastMeetingSelection => lastMeetingSelection;

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
        if (gameEnded)
        {
            reason = "이미 데이트 엔딩 조건에 도달했다.";
            return false;
        }

        if (totalDateCount >= GameEndingDateCount)
        {
            reason = "데이트를 10번 진행하여 게임이 종료된다.";
            return false;
        }

        if (currentSemesterDateCount >= CurrentSemesterDateLimit)
        {
            reason = "이번 학기에 더 이상 데이트할 수 없다.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public void CompleteDate(DatingCharacter character, DatingLocation location, int affectionDelta)
    {
        if (gameEnded) return;

        int finalDelta = affectionDelta + GetLocationPreferenceBonus(character, location);
        AddAffection(character, finalDelta);

        currentSemesterDateCount++;
        totalDateCount++;

        if (totalDateCount >= GameEndingDateCount)
        {
            gameEnded = true;
        }
    }

    public void RegisterMeetingAfterSelection(DatingCharacter character)
    {
        lastMeetingSelection = character;
    }

    public void StartNextSemester()
    {
        previousSemesterDateCount = currentSemesterDateCount;
        currentSemesterDateCount = 0;
        currentSemester++;
    }

    public string GetDateFeedback(DatingCharacter character)
    {
        int affection = GetAffection(character);
        if (affection >= 6) return "오늘 데이트를 마음에 들어한 것 같다.";
        if (affection <= -3) return "오늘 데이트는 조금 아쉬웠던 것 같다.";
        return "오늘 데이트는 무난하게 지나간 것 같다.";
    }

    public int GetLocationPreferenceBonus(DatingCharacter character, DatingLocation location)
    {
        if (location == DatingLocation.HanRiver) return 0;

        switch (character)
        {
            case DatingCharacter.ChildhoodFriend:
                return IsAny(location, DatingLocation.MovieTheater, DatingLocation.AmusementPark, DatingLocation.Aquarium) ? 2 : -1;
            case DatingCharacter.HonorStudent:
                return IsAny(location, DatingLocation.BlueDragonBath, DatingLocation.BbaekGwang, DatingLocation.Building310Rooftop) ? 2 : -1;
            case DatingCharacter.Tsundere:
                return IsAny(location, DatingLocation.CatCafe, DatingLocation.HomeDate, DatingLocation.NamsanTower) ? 2 : -1;
            default:
                return 0;
        }
    }

    private void AddAffection(DatingCharacter character, int delta)
    {
        if (character == DatingCharacter.None) return;
        EnsureAffectionKeys();
        affectionByCharacter[character] += delta;
    }

    private int GetAffection(DatingCharacter character)
    {
        EnsureAffectionKeys();
        return affectionByCharacter.TryGetValue(character, out int affection) ? affection : 0;
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

    private static bool IsAny(DatingLocation location, params DatingLocation[] candidates)
    {
        for (int i = 0; i < candidates.Length; i++)
        {
            if (location == candidates[i]) return true;
        }

        return false;
    }
}
