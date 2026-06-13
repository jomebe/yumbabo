using UnityEngine;

public sealed class FinishZone : MonoBehaviour
{
    private void Reset()
    {
        Collider finishCollider = GetComponent<Collider>();
        if (finishCollider != null)
        {
            finishCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryClear(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryClear(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryClear(collision.collider);
    }

    private static void TryClear(Collider other)
    {
        if (other == null)
        {
            return;
        }

        LunchRushPlayerController player = other.GetComponentInParent<LunchRushPlayerController>();
        if (player != null)
        {
            player.ClearGame();
        }
    }
}
