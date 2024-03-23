using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Search;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

/// <summary>
/// This file is long but needs to be.
/// Map generation and other spatial stuff where a square is better uses chunks.
/// Navigation uses areas, ramps and nodes.
/// </summary>

public class MapGeneratorBase : MonoBehaviour, IGraph<Node>, IOnValidateListener<NoiseSettings>
{
	[Header("Settings")]
	[SerializeField]
	public TerrainGenerationSettings terrainSettings;
	public Chunk chunkPrefab;
	public int chunkSize;

	[Header("Generated Data")]
	
	protected SerializableDictionary<Vector2Int, Chunk> chunks;
	protected List<Area> areas = new List<Area>();
	protected List<Ramp> ramps = new List<Ramp>();
	protected List<AbstractObstruction> obstructions = new List<AbstractObstruction>();
	protected SerializableDictionary<Node, SerializableDictionary<Node, float>> adjacency = new SerializableDictionary<Node, SerializableDictionary<Node, float>>();

	/// <summary>
	/// This function regenerates the whole level.
	/// </summary>
	[ContextMenu("Regenerate")]
	public void Regenerate()
	{
		Debug.Log("----- Regenerating -----");

		// Delete all
		DeleteAll();

		CreateChunks();
		IdentifyAreas();
		IdentifyRamps();
		IdentifyNodes();

		foreach (Chunk chunk in chunks.Values)
			chunk.SetupOverheadCamera();

		foreach (Chunk chunk in chunks.Values)
			chunk.SetupGrass();

		foreach (Chunk chunk in chunks.Values)
			chunk.GenerateRocks();

		foreach (Chunk chunk in chunks.Values)
			chunk.RegenerateDecorations();

	}

	[ContextMenu("Delete All")]
	public void DeleteAll()
	{
		if (chunks != null)
			foreach (var chunk in chunks)
				if (chunk.Value != null)
					DestroyImmediate(chunk.Value.gameObject);

		for (int i = transform.childCount - 1; i >= 0; i--)
			DestroyImmediate(transform.GetChild(i).gameObject);

		chunks?.Clear();
		areas?.Clear();
		ramps?.Clear();
		obstructions?.Clear();
	}

	protected virtual void CreateChunks()
	{
		for (int x = 0; x <= 1; x++)
			for (int y = 0; y <= 1; y++)
				GetOrCreateChunk(x, y);
	}

