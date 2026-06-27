using UnityEngine;
using UnityEngine.UI;

public sealed class HeartArrowIndicator : MonoBehaviour
{
    public Canvas canvas;
    public Sprite arrowSprite;
    public Vector2 arrowSize = new Vector2(56f, 56f);
    public float edgePadding = 42f;
    public float topReservedHeight = 72f;

    private RectTransform arrowTransform;
    private Image arrowImage;

    public void Track(Camera targetCamera, Transform player, Transform target)
    {
        if (targetCamera == null || player == null || target == null)
        {
            Hide();
            return;
        }

        EnsureArrow();
        if (arrowTransform == null) return;

        Vector3 viewport = targetCamera.WorldToViewportPoint(target.position);
        bool isVisible = viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
        if (isVisible)
        {
            Hide();
            return;
        }

        Vector2 direction = target.position - player.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            Hide();
            return;
        }

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 halfSize = canvasRect.rect.size * 0.5f;
        Vector2 arrowHalfSize = arrowSize * 0.5f;
        Vector2 position;
        float rotationZ;

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
        {
            bool east = direction.x >= 0f;
            position = new Vector2(east ? halfSize.x - edgePadding - arrowHalfSize.x : -halfSize.x + edgePadding + arrowHalfSize.x, 0f);
            rotationZ = east ? 0f : 180f;
        }
        else
        {
            bool north = direction.y >= 0f;
            float topPadding = edgePadding + (north ? topReservedHeight : 0f);
            position = new Vector2(0f, north ? halfSize.y - topPadding - arrowHalfSize.y : -halfSize.y + edgePadding + arrowHalfSize.y);
            rotationZ = north ? 90f : -90f;
        }

        arrowTransform.anchoredPosition = position;
        arrowTransform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
        arrowImage.enabled = true;
    }

    public void Hide()
    {
        if (arrowImage != null) arrowImage.enabled = false;
    }

    private void EnsureArrow()
    {
        if (arrowTransform != null) return;
        if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null || arrowSprite == null) return;

        GameObject arrowObject = new GameObject("HeartArrow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        arrowObject.transform.SetParent(canvas.transform, false);
        arrowTransform = arrowObject.GetComponent<RectTransform>();
        arrowTransform.anchorMin = new Vector2(0.5f, 0.5f);
        arrowTransform.anchorMax = new Vector2(0.5f, 0.5f);
        arrowTransform.pivot = new Vector2(0.5f, 0.5f);
        arrowTransform.sizeDelta = arrowSize;

        arrowImage = arrowObject.GetComponent<Image>();
        arrowImage.sprite = arrowSprite;
        arrowImage.raycastTarget = false;
        arrowImage.preserveAspect = true;
        arrowImage.enabled = false;
    }
}
