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

    [Header("Animation State Names")]
    [SerializeField] private string idleStateName = "idle1";
    [SerializeField] private string drinkStateName = "drink";
    [SerializeField] private string lectureStateName = "lecture";
    [SerializeField] private string teamProjectStateName = "teamproj";

    [Header("Blocked Interaction")]
    [Tooltip("스테이터스가 부족해서 zone 상호작용을 못할 때 재생할 idle 계열 state 이름. 실제 Animator state가 init1이면 여기에 init1을 넣으세요.")]
    [SerializeField] private string blockedStateName = "idle1";

    private float statTickTimer;
    private ZoneType appliedAnimationZone = ZoneType.None;
    private bool appliedBlockedAnimation;
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
        CampusLifeStatDelta delta = GetDelta(currentZone);
        bool canInteract = CanInteractWithZone(delta);

        SetZoneInteractionAllowed(currentZone, canInteract);
        UpdatePlayerAnimation(currentZone, canInteract);
        ApplyCurrentZoneActivityOverTime(currentZone, delta, canInteract);
    }

    private void RefreshZoneSpriteSwitchers()
    {
        zoneSpriteSwitchers = FindObjectsOfType<ZoneSpriteSwitcher>();
    }

    private bool CanInteractWithZone(CampusLifeStatDelta delta)
    {
        if (delta.IsZero)
            return true;

        if (CampusLifeGameManager.Instance == null)
            return true;

        return CampusLifeGameManager.Instance.CanApplyDelta(delta, out _);
    }

    private void SetZoneInteractionAllowed(ZoneType zone, bool allowed)
    {
        ZoneSpriteSwitcher switcher = GetZoneSpriteSwitcher(zone);

        if (switcher != null)
            switcher.SetInteractionAllowed(allowed);
    }

    private ZoneSpriteSwitcher GetZoneSpriteSwitcher(ZoneType zone)
    {
        if (zone == ZoneType.None)
            return null;

        if (zoneSpriteSwitchers == null || zoneSpriteSwitchers.Length == 0)
            RefreshZoneSpriteSwitchers();

        if (zoneSpriteSwitchers == null)
            return null;

        foreach (ZoneSpriteSwitcher switcher in zoneSpriteSwitchers)
        {
            if (switcher == null)
                continue;

            if (switcher.ZoneType == zone)
                return switcher;
        }

        return null;
    }

    private void ApplyCurrentZoneActivityOverTime(ZoneType zone, CampusLifeStatDelta delta, bool canInteract)
    {
        if (appliedStatZone != zone)
        {
            appliedStatZone = zone;
            statTickTimer = 0f;
        }

        if (delta.IsZero || !canInteract)
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

    private void UpdatePlayerAnimation(ZoneType zone, bool canInteract)
    {
        bool shouldUseBlockedAnimation = IsActivityZone(zone) && !canInteract;

        if (appliedAnimationZone == zone && appliedBlockedAnimation == shouldUseBlockedAnimation)
            return;

        bool success;

        if (shouldUseBlockedAnimation)
        {
            success = SetPlayerAnimation(idleController, blockedStateName);
        }
        else
        {
            switch (zone)
            {
                case ZoneType.Drink:
                    success = SetPlayerAnimation(drinkController, drinkStateName);
                    break;

                case ZoneType.Classroom:
                    success = SetPlayerAnimation(lectureController, lectureStateName);
                    break;

                case ZoneType.TeamProjectRoom:
                    success = SetPlayerAnimation(teamProjectController, teamProjectStateName);
                    break;

                default:
                    success = SetPlayerAnimation(idleController, idleStateName);
                    break;
            }
        }

        if (success)
        {
            appliedAnimationZone = zone;
            appliedBlockedAnimation = shouldUseBlockedAnimation;
        }
    }

    private bool IsActivityZone(ZoneType zone)
    {
        return zone == ZoneType.Drink ||
               zone == ZoneType.Classroom ||
               zone == ZoneType.TeamProjectRoom;
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
        ZoneSpriteSwitcher switcher = GetZoneSpriteSwitcher(zone);

        if (switcher == null)
            return 1;

        return Mathf.Clamp(switcher.CurrentLevel, 1, 3);
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