	void IdentifyRamps()
	{
		ramps.Clear();

		HashSet<Vector2Int> sites = new HashSet<Vector2Int>();

		void Helper(Orientation orientation)
		{
			Vector2Int acrossVector = orientation == Orientation.HORIZONTAL ? Vector2Int.right : Vector2Int.up;
			Vector2Int alongVector = orientation == Orientation.HORIZONTAL ? Vector2Int.up : Vector2Int.right;

			bool ValidSite(Vector2Int tile)
			{
				int o = GetPermanentObstructionType(tile);
				int n = GetPermanentObstructionType(tile + alongVector);
				int s = GetPermanentObstructionType(tile - alongVector);

				return o == Chunk.CLIFF && n >= 0 && s >= 0 && n != s;
			}

			foreach (Chunk chunk in chunks.Values)
			{
				foreach (Vector2Int tile in chunk.Tiles)
				{
					if (sites.Contains(tile))
						continue; // Already found a ramp here

					// Determine the length across the ramp
					// Where 0 = 1 tile
					int length = -1;
					while (ValidSite(tile + acrossVector * (++length)))
						sites.Add(tile + acrossVector * length);
					length -= 3;

					if (length >= 0)
					{
						Node node00 = AddNode(tile + acrossVector - alongVector);
						Node node01 = AddNode(tile + acrossVector + alongVector);
						Connect(node00, node01);

						Node node10 = null;
						Node node11 = null;

						// If the ramp is longer than 0
						// We add nodes to top and bottom
						// at both ends of the ramp
						if (length > 0)
						{
							node10 = AddNode(tile + acrossVector - alongVector + acrossVector * length);
							node11 = AddNode(tile + acrossVector + alongVector + acrossVector * length);
							Connect(node10, node11);

							Connect(node00, node11);
							Connect(node10, node01);
						}

						ramps.Add(new Ramp
						(
							tile + acrossVector, length,
							orientation,
							node00, node01,
							node10, node11
						));
					}
				}
			}
		}

		Helper(Orientation.HORIZONTAL);
		Helper(Orientation.VERTICAL);


		// Raises or lowers the vertices of the terrain mesh to create a smooth gradient under a ramp.

		// We populate a dictionary with the chunks that need to be updated
		Dictionary<Chunk, List<VertexUpdate>> updates = new Dictionary<Chunk, List<VertexUpdate>>();
		void AddUpdate(Chunk chunk, Vector2Int localPosition, float height)
		{
			if (!updates.TryGetValue(chunk, out List<VertexUpdate> list))
			{
				list = new List<VertexUpdate>();
				updates[chunk] = list;
			}

			int index = localPosition.x + localPosition.y * (chunkSize + 1);
			list.Add(new VertexUpdate(index, new Vector3(localPosition.x - 0.5f, height, localPosition.y - 0.5f)));
		}

		foreach (Ramp ramp in ramps)
		{
			// Bottom left and top right corners of the ramp
			Vector2Int V00 = ramp.position;
			Vector2Int V11 = ramp.position + new Vector2Int(ramp.orientation == Orientation.HORIZONTAL ? ramp.length + 1 : 1, ramp.orientation == Orientation.VERTICAL ? ramp.length + 1 : 1);

			// Height at the bottom left and top right corners of the ramp
			float height0 = ramp.n00.position.y;
			float height1 = ramp.n01.position.y;

			for (int y = V00.y; y <= V11.y; y++)
			{
				for (int x = V00.x; x <= V11.x; x++)
				{
					Vector2Int tile = new Vector2Int(x, y);
					Vector2Int chunkCoord = tile.ToChunkCoord(chunkSize);
					Vector2Int localPosition = tile - chunkCoord * chunkSize;

					float height = ramp.orientation != Orientation.HORIZONTAL ? Mathf.Lerp(height0, height1, (x - V00.x + 1) / (float)(V11.x - V00.x + 2)) : Mathf.Lerp(height0, height1, (y - V00.y + 1) / (float)(V11.y - V00.y + 2));

					if (TryGetChunk(chunkCoord, out Chunk chunk))
						AddUpdate(chunk, localPosition, height);

					// Also need to update the neighbour if the local position is on the edge.
					// This is because the vertices on the edge are shared between chunks.
					if (localPosition.x == 0
						&& TryGetChunk(chunkCoord + Vector2Int.left, out Chunk left))
						AddUpdate(left, new Vector2Int(chunkSize, localPosition.y), height);

					if (localPosition.x == chunkSize
						&& TryGetChunk(chunkCoord + Vector2Int.right, out Chunk right))
						AddUpdate(right, new Vector2Int(0, localPosition.y), height);

					if (localPosition.y == 0
						&& TryGetChunk(chunkCoord + Vector2Int.down, out Chunk down))
						AddUpdate(down, new Vector2Int(localPosition.x, chunkSize), height);

					if (localPosition.y == chunkSize
						&& TryGetChunk(chunkCoord + Vector2Int.up, out Chunk up))
						AddUpdate(up, new Vector2Int(localPosition.x, 0), height);
				}
			}
		}

		// Push the updates
		foreach (var (chunk, list) in updates)
			chunk.UpdateVertices(list);


		// Pop the cliffs that are under the ramps
		foreach (Ramp ramp in ramps)
		{
			Vector2Int V00 = ramp.position;// (ramp.orientation == Orientation.HORIZONTAL ? Vector2Int.right : Vector2Int.up);
			Vector2Int V11 = ramp.position + new Vector2Int(ramp.orientation == Orientation.HORIZONTAL ? ramp.length : 0, ramp.orientation == Orientation.VERTICAL ? ramp.length : 0);

			for (int y = V00.y; y <= V11.y; y++)
				for (int x = V00.x; x <= V11.x; x++)
				{
					Vector2Int tile = new Vector2Int(x, y);
					Vector2Int chunkCoord = tile.ToChunkCoord(chunkSize);

					if (TryGetChunk(chunkCoord, out Chunk chunk))
					{
						Vector2Int localPosition = new Vector2Int(x, y) - chunkCoord * chunkSize;
						chunk.SetRamp(localPosition.x, localPosition.y);
					}
				}
		}
	}

