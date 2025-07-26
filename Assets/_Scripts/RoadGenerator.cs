using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// RoadGenerator.cs

// 노드 클래스
// 아마 너가 만든 코드에도 같은 이름의 클래스가 있어서 버그가 날 수 있으니까
// 다른 맵에서 돌리거나 코드파일 다른 곳에 옮겨 놓고 돌려야함
public class Node
{
    public Vector3Int pos;
    public Vector3Int dir = Vector3Int.zero;
    public Node parent;

    public int gCost = 0;
    public int hCost = 0;
    public int fCost = 0;

    public Node(Vector3Int pos)
    {
        this.pos = pos;
    }
}


[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))] // 메쉬관련 컴포넌트가 없으면 강제 추가
[RequireComponent(typeof(Rigidbody))]
public class RoadGenerator : MonoBehaviour
{
    // 테마별 / 설정별 변수
    public Vector3Int gridSize = new Vector3Int(40, 7, 40); // 그리드 사이즈
    public int maxWayPoint = 30;            // wayPoint의 최대 개수
    public float minDistance = 7f;         // wayPoint 사이의 최소거리
    public float carHeight = 0f;            // 자동차 생성 높이 조절
    public GameObject car;                  // 자동차 오브젝트
    public GameObject Landmark;             // 랜드마크 
    public List<GameObject> building_O;     // 2 * 2 건물
    public List<GameObject> building_I;     // 1 * 2 건물
    public List<GameObject> building_L;     // 2 * 2 꺽인 건물
    public List<GameObject> building_dot;   // 1 * 1 건물

    // 모든 맵 공통 변수
    public float height = 0.5f;               // 도로 간의 높이 // 정배율은 1인데 개인적으로 0.5 쯤 돼야 도로가 이쁜듯
    public float scale = 10f;                // 맵 크기
    public float roadHeight = 0.05f;         // 도로 두께
    public float roadWidth = 0.6f;         // 도로 너비
    public float lineWidth = 0.02f;         // 중앙선 너비
    public float colliderHeight = 1f;       // 콜라이더 높이
    public float smoothness = 0.125f;       // 곡면을 얼마나 부드럽게 이을건지 설정 // 0 ~ 1 사이 값만 허용 // 절대 0 넣지 말기
    public int startLength = 3;             // 처음 직선 구간 길이

    // 내부 변수
    private byte[,,] grid;                                      // void 0 // road 1 // 시작점까지 이동 가능한 점 2
    private List<Vector3Int> path = new List<Vector3Int>();     // 도로가 시작점부터 순서대로 그리드의 어느 좌표로 이동하는지를 저장함
    private List<Vector3Int> pathDir = new List<Vector3Int>();  // 얘는 도로의 방향을 저장함 // path랑 pathDir에서 리스트 내 위치가 같으면 같은 도로임
    private List<Vector3Int> aPath = new List<Vector3Int>();    // path랑 똑같지만 aStar를 실행할 때 부분적으로만(각 wayPoint 사이) 작동함
    private List<Vector3Int> aPathDir = new List<Vector3Int>();
    private List<Vector3> roadPoint = new List<Vector3>();      // 얘는 보간이 적용된 path라고 생각하면 됨
    private List<Vector3> roadDir = new List<Vector3>();
    private List<Vector3Int> wayPoint = new List<Vector3Int>();
    private byte[,] map;                                        // 건물이 설치될 위치를 구하기 위한 그리드
    private bool closedRoad;                                    // 도로가 제대로 연결됐는지를 나타냄

    private void Start()
    {
        Transform trans = GetComponent<Transform>();
        trans.position = Vector3.zero;

        Rigidbody rigid = GetComponent<Rigidbody>();
        rigid.isKinematic = true;

        if (smoothness == 0)    // smoothness 값은 0이 되면 코드가 무한 반복 됨
            smoothness = 0.01f; // 그거 방지용 코드

        while (!closedRoad)
        {
            closedRoad = true;
            // clearVar();             // 변수 초기화
            grid = new byte[gridSize.x, gridSize.y, gridSize.z];    // 그리드 생성
            map = new byte[gridSize.x + 4, gridSize.z + 4];
            setLandmark();          // 랜드마크 생성
            generateWayPoints();    // wayPoint 생성
            generateRoadMap();      // wayPoint를 잇는 도로를 그리드 위에 생성
        }

        setLine();  // 그리드에 그려진 도로를 보간법을 이용해 부드럽게 이어줌
        setMesh();  // 보간된 도로를 그래픽으로 변환
        build();    // 주변 건물 설치
        // clearVar(); // 변수 초기화
        Instantiate(car, new Vector3(gridSize.x / 2, gridSize.y /2 * height + carHeight, gridSize.z / 2) * scale, Quaternion.identity);

    }

    private void clearVar()
    {
        grid = null;
        path.Clear();
        pathDir.Clear();
        aPath.Clear();
        aPathDir.Clear();
        roadPoint.Clear();
        roadDir.Clear();
        wayPoint.Clear();
        map = null;
    }

    private void setLandmark()
    {
        Vector3Int landPos = gridSize / 2 + Vector3Int.right * 7;
        for (int dx = -2; dx <= 2; dx++)
            for (int dz = -2; dz <= 2; dz++)
            {
                map[landPos.x + dx + 2, landPos.z + dz + 2] = 1;
                for (int y = 0; y < gridSize.y; y++)
                    grid[landPos.x + dx, y, landPos.z + dz] = 1;
            }
    }

