using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// RoadGenerator.cs

/// <summary>
/// <버그>
/// a* wayPoint 누락 버그 수정 ### 핵심버그
/// => a star 도달 불능점
/// => 맵을 크게 만들고 wayPoint 간 거리 줄이면 발생 확률 낮아짐
/// 
/// 첫부분 급코너 수정 ## 수정 된건가?? >> 아님
/// 
/// 뭔가 a*에 문제가 있나?
/// 아니 길이 뚫려있는데 왜 못찾아가
/// 
/// 
/// 
/// <추가>
/// 
/// 
/// <할거>
/// 튜토리얼용 맵 뽑기
/// 
/// <부분 완료>
/// 시작부 직선코스 
/// A* 안전 공간 미확보 수정
/// 
/// 
/// </summary>


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

// 디버깅 용이라 적힌건 무시하셈
// 건물 배치 코드 만들기 위해 나중에 코드가 좀 바뀔 수도 있음
// 그리고 왜인지는 모르겠는데 가끔 도로가 꼬이는 버그가 발생함 // 그거 수정해야됨
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))] // 메쉬관련 컴포넌트가 없으면 강제 추가
public class RoadGenerator : MonoBehaviour
{
    // 맵 설정
    public Vector3Int gridSize = new Vector3Int(40, 7, 40); // 그리드 사이즈
    public float height = 1f;   // 도로 간의 높이 // 정배율은 1인데 개인적으로 0.5 쯤 돼야 도로가 이쁜듯
    public float roadHeight = 0.1f; // 도로 두께
    public float roadWidth = 0.25f; // 도로 너비
    public float guardRailHeight = 0.1f; // 가드레일 높이
    public float guardRailWidth = 0.05f; // 가드레일 두께
    public float smoothness = 0.125f; // 곡면을 얼마나 부드럽게 이을건지 설정 // 0 ~ 1 사이 값만 허용 // 절대 0 넣지 말기
    public int startLength = 3; // 처음 직선 구간 길이
    public int maxWayPoint = 30;    // wayPoint의 최대 개수
    public float minDistance = 10f;  // wayPoint 사이의 최소거리
    public Transform buildingParent;       // 건물이 생성될 부모 오브젝트
    public GameObject Landmark;             // 랜드마크 
    public List<GameObject> building_O;     // 2 * 2 건물
    public List<GameObject> building_I;     // 1 * 2 건물
    public List<GameObject> building_L;     // 2 * 2 꺽인 건물
    public List<GameObject> building_dot;   // 1 * 1 건물
    public GameObject roadPrefab; // 디버깅용
    public Transform parent; // 디버깅용

    // 내부 변수
    private byte[,,] grid; // void 0 // road 1 // 시작점까지 이동 가능한 점 2
    public List<byte> gridList; // 디버깅 용
    private List<Vector3Int> path = new List<Vector3Int>();     // 도로가 시작점부터 순서대로 그리드의 어느 좌표로 이동하는지를 저장함
    private List<Vector3Int> pathDir = new List<Vector3Int>();  // 얘는 도로의 방향을 저장함 // path랑 pathDir에서 리스트 내 위치가 같으면 같은 도로임
    private List<Vector3Int> aPath = new List<Vector3Int>();    // path랑 똑같지만 aStar를 실행할 때 부분적으로만(각 wayPoint 사이) 작동함
    private List<Vector3Int> aPathDir = new List<Vector3Int>();
    private List<Vector3> roadPoint = new List<Vector3>();  // 얘는 보간이 적용된 path라고 생각하면 됨
    private List<Vector3> roadDir = new List<Vector3>();
    private List<Vector3Int> wayPoint = new List<Vector3Int>();
    private byte[,] map; // 건물이 설치될 위치를 구하기 위한 그리드

