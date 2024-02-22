using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static Search;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

/// <summary>
/// This file is long but needs to be.
/// Map generation and other spatial stuff where a square is better uses chunks.
/// Navigation uses areas, ramps and nodes.
/// </summary>

[ExecuteInEditMode]
public class Map : MonoBehaviour, IGraph<Node>, IOnValidateListener<NoiseSettings>
{
	[Header("Settings")]
	[SerializeField]
	public TerrainGenerationSettings terrainSettings;
	public Chunk chunkPrefab;
	public int chunkSize;

	[Header("Generated Data")]
	[SerializeField] SerializableDictionary<Vector2Int, Chunk> chunks;
	[SerializeField] List<Area> areas = new List<Area>();
	[SerializeField] List<Ramp> ramps = new List<Ramp>();
	[SerializeField] List<AbstractObstruction> obstructions = new List<AbstractObstruction>();
	[SerializeField] SerializableDictionary<Node, SerializableDictionary<Node, float>> adjacency;

	[Header("Mouse")]
	public Vector2Int mouseTile;
	public Vector3 mouseMesh;
	public Vector2Int mouseChunkCoord;
	public Chunk mouseChunk;
	public Area mouseArea;

	[Header("Gizmos")]
	public bool drawNodes;
	public bool drawEdges;
	public bool drawTiles;
	public bool drawRamps;
	public bool drawCliffs;

	[Header("Debugging")]
	public Transform start;
	public Transform goal;
	public List<Transform> goals = new List<Transform>();
	public AbstractObstruction obstruction;
	public Transform ray;

