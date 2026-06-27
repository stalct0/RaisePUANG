using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SemesterBuffSelectionUI : MonoBehaviour
{
    [SerializeField] private SemesterBuffDatabase database;

    [SerializeField] private Button button1;
    [SerializeField] private Button button2;
    [SerializeField] private Button button3;

    [SerializeField] private TMP_Text buttonText1;
    [SerializeField] private TMP_Text buttonText2;
    [SerializeField] private TMP_Text buttonText3;

    private SemesterBuff[] currentChoices;
    private CourseRegistrationMinigameController owner;

    public void Open(BuffGrade grade, CourseRegistrationMinigameController ownerController)
    {
        owner = ownerController;
        currentChoices = PickRandomThree(grade);

        SetupButton(button1, buttonText1, 0);
        SetupButton(button2, buttonText2, 1);
        SetupButton(button3, buttonText3, 2);
    }

    private SemesterBuff[] PickRandomThree(BuffGrade grade)
    {
        List<SemesterBuff> pool = new(database.GetPool(grade));
        List<SemesterBuff> result = new();

        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result.ToArray();
    }

    private void SetupButton(Button button, TMP_Text text, int index)
    {
        if (button == null || text == null)
            return;

        if (currentChoices == null || index >= currentChoices.Length)
        {
            button.gameObject.SetActive(false);
            return;
        }

        SemesterBuff buff = currentChoices[index];

        button.gameObject.SetActive(true);
        text.text = $"{buff.title}\n{buff.description}";

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectBuff(index));
    }

    private void SelectBuff(int index)
    {
        if (index < 0 || index >= currentChoices.Length)
            return;

        SemesterBuff buff = currentChoices[index];

        if (SemesterBuffManager.Instance != null)
            SemesterBuffManager.Instance.ApplyBuff(buff);

        if (owner != null)
            owner.CloseAfterRewardSelected();
    }
}