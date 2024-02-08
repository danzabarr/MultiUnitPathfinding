using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Path
{
	public Path parent;
	public Vector2Int position;

	public void DrawGizmos()
	{
		if (parent != null)
		{
			Gizmos.DrawLine(new Vector3(position.x, 0, position.y), new Vector3(parent.position.x, 0, parent.position.y));
			parent.DrawGizmos();
		}
	}
}

public class NavigationGraph : MonoBehaviour
{

	public const int UNVISITED = int.MaxValue;
	public const int GOAL = 0;
	public const int OBSTACLE = -1;

	public int width, height;
	public int max = 20;

	public int[,] values;

	public int Get(int x, int y)
	{
		if (x < 0 || x >= width || y < 0 || y >= height)
			return OBSTACLE;
		return values[x, y];
	}

	public void Reset()
	{
		for (int i = 0; i < width; i++)
			for (int j = 0; j < height; j++)
				values[i, j] = UNVISITED;
	}

	public void Start()
	{
		Initialize(width, height);
	}

	public void Initialize(int width, int height)
	{
		this.width = width;
		this.height = height;
		values = new int[width, height];
		for (int i = 0; i < width; i++)
			for (int j = 0; j < height; j++)
				values[i, j] = UNVISITED;
	}

	public void RemoveGoal(int x, int y)
	{
		
	}

	public void SetUnvisited(int x, int y)
	{
		if (values[x, y] == UNVISITED)
			return;

		if (values[x, y] == OBSTACLE)
		{
			values[x, y] = UNVISITED;
			List<Vector2Int> path = new List<Vector2Int>();
			if (PathToGoal(x, y, path))
			{
				Debug.Log("Removing obstacle, path found to a goal. Propagating from " + path[0]);
				Vector2Int goal = path[0];
				Propagate(goal.x + 1, goal.y, max);
				Propagate(goal.x - 1, goal.y, max);
				Propagate(goal.x, goal.y + 1, max);
				Propagate(goal.x, goal.y - 1, max);
			}
			return;
		}

		values[x, y] = UNVISITED;
		Refresh();
		return;

		// find the nearest goal
		List<Vector2Int> visited = new List<Vector2Int>();
		List<Vector2Int> frontier = new List<Vector2Int>();
		frontier.Add(new Vector2Int(x, y));
		while (frontier.Count > 0)
		{
			Vector2Int current = frontier[0];
			frontier.RemoveAt(0);
			if (visited.Contains(current))
				continue;
			visited.Add(current);
			int value = Get(current.x, current.y);
			if (value == GOAL)
			{
				Propagate(current.x + 1, current.y, 10000);
				Propagate(current.x - 1, current.y, 10000);
				Propagate(current.x, current.y + 1, 10000);
				Propagate(current.x, current.y - 1, 10000);
				return;
			}
			if (value == OBSTACLE)
				continue;
			frontier.Add(new Vector2Int(current.x + 1, current.y));
			frontier.Add(new Vector2Int(current.x - 1, current.y));
			frontier.Add(new Vector2Int(current.x, current.y + 1));
			frontier.Add(new Vector2Int(current.x, current.y - 1));
		}
		// if no goal found, just propagate from the unvisited node
		//Propagate(x, y, x + 1, y);
		//Propagate(x, y, x - 1, y);
		//Propagate(x, y, x, y + 1);
		//Propagate(x, y, x, y - 1);
	}

	public void SetObstacle(int x, int y)
	{
		if (values[x, y] == OBSTACLE)
			return;
		values[x, y] = OBSTACLE;

		Refresh();
		return;
	}

	public void SetGoal(int x, int y)
	{
		if (values[x, y] == GOAL)
			return;
		values[x, y] = GOAL;
		Propagate(x + 1, y, max);
		Propagate(x - 1, y, max);
		Propagate(x, y + 1, max);
		Propagate(x, y - 1, max);
	}

	public int modder = 0;

	public void Refresh()
	{
		for (int y = 0; y < height; y++)
			for (int x = 0; x < width; x++)
			{
				if (values[x, y] != GOAL && values[x, y] != OBSTACLE)
					values[x, y] = UNVISITED;
			}

		for (int y = 0; y < height; y++)
			for (int x = 0; x < width; x++)
			{
				if (values[x, y] == GOAL)
				{
					Propagate(x + 1, y, max);
					Propagate(x - 1, y, max);
					Propagate(x, y + 1, max);
					Propagate(x, y - 1, max);
				}
			}
	}

	public bool HasLineOfSight(Vector2Int start, Vector2Int end)
	{
		bool hasLineOfSight = true;
		Voxel2D.Line(start, end, Vector2.one, Vector2.one * -0.5f, (Vector2Int block, Vector2 intersection, Vector2 normal, float distance) =>
		{
			if (values[block.x, block.y] == OBSTACLE)
			{
				hasLineOfSight = false;
				return true;
			}
			return false;
		});
		return hasLineOfSight;
	}

