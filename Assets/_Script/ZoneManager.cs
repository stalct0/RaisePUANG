using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    public static ZoneManager Instance { get; private set; }

    public ZoneType CurrentZone { get; private set; } = ZoneType.None;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void EnterZone(ZoneType zone)
    {
        CurrentZone = zone;
        Debug.Log($"Enter : {zone}");
    }

    public void ExitZone(ZoneType zone)
    {
        if (CurrentZone != zone)
            return;

        CurrentZone = ZoneType.None;
        Debug.Log("Exit Zone");
    }
}