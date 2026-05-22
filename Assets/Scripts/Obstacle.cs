using UnityEngine;

public sealed class Obstacle : MonoBehaviour
{
    [SerializeField] private bool destroyAfterHit;

    private void Reset()
    {
        gameObject.name = string.IsNullOrEmpty(gameObject.name) ? "Obstacle" : gameObject.name;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void TryHit(Collider other)
    {
        LunchRushPlayerController player = other.GetComponentInParent<LunchRushPlayerController>();
        if (player == null)
        {
            return;
        }

        player.HitObstacle(name);
        Debug.Log($"[Obstacle] {name} hit {player.name}");

        if (destroyAfterHit)
        {
            Destroy(gameObject);
        }
    }
}