	public bool PathToGoal(int x, int y, List<Vector2Int> path)
	{
		//if (values[x, y] == OBSTACLE)
		//	return false;

		if (values[x, y] == GOAL)
		{
			path.Add(new Vector2Int(x, y));
			return true;
		}

		int n = Get(x, y - 1);
		int e = Get(x + 1, y);
		int s = Get(x, y + 1);
		int w = Get(x - 1, y);

		int lowest_neighbour = UNVISITED;
		if (n != OBSTACLE && n != UNVISITED) lowest_neighbour = Mathf.Min(lowest_neighbour, n);
		if (e != OBSTACLE && e != UNVISITED) lowest_neighbour = Mathf.Min(lowest_neighbour, e);
		if (s != OBSTACLE && s != UNVISITED) lowest_neighbour = Mathf.Min(lowest_neighbour, s);
		if (w != OBSTACLE && w != UNVISITED) lowest_neighbour = Mathf.Min(lowest_neighbour, w);

		if (lowest_neighbour == UNVISITED)
			return false;

		Vector2Int current = new Vector2Int(x, y);
		if (n == lowest_neighbour && PathToGoal(x, y - 1, path))
		{
			path.Add(current);
			return true;
		}
		if (e == lowest_neighbour && PathToGoal(x + 1, y, path))
		{
			path.Add(current);
			return true;
		}
		if (s == lowest_neighbour && PathToGoal(x, y + 1, path))
		{
			path.Add(current);
			return true;
		}
		if (w == lowest_neighbour && PathToGoal(x - 1, y, path))
		{
			path.Add(current);
			return true;
		}

		return false;
	}

	public void SetObstacles(List<Vector2> obstacles)
	{
		List<Vector2> to_propagate = new List<Vector2>();
		foreach (Vector2 obstacle in obstacles)
		{
			if (obstacle.x < 0 || obstacle.x >= width || obstacle.y < 0 || obstacle.y >= height)
				continue;

			if (values[(int)obstacle.x, (int)obstacle.y] == OBSTACLE)
				continue;

			to_propagate.Add(obstacle);
			values[(int)obstacle.x, (int)obstacle.y] = OBSTACLE;
		}
	}

	private void Propagate(int x, int y, int max)
	{
		if (x < 0 || x >= width || y < 0 || y >= height)
			return;

		int value = Get(x, y);

		if (value == OBSTACLE)
			return;

		int n = Get(x, y - 1);
		int e = Get(x + 1, y);
		int s = Get(x, y + 1);
		int w = Get(x - 1, y);

		int lowest_neighbour = value;
		if (n != OBSTACLE && n != UNVISITED) lowest_neighbour = Mathf.Min(lowest_neighbour, n);
		if (e != OBSTACLE && e != UNVISITED) lowest_neighbour = Mathf.Min(lowest_neighbour, e);
		if (s != OBSTACLE && s != UNVISITED) lowest_neighbour = Mathf.Min(lowest_neighbour, s);
		if (w != OBSTACLE && w != UNVISITED) lowest_neighbour = Mathf.Min(lowest_neighbour, w);

		int new_value = lowest_neighbour + 1;
		if (new_value < value && new_value < max)
		{
			values[x, y] = new_value;
			if (n >= new_value)
				Propagate(x, y - 1, max);
			if (e >= new_value)
				Propagate(x + 1, y, max);
			if (s >= new_value)
				Propagate(x, y + 1, max);
			if (w >= new_value)
				Propagate(x - 1, y, max);
		}
	}

	private void DrawNode(int x, int y)
	{
		Vector3 position = new Vector3(x, 0, y);
		transform.TransformPoint(position);
		Gizmos.DrawSphere(position, 0.1f);
	}

	private void OnDrawGizmos()
	{
		if (values == null)
			return;

		// Draw  edges
		for (int i = 0; i < width; i++)
			for (int j = 0; j < height; j++)
			{
				if (values[i, j] == OBSTACLE)
				{
					Gizmos.color = Color.red;
					Gizmos.DrawCube(new Vector3(i, 0.5f, j), Vector3.one);
					continue;
				}

				int e = Get(i + 1, j);
				int s = Get(i, j + 1);

				if (e != OBSTACLE)
					Handles.DrawLine(new Vector3(i, 0, j), new Vector3(i + 1, 0, j));
				if (s != OBSTACLE)
					Handles.DrawLine(new Vector3(i, 0, j), new Vector3(i, 0, j + 1));

				if (values[i, j] == GOAL)
				{
					Gizmos.color = Color.green;
					Gizmos.DrawSphere(new Vector3(i, 0, j), 0.5f);
				}
			}

		// Draw step labels
		for (int i = 0; i < width; i++)
			for (int j = 0; j < height; j++)
			{
				if (values[i, j] == OBSTACLE)
					Gizmos.color = Color.red;

				else if (values[i, j] == GOAL)
					Gizmos.color = Color.green;

				else if (values[i, j] == UNVISITED)
					Gizmos.color = Color.white;

				else
					Gizmos.color = Color.blue;

				//Gizmos.DrawCube(new Vector3(i, 0, j), Vector3.one);
				int value = values[i, j];
				if (value != UNVISITED)
					Handles.Label(new Vector3(i, 0, j), value.ToString());
			}

	}

