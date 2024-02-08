using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GridGraphRenderer : MonoBehaviour
{
    public GridGraph graph;

	public Transform start;
	public Transform end;

	
	public float radius;
	public List<Transform> goals;

	void DrawGraph()
	{
		if (graph == null)
			return;


		for (int x = 0; x < graph.size.x; x++)
		{
			for (int y = 0; y < graph.size.y; y++)
			{
				Vector2Int node = new Vector2Int(x, y);
				foreach (Vector2Int neighbour in graph.Neighbours(node))
				{
					Vector3 from = new Vector3(node.x, 0, node.y);
					Vector3 to = new Vector3(neighbour.x, 0, neighbour.y);
					Gizmos.DrawLine(from, to);
				}
			}
		}
	}

	public static void DrawCircle(Vector3 origin, Vector3 normal, float radius, int segments)
	{
		Vector3 right = Vector3.right; 
		Vector3 forward = Vector3.forward;
		if (normal != Vector3.up)
		{
			right = Vector3.Cross(normal, Vector3.up).normalized;
			forward = Vector3.Cross(normal, right).normalized;
		}
		
		for (int i = 0; i < segments; i++)
		{
			float angle = i / (float)segments * 360 * Mathf.Deg2Rad;
			Vector3 from = origin + right * Mathf.Cos(angle) * radius + forward * Mathf.Sin(angle) * radius;
			angle = (i + 1) / (float)segments * 360 * Mathf.Deg2Rad;
			Vector3 to = origin + right * Mathf.Cos(angle) * radius + forward * Mathf.Sin(angle) * radius;
			Gizmos.DrawLine(from, to);
		}
	}

	public static void DrawSquare(Vector2 where)
	{
		Gizmos.DrawLine(new Vector3(where.x, 0, where.y), new Vector3(where.x + 1, 0, where.y));
		Gizmos.DrawLine(new Vector3(where.x + 1, 0, where.y), new Vector3(where.x + 1, 0, where.y + 1));
		Gizmos.DrawLine(new Vector3(where.x + 1, 0, where.y + 1), new Vector3(where.x, 0, where.y + 1));
		Gizmos.DrawLine(new Vector3(where.x, 0, where.y + 1), new Vector3(where.x, 0, where.y));
	}

	public static Vector2[] Extrude(Vector2 p1, Vector2 p2, float width)
	{
		Vector2 dir = (p2 - p1).normalized;
		Vector2 normal = new Vector2(-dir.y, dir.x);
		return new Vector2[] { p1 + normal * width, p2 + normal * width, p2 - normal * width, p1 - normal * width };
	}

	private void OnDrawGizmos()
	{
		DrawGraph();
	}


	public static void DrawArrow(Vector3 start, Vector3 end, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		Gizmos.DrawLine(start, end);
		Vector3 direction = end - start;
		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
		Gizmos.DrawRay(end, right * arrowHeadLength);
		Gizmos.DrawRay(end, left * arrowHeadLength);
	}
}
