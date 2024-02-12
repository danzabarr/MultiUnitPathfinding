using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static Search;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

[ExecuteInEditMode]
public class Map : MonoBehaviour, IGraph<Node>, IOnValidateListener<NoiseSettings>
{
	[Header("Prefabs")]
    public Chunk chunkPrefab;

	[Header("Map Settings")]
	public int chunkSize;
	public float snapIncrement;
	public float incrementSize;
	public NoiseSettings noise;
	public NoiseSettings riverNoise;

	[Header("Mouse")]
	public Vector2Int mouseTile;
	public Vector3 mouseMesh;
	public Chunk mouseChunk;
	public Vector2Int mouseChunkCoord;
	public Area mouseArea;

	[Header("Gizmos")]
	public bool drawNodes;
	public bool drawEdges;
	public bool drawTiles;
	public bool drawRamps;

	[Header("Debugging")]
	public Transform start;
	private Node tempStart;
	public int connections = 0;

	[Header("Lists")]
	[SerializeField] List<Area> areas = new List<Area>();
	[SerializeField] List<Ramp> ramps = new List<Ramp>();
	[SerializeField] Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
	[SerializeField] HashSet<Vector2Int> riverTiles = new HashSet<Vector2Int>();
	
	[ContextMenu("Regenerate")]
	public void Regenerate()
	{
		Debug.Log("----- Regenerating -----");

		// Delete all
		DeleteAll();
		
		// Chunks
		for (int x = 0; x <= 1; x++)
			for (int y = 0; y <= 1; y++)
				GetOrCreateChunk(x, y);

		// Areas
		areas = IdentifyAreas();

		// Nodes
		foreach (Area area in areas)
			IdentifyNodes(area);

		// Ramps
		ramps = IdentifyRamps();
		
		// Count connections
		connections = 0;
		foreach (Area area in areas)
			connections += area.CountConnections();
	}

	[ContextMenu("Delete All")]
	public void DeleteAll()
	{
		DeleteChunks();
		DeleteAreas();
		DeleteRamps();
		DestroyChildren();
	}

	public void DeleteChunks()
	{
		if (chunks == null)
			return;

		foreach (var chunk in chunks)
			if (chunk.Value != null)
				DestroyImmediate(chunk.Value.gameObject);

		chunks.Clear();
	}

	void DeleteAreas()
	{
		areas.Clear();
	}

	void DeleteRamps()
	{
		ramps.Clear();
	}

	void DestroyChildren()
	{
		for (int i = transform.childCount - 1; i >= 0; i--)
			DestroyImmediate(transform.GetChild(i).gameObject);
	}

	public Vector2Int WorldToTile(Vector3 position)
	{
		return Vector2Int.RoundToInt(position.XZ());
	}
	public Vector2Int TileToChunk(Vector2Int tile)
	{
		int AdjustForNegative(int coordinate) => 
			(coordinate < 0) ? (coordinate - chunkSize + 1) : coordinate;

		return new Vector2Int
		(
			AdjustForNegative(tile.x) / chunkSize, 
			AdjustForNegative(tile.y) / chunkSize
		);
	}

	public bool Raycast(Ray ray, out RaycastHit hit, out Chunk chunk)
	{
		hit = new RaycastHit();
		chunk = null;
		foreach (var c in chunks)
			if (c.Value.Raycast(ray, out hit))
			{ 
				chunk = c.Value;
				return true;
			} 

		return false;
	}

	public bool IsCliff(Vector2Int tile)
	{
		Vector2Int chunkCoord = TileToChunk(tile);
		if (chunks.TryGetValue(chunkCoord, out Chunk chunk))
		{
			tile -= chunkCoord * chunkSize;

			return chunk.IsCliff(tile.x, tile.y);
		}

		return false;
	}

	public bool IsAccessible(Vector2Int tile)
	{
		if (SampleHeight(tile.x, tile.y) <= -0.5f)
			return false;
		
		Vector2Int chunkCoord = TileToChunk(tile);
		if (chunks.TryGetValue(chunkCoord, out Chunk chunk))
		{
			tile -= chunkCoord * chunkSize;
			if (chunk.IsCliff(tile.x, tile.y))
				return false;

			return true;
		}

		return false;
	}

	public Vector3 OnGround(float x, float z)
	{
		return new Vector3(x, SampleHeight(x, z), z);
	}

	public int GetIncrement(Vector2Int tile)
	{
		Vector2Int chunkCoord = TileToChunk(tile);
		if (chunks.TryGetValue(chunkCoord, out Chunk chunk))
		{
			tile -= chunkCoord * chunkSize;
			return chunk.increments[tile.x + tile.y * chunkSize];
		}

		return -1;
	}

