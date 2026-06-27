using System.Collections.Generic;
using UnityEngine;

public class SemesterBuffManager : MonoBehaviour
{
    public static SemesterBuffManager Instance { get; private set; }

    private readonly List<SemesterBuff> activeSemesterBuffs = new();

    [Header("Runtime Move Speed")]
    [SerializeField] private float moveSpeedMultiplier = 1f;
    [SerializeField] private float moveSpeedTimer;

    public float MoveSpeedMultiplier => moveSpeedMultiplier;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        UpdateMoveSpeedBuff();
    }

    public void ClearSemesterBuffs()
    {
        activeSemesterBuffs.Clear();

        moveSpeedMultiplier = 1f;
        moveSpeedTimer = 0f;
    }

    public void ApplyBuff(SemesterBuff buff)
    {
        if (buff == null)
            return;

        switch (buff.effectType)
        {
            case BuffEffectType.ZoneStatMultiplier:
            case BuffEffectType.AllZoneStatMultiplier:
                activeSemesterBuffs.Add(buff);
                break;

            case BuffEffectType.InstantStatGain:
                ApplyInstantStatGain(buff);
                break;

            case BuffEffectType.PermanentZoneLevelUp:
                ApplyPermanentZoneLevelUp(buff);
                break;

            case BuffEffectType.MoveSpeedTemporary:
                ApplyTemporaryMoveSpeed(buff);
                break;
        }
    }

    public CampusLifeStatDelta ApplyZoneBuffs(ZoneType zone, CampusLifeStatDelta baseDelta)
    {
        CampusLifeStatDelta result = baseDelta;

        for (int i = 0; i < activeSemesterBuffs.Count; i++)
        {
            SemesterBuff buff = activeSemesterBuffs[i];

            if (!IsZoneMatched(buff, zone))
                continue;

            result.money = ApplyStatMultiplier(result.money, BuffTargetStat.Money, buff);
            result.condition = ApplyStatMultiplier(result.condition, BuffTargetStat.Condition, buff);
            result.grades = ApplyStatMultiplier(result.grades, BuffTargetStat.Grades, buff);
            result.relationship = ApplyStatMultiplier(result.relationship, BuffTargetStat.Relationship, buff);
        }

        return result;
    }

    private int ApplyStatMultiplier(int value, BuffTargetStat stat, SemesterBuff buff)
    {
        if (value == 0)
            return value;

        if (buff.targetStat != BuffTargetStat.All && buff.targetStat != stat)
            return value;

        return Mathf.RoundToInt(value * buff.multiplier);
    }

    private bool IsZoneMatched(SemesterBuff buff, ZoneType zone)
    {
        if (buff.effectType == BuffEffectType.AllZoneStatMultiplier)
            return true;

        if (buff.targetZone == BuffTargetZone.All)
            return true;

        return ConvertZone(buff.targetZone) == zone;
    }

    private ZoneType ConvertZone(BuffTargetZone zone)
    {
        switch (zone)
        {
            case BuffTargetZone.Classroom:
                return ZoneType.Classroom;

            case BuffTargetZone.Drink:
                return ZoneType.Drink;

            case BuffTargetZone.TeamProjectRoom:
                return ZoneType.TeamProjectRoom;

            default:
                return ZoneType.None;
        }
    }

    private void ApplyInstantStatGain(SemesterBuff buff)
    {
        if (CampusLifeGameManager.Instance == null)
            return;

        CampusLifeStatDelta delta = new CampusLifeStatDelta();

        switch (buff.targetStat)
        {
            case BuffTargetStat.Money:
                delta.money = buff.instantAmount;
                break;

            case BuffTargetStat.Condition:
                delta.condition = buff.instantAmount;
                break;

            case BuffTargetStat.Grades:
                delta.grades = buff.instantAmount;
                break;

            case BuffTargetStat.Relationship:
                delta.relationship = buff.instantAmount;
                break;
        }

        if (!delta.IsZero)
        {
            CampusLifeGameManager.Instance.TryApplyActivity("수강신청 보상", delta);
        }
    }

    private void ApplyPermanentZoneLevelUp(SemesterBuff buff)
    {
        ZoneType targetZone = ConvertZone(buff.targetZone);

        if (targetZone == ZoneType.None)
            return;

        ZoneSpriteSwitcher[] switchers = FindObjectsOfType<ZoneSpriteSwitcher>();

        for (int i = 0; i < switchers.Length; i++)
        {
            if (switchers[i] != null && switchers[i].ZoneType == targetZone)
            {
                switchers[i].ForceLevelUp();
                return;
            }
        }
    }

    private void ApplyTemporaryMoveSpeed(SemesterBuff buff)
    {
        moveSpeedMultiplier = buff.multiplier <= 0f ? 1f : buff.multiplier;
        moveSpeedTimer = buff.duration;
    }

    private void UpdateMoveSpeedBuff()
    {
        if (moveSpeedTimer <= 0f)
            return;

        moveSpeedTimer -= Time.deltaTime;

        if (moveSpeedTimer <= 0f)
        {
            moveSpeedTimer = 0f;
            moveSpeedMultiplier = 1f;
        }
    }
}