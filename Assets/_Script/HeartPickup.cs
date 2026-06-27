using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class HeartPickup : MonoBehaviour
{
    public Sprite normalSprite;
    public Sprite collectedSprite;
    public float displaySeconds = 20f;
    public float collectGraceSeconds = 10f;
    public float collectedAnimationSeconds = 0.35f;

    public bool IsCollected { get; private set; }
    public Transform Transform => transform;

    private SpriteRenderer spriteRenderer;
    private HeartSpawnManager owner;
    private float elapsedSeconds;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }

        Collider2D pickupCollider = GetComponent<Collider2D>();
        pickupCollider.isTrigger = true;
    }

    private void Update()
    {
        if (IsCollected) return;

        elapsedSeconds += Time.deltaTime;
        if (elapsedSeconds >= displaySeconds + collectGraceSeconds)
        {
            if (owner != null) owner.NotifyHeartExpired(this);
            else Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsCollected || !other.CompareTag("Player")) return;

        StartCoroutine(CollectRoutine());
    }

    public void Initialize(HeartSpawnManager manager, float visibleSeconds, float graceSeconds)
    {
        owner = manager;
        displaySeconds = visibleSeconds;
        collectGraceSeconds = graceSeconds;
        elapsedSeconds = 0f;
        IsCollected = false;
    }

    private IEnumerator CollectRoutine()
    {
        IsCollected = true;

        Collider2D pickupCollider = GetComponent<Collider2D>();
        if (pickupCollider != null) pickupCollider.enabled = false;

        if (spriteRenderer != null && collectedSprite != null)
        {
            spriteRenderer.sprite = collectedSprite;
        }

        yield return new WaitForSeconds(collectedAnimationSeconds);
        if (owner != null) owner.NotifyHeartCollected(this);
        else Destroy(gameObject);
    }
}