	public float SampleHeight(float x, float z)
	{
#if UNITY_EDITOR
		noise.AddListener(this);
		riverNoise.AddListener(this);
#endif
		float value = noise.Sample(x, z);

		float riverValue = riverNoise.Sample(x, z);
		value *= riverValue;
		value += (riverValue - 1) * incrementSize;

		if (snapIncrement > 0)
		{
			int increment = Mathf.RoundToInt(value / snapIncrement);
			value = increment * incrementSize;
		}
		if 
		(
			riverTiles.Contains(Vector2Int.FloorToInt(new Vector2(x, z))) || 
			riverTiles.Contains(Vector2Int.FloorToInt(new Vector2(x, z + 1))) || 
			riverTiles.Contains(Vector2Int.FloorToInt(new Vector2(x + 1, z))) || 
			riverTiles.Contains(Vector2Int.FloorToInt(new Vector2(x + 1, z + 1)))
		)
			value -= incrementSize * 0.5f;

		return value;
	}

	static Vector2Int[] DIAGONAL_DIRECTIONS = new Vector2Int[]
	{
		new Vector2Int(1, 0),
		new Vector2Int(1, 1),
		new Vector2Int(0, 1),
		new Vector2Int(-1, 1),
		new Vector2Int(-1, 0),
		new Vector2Int(-1, -1),
		new Vector2Int(0, -1),
		new Vector2Int(1, -1)
	};

	public bool IsVisible(Vector2 start, Vector2 end)
	{
		if (start == end)
			return true;

		return !Voxel2D.Line(start, end, Vector2.one, -0.5f * Vector2.one, (node, steps) =>
		{
			return !IsAccessible(node);
		});
	}

	void OnEnable()
	{  
		Debug.Log("Adding listener");
		AssemblyReloadEvents.afterAssemblyReload += Regenerate;
	}

	void OnDisable()
	{
		Debug.Log("Removing listener");
		AssemblyReloadEvents.afterAssemblyReload -= Regenerate;
	}

	public void OnScriptValidated(NoiseSettings script)
	{
		noise.AddListener(this);
		ExecuteAfter(seconds: 0.01f, () => Regenerate());
		//Regenerate();
	}


#if UNITY_EDITOR
	//public void OnValidate()
	//{
		//if (autoUpdate) ExecuteAfter(seconds: 0.01f, () => Generate());
	//}

	/// <summary>Executes a function after short period of time</summary>
	/// <param name="seconds">delay time from now</param>
	/// <param name="theDelegate">The function which will be called</param>
	void ExecuteAfter(float seconds, Action theDelegate)
	{
		if (isActiveAndEnabled)
			StartCoroutine(ExecuteAfterPrivate(seconds, theDelegate));
	}

	IEnumerator ExecuteAfterPrivate(float seconds, Action theDelegate)
	{
		yield return new WaitForSeconds(seconds);
		theDelegate();
	}
#endif

	public Area GetArea(int x, int y)
	{
		foreach (Area area in areas)
			if (area.Contains(x, y))
				return area;
		return null;
	}

    void Start()
    {
		Regenerate();
	}

