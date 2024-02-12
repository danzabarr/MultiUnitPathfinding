using System.Collections.Generic;
using UnityEngine;

public class Obstructions : Dictionary<Node, int>
{
	public new int this[Node node]
	{
		get
		{
			if (TryGetValue(node, out int value))
				return value;
			
			return 0;
		}
		set
		{
			int new_value = Mathf.Max(0, value);
			if (new_value == 0)
				Remove(node);
			
			else if (ContainsKey(node))
				this[node] = new_value;
			
			else
				Add(node, new_value);
		}
	}		
}

[System.Serializable]
public class Node
{
	public Vector2Int tile;
	public Vector3 position;
	public int area;
	public Dictionary<Node, float> neighbours;
	public Obstructions obstructions;

	public static void Connect(Node a, Node b)
	{
		float cost = Vector3.Distance(a.position, b.position);
		//Edge edge = new Edge(a, b, cost);
		a.neighbours[b] = cost;
		b.neighbours[a] = cost;
	}

	public static void Disconnect(Node a, Node b)
	{
		a.neighbours.Remove(b);
		b.neighbours.Remove(a);
	}

	public Node(Vector2Int tile, Vector3 position, int area)
	{
		this.position = position;
		this.tile = tile;
		this.area = area;
		neighbours = new Dictionary<Node, float>();
	}

	public bool IsNeighbour(Node other, out float cost)
	{
		if (neighbours.TryGetValue(other, out cost))
		{
			//cost = edge.cost;
			return true;
		}
			
		cost = float.PositiveInfinity;
		return false;
	}

	public float GetCost(Node other)
	{
		if (neighbours.TryGetValue(other, out float cost))
			return cost;
		return float.PositiveInfinity;
	}

	public IEnumerable<Node> Neighbours => neighbours.Keys;

	public int NeighbourCount => neighbours.Count;
}
