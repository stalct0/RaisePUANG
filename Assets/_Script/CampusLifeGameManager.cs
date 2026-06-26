using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CampusLifeGameManager : MonoBehaviour
{
    public static CampusLifeGameManager Instance { get; private set; }

    [Header("Semester Loop")]
    [SerializeField] private int maxSemesters = 8;
    [SerializeField] private int currentSemester = 1;

    [Header("Starting Stats")]
    [SerializeField] private CampusLifeStats startingStats = new CampusLifeStats();

    [Header("Runtime Stats")]
    [SerializeField] private CampusLifeStats currentStats = new CampusLifeStats();
    
    [Header("Ending Rules")]
    [SerializeField] private List<EndingDefinition> endingDefinitions = new List<EndingDefinition>();

    private readonly List<SemesterReport> semesterReports = new List<SemesterReport>();
    private readonly HashSet<string> completedSemesterActivities =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private bool hasFinishedRun;
    private string lastSummary = string.Empty;

    public event Action StateChanged;

    public CampusLifeStats Stats => currentStats;
    public int CurrentSemester => currentSemester;
    public int MaxSemesters => maxSemesters;
    public bool HasFinishedRun => hasFinishedRun;
    public string LastSummary => lastSummary;
    public IReadOnlyList<SemesterReport> SemesterReports => semesterReports;
    public EndingDefinition CurrentEndingPreview => EvaluateEnding(currentStats);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureData();
        BuildDefaultEndingsIfNeeded();
        ResetRun();
    }

    public void ResetRun()
    {
        EnsureData();
        BuildDefaultEndingsIfNeeded();

        currentSemester = 1;
        currentStats = startingStats.Clone();
        currentStats.Clamp();
        semesterReports.Clear();
        completedSemesterActivities.Clear();
        hasFinishedRun = false;
        lastSummary = "Semester 1 has started. Other minigames can now push stat changes into this manager.";

        NotifyStateChanged();
    }

    public bool CanStartSemesterActivity(string activityId, out string failureReason)
    {
        if (hasFinishedRun)
        {
            failureReason = "The 8-semester run is already over.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(activityId))
        {
            failureReason = "Invalid semester activity id.";
            return false;
        }

        if (completedSemesterActivities.Contains(activityId))
        {
            failureReason = "This activity is already completed for the current semester.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public bool CanApplyActivity(CampusLifeStatDelta delta, out string failureReason)
    {
        if (currentStats.money + delta.money < 0)
        {
            failureReason = "Not enough money.";
            return false;
        }

        if (currentStats.condition + delta.condition < 0)
        {
            failureReason = "Not enough condition.";
            return false;
        }

        if (currentStats.grades + delta.grades < 0)
        {
            failureReason = "Grades cannot go below zero.";
            return false;
        }

        if (currentStats.relationship + delta.relationship < 0)
        {
            failureReason = "Relationship cannot go below zero.";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public bool TryApplyActivityResult(string activityName, CampusLifeStatDelta delta)
    {
        return TryApplyActivityResult(activityName, delta, string.Empty, string.Empty);
    }

    public bool TryApplyActivityResult(
        string activityName,
        CampusLifeStatDelta delta,
        string detailSummary,
        string semesterActivityId)
    {
        if (hasFinishedRun)
        {
            lastSummary = "The 8-semester run is already over. Restart the run before applying more actions.";
            NotifyStateChanged();
            return false;
        }

        if (!string.IsNullOrWhiteSpace(semesterActivityId) &&
            !CanStartSemesterActivity(semesterActivityId, out string activityFailureReason))
        {
            lastSummary = $"{activityName} is unavailable. {activityFailureReason}";
            NotifyStateChanged();
            return false;
        }

        if (!CanApplyActivity(delta, out string failureReason))
        {
            lastSummary = $"{activityName} is unavailable. {failureReason}";
            NotifyStateChanged();
            return false;
        }

        ApplyDelta(delta);

        if (!string.IsNullOrWhiteSpace(semesterActivityId))
        {
            completedSemesterActivities.Add(semesterActivityId);
        }

        lastSummary = BuildActivitySummary(activityName, delta, detailSummary);
        NotifyStateChanged();
        return true;
    }

    public void EndCurrentSemester()
    {
        if (hasFinishedRun)
        {
            ResetRun();
            return;
        }

        string summary = $"Semester {currentSemester} is now closed.";


        EndingDefinition previewEnding = EvaluateEnding(currentStats);
        summary = $"{summary}\nEnding hint: {previewEnding.displayName} - {previewEnding.description}";

        semesterReports.Add(new SemesterReport
        {
            semesterNumber = currentSemester,
            summary = summary
        });

        if (currentSemester >= maxSemesters)
        {
            hasFinishedRun = true;
            EndingDefinition finalEnding = EvaluateEnding(currentStats);
            lastSummary = $"{summary}\nFinal ending: {finalEnding.displayName}\n{finalEnding.description}";
            NotifyStateChanged();
            return;
        }

        currentSemester++;
        completedSemesterActivities.Clear();
        lastSummary = $"{summary}\nSemester {currentSemester} begins.";
        NotifyStateChanged();
    }

    public bool IsFinalSemester()
    {
        return currentSemester >= maxSemesters;
    }

    private void ApplyDelta(CampusLifeStatDelta delta)
    {
        currentStats.Apply(delta);
        currentStats.Clamp();
    }

    
    private string BuildActivitySummary(string activityName, CampusLifeStatDelta delta, string detailSummary)
    {
        string deltaSummary = BuildDeltaSummary(delta);
        if (string.IsNullOrWhiteSpace(detailSummary))
        {
            return $"{activityName} applied.\n{deltaSummary}";
        }

        if (delta.IsZero)
        {
            return $"{activityName}\n{detailSummary}";
        }

        return $"{activityName}\n{detailSummary}\n{deltaSummary}";
    }

    private string BuildDeltaSummary(CampusLifeStatDelta delta)
    {
        List<string> fragments = new List<string>();

        AppendDeltaFragment(fragments, "Money", delta.money);
        AppendDeltaFragment(fragments, "Condition", delta.condition);
        AppendDeltaFragment(fragments, "Grades", delta.grades);
        AppendDeltaFragment(fragments, "Relationship", delta.relationship);

        if (fragments.Count == 0)
        {
            fragments.Add("No stat changes.");
        }

        return string.Join(", ", fragments);
    }

    private static void AppendDeltaFragment(List<string> fragments, string label, int value)
    {
        if (value == 0)
        {
            return;
        }

        string sign = value > 0 ? "+" : string.Empty;
        fragments.Add($"{label} {sign}{value}");
    }

    private EndingDefinition EvaluateEnding(CampusLifeStats stats)
    {
        EndingDefinition bestMatch = null;

        for (int i = 0; i < endingDefinitions.Count; i++)
        {
            EndingDefinition candidate = endingDefinitions[i];
            if (!candidate.Matches(stats))
            {
                continue;
            }

            if (bestMatch == null || candidate.priority > bestMatch.priority)
            {
                bestMatch = candidate;
            }
        }

        if (bestMatch != null)
        {
            return bestMatch;
        }

        return endingDefinitions[endingDefinitions.Count - 1];
    }

    private void BuildDefaultEndingsIfNeeded()
    {
        if (endingDefinitions.Count > 0)
        {
            return;
        }

        endingDefinitions = new List<EndingDefinition>
        {
            new EndingDefinition
            {
                id = "burnout",
                displayName = "Burnout",
                description = "Stress wins the final race and graduation ends in survival mode.",
                priority = 10,
            },
            new EndingDefinition
            {
                id = "hynix_job",
                displayName = "Hynix Job",
                description = "Strong grades and stable self-management open the door to a dream offer.",
                priority = 8,
                minimumMoney = 35,
                minimumCondition = 45,
                minimumGrades = 75,
                minimumRelationship = 35,
            },
            new EndingDefinition
            {
                id = "graduate_school",
                displayName = "Graduate Student",
                description = "Top grades pull Puang deeper into the lab and into the next academic chapter.",
                priority = 7,
                minimumMoney = 10,
                minimumCondition = 30,
                minimumGrades = 85,
                minimumRelationship = 20,
            },
            new EndingDefinition
            {
                id = "campus_connector",
                displayName = "Campus Connector",
                description = "Friendships and campus presence become Puang's biggest long-term asset.",
                priority = 6,
                minimumMoney = 15,
                minimumCondition = 35,
                minimumGrades = 40,
                minimumRelationship = 80,
            },
            new EndingDefinition
            {
                id = "steady_graduate",
                displayName = "Steady Graduate",
                description = "Puang clears all eight semesters and graduates with a workable balance.",
                priority = 0,
                alwaysAvailable = true
            }
        };
    }

    private void EnsureData()
    {
        if (startingStats == null)
        {
            startingStats = new CampusLifeStats();
        }

        if (currentStats == null)
        {
            currentStats = new CampusLifeStats();
        }

        startingStats.Clamp();
        currentStats.Clamp();
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke();
    }
}
