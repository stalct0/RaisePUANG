using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ZoneTrigger : MonoBehaviour
{
    public ZoneType zoneType;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        ZoneManager.Instance.EnterZone(zoneType);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        ZoneManager.Instance.ExitZone(zoneType);
    }
}