    private void Start()
    {
        if (smoothness == 0)    // smoothness 값은 0이 되면 코드가 무한 반복 됨
            smoothness = 0.01f; // 그거 방지용 코드

        //gridSize += new Vector3Int(2, 2, 2); // 외부 여유 그리드 확보
        grid = new byte[gridSize.x, gridSize.y, gridSize.z]; // 그리드 생성
        map = new byte[gridSize.x + 4, gridSize.z + 4];
        setLandmark(); // 랜드마크 생성
        generateWayPoints(); // wayPoint 생성
        generateRoadMap(); // wayPoint를 잇는 도로를 그리드 위에 생성

        gridList = new List<byte>(); // 디버깅용
        setLine(); // 그리드에 그려진 도로를 보간법을 이용해 부드럽게 이어줌
        setMesh(); // 보간된 도로를 그래픽으로 변환


        // 디버깅
        Debug.DrawLine(path[0], path[0] + Vector3.up * 10, Color.yellow, 100f);
        for (int i = 0; i < path.Count; i++)
        {
            //Debug.DrawLine(path[i], path[i] + Vector3.up, Color.green, 100f); // road
            Instantiate(roadPrefab, path[i], Quaternion.identity, parent); // block
        }
        foreach (var p in roadPoint)
        {
            Vector3 q = new Vector3(p.x, p.y * height, p.z);
            //Debug.DrawLine(q, q + Vector3.up, Color.red, 100f); // vertex line
        }
        for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
                for (int z = 0; z < grid.GetLength(2); z++)
                {
                    gridList.Add(grid[x, y, z]);
                    if (grid[x, y, z] == 1)
                    {
                        Debug.DrawLine(new Vector3(x, y * height, z), new Vector3(x, y * height, z) + Vector3.up * 0.25f, Color.blue, 100f); // 장애물
                    }
                    else if (grid[x, y, z] == 2)
                    {
                        //Debug.DrawLine(new Vector3(x, y * height, z), new Vector3(x, y * height, z) + Vector3.up * 1f, Color.green, 100f); // 장애물
                    }
                }

        build(); // 주변 건물 설치

    }

    private void setLandmark()
    {
        Vector3Int landPos = gridSize / 2 + Vector3Int.right * 7;
        Instantiate(Landmark, landPos + Vector3.down * landPos.y, Quaternion.identity, buildingParent);
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
            //if (aPath == null)  // wayPoint까지 경로가 없으면 다음 wayPoint로 넘어감
            //    continue;

            beforeWayPoint = wayPoint[i];   // 이전 wayPoint를 현재 wayPoint로 바꿈 // 다음 루프에서 쓰기 위함

            // aStar에서 연산한 wayPoint 사이 도로를 전체 도로에 연결
            for (int j = 0; j < aPath.Count; j++)
            {
                path.Add(aPath[j]);
                pathDir.Add(aPathDir[j]);
            }
        }

        // 아래는 마지막 wayPoint와 시작 wayPoint를 잇는 코드

        // 시작점을 찾을 수 있게 그리드에서 시작점 도로를 없애줌
        grid[wayPoint[0].x, wayPoint[0].y, wayPoint[0].z] = 0;

        // 시작점과 부드럽게 이어지는 지점 표시
        Debug.DrawLine(path[0], path[0] + pathDir[0] * 5, Color.red, 100f); // 디버깅 용
        foreach (Vector3Int dir in getDirections(pathDir[0] * -1))
            if (isInBounds(wayPoint[0] + dir))
            {
                grid[wayPoint[0].x + dir.x, wayPoint[0].y + dir.y, wayPoint[0].z + dir.x] = 2;
                Debug.DrawLine(wayPoint[0] + dir, wayPoint[0], Color.green, 100f); // 시작점과 연결되는 노드 위치
            }

        // A*
        findPath(beforeWayPoint, wayPoint[0], pathDir[pathDir.Count - 1]);

        //grid[wayPoint[0].x, wayPoint[0].y, wayPoint[0].z] = 1;  // 시작점에 다시 도로로 변환 대입

        for (int j = 0; j < aPath.Count; j++)
        {
            //Debug.DrawLine(aPath[j], aPath[j] + Vector3.up * 5, Color.green, 100f); // 디버깅 용
            path.Add(aPath[j]);
            pathDir.Add(aPathDir[j]);
        }

        // 나중에 리스트 연산을 편하게 하기 위해 첫 두점을 마지막에도 추가
        path.Add(path[0]);
        path.Add(path[1]);
        pathDir.Add(pathDir[0]);
        pathDir.Add(pathDir[1]);

