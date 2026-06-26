using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Follow")]
    public float smoothSpeed = 8f;
    public Vector3 offset = new Vector3(0, 0, -10);

    [Header("Map Bounds")]
    public Vector2 minBounds;
    public Vector2 maxBounds;

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 targetPos = target.position + offset;

        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        float clampedX = Mathf.Clamp(
            targetPos.x,
            minBounds.x + camWidth,
            maxBounds.x - camWidth
        );

        float clampedY = Mathf.Clamp(
            targetPos.y,
            minBounds.y + camHeight,
            maxBounds.y - camHeight
        );

        Vector3 finalPos = new Vector3(clampedX, clampedY, offset.z);

        transform.position = Vector3.Lerp(
            transform.position,
            finalPos,
            smoothSpeed * Time.deltaTime
        );
    }
}