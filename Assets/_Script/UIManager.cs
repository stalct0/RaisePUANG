using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class UIManager : MonoBehaviour
{
    public int money = 25000;
    public int condition = 130;
    public int grade = 470;
    public int friendship = 500;


    public int currentGrade = 1;
    public int currentSemester = 1; 
    public string currentArea = "광장"; 

   
    public TextMeshProUGUI moneyText;
    public TextMeshProUGUI conditionText;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI friendshipText;
    public TextMeshProUGUI semesterText;
    public TextMeshProUGUI areaNameText;

    void Start()
    {
        UpdateUI();
    }

    // 스탯 및 정보를 UI에 반영하는 메서드
    public void UpdateUI()
    {
        moneyText.text = money.ToString();
        conditionText.text = condition.ToString();
        gradeText.text = grade.ToString();
        friendshipText.text = friendship.ToString();
        
        semesterText.text = $"{currentGrade}학년 {currentSemester}학기";
        areaNameText.text = currentArea;
    }

    public void ExecuteAction(string areaName, int moneyCost, int conditionChange, int gradeChange, int friendshipChange)
    {
        // 돈이 부족하면 행동 불가 처리
        if (money + moneyCost < 0)
        {
            Debug.LogWarning($"[경고] 돈이 부족하여 '{areaName}' 행동을 할 수 없습니다! (필요 금액: {-moneyCost})");
            return; 
        }

        money += moneyCost;
        condition += conditionChange;
        grade += gradeChange;
        friendship += friendshipChange;

        currentArea = areaName;

        UpdateUI();
        Debug.Log($"[{areaName}] /소모 비용: {moneyCost}");
    }
}