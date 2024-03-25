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
	public GameObject bridgePrefab;
	public int chunkSize;

	[Header("Generated Data")]
	 
	[SerializeField] protected SerializableDictionary<Vector2Int, Chunk> chunks = new SerializableDictionary<Vector2Int, Chunk>();
	[SerializeField] protected List<Area> areas = new List<Area>();
	[SerializeField] protected List<Ramp> ramps = new List<Ramp>();
	[SerializeField] protected List<AbstractObstruction> obstructions = new List<AbstractObstruction>();
	

	// this is a big boy
	protected Dictionary<Node, Dictionary<Node, float>> adjacency = new Dictionary<Node, Dictionary<Node, float>>();
	
	void Start()
	{
		Regenerate();
	}

	void OnValidate()
	{
		// Regenerate();
	}

	/// <summary>
	/// This function regenerates the whole level.
	/// </summary>
	[ContextMenu("Regenerate")]
	public void Regenerate()
	{
		Debug.Log("----- Regenerating -----");
		DeleteAll();
		CreateChunks();
		IdentifyObstructions();
		IdentifyAreas();
		IdentifyRamps();
		IdentifyBridges();
		IdentifyNodes();
		//UpdateWaypoints();

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

		DeleteBridges();
		chunks?.Clear();
		areas?.Clear();
		ramps?.Clear();
		obstructions?.Clear();
	}

	void CreateChunks()
	{
		for (int x = 0; x <= 2; x++)
			for (int y = 0; y <= 2; y++)
				GetOrCreateChunk(x, y);
	}

	protected List<Bridge> bridges = new List<Bridge>();
	
	[ContextMenu("Delete Bridges")]
	public void DeleteBridges()
	{
		foreach (Bridge bridge in bridges)
		{
			Node start = bridge.n0;
			Node end = bridge.n1;

			RemoveNode(start);
			RemoveNode(end);
			bridge.Delete();
		}

		bridges.Clear();
	}

	void IdentifyObstructions()
	{
		foreach (AbstractObstruction obstruction in FindObjectsOfType<AbstractObstruction>())
			AddObstruction(obstruction);
	}

	/// <summary>
	/// A bridge is another kind of path that connects two areas.
	/// We look for sites where a bridge can be placed.
	/// Must go 1xFLAT, 0-2xCLIFF 1-4xWATER 0-2xCLIFF 1xFLAT in a straight line.
	/// </summary>
	/// 
	[ContextMenu("Identify Bridges")]
	public void IdentifyBridges()
	{
		HashSet<Vector2Int> sites = new HashSet<Vector2Int>();

		bool ValidSite(Vector2Int tile, Vector2Int dir, out int length)
		{
			int max = 8;
			int min = 3;
			int increment = -1;
			bool foundWater = false;
			length = 0;
			Area startArea = GetArea(tile.x, tile.y);
			if (startArea == null)
				return false;

			for (; length < max; length++)
			{
				Vector2Int pos = tile + dir * length;
				int o = GetPermanentObstructionType(pos);

				if (o == Chunk.OUT_OF_BOUNDS)
					return false;

				if (o == Chunk.RAMP)
					return false;

				if (sites.Contains(pos))
					return false;

				if (length == 0)
				{
					if (o != Chunk.FLAT)
						return false;
					increment = GetIncrement(pos);
				}

				else if (length > min && o == Chunk.FLAT)
				{
					if (!foundWater)
						return false;

					int inc = GetIncrement(pos);
					if (inc != increment)
						return false;

					Area endArea = GetArea(pos.x, pos.y);
					if (endArea == null)
						return false;

					if (endArea == startArea)
						return false;

					return true;
				}

				else 
				{
					if (o == Chunk.FLAT)
						return false;

					if (o == Chunk.OUT_OF_BOUNDS)
						return false;

					if (o == Chunk.RAMP)
						return false;

					int inc = GetIncrement(pos);

					// can't go through hills
					if (inc > increment)
						return false;

					if (o == Chunk.WATER)
						foundWater = true;
					// otherwise we're good.
				}
			}

			// If we reach the end of the loop, then we didn't find a valid site.
			return false;
		}

		Bridge TryAddBridge(Vector2Int tile, Vector2Int dir)
		{
			if (sites.Contains(tile))
				return null;

			if (ValidSite(tile, dir, out int length))
			{
				for (int i = 0; i <= length; i++)
				{
					Vector2Int bridgeTile = tile + dir * i;
					sites.Add(bridgeTile);
				}

				return new Bridge
				{
					start = tile,
					length = length,
					orientation = dir == Vector2Int.right ? Orientation.HORIZONTAL : Orientation.VERTICAL,
					//n0 = AddNode(tile),
					//n1 = AddNode(tile + dir * length)
				};
			}

			return null;
		}

		

		List<Bridge> tentativeBridges = new List<Bridge>();
		
		foreach (Chunk chunk in chunks.Values)
		{
			foreach (Vector2Int tile in chunk.Tiles)
			{
				Bridge right = TryAddBridge(tile, Vector2Int.right);
				if (right != null)
				{
					tentativeBridges.Add(right);
					continue;
				}
			
				Bridge up = TryAddBridge(tile, Vector2Int.up);
				if (up != null)
				{
					tentativeBridges.Add(up);
					continue;
				}
			}
		}

		List<Bridge> SelectBridges(List<Bridge> list)
		{
			// we need only one bridge per pair of areas
			// we prefer shorter bridges

			Dictionary<Vector2Int, Bridge> dict = new Dictionary<Vector2Int, Bridge>();
			
			Vector2Int Ordered(int a1, int a2) => a1 < a2 ? new Vector2Int(a1, a2) : new Vector2Int(a2, a1);

			bool Add(Bridge bridge)
			{
				Area area1 = GetArea(bridge.start.x, bridge.start.y);
				Area area2 = GetArea(bridge.start.x + bridge.length * (bridge.orientation == Orientation.HORIZONTAL ? 1 : 0), bridge.start.y + bridge.length * (bridge.orientation == Orientation.VERTICAL ? 1 : 0));
				int a1 = area1 == null ? -1 : area1.ID;
				int a2 = area2 == null ? -1 : area2.ID;

				Vector2Int key = Ordered(a1, a2);
				if (dict.TryGetValue(key, out Bridge existing))
				{
					if (existing.length < bridge.length)
						return false;
				}

				dict[key] = bridge;
				return true;
			}

			foreach (Bridge bridge in list)
				Add(bridge);

			return new List<Bridge>(dict.Values);
		}

		void AddBridge(Bridge bridge)
		{
			bridges.Add(bridge);

			Vector2Int tile = bridge.start;
			Vector2Int dir = bridge.orientation == Orientation.HORIZONTAL ? Vector2Int.right : Vector2Int.up;
			int length = bridge.length;

			// add the nodes
			bridge.n0 = AddNode(tile);
			bridge.n1 = AddNode(tile + dir * length);

			Connect(bridge.n0, bridge.n1);

			// Lower vertices along the bridge to no greater than the height of the terrain at the start and end of the bridge.

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

			float height = OnGround(tile.x, tile.y).y;
			for (int j = 0; j < 2; j++)
			for (int i = 0; i <= length; i++)
			{
				Vector2Int bridgeTile = tile + dir * i + new Vector2Int(dir.y, dir.x) * j;
				int o = GetPermanentObstructionType(bridgeTile);
				if (o == Chunk.WATER)
					continue;					
				int e = GetPermanentObstructionType(bridgeTile + Vector2Int.right);
				if (e == Chunk.WATER)
					continue;
				int n = GetPermanentObstructionType(bridgeTile + Vector2Int.up);
				if (n == Chunk.WATER)
					continue;


				Vector2Int chunkCoord = bridgeTile.ToChunkCoord(chunkSize);
				Vector2Int localPosition = bridgeTile - chunkCoord * chunkSize;
				if (TryGetChunk(chunkCoord, out Chunk chunk))
				{
					AddUpdate(chunk, localPosition, height);
				}

				// Also need to update the neighbour if the local position is on the edge.

				if (localPosition.x == 0
					&& TryGetChunk(chunkCoord + Vector2Int.left, out Chunk left))
					AddUpdate(left, new Vector2Int(chunkSize, localPosition.y), height);

				if (localPosition.x == chunkSize - 1
					&& TryGetChunk(chunkCoord + Vector2Int.right, out Chunk right))
					AddUpdate(right, new Vector2Int(0, localPosition.y), height);

				if (localPosition.y == 0 
					&& TryGetChunk(chunkCoord + Vector2Int.down, out Chunk down))
					AddUpdate(down, new Vector2Int(localPosition.x, chunkSize), height);

				if (localPosition.y == chunkSize - 1 
					&& TryGetChunk(chunkCoord + Vector2Int.up, out Chunk up))
					AddUpdate(up, new Vector2Int(localPosition.x, 0), height);
			}

			// Push the updates

			for (int i = 0; i <= length; i++)
			{
				Vector2Int bridgeTile = tile + dir * i;

				Vector2Int chunkCoord = bridgeTile.ToChunkCoord(chunkSize);
				if (TryGetChunk(chunkCoord, out Chunk chunk))
				{
					Vector2Int localPosition = bridgeTile - chunk.chunkPosition * chunkSize;
					chunk.SetBridge(localPosition.x, localPosition.y);
				}

				GameObject go = Instantiate(bridgePrefab, transform);
				go.transform.position = new Vector3(bridgeTile.x, height, bridgeTile.y);
				go.transform.forward = new Vector3(dir.x, 0, dir.y);
				bridge.pieces.Add(go);
			}

			foreach (var (chunk, list) in updates)
				chunk.UpdateVertices(list);
		}

		

		List<Bridge> selection = SelectBridges(tentativeBridges);
		foreach (Bridge bridge in selection)
			AddBridge(bridge);
	}

	/// <summary>
	/// A ramp is a path that connects two areas.
	/// We look for sites where a ramp can be placed.
	/// We want two tiles from different areas with a cliff between them.
	/// We look left and right from the cliff to determine the length of the ramp.
	/// </summary>
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

	void IdentifyNodes()
	{
		foreach (Area area in areas)
			IdentifyNodes(area);
	}

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

	void RecalculateConnections(Area area)
	{
		List<Node> nodes = new List<Node>(area.Nodes);
		foreach(Node node in nodes)
			RemoveNode(node);

		foreach(Node node in nodes)
			AddNode(node);
	}

	void AddObstruction(AbstractObstruction obstruction)
	{
		if (!obstructions.Contains(obstruction))
			obstructions.Add(obstruction);

		List<Vector2Int> tiles = new List<Vector2Int>();
		foreach (Vector2Int tile in obstruction)
			tiles.Add(tile);

		if (tiles.Count == 0)
			return;

		List<Area> affectedAreas = new List<Area>();
		foreach (Vector2Int tile in tiles)
		{
			Area area = GetArea(tile.x, tile.y);
			if (area != null && !affectedAreas.Contains(area))
				affectedAreas.Add(area);
		}

		foreach (Area area in affectedAreas)
			RecalculateConnections(area);
	}

	void RemoveObstruction(AbstractObstruction obstruction)
	{
		List<Vector2Int> tiles = new List<Vector2Int>();
		foreach (Vector2Int tile in obstruction)
			tiles.Add(tile);
		obstructions.Remove(obstruction);

		if (tiles.Count == 0)
			return;

		List<Area> affectedAreas = new List<Area>();
		foreach (Vector2Int tile in tiles)
		{
			Area area = GetArea(tile.x, tile.y);
			if (area != null && !affectedAreas.Contains(area))
				affectedAreas.Add(area);
		}

		foreach (Area area in affectedAreas)
			RecalculateConnections(area);
	}

	public void UpdateObstruction(AbstractObstruction obstruction)
	{
		if (obstruction == null)
			return;

		RemoveObstruction(obstruction);

		if (obstruction.enabled)
			AddObstruction(obstruction);
	}

	public void UpdateWaypoint(Waypoint wp)
	{
		if (wp == null)
			return;

		RemoveNode(wp.Node);
		
		Vector3 position = wp.transform.position;
		Vector2Int tile = position.ToTileCoord();
		Area area = GetArea(tile.x, tile.y);
		
		wp.Node.position = position;
		wp.Node.tile = tile;
		wp.Node.area = area == null ? -1 : area.ID;

		if (wp.enabled)
			AddNode(wp.Node);
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
		foreach (AbstractObstruction obstruction in obstructions)
			if (obstruction.Contains(tile))
				return false;

		Vector2Int chunkCoord = tile.ToChunkCoord(chunkSize);
		if (TryGetChunk(chunkCoord, out Chunk chunk))
		{
			tile -= chunkCoord * chunkSize;
			return chunk.GetPermanentObstructionType(tile.x, tile.y) >= Chunk.BRIDGE;
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

	protected bool TryGetBridge(Vector2Int tile, out Bridge bridge)
	{
		foreach (Bridge b in bridges)
			if (b.Contains(tile))
			{
				bridge = b;
				return true;
			}

		bridge = null;
		return false;
	}

	public Area GetArea(int id)
	{
		foreach (Area area in areas)
			if (area.ID == id)
				return area;
		return null;
	}

	public Area GetArea(int x, int y)
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

		if (AddNode(node))
			return node;

		return null;
	}

	private bool AddNodeOnRamp(Node node)
	{
		int o = GetPermanentObstructionType(node.tile);

		if (o != Chunk.RAMP)
			return false;
		
		if (!TryGetRamp(node.tile, out Ramp ramp))
			return false;
	
		Connect(node, ramp.n00);
		Connect(node, ramp.n01);

		if (ramp.n10 != null)
			Connect(node, ramp.n10);

		if (ramp.n11 != null)
			Connect(node, ramp.n11);

		// now find all other waypoint nodes on the ramp

		return true;
	}

	protected bool AddNode(Node node)
	{
		if (node == null)
			return false;
		
		Area area = GetArea(node.area);
		if (area == null)
		{
			// we are likely on a ramp or a bridge

			int o = GetPermanentObstructionType(node.tile);

			if (o == Chunk.RAMP)
			{
				if (!TryGetRamp(node.tile, out Ramp ramp))
					return false;
			
				Connect(node, ramp.n00);
				Connect(node, ramp.n01);

				if (ramp.n10 != null)
					Connect(node, ramp.n10);

				if (ramp.n11 != null)
					Connect(node, ramp.n11);

				return true;
			}

			else if (o == Chunk.BRIDGE)
			{
				if (!TryGetBridge(node.tile, out Bridge bridge))
					return false;

				Connect(node, bridge.n0);
				Connect(node, bridge.n1);
				
				return true;
			}

			return false;
		}

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
		return true;
	}

	protected void Connect(Node from, Node to, float cost)
	{
		if (!adjacency.TryGetValue(from, out Dictionary<Node, float> dictionary))
		{
			dictionary = new Dictionary<Node, float>();
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
		Dictionary<Node, float> dictionary;

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
		if (adjacency.TryGetValue(current, out Dictionary<Node, float> dictionary))
			return dictionary.Keys;

		return new List<Node>();
	}

	public int NeighbourCount(Node current)
	{
		return adjacency.TryGetValue(current, out Dictionary<Node, float> dictionary) ? dictionary.Count : 0;
	}

	public float EdgeCost(Node current, Node next)
	{
		if (adjacency.TryGetValue(current, out Dictionary<Node, float> dictionary))
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

	
	protected Node TempStart(float x, float z, Node tempEnd = null)
	{
		Vector3 position = OnGround(x, z);
		Vector2Int tile = position.ToTileCoord();
		Area area = GetArea(tile.x, tile.y);
		int areaID = area != null ? area.ID : -1;

		// Create a temporary node for the start position.
		// Connect the start node to all the visible nodes in the area
		Node temp = new Node(tile, position, areaID);
		if (area != null)
		{
			foreach (Node node in area.Nodes)
				if (IsVisible(new Vector2(x, z), node.position.XZ(), true, true))
					Connect(temp, node, false);
		}

		// we could be on a ramp
		else if (TryGetRamp(tile, out Ramp ramp))
		{
			foreach (Node node in ramp.Nodes)
				Connect(temp, node, false);
		}

		// Also try to connect to the end node
		if (tempEnd != null)
		{
			if (IsVisible(temp.position.XZ(), tempEnd.position.XZ(), true, true))
				Connect(temp, tempEnd, false);
		}

		return temp;
	}

	protected Node TempEnd(float x, float z)
	{
		Vector3 position = OnGround(x, z);
		Vector2Int tile = position.ToTileCoord();
		Area area = GetArea(tile.x, tile.y);
		int areaID = area != null ? area.ID : -1;

		// Create a temporary node for the end position.
		// Connect all the visible nodes in the area to the end node.
		Node temp = new Node(tile, position, areaID);
		if (area != null)
		{
			foreach (Node node in area.Nodes)
				if (IsVisible(node.position.XZ(), new Vector2(x, z), true, true))
					Connect(node, temp, false);
		}
		return temp;
	}

	public List<Node> AStar(Node start, Node end)
	{
		return Search.AStar(start, end, this, (node, cost) =>
		{
			return false;//!IsVisible(node.position.XZ(), end.position.XZ(), true, true);
		});
	}

	public List<Node> AStar(Vector3 start, Node goal)
	{
		Node tempStart = TempStart(start.x, start.z);
		List<Node> path = Search.AStar(tempStart, goal, this, (_,_) => { return false; });
		RemoveNode(tempStart);
		return path;
	}

	public List<Node> AStar(Node start, Vector3 goal)
	{
		Node tempEnd = TempEnd(goal.x, goal.z);
		List<Node> path = Search.AStar(start, tempEnd, this, (_,_) => { return false; });
		RemoveNode(tempEnd);
		return path;
	}

	/// <summary>
	/// Runs the A* algorithm to find a path between position and goal.
	/// </summary>
	/// <param name="start"></param>
	/// <param name="goal"></param>
	/// <returns></returns>
	public List<Node> AStar(Vector3 start, Vector3 goal)
	{
		Node tempEnd = TempEnd(goal.x, goal.z);
		Node tempStart = TempStart(start.x, start.z, tempEnd);

		List<Node> path = Search.AStar(tempStart, tempEnd, this, (Node node, float cost) =>
		{
			return false;//IsVisible(node.position.XZ(), tempEnd.position.XZ(), true, true);
		});

		//if (false && path != null && path.Count > 0)
		//{
		//	Gizmos.color = Color.blue;
		//	foreach (Node neighbour in Neighbours(path[0]))
		//	{
		//		Gizmos.DrawLine(path[0].position, neighbour.position);
		//	}
		//}

		RemoveNode(tempStart);
		RemoveNode(tempEnd);

		return path;
	}

	/// <summary>
	/// Does physics raycasts on all the chunk meshes and returns the first hit.
	/// </summary>
	/// <param name="ray"></param>
	/// <param name="hit"></param>
	/// <param name="chunk"></param>
	/// <returns></returns>
	public bool Raycast(Ray ray, out RaycastHit hit, out Chunk chunk)
	{
		hit = new RaycastHit();
		chunk = null;
		foreach (Chunk c in chunks.Values)
			if (c.Raycast(ray, out hit))
			{
				chunk = c;
				return true;
			}

		return false;
	}
}
