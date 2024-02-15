using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

public class Search 
{
	public delegate bool BreakCondition<Node>(Node current, float cost);

	public static List<Node> AStar<Node>(Node start, Node goal, IGraph<Node> graph, BreakCondition<Node> breakOn = null)
	{
		if (breakOn == null)
			breakOn = (node, cost) => false;

		Dictionary<Node, Node> parents = new Dictionary<Node, Node>();
		
		Dictionary<Node, float> fScore = new Dictionary<Node, float>();
		Dictionary<Node, float> gScore = new Dictionary<Node, float>();
		
		float GScore(Node node) 
			=> gScore.GetValueOrDefault(node, float.PositiveInfinity);
		
		float FScore(Node node) 
			=> fScore.GetValueOrDefault(node, float.PositiveInfinity);

		IComparer<Node> comparer = Comparer<Node>.Create((a, b) => FScore(a).CompareTo(FScore(b)));
		PriorityQueue<Node> openSet = new PriorityQueue<Node>(comparer);
		
		gScore[start] = 0;
		fScore[start] = graph.HeuristicCost(start, goal);
		openSet.Enqueue(start);

		List<Node> ReconstructPath(Node current)
		{
			List<Node> path = new List<Node>();

			while (parents.ContainsKey(current))
			{
				path.Add(current);
				current = parents[current];
			}

			path.Add(start);
			path.Reverse();
			return path;
		}

		while (openSet.Count > 0)
		{
			Node current = openSet.Dequeue();

			if (current.Equals(goal) || breakOn(current, GScore(current))) // Goal reached or break condition met
			{ 
				return ReconstructPath(current);
			}	

			foreach (Node neighbour in graph.Neighbours(current))
			{
				float tentative_gScore = GScore(current) + graph.EdgeCost(current, neighbour);

				if (tentative_gScore < GScore(neighbour))
				{
					parents[neighbour] = current;

					gScore[neighbour] = tentative_gScore;
					fScore[neighbour] = tentative_gScore + graph.HeuristicCost(neighbour, goal);
					
					if (!openSet.Contains(neighbour))
						openSet.Enqueue(neighbour);
				}
			}
		}
		return null;
	}

	public static Tree<Node> AStarTree<Node>(Node start, List<Node> goals, IGraph<Node> graph)
	{
		Tree<Node> root = new Tree<Node>(start);
		Dictionary<Node, Tree<Node>> tree = new Dictionary<Node, Tree<Node>>();
		tree[start] = root;

		foreach (Node goal in goals)
		{
			List<Node> path = AStar(goal, start, graph, (node, cost) => tree.ContainsKey(node));

			for (int i = 0; i < path.Count - 1; i++)
			{
				Node current = path[i];
				Tree<Node> currentTree = tree.ContainsKey(current) ? tree[current] : new Tree<Node>(current);
				
				if (!tree.ContainsKey(current))
					tree[current] = currentTree;
				

				Node next = path[i + 1];
				Tree<Node> nextTree = tree.ContainsKey(next) ? tree[next] : new Tree<Node>(next);
				
				nextTree.children.Add(currentTree);

				if (!tree.ContainsKey(next))
					tree[next] = nextTree;
				else
					break;
			}
		}

		return root;
	}

	// TODO: Flood fill from multiple starts to a single goal, using a heuristic to select the path.
	public static void FloodFill<Node>(List<Node> starts, Node goal, IGraph<Node> graph, BreakCondition<Node> breakOn)
	{
		Queue<Node> queue = new Queue<Node>();
		foreach (Node start in starts)
			queue.Enqueue(start);

		Dictionary<Node, float> costs = new Dictionary<Node, float>();
		foreach (Node start in starts)
			costs[start] = 0;

		while (queue.Count > 0)
		{
			Node current = queue.Dequeue();
			float cost = costs[current];

			if (breakOn(current, cost))
				continue;

			foreach (Node neighbour in graph.Neighbours(current))
			{
				float edgeCost = graph.EdgeCost(current, neighbour);
				float newCost = cost + edgeCost;

				if (!costs.ContainsKey(neighbour) || newCost < costs[neighbour])
				{
					costs[neighbour] = newCost;
					queue.Enqueue(neighbour);
				}
			}
		}
	}

	public static void FloodFill<Node>(Node start, IGraph<Node> graph, BreakCondition<Node> breakOn)
	{
		Queue<Node> queue = new Queue<Node>();
		queue.Enqueue(start);

		Dictionary<Node, float> costs = new Dictionary<Node, float>();
		costs[start] = 0;

		while (queue.Count > 0)
		{
           
			Node current = queue.Dequeue();
			float cost = costs[current];

			if (breakOn(current, cost))
				return;

			foreach (Node neighbour in graph.Neighbours(current))
			{
				float edgeCost = graph.EdgeCost(current, neighbour);

				if (edgeCost == float.PositiveInfinity)
					continue;

				float newCost = cost + edgeCost;

				if (!costs.ContainsKey(neighbour) || newCost < costs[neighbour])
				{
					costs[neighbour] = newCost;
					queue.Enqueue(neighbour);
				}
			}
		}
	}

	public delegate bool VisitNode(Vector2Int node, int steps);

	public static List<Vector2Int> Flood(Vector2Int start, int max, VisitNode callback)
	{
		return Flood(new List<Vector2Int>() { start }, max, callback);
	}

	public static List<Vector2Int> Flood(List<Vector2Int> start, int max, VisitNode callback)
	{
		List<Vector2Int> visited = new List<Vector2Int>();
		void Helper(Vector2Int start, int steps, int max, VisitNode callback)
		{
			if (max > -1 && steps >= max)
				return;

			if (visited.Contains(start))
				return;

			visited.Add(start);

			if (callback(start, steps))
				return;

			steps++;

			Helper(start + Vector2Int.right, steps, max, callback);
			Helper(start + Vector2Int.left, steps, max, callback);
			Helper(start + Vector2Int.up, steps, max, callback);
			Helper(start + Vector2Int.down, steps, max, callback);
		}

		for (int i = 0; i < start.Count; i++)
			Helper(start[i], 0, max, callback);

		return visited;
	}

	public static List<Vector2Int> Flood(Vector2Int start, int max)
	{
		return Flood(start, max, (_, _) => false);
	}
}

public class Tree<Node>
{
	public Node node;
	public List<Tree<Node>> children;

	public Tree(Node node)
	{
		this.node = node;
		children = new List<Tree<Node>>();
	}

	public void Traverse(System.Action<Tree<Node>> action)
	{
		action(this);
		foreach (Tree<Node> child in children)
			child.Traverse(action);
	}
}