using System.Collections.Generic;
using UnityEngine;

public sealed class ObstacleSpawner : MonoBehaviour
{
    [Header("Obstacles")]
    [SerializeField] private GameObject[] obstaclePrefabs;
    [SerializeField] private int spawnCount = 12;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private Transform spawnedParent;

    [Header("Spawn Area")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Vector3 areaCenter = new Vector3(0f, 0f, 20f);
    [SerializeField] private Vector3 areaSize = new Vector3(6f, 0f, 40f);
    [SerializeField] private float minSpacing = 1.5f;
    [SerializeField] private int maxAttemptsPerSpawn = 12;
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

        usedPositions.Clear();

        for (int i = 0; i < spawnCount; i++)
        {
            SpawnSingle();
        }
    }

    public void SpawnSingle()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0)
        {
            return;
        }

        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        Vector3 position = GetSpawnPosition();
        Quaternion rotation = randomYaw ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;
        Transform parent = spawnedParent != null ? spawnedParent : transform;

        Instantiate(prefab, position, rotation, parent);
        usedPositions.Add(position);
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return point != null ? point.position : transform.position;
        }

        Vector3 half = areaSize * 0.5f;
        int attempts = Mathf.Max(1, maxAttemptsPerSpawn);

        for (int attempt = 0; attempt < attempts; attempt++)
        {
            Vector3 candidate = areaCenter + new Vector3(
                Random.Range(-half.x, half.x),
                Random.Range(-half.y, half.y),
                Random.Range(-half.z, half.z));

            if (minSpacing <= 0f || IsFarEnough(candidate))
            {
                return candidate;
            }
        }

        return areaCenter;
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
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.5f);
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                if (spawnPoints[i] != null)
                {
                    Gizmos.DrawSphere(spawnPoints[i].position, 0.25f);
                }
            }

            return;
        }

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.3f);
        Gizmos.DrawWireCube(areaCenter, areaSize);
    }
}
