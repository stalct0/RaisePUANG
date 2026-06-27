using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }

    [Header("MiniGame")]
    [SerializeField] private DriveMiniGameController driveMiniGameController;

    public ZoneType CurrentZone { get; private set; } = ZoneType.None;

    private bool workPopupShownThisEnter;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (driveMiniGameController == null)
            driveMiniGameController = FindFirstObjectByType<DriveMiniGameController>();
    }

    public void EnterZone(ZoneType zone)
    {
        CurrentZone = zone;
        Debug.Log($"Enter : {zone}");

        if (zone == ZoneType.Work)
        {
            TryOpenWorkIntro();
        }
        if (EndingManager.Instance != null)
        {
            EndingManager.Instance.NotifyZoneEntered(zone);
        }
    }

    public void ExitZone(ZoneType zone)
    {
        if (CurrentZone != zone)
            return;

        if (zone == ZoneType.Work)
        {
            workPopupShownThisEnter = false;
        }

        CurrentZone = ZoneType.None;
        Debug.Log("Exit Zone");
    }

    private void TryOpenWorkIntro()
    {
        if (workPopupShownThisEnter)
            return;

        workPopupShownThisEnter = true;

        if (driveMiniGameController != null)
        {
            driveMiniGameController.OpenMiniGame();
        }
    }
}