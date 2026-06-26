using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class CampusLifeHud : MonoBehaviour
{
    private Canvas canvas;
    private Text semesterText;
    private Text statsText;
    private Text endingText;
    private Text summaryText;
    private Text buttonLabel;
    private Button endSemesterButton;
    private CampusLifeGameManager manager;
    private Font hudFont;

    private void Start()
    {
        manager = CampusLifeGameManager.Instance ?? FindFirstObjectByType<CampusLifeGameManager>();
        EnsureUi();
        SceneManager.sceneLoaded += OnSceneLoaded;
        Subscribe();
        Refresh();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Unsubscribe();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        EnsureEventSystem();
    }

    private void Subscribe()
    {
        if (manager == null)
        {
            return;
        }

        manager.StateChanged -= Refresh;
        manager.StateChanged += Refresh;
    }

    private void Unsubscribe()
    {
        if (manager == null)
        {
            return;
        }

        manager.StateChanged -= Refresh;
    }

    private void EnsureUi()
    {
        if (canvas != null)
        {
            return;
        }

        EnsureEventSystem();

        hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("CampusLifeHudCanvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform statsPanel = CreatePanel(
            "StatsPanel",
            canvas.transform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(24f, -304f),
            new Vector2(424f, -24f),
            new Color(0.08f, 0.12f, 0.18f, 0.82f));

        CreateText(
            "Title",
            statsPanel,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(16f, -52f),
            new Vector2(-16f, -12f),
            30,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            "Puang Raising");

        semesterText = CreateText(
            "SemesterText",
            statsPanel,
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(16f, -98f),
            new Vector2(-16f, -56f),
            22,
            FontStyle.Bold,
            TextAnchor.UpperLeft,
            string.Empty);

        statsText = CreateText(
            "StatsText",
            statsPanel,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(16f, 16f),
            new Vector2(-16f, -112f),
            22,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            string.Empty);

        RectTransform endingPanel = CreatePanel(
            "EndingPanel",
            canvas.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-464f, -264f),
            new Vector2(-24f, -24f),
            new Color(0.18f, 0.11f, 0.11f, 0.82f));

        endingText = CreateText(
            "EndingText",
            endingPanel,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(16f, 16f),
            new Vector2(-16f, -16f),
            21,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            string.Empty);

        RectTransform summaryPanel = CreatePanel(
            "SummaryPanel",
            canvas.transform,
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(24f, 24f),
            new Vector2(-264f, 224f),
            new Color(0.07f, 0.09f, 0.12f, 0.82f));

        summaryText = CreateText(
            "SummaryText",
            summaryPanel,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(16f, 16f),
            new Vector2(-16f, -16f),
            20,
            FontStyle.Normal,
            TextAnchor.UpperLeft,
            string.Empty);

        endSemesterButton = CreateButton(
            "EndSemesterButton",
            canvas.transform,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-232f, 24f),
            new Vector2(-24f, 96f));

        buttonLabel = endSemesterButton.GetComponentInChildren<Text>();
        buttonLabel.font = hudFont;
        buttonLabel.fontSize = 24;
        buttonLabel.fontStyle = FontStyle.Bold;
        buttonLabel.alignment = TextAnchor.MiddleCenter;
        endSemesterButton.onClick.AddListener(OnEndSemesterClicked);
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        StandaloneInputModule standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (standaloneModule != null)
        {
            standaloneModule.enabled = false;
        }

        InputSystemUIInputModule inputSystemModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputSystemModule == null)
        {
            inputSystemModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        inputSystemModule.AssignDefaultActions();
    }

    private void OnEndSemesterClicked()
    {
        if (manager == null)
        {
            manager = CampusLifeGameManager.Instance ?? FindFirstObjectByType<CampusLifeGameManager>();
            Subscribe();
        }

        if (manager == null)
        {
            return;
        }

        manager.EndCurrentSemester();
    }

    private void Refresh()
    {
        if (manager == null)
        {
            manager = CampusLifeGameManager.Instance ?? FindFirstObjectByType<CampusLifeGameManager>();
            Subscribe();
        }

        if (manager == null)
        {
            return;
        }

        CampusLifeStats stats = manager.Stats;
        semesterText.text = manager.HasFinishedRun
            ? $"All semesters complete ({manager.MaxSemesters}/{manager.MaxSemesters})"
            : $"Semester {manager.CurrentSemester} / {manager.MaxSemesters}";

        statsText.text =
            $"Money: {stats.money}\n" +
            $"Condition: {stats.condition}\n" +
            $"Grades: {stats.grades}\n" +
            $"Relationships: {stats.relationship}\n" +
            $"Stress: {stats.stress}";

        EndingDefinition preview = manager.CurrentEndingPreview;
        endingText.text =
            "Ending Preview\n\n" +
            $"{preview.displayName}\n" +
            $"{preview.description}";

        summaryText.text = manager.LastSummary;
        buttonLabel.text = manager.HasFinishedRun ? "Restart Run" : "End Semester";
    }

    private RectTransform CreatePanel(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Color color)
    {
        GameObject panelObject = new GameObject(objectName);
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        return rectTransform;
    }

    private Text CreateText(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        int fontSize,
        FontStyle fontStyle,
        TextAnchor alignment,
        string initialValue)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        Text text = textObject.AddComponent<Text>();
        text.font = hudFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = initialValue;
        return text;
    }

    private Button CreateButton(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.45f, 0.28f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = image.color;
        colors.highlightedColor = new Color(0.25f, 0.58f, 0.35f, 0.98f);
        colors.pressedColor = new Color(0.14f, 0.33f, 0.20f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.2f, 0.2f, 0.2f, 0.65f);
        button.colors = colors;

        CreateText(
            "Label",
            buttonObject.transform,
            Vector2.zero,
            Vector2.one,
            new Vector2(12f, 8f),
            new Vector2(-12f, -8f),
            24,
            FontStyle.Bold,
            TextAnchor.MiddleCenter,
            "End Semester");

        return button;
    }
}
