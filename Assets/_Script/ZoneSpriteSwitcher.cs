using UnityEngine;

public class ZoneSpriteSwitcher : MonoBehaviour
{
    [Header("Zone")]
    [SerializeField] private ZoneType zoneType = ZoneType.None;

    [Header("Renderer")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("Normal Sprites")]
    [SerializeField] private Sprite normalStage0;
    [SerializeField] private Sprite normalStage1;
    [SerializeField] private Sprite normalStage2;

    [Header("Active Sprites")]
    [SerializeField] private Sprite activeStage0;
    [SerializeField] private Sprite activeStage1;
    [SerializeField] private Sprite activeStage2;

    [Header("Evolution")]
    [SerializeField] private float secondsPerEvolution = 180f;
    [SerializeField] private string playerTag = "Player";

    private bool isPlayerInside;
    private bool interactionAllowed = true;
    private float accumulatedInsideTime;
    private int evolutionStage;

    public ZoneType ZoneType => zoneType;
    public int CurrentLevel => evolutionStage + 1;
    public float AccumulatedInsideTime => accumulatedInsideTime;
    public bool InteractionAllowed => interactionAllowed;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<SpriteRenderer>();

        if (normalStage0 == null && targetRenderer != null)
            normalStage0 = targetRenderer.sprite;

        ApplyCurrentSprite();
    }

    private void Update()
    {
        if (!isPlayerInside)
            return;

        if (!interactionAllowed)
            return;

        if (evolutionStage >= 2)
            return;

        accumulatedInsideTime += Time.deltaTime;

        while (accumulatedInsideTime >= secondsPerEvolution && evolutionStage < 2)
        {
            accumulatedInsideTime -= secondsPerEvolution;
            evolutionStage++;
            ApplyCurrentSprite();

            Debug.Log($"[ZoneSpriteSwitcher] {gameObject.name} evolved to level {CurrentLevel}.", this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        isPlayerInside = true;
        ApplyCurrentSprite();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        isPlayerInside = false;
        interactionAllowed = true;
        ApplyCurrentSprite();
    }

    public void SetInteractionAllowed(bool allowed)
    {
        if (interactionAllowed == allowed)
            return;

        interactionAllowed = allowed;
        ApplyCurrentSprite();
    }

    private void ApplyCurrentSprite()
    {
        if (targetRenderer == null)
            return;

        Sprite nextSprite = isPlayerInside && interactionAllowed ? GetActiveSprite() : GetNormalSprite();

        if (nextSprite != null)
            targetRenderer.sprite = nextSprite;
    }

    private Sprite GetNormalSprite()
    {
        switch (evolutionStage)
        {
            case 0:
                return normalStage0;
            case 1:
                return normalStage1 != null ? normalStage1 : normalStage0;
            case 2:
                return normalStage2 != null ? normalStage2 : normalStage1 != null ? normalStage1 : normalStage0;
            default:
                return normalStage2;
        }
    }

    private Sprite GetActiveSprite()
    {
        switch (evolutionStage)
        {
            case 0:
                return activeStage0 != null ? activeStage0 : normalStage0;
            case 1:
                return activeStage1 != null ? activeStage1 : GetNormalSprite();
            case 2:
                return activeStage2 != null ? activeStage2 : GetNormalSprite();
            default:
                return activeStage2 != null ? activeStage2 : GetNormalSprite();
        }
    }
    public void ForceLevelUp()
    {
        if (evolutionStage >= 2)
            return;

        evolutionStage++;
        accumulatedInsideTime = 0f;
        ApplyCurrentSprite();

        Debug.Log($"[ZoneSpriteSwitcher] {gameObject.name} force level up to {CurrentLevel}.", this);
    }
    
    public void DebugSetLevel(int level)
    {
        level = Mathf.Clamp(level, 1, 3);

        evolutionStage = level - 1;
        accumulatedInsideTime = 0f;

        ApplyCurrentSprite();
    }
}