    // 여긴 gpt가 만들어준 코드임
    // wayPoint를 찍어주는 코드
    // 쉬우니까 알아서 해석하길
    private void generateWayPoints()
    {
        int attempts = 0;
        int maxAttempts = 1000;

        wayPoint.Add(gridSize / 2 + Vector3Int.back * startLength);
        wayPoint.Add(gridSize / 2 + Vector3Int.forward * startLength);

        while (wayPoint.Count < maxWayPoint - 2 && attempts < maxAttempts)
        {
            Vector3Int candidate = new Vector3Int(
                Random.Range(2, gridSize.x - 2),
                Random.Range(2, gridSize.y - 2),
                Random.Range(2, gridSize.z - 2)
            );

            if (IsValid(candidate))
            {
                wayPoint.Add(candidate);
            }

            attempts++;
        }
    }

    // 여기까지도 gpt
    private bool IsValid(Vector3Int candidate)
    {
        Vector3 vec1 = new Vector3(candidate.x, 0, candidate.z);
        foreach (Vector3Int p in wayPoint)
        {
            Vector3 vec2 = new Vector3(p.x, 0, p.z);
            if (Vector3.Distance(vec2, vec1) < minDistance || grid[candidate.x, candidate.y, candidate.z] == 1)
                return false;
        }
        return true;
    }

    // wayPoint들의 각 사이를 aStar로 이어주는 코드
    private void generateRoadMap()
    {
        // 초기 설정
        Vector3Int beforeWayPoint = wayPoint[0];

        for (int i = 1; i < wayPoint.Count; i++)
        {
            if (grid[wayPoint[i].x, wayPoint[i].y, wayPoint[i].z] == 1) // wayPoint가 도로 위에 있으면 다음 wayPoint로 넘어감
                continue;

            Vector3Int beforeDir = Vector3Int.zero;
            if (i != 1)
                beforeDir = pathDir[pathDir.Count - 1];

            findPath(beforeWayPoint, wayPoint[i], beforeDir);   // findPath가 aStar임
            if (!closedRoad)  // wayPoint까지 경로가 없으면 도로 생성을 중지
                return;

            beforeWayPoint = wayPoint[i];   // 이전 wayPoint를 현재 wayPoint로 바꿈 // 다음 루프에서 쓰기 위함

            // aStar에서 연산한 wayPoint 사이 도로를 전체 도로에 연결
            for (int j = 0; j < aPath.Count; j++)
            {
                path.Add(aPath[j]);
                pathDir.Add(aPathDir[j]);
            }
        }

        // 아래는 마지막 wayPoint와 시작 wayPoint를 잇는 코드
        grid[wayPoint[0].x, wayPoint[0].y, wayPoint[0].z] = 0;      // 시작점을 찾을 수 있게 그리드에서 시작점 도로를 없애줌

        foreach (Vector3Int dir in getDirections(pathDir[0] * -1))  // 시작점과 부드럽게 이어지는 지점 표시
            if (isInBounds(wayPoint[0] + dir))
                grid[wayPoint[0].x + dir.x, wayPoint[0].y + dir.y, wayPoint[0].z + dir.x] = 2;

        // A*
        findPath(beforeWayPoint, wayPoint[0], pathDir[pathDir.Count - 1]);
        if (!closedRoad)  // wayPoint까지 경로가 없으면 도로 생성을 중지
            return;

        // path data
        for (int j = 0; j < aPath.Count; j++)
        {
            path.Add(aPath[j]);
            pathDir.Add(aPathDir[j]);
        }

        // 리스트 연산 편의용
        path.Add(path[0]);
        path.Add(path[1]);
        pathDir.Add(pathDir[0]);
        pathDir.Add(pathDir[1]);
    }