        // 디버깅 용
        for (int i = 0; i < wayPoint.Count; i++)
            Debug.DrawLine(wayPoint[i], wayPoint[i] + Vector3.up * 5, Color.cyan, 100f);
    }

    // 얘가 aStar 알고리즘임
    private void findPath(Vector3Int start, Vector3Int goal, Vector3Int beforeDir)
    {
        // 초기 설정
        aPath.Clear();      // 리스트 초기화
        aPathDir.Clear();   // 리스트 초기화
        List<Node> openSet = new List<Node>();  // 탐색 중인 노드
        List<Vector3Int> closedSetDebug = new List<Vector3Int>();  // 디버깅
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>(); // 방문 완료 노드
        Node startNode = new Node(start) { gCost = 0, hCost = getHCost(start, goal), dir = beforeDir };
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = getLowestFCostNode(openSet); // 탐색 노드 중 가장 저비용인 노드를 현재노드로 선택

            if (currentNode.pos == goal) // 도착점 도달
            {
                reconstructPath(currentNode); // 이동경로(도로) 저장
                return;
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode.pos);
            closedSetDebug.Add(currentNode.pos);

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
                if (isBlocked(currentNode.pos, dir))
                    continue;

                // 도로를 시작점에 연결할 때 현재 노드가 시작점까지 연결 불가능한 노드면 패스
                if (wayPoint[0] == neighborPos && grid[currentNode.pos.x, currentNode.pos.y, currentNode.pos.z] == 2)
                {
                    Debug.DrawLine(currentNode.pos, neighborPos, Color.red, 100f);
                    Debug.Log("aaa");
                    continue;
                }

                int tentativeG = currentNode.gCost + Mathf.Abs(dir.x) + Mathf.Abs(dir.y) + Mathf.Abs(dir.z); // 첫 노드부터 다음 노드까지 이동비용

                Node neighborNode = openSet.Find(n => n.pos == neighborPos);
                if (neighborNode == null)   // 이웃노드가 탐색중이지 않은 노드인 경우
                {
                    neighborNode = new Node(neighborPos)
                    {
                        // 비용 저장
                        gCost = tentativeG,
                        hCost = getHCost(neighborPos, goal),

                        dir = dir, // 이 노드가 이전 노드로부터 어느 방향으로 왔는지 저장
                        parent = currentNode // 현재 노드를 부모 노드로 설정 // 어떤 노드에서 왔는지 확인할 때 사용
                    };
                    openSet.Add(neighborNode); // 탐색 중인 노드 목록에 추가
                }
                else if (tentativeG < neighborNode.gCost) // 이웃노드가 탐색 중인 노드이고 현재 이동 방법이 더 빠른(저비용인) 방법이면 현재 이동 방법으로 갱신
                {
                    neighborNode.gCost = tentativeG;
                    neighborNode.parent = currentNode;
                }
            }
        }
        
        foreach (Vector3Int i in closedSetDebug)
        {
            Debug.DrawLine(i, i + Vector3.up * 0.5f, Color.red, 100f);
        }
        
        Debug.Log(start);
        //return; // 경로 없음
    }

    // 탐색된 aStar 경로를 저장
    private void reconstructPath(Node endNode)
    {
        // 초기 설정
        Node current = endNode;

        while (current.parent != null)
        {
            aPath.Add(current.parent.pos);
            aPathDir.Add(current.dir);
            block(current.parent.pos, current.dir); // 그리드에 이동 불능점 표시
            current = current.parent;
        }
        grid[endNode.pos.x, endNode.pos.y, endNode.pos.z] = 0; // 다음 A*를 위해 끝점 0으로 설정
        aPath.Reverse();
        aPathDir.Reverse();
    }

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

    // aStar의 비용 계산식 : f(n) = g(n) + h(n)
    // f(n) : 해당 노드의 비용 // g(n) : 시작점부터 해당 노드까지 이동하는데 필요한 비용
    // h(n) : 휴리스틱함수, 효율적인 길 탐색을 위한 보정치, 단 휴리스틱이 적용되면 탐색된 길이 최단경로가 아닐 수 있음
    //        단지 탐색 과정의 효율을 위해 사용
    // h(n) = 0 으로 만들면 최단경로를 얻을 수 있음 (미방문 노드의 비용은 무한대 또는 그에 준하는 아주 큰 값) // 이게 다익스트라 알고리즘

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
    // a, b 사이의 (택시) 거리값 반환
    // 택시 거리 : 직교로만 움직일 수 있을 때의 거리 
    // ex) 원점과 (4,3) 사이 거리
    // 유클리드 거리 : 5 = {(4-0)^2 + (3-0)^2} ^ 0.5 // 택시 거리 : 7 = |4-0| + |3-0|
    private int getHCost(Vector3Int a, Vector3Int b)    // a b 중 하나는 현재 노드고 다른 하나는 다음 노드임
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

    // 한 노드에서 갈 수 있는 모든 방향을 리스트로 반환함
    // 너무 급격히 꺽이는 노드는 제외
    private List<Vector3Int> getDirections(Vector3Int dir)
    {
        // 기본 설정
        Vector3Int v0 = new Vector3Int(dir.x, 0, dir.z);
        Vector3Int v1 = Vector3Int.zero;
        Vector3Int v2 = Vector3Int.zero;
        int scale = 1;

        // v1 v2는 dir의 법선벡터
        // scale은 3 * 3 단위 격자 안에 넣기 위해 크기 조정용으로 넣은거임
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

    // 앞으로 갈 길이 막혔는지 확인하는 코드
    // 예를들어 대각선을 이동할때 대각선 옆방향이 비었는지 확인하는 코드임
    // <이동가능>   <이동 불가능>
    //  0 /          1 /
    //  / 0          / 1        (0 void // 1 장애물)
    private bool isBlocked(Vector3Int current, Vector3Int dir)
    {
        if ((isInBounds(new Vector3Int(current.x, current.y, current.z)) && grid[current.x, current.y, current.z] == 1) || // 0 0 0
            (isInBounds(new Vector3Int(current.x + dir.x, current.y, current.z)) && grid[current.x + dir.x, current.y, current.z] == 1) || // x 0 0
            (isInBounds(new Vector3Int(current.x, current.y + dir.y, current.z)) && grid[current.x, current.y + dir.y, current.z] == 1) || // 0 y 0
            (isInBounds(new Vector3Int(current.x, current.y, current.z + dir.z)) && grid[current.x, current.y, current.z + dir.z] == 1) || // 0 0 z
            (isInBounds(new Vector3Int(current.x + dir.x, current.y + dir.y, current.z)) && grid[current.x + dir.x, current.y + dir.y, current.z] == 1) || // x y 0
            (isInBounds(new Vector3Int(current.x, current.y + dir.y, current.z + dir.z)) && grid[current.x, current.y + dir.y, current.z + dir.z] == 1) || // 0 y z
            (isInBounds(new Vector3Int(current.x + dir.x, current.y, current.z + dir.z)) && grid[current.x + dir.x, current.y, current.z + dir.z] == 1) || // x 0 z
            (isInBounds(new Vector3Int(current.x + dir.x, current.y + dir.z, current.z + dir.z)) && grid[current.x + dir.x, current.y + dir.z, current.z + dir.z] == 1)) // x y z
            return true;

        return false;
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
                if (currentDir == nextDir)  // 이전, 현재, 다음 셋의 방향이 같을 때 연산량을 줄이기 위해
                {                           // 중간 노드를 연산하지 않고 직선으로 이음
                    if (i == path.Count - 3)    // 마지막 항
                    {
                        roadPoint.RemoveAt(0);
                        roadDir.RemoveAt(0);
                        roadPoint.Add(beforePos + beforeDir * 0.5f);
                        roadDir.Add(beforeDir);
                    }
                    continue;   // 일반항
                }
                else if (beforePos.y != path[i].y)  // 좀 헷갈리게 써놓긴 했는데 [i]면 현재고 [i+1]이면 다음임
                {                                   // 이전이랑 현재의 높이가 다르면 보간으로 이음
                    List<Vector3> curve = interpolation(beforePos + beforeDir * 0.5f, path[i] + currentDir * 0.5f, beforeDir.normalized, currentDir.normalized, 1);
                    for (int j = 0; j < curve.Count / 2; j++)
                    {
                        roadPoint.Add(curve[2 * j]);
                        roadDir.Add(curve[2 * j + 1]);
                    }
                }
                else // 현재랑 다음이랑 방향이 다르면 이전과 현재를 직선으로 이음 (이전과 현재는 방향은 같은 상황)
                {
                    roadPoint.Add(beforePos + beforeDir * 0.5f);
                    roadDir.Add(beforeDir);
                }
            }
            else // 이전이랑 현재랑 방향이 다르면 이전과 현재를 곡선으로 이음
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

    // 두 점을 부드럽게 이어주는 함수
    // vec1, vec2는 두 점의 위치이고 vec1Grd, vec2Grd는 두 점(도로)에서 방향 값임 // Grd는 gradient의 약어
    // 엄밀히 말하면 vec1Grd, vec2Grd는 각 점에서 미분값(기울기 값 또는 변화량)을 의미하는데 그게 방향이랑 같음
    // scale 값은 경사에 쓰려고 넣었는데 안쓰는 중 // 하지만 나중에 수정할까봐 남겨놓음
    // 다항함수 보간법을 조금 개량해서 만든 보간법
    private List<Vector3> interpolation(Vector3 vec1, Vector3 vec2, Vector3 vec1Grd, Vector3 vec2Grd, float scale)
    {
        List<Vector3> points = new List<Vector3>(); // 보간되어 찾아진 각 지점들의 위치와 방향을 저장하는 리스트

        // x에 대한 계수값
        float Xa = 2 * vec1.x - 2 * vec2.x + vec1Grd.x + vec2Grd.x;
        float Xb = -3 * vec1.x + 3 * vec2.x - 2 * vec1Grd.x - vec2Grd.x;
        float Xc = vec1Grd.x;
        float Xd = vec1.x;

        // y에 대한 계수값
        float Ya = 2 * vec1.y - 2 * vec2.y + vec1Grd.y + vec2Grd.y;
        float Yb = -3 * vec1.y + 3 * vec2.y - 2 * vec1Grd.y - vec2Grd.y;
        float Yc = vec1Grd.y;
        float Yd = vec1.y;

        // z에 대한 계수값
        float Za = 2 * vec1.z - 2 * vec2.z + vec1Grd.z + vec2Grd.z;
        float Zb = -3 * vec1.z + 3 * vec2.z - 2 * vec1Grd.z - vec2Grd.z;
        float Zc = vec1Grd.z;
        float Zd = vec1.z;

        for (float t = 0f; t <= 1f - smoothness / scale; t += smoothness / scale) // 매개변수 t(0 <= t <= 1)는 xyz 값이 균일해지도록 하는 기준 역할을 함
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
                                                    // 이렇게 위치가 홀짝으로 나뉜 이유는 원래 위치만 저장했지만 방향도 필요해져서 이렇게 나눔
                                                    // 따로 return에 넣을 때 따로 나눠서 넣으면 코드가 더러워짐
        }

        return points; // 반환
    }

    // 부드러워진 도로(선)를 바탕으로 도로 그래픽을 만듦
    private void setMesh()
    {
        // 메쉬 컴포넌트 설정
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshCollider collider = GetComponent<MeshCollider>();
        Mesh mesh = new Mesh();
        mesh.name = "RoadMesh";

        // 변수 설정
        Vector3[] vertices = new Vector3[roadPoint.Count * 8];
        int[] triangles = new int[roadPoint.Count * 48]; // 8 * 2 * 3

        // 버텍스(꼭짓점) 생성
        for (int i = 0; i < roadPoint.Count; i++)
        {
            Vector3 vecH = new Vector3(roadPoint[i].x, roadPoint[i].y * height, roadPoint[i].z);

            vertices[i * 8 + 0] = vecH + Vector3.Cross(roadDir[i], Vector3.up).normalized * (roadWidth + guardRailWidth) * 0.5f + Vector3.up * guardRailHeight;
            vertices[i * 8 + 1] = vecH + Vector3.Cross(roadDir[i], Vector3.up).normalized * roadWidth * 0.5f + Vector3.up * guardRailHeight;
            vertices[i * 8 + 2] = vecH + Vector3.Cross(roadDir[i], Vector3.up).normalized * roadWidth * 0.5f;
            vertices[i * 8 + 3] = vecH + Vector3.Cross(roadDir[i], Vector3.down).normalized * roadWidth * 0.5f;
            vertices[i * 8 + 4] = vecH + Vector3.Cross(roadDir[i], Vector3.down).normalized * roadWidth * 0.5f + Vector3.up * guardRailHeight;
            vertices[i * 8 + 5] = vecH + Vector3.Cross(roadDir[i], Vector3.down).normalized * (roadWidth + guardRailWidth) * 0.5f + Vector3.up * guardRailHeight;
            vertices[i * 8 + 6] = vecH + Vector3.Cross(roadDir[i], Vector3.down).normalized * (roadWidth + guardRailWidth) * 0.5f + Vector3.down * roadHeight;
            vertices[i * 8 + 7] = vecH + Vector3.Cross(roadDir[i], Vector3.up).normalized * (roadWidth + guardRailWidth) * 0.5f + Vector3.down * roadHeight;
        }

        // 버텍스를 바탕으로 폴리곤(삼각형) 생성
        for (int i = 0; i < roadPoint.Count; i++)
        {
            // triangle
            int len = vertices.Length;
            triangles[i * 48 + 0] = (i * 8 + 8) % len; triangles[i * 48 + 1] = (i * 8 + 0) % len; triangles[i * 48 + 2] = (i * 8 + 7) % len; //
            triangles[i * 48 + 3] = (i * 8 + 8) % len; triangles[i * 48 + 4] = (i * 8 + 7) % len; triangles[i * 48 + 5] = (i * 8 + 15) % len; //
            triangles[i * 48 + 6] = (i * 8 + 8) % len; triangles[i * 48 + 7] = (i * 8 + 9) % len; triangles[i * 48 + 8] = (i * 8 + 1) % len; //
            triangles[i * 48 + 9] = (i * 8 + 8) % len; triangles[i * 48 + 10] = (i * 8 + 1) % len; triangles[i * 48 + 11] = (i * 8 + 0) % len; //
            triangles[i * 48 + 12] = (i * 8 + 10) % len; triangles[i * 48 + 13] = (i * 8 + 2) % len; triangles[i * 48 + 14] = (i * 8 + 1) % len; //
            triangles[i * 48 + 15] = (i * 8 + 10) % len; triangles[i * 48 + 16] = (i * 8 + 1) % len; triangles[i * 48 + 17] = (i * 8 + 9) % len; //
            triangles[i * 48 + 18] = (i * 8 + 10) % len; triangles[i * 48 + 19] = (i * 8 + 11) % len; triangles[i * 48 + 20] = (i * 8 + 3) % len; //
            triangles[i * 48 + 21] = (i * 8 + 10) % len; triangles[i * 48 + 22] = (i * 8 + 3) % len; triangles[i * 48 + 23] = (i * 8 + 2) % len; //
            triangles[i * 48 + 24] = (i * 8 + 12) % len; triangles[i * 48 + 25] = (i * 8 + 4) % len; triangles[i * 48 + 26] = (i * 8 + 3) % len; //
            triangles[i * 48 + 27] = (i * 8 + 12) % len; triangles[i * 48 + 28] = (i * 8 + 3) % len; triangles[i * 48 + 29] = (i * 8 + 11) % len; //
            triangles[i * 48 + 30] = (i * 8 + 12) % len; triangles[i * 48 + 31] = (i * 8 + 13) % len; triangles[i * 48 + 32] = (i * 8 + 5) % len; //
            triangles[i * 48 + 33] = (i * 8 + 12) % len; triangles[i * 48 + 34] = (i * 8 + 5) % len; triangles[i * 48 + 35] = (i * 8 + 4) % len; //
            triangles[i * 48 + 36] = (i * 8 + 14) % len; triangles[i * 48 + 37] = (i * 8 + 6) % len; triangles[i * 48 + 38] = (i * 8 + 5) % len; //
            triangles[i * 48 + 39] = (i * 8 + 14) % len; triangles[i * 48 + 40] = (i * 8 + 5) % len; triangles[i * 48 + 41] = (i * 8 + 13) % len; //
            triangles[i * 48 + 42] = (i * 8 + 14) % len; triangles[i * 48 + 43] = (i * 8 + 15) % len; triangles[i * 48 + 44] = (i * 8 + 7) % len; //
            triangles[i * 48 + 45] = (i * 8 + 14) % len; triangles[i * 48 + 46] = (i * 8 + 7) % len; triangles[i * 48 + 47] = (i * 8 + 6) % len; //
        }

        // 대입
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        collider.sharedMesh = meshFilter.sharedMesh;
    }

    // 건물 배치 함수
    private void build()
    {
        // 설치 불가능한 지역 검사 // 0 설치 가능 // 1 설치 불가능
        Vector3Int pos = new Vector3Int(path[0].x + 2, path[0].y, path[0].z + 2);
        foreach (var i in pathDir)
        {
            map[pos.x, pos.z] = 1;
            map[pos.x + i.x, pos.z] = 1;
            map[pos.x, pos.z + i.z] = 1;
            pos += i;
        }

        // 건물 설치
        for (int x = 0; x < gridSize.x + 4; x++)
            for (int z = 0; z < gridSize.z + 4; z++)
            {
                // O형
                if (isInMap(new Vector3Int(x + 1, 0, z + 1)) && map[x, z] == 0 && map[x + 1, z] == 0 && map[x, z + 1] == 0 && map[x + 1, z + 1] == 0) // O형
                {
                    Instantiate(building_O[Random.Range(0, building_O.Count)], new Vector3(x - 1.5f, 0, z - 1.5f), Quaternion.Euler(0, Random.Range(0, 4) * 90, 0), buildingParent);
                    map[x, z] = 1;
                    map[x + 1, z] = 1;
                    map[x, z + 1] = 1;
                    map[x + 1, z + 1] = 1;
                }
                // L형
                else if (isInMap(new Vector3Int(x + 1, 0, z + 1)) && map[x + 1, z] == 0 && map[x, z + 1] == 0 && map[x + 1, z + 1] == 0) // void x0 z0
                {
                    Instantiate(building_L[Random.Range(0, building_L.Count)], new Vector3(x - 1.5f, 0, z - 1.5f), Quaternion.Euler(0, 0, 0), buildingParent);
                    map[x + 1, z] = 1;
                    map[x, z + 1] = 1;
                    map[x + 1, z + 1] = 1;
                }
                else if (isInMap(new Vector3Int(x + 1, 0, z + 1)) && map[x, z] == 0 && map[x, z + 1] == 0 && map[x + 1, z + 1] == 0) // void x+ z0
                {
                    Instantiate(building_L[Random.Range(0, building_L.Count)], new Vector3(x - 1.5f, 0, z - 1.5f), Quaternion.Euler(0, 270, 0), buildingParent);
                    map[x, z] = 1;
                    map[x, z + 1] = 1;
                    map[x + 1, z + 1] = 1;
                }
                else if (isInMap(new Vector3Int(x + 1, 0, z + 1)) && map[x, z] == 0 && map[x + 1, z] == 0 && map[x + 1, z + 1] == 0) // void x0 z+
                {
                    Instantiate(building_L[Random.Range(0, building_L.Count)], new Vector3(x - 1.5f, 0, z - 1.5f), Quaternion.Euler(0, 90, 0), buildingParent);
                    map[x, z] = 1;
                    map[x + 1, z] = 1;
                    map[x + 1, z + 1] = 1;
                }
                else if (isInMap(new Vector3Int(x + 1, 0, z + 1)) && map[x, z] == 0 && map[x + 1, z] == 0 && map[x, z + 1] == 0) // void x+ z+
                {
                    Instantiate(building_L[Random.Range(0, building_L.Count)], new Vector3(x - 1.5f, 0, z - 1.5f), Quaternion.Euler(0, 180, 0), buildingParent);
                    map[x, z] = 1;
                    map[x + 1, z] = 1;
                    map[x, z + 1] = 1;
                }
                // I형
                else if (isInMap(new Vector3Int(x + 1, 0, z)) && map[x, z] == 0 && map[x + 1, z] == 0) // x+
                {
                    Instantiate(building_I[Random.Range(0, building_I.Count)], new Vector3(x - 1.5f, 0, z - 1.5f), Quaternion.Euler(0, 0, 0), buildingParent);
                    map[x, z] = 1;
                    map[x + 1, z] = 1;
                }
                else if (isInMap(new Vector3Int(x, 0, z + 1)) && map[x, z] == 0 && map[x, z + 1] == 0) // z+
                {
                    Instantiate(building_I[Random.Range(0, building_I.Count)], new Vector3(x - 1.5f, 0, z - 1.5f), Quaternion.Euler(0, 90, 0), buildingParent);
                    map[x, z] = 1;
                    map[x, z + 1] = 1;
                }
                // dot
                else if (map[x, z] == 0) // dot
                {
                    Instantiate(building_dot[Random.Range(0, building_dot.Count)], new Vector3(x - 2f, 0, z - 2f), Quaternion.Euler(0, Random.Range(0, 4) * 90, 0), buildingParent);
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
