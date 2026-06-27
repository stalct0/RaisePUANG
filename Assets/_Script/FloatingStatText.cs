using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshPro))]
public class FloatingStatText : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private float moveSpeed = 0.7f;
    [SerializeField] private string sortingLayerName = "Player";
    [SerializeField] private int sortingOrder = 100;

    private float timer;
    private Color originalColor = Color.white;
    private bool loggedMissingTextMesh;

    private void Awake()
    {
        EnsureTextMesh();
        ApplySorting();
    }

    public void Init(string text)
    {
        if (!EnsureTextMesh())
        {
            Debug.LogError($"[FloatingStatText] {gameObject.name}: Cannot initialize popup because TextMeshPro is missing.", this);
            return;
        }

        textMesh.text = text;
        originalColor = textMesh.color;
        timer = 0f;

        ApplySorting();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        float duration = Mathf.Max(0.01f, lifetime);

        if (textMesh != null)
        {
            float alpha = Mathf.Lerp(originalColor.a, 0f, timer / duration);
            textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }

    private bool EnsureTextMesh()
    {
        if (textMesh == null)
            textMesh = GetComponent<TextMeshPro>();

        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMeshPro>(true);

        if (textMesh != null)
            return true;

        if (!loggedMissingTextMesh)
        {
            Debug.LogError(
                $"[FloatingStatText] {gameObject.name}: TextMeshPro component is missing. " +
                "Use a world-space TextMeshPro component, not TextMeshProUGUI.",
                this);
            loggedMissingTextMesh = true;
        }

        return false;
    }

    private void ApplySorting()
    {
        if (textMesh == null)
            return;

        int sortingLayerId = SortingLayer.NameToID(sortingLayerName);

        textMesh.sortingLayerID = sortingLayerId;
        textMesh.sortingOrder = sortingOrder;

        MeshRenderer meshRenderer = textMesh.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerID = sortingLayerId;
            meshRenderer.sortingOrder = sortingOrder;
        }
    }
}
