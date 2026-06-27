using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SemesterBuffDatabase", menuName = "Semester/Buff Database")]
public class SemesterBuffDatabase : ScriptableObject
{
    public List<SemesterBuff> goodBuffs = new();
    public List<SemesterBuff> midBuffs = new();
    public List<SemesterBuff> midDebuffs = new();
    public List<SemesterBuff> badDebuffs = new();

    public List<SemesterBuff> GetPool(BuffGrade grade)
    {
        return grade switch
        {
            BuffGrade.Good => goodBuffs,
            BuffGrade.MidBuff => midBuffs,
            BuffGrade.MidDebuff => midDebuffs,
            BuffGrade.Bad => badDebuffs,
            _ => goodBuffs
        };
    }
}