using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class DeliveryRandomSprite : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private bool preserveAspect = true;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        ApplyRandomSprite();
    }

    public void ApplyRandomSprite()
    {
        if (targetImage == null)
        {
            Debug.LogError($"[DeliveryRandomSprite] {gameObject.name}: Image component is missing.", this);
            return;
        }

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError($"[DeliveryRandomSprite] {gameObject.name}: Sprites array is empty.", this);
            return;
        }

        Sprite selected = sprites[Random.Range(0, sprites.Length)];
        if (selected == null)
        {
            Debug.LogError($"[DeliveryRandomSprite] {gameObject.name}: Selected sprite is null.", this);
            return;
        }

        targetImage.sprite = selected;
        targetImage.color = Color.white;
        targetImage.preserveAspect = preserveAspect;
        targetImage.raycastTarget = false;
    }
}