	void IdentifyAreas()
	{
		areas.Clear();

		Area CreateArea(Vector2Int tile, HashSet<Vector2Int> visited)
		{
			List<Vector2Int> area = new List<Vector2Int>();
			void Helper(Vector2Int current, int steps, int max, VisitNode callback)
			{
				if (max > -1 && steps >= max)
					return;

				if (visited.Contains(current))
					return;

				visited.Add(current);

				if (callback(current, steps))
					return;

				steps++;

				Helper(current + Vector2Int.right, steps, max, callback);
				Helper(current + Vector2Int.left, steps, max, callback);
				Helper(current + Vector2Int.up, steps, max, callback);
				Helper(current + Vector2Int.down, steps, max, callback);
			}

			Helper(tile, 0, 2000, (tile, steps) =>
			{
				if (!IsAccessible(tile))
					return true;

				area.Add(tile);
				return false;
			});

			if (area.Count == 0)
				return null;

			int increment = GetIncrement(tile);

			if (increment < 0)
				return null;

			return new Area(increment, area);
		}

		HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

		foreach (Chunk chunk in chunks.Values)
		{
			//foreach (Vector2Int tile in chunk.Tiles)

			for (int x = 0; x < chunkSize; x++)
				for (int y = 0; y < chunkSize; y++)
				{
					Vector2Int tile = new Vector2Int(x, y) + chunk.chunkPosition * chunkSize;
					if (!visited.Contains(tile) && IsAccessible(tile))
					{
						Area area = CreateArea(tile, visited);
						if (area != null)
							areas.Add(area);
					}
				}
		}
	}

	// Add triangles in all the square corners, and trace the edge of the areas.
	// This forms the navigation mesh.


	static readonly Vector2Int[] DIAGONAL_DIRECTIONS = new Vector2Int[]
	{
		Vector2Int.right,
		Vector2Int.right + Vector2Int.up,
		Vector2Int.up,
		Vector2Int.left + Vector2Int.up,
		Vector2Int.left,
		Vector2Int.left + Vector2Int.down,
		Vector2Int.down,
		Vector2Int.right + Vector2Int.down
	};

	void IdentifyNodes()
	{
		foreach (Area area in areas)
			IdentifyNodes(area);
	}