	/// <summary>
	/// This function regenerates the whole level.
	/// </summary>
	[ContextMenu("Regenerate")]
	public void Regenerate()
	{
		Debug.Log("----- Regenerating -----");

		// Delete all
		DeleteAll();
		
		// ChunkManager
		for (int x = 0; x <= 1; x++)
			for (int y = 0; y <= 1; y++)
				GetOrCreateChunk(x, y);

		// Areas
		IdentifyAreas();

		// Nodes
		foreach (Area area in areas)
			IdentifyNodes(area);

		// Ramps
		ramps = IdentifyRamps();
		SmoothRampVertices();
		RemoveRampCliffs();
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

	public Agent selected;


	void Update()
	{
		mouseTile = Vector2Int.zero;
		mouseMesh = Vector3.zero;
		mouseChunk = null;
		mouseChunkCoord = Vector2Int.zero;
		mouseArea = null;

		if (Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, out mouseChunk))
		{
			mouseMesh = hit.point;
			mouseTile = ToTileCoord(hit.point);
			mouseChunkCoord = ToChunkCoord(mouseTile);
			mouseArea = GetArea(mouseTile.x, mouseTile.y);
		
			if (Input.GetMouseButtonDown(0) && selected != null)
			{
				selected.SetPath(AStar(selected.transform.position, hit.point));
			}
		}
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

	public static Vector2Int ToTileCoord(Vector3 position)
	{
		return Vector2Int.RoundToInt(position.XZ());
	}

	public Vector2Int ToChunkCoord(Vector2Int tile)
	{
		int AdjustForNegative(int coordinate) =>
			(coordinate < 0) ? (coordinate - chunkSize + 1) : coordinate;

		return new Vector2Int
		(
			AdjustForNegative(tile.x) / chunkSize,
			AdjustForNegative(tile.y) / chunkSize
		);
	}

	

	public Chunk GetOrCreateChunk(int x, int y)
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

	public Chunk CreateChunk(int x, int y)
	{
		if (chunkPrefab == null)
			return null;

		Chunk chunk = Instantiate(chunkPrefab);
		chunk.transform.parent = transform;
		chunk.name = "Chunk [" + x + ", " + y + "]";
		chunk.chunkPosition = new Vector2Int(x, y);
		//chunk.size = Vector2Int.one * chunkSize;
		//chunk.terrainSettings.offset = new Vector3(-0.5f, 0f, -0.5f);
		//chunk.height = SampleHeight;
		chunks.Add(new Vector2Int(x, y), chunk);
		chunk.Generate();
		//IdentifyAreas(chunk);
		return chunk;
	}

	public Area GetArea(int x, int y)
	{
		foreach (Area area in areas)
			if (area.Contains(x, y))
				return area;
		return null;
	}

	public bool TryGetArea(int id, out Area area)
	{
		area = null;
		if (id < 0 || id >= areas.Count)
			return false;

		area = areas[id];
		return true;
	}

	Node GetNode(int x, int z)
	{
		Area area = GetArea(x, z);
		if (area == null)
			return null;
		return area.GetNode(x, z);
	}

	bool RemoveNode(Node node)
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

	Node AddNode(Vector2Int tile, Vector3 position, Area area)
	{
		Node node = new Node(tile, position, area.ID);
		return AddNode(node, area);
	}

	Node AddNode(Node node, Area area)
	{
		//if (nodes.ContainsKey(tile))
		//	return nodes[tile];

		//Node node = new Node(tile, position, id);

		foreach (Node existing in area.Nodes)
		{
			HashSet<IObstruction> visited = new HashSet<IObstruction>();

			if (Voxel2D.Line(node.tile, existing.tile, Vector2.one, -0.5f * Vector2.one, (n, steps) =>
			{
				if (!area.Contains(n.x, n.y))
					return true;

				//if (obstructions.TryGetValue((node, existing), out HashSet<IObstruction> set))
				//{
				//	foreach (IObstruction obstruction in set)
				//	{
				//		if (visited.Contains(obstruction))
				//			continue;
				//
				//		if (obstruction.Contains(n))
				//			visited.Add(obstruction);
				//	}
				//}

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
				if (Codirectional(new_dir, old_dir))
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

	public IEnumerable<Node> Neighbours(Node current)
	{
		if (adjacency.TryGetValue(current, out SerializableDictionary<Node, float> dictionary))
			return dictionary.Keys;

		return new List<Node>();
		//return current.neighbours.Keys;
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

	/// <summary>
	/// Returns whether the tiles that intersect the line between start and end are unobstructed.
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

	bool TryGetRamp(Vector2Int tile, out Ramp ramp)
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

	void SetAdjacency(Node from, Node to, float cost)
	{
		SerializableDictionary<Node, float> dictionary;
		if (!adjacency.TryGetValue(from, out dictionary))
		{
			dictionary = new SerializableDictionary<Node, float>();
			adjacency[from] = dictionary;
		}
		dictionary[to] = cost;
	}

	void Connect(Node a, Node b)
	{
		float cost = Vector3.Distance(a.position, b.position);
		SetAdjacency(a, b, cost);
		SetAdjacency(b, a, cost);
	}

	void Disconnect(Node a, Node b)
	{
		SerializableDictionary<Node, float> dictionary;

		if (adjacency.TryGetValue(a, out dictionary))
			dictionary.Remove(b);

		if (adjacency.TryGetValue(b, out dictionary))
			dictionary.Remove(a);
	}

	/// <summary>
	/// Runs the A* algorithm to find a path between start and goal.
	/// </summary>
	/// <param name="start"></param>
	/// <param name="goal"></param>
	/// <returns></returns>
	public List<Node> AStar(Vector3 start, Vector3 goal)
	{
		Node TempStart(float x, float z, Node tempEnd = null)
		{
			Vector3 position = OnGround(x, z);
			Vector2Int tile = ToTileCoord(position);
			Area area = GetArea(tile.x, tile.y);

			int areaID = area != null ? area.ID : -1;

			Node temp = new Node(tile, position, areaID);

			if (area != null)
			{
				foreach (Node node in area.Nodes)
					if (IsVisible(new Vector2(x, z), node.tile, true, true))
						SetAdjacency(temp, node, Vector3.Distance(position, node.position));
			}

			// we could be on a ramp
			else if (TryGetRamp(tile, out Ramp ramp))
			{
				Debug.Log("On ramp");
				foreach (Node node in ramp.Nodes)
					SetAdjacency(temp, node, Vector3.Distance(position, node.position));
			}

			if (tempEnd != null)
			{
				if (IsVisible(temp.tile, tempEnd.tile, true, true))
					SetAdjacency(temp, tempEnd, Vector3.Distance(position, tempEnd.position));
			}

			return temp;
		}

		Node TempEnd(float x, float z)
		{
			Vector3 position = OnGround(x, z);
			Vector2Int tile = ToTileCoord(position);
			Area area = GetArea(tile.x, tile.y);
			int areaID = area != null ? area.ID : -1;

			Node temp = new Node(tile, position, areaID);
			if (area != null)
			{
				foreach (Node node in area.Nodes)
					if (IsVisible(node.tile, new Vector2(x, z), true, true))
						SetAdjacency(temp, node, Vector3.Distance(position, node.position));
							//node.neighbours[temp] = Vector3.Distance(position, node.position);
			}
			return temp;
		}

		Node tempEnd = TempEnd(goal.x, goal.z);
		Node tempStart = TempStart(start.x, start.z, tempEnd);

		List<Node> path = Search.AStar(tempStart, tempEnd, this, (Node node, float cost) =>
		{
			return false;// IsVisible(node.position.XZ(), tempEnd.position.XZ(), true, true);
		});

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
	

	public bool IsCliff(Vector2Int tile)
	{
		Vector2Int chunkCoord = ToChunkCoord(tile);
		if (TryGetChunk(chunkCoord, out Chunk chunk))
		{
			tile -= chunkCoord * chunkSize;
			return chunk.cliffs[tile.x + tile.y * chunkSize];
		}

		return false;
	}

	public int GetIncrement(Vector2Int tile)
	{
		Vector2Int chunkCoord = ToChunkCoord(tile);
		if (TryGetChunk(chunkCoord, out Chunk chunk))
		{
			tile -= chunkCoord * chunkSize;
			return chunk.increments[tile.x + tile.y * chunkSize];
		}

		return -1;
	}

	public bool IsAccessible(Vector2Int tile)
	{
		Vector2Int chunkCoord = ToChunkCoord(tile);
		if (TryGetChunk(chunkCoord, out Chunk chunk))
		{
			tile -= chunkCoord * chunkSize;
			return !chunk.cliffs[tile.x + tile.y * chunkSize];
		}

		return false;
	}

	void ScatterCliffDecos(Chunk chunk)
	{
		Random.InitState(chunk.chunkPosition.x * 1000 + chunk.chunkPosition.y * 1000);
		for (int x = 0; x < chunkSize; x++)
			for (int y = 0; y < chunkSize; y++)
			{
				Vector2Int tile = new Vector2Int(x, y) + chunk.chunkPosition * chunkSize;
				if (IsCliff(tile))
				{
					int number = Random.Range(5, 10);
					int subGrid = 4;

					HashSet<int> unique_indices = new HashSet<int>();
					while (unique_indices.Count < number)
						unique_indices.Add(Random.Range(0, subGrid * subGrid));

					foreach (int index in unique_indices)
					{
						int i = index % subGrid;
						int j = index / subGrid;

						Vector3 position = OnGround(tile.x + (float)i / subGrid - 0.5f, tile.y + (float)j / subGrid - 0.5f);
						//Instantiate(cliffDecoPrefab, position, Quaternion.identity);
						Gizmos.DrawSphere(position + Vector3.up * 0.125f, 0.06125f);
					}

					//Instantiate(cliffDecoPrefab, position, Quaternion.identity);
				}
			}

	}

	// This can surely be shortened.
	List<Ramp> IdentifyRamps(bool drawGizmos = false)
	{
		List<Ramp> ramps = new List<Ramp>();

		foreach (Chunk chunk in chunks.Values)
		{
			for (int cy = 0; cy < chunkSize; cy++)
			{
				int start = -1;
				int incrementTop = -1;
				int incrementBottom = -1;
				for (int cx = 0; cx < chunkSize; cx++)
				{
					Vector2Int tile = new Vector2Int(cx, cy) + chunk.chunkPosition * chunkSize;
					int i00 = GetIncrement(tile);
					int i02 = GetIncrement(tile + Vector2Int.up * 2);

					bool c00 = IsCliff(tile);
					bool c01 = IsCliff(tile + Vector2Int.up);
					bool c02 = IsCliff(tile + Vector2Int.up * 2);

					if (start > -1)
					{
						bool validSite = !c00 && c01 && !c02 && i00 == incrementTop && i02 == incrementBottom;
						if (!validSite)
						{
							int end = cx - 1;
							int length = end - start + 1;

							if (length >= 3)
							{
								Vector2Int r00 = new Vector2Int(start + 1, cy);
								Vector2Int r10 = new Vector2Int(end - 1, cy);
								Vector2Int r01 = new Vector2Int(start + 1, cy + 2);
								Vector2Int r11 = new Vector2Int(end - 1, cy + 2);

								r00 += chunk.chunkPosition * chunkSize;
								r10 += chunk.chunkPosition * chunkSize;
								r01 += chunk.chunkPosition * chunkSize;
								r11 += chunk.chunkPosition * chunkSize;

								Area areaTop = GetArea(r00.x, r00.y);
								Area areaBottom = GetArea(r01.x, r01.y);

								Vector3 p00 = OnGround(r00.x, r00.y);
								Vector3 p10 = OnGround(r10.x, r10.y);
								Vector3 p01 = OnGround(r01.x, r01.y);
								Vector3 p11 = OnGround(r11.x, r11.y);

								if (drawGizmos)
								{
									Gizmos.color = Color.yellow;
									Gizmos.DrawLine(p00, p10);
									Gizmos.DrawLine(p01, p11);
									Gizmos.DrawLine(p00, p01);
									Gizmos.DrawLine(p10, p11);
								}
								else
								{
									Node n00 = AddNode(r00, p00, areaTop);
									Node n01 = AddNode(r01, p01, areaBottom);

									// Add the nodes to both areas!
									//areaBottom.AddNode(n00);
									//areaTop.AddNode(n01);

									Connect(n00, n01);

									Node n10 = null;
									Node n11 = null;

									if (r00 != r10)
									{
										n10 = AddNode(r10, p10, areaTop);
										n11 = AddNode(r11, p11, areaBottom);

										// Add the nodes to both areas!
										//areaBottom.AddNode(n10);
										//areaTop.AddNode(n11);

										// Connect other end
										Connect(n10, n11);
										
										// Connect diagonals
										Connect(n00, n11);
										Connect(n10, n01);
									}

									Ramp ramp = new Ramp();
									ramp.start = new Vector2Int(start, cy) + chunk.chunkPosition * chunkSize;
									ramp.orientation = Orientation.HORIZONTAL;
									ramp.length = length - 1;
									ramp.n00 = n00;
									ramp.n01 = n01;
									ramp.n10 = n10;
									ramp.n11 = n11;
									ramps.Add(ramp);
								}
							}

							start = -1;
							incrementTop = -1;
							incrementBottom = -1;
						}
					}
					else
					{
						//horrible
						bool validSite = !c00 && c01 && !c02 && Mathf.Abs(i00 - i02) == 1 && i00 >= 0 && i02 >= 0;
						if (validSite)
						{
							start = cx;
							incrementTop = i00;
							incrementBottom = i02;
						}
					}
				}
			}

			for (int cx = 0; cx < chunkSize; cx++)
			{
				int start = -1;
				int incrementLeft = -1;
				int incrementRight = -1;

				for (int cy = 0; cy < chunkSize; cy++)
				{
					Vector2Int tile = new Vector2Int(cx, cy) + chunk.chunkPosition * chunkSize;

					int i00 = GetIncrement(tile);
					int i20 = GetIncrement(tile + Vector2Int.right * 2);

					bool c00 = IsCliff(tile);
					bool c10 = IsCliff(tile + Vector2Int.right);
					bool c20 = IsCliff(tile + Vector2Int.right * 2);

					if (start > -1)
					{
						// horrible
						bool validSite = !c00 && c10 && !c20 && i00 == incrementLeft && i20 == incrementRight;
						if (!validSite)
						{
							int end = cy - 1;

							int length = end - start + 1;
							if (length >= 3)
							{
								Vector2Int r00 = new Vector2Int(cx, start + 1);
								Vector2Int r10 = new Vector2Int(cx, end - 1);
								Vector2Int r01 = new Vector2Int(cx + 2, start + 1);
								Vector2Int r11 = new Vector2Int(cx + 2, end - 1);

								r00 += chunk.chunkPosition * chunkSize;
								r10 += chunk.chunkPosition * chunkSize;
								r01 += chunk.chunkPosition * chunkSize;
								r11 += chunk.chunkPosition * chunkSize;

								Area areaLeft = GetArea(r00.x, r00.y);
								Area areaRight = GetArea(r01.x, r01.y);

								Vector3 p00 = OnGround(r00.x, r00.y);
								Vector3 p10 = OnGround(r10.x, r10.y);
								Vector3 p01 = OnGround(r01.x, r01.y);
								Vector3 p11 = OnGround(r11.x, r11.y);

								if (drawGizmos)
								{
									Gizmos.color = Color.yellow;
									Gizmos.DrawLine(p00, p10);
									Gizmos.DrawLine(p01, p11);
									Gizmos.DrawLine(p00, p01);
									Gizmos.DrawLine(p10, p11);
								}
								else
								{
									Node n00 = AddNode(r00, p00, areaLeft);
									Node n01 = AddNode(r01, p01, areaRight);
									//areaRight.AddNode(n00);
									//areaLeft.AddNode(n01);

									Connect(n00, n01);

									Node n10 = null;
									Node n11 = null;

									if (r00 != r10)
									{
										n10 = AddNode(r10, p10, areaLeft);
										n11 = AddNode(r11, p11, areaRight);
										//areaRight.AddNode(n10);
										//areaLeft.AddNode(n11);

										Connect(n10, n11);
										Connect(n00, n11);
										Connect(n10, n01);
									}

									Ramp ramp = new Ramp();
									ramp.start = new Vector2Int(cx, start) + chunk.chunkPosition * chunkSize;
									ramp.orientation = Orientation.VERTICAL;
									ramp.length = length - 1;
									ramp.n00 = n00;
									ramp.n01 = n01;
									ramp.n10 = n10;
									ramp.n11 = n11;
									ramps.Add(ramp);
								}
							}
							start = -1;
							incrementLeft = -1;
							incrementRight = -1;
						}
					}
					else
					{
						bool validSite = !c00 && c10 && !c20 && Mathf.Abs(i00 - i20) == 1 && i00 >= 0 && i20 >= 0;
						if (validSite)
						{
							start = cy;
							incrementLeft = i00;
							incrementRight = i20;
						}
					}
				}
			}
		}


		foreach (Ramp ramp in ramps)
		{

		}

		return ramps;
	}

	void RemoveRampCliffs()
	{
		foreach (Ramp ramp in ramps)
		{
			Vector2Int V00 = ramp.start + (ramp.orientation == Orientation.HORIZONTAL ? Vector2Int.right : Vector2Int.up);
			Vector2Int V11 = ramp.start + new Vector2Int(ramp.orientation == Orientation.HORIZONTAL ? ramp.length - 1 : 2, ramp.orientation == Orientation.VERTICAL ? ramp.length - 1 : 2);

			for (int y = V00.y; y <= V11.y; y++)
				for (int x = V00.x; x <= V11.x; x++)
				{
					int chunkX = x / chunkSize;
					int chunkY = y / chunkSize;

					Vector2Int localPosition = new Vector2Int(x, y) - new Vector2Int(chunkX * chunkSize, chunkY * chunkSize);

					if (TryGetChunk(chunkX, chunkY, out Chunk chunk))
					{
						chunk.cliffs[localPosition.x + localPosition.y * chunkSize] = false;
					}
				}
		}
	}

	void SmoothRampVertices(bool drawGizmos = false)
	{
		// Iterates over all the ramps and 
		// raises or lowers the vertices to create a smooth gradient.
		// A ramp is defined as a rectangle, and the vertices inside (but not on the edges) are smoothed.
		// Ramps are either horizontal or vertical, and the vertices are smoothed in the direction of the ramp.

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


			Vector2Int V00 = ramp.start + Vector2Int.one;
			Vector2Int V11 = ramp.start + new Vector2Int(ramp.orientation == Orientation.HORIZONTAL ? ramp.length : 2, ramp.orientation == Orientation.VERTICAL ? ramp.length : 2);

			float height0 = ramp.n00.position.y;
			float height1 = ramp.n01.position.y;

			Gizmos.color = Color.red;
			for (int y = V00.y; y <= V11.y; y++)
			{

				for (int x = V00.x; x <= V11.x; x++)
				{
					int chunkX = x / chunkSize;
					int chunkY = y / chunkSize;

					Vector2Int localPosition = new Vector2Int(x, y) - new Vector2Int(chunkX * chunkSize, chunkY * chunkSize);

					float height = ramp.orientation != Orientation.HORIZONTAL ? Mathf.Lerp(height0, height1, (x - V00.x + 1) / (float)(V11.x - V00.x + 2)) : Mathf.Lerp(height0, height1, (y - V00.y + 1) / (float)(V11.y - V00.y + 2));

					if (drawGizmos)
					{

						Gizmos.DrawSphere(new Vector3(x - 0.5f, height, y - 0.5f), 0.1f);
						Gizmos.color = Color.yellow;
					}
					else
					if (TryGetChunk(chunkX, chunkY, out Chunk chunk))
					{
						AddUpdate(chunk, localPosition, height);
					}

				}
			}

		}

		foreach (var (chunk, list) in updates)
			chunk.UpdateVertices(list);

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

				AddNode(tile, OnGround(tile.x, tile.y), area);
			}
		}
	}

	void OnDrawGizmos()
	{
		if (ray != null)
		{
			Ray r = new Ray(ray.position, ray.forward);

			if (Voxel2D.Ray(r, 100, Vector2.one, -0.5f * Vector2.one, obstruction))
				Gizmos.color = Color.green;
			else
				Gizmos.color = Color.red;


			Gizmos.DrawRay(r);
		}

		if (start != null) start.position = OnGround(start.position.x, start.position.z);
		if (goal != null) goal.position = OnGround(goal.position.x, goal.position.z);


		foreach (Chunk chunk in chunks.Values)
			ScatterCliffDecos(chunk);

		if (drawCliffs)
		{
			Gizmos.color = Color.red;
			foreach (Chunk chunk in chunks.Values)
			{
				for (int x = 0; x < chunkSize; x++)
					for (int y = 0; y < chunkSize; y++)
					{
						Vector2Int tile = new Vector2Int(x, y) + chunk.chunkPosition * chunkSize;
						if (IsCliff(tile))
							Gizmos.DrawCube(OnGround(tile.x, tile.y), Vector3.one);
					}
			}
		}



		if (start != null && goal != null)
		{
			List<Node> path = AStar(start.position, goal.position);

			if (path != null)
			{
				Gizmos.color = Color.blue;

				for (int i = 1; i < path.Count; i++)
				{
					Gizmos.DrawLine(path[i - 1].position, path[i].position);
					Gizmos.DrawSphere(path[i].position, 0.5f);
				}
			}

			//foreach (Node neighbour in tempStart.Neighbours)
			//{
			//	Gizmos.color = Color.red;
			//	Gizmos.DrawLine(tempStart.position, neighbour.position);	
			//}


			//if (TryGetArea(tempStart.area, out Area startArea))
			//{
			//	foreach (Node node in startArea.Nodes)
			//		Gizmos.DrawSphere(node.position, 0.5f);
			//}

		}
		{
			int i = 0;
			foreach (Area area in areas)
			{
				//	areaIndex %= areas.Count;
				//	areaIndex += areas.Count;
				//	areaIndex %= areas.Count;
				//	mouseArea = areas[areaIndex];

				if (drawTiles && area.Tiles != null)
				{
					Gizmos.color = Color.HSVToRGB((float)i / areas.Count, 1f, 1f);
					Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);
					foreach (Vector2Int tile in area.Tiles)
					{
						Gizmos.DrawCube
						(
							new Vector3(tile.x, terrainSettings.Sample(tile.x, tile.y), tile.y),
							new Vector3(1f, 0.001f, 1f)
						);
					}
				}

				//if (false)
				if (drawNodes || drawEdges || GetNode(mouseTile.x, mouseTile.y) != null)
				{
					GUIStyle style = new GUIStyle();
					style.normal.textColor = Color.red;
					foreach (Node node in area.Nodes)
					{
						if (drawNodes)
						{
							Gizmos.color = Color.black;
							Gizmos.DrawSphere(node.position, 0.25f);
						}

						if (drawEdges || node.tile == mouseTile)
						{
							foreach (var pair in adjacency)
							{
								Node n = pair.Key;
								SerializableDictionary<Node, float> dictionary = pair.Value;
								foreach (var pair2 in dictionary)
								{
									Node n2 = pair2.Key;
									float cost = pair2.Value;
									if (n == node)
									{
										Gizmos.color = Color.red;
										Gizmos.DrawLine(n.position, n2.position);
										Handles.Label((n.position + n2.position) / 2.0f, cost.ToString(), style);
									}
								}
							}
						}
					}
				}
				i++;
			}
		}
		if (drawRamps)
		{
			Gizmos.color = Color.yellow;
			foreach (Ramp ramp in ramps)
			{
				Vector2Int start = ramp.start;
				Vector2Int startEx = start + ramp.length * (ramp.orientation == Orientation.HORIZONTAL ? Vector2Int.right : Vector2Int.up);
				Vector2Int end = start + 2 * (ramp.orientation == Orientation.HORIZONTAL ? Vector2Int.up : Vector2Int.right);
				Vector2Int endEx = startEx + 2 * (ramp.orientation == Orientation.HORIZONTAL ? Vector2Int.up : Vector2Int.right);

				Vector3 p00 = OnGround(start.x, start.y);
				Vector3 p10 = OnGround(startEx.x, startEx.y);
				Vector3 p01 = OnGround(end.x, end.y);
				Vector3 p11 = OnGround(endEx.x, endEx.y);

				Gizmos.DrawLine(p00, p10);
				Gizmos.DrawLine(p01, p11);
				Gizmos.DrawLine(p00, p01);
				Gizmos.DrawLine(p10, p11);


			}

			//SmoothRampVertices(true);
		}
	}
}
