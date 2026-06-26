using System;
using UnityEngine;

[Serializable]
public class CampusLifeStats
{
    [Min(0)] public int money = 20;
    [Range(0, 100)] public int condition = 70;
    [Range(0, 100)] public int grades = 30;
    [Range(0, 100)] public int relationship = 45;
    [Range(0, 100)] public int stress = 15;

    public CampusLifeStats Clone()
    {
        return new CampusLifeStats
        {
            money = money,
            condition = condition,
            grades = grades,
            relationship = relationship,
            stress = stress
        };
    }

    public void Apply(CampusLifeStatDelta delta)
    {
        money += delta.money;
        condition += delta.condition;
        grades += delta.grades;
        relationship += delta.relationship;
        stress += delta.stress;
    }

    public void Clamp()
    {
        money = Mathf.Max(0, money);
        condition = Mathf.Clamp(condition, 0, 100);
        grades = Mathf.Clamp(grades, 0, 100);
        relationship = Mathf.Clamp(relationship, 0, 100);
        stress = Mathf.Clamp(stress, 0, 100);
    }
}

[Serializable]
public struct CampusLifeStatDelta
{
    public int money;
    public int condition;
    public int grades;
    public int relationship;
    public int stress;

    public bool IsZero =>
        money == 0 &&
        condition == 0 &&
        grades == 0 &&
        relationship == 0 &&
        stress == 0;
}

[Serializable]
public class SemesterReport
{
    public int semesterNumber;

    [TextArea(2, 6)]
    public string summary = string.Empty;
}

[Serializable]
public class EndingDefinition
{
    public string id = "steady_graduate";
    public string displayName = "Steady Graduate";

    [TextArea(2, 6)]
    public string description = "Puang makes it through college in one piece.";

    [Range(0, 10)] public int priority = 0;
    [Min(0)] public int minimumMoney;
    [Range(0, 100)] public int minimumCondition;
    [Range(0, 100)] public int minimumGrades;
    [Range(0, 100)] public int minimumRelationship;
    [Range(0, 100)] public int minimumStress;
    [Range(0, 100)] public int maximumStress = 100;
    public bool alwaysAvailable;

    public bool Matches(CampusLifeStats stats)
    {
        if (alwaysAvailable)
        {
            return true;
        }

        return stats.money >= minimumMoney &&
               stats.condition >= minimumCondition &&
               stats.grades >= minimumGrades &&
               stats.relationship >= minimumRelationship &&
               stats.stress >= minimumStress &&
               stats.stress <= maximumStress;
    }
}