	void OnValidate()
	{
	}

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
			mouseTile = WorldToTile(hit.point);
			mouseChunkCoord = TileToChunk(mouseTile);
			mouseArea = CreateArea(mouseTile);
		}
	}

	Node GetNode(int x, int z)
	{
		Area area = GetArea(x, z);
		if (area == null)
			return null;
		return area.GetNode(x, z);
	}

	Node Temp(float x, float z)
	{
		Vector3 position = OnGround(x, z);
		Vector2Int tile = WorldToTile(position);
		Area area = GetArea(tile.x, tile.y);
		int areaID = area != null ? area.ID : -1;

		Node temp = new Node(tile, position, areaID);


		if (area != null)
		foreach (Node node in area.Nodes)
			if (IsVisible(new Vector2(x, z), node.tile))
				temp.neighbours[node] = Vector3.Distance(position, node.position);

		return temp;
	}

	Area CreateArea(Vector2Int tile)
	{
		return CreateArea(tile, new HashSet<Vector2Int>());
	}

	Area CreateArea(Vector2Int tile, HashSet<Vector2Int> visited)
	{
		List<Vector2Int> area = new List<Vector2Int>();
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

		if (increment < 0) return null;

		return new Area(increment, area);
	}

	List<Area> IdentifyAreas()
	{
		HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

		List<Area> areas = new List<Area>();

		foreach (Chunk chunk in chunks.Values)
		{
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

		return areas;
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
				
				area.AddNode(tile, OnGround(tile.x, tile.y));
			}
		}
	}

	List<Ramp> IdentifyRamps(bool drawGizmos = false)
	{
		// This can surely be shortened.
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
									Node n00 = areaTop.AddNode(r00, p00);
									Node n01 = areaBottom.AddNode(r01, p01);
									Node.Connect(n00, n01);

									Node n10 = null;
									Node n11 = null;
									if (r00 != r10)
									{
										n10 = areaTop.AddNode(r10, p10);
										n11 = areaBottom.AddNode(r11, p11);
										Node.Connect(n10, n11);

										Node.Connect(n00, n11);
										Node.Connect(n10, n01);
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
									Node n00 = areaLeft.AddNode(r00, p00);
									Node n01 = areaRight.AddNode(r01, p01);
									Node.Connect(n00, n01);

									Node n10 = null;
									Node n11 = null;

									if (r00 != r10)
									{
										n10 = areaLeft.AddNode(r10, p10);
										n11 = areaRight.AddNode(r11, p11);
										Node.Connect(n10, n11);
										Node.Connect(n00, n11);
										Node.Connect(n10, n01);
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

		return ramps;
	}

	/*
	 * 
	 * 
	public int maxBridgeLength = 10;
	public bool onlyConnectDifferentAreas = true;

	public void IdentifyBridgeSite()
	{
	}
	 * 
	 */

	public Vector3 WeightedRandomPoint(int seed, float x0, float x1, float y0, float y1)
	{
		float MAX_HEIGHT = 2f;
		int tries = 10000;
		
		while (tries-- > 0)
		{
			Random.InitState(seed + tries);
			float x = Random.Range(x0, x1);
			float z = Random.Range(y0, y1);
			float r = Random.value;
			float y = noise.Sample(x, z);

			//Debug.Log(x + ", " + y + ", " + z + " = " + r + " < " + y / MAX_HEIGHT);

			if (r < y / MAX_HEIGHT)
				return new Vector3(x, y, z);
		}

		return Vector3.zero;
	}

	Chunk GetOrCreateChunk(int x ,int y)
	{
		if (chunks == null)
			chunks = new Dictionary<Vector2Int, Chunk>();

		if (chunks.TryGetValue(new Vector2Int(x, y), out Chunk get))
			return get;

		return CreateChunk(x, y);
	}

	Chunk CreateChunk(int x, int y)
	{
		if (chunkPrefab == null)
			return null;

		Chunk chunk = Instantiate(chunkPrefab);
		chunk.transform.parent = transform;
		chunk.name = "Chunk [" + x + ", " + y + "]";
		chunk.chunkPosition = new Vector2Int(x, y);
		//chunk.size = Vector2Int.one * chunkSize;
		chunk.offset = new Vector3(-0.5f, 0f, -0.5f);
		//chunk.height = SampleHeight;
		chunks.Add(chunk.chunkPosition, chunk);
		chunk.Generate();
		//IdentifyAreas(chunk);
		return chunk;
	}

	public IEnumerable<Node> Neighbours(Node current)
	{
		return current.neighbours.Keys;
	}

	public float EdgeCost(Node current, Node next)
	{
		return current.GetCost(next);
	}

	public float HeuristicCost(Node current, Node next)
	{
		return Vector3.Distance(current.position, next.position);
	}

	void OnDrawGizmos()
	{
		Node temp = Temp(start.position.x, start.position.z);
		Node goal = GetNode(mouseTile.x, mouseTile.y);

		if (goal != null)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawSphere(goal.position, 0.5f);
		}
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(temp.position, 0.5f);

		//foreach (Node neighbour in temp.Neighbours)
		//	Gizmos.DrawLine(temp.position, neighbour.position);

		if (temp != null && goal != null)
		{
			List<Node> path = AStar(temp, goal, this);

			if (path != null)
			{
				float length = 0;
				Node last = goal;
				Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
				foreach (Node node in path)
				{
					length += Vector3.Distance(last.position, node.position);
					Gizmos.DrawSphere(node.position, 0.5f);
					Gizmos.DrawLine(last.position, node.position);
					float cost = node.GetCost(last);
					Handles.Label((last.position + node.position) / 2.0f, cost.ToString());
					last = node;
				}

				Debug.Log("Path length: " + length);
			}
		}


		for (int i = 0; i < areas.Count; i++)
		//if (areas.Count > 0)
		{
			Area area = areas[i];
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
						new Vector3(tile.x, SampleHeight(tile.x, tile.y), tile.y),
						new Vector3(1f, 0.001f, 1f)
					);
				}
			}

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
						foreach (Node neighbour in node.Neighbours)
						{
							Gizmos.color = new Color(0f, 0f, 0f, 0.25f);
							Gizmos.DrawLine(node.position, neighbour.position);
							Gizmos.color = Color.red;
							Gizmos.DrawSphere(neighbour.position, 0.5f);
							float cost = node.GetCost(neighbour);

							Handles.Label((node.position + neighbour.position) / 2.0f, cost.ToString(), style);
						}
					}
				}
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
		}
	}
}
