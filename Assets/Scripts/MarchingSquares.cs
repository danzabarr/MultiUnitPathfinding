using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarchingSquares : MonoBehaviour
{
    public Vector2 cellSize;
    public Vector2Int gridSize;
    public float[] heightMap;
    public float isoLevel;


	public void DrawLine(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
	{
		Vector2[] points = new Vector2[4];
		points[0] = p1;
		points[1] = p2;
		points[2] = p3;
		points[3] = p4;

		for (int i = 0; i < 4; i++)
		{
			int j = (i + 1) % 4;
			if ((heightMap[(int)points[i].y * gridSize.x + (int)points[i].x] > isoLevel) != (heightMap[(int)points[j].y * gridSize.x + (int)points[j].x] > isoLevel))
			{
				float t = (isoLevel - heightMap[(int)points[i].y * gridSize.x + (int)points[i].x]) / (heightMap[(int)points[j].y * gridSize.x + (int)points[j].x] - heightMap[(int)points[i].y * gridSize.x + (int)points[i].x]);
				Vector2 p = Vector2.Lerp(points[i], points[j], t);
				Gizmos.DrawSphere(p.X0Z(), 0.1f);
			}
		}
	}

	public void OnValidate()
	{
		if (heightMap == null || heightMap.Length != (gridSize.x + 1) * (gridSize.y + 1))
			heightMap = new float[(gridSize.x + 1) * (gridSize.y + 1)];

		cellSize.x = Mathf.Max(cellSize.x, 0.1f);
		cellSize.y = Mathf.Max(cellSize.y, 0.1f);
		gridSize.x = Mathf.Max(gridSize.x, 1);
		gridSize.y = Mathf.Max(gridSize.y, 1);

	}

	private void OnDrawGizmos()
    {
		if (heightMap == null || heightMap.Length != (gridSize.x + 1) * (gridSize.y + 1))
			return;

		for (int y = 0; y < gridSize.y; y++)
        {
			for (int x = 0; x < gridSize.x; x++)
            {
                Vector2Int[] cell = new Vector2Int[4];
                cell[0] = new Vector2Int(x, y);
                cell[1] = new Vector2Int(x + 1, y);
                cell[2] = new Vector2Int(x + 1, y + 1);
                cell[3] = new Vector2Int(x, y + 1);

				int cellIndex = 0;
				for (int i = 0; i < 4; i++)
                {
					int index = cell[i].y * gridSize.x + cell[i].x;

					if (heightMap[index] > isoLevel)
						cellIndex |= 1 << i;
				}

                Vector2[] points = new Vector2[4];
				for (int i = 0; i < 4; i++)
                {
                    points[i] = cell[i] * cellSize;
				}

				switch (cellIndex)
                {
					case 0:
					case 15:
						break;
					case 1:
					case 14:
                        Gizmos.DrawLine(points[0].X0Z(), points[1].X0Z());
						break;
					case 2:
					case 13:
                        Gizmos.DrawLine(points[1].X0Z(), points[2].X0Z());
						break;
					case 3:
					case 12:
						Gizmos.DrawLine(points[0].X0Z(), points[3].X0Z());
						break;
					case 4:
					case 11:
						Gizmos.DrawLine(points[2].X0Z(), points[3].X0Z());
						break;
					case 6:
					case 9:
						DrawLine(points[0], points[1], points[2], points[3]);
						break;
					case 7:
					case 8:
						DrawLine(points[0], points[1], points[2], points[3]);
						break;
					case 10:
					case 5:
						DrawLine(points[0], points[1], points[2], points[3]);
						break;
				}
            }
        }
    }
}
