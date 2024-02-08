using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;


public class NavigationGraphController : MonoBehaviour
{
	[SerializeField]
    private NavigationGraph graph;

    [SerializeField]
    private ScreenCast screenCast;

	public Vector3 pointOnPlane;

	public Vector2Int mouseNode;
	public List<Vector2Int> starts = new List<Vector2Int>();

	private void OnDrawGizmos()
	{

		Ray ray = new Ray(pointOnPlane + Vector3.up * 0.1f, Vector3.up);

		Gizmos.color = Color.red;
		Gizmos.DrawRay(ray);

		//Gizmos.color = Color.blue;
		//Gizmos.DrawCube(new Vector3(mouseNode.x, 0, mouseNode.y), new Vector3(1f, .1f, 1f));


		Vector2 origin = new Vector2(2.5f, 5.25f);
		Vector2 direction = (new Vector2(pointOnPlane.x, pointOnPlane.z) - origin).normalized;
		float distance = 10;

		//graph.Flood(mouseNode.x, mouseNode.y, 0, 10, this);
		//graph.BresenhamLine(origin, direction, distance, this);

		/*
		Gizmos.color = Color.red;
		Gizmos.DrawLine(new Vector3(origin.x, 0, origin.y), new Vector3(origin.x + direction.x * distance, 0, origin.y + direction.y * distance));

		Gizmos.color = new Color(0f, 1f, 0f, .25f);
		Voxel2D.Line(origin, new Vector2(pointOnPlane.x, pointOnPlane.z), Vector2.one, -Vector2.one * 0.5f, (Vector2Int block, Vector2 intersection, Vector2 normal, float distance) =>
		{
			if (graph.Get(block.x, block.y) == NavigationGraph.OBSTACLE)
				return true;
			Gizmos.DrawCube(new Vector3(block.x, 0, block.y), new Vector3(1f, .05f, 1f));
			return false;
		});

		Gizmos.color = Color.blue;

		if (false)foreach (Vector2Int s in starts)
		{
			List<Vector2Int> pathToGoal = new List<Vector2Int>();
			if (graph.PathToGoal(s.x, s.y, pathToGoal))
			{
				for (int i = 0; i < pathToGoal.Count - 1; i++)
					Gizmos.DrawLine(new Vector3(pathToGoal[i].x, 0, pathToGoal[i].y), new Vector3(pathToGoal[i + 1].x, 0, pathToGoal[i + 1].y));
			}

			Gizmos.DrawSphere(new Vector3(s.x, 0, s.y), 0.1f);
		}


		Gizmos.color = Color.red;
		List<Vector2Int> path = new List<Vector2Int>();
		NavigationGraph.AStar(Vector2Int.RoundToInt(origin), mouseNode, graph, path);
		if (path.Count > 1)
		{
			for (int i = 0; i < path.Count - 1; i++)
				Gizmos.DrawLine(new Vector3(path[i].x, 0, path[i].y), new Vector3(path[i + 1].x, 0, path[i + 1].y));
		}

		Gizmos.color = Color.blue;
		foreach (Vector2Int s in starts)
		{
			Tree tree = NavigationGraph.AStar(s, graph);
			if (tree != null)
				tree.DrawGizmos();
		}
		*/

		Gizmos.color = Color.red;
		List<Path> merge = graph.AStarMerge(mouseNode);

		

		for (int i= 0 ; i < Mathf.Min(draw_paths, merge.Count); i++)
		{
			merge[i].DrawGizmos();
		}

	}
	public int draw_paths = 10;
	public bool Visit(int x, int y, int value, int steps)
	{
		if (steps > 5 || value == NavigationGraph.OBSTACLE)
		{
			return true;
		}
		else
		{
			Gizmos.DrawCube(new Vector3(x, 0, y), new Vector3(1f, .1f, 1f));
			return false;
		}
	}

	void Update()
	{
		if (!screenCast.MouseOnXZPlane(0, out pointOnPlane, out float distance))
		{
			//Debug.Log("Mouse not on plane");
			return;
		}
		if (!graph.WorldToGraph(pointOnPlane, out int x, out int y))
		{
			//Debug.Log("Mouse not on graph");
			return;
		}

		mouseNode = new Vector2Int(x, y);

		if (Input.GetMouseButtonDown(0))
		{
			//Debug.Log("Click!");
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				if (starts.Contains(new Vector2Int(x, y)))
					starts.Remove(new Vector2Int(x, y));
				else
					starts.Add(new Vector2Int(x, y));
			}
			else if (graph.Get(x, y) == NavigationGraph.GOAL)
				graph.SetUnvisited(x, y);
			else
				graph.SetGoal(x, y);
		}

		if (Input.GetMouseButtonDown(1))
		{
			//Debug.Log("Right Click!");
			if (graph.Get(x, y) == NavigationGraph.OBSTACLE)
				graph.SetUnvisited(x, y);
			else
				graph.SetObstacle(x, y);
		}
	}
}
