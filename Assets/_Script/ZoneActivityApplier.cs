using UnityEngine;
using UnityEngine.InputSystem;

public class ZoneActivityApplier : MonoBehaviour
{
    private CampusLifeGameManager gameManager;

    private void Start()
    {
        gameManager = CampusLifeGameManager.Instance;
    }

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
        if (gameManager == null)
        {
            gameManager = CampusLifeGameManager.Instance;
        }

        if (gameManager == null || ZoneManager.Instance == null)
            return;

        ZoneType currentZone = ZoneManager.Instance.CurrentZone;

        CampusLifeStatDelta delta = GetDeltaByZone(currentZone);

        if (delta.IsZero)
        {
            Debug.Log("적용할 행동이 없는 구역입니다.");
            return;
        }

        gameManager.TryApplyActivityResult(currentZone.ToString(), delta);
    }

    private CampusLifeStatDelta GetDeltaByZone(ZoneType zone)
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
}