	public bool WorldToGraph(Vector3 position, out int x, out int y)
	{
		position = transform.InverseTransformPoint(position);
		x = Mathf.RoundToInt(position.x);
		y = Mathf.RoundToInt(position.z);

		return x >= 0 && x < width && y >= 0 && y < height;
	}

	public List<Vector2Int> Neighbours(Vector2Int current)
	{
		List<Vector2Int> neighbours = new List<Vector2Int>();
		if (Get(current.x, current.y - 1) != OBSTACLE)
			neighbours.Add(new Vector2Int(current.x, current.y - 1));
		if (Get(current.x + 1, current.y) != OBSTACLE)
			neighbours.Add(new Vector2Int(current.x + 1, current.y));
		if (Get(current.x, current.y + 1) != OBSTACLE)
			neighbours.Add(new Vector2Int(current.x, current.y + 1));
		if (Get(current.x - 1, current.y) != OBSTACLE)
			neighbours.Add(new Vector2Int(current.x - 1, current.y));
		return neighbours;
	}

	public static void AStar(Vector2Int start, Vector2Int goal, NavigationGraph graph, List<Vector2Int> path)
	{
		// A* algorithm
		// https://en.wikipedia.org/wiki/A*_search_algorithm
		// https://www.redblobgames.com/pathfinding/a-star/introduction.html
		// https://www.redblobgames.com/pathfinding/a-star/implementation.html

		float HeuristicCostEstimate(Vector2Int a, Vector2Int b)
		{
			//return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
			return Vector2Int.Distance(a, b);
		}

		void ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, List<Vector2Int> path)
		{
			if (cameFrom.ContainsKey(current))
			{
				ReconstructPath(cameFrom, cameFrom[current], path);
				path.Add(current);
			}
		}

		// The set of nodes already evaluated
		List<Vector2Int> closedSet = new List<Vector2Int>();

		// The set of currently discovered nodes that are not evaluated yet.
		// Initially, only the start node is known.
		List<Vector2Int> openSet = new List<Vector2Int>();
		openSet.Add(start);

		// For each node, which node it can most efficiently be reached from.
		// If a node can be reached from many nodes, cameFrom will eventually contain the
		// most efficient previous step.
		Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

		// For each node, the cost of getting from the start node to that node.
		Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
		gScore[start] = 0;
		float GScore(Vector2Int node) => gScore.ContainsKey(node) ? gScore[node] : float.MaxValue;

		// For each node, the total cost of getting from the start node to the goal
		// by passing by that node. That value is partly known, partly heuristic.
		Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();
		fScore[start] = HeuristicCostEstimate(start, goal);
		float FScore(Vector2Int node) => fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;

