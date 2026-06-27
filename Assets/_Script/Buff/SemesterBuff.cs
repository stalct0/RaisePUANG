using UnityEngine;

public enum BuffGrade
{
    Bad,
    MidDebuff,
    MidBuff,
    Good
}

public enum BuffTargetZone
{
    All,
    Classroom,
    Drink,
    TeamProjectRoom
}

public enum BuffTargetStat
{
    All,
    Money,
    Condition,
    Grades,
    Relationship
}

public enum BuffEffectType
{
    ZoneStatMultiplier,
    AllZoneStatMultiplier,
    InstantStatGain,
    PermanentZoneLevelUp,
    MoveSpeedTemporary
}

[CreateAssetMenu(fileName = "NewSemesterBuff", menuName = "Semester/Buff")]
public class SemesterBuff : ScriptableObject
{
    public string title;

    [TextArea(2, 4)]
    public string description;

    public BuffGrade grade;
    public BuffEffectType effectType;

    public BuffTargetZone targetZone;
    public BuffTargetStat targetStat;

    public float multiplier = 1f;
    public int instantAmount;
    public float duration;
}