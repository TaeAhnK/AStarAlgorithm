using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEngine;

public class Plane : MonoBehaviour
{
    [SerializeField] public int gridLength;
    [SerializeField] GameObject tree;
    public LineRenderer lineRenderer;
    public List<GameObject> trees;
    public bool[ , ] Grid;
    private float min_x = -1f;
    private float max_x = 1f;
    private float min_z = -1f;
    private float max_z = 1f;

    private void Awake()
    {
        lineRenderer = this.GetComponent<LineRenderer>();
        lineRenderer.startWidth = lineRenderer.endWidth = 0.01f;
        MakeGrid(10);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.gameObject.CompareTag("Plane"))
                {
                    Vector2Int plant = GetIndex(hit.point);
                    if (Grid[plant.y, plant.x])
                    {
                        Grid[plant.y, plant.x] = false;
                        GameObject newtree = Instantiate(tree, GetCoord(plant), Quaternion.identity);
                        newtree.transform.localScale = new Vector3(newtree.transform.localScale.x * 10 / gridLength,
                            newtree.transform.localScale.y * 10 / gridLength,
                            newtree.transform.localScale.z * 10 / gridLength); 
                        trees.Add(newtree);
                    }
                }
            }
        }
    }

    public Vector2Int GetIndex(Vector3 coordinate)
    {
        if (1 < coordinate.x || coordinate.x < -1 || 1 < coordinate.z || coordinate.z < -1)
        {
            return Vector2Int.zero;
        }

        float cell_width = (max_x - min_x) / gridLength;
        float cell_height = (max_z - min_z) / gridLength;

        int col_index = (int)((coordinate.x - min_z) / cell_width);
        int row_index = (int)((coordinate.z - min_z) / cell_height);

        col_index = Mathf.Min(col_index, gridLength - 1);
        row_index = Mathf.Min(row_index, gridLength - 1);

        return new Vector2Int(col_index, row_index);
    }

    public Vector3 GetCoord(Vector2Int index)
    {
        float cell_Width = (max_x - min_x) / gridLength;
        float cell_Height = (max_z - min_z) / gridLength;

        // Calculate the center coordinates
        float center_X = min_x + (index.x + 0.5f) * cell_Width;
        float center_Z = min_z + (index.y + 0.5f) * cell_Width;

        return new Vector3(center_X, 0, center_Z);
    }

    public void MakeGrid(int gridLength)
    {
        this.gridLength = gridLength;
        for (int i = 0; i < trees.Count; i++)
        {
            Destroy(trees[i]);
        }
        trees.Clear();


        Grid = new bool[this.gridLength, gridLength];
        for (int i = 0; i < this.gridLength; i++)
        {
            for (int j = 0; j < this.gridLength; j++)
            {
                Grid[i, j] = true;
            }
        }

        List<Vector3> gridPos = new List<Vector3>();
        float sc = min_x;
        float sr = min_z;
        float gridSize = (max_x-min_x) / this.gridLength;
        float ec = sc + gridLength * gridSize;

        gridPos.Add(new Vector3(sr, 0.1f, sc));
        gridPos.Add(new Vector3(sr, 0.1f, ec));

        int toggle = -1;
        Vector3 currentPos = new Vector3(sr, 0.1f, ec);
        for (int i = 0; i < this.gridLength; i++)
        {
            Vector3 nextPos = currentPos;

            nextPos.x += gridSize;
            gridPos.Add(nextPos);

            nextPos.z += (this.gridLength * toggle * gridSize);
            gridPos.Add(nextPos);

            currentPos = nextPos;
            toggle *= -1;
        }

        currentPos.x = sr;
        gridPos.Add(currentPos);

        int colToggle = toggle = 1;
        if (currentPos.z == ec) colToggle = -1;

        for (int i = 0; i < this.gridLength; i++)
        {
            Vector3 nextPos = currentPos;

            nextPos.z += (colToggle * gridSize);
            gridPos.Add(nextPos);

            nextPos.x += (this.gridLength * toggle * gridSize);
            gridPos.Add(nextPos);

            currentPos = nextPos;
            toggle *= -1;
        }

        lineRenderer.positionCount = gridPos.Count;
        lineRenderer.SetPositions(gridPos.ToArray());
    }

}
