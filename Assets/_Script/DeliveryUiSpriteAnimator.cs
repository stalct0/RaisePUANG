using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class DeliveryUiSpriteAnimator : MonoBehaviour
{
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 4f;
    [SerializeField] private bool playOnAwake = true;
    [SerializeField] private bool preserveAspect = true;

    private float timer;
    private int currentFrame;
    private bool isPlaying;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        isPlaying = playOnAwake;
        ApplyFrame(0);
    }

    private void OnEnable()
    {
        timer = 0f;
        currentFrame = 0;
        isPlaying = playOnAwake;
        ApplyFrame(0);
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        if (targetImage == null || frames == null || frames.Length == 0)
            return;

        if (framesPerSecond <= 0f)
            return;

        timer += Time.unscaledDeltaTime;
        float secondsPerFrame = 1f / framesPerSecond;

        while (timer >= secondsPerFrame)
        {
            timer -= secondsPerFrame;
            currentFrame = (currentFrame + 1) % frames.Length;
            ApplyFrame(currentFrame);
        }
    }

    public void Play()
    {
        isPlaying = true;
    }

    public void Stop()
    {
        isPlaying = false;
    }

    private void ApplyFrame(int frameIndex)
    {
        if (targetImage == null)
            return;

        if (frames == null || frames.Length == 0)
            return;

        frameIndex = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
        Sprite frame = frames[frameIndex];

        if (frame == null)
            return;

        targetImage.sprite = frame;
        targetImage.color = Color.white;
        targetImage.preserveAspect = preserveAspect;
        targetImage.raycastTarget = false;
    }
}
