using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] public float moveSpeed = 1f;
    private bool isMoving = false;
    // ���
    private List<Vector3> path;
    // ���� ����� ��ǥ
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

        // Neighbor Index ��ȯ�� ���� dx dy
        private static int[] dx = { -1, 0, 0, 1, -1, -1, 1, 1 };
        private static int[] dy = { 0, 1, -1, 0, 1, -1, 1, -1 };

        public static List<Vector3> FindPath(Vector3 start, Vector3 finalDest)
        {
            // ��ǥ�� index ã��
            Vector2Int startIndex = GameMode.Instance.plane.GetIndex(start);
            Vector2Int finalIndex = GameMode.Instance.plane.GetIndex(finalDest);

            // �켱���� ť
            List<Node> queue = new List<Node>();
            // �湮�� ���
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            // ���� ��带 queue�� �ִ´�.
            queue.Add(new Node(startIndex, 0, Heuristic.GetH(startIndex, finalIndex), null));

            // queue�� ����� ������
            while (queue.Count > 0)
            {
                // ���� ���� F�� ���� ��� ã�� (�켱���� ť�� ���� �� ������)
                Node current = queue[0];
                for (int i = 1; i < queue.Count; i++)
                {
                    current = (queue[i].F < current.F) ? queue[i] : current;
                }

                // ã�� ���� ���� �����̶�� 
                // ReconstructionPath�� ��θ� ������ ��ȯ
                if (current.index == finalIndex)
                {
                    string arrayAsString = string.Join(", ", visited);
                    Debug.Log("Visited : " + arrayAsString);
                    GameMode.Instance.SetSearched(visited.Count);
                    return ReconstructionPath(current, start, finalDest);
                }

                // ���� ���� F�� ���� ��带 Pop
                queue.Remove(current);
                visited.Add(current.index);

                // Pop�� ��忡�� �̵��� �� �ִ� ��忡 ����
                for (int i = 0; i < 8; i++)
                {
                    Vector2Int index = new Vector2Int(current.index.x + dx[i], current.index.y + dy[i]);
                    // ��ȿ�� index�̰�, visited�� �ƴ϶��
                    if (isValidIndex(index) && !visited.Contains(index))
                    {
                        // �����¿�
                        if (i < 4)
                        {
                            float tempG = current.G + 1; // �����¿� �̵��� 1
                            //Heuristic.GetH(index, finalIndex);
                            float tempH = Heuristic.GetH(index, finalIndex);
                            queue.Add(new Node(index, tempG, tempH, current));
                        }
                        // �밢��
                        else
                        {
                            float tempG = current.G + 1.4f; // �밢�� �̵��� ��Ʈ 2
                            float tempH =  Heuristic.GetH(index, finalIndex);
                            queue.Add(new Node(index, tempG, tempH, current));
                        }
                    }
                }
            }
            // �� ã������
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
            // Grid ũ�� ����
            int gridLength = GameMode.Instance.plane.gridLength;
            if (index.x < 0 || index.y < 0 || gridLength <= index.x || gridLength <= index.y)
            {
                return false;
            }
            // Grid�� ��ǥ�� ����������
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
                // GetCoord�� Index�� ��ǥ���� ã�� �Լ��̴�.
                path.Add(GameMode.Instance.plane.GetCoord(node.index));
                node = node.parent;
            }
            path.Reverse();

            // ���� Index�� ��ġ�� �ƴ� ���� ��ġ
            path[0] = start;
            // �� Index�� �ƴ� ���� ��ġ
            path[path.Count - 1] = dest;
            return path;
        }
    }

    private void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 100f;
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);

        // ���콺 Ŭ���� �޾��� ��
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
                        // ���� ��ġ���� ���콺�� Ŭ���� ���������� ���
                        path = AStar.FindPath(gameObject.transform.position, hit.point);

                        if (path != null)
                        {
                            isMoving = true;
                            // ù ��� Pop
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

            // Path���� ���� ��ġ�� �̵�
            transform.position = Vector3.MoveTowards(transform.position, dest, moveSpeed * Time.deltaTime);

            // Path ������ ����������
            if (Vector3.Distance(transform.position, dest) < 0.01f)
            {
                //��� Path�� �̵������� �̵� ����
                if (path.Count == 0)
                {
                    isMoving = false;
                }
                else
                {
                    // �� ���� Pop
                    dest = path[0];
                    path.RemoveAt(0);
                }
            }

        }
    }
}
