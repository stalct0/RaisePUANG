using UnityEngine;
using UnityEngine.InputSystem;

public class ZoneActivityApplier : MonoBehaviour
{
    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            ApplyCurrentZoneActivity();
        }
    }

    private void ApplyCurrentZoneActivity()
    {
        if (CampusLifeGameManager.Instance == null) return;
        if (ZoneManager.Instance == null) return;

        ZoneType zone = ZoneManager.Instance.CurrentZone;

        CampusLifeStatDelta delta = GetDelta(zone);

        if (delta.IsZero) return;

        string activityName = GetActivityName(zone);

        CampusLifeGameManager.Instance.TryApplyActivity(activityName, delta);
    }

    private CampusLifeStatDelta GetDelta(ZoneType zone)
    {
        switch (zone)
        {
            case ZoneType.Classroom:
                return new CampusLifeStatDelta
                {
                    money = 5,
                    condition = -5
                };

            case ZoneType.Drink:
                return new CampusLifeStatDelta
                {
                    condition = 10,
                    relationship = 10,
                    money = -5
                };

            case ZoneType.TeamProjectRoom:
                return new CampusLifeStatDelta
                {
                    grades = 10,
                    relationship = -5
                };

            default:
                return new CampusLifeStatDelta();
        }
    }

    private string GetActivityName(ZoneType zone)
    {
        switch (zone)
        {
            case ZoneType.Classroom:
                return "수업 듣기";

            case ZoneType.Drink:
                return "술자리 가기";

            case ZoneType.TeamProjectRoom:
                return "팀플하기";

            default:
                return "아무것도 안 하기";
        }
    }
}