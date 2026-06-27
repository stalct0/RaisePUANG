using UnityEngine;

public class ZoneActivityApplier : MonoBehaviour
{
    [Header("Stat Tick")]
    [SerializeField] private float statTickInterval = 1f;

    [Header("Player Animation")]
    [SerializeField] private Animator playerAnimator = null;
    [SerializeField] private RuntimeAnimatorController idleController = null;
    [SerializeField] private RuntimeAnimatorController drinkController = null;
    [SerializeField] private RuntimeAnimatorController lectureController = null;
    [SerializeField] private RuntimeAnimatorController teamProjectController = null;

    private float statTickTimer;
    private ZoneType appliedAnimationZone = ZoneType.None;
    private ZoneType appliedStatZone = ZoneType.None;

    private void Update()
    {
        if (ZoneManager.Instance == null)
            return;

        ZoneType currentZone = ZoneManager.Instance.CurrentZone;

        UpdatePlayerAnimation(currentZone);
        ApplyCurrentZoneActivityOverTime(currentZone);
    }

    private void ApplyCurrentZoneActivityOverTime(ZoneType zone)
    {
        if (appliedStatZone != zone)
        {
            appliedStatZone = zone;
            statTickTimer = 0f;
        }

        CampusLifeStatDelta delta = GetDelta(zone);

        if (delta.IsZero)
        {
            statTickTimer = 0f;
            return;
        }

        statTickTimer += Time.deltaTime;

        if (statTickTimer < statTickInterval)
            return;

        statTickTimer -= statTickInterval;

        if (CampusLifeGameManager.Instance == null)
            return;

        CampusLifeGameManager.Instance.TryApplyContinuousActivity(GetActivityName(zone), delta);
    }

    private void UpdatePlayerAnimation(ZoneType zone)
    {
        if (appliedAnimationZone == zone)
            return;

        appliedAnimationZone = zone;

        switch (zone)
        {
            case ZoneType.Drink:
                SetPlayerAnimation(drinkController, "drink");
                break;

            case ZoneType.Classroom:
                SetPlayerAnimation(lectureController, "lecture");
                break;

            case ZoneType.TeamProjectRoom:
                SetPlayerAnimation(teamProjectController, "teamproj");
                break;

            default:
                SetPlayerAnimation(idleController, "idle1");
                break;
        }
    }

    private void SetPlayerAnimation(RuntimeAnimatorController controller, string stateName)
    {
        if (playerAnimator == null)
        {
            Debug.LogError("[ZoneActivityApplier] Player Animator is not assigned.");
            return;
        }

        if (controller == null)
        {
            Debug.LogError($"[ZoneActivityApplier] Animator controller for '{stateName}' is not assigned.");
            return;
        }

        if (playerAnimator.runtimeAnimatorController != controller)
            playerAnimator.runtimeAnimatorController = controller;

        playerAnimator.Rebind();
        playerAnimator.Play(stateName, 0, 0f);
        playerAnimator.Update(0f);
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
                return "Lecture";

            case ZoneType.Drink:
                return "Drink";

            case ZoneType.TeamProjectRoom:
                return "Team Project";

            default:
                return "Idle";
        }
    }
}
