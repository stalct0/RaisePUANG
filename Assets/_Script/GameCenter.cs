using UnityEngine;

public class GameCenter : MonoBehaviour
{
    // 어디서나 접근할 수 있는 단 하나의 저장소 (싱글톤)
    public static GameCenter Instance { get; private set; }

    [Header("--- 글로벌 스탯 보관소 ---")]
    public int money = 25000;
    public int condition = 130;
    public int grade = 470;
    public int friendship = 500;

    public int currentGrade = 1;
    public int currentSemester = 1;

    private void Awake()
    {
        // 씬이 바뀌어도 이 오브젝트를 파괴하지 않고 단 하나만 유지함
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 외부에서 스탯을 변동시킬 때 사용할 메서드
    public void ChangeStatus(int m, int c, int g, int f)
    {
        money += m;
        condition += c;
        grade += g;
        friendship += f;

        Debug.Log($"[스탯 변경 완료] 돈: {money}, 컨디션: {condition}, 학점: {grade}, 친구: {friendship}");
    }
}