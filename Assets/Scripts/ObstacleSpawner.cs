using System.Collections.Generic;
using UnityEngine;

public sealed class ObstacleSpawner : MonoBehaviour
{
    [Header("Obstacles")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private int spawnCount = 12;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private Transform spawnedParent;

    [Header("Spawn Target")]
    [SerializeField] private string targetTag = "yumbabo";
    [SerializeField] private float spawnHeightOffset = 0.5f;
    [SerializeField] private float minSpacing = 1.5f;
    [SerializeField] private bool randomYaw = true;

    private readonly List<Vector3> usedPositions = new List<Vector3>();

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnBatch();
        }
    }

    public void SpawnBatch()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            Debug.LogWarning("[ObstacleSpawner] No obstacle prefabs set.");
            return;
        }

        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);
        if (targets == null || targets.Length == 0)
        {
            Debug.LogWarning($"[ObstacleSpawner] No objects found with tag '{targetTag}'.");
            return;
        }

        usedPositions.Clear();

        int maxSpawns = Mathf.Min(spawnCount, targets.Length);
        for (int i = 0; i < maxSpawns; i++)
        {
            SpawnOnTarget(targets[i]);
        }
    }

    public void SpawnSingle()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            return;
        }

        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);
        if (targets == null || targets.Length == 0)
        {
            return;
        }

        SpawnOnTarget(targets[Random.Range(0, targets.Length)]);
    }

    private void SpawnOnTarget(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Vector3 position = GetTopPosition(target) + Vector3.up * spawnHeightOffset;
        if (minSpacing > 0f && !IsFarEnough(position))
        {
            return;
        }

        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        Quaternion rotation = randomYaw ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;
        Transform parent = spawnedParent != null ? spawnedParent : transform;

        Instantiate(prefab, position, rotation, parent);
        usedPositions.Add(position);
    }

    private static Vector3 GetTopPosition(GameObject target)
    {
        Collider collider = target.GetComponentInChildren<Collider>();
        if (collider != null)
        {
            return collider.bounds.center + Vector3.up * collider.bounds.extents.y;
        }

        Renderer renderer = target.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds.center + Vector3.up * renderer.bounds.extents.y;
        }

        return target.transform.position;
    }

    private bool IsFarEnough(Vector3 candidate)
    {
        float minSqr = minSpacing * minSpacing;
        for (int i = 0; i < usedPositions.Count; i++)
        {
            if ((usedPositions[i] - candidate).sqrMagnitude < minSqr)
            {
                return false;
            }
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.6f);
        GameObject[] targets = GameObject.FindGameObjectsWithTag(targetTag);
        if (targets == null)
        {
            return;
        }

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
            {
                continue;
            }

            Gizmos.DrawSphere(GetTopPosition(targets[i]) + Vector3.up * spawnHeightOffset, 0.25f);
        }
    }
}
