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

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	void DrawGraph()
	{
		if (graph == null)
			return;


		for (int x = 0; x < graph.size.x; x++)
		{
			for (int y = 0; y < graph.size.y; y++)
			{
				Vector2Int node = new Vector2Int(x, y);
				List<Vector2Int> neighbours = graph.Neighbours(node);
				foreach (Vector2Int neighbour in neighbours)
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
		if (graph == null)
			return;

		DrawGraph();


		Gizmos.color = Color.green;

		DrawCircle(start.position, Vector3.up, radius, 16);
		DrawCircle(end.position, Vector3.up, radius, 16);

		Vector2[] lines = Extrude(start.position.XZ(), end.position.XZ(), radius);
		
		Vector2 p00 = lines[0];
		Vector2 p01 = lines[1];
		Vector2 p10 = lines[2];
		Vector2 p11 = lines[3];

		Gizmos.DrawLine(lines[0].X0Z(), lines[1].X0Z());
		Gizmos.DrawLine(lines[2].X0Z(), lines[3].X0Z());

		Voxel2D.Line(p00, p01, Vector2.one, -Vector2.one * 0.5f, (block, intersection, normal, distance) =>
		{
			Gizmos.DrawSphere(new Vector3(intersection.x, 0, intersection.y), 0.1f);
			DrawSquare(new Vector2(block.x - 0.5f, block.y - 0.5f));
			return false;
		});
		Voxel2D.Line(p10, p11, Vector2.one, -Vector2.one * 0.5f, (block, intersection, normal, distance) =>
		{
			Gizmos.DrawSphere(new Vector3(intersection.x, 0, intersection.y), 0.1f);
			DrawSquare(new Vector2(block.x - 0.5f, block.y - 0.5f));
			return false;
		});

		if (false) 
        {
            List<Vector2Int> path = Search.AStar(new Vector2Int(2, 2), new Vector2Int(5, 9), graph);

            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3 from = new Vector3(path[i].x, 0, path[i].y);
                Vector3 to = new Vector3(path[i + 1].x, 0, path[i + 1].y);
                Gizmos.color = Color.red;
                Gizmos.DrawLine(from, to);
            }

		Vector2Int startNode = new Vector2Int((int)start.position.x, (int)start.position.z);
		List<Vector2Int> goalNodes = new List<Vector2Int>();
		foreach (Transform goal in goals)
			goalNodes.Add(Vector2Int.RoundToInt(goal.position.XZ()));

        Gizmos.color = Color.red;
        //Tree<Vector2Int> tree = Search.AStar(new Vector2Int(2, 2), new List<Vector2Int> { new Vector2Int(5, 9), new Vector2Int(9, 5) }, graph);
		Tree<Vector2Int> tree = Search.AStarTree(startNode, goalNodes, graph);

		int count = 0;
		tree.Traverse(node =>
		{
			if (count > 10000)
				return;
			count++;
			//Debug.Log("Drew " + count + " nodes");
			foreach (Tree<Vector2Int> child in node.children)
			{
				Vector3 from = new Vector3(node.node.x, 0, node.node.y);
				Vector3 to = new Vector3(child.node.x, 0, child.node.y);
				DrawArrow(from, to);
			}
		});


		Gizmos.color = Color.green;
		
		HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

		Search.FloodFill(goalNodes, startNode, graph, (node, cost) =>
		{
			if (visited.Contains(node))
				Debug.Log("Already visited " + node);
			visited.Add(node);
			//Gizmos.DrawCube(new Vector3(node.x, 0, node.y), Vector3.one * 0.5f);
			Handles.Label(new Vector3(node.x, 0, node.y), cost.ToString());

			return cost > 100 || node == startNode;
		});
        }
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
