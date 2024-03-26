using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Areas are collections of connected tiles, and a set of nodes.
/// They are useful for doing navigation.
/// </summary>
[System.Serializable]
public class Area 
{
	private static int idCounter = 0;

	[SerializeField] private int id;
	[SerializeField] private int increment;
	[SerializeField] private List<Vector2Int> tiles;
	[SerializeField] private SerializableDictionary<Vector2Int, Node> nodes;
	[SerializeField] private List<Ramp> ramps;

	[SerializeField] private SerializableDictionary<(Ramp, Ramp), List<Node>> interconnections;
	// List or ramp to ramp interconnections
	// This could be used for saving longer paths

	public Area(int increment, ICollection<Vector2Int> tiles)
	{
		this.increment = increment;
		this.tiles = new List<Vector2Int>(tiles);
		nodes = new SerializableDictionary<Vector2Int, Node>();
		id = idCounter++; 
	}
	
	public Node GetNode(int x, int z)
	{
		return nodes.GetValueOrDefault(new Vector2Int(x, z));
	}

	public bool AddNode(Node node)
	{
		if (node == null)
			return false;

		if (node.area != id)
			return false;

		if (nodes.ContainsKey(node.tile))
			return false;

		if (!tiles.Contains(node.tile))
			return false;

		nodes[node.tile] = node;
		return true;
	}

	public bool RemoveNode(Node node)
	{
		if (node == null)
			return false;

		if (nodes.TryGetValue(node.tile, out Node n) && n == node)
			return nodes.Remove(node.tile);

		return false;
	}

	public int TileCount => tiles == null ? 0 : tiles.Count;
	public int NodeCount => nodes == null ? 0 : nodes.Count;
	public int ID => id;
	public bool Contains(Vector2Int tile) => tiles != null && tiles.Contains(tile);
	public bool Contains(int x, int z) => Contains(new Vector2Int(x, z));
	public bool TryGetNode(Vector2Int tile) => nodes.TryGetValue(tile, out Node node);
	public IEnumerable<Node> Nodes => nodes == null ? new Node[0] : nodes.Values;
	public IEnumerable<Vector2Int> Tiles => tiles == null ? new Vector2Int[0] : tiles;
}
