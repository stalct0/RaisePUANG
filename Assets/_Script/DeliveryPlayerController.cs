using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class DeliveryPlayerController : MonoBehaviour
{
    [Header("--- Lane ---")]
    public int laneCount = 4;
    public int startLaneIndex = 1;
    public float moveLerpSpeed = 16f;
    public float lanePadding = 80f;

    [Header("--- Play Area ---")]
    public RectTransform playArea;

    public int CurrentLaneIndex { get; private set; }
    public RectTransform RectTransform { get; private set; }

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        CurrentLaneIndex = Mathf.Clamp(startLaneIndex, 0, laneCount - 1);
        SnapToLane();
    }

    private void Update()
    {
        if (WasMoveLeftPressed())
        {
            MoveLane(-1);
        }

        if (WasMoveRightPressed())
        {
            MoveLane(1);
        }

        Vector2 target = RectTransform.anchoredPosition;
        target.x = GetLaneX(CurrentLaneIndex);
        RectTransform.anchoredPosition = Vector2.Lerp(
            RectTransform.anchoredPosition,
            target,
            Time.unscaledDeltaTime * moveLerpSpeed
        );
    }

    public void Configure(RectTransform area, int lanes, float padding)
    {
        playArea = area;
        laneCount = Mathf.Max(1, lanes);
        lanePadding = Mathf.Max(0f, padding);
        CurrentLaneIndex = Mathf.Clamp(startLaneIndex, 0, laneCount - 1);
        SnapToLane();
    }

    public float GetLaneX(int laneIndex)
    {
        if (playArea == null || laneCount <= 1) return 0f;

        float width = playArea.rect.width - lanePadding * 2f;
        float step = width / (laneCount - 1);
        return -width * 0.5f + step * Mathf.Clamp(laneIndex, 0, laneCount - 1);
    }

    public void SnapToLane()
    {
        if (RectTransform == null) RectTransform = GetComponent<RectTransform>();
        RectTransform.anchoredPosition = new Vector2(GetLaneX(CurrentLaneIndex), RectTransform.anchoredPosition.y);
    }

    private void MoveLane(int direction)
    {
        CurrentLaneIndex = Mathf.Clamp(CurrentLaneIndex + direction, 0, laneCount - 1);
    }

    private bool WasMoveLeftPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.leftArrowKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.LeftArrow);
#endif
    }

    private bool WasMoveRightPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rightArrowKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.RightArrow);
#endif
    }
}
