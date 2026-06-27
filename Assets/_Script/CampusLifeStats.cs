using System;
using UnityEngine;

[Serializable]
public class CampusLifeStats
{
    [Min(0)] public int money = 20;
    [Min(0)] public int condition = 70;
    [Min(0)] public int grades = 30;
    [Min(0)] public int relationship = 45;

    public CampusLifeStats Clone()
    {
        return new CampusLifeStats
        {
            money = money,
            condition = condition,
            grades = grades,
            relationship = relationship
        };
    }

    public void Apply(CampusLifeStatDelta delta)
    {
        money += delta.money;
        condition += delta.condition;
        grades += delta.grades;
        relationship += delta.relationship;

        Clamp();
    }

    public CampusLifeStatDelta ApplyAvailable(CampusLifeStatDelta delta)
    {
        CampusLifeStatDelta appliedDelta = new CampusLifeStatDelta
        {
            money = GetApplicableDelta(money, delta.money),
            condition = GetApplicableDelta(condition, delta.condition),
            grades = GetApplicableDelta(grades, delta.grades),
            relationship = GetApplicableDelta(relationship, delta.relationship)
        };

        Apply(appliedDelta);

        return appliedDelta;
    }

    private int GetApplicableDelta(int currentValue, int delta)
    {
        if (delta >= 0)
            return delta;

        if (currentValue <= 0)
            return 0;

        return delta;
    }

    public void Clamp()
    {
        money = Mathf.Max(0, money);
        condition = Mathf.Clamp(condition, 0, 100);
        grades = Mathf.Clamp(grades, 0, 100);
        relationship = Mathf.Clamp(relationship, 0, 100);
    }
}

[Serializable]
public struct CampusLifeStatDelta
{
    public int money;
    public int condition;
    public int grades;
    public int relationship;

    public bool IsZero =>
        money == 0 &&
        condition == 0 &&
        grades == 0 &&
        relationship == 0;
}
