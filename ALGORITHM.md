# LunchRush 적 이동 알고리즘 정리

## 사용한 알고리즘

이 게임의 적 이동에는 **BFS(너비 우선 탐색, Breadth-First Search)** 알고리즘을 사용했다.

BFS는 시작 지점에서 가까운 칸부터 차례대로 탐색하는 알고리즘이다. 그래서 격자 맵에서 목표 지점까지 가는 경로를 찾을 때 사용할 수 있다. 이 게임에서는 적이 생성된 위치 주변을 작은 격자로 나누고, 현재 칸에서 랜덤 목표 칸까지 BFS로 이동 경로를 만든다.

## 왜 BFS를 사용했는가

적이 그냥 아무 방향으로 움직이면 알고리즘 수행평가에서 설명하기 어렵다. 그래서 적의 이동 범위를 격자 형태로 보고, 현재 위치에서 목표 위치까지 탐색 알고리즘으로 경로를 찾도록 만들었다.

BFS를 사용한 이유:

- 시작점에서 가까운 칸부터 탐색해서 경로를 찾을 수 있다.
- 상하좌우 방향으로 이동하는 격자 이동과 잘 맞는다.
- 목표 칸을 찾으면 이전 칸 정보를 따라가며 실제 이동 경로를 복원할 수 있다.
- DFS보다 최단 경로를 찾는 데 더 적합하다.

## 적 생성 방식

적 생성은 `ObstacleSpawner`가 담당한다.

1. 씬에서 `targetTag` 값이 `"yumbabo"`인 오브젝트들을 찾는다.
2. 찾은 위치들 위에 적을 생성한다.
3. 적 프리팹은 `obstaclePrefabs` 배열에서 랜덤으로 선택한다.
4. 같은 위치에 너무 붙어서 생성되지 않도록 `minSpacing` 거리 검사를 한다.

씬 설정 기준:

- `spawnCount`: 120
- `targetTag`: `yumbabo`
- `spawnHeightOffset`: 0.5
- `minSpacing`: 1.5
- `randomYaw`: 켜짐

## BFS 이동 방식

적 이동은 `Obstacle` 스크립트가 담당한다.

적이 처음 생성되면 현재 위치를 `origin`으로 저장한다. 이후 일정 시간이 지나면 `origin` 주변에서 랜덤 목표 위치를 정한다. 이 목표 위치를 바로 향해 움직이지 않고, 먼저 BFS로 경로를 계산한다.

사용한 주요 값:

- `roamRadius`: 적이 돌아다닐 수 있는 반경
- `bfsCellSize`: BFS 격자 한 칸의 크기
- `retargetInterval`: 새 목표를 다시 고르는 시간
- `moveSpeed`: 경로를 따라 움직이는 속도
- `arriveDistance`: waypoint에 도착했다고 판단하는 거리

## BFS 경로 탐색 과정

1. 현재 월드 좌표를 BFS 격자 좌표로 바꾼다.
2. 목표 월드 좌표도 BFS 격자 좌표로 바꾼다.
3. 시작 칸을 큐에 넣는다.
4. 큐에서 칸을 하나 꺼낸다.
5. 그 칸의 상하좌우 이웃 칸을 확인한다.
6. 아직 방문하지 않은 칸이면 `previous`에 이전 칸을 저장하고 큐에 넣는다.
7. 목표 칸을 찾을 때까지 반복한다.
8. 목표 칸을 찾으면 `previous`를 거꾸로 따라가 경로를 복원한다.
9. 복원된 경로를 월드 좌표 waypoint로 바꾼다.
10. 적은 waypoint를 하나씩 따라 이동한다.

핵심 BFS 코드:

```csharp
Queue<Vector2Int> queue = new Queue<Vector2Int>();
Dictionary<Vector2Int, Vector2Int> previous = new Dictionary<Vector2Int, Vector2Int>();

queue.Enqueue(start);
previous[start] = start;

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
        if (previous.ContainsKey(next))
        {
            continue;
        }

        previous[next] = current;
        queue.Enqueue(next);
    }
}
```

## 실제 이동 과정

BFS는 경로를 계산하는 역할이고, 실제 이동은 `Vector3.MoveTowards`로 처리한다.

```csharp
Vector3 next = Vector3.MoveTowards(
    transform.position,
    GetCurrentWaypoint(),
    moveSpeed * Time.deltaTime
);

transform.position = next;
```

즉, BFS로 `현재 칸 -> 목표 칸`까지의 waypoint 목록을 만들고, 적은 그 waypoint를 순서대로 따라간다. 현재 waypoint에 가까워지면 다음 waypoint로 넘어간다. 모든 waypoint를 지나거나 시간이 지나면 새 목표를 정하고 BFS를 다시 실행한다.

## 방향 회전

적은 이동할 waypoint 방향을 바라보도록 회전한다.

```csharp
Vector3 direction = GetCurrentWaypoint() - transform.position;
Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
Quaternion newRotation = Quaternion.Slerp(transform.rotation, targetRotation, deltaTime * turnSpeed);
```

이렇게 해서 적이 순간적으로 방향을 바꾸지 않고, 이동 방향으로 부드럽게 회전한다.

## 의사 코드

```text
처음 시작:
    origin = 현재 위치
    새 목표 선택
    BFS로 목표까지 경로 생성

새 목표 선택:
    origin 주변에서 랜덤 목표 위치 선택
    현재 위치를 시작 칸으로 변환
    목표 위치를 목표 칸으로 변환
    BFS 실행
    찾은 칸들을 waypoint 경로로 저장

BFS:
    queue에 시작 칸 삽입
    시작 칸 방문 처리

    while queue가 비어 있지 않으면:
        current = queue에서 꺼낸 칸

        if current가 목표 칸이면:
            탐색 종료

        상하좌우 이웃 칸 확인:
            방문하지 않은 칸이면:
                이전 칸 정보 저장
                queue에 삽입

    목표 칸부터 시작 칸까지 previous를 따라가며 경로 복원

매 프레임:
    현재 waypoint 쪽으로 이동

    if waypoint에 도착:
        다음 waypoint로 변경

    if 경로가 끝났거나 시간이 지나면:
        새 목표 선택 후 BFS 다시 실행
```

## 한 줄 요약

LunchRush의 적은 생성 위치 주변을 격자로 나누고, 현재 칸에서 랜덤 목표 칸까지 **BFS 알고리즘**으로 경로를 찾은 다음, 그 경로의 waypoint를 따라 이동한다.
