using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class Obstacle : MonoBehaviour
{
    [SerializeField] private bool destroyAfterHit;

    [Header("Move")]
    [SerializeField] private bool randomMove;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float roamRadius = 3f;
    [SerializeField] private float retargetInterval = 1.5f;
    [SerializeField] private float arriveDistance = 0.2f;
    [SerializeField] private bool constrainToXZ = true;
    [SerializeField] private float bfsCellSize = 1f;

    [Header("Facing")]
    [SerializeField] private bool faceMoveDirection = true;
    [SerializeField] private float turnSpeed = 12f;

    [Header("Hit FX")]
    [SerializeField] private bool explodeOnHit = true;
    [SerializeField] private float liftHeight = 0.6f;
    [SerializeField] private float liftDuration = 0.18f;
    [SerializeField] private Color hitTint = new Color(1f, 0.1f, 0.1f, 1f);
    [SerializeField] private float tintDuration = 0.18f;
    [SerializeField] private float popScale = 1.25f;
    [SerializeField] private float popDuration = 0.15f;
    [SerializeField] private float shrinkDuration = 0.08f;
    [SerializeField] private float destroyDelay = 0.05f;
    [SerializeField] private bool disableCollisionsOnHit = true;

    [Header("Animation")]
    [SerializeField] private bool playRunAnimation = true;
    [SerializeField] private string runStateName = "HumanF@Sprint01_Forward";
    [SerializeField] private float animationFadeTime = 0.06f;

    private Vector3 origin;
    private Vector3 targetPosition;
    private readonly List<Vector3> path = new List<Vector3>();
    private int pathIndex;
    private float retargetTimer;
    private Rigidbody cachedRigidbody;
    private Animator animator;
    private int runStateHash;
    private Renderer[] renderers;
    private Color[] baseColors;
    private Collider[] colliders;
    private Vector3 baseScale;
    private bool dying;

    private void OnEnable()
    {
        origin = transform.position;
        cachedRigidbody = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        runStateHash = Animator.StringToHash(runStateName ?? string.Empty);
        renderers = GetComponentsInChildren<Renderer>();
        colliders = GetComponentsInChildren<Collider>();
        baseScale = transform.localScale;
        CacheBaseColors();
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }
        PickNewTarget();
    }

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

    private void Update()
    {
        if (dying || !randomMove || cachedRigidbody != null)
        {
            return;
        }

        StepWander(Time.deltaTime);
        UpdateRunAnimation();
        UpdateFacing(Time.deltaTime);
        Vector3 next = Vector3.MoveTowards(transform.position, GetCurrentWaypoint(), moveSpeed * Time.deltaTime);
        transform.position = next;
    }

    private void FixedUpdate()
    {
        if (dying || !randomMove || cachedRigidbody == null)
        {
            return;
        }

        StepWander(Time.fixedDeltaTime);
        UpdateRunAnimation();
        UpdateFacing(Time.fixedDeltaTime);
        Vector3 next = Vector3.MoveTowards(transform.position, GetCurrentWaypoint(), moveSpeed * Time.fixedDeltaTime);
        cachedRigidbody.MovePosition(next);
    }

    private void TryHit(Collider other)
    {
        if (dying)
        {
            return;
        }

        LunchRushPlayerController player = other.GetComponentInParent<LunchRushPlayerController>();
        if (player == null)
        {
            return;
        }

        player.HitObstacle(name);
        Debug.Log($"[Obstacle] {name} hit {player.name}");

        if (explodeOnHit)
        {
            StartCoroutine(PlayHitAndDestroy());
            return;
        }

        if (destroyAfterHit)
        {
            Destroy(gameObject);
        }
    }

    private void StepWander(float deltaTime)
    {
        retargetTimer -= deltaTime;
        if (path.Count > 0 && Vector3.Distance(transform.position, GetCurrentWaypoint()) <= arriveDistance)
        {
            pathIndex++;
        }

        if (retargetTimer <= 0f || pathIndex >= path.Count)
        {
            PickNewTarget();
        }
    }

    private void PickNewTarget()
    {
        retargetTimer = Mathf.Max(0.05f, retargetInterval);
        Vector3 offset = Random.insideUnitSphere * roamRadius;
        if (constrainToXZ)
        {
            offset.y = 0f;
        }

        targetPosition = origin + offset;
        BuildBfsPath(targetPosition);
    }

    private Vector3 GetCurrentWaypoint()
    {
        if (path.Count == 0 || pathIndex >= path.Count)
        {
            return targetPosition;
        }

        return path[pathIndex];
    }

    private void BuildBfsPath(Vector3 destination)
    {
        path.Clear();
        pathIndex = 0;

        float cellSize = Mathf.Max(0.1f, bfsCellSize);
        int radiusCells = Mathf.Max(1, Mathf.CeilToInt(roamRadius / cellSize));
        Vector2Int start = WorldToCell(transform.position, cellSize);
        Vector2Int goal = WorldToCell(destination, cellSize);
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> previous = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        previous[start] = start;

        Vector2Int[] directions =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            if (current == goal)
            {
                break;
            }

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int next = current + directions[i];
                if (previous.ContainsKey(next) || next.sqrMagnitude > radiusCells * radiusCells)
                {
                    continue;
                }

                previous[next] = current;
                queue.Enqueue(next);
            }
        }

        if (!previous.ContainsKey(goal))
        {
            path.Add(destination);
            return;
        }

        List<Vector2Int> cells = new List<Vector2Int>();
        Vector2Int step = goal;
        while (step != start)
        {
            cells.Add(step);
            step = previous[step];
        }

        cells.Reverse();
        for (int i = 0; i < cells.Count; i++)
        {
            path.Add(CellToWorld(cells[i], cellSize));
        }
    }

    private Vector2Int WorldToCell(Vector3 worldPosition, float cellSize)
    {
        Vector3 local = worldPosition - origin;
        return new Vector2Int(
            Mathf.RoundToInt(local.x / cellSize),
            Mathf.RoundToInt(local.z / cellSize)
        );
    }

    private Vector3 CellToWorld(Vector2Int cell, float cellSize)
    {
        return origin + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);
    }

    private void UpdateRunAnimation()
    {
        if (!randomMove || !playRunAnimation || animator == null || string.IsNullOrEmpty(runStateName))
        {
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName(runStateName))
        {
            animator.CrossFadeInFixedTime(runStateHash, animationFadeTime);
        }
    }

    private void UpdateFacing(float deltaTime)
    {
        if (!randomMove || !faceMoveDirection)
        {
            return;
        }

        Vector3 direction = GetCurrentWaypoint() - transform.position;
        if (constrainToXZ)
        {
            direction.y = 0f;
        }

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Quaternion newRotation = turnSpeed <= 0f
            ? targetRotation
            : Quaternion.Slerp(transform.rotation, targetRotation, deltaTime * turnSpeed);

        if (cachedRigidbody != null)
        {
            cachedRigidbody.MoveRotation(newRotation);
        }
        else
        {
            transform.rotation = newRotation;
        }
    }

    private void CacheBaseColors()
    {
        if (renderers == null || renderers.Length == 0)
        {
            baseColors = null;
            return;
        }

        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i] != null ? renderers[i].material : null;
            if (material != null && material.HasProperty("_Color"))
            {
                baseColors[i] = material.color;
            }
            else
            {
                baseColors[i] = Color.white;
            }
        }
    }

    private void ApplyTint(float t)
    {
        if (renderers == null || baseColors == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i] != null ? renderers[i].material : null;
            if (material != null && material.HasProperty("_Color"))
            {
                Color baseColor = baseColors.Length > i ? baseColors[i] : material.color;
                material.color = Color.Lerp(baseColor, hitTint, t);
            }
        }
    }

    private void DisableCollisions()
    {
        if (colliders == null)
        {
            return;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
            {
                colliders[i].enabled = false;
            }
        }
    }

    private IEnumerator PlayHitAndDestroy()
    {
        dying = true;
        randomMove = false;

        if (disableCollisionsOnHit)
        {
            DisableCollisions();
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
            cachedRigidbody.isKinematic = true;
        }

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * liftHeight;
        float liftTime = Mathf.Max(0.01f, liftDuration);
        float tintTime = Mathf.Max(0.01f, tintDuration);
        float timer = 0f;

        while (timer < liftTime || timer < tintTime)
        {
            timer += Time.deltaTime;
            float liftT = Mathf.Clamp01(timer / liftTime);
            float tintT = Mathf.Clamp01(timer / tintTime);
            transform.position = Vector3.Lerp(startPos, endPos, liftT);
            ApplyTint(tintT);
            yield return null;
        }

        Vector3 popTarget = baseScale * popScale;
        float popTime = Mathf.Max(0.01f, popDuration);
        timer = 0f;
        while (timer < popTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / popTime);
            transform.localScale = Vector3.Lerp(baseScale, popTarget, t);
            yield return null;
        }

        float shrinkTime = Mathf.Max(0.01f, shrinkDuration);
        timer = 0f;
        while (timer < shrinkTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / shrinkTime);
            transform.localScale = Vector3.Lerp(popTarget, Vector3.zero, t);
            yield return null;
        }

        if (destroyDelay > 0f)
        {
            yield return new WaitForSeconds(destroyDelay);
        }

        Destroy(gameObject);
    }
}