	void IdentifyNodes(Area area)
	{
		foreach (Vector2Int tile in area.Tiles)
		{
			for (int i = 0; i < 8; i += 2)
			{
				Vector2Int v0 = DIAGONAL_DIRECTIONS[(i + 0) % 8] + tile;
				Vector2Int v1 = DIAGONAL_DIRECTIONS[(i + 1) % 8] + tile;
				Vector2Int v2 = DIAGONAL_DIRECTIONS[(i + 2) % 8] + tile;

				if (!area.Contains(v0.x, v0.y))
					continue;

				if (!area.Contains(v2.x, v2.y))
					continue;

				if (area.Contains(v1.x, v1.y))
					continue;

				AddNode(tile, area);
			}
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// These hooks are useful for debugging.
	/// </summary>
	void OnEnable()
	{
		AssemblyReloadEvents.afterAssemblyReload += Regenerate;
	}

	void OnDisable()
	{
		AssemblyReloadEvents.afterAssemblyReload -= Regenerate;
	}

#endif

	void Start()
	{
		Regenerate();
	}

	void OnValidate()
	{
		// Regenerate();
	}

	// This gets called if the noise changes in the editor.
	public void OnScriptValidated(NoiseSettings script)
	{
		//Regenerate();
	}

	protected Chunk GetOrCreateChunk(int x, int y)
	{
		if (chunks == null)
			chunks = new SerializableDictionary<Vector2Int, Chunk>();

		if (chunks.TryGetValue(new Vector2Int(x, y), out Chunk get))
			return get;

		return CreateChunk(x, y);
	}

	public bool TryGetChunk(int x, int y, out Chunk get)
	{
		return TryGetChunk(new Vector2Int(x, y), out get);
	}

	public bool TryGetChunk(Vector2Int tile, out Chunk get)
	{
		get = null;

		if (chunks == null)
			return false;

		return chunks.TryGetValue(tile, out get);
	}

	protected Chunk CreateChunk(int x, int y)
	{
		if (chunkPrefab == null)
			return null;

		Chunk chunk = Instantiate(chunkPrefab);
		chunk.transform.parent = transform;
		chunk.name = "Chunk [" + x + ", " + y + "]";
		chunk.chunkPosition = new Vector2Int(x, y);
		chunk.terrainSettings = terrainSettings;
		chunk.size = chunkSize * Vector2Int.one;

		chunks.Add(new Vector2Int(x, y), chunk);
		chunk.Generate();
		//IdentifyAreas(chunk);
		return chunk;
	}

	public bool IsCliff(Vector2Int tile)
	{
		Vector2Int chunkCoord = tile.ToChunkCoord(chunkSize);
		if (TryGetChunk(chunkCoord, out Chunk chunk))
		{
			tile -= chunkCoord * chunkSize;
			return chunk.GetPermanentObstructionType(tile.x, tile.y) == Chunk.CLIFF;
		}

		return false;
	}

	public int GetIncrement(Vector2Int tile)
	{
		Vector2Int chunkCoord = tile.ToChunkCoord(chunkSize);
		if (TryGetChunk(chunkCoord, out Chunk chunk))
		{
			tile -= chunkCoord * chunkSize;

			return Mathf.Max(-1, chunk.GetPermanentObstructionType(tile.x, tile.y));
		}

		return Chunk.OUT_OF_BOUNDS;
	}

	public int GetPermanentObstructionType(Vector2Int tile)
	{
		Vector2Int chunkCoord = tile.ToChunkCoord(chunkSize);
		if (TryGetChunk(chunkCoord, out Chunk chunk))
		{
			tile -= chunkCoord * chunkSize;

			return chunk.GetPermanentObstructionType(tile.x, tile.y);
		}

		return Chunk.OUT_OF_BOUNDS;
	}

	public bool IsAccessible(Vector2Int tile)
	{
		Vector2Int chunkCoord = tile.ToChunkCoord(chunkSize);
		if (TryGetChunk(chunkCoord, out Chunk chunk))
		{
			tile -= chunkCoord * chunkSize;
			return chunk.GetPermanentObstructionType(tile.x, tile.y) >= -1;
		}

		return false;
	}

	protected bool TryGetRamp(Vector2Int tile, out Ramp ramp)
	{
		foreach (Ramp r in ramps)
			if (r.Contains(tile))
			{
				ramp = r;
				return true;
			}

		ramp = null;
		return false;
	}

	protected Area GetArea(int x, int y)
	{
		foreach (Area area in areas)
			if (area.Contains(x, y))
				return area;
		return null;
	}

	protected bool TryGetArea(int id, out Area area)
	{
		area = null;
		if (id < 0 || id >= areas.Count)
			return false;

		area = areas[id];
		return true;
	}

	protected Node GetNode(int x, int z)
	{
		Area area = GetArea(x, z);
		if (area == null)
			return null;
		return area.GetNode(x, z);
	}

	protected bool RemoveNode(Node node)
	{
		if (node == null)
			return false;


		// Enumerate into a new list to avoid modifying the collection while iterating
		foreach (Node neighbour in new List<Node>(Neighbours(node)))
			Disconnect(node, neighbour);

		if (TryGetArea(node.area, out Area area))
			area.RemoveNode(node);

		return false;
	}

	protected Node AddNode(Vector2Int tile, Area area = null)
	{
		if (area == null)
			area = GetArea(tile.x, tile.y);

		Node node = new Node(tile, OnGround(tile), area == null ? -1 : area.ID);

		foreach (Node existing in area.Nodes)
		{
			HashSet<IObstruction> visited = new HashSet<IObstruction>();

			if (Voxel2D.Line(node.tile, existing.tile, Vector2.one, -0.5f * Vector2.one, (n, steps) =>
			{
				if (!area.Contains(n.x, n.y))
					return true;

				return false;

			})) continue;

			// from the existing node to the new node
			Vector2Int new_dir = node.tile - existing.tile;
			float new_cost = Vector2.Distance(node.tile, existing.tile);
			bool abort = false;

			foreach (Node neighbour in Neighbours(existing))
			{
				// from the existing node to its neighbour
				Vector2Int old_dir = neighbour.tile - existing.tile;

				// If the existing node already has a codirectional neighbour
				if (new_dir.Codirectional(old_dir))
				{
					float old_cost = Vector2.Distance(neighbour.tile, existing.tile);
					// If the new node is closer to the neighbour, then disconnect the existing node from the neighbour
					if (new_cost < old_cost)
						Disconnect(existing, neighbour);

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

			Connect(existing, node);
		}

		area.AddNode(node);
		return node;
	}

	protected void Connect(Node from, Node to, float cost)
	{
		if (!adjacency.TryGetValue(from, out SerializableDictionary<Node, float> dictionary))
		{
			dictionary = new SerializableDictionary<Node, float>();
			adjacency[from] = dictionary;
		}
		dictionary[to] = cost;
	}

	protected void Connect(Node a, Node b, bool bidirectional = true)
	{
		float cost = Vector3.Distance(a.position, b.position);
		Connect(a, b, cost);
		if (bidirectional)
			Connect(b, a, cost);
	}

	protected void Disconnect(Node a, Node b)
	{
		SerializableDictionary<Node, float> dictionary;

		if (adjacency.TryGetValue(a, out dictionary))
		{
			dictionary.Remove(b);
			if (dictionary.Count == 0)
				adjacency.Remove(a);
		}

		if (adjacency.TryGetValue(b, out dictionary))
		{
			dictionary.Remove(a);
			if (dictionary.Count == 0)
				adjacency.Remove(b);
		}
	}

	public IEnumerable<Node> Neighbours(Node current)
	{
		if (adjacency.TryGetValue(current, out SerializableDictionary<Node, float> dictionary))
			return dictionary.Keys;

		return new List<Node>();
	}

	public float EdgeCost(Node current, Node next)
	{
		if (adjacency.TryGetValue(current, out SerializableDictionary<Node, float> dictionary))
			return dictionary.GetValueOrDefault(next, float.PositiveInfinity);

		return float.PositiveInfinity;
	}

	public float HeuristicCost(Node current, Node next)
	{
		return Vector3.Distance(current.position, next.position);
	}


	/// <summary>
	/// Returns the position of the terrain at the given x and z.
	/// </summary>
	/// <param name="x"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public Vector3 OnGround(float x, float z)
	{
		return new Vector3(x, terrainSettings.Sample(x, z), z);
	}

	public Vector3 OnGround(Vector2Int tile)
	{
		return OnGround(tile.x, tile.y);
	}

	/// <summary>
	/// Returns whether the tiles that intersect the line between position and end are unobstructed.
	/// </summary>
	/// <param name="start"></param>
	/// <param name="end"></param>
	/// <param name="allowInaccessibleStart"></param>
	/// <param name="allowInaccessibleEnd"></param>
	/// <returns></returns>
	public bool IsVisible(Vector2 start, Vector2 end, bool allowInaccessibleStart = false, bool allowInaccessibleEnd = false, bool allowOverRamps = false)
	{
		if (start == end)
			return true;

		return !Voxel2D.Line(start, end, Vector2.one, -0.5f * Vector2.one, (node, steps) =>
		{
			if (node == start && allowInaccessibleStart)
				return false;

			if (node == end && allowInaccessibleEnd)
				return false;

			if (!allowOverRamps && TryGetRamp(node, out Ramp ramp))
				return true;

			return !IsAccessible(node);
		});
	}
}