    // 얘가 aStar 알고리즘임
    private void findPath(Vector3Int start, Vector3Int goal, Vector3Int beforeDir)
    {
        aPath.Clear();
        aPathDir.Clear();

        List<Node> openSet = new List<Node>();                      // 탐색 중인 노드
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();  // 방문 완료 노드
        Node startNode = new Node(start) { gCost = 0, hCost = getHCost(start, goal), dir = beforeDir };
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = getLowestFCostNode(openSet);         // 탐색 노드 중 가장 저비용인 노드를 현재노드로 선택

            if (currentNode.pos == goal)                            // 도착점 도달
            {
                reconstructPath(currentNode);                       // 이동경로(도로) 저장
                return;
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode.pos);

            foreach (Vector3Int dir in getDirections(currentNode.dir))
            {
                // 이웃한 지점 설정
                Vector3Int neighborPos = currentNode.pos + dir;

                // 이웃한 점이 방문완료 노드면 패스
                if (closedSet.Contains(neighborPos))
                    continue;

                // 이웃한 점이 범위 밖이면 패스
                if (!isInBounds(neighborPos))
                    continue;

                // 이동 경로 중 막혀서 갈 수 없는 길이면 패스
                if (isBlocked(currentNode, dir, closedSet))
                    continue;

                // 도로를 시작점에 연결할 때 현재 노드가 시작점까지 연결 불가능한 노드면 패스
                if (wayPoint[0] == neighborPos && grid[currentNode.pos.x, currentNode.pos.y, currentNode.pos.z] == 2)
                    continue;

                int tentativeG = currentNode.gCost + Mathf.Abs(dir.x) + Mathf.Abs(dir.y) + Mathf.Abs(dir.z); // 첫 노드부터 다음 노드까지 이동비용

                Node neighborNode = openSet.Find(n => n.pos == neighborPos);
                if (neighborNode == null)                       // 이웃노드가 탐색되지 않은 노드인 경우
                {
                    neighborNode = new Node(neighborPos)
                    {
                        gCost = tentativeG,
                        hCost = getHCost(neighborPos, goal),
                        dir = dir,                              // 이 노드가 이전 노드로부터 어느 방향으로 왔는지 저장
                        parent = currentNode                    // 현재 노드를 부모 노드로 설정 // 어떤 노드에서 왔는지 확인할 때 사용
                    };
                    openSet.Add(neighborNode);                  // 탐색 중인 노드 목록에 추가
                }
                else if (tentativeG < neighborNode.gCost)       // 이웃노드가 탐색 중인 노드이고 현재 이동 방법이 더 빠른(저비용인) 방법이면 현재 이동 방법으로 갱신
                {
                    neighborNode.gCost = tentativeG;
                    neighborNode.parent = currentNode;
                    neighborNode.dir = dir;
                }
            }
        }
        closedRoad = false;
    }

    // 탐색된 aStar 경로를 저장
    private void reconstructPath(Node endNode)
    {
        Node current = endNode;

        while (current.parent != null)
        {
            aPath.Add(current.parent.pos);
            aPathDir.Add(current.dir);
            block(current.parent.pos, current.dir);             // 그리드에 이동 불능점 표시
            current = current.parent;
        }
        grid[endNode.pos.x, endNode.pos.y, endNode.pos.z] = 0;  // 다음 A*를 위해 끝점 0으로 설정
        aPath.Reverse();
        aPathDir.Reverse();
    }

    // 도로가 /. 이런식으로 생길 때를 막기 위한 함수
    private void block(Vector3Int pos, Vector3Int dir)
    {
        if (isInBounds(new Vector3Int(pos.x, pos.y, pos.z)))
            grid[pos.x, pos.y, pos.z] = 1;

        if (isInBounds(new Vector3Int(pos.x + dir.x, pos.y, pos.z)))
            grid[pos.x + dir.x, pos.y, pos.z] = 1;

        if (isInBounds(new Vector3Int(pos.x, pos.y + dir.y, pos.z)))
            grid[pos.x, pos.y + dir.y, pos.z] = 1;

        if (isInBounds(new Vector3Int(pos.x, pos.y, pos.z + dir.z)))
            grid[pos.x, pos.y, pos.z + dir.z] = 1;

        if (isInBounds(new Vector3Int(pos.x + dir.x, pos.y + dir.y, pos.z)))
            grid[pos.x + dir.x, pos.y + dir.y, pos.z] = 1;

        if (isInBounds(new Vector3Int(pos.x, pos.y + dir.y, pos.z + dir.z)))
            grid[pos.x, pos.y + dir.y, pos.z + dir.z] = 1;

        if (isInBounds(new Vector3Int(pos.x + dir.x, pos.y, pos.z + dir.z)))
            grid[pos.x + dir.x, pos.y, pos.z + dir.z] = 1;

        if (isInBounds(new Vector3Int(pos.x + dir.x, pos.y + dir.y, pos.z + dir.z)))
            grid[pos.x + dir.x, pos.y + dir.y, pos.z + dir.z] = 1;

    }

    // 한 노드에서 갈 수 있는 모든 방향을 리스트로 반환함
    // 너무 급격히 꺽이는 노드는 제외
    private List<Vector3Int> getDirections(Vector3Int dir)
    {
        Vector3Int v0 = new Vector3Int(dir.x, 0, dir.z);
        Vector3Int v1 = Vector3Int.zero;
        Vector3Int v2 = Vector3Int.zero;
        int scale = 1;

        if (dir.x == 0 && dir.z != 0)
        {
            v1 = new Vector3Int(1, 0, 0);
            v2 = new Vector3Int(-1, 0, 0);
        }
        else if (dir.z == 0 && dir.x != 0)
        {
            v1 = new Vector3Int(0, 0, 1);
            v2 = new Vector3Int(0, 0, -1);
        }
        else if (dir.x * dir.z > 0)
        {
            v1 = new Vector3Int(1, 0, -1);
            v2 = new Vector3Int(-1, 0, 1);
            scale = 2;
        }
        else if (dir.x * dir.z < 0)
        {
            v1 = new Vector3Int(1, 0, 1);
            v2 = new Vector3Int(-1, 0, -1);
            scale = 2;
        }
        else // 이전 노드의 방향이 없는 경우
        {
            return new List<Vector3Int>
            {
                new Vector3Int(1, 0, 0),    new Vector3Int(1, 0, 1),    new Vector3Int(1, 0, -1),   // x+ y0
                new Vector3Int(1, 1, 0),    new Vector3Int(1, 1, 1),    new Vector3Int(1, 1, -1),   // x+ y+
                new Vector3Int(1, -1, 0),   new Vector3Int(1, -1, 1),   new Vector3Int(1, -1, -1),  // x+ y-
                new Vector3Int(-1, 0, 0),   new Vector3Int(-1, 0, 1),   new Vector3Int(-1, 0, -1),  // x- y0
                new Vector3Int(-1, 1, 0),   new Vector3Int(-1, 1, 1),   new Vector3Int(-1, 1, -1),  // x- y+
                new Vector3Int(-1, -1, 0),  new Vector3Int(-1, -1, 1),  new Vector3Int(-1, -1, -1), // x- y+
                new Vector3Int(0, 0, 1),    new Vector3Int(0, 0, -1),   // x0 y0
                new Vector3Int(0, 1, 1),    new Vector3Int(0, 1, -1),   // x0 y+
                new Vector3Int(0, -1, 1),   new Vector3Int(0, -1, -1)   // x0 y-
            };
        }

        List<Vector3Int> newDir = new List<Vector3Int>();

        if (dir.y == 0) // 평지
        {
            newDir.Add(v1);
            newDir.Add(v2);
            newDir.Add((v0 + v1) / scale);
            newDir.Add((v0 + v2) / scale);

            newDir.Add(v0);
            newDir.Add(v0 + Vector3Int.up);
            newDir.Add(v0 + Vector3Int.down);
        }
        else if (dir.y == 1) // 오르막
        {
            newDir.Add(v0);
            newDir.Add(v0 + Vector3Int.up);
        }
        else if (dir.y == -1) // 내리막
        {
            newDir.Add(v0);
            newDir.Add(v0 + Vector3Int.down);
        }

        return newDir;
    }

    // 앞으로 갈 길이 막혔는지 확인하는 코드 (ex 대각선)
    private bool isBlocked(Node current, Vector3Int dir, HashSet<Vector3Int> closedSet)
    {
        Vector3Int pos = current.pos;
        if ((isInBounds(new Vector3Int(pos.x, pos.y, pos.z)) && (grid[pos.x, pos.y, pos.z] == 1 || isParentNode(new Vector3Int(pos.x, pos.y, pos.z), current, closedSet))) || // 0 0 0
            (isInBounds(new Vector3Int(pos.x + dir.x, pos.y, pos.z)) && (grid[pos.x + dir.x, pos.y, pos.z] == 1 || isParentNode(new Vector3Int(pos.x + dir.x, pos.y, pos.z), current, closedSet))) || // x 0 0
            (isInBounds(new Vector3Int(pos.x, pos.y + dir.y, pos.z)) && (grid[pos.x, pos.y + dir.y, pos.z] == 1 || isParentNode(new Vector3Int(pos.x, pos.y + dir.y, pos.z), current, closedSet))) || // 0 y 0
            (isInBounds(new Vector3Int(pos.x, pos.y, pos.z + dir.z)) && (grid[pos.x, pos.y, pos.z + dir.z] == 1 || isParentNode(new Vector3Int(pos.x, pos.y, pos.z + dir.z), current, closedSet))) || // 0 0 z
            (isInBounds(new Vector3Int(pos.x + dir.x, pos.y + dir.y, pos.z)) && (grid[pos.x + dir.x, pos.y + dir.y, pos.z] == 1 || isParentNode(new Vector3Int(pos.x + dir.x, pos.y + dir.y, pos.z), current, closedSet))) || // x y 0
            (isInBounds(new Vector3Int(pos.x, pos.y + dir.y, pos.z + dir.z)) && (grid[pos.x, pos.y + dir.y, pos.z + dir.z] == 1 || isParentNode(new Vector3Int(pos.x, pos.y + dir.y, pos.z + dir.z), current, closedSet))) || // 0 y z
            (isInBounds(new Vector3Int(pos.x + dir.x, pos.y, pos.z + dir.z)) && (grid[pos.x + dir.x, pos.y, pos.z + dir.z] == 1 || isParentNode(new Vector3Int(pos.x + dir.x, pos.y, pos.z + dir.z), current, closedSet))) || // x 0 z
            (isInBounds(new Vector3Int(pos.x + dir.x, pos.y + dir.z, pos.z + dir.z)) && (grid[pos.x + dir.x, pos.y + dir.z, pos.z + dir.z] == 1 || isParentNode(new Vector3Int(pos.x + dir.x, pos.y + dir.z, pos.z + dir.z), current, closedSet)))) // x y z
            return true;

        return false;
    }

    // 해당 위치를 부모노드가 지나갔는지 확인
    private bool isParentNode(Vector3Int pos, Node current, HashSet<Vector3Int> closedSet)
    {
        if (closedSet.Contains(pos))
        {
            while (current.parent != null)
            {
                if (current.parent.pos == pos)
                    return true;
                current = current.parent;
            }
        }
        return false;
    }

    // 주어진 노드들 중 f(n) 값이 가장 작은 노드를 선택
    private Node getLowestFCostNode(List<Node> nodes)
    {
        Node best = nodes[0];
        foreach (Node node in nodes)
        {
            if (node.fCost < best.fCost || (node.fCost == best.fCost && node.hCost < best.hCost))
                best = node;
        }
        return best;
    }

    // aStar 휴리스틱 함수
    private int getHCost(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
    }

    // 지정된 위치가 그리드 범위 안에 있는지 검사하는 코드
    private bool isInBounds(Vector3Int pos)
    {
        return 0 <= pos.x && pos.x < gridSize.x &&
               0 <= pos.y && pos.y < gridSize.y &&
               0 <= pos.z && pos.z < gridSize.z;
    }

    // grid에 저장된 도로를 부드럽게 이어주는 함수
    private void setLine()
    {
        // 초기 설정
        Vector3 beforePos = path[0];
        Vector3 beforeDir = pathDir[0];
        Vector3 currentDir;
        Vector3 nextDir;

        for (int i = 1; i < path.Count - 1; i++)
        {
            currentDir = pathDir[i];
            nextDir = pathDir[i + 1];

            if (currentDir == beforeDir)
            {
                if (currentDir == nextDir)                  // 이전, 현재, 다음 셋의 방향이 같을 때 중간 노드를 연산하지 않고 직선으로 이음
                {
                    if (i == path.Count - 3)                // 마지막 항
                    {
                        roadPoint.RemoveAt(0);
                        roadDir.RemoveAt(0);
                        roadPoint.Add(beforePos + beforeDir * 0.5f);
                        roadDir.Add(beforeDir);
                    }
                    continue;
                }
                else if (beforePos.y != path[i].y)          // 이전이랑 현재의 높이가 다르면 보간으로 이음
                {
                    List<Vector3> curve = interpolation(beforePos + beforeDir * 0.5f, path[i] + currentDir * 0.5f, beforeDir.normalized, currentDir.normalized, 1);
                    for (int j = 0; j < curve.Count / 2; j++)
                    {
                        roadPoint.Add(curve[2 * j]);
                        roadDir.Add(curve[2 * j + 1]);
                    }
                }
                else                                        // 현재랑 다음이랑 방향이 다르면 이전과 현재를 직선으로 이음 (이전과 현재는 방향은 같은 상황)
                {
                    roadPoint.Add(beforePos + beforeDir * 0.5f);
                    roadDir.Add(beforeDir);
                }
            }
            else                                            // 이전이랑 현재랑 방향이 다르면 이전과 현재를 곡선으로 이음
            {
                List<Vector3> curve = interpolation(beforePos + beforeDir * 0.5f, path[i] + currentDir * 0.5f, beforeDir.normalized, currentDir.normalized, 1);
                for (int j = 0; j < curve.Count / 2; j++)
                {
                    roadPoint.Add(curve[2 * j]);
                    roadDir.Add(curve[2 * j + 1]);
                }
            }
            beforeDir = currentDir;
            beforePos = path[i];
        }
    }

    // 두 점을 부드럽게 이어주는 함수 (보간함수)
    private List<Vector3> interpolation(Vector3 vec1, Vector3 vec2, Vector3 vec1Grd, Vector3 vec2Grd, float scale)
    {
        List<Vector3> points = new List<Vector3>(); // 보간되어 찾아진 각 지점들의 위치와 방향을 저장하는 리스트

        float Xa = 2 * vec1.x - 2 * vec2.x + vec1Grd.x + vec2Grd.x;
        float Xb = -3 * vec1.x + 3 * vec2.x - 2 * vec1Grd.x - vec2Grd.x;
        float Xc = vec1Grd.x;
        float Xd = vec1.x;

        float Ya = 2 * vec1.y - 2 * vec2.y + vec1Grd.y + vec2Grd.y;
        float Yb = -3 * vec1.y + 3 * vec2.y - 2 * vec1Grd.y - vec2Grd.y;
        float Yc = vec1Grd.y;
        float Yd = vec1.y;

        float Za = 2 * vec1.z - 2 * vec2.z + vec1Grd.z + vec2Grd.z;
        float Zb = -3 * vec1.z + 3 * vec2.z - 2 * vec1Grd.z - vec2Grd.z;
        float Zc = vec1Grd.z;
        float Zd = vec1.z;

        for (float t = 0f; t <= 1f - smoothness / scale; t += smoothness / scale)
        {
            // 위치
            float x = (Xa * t * t * t) + (Xb * t * t) + (Xc * t) + Xd;
            float y = (Ya * t * t * t) + (Yb * t * t) + (Yc * t) + Yd;
            float z = (Za * t * t * t) + (Zb * t * t) + (Zc * t) + Zd;

            // 방향 (미분값)
            float dx = 3 * (Xa * t * t) + 2 * (Xb * t) + Xc;
            float dy = 3 * (Ya * t * t) + 2 * (Yb * t) + Yc;
            float dz = 3 * (Za * t * t) + 2 * (Zb * t) + Zc;

            points.Add(new Vector3(x, y, z));       // 홀수번째 : 위치
            points.Add(new Vector3(dx, dy, dz));    // 짝수번째 : 방향
        }

        return points;
    }

    // 부드러워진 도로(선)를 바탕으로 도로 그래픽을 만듦
    private void setMesh()
    {
        // 라인 설정
        GameObject lineC = new GameObject("Line Center");
        GameObject lineR = new GameObject("Line Right");
        GameObject lineL = new GameObject("Line Left");

        lineC.transform.SetParent(this.transform);
        lineR.transform.SetParent(this.transform);
        lineL.transform.SetParent(this.transform);

        // 메쉬 컴포넌트 설정
        MeshFilter meshFilterT = GetComponent<MeshFilter>();
        MeshRenderer rendererT = GetComponent<MeshRenderer>();
        rendererT.material = setMaterial(Color.gray);   // 도로색
        Mesh meshT = new Mesh();
        meshT.name = "TextureMesh";

        MeshFilter meshFilterLc = lineC.AddComponent<MeshFilter>();
        MeshRenderer rendererLc = lineC.AddComponent<MeshRenderer>();
        rendererLc.material = setMaterial(Color.white);     // 라인색 중앙
        Mesh meshLc = new Mesh();
        meshLc.name = "LineMeshCenter";

        MeshFilter meshFilterLr = lineR.AddComponent<MeshFilter>();
        MeshRenderer rendererLr = lineR.AddComponent<MeshRenderer>();
        rendererLr.material = setMaterial(Color.white);     // 라인색 오른쪽
        Mesh meshLr = new Mesh();
        meshLr.name = "LineMeshRight";

        MeshFilter meshFilterLl = lineL.AddComponent<MeshFilter>();
        MeshRenderer rendererLl = lineL.AddComponent<MeshRenderer>();
        rendererLl.material = setMaterial(Color.white);     // 라인색 왼쪽
        Mesh meshLl = new Mesh();
        meshLl.name = "LineMeshLeft";

        MeshCollider collider = GetComponent<MeshCollider>();
        Mesh meshC = new Mesh();
        meshC.name = "ColliderMesh";

        PhysicsMaterial physMat = new PhysicsMaterial();
        physMat.dynamicFriction = 0f;
        physMat.staticFriction = 0f;
        collider.material = physMat;

        // 변수 설정
        Vector3[] verticesT = new Vector3[roadPoint.Count * 4];
        int[] trianglesT = new int[roadPoint.Count * 24]; // 4 * 2 * 3

        Vector3[] verticesLc = new Vector3[roadPoint.Count * 2];
        int[] trianglesLc = new int[roadPoint.Count * 6]; // 1 * 2 * 3

        Vector3[] verticesLr = new Vector3[roadPoint.Count * 2];
        int[] trianglesLr = new int[roadPoint.Count * 6]; // 1 * 2 * 3

        Vector3[] verticesLl = new Vector3[roadPoint.Count * 2];
        int[] trianglesLl = new int[roadPoint.Count * 6]; // 1 * 2 * 3

        Vector3[] verticesC = new Vector3[roadPoint.Count * 4];
        int[] trianglesC = new int[roadPoint.Count * 24]; // 4 * 2 * 3

        // 버텍스 생성
        for (int i = 0; i < roadPoint.Count; i++)
        {
            Vector3 vecH = new Vector3(roadPoint[i].x, roadPoint[i].y * height, roadPoint[i].z);

            // [texture]
            verticesT[i * 4 + 0] = (vecH + Vector3.Cross(roadDir[i], Vector3.up).normalized * roadWidth * 0.5f + Vector3.down * roadHeight) * scale;
            verticesT[i * 4 + 1] = (vecH + Vector3.Cross(roadDir[i], Vector3.down).normalized * roadWidth * 0.5f + Vector3.down * roadHeight) * scale;
            verticesT[i * 4 + 2] = (vecH + Vector3.Cross(roadDir[i], Vector3.down).normalized * roadWidth * 0.5f ) * scale;
            verticesT[i * 4 + 3] = (vecH + Vector3.Cross(roadDir[i], Vector3.up).normalized * roadWidth * 0.5f) * scale;

            // [center line]
            verticesLc[i * 2 + 0] = (vecH + Vector3.Cross(roadDir[i], Vector3.up).normalized * lineWidth * 0.5f) * scale + Vector3.up * 0.01f;
            verticesLc[i * 2 + 1] = (vecH + Vector3.Cross(roadDir[i], Vector3.down).normalized * lineWidth * 0.5f) * scale + Vector3.up * 0.01f;

            // [right line]
            verticesLr[i * 2 + 0] = (vecH + Vector3.Cross(roadDir[i], Vector3.up).normalized * (roadWidth - 0.1f) * 0.5f) * scale + Vector3.up * 0.01f;
            verticesLr[i * 2 + 1] = (vecH + Vector3.Cross(roadDir[i], Vector3.up).normalized * (roadWidth - lineWidth * 2 - 0.1f) * 0.5f) * scale + Vector3.up * 0.01f;

            // [left line]
            verticesLl[i * 2 + 0] = (vecH + Vector3.Cross(roadDir[i], Vector3.down).normalized * (roadWidth - lineWidth * 2 - 0.1f) * 0.5f) * scale + Vector3.up * 0.01f;
            verticesLl[i * 2 + 1] = (vecH + Vector3.Cross(roadDir[i], Vector3.down).normalized * (roadWidth - 0.1f) * 0.5f) * scale + Vector3.up * 0.01f;

            // [collider]
            verticesC[i * 4 + 0] = (vecH + Vector3.Cross(roadDir[i], Vector3.up).normalized * roadWidth * 0.5f) * scale;
            verticesC[i * 4 + 1] = (vecH + Vector3.Cross(roadDir[i], Vector3.down).normalized * roadWidth * 0.5f) * scale;
            verticesC[i * 4 + 2] = (vecH + Vector3.Cross(roadDir[i], Vector3.down).normalized * roadWidth * 0.5f + Vector3.up * colliderHeight * height) * scale;
            verticesC[i * 4 + 3] = (vecH + Vector3.Cross(roadDir[i], Vector3.up).normalized * roadWidth * 0.5f + Vector3.up * colliderHeight * height) * scale;

        }

        // 폴리곤 생성
        int lenT = verticesT.Length;
        int lenL = verticesLc.Length;
        int lenLr = verticesLr.Length;
        int lenLl = verticesLl.Length;
        int lenC = verticesC.Length;

        for (int i = 0; i < roadPoint.Count; i++)
        {
            // [texture]
            trianglesT[i * 24 + 0] = (i * 4 + 0) % lenT; trianglesT[i * 24 + 1] = (i * 4 + 7) % lenT; trianglesT[i * 24 + 2] = (i * 4 + 3) % lenT;
            trianglesT[i * 24 + 3] = (i * 4 + 0) % lenT; trianglesT[i * 24 + 4] = (i * 4 + 4) % lenT; trianglesT[i * 24 + 5] = (i * 4 + 7) % lenT;
            trianglesT[i * 24 + 6] = (i * 4 + 0) % lenT; trianglesT[i * 24 + 7] = (i * 4 + 5) % lenT; trianglesT[i * 24 + 8] = (i * 4 + 4) % lenT;
            trianglesT[i * 24 + 9] = (i * 4 + 0) % lenT; trianglesT[i * 24 + 10] = (i * 4 + 1) % lenT; trianglesT[i * 24 + 11] = (i * 4 + 5) % lenT;
            trianglesT[i * 24 + 12] = (i * 4 + 2) % lenT; trianglesT[i * 24 + 13] = (i * 4 + 5) % lenT; trianglesT[i * 24 + 14] = (i * 4 + 1) % lenT;
            trianglesT[i * 24 + 15] = (i * 4 + 2) % lenT; trianglesT[i * 24 + 16] = (i * 4 + 6) % lenT; trianglesT[i * 24 + 17] = (i * 4 + 5) % lenT;
            trianglesT[i * 24 + 18] = (i * 4 + 2) % lenT; trianglesT[i * 24 + 19] = (i * 4 + 7) % lenT; trianglesT[i * 24 + 20] = (i * 4 + 6) % lenT;
            trianglesT[i * 24 + 21] = (i * 4 + 2) % lenT; trianglesT[i * 24 + 22] = (i * 4 + 3) % lenT; trianglesT[i * 24 + 23] = (i * 4 + 7) % lenT;

            // [center line]
            trianglesLc[i * 6 + 0] = (i * 2 + 0) % lenL; trianglesLc[i * 6 + 1] = (i * 2 + 3) % lenL; trianglesLc[i * 6 + 2] = (i * 2 + 1) % lenL;
            trianglesLc[i * 6 + 3] = (i * 2 + 0) % lenL; trianglesLc[i * 6 + 4] = (i * 2 + 2) % lenL; trianglesLc[i * 6 + 5] = (i * 2 + 3) % lenL;

            // [right line]
            trianglesLr[i * 6 + 0] = (i * 2 + 0) % lenLr; trianglesLr[i * 6 + 1] = (i * 2 + 3) % lenLr; trianglesLr[i * 6 + 2] = (i * 2 + 1) % lenLr;
            trianglesLr[i * 6 + 3] = (i * 2 + 0) % lenLr; trianglesLr[i * 6 + 4] = (i * 2 + 2) % lenLr; trianglesLr[i * 6 + 5] = (i * 2 + 3) % lenLr;

            // [left line]
            trianglesLl[i * 6 + 0] = (i * 2 + 0) % lenLl; trianglesLl[i * 6 + 1] = (i * 2 + 3) % lenLl; trianglesLl[i * 6 + 2] = (i * 2 + 1) % lenLl;
            trianglesLl[i * 6 + 3] = (i * 2 + 0) % lenLl; trianglesLl[i * 6 + 4] = (i * 2 + 2) % lenLl; trianglesLl[i * 6 + 5] = (i * 2 + 3) % lenLl;

            // [collider]
            trianglesC[i * 24 + 0] = (i * 4 + 0) % lenC; trianglesC[i * 24 + 1] = (i * 4 + 3) % lenC; trianglesC[i * 24 + 2] = (i * 4 + 7) % lenC;
            trianglesC[i * 24 + 3] = (i * 4 + 0) % lenC; trianglesC[i * 24 + 4] = (i * 4 + 7) % lenC; trianglesC[i * 24 + 5] = (i * 4 + 4) % lenC;
            trianglesC[i * 24 + 6] = (i * 4 + 0) % lenC; trianglesC[i * 24 + 7] = (i * 4 + 4) % lenC; trianglesC[i * 24 + 8] = (i * 4 + 5) % lenC;
            trianglesC[i * 24 + 9] = (i * 4 + 0) % lenC; trianglesC[i * 24 + 10] = (i * 4 + 5) % lenC; trianglesC[i * 24 + 11] = (i * 4 + 1) % lenC;
            trianglesC[i * 24 + 12] = (i * 4 + 2) % lenC; trianglesC[i * 24 + 13] = (i * 4 + 1) % lenC; trianglesC[i * 24 + 14] = (i * 4 + 5) % lenC;
            trianglesC[i * 24 + 15] = (i * 4 + 2) % lenC; trianglesC[i * 24 + 16] = (i * 4 + 5) % lenC; trianglesC[i * 24 + 17] = (i * 4 + 6) % lenC;
            trianglesC[i * 24 + 18] = (i * 4 + 2) % lenC; trianglesC[i * 24 + 19] = (i * 4 + 6) % lenC; trianglesC[i * 24 + 20] = (i * 4 + 7) % lenC;
            trianglesC[i * 24 + 21] = (i * 4 + 2) % lenC; trianglesC[i * 24 + 22] = (i * 4 + 7) % lenC; trianglesC[i * 24 + 23] = (i * 4 + 3) % lenC;
        }

        // [texture]
        meshT.vertices = verticesT;
        meshT.triangles = trianglesT;
        meshT.RecalculateNormals();
        meshFilterT.mesh = meshT;

        // [center line]
        meshLc.vertices = verticesLc;
        meshLc.triangles = trianglesLc;
        meshLc.RecalculateNormals();
        meshFilterLc.mesh = meshLc;

        // [right line]
        meshLr.vertices = verticesLr;
        meshLr.triangles = trianglesLr;
        meshLr.RecalculateNormals();
        meshFilterLr.mesh = meshLr;

        // [left line]
        meshLl.vertices = verticesLl;
        meshLl.triangles = trianglesLl;
        meshLl.RecalculateNormals();
        meshFilterLl.mesh = meshLl;

        // [collider]
        meshC.vertices = verticesC;
        meshC.triangles = trianglesC;
        meshC.RecalculateNormals();
        collider.sharedMesh = meshC;
    }

    private Material setMaterial(Color color)
    {
        Material newMat = new Material(Shader.Find("Standard"));
        newMat.color = color;
        newMat.SetFloat("_Metallic", 0f);
        newMat.SetFloat("_Glossiness", 0f);
        return newMat;
    }

    // 건물 배치 함수
    private void build()
    {
        // object
        GameObject building = new GameObject("Building");
        building.transform.SetParent(this.transform);
        Transform buildingParent = building.GetComponent<Transform>();

        // scale
        buildingParent.localScale = Vector3.one * scale;

        // 설치 불가능한 지역 검사 // 0 설치 가능 // 1 설치 불가능
        Vector3Int pos = new Vector3Int(path[0].x + 2, path[0].y, path[0].z + 2);
        foreach (var i in pathDir)
        {
            map[pos.x, pos.z] = 1;
            map[pos.x + i.x, pos.z] = 1;
            map[pos.x, pos.z + i.z] = 1;
            pos += i;
        }

        // 랜드마크 설치
        Vector3Int landPos = gridSize / 2 + Vector3Int.right * 7;
        Instantiate(Landmark, (landPos + Vector3.down * landPos.y) * scale, Quaternion.identity, buildingParent);

        // 건물 설치
        for (int x = 0; x < gridSize.x + 4; x++)
            for (int z = 0; z < gridSize.z + 4; z++)
            {
                // O형
                if (isInMap(new Vector3Int(x + 1, 0, z + 1)) && map[x, z] == 0 && map[x + 1, z] == 0 && map[x, z + 1] == 0 && map[x + 1, z + 1] == 0) // O
                {
                    Instantiate(building_O[Random.Range(0, building_O.Count)], new Vector3(x - 1.5f, 0, z - 1.5f) * scale, Quaternion.Euler(0, Random.Range(0, 4) * 90, 0), buildingParent);
                    map[x, z] = 1;
                    map[x + 1, z] = 1;
                    map[x, z + 1] = 1;
                    map[x + 1, z + 1] = 1;
                }
                // L형
                else if (isInMap(new Vector3Int(x + 1, 0, z + 1)) && map[x + 1, z] == 0 && map[x, z + 1] == 0 && map[x + 1, z + 1] == 0) // void x0 z0
                {
                    Instantiate(building_L[Random.Range(0, building_L.Count)], new Vector3(x - 1.5f, 0, z - 1.5f) * scale, Quaternion.Euler(0, 0, 0), buildingParent);
                    map[x + 1, z] = 1;
                    map[x, z + 1] = 1;
                    map[x + 1, z + 1] = 1;
                }
                else if (isInMap(new Vector3Int(x + 1, 0, z + 1)) && map[x, z] == 0 && map[x, z + 1] == 0 && map[x + 1, z + 1] == 0) // void x+ z0
                {
                    Instantiate(building_L[Random.Range(0, building_L.Count)], new Vector3(x - 1.5f, 0, z - 1.5f) * scale, Quaternion.Euler(0, 270, 0), buildingParent);
                    map[x, z] = 1;
                    map[x, z + 1] = 1;
                    map[x + 1, z + 1] = 1;
                }
                else if (isInMap(new Vector3Int(x + 1, 0, z + 1)) && map[x, z] == 0 && map[x + 1, z] == 0 && map[x + 1, z + 1] == 0) // void x0 z+
                {
                    Instantiate(building_L[Random.Range(0, building_L.Count)], new Vector3(x - 1.5f, 0, z - 1.5f) * scale, Quaternion.Euler(0, 90, 0), buildingParent);
                    map[x, z] = 1;
                    map[x + 1, z] = 1;
                    map[x + 1, z + 1] = 1;
                }
                else if (isInMap(new Vector3Int(x + 1, 0, z + 1)) && map[x, z] == 0 && map[x + 1, z] == 0 && map[x, z + 1] == 0) // void x+ z+
                {
                    Instantiate(building_L[Random.Range(0, building_L.Count)], new Vector3(x - 1.5f, 0, z - 1.5f) * scale, Quaternion.Euler(0, 180, 0), buildingParent);
                    map[x, z] = 1;
                    map[x + 1, z] = 1;
                    map[x, z + 1] = 1;
                }
                // I형
                else if (isInMap(new Vector3Int(x + 1, 0, z)) && map[x, z] == 0 && map[x + 1, z] == 0) // x+
                {
                    Instantiate(building_I[Random.Range(0, building_I.Count)], new Vector3(x - 1.5f, 0, z - 1.5f) * scale, Quaternion.Euler(0, 0, 0), buildingParent);
                    map[x, z] = 1;
                    map[x + 1, z] = 1;
                }
                else if (isInMap(new Vector3Int(x, 0, z + 1)) && map[x, z] == 0 && map[x, z + 1] == 0) // z+
                {
                    Instantiate(building_I[Random.Range(0, building_I.Count)], new Vector3(x - 1.5f, 0, z - 1.5f) * scale, Quaternion.Euler(0, 90, 0), buildingParent);
                    map[x, z] = 1;
                    map[x, z + 1] = 1;
                }
                // dot
                else if (map[x, z] == 0) // dot
                {
                    Instantiate(building_dot[Random.Range(0, building_dot.Count)], new Vector3(x - 2f, 0, z - 2f) * scale, Quaternion.Euler(0, Random.Range(0, 4) * 90, 0), buildingParent);
                    map[x, z] = 1;
                }
            }
    }
    
    // 지정된 위치가 그리드 범위 안에 있는지 검사하는 코드 (건물 배치용)
    private bool isInMap(Vector3Int pos)
    {
        return 0 <= pos.x && pos.x < gridSize.x + 4 &&
               0 <= pos.y && pos.y < gridSize.y &&
               0 <= pos.z && pos.z < gridSize.z + 4;
    }

}
