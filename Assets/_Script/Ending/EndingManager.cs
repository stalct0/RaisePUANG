using UnityEngine;

public class EndingManager : MonoBehaviour
{
    public static EndingManager Instance { get; private set; }

    [Header("Database")]
    [SerializeField] private EndingDatabase endingDatabase;

    [Header("UI")]
    [SerializeField] private EndingUI endingUI;

    [Header("Hidden Ending")]
    [SerializeField] private float drunkDriverWindow = 3f;

    private float lastDrinkVisitTime = -999f;
    private bool endingTriggered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (endingUI == null)
            endingUI = FindFirstObjectByType<EndingUI>();
    }

    public void NotifyZoneEntered(ZoneType zone)
    {
        if (endingTriggered)
            return;

        if (zone == ZoneType.Drink)
        {
            lastDrinkVisitTime = Time.time;
            return;
        }

        if (zone == ZoneType.Work)
        {
            if (Time.time - lastDrinkVisitTime <= drunkDriverWindow)
            {
                TriggerEnding("hidden_drunk_driver");
            }
        }
    }

    public void TriggerTrueLoveEnding()
    {
        TriggerEnding("hidden_true_love");
    }

    public void TriggerNormalEnding()
    {
        string endingId = DecideNormalEndingId();
        TriggerEnding(endingId);
    }

    private void TriggerEnding(string endingId)
    {
        if (endingTriggered)
            return;

        endingTriggered = true;
        EndingCollectionManager.UnlockEnding(endingId);
        if (CampusLifeGameManager.Instance != null)
            CampusLifeGameManager.Instance.SetFinishedByEnding();

        EndingData data = endingDatabase != null
            ? endingDatabase.GetEnding(endingId)
            : null;

        if (endingUI != null)
            endingUI.ShowEnding(data);
    }

    private string DecideNormalEndingId()
    {
        int classroom = GetZoneLevel(ZoneType.Classroom);
        int drink = GetZoneLevel(ZoneType.Drink);
        int team = GetZoneLevel(ZoneType.TeamProjectRoom);
        int work = GetZoneLevel(ZoneType.Work);

        if (team >= 3)
            return "normal_bus_driver";

        if (work >= 3 && classroom >= 3)
            return "normal_entrepreneur_ceo";

        if (work >= 3 && classroom < 3)
            return "normal_road_man";

        if (classroom >= 3 && drink >= 3)
            return "normal_top_graduate";

        if (classroom >= 3 && drink < 3)
            return "normal_perfect_attendance";

        if (drink >= 3 && classroom < 3)
            return "normal_social_butterfly";

        return DecideStatEndingId();
    }

    private string DecideStatEndingId()
    {
        CampusLifeStats stats = CampusLifeGameManager.Instance.Stats;

        int best = stats.money;
        string result = "normal_rich_student";

        if (stats.condition > best)
        {
            best = stats.condition;
            result = "normal_happy_student";
        }

        if (stats.grades > best)
        {
            best = stats.grades;
            result = "normal_status_downfall";
        }

        if (stats.relationship > best)
        {
            result = "normal_bonds";
        }

        return result;
    }

    private int GetZoneLevel(ZoneType zone)
    {
        ZoneSpriteSwitcher[] switchers = FindObjectsOfType<ZoneSpriteSwitcher>();

        foreach (ZoneSpriteSwitcher switcher in switchers)
        {
            if (switcher != null && switcher.ZoneType == zone)
                return switcher.CurrentLevel;
        }

        return 1;
    }
}