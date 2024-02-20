using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static Search;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

[System.Serializable]
public class Chunks : SerializableDictionary<Vector2Int, Chunk> { }

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
	public bool drawCliffs;

	[Header("Debugging")]
	public Transform start;
	public Transform goal;
	public List<Transform> goals = new List<Transform>();
	private Node tempStart;
	public int connections = 0;

	[Header("Lists")]
	[SerializeField] List<Area> areas = new List<Area>();
	[SerializeField] List<Ramp> ramps = new List<Ramp>();
	[SerializeField] Chunks chunks = new Chunks();
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

		SmoothRampVertices();
		RemoveRampCliffs();
		
		// Count connections
		connections = 0;
		foreach (Area area in areas)
			connections += area.CountConnections();
	}

	public struct VertexUpdate
	{
		public int index;
		public Vector3 position;

		public VertexUpdate(int index, Vector3 position)
		{
			this.index = index;
			this.position = position;
		}
	}
	void RemoveRampCliffs()
	{
		foreach (Ramp ramp in ramps)
		{
			Vector2Int V00 = ramp.start + (ramp.orientation == Orientation.HORIZONTAL ? Vector2Int.right : Vector2Int.up);
			Vector2Int V11 = ramp.start + new Vector2Int(ramp.orientation == Orientation.HORIZONTAL ? ramp.length - 1 : 2, ramp.orientation == Orientation.VERTICAL ? ramp.length - 1 : 2);

			Vector2Int v10 = new Vector2Int(V11.x, V00.y);
			Vector2Int v01 = new Vector2Int(V00.x, V11.y);

			//Area A00 = GetArea(V00.x, V00.y);
			//Area A11 = GetArea(V11.x, V11.y);
			//Area A10 = GetArea(v10.x, v10.y);
			//Area A01 = GetArea(v01.x, v01.y);
			//
			//A00?.RemoveTile(V00.x, V00.y);
			//A11?.RemoveTile(V11.x, V11.y);
			//A10?.RemoveTile(v10.x, v10.y);
			//A01?.RemoveTile(v01.x, v01.y);

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

	public Vector3 OnGround(Vector2Int tile) => OnGround(tile.x, tile.y);

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

	public bool IsVisible(Vector2 start, Vector2 end, bool allowInaccessibleStart = false, bool allowInaccessibleEnd = false)
	{
		if (start == end)
			return true;


		return !Voxel2D.Line(start, end, Vector2.one, -0.5f * Vector2.one, (node, steps) =>
		{
			if (node == start && allowInaccessibleStart)
				return false;

			if (node == end && allowInaccessibleEnd)
				return false;

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

	public bool TryGetArea(int x, int y, out Area area)
	{
		area = null;
		for (int i = 0; i < areas.Count; i++)
			if (areas[i].Contains(x, y))
			{
				area = areas[i];
				return true;
			}

		return false;
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

	Node TempStart(float x, float z)
	{
		Vector3 position = OnGround(x, z);
		Vector2Int tile = WorldToTile(position);
		Area area = GetArea(tile.x, tile.y);

		int areaID = area != null ? area.ID : -1;

		Node temp = new Node(tile, position, areaID);

		if (area != null)
		{
			foreach (Node node in area.Nodes)
				if (IsVisible(new Vector2(x, z), node.tile, true, true))
					temp.neighbours[node] = Vector3.Distance(position, node.position);
		}

		// we could be on a ramp
		else if (TryGetRamp(tile, out Ramp ramp))
		{
			Debug.Log("On ramp");
			foreach (Node node in ramp.Nodes)
				temp.neighbours[node] = Vector3.Distance(position, node.position);
		}

		if (tempEnd != null)
		{
			if (IsVisible(temp.tile, tempEnd.tile, true, true))
				temp.neighbours[tempEnd] = Vector3.Distance(temp.position, tempEnd.position);
		}

		return temp;
	}

	Node TempEnd(float x, float z)
	{
		Vector3 position = OnGround(x, z);
		Vector2Int tile = WorldToTile(position);
		Area area = GetArea(tile.x, tile.y);
		int areaID = area != null ? area.ID : -1;

		Node temp = new Node(tile, position, areaID);
		if (area != null)
		{
			foreach (Node node in area.Nodes)
				if (IsVisible(node.tile, new Vector2(x, z), true, true))
					node.neighbours[temp] = Vector3.Distance(position, node.position);
		}
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

		if (increment < 0) 
			return null;

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
									Node n00 = areaTop.AddNode(r00, p00);
									Node n01 = areaBottom.AddNode(r01, p01);

									// Add the nodes to both areas!
									areaBottom.AddNode(n00);
									areaTop.AddNode(n01);

									Node.Connect(n00, n01);

									Node n10 = null;
									Node n11 = null;
									
									if (r00 != r10)
									{
										n10 = areaTop.AddNode(r10, p10);
										n11 = areaBottom.AddNode(r11, p11);

										areaBottom.AddNode(n10);
										areaTop.AddNode(n11);

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
									Node n00 = areaLeft.AddNode(r00, p00);
									Node n01 = areaRight.AddNode(r01, p01);
									areaRight.AddNode(n00);
									areaLeft.AddNode(n01);

									Node.Connect(n00, n01);

									Node n10 = null;
									Node n11 = null;

									if (r00 != r10)
									{
										n10 = areaLeft.AddNode(r10, p10);
										n11 = areaRight.AddNode(r11, p11);
										areaRight.AddNode(n10);
										areaLeft.AddNode(n11);

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


		foreach (Ramp ramp in ramps)
		{

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
			chunks = new Chunks();

		if (chunks.TryGetValue(new Vector2Int(x, y), out Chunk get))
			return get;

		return CreateChunk(x, y);
	}

	bool TryGetChunk(int x, int y, out Chunk get)
	{
		get = null;

		if (chunks == null)
			return false;

		return chunks.TryGetValue(new Vector2Int(x, y), out get);
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

	Node tempEnd;

	public bool RemoveNode(Node node)
	{
		if (node == null)
			return false;

		if (node.area < 0 || node.area >= areas.Count)
			return false;

		return areas[node.area].RemoveNode(node);
	}

	public bool TryGetArea(int id, out Area area)
	{
		area = null;
		if (id < 0 || id >= areas.Count)
			return false;

		area = areas[id];
		return true;
	}


	void OnDrawGizmos()
	{
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
							Gizmos.DrawCube(OnGround(tile), Vector3.one);
					}
			}
		}



		if (start != null && goal != null)
		{
			RemoveNode(tempEnd);

			tempEnd = TempEnd(goal.position.x, goal.position.z);
			Node tempStart = TempStart(start.position.x, start.position.z);


			List<Node> path = AStar(tempStart, tempEnd, this, (Node node, float cost) =>
			{
				if (IsVisible(node.position, tempEnd.position, true, true))
					return true;
				return false;
			});

			Gizmos.color = Color.blue;
			
			Gizmos.DrawSphere(tempStart.position, 0.5f);
			Gizmos.DrawSphere(tempEnd.position, 0.5f);

			for (int i = 0; i < path.Count - 1; i++)
			{
				Gizmos.DrawLine(path[i].position, path[i + 1].position);
				Gizmos.DrawSphere(path[i].position, 0.5f);
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
		if (false)
		{
			Node temp = TempStart(start.position.x, start.position.z);
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
					Node last = temp;
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

			//SmoothRampVertices(true);
		}
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
}
