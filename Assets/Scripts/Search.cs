using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Search;

public class Search 
{
	public delegate bool BreakOn<Node>(Node current, float cost);

	public static List<Node> AStar<Node>(Node start, Node goal, IGraph<Node> graph, BreakOn<Node> breakOn = null)
	{
		if (breakOn == null)
			breakOn = (node, cost) => false;

		List<Node> path = new List<Node>();

		void ReconstructPath(Dictionary<Node, Node> cameFrom, Node current)
		{
			if (cameFrom.ContainsKey(current))
				ReconstructPath(cameFrom, cameFrom[current]);
			path.Add(current);
		}

		List<Node> closedSet = new List<Node>();
		List<Node> openSet = new List<Node> { start };

		// For each node, which node it can most efficiently be reached from.
		// If a node can be reached from many nodes, cameFrom will eventually contain the
		// most efficient previous step.
		Dictionary<Node, Node> parents = new Dictionary<Node, Node>();
		Dictionary<Node, float> gScore = new Dictionary<Node, float>();
		Dictionary<Node, float> fScore = new Dictionary<Node, float>();
		
		gScore[start] = 0;
		
		fScore[start] = graph.HeuristicCost(start, goal);

		float GScore(Node node) => gScore.ContainsKey(node) ? gScore[node] : float.MaxValue;
		float FScore(Node node) => fScore.ContainsKey(node) ? fScore[node] : float.MaxValue;

		while (openSet.Count > 0)
		{
			Node current = openSet[0];
			for (int i = 1; i < openSet.Count; i++)
				if (FScore(openSet[i]) < FScore(current))
					current = openSet[i];

			if (current.Equals(goal) || breakOn(current, GScore(current))) // Goal reached or break condition met
			{
				ReconstructPath(parents, current);
				return path;
			}

			openSet.Remove(current);
			closedSet.Add(current);

			foreach (Node neighbour in graph.Neighbours(current))
			{
				if (closedSet.Contains(neighbour))
					continue; // Ignore the neighbor which is already evaluated.

				
				float tentative_gScore = GScore(current) + graph.EdgeCost(current, neighbour); // Add the edge cost to the neighbour

				if (!openSet.Contains(neighbour)) // Discover a new node
					openSet.Add(neighbour);

				else if (tentative_gScore >= GScore(neighbour))
					continue; // This is not a better path.

				// This path is the best until now. Record it!
				parents[neighbour] = current;
				gScore[neighbour] = tentative_gScore;
				
				fScore[neighbour] = GScore(neighbour) + graph.HeuristicCost(neighbour, goal);
			}
		}

		// Open set is empty but goal was never reached
		return path;
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
	public static void FloodFill<Node>(List<Node> starts, Node goal, IGraph<Node> graph, BreakOn<Node> breakOn)
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

	public static void FloodFill<Node>(Node start, IGraph<Node> graph, BreakOn<Node> breakOn)
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
				float newCost = cost + edgeCost;

				if (!costs.ContainsKey(neighbour) || newCost < costs[neighbour])
				{
					costs[neighbour] = newCost;
					queue.Enqueue(neighbour);
				}
			}
		}
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