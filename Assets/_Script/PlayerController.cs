using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float maxSpeed = 6f;
    [SerializeField] private float acceleration = 25f;
    [SerializeField] private float deceleration = 35f;

    private Rigidbody2D rb;

    private Vector2 input;
    private Vector2 currentVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        input = Vector2.zero;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            input.x--;

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            input.x++;

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            input.y--;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            input.y++;

        input = input.normalized;
    }

    private void FixedUpdate()
    {
        Vector2 targetVelocity = input * maxSpeed;

        float accel = input.sqrMagnitude > 0.01f ? acceleration : deceleration;

        currentVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            accel * Time.fixedDeltaTime);

        rb.linearVelocity = currentVelocity;
    }
}