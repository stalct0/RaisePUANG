using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class DeliveryCollisionReceiver : MonoBehaviour
{
    private DeliveryGameManager manager;

    public void Initialize(DeliveryGameManager owner)
    {
        manager = owner;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (manager == null) return;

        DeliveryFallingItem item = other.GetComponent<DeliveryFallingItem>();
        if (item != null)
        {
            manager.HandleItemCollision(item);
        }
    }
}