		while (openSet.Count > 0)
		{
			Vector2Int current = openSet[0];
			for (int i = 1; i < openSet.Count; i++)
				if (FScore(openSet[i]) < GScore(current))
					current = openSet[i];

			if (current == goal)
			{
				ReconstructPath(cameFrom, current, path);
				return;
			}

			openSet.Remove(current);
			closedSet.Add(current);

			foreach (Vector2Int neighbour in graph.Neighbours(current))
			{
				if (closedSet.Contains(neighbour))
					continue; // Ignore the neighbor which is already evaluated.

				float tentative_gScore = GScore(current) + 1; // 1 is the distance from current to a neighbor

				if (!openSet.Contains(neighbour)) // Discover a new node
					openSet.Add(neighbour);
				else if (tentative_gScore >= GScore(neighbour))
					continue; // This is not a better path.

				// This path is the best until now. Record it!
				cameFrom[neighbour] = current;
				gScore[neighbour] = tentative_gScore;
				fScore[neighbour] = GScore(neighbour) + HeuristicCostEstimate(neighbour, goal);
			}
		}
	}


	public static Tree AStar(Vector2Int start, NavigationGraph graph)
	{
		List<Vector2Int> goals = new List<Vector2Int>();
		for (int i = 0; i < graph.width; i++)
			for (int j = 0; j < graph.height; j++)
				if (graph.values[i, j] == GOAL)
					goals.Add(new Vector2Int(i, j));

		Tree root = new Tree(start);
		Dictionary<Vector2Int, Tree> tree = new Dictionary<Vector2Int, Tree>();
		tree[start] = root;

		foreach (Vector2Int goal in goals)
		{
			List<Vector2Int> path = new List<Vector2Int>();
			AStar(start, goal, graph, path);
			if (path.Count > 0)
			{
				Tree current = root;
				for (int i = 0; i < path.Count; i++)
				{
					Vector2Int node = path[i];
					if (tree.ContainsKey(node))
					{
						if (!current.children.Contains(tree[node]))
							current.children.Add(tree[node]);
						current = tree[node];

					}
					else
					{
						Tree child = new Tree(node);
						tree[node] = child;
						current.children.Add(child);
						current = child;
					}
				}
			}
		}

		return root;
	}

	public List<Path> AStarMerge(Vector2Int start)
	{
		// The list of paths, each one is a goal node with a path to the start node
		List<Path> paths = new List<Path>();

		if (values == null)
			return paths;
		// The map of paths, each one is a node with a path to the start node
		// The map is used to merge paths when a node is reached more than once
		Dictionary<Vector2Int, Path> map = new Dictionary<Vector2Int, Path>();

		// The list of goal nodes
		List<Vector2Int> goals = new List<Vector2Int>();
		for (int i = 0; i < width; i++)
			for (int j = 0; j < height; j++)
				if (values[i, j] == GOAL)
					goals.Add(new Vector2Int(i, j));

		if (goals.Count == 0)
			return paths;

		// The heuristic cost estimate
		float HeuristicCostEstimate(Vector2Int a, Vector2Int b)
		{
			//return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
			return Vector2Int.Distance(a, b);
		}

		// Reconstruct the path
		void ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, Path path)
		{
			if (cameFrom.ContainsKey(current))
			{
				ReconstructPath(cameFrom, cameFrom[current], path);
				path.parent = new Path();
				path.parent.position = cameFrom[current];
			}
		}

		// For each goal node, find a path to the start node, or merge with an existing path, whichever is shorter

		foreach (Vector2Int goal in goals)
		{
			// The set of nodes already evaluated
			List<Vector2Int> closedSet = new List<Vector2Int>();

			// The set of currently discovered nodes that are not evaluated yet.
			// Initially, only the start node is known.
			List<Vector2Int> openSet = new List<Vector2Int>();
			openSet.Add(goal);

			// For each node, which node it can most efficiently be reached from.
			// If a node can be reached from many nodes, cameFrom will eventually contain the
			// most efficient previous step.
			Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

			// For each node, the cost of getting from the start node to that node.
			Dictionary<Vector2Int, float> gScore = new Dictionary<Vector2Int, float>();
			for (int i = 0; i < width; i++)
				for (int j = 0; j < height; j++)
					gScore[new Vector2Int(i, j)] = float.MaxValue;
			gScore[goal] = 0;

			// For each node, the total cost of getting from the start node to the goal
			// by passing by that node. That value is partly known, partly heuristic.
			Dictionary<Vector2Int, float> fScore = new Dictionary<Vector2Int, float>();
			for (int i = 0; i < width; i++)
				for (int j = 0; j < height; j++)
					fScore[new Vector2Int(i, j)] = float.MaxValue;
			fScore[goal] = HeuristicCostEstimate(goal, start);

			while (openSet.Count > 0)
			{
				Vector2Int current = openSet[0];
				for (int i = 1; i < openSet.Count; i++)
					if (fScore[openSet[i]] < fScore[current])
						current = openSet[i];

				if (current == start)
				{
					Path path = new Path();
					path.position = goal;
					ReconstructPath(cameFrom, current, path);
					paths.Add(path);
					break;
				}

				openSet.Remove(current);
				closedSet.Add(current);

				foreach (Vector2Int neighbour in Neighbours(current))
				{
					if (closedSet.Contains(neighbour))
						continue; // Ignore the neighbor which is already evaluated.

					float tentative_gScore = gScore[current] + 1; // 1 is the distance from current to a neighbor

					if (!openSet.Contains(neighbour)) // Discover a new node
						openSet.Add(neighbour);
					else if (tentative_gScore >= gScore[neighbour])
						continue; // This is not a better path.

					// This path is the best until now. Record it!
					cameFrom[neighbour] = current;
					gScore[neighbour] = tentative_gScore;
					fScore[neighbour] = gScore[neighbour] + HeuristicCostEstimate(neighbour, start);
				
					if (map.ContainsKey(neighbour))
					{
						Path path = map[neighbour];
						if (path.parent == null || gScore[current] < gScore[path.parent.position])
						{
							path.parent = new Path();
							path.parent.position = current;
						}

						// Return early


					}
					else
					{
						Path path = new Path();
						path.position = neighbour;
						path.parent = new Path();
						path.parent.position = current;
						map[neighbour] = path;
					}
				}
			}
		}

		return paths;
	}
}
