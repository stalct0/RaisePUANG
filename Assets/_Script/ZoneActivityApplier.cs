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
    private ZoneSpriteSwitcher[] zoneSpriteSwitchers;

    private void Awake()
    {
        RefreshZoneSpriteSwitchers();
    }

    private void Update()
    {
        if (ZoneManager.Instance == null)
            return;

        ZoneType currentZone = ZoneManager.Instance.CurrentZone;

        UpdatePlayerAnimation(currentZone);
        ApplyCurrentZoneActivityOverTime(currentZone);
    }

    private void RefreshZoneSpriteSwitchers()
    {
        zoneSpriteSwitchers = FindObjectsOfType<ZoneSpriteSwitcher>();
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

        bool success = false;

        switch (zone)
        {
            case ZoneType.Drink:
                success = SetPlayerAnimation(drinkController, "drink");
                break;

            case ZoneType.Classroom:
                success = SetPlayerAnimation(lectureController, "lecture");
                break;

            case ZoneType.TeamProjectRoom:
                success = SetPlayerAnimation(teamProjectController, "teamproj");
                break;

            default:
                success = SetPlayerAnimation(idleController, "idle1");
                break;
        }

        if (success)
            appliedAnimationZone = zone;
    }

    private bool SetPlayerAnimation(RuntimeAnimatorController controller, string stateName)
    {
        if (playerAnimator == null)
        {
            Debug.LogError($"[ZoneActivityApplier] {gameObject.name}: Player Animator is not assigned.", this);
            return false;
        }

        if (controller == null)
        {
            Debug.LogError($"[ZoneActivityApplier] {gameObject.name}: Animator controller for '{stateName}' is not assigned.", this);
            return false;
        }

        if (playerAnimator.runtimeAnimatorController != controller)
            playerAnimator.runtimeAnimatorController = controller;

        int stateHash = Animator.StringToHash(stateName);

        if (!playerAnimator.HasState(0, stateHash))
        {
            Debug.LogError($"[ZoneActivityApplier] Controller '{controller.name}' does not have state '{stateName}'.", this);
            return false;
        }

        playerAnimator.Play(stateHash, 0, 0f);
        playerAnimator.Update(0f);
        return true;
    }

    private CampusLifeStatDelta GetDelta(ZoneType zone)
    {
        int level = GetZoneLevel(zone);

        switch (zone)
        {
            case ZoneType.Drink:
                return GetDrinkDelta(level);

            case ZoneType.Classroom:
                return GetLectureDelta(level);

            case ZoneType.TeamProjectRoom:
                return GetTeamProjectDelta(level);

            default:
                return new CampusLifeStatDelta();
        }
    }

    private int GetZoneLevel(ZoneType zone)
    {
        if (zone == ZoneType.None)
            return 1;

        if (zoneSpriteSwitchers == null || zoneSpriteSwitchers.Length == 0)
            RefreshZoneSpriteSwitchers();

        if (zoneSpriteSwitchers == null)
            return 1;

        foreach (ZoneSpriteSwitcher switcher in zoneSpriteSwitchers)
        {
            if (switcher == null)
                continue;

            if (switcher.ZoneType == zone)
                return Mathf.Clamp(switcher.CurrentLevel, 1, 3);
        }

        return 1;
    }

    private CampusLifeStatDelta GetDrinkDelta(int level)
    {
        switch (level)
        {
            case 1:
                return new CampusLifeStatDelta
                {
                    condition = 5,
                    relationship = 5,
                    money = -10
                };

            case 2:
                return new CampusLifeStatDelta
                {
                    condition = 10,
                    relationship = 10,
                    money = -15
                };

            case 3:
                return new CampusLifeStatDelta
                {
                    condition = 15,
                    relationship = 15,
                    money = -15
                };

            default:
                return new CampusLifeStatDelta();
        }
    }

    private CampusLifeStatDelta GetLectureDelta(int level)
    {
        switch (level)
        {
            case 1:
                return new CampusLifeStatDelta
                {
                    grades = 5,
                    condition = -5
                };

            case 2:
                return new CampusLifeStatDelta
                {
                    grades = 10,
                    condition = -5
                };

            case 3:
                return new CampusLifeStatDelta
                {
                    grades = 20,
                    condition = -10
                };

            default:
                return new CampusLifeStatDelta();
        }
    }

    private CampusLifeStatDelta GetTeamProjectDelta(int level)
    {
        switch (level)
        {
            case 1:
                return new CampusLifeStatDelta
                {
                    grades = 5,
                    relationship = -5
                };

            case 2:
                return new CampusLifeStatDelta
                {
                    grades = 10,
                    relationship = -5
                };

            case 3:
                return new CampusLifeStatDelta
                {
                    grades = 15,
                    relationship = -10
                };

            default:
                return new CampusLifeStatDelta();
        }
    }

    private string GetActivityName(ZoneType zone)
    {
        int level = GetZoneLevel(zone);

        switch (zone)
        {
            case ZoneType.Classroom:
                return $"Lecture Lv.{level}";

            case ZoneType.Drink:
                return $"Drink Lv.{level}";

            case ZoneType.TeamProjectRoom:
                return $"Team Project Lv.{level}";

            default:
                return "Idle";
        }
    }
}
