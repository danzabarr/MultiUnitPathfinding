using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class Area 
{
	private static int idCounter = 0;

	[SerializeField] private int id;
	[SerializeField] private int increment;
	[SerializeField] private HashSet<Vector2Int> tiles;
	private Dictionary<Vector2Int, Node> nodes;
	// an obstruction is removed, i need the list of nodes that are affected
	public Dictionary<(Node, Node), HashSet<IObstruction>> obstructions;
	public Dictionary<IObstruction, HashSet<(Node, Node)>> dependencies;
	
	public void AddObstruction(IObstruction obstruction)
	{
		HashSet<(Node, Node)> affectedEdges = new HashSet<(Node, Node)>();
		foreach (Node a in nodes.Values)
		{
			foreach (Node b in a.Neighbours)
			{
				bool obstructed = false;
				Voxel2D.Line(a.tile, b.tile, Vector2.one, -0.5f * Vector2.one, (node, steps) =>
				{
					if (!tiles.Contains(node))
						return true;

					if (obstruction.IsObstructed(node))
					{
						obstructed = true;
						return true;
					}

					// also get obstructions

					return false;
				});

				if (obstructed)
					affectedEdges.Add((a, b));
			}
		}

		dependencies[obstruction] = affectedEdges;
		foreach ((Node a, Node b) in affectedEdges)
		{
			obstructions[(a, b)].Add(obstruction);
			obstructions[(b, a)].Add(obstruction);
		}
	}

	public void RemoveObstruction(IObstruction obstruction)
	{
		foreach ((Node a, Node b) in dependencies[obstruction])
		{
			obstructions[(a, b)].Remove(obstruction);
			obstructions[(b, a)].Remove(obstruction);
		}
		dependencies.Remove(obstruction);
	}

	public int count;
	//public Dictionary<Node, HashSet<Vector2Int>> neighbours;
	
	public Area(int increment, ICollection<Vector2Int> tiles)
	{
		this.increment = increment;
		this.tiles = new HashSet<Vector2Int>(tiles);
		obstructions = new Dictionary<(Node, Node), HashSet<IObstruction>>();
		nodes = new Dictionary<Vector2Int, Node>();
		id = idCounter++;
	}

	public Node GetNode(int x, int z)
	{
		return nodes.GetValueOrDefault(new Vector2Int(x, z));
	}

	public int CountConnections()
	{
		count = 0;

		foreach (Node node in Nodes)
			count += node.NeighbourCount;

		return count;
	}

	static bool Codirectional(Vector2Int a, Vector2Int b)
	{
		// Check if either vector is the zero vector
		if (a == Vector2Int.zero || b == Vector2Int.zero)
			return false;

		// Direct comparison for equality
		if (a == b)
			return true;

		// Handling cases where one of b's components is zero
		if (b.x == 0 || b.y == 0)
		{
			// If both components of b are zero, a must also be zero for them to be codirectional (already checked)
			// If only one component of b is zero, the corresponding component of a must also be zero, and check the other component for positive integer multiple
			if ((b.x == 0 && a.x != 0) || (b.y == 0 && a.y != 0))
				return false;

			// Check the non-zero component for being a positive integer multiple
			int nonZeroComponentRatio = (b.x == 0) ? a.y / b.y : a.x / b.x;
			return nonZeroComponentRatio > 0 && ((b.x == 0) ? a.y % b.y == 0 : a.x % b.x == 0);
		}

		// Ensuring both components of b are non-zero, check for integer multiple and same direction
		if (a.x % b.x != 0 || a.y % b.y != 0)
			return false;

		int xRatio = a.x / b.x;
		int yRatio = a.y / b.y;

		return xRatio == yRatio && xRatio > 0;
	}

	public Node AddNode(Vector2Int tile, Vector3 position)
	{
		if (nodes.ContainsKey(tile))
			return nodes[tile];

		Node node = new Node(tile, position, id);

		foreach (Node existing in nodes.Values)
		{
			HashSet<IObstruction> visited = new HashSet<IObstruction>();

			if (Voxel2D.Line(node.tile, existing.tile, Vector2.one, -0.5f * Vector2.one, (n, steps) =>
			{
				if (!tiles.Contains(n))
					return true;

				//if (obstructions.TryGetValue((node, existing), out HashSet<IObstruction> set))
				//{
				//	foreach (IObstruction obstruction in set)
				//	{
				//		if (visited.Contains(obstruction))
				//			continue;
				//
				//		if (obstruction.IsObstructed(n))
				//			visited.Add(obstruction);
				//	}
				//}

				return false;

			})) continue;

			// from the existing node to the new node
			Vector2Int new_dir = tile - existing.tile;
			float new_cost = Vector2.Distance(tile, existing.tile);
			bool abort = false;
			
			foreach (Node neighbour in existing.Neighbours)
			{
				// from the existing node to its neighbour
				Vector2Int old_dir = neighbour.tile - existing.tile;

				// If the existing node already has a codirectional neighbour
				if (Codirectional(new_dir, old_dir))
				{
					float old_cost = Vector2.Distance(neighbour.tile, existing.tile);
					// If the new node is closer to the neighbour, then disconnect the existing node from the neighbour
					if (new_cost < old_cost)
						Node.Disconnect(existing, neighbour);
					
					// If the new node is further from the neighbour, then skip adding the new node
					else
					{
						abort = true;
						break;
					}
				}
			}

			if (abort)
				continue;

			Node.Connect(existing, node);

			foreach (IObstruction obstruction in visited)
			{
				obstructions[(existing, node)].Add(obstruction);
				obstructions[(node, existing)].Add(obstruction);

				dependencies[obstruction].Add((existing, node));
				dependencies[obstruction].Add((node, existing));
			}
		}

		return nodes[tile] = node;
	}

	public int TileCount => tiles == null ? 0 : tiles.Count;
	public int NodeCount => nodes == null ? 0 : nodes.Count;
	public int ID => id;
	public bool Contains(int x, int y) => tiles != null && tiles.Contains(new Vector2Int(x, y));
	public bool IsNode(int x, int y) => nodes != null && nodes.ContainsKey(new Vector2Int(x, y));
	public IEnumerable<Node> Nodes => nodes == null ? new Node[0] : nodes.Values;
	public IEnumerable<Vector2Int> Tiles => tiles;
	public IEnumerable<IObstruction> Obstructions => dependencies?.Keys;
}
