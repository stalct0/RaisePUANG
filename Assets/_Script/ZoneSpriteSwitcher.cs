using UnityEngine;

public class ZoneSpriteSwitcher : MonoBehaviour
{
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
    [SerializeField] private float secondsPerEvolution = 180f; // 3분
    [SerializeField] private string playerTag = "Player";

    private bool isPlayerInside;
    private float accumulatedInsideTime;
    private int evolutionStage;

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

        if (evolutionStage >= 2)
            return;

        accumulatedInsideTime += Time.deltaTime;

        if (accumulatedInsideTime >= secondsPerEvolution)
        {
            accumulatedInsideTime -= secondsPerEvolution;
            evolutionStage++;

            ApplyCurrentSprite();

            Debug.Log($"{gameObject.name} evolved to stage {evolutionStage}");
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
        ApplyCurrentSprite();
    }

    private void ApplyCurrentSprite()
    {
        if (targetRenderer == null)
            return;

        Sprite nextSprite = isPlayerInside
            ? GetActiveSprite()
            : GetNormalSprite();

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
                return normalStage1;
            case 2:
                return normalStage2;
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
                return activeStage1 != null ? activeStage1 : normalStage1;
            case 2:
                return activeStage2 != null ? activeStage2 : normalStage2;
            default:
                return activeStage2 != null ? activeStage2 : normalStage2;
        }
    }
}