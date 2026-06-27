using UnityEngine;

public class WorkZoneProgressManager : MonoBehaviour
{
    public static WorkZoneProgressManager Instance { get; private set; }

    [Header("Level Up Rule")]
    [SerializeField] private int successRequiredPerLevel = 3;

    [Header("Target Zone")]
    [SerializeField] private ZoneSpriteSwitcher workZoneSwitcher;

    private int currentSuccessCount;

    public int CurrentSuccessCount => currentSuccessCount;
    public int SuccessRequiredPerLevel => successRequiredPerLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (workZoneSwitcher == null)
            FindWorkZoneSwitcher();
    }

    public void RegisterDeliverySuccess()
    {
        if (workZoneSwitcher == null)
            FindWorkZoneSwitcher();

        if (workZoneSwitcher == null)
        {
            Debug.LogError("[WorkZoneProgressManager] Work ZoneSpriteSwitcher를 찾지 못했습니다.");
            return;
        }

        if (workZoneSwitcher.CurrentLevel >= 3)
            return;

        currentSuccessCount++;

        Debug.Log($"[WorkZoneProgressManager] 배달 성공 {currentSuccessCount}/{successRequiredPerLevel}");

        if (currentSuccessCount >= successRequiredPerLevel)
        {
            currentSuccessCount = 0;
            workZoneSwitcher.ForceLevelUp();

            Debug.Log($"[WorkZoneProgressManager] 알바 구역 레벨업. 현재 Lv.{workZoneSwitcher.CurrentLevel}");
        }
    }

    public void ResetProgress()
    {
        currentSuccessCount = 0;
    }

    private void FindWorkZoneSwitcher()
    {
        ZoneSpriteSwitcher[] switchers = FindObjectsOfType<ZoneSpriteSwitcher>();

        foreach (ZoneSpriteSwitcher switcher in switchers)
        {
            if (switcher != null && switcher.ZoneType == ZoneType.Work)
            {
                workZoneSwitcher = switcher;
                return;
            }
        }
    }
}