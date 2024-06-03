using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] public float moveSpeed = 1f;
    private bool isMoving = false;
    // 경로
    private List<Vector3> path;
    // 현재 경로의 목표
    private Vector3 dest;

    private class AStar
    {
        public class Node
        {
            public Vector2Int index;
            public float G;
            public float H;
            public float F;
            public Node parent;

            public Node(Vector2Int index, float G, float H, Node parent)
            {
                this.index = index;
                this.G = G;
                this.H = H;
                this.F = G + H;
                this.parent = parent;
            }
        }

        // Neighbor Index 순환을 위한 dx dy
        private static int[] dx = { -1, 0, 0, 1, -1, -1, 1, 1 };
        private static int[] dy = { 0, 1, -1, 0, 1, -1, 1, -1 };

        public static List<Vector3> FindPath(Vector3 start, Vector3 finalDest)
        {
            // 좌표의 index 찾기
            Vector2Int startIndex = GameMode.Instance.plane.GetIndex(start);
            Vector2Int finalIndex = GameMode.Instance.plane.GetIndex(finalDest);

            // 우선순위 큐
            List<Node> queue = new List<Node>();
            // 방문한 노드
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            // 시작 노드를 queue에 넣는다.
            queue.Add(new Node(startIndex, 0, Heuristic.GetH(startIndex, finalIndex), null));

            // queue가 비어질 때까지
            while (queue.Count > 0)
            {
                // 가장 작은 F를 가진 노드 찾기 (우선순위 큐를 쓰면 더 빠르다)
                Node current = queue[0];
                for (int i = 1; i < queue.Count; i++)
                {
                    current = (queue[i].F < current.F) ? queue[i] : current;
                }

                // 찾은 값이 도착 지점이라면 
                // ReconstructionPath로 경로를 추적해 반환
                if (current.index == finalIndex)
                {
                    string arrayAsString = string.Join(", ", visited);
                    Debug.Log("Visited : " + arrayAsString);
                    GameMode.Instance.SetSearched(visited.Count);
                    return ReconstructionPath(current, start, finalDest);
                }

                // 가장 작은 F를 가진 노드를 Pop
                queue.Remove(current);
                visited.Add(current.index);

                // Pop한 노드에서 이동할 수 있는 노드에 대해
                for (int i = 0; i < 8; i++)
                {
                    Vector2Int index = new Vector2Int(current.index.x + dx[i], current.index.y + dy[i]);
                    // 유효한 index이고, visited가 아니라면
                    if (isValidIndex(index) && !visited.Contains(index))
                    {
                        // 상하좌우
                        if (i < 4)
                        {
                            float tempG = current.G + 1; // 상하좌우 이동은 1
                            //Heuristic.GetH(index, finalIndex);
                            float tempH = Heuristic.GetH(index, finalIndex);
                            queue.Add(new Node(index, tempG, tempH, current));
                        }
                        // 대각선
                        else
                        {
                            float tempG = current.G + 1.4f; // 대각선 이동은 루트 2
                            float tempH =  Heuristic.GetH(index, finalIndex);
                            queue.Add(new Node(index, tempG, tempH, current));
                        }
                    }
                }
            }
            // 못 찾았으면
            return null;
        }

        public interface IHeuristic
        {
            float Calc(Vector2Int start, Vector2Int dest);
        }

        public class ManhattanHeuristic : IHeuristic
        {
            public float Calc(Vector2Int start, Vector2Int dest)
            {
                return Mathf.Abs(start.x - dest.x) + Mathf.Abs(start.y - dest.y);
            }
        }

        public class EuclidHeuristic : IHeuristic
        {
            public float Calc(Vector2Int start, Vector2Int dest)
            {
                return Vector2.Distance(start, dest);
            }
        }

        public class DijkstraHeuristic : IHeuristic
        {
            public float Calc(Vector2Int start, Vector2Int dest)
            {
                return 1f;
            }
        }

        public class Heuristic
        {
            private static IHeuristic heuristic;
            public static float GetH(Vector2Int start, Vector2Int dest)
            {
                switch (GameMode.Instance.heuristicIndex)
                {
                    case 0:
                        heuristic = new ManhattanHeuristic();
                        break;
                    case 1:
                        heuristic = new EuclidHeuristic();
                        break;
                    case 2:
                        heuristic = new DijkstraHeuristic();
                        break;
                    default:
                        Debug.LogError("Unknown heuristic selected");
                        heuristic = new ManhattanHeuristic();
                        break;
                }
                return heuristic.Calc(start, dest);
            }
        }

        private static bool isValidIndex(Vector2Int index)
        {
            // Grid 크기 변수
            int gridLength = GameMode.Instance.plane.gridLength;
            if (index.x < 0 || index.y < 0 || gridLength <= index.x || gridLength <= index.y)
            {
                return false;
            }
            // Grid의 좌표가 막혀있으면
            if (GameMode.Instance.plane.Grid[index.y, index.x] == false)
            {
                return false;
            }
            return true;
        }

        public static List<Vector3> ReconstructionPath(Node node, Vector3 start, Vector3 dest)
        {
            List<Vector3> path = new List<Vector3>();
            while (node != null)
            {
                // GetCoord는 Index로 좌표값을 찾는 함수이다.
                path.Add(GameMode.Instance.plane.GetCoord(node.index));
                node = node.parent;
            }
            path.Reverse();

            // 시작 Index의 위치가 아닌 현재 위치
            path[0] = start;
            // 끝 Index가 아닌 도착 위치
            path[path.Count - 1] = dest;
            return path;
        }
    }

    private void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 100f;
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);

        // 마우스 클릭을 받았을 때
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject.CompareTag("Plane"))
                {
                    Vector2Int hitIndex = GameMode.Instance.plane.GetIndex(hit.point);
                    if (GameMode.Instance.plane.Grid[hitIndex.y, hitIndex.x])
                    {
                        // 현재 위치부터 마우스로 클릭한 지점까지의 경로
                        path = AStar.FindPath(gameObject.transform.position, hit.point);

                        if (path != null)
                        {
                            isMoving = true;
                            // 첫 경로 Pop
                            dest = path[0];
                            path.RemoveAt(0);
                        }
                    }
                }
            }
        }

        if (isMoving && path != null)
        {
            // Rotation
            Vector3 direction = (dest - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = toRotation;
            }

            // Path에서 뽑은 위치로 이동
            transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * Time.deltaTime);

            // Path 지점에 도착했으면
            if (Vector3.Distance(transform.position, dest) < 0.01f)
            {
                //모든 Path를 이동했으면 이동 종료
                if (path.Count == 0)
                {
                    isMoving = false;
                }
                else
                {
                    // 새 지점 Pop
                    dest = path[0];
                    path.RemoveAt(0);
                }
            }

        }
    }
}
