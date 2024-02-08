using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Node
{
	public Vector2Int tile;
	public Vector3 position;
	public int area;
	public Dictionary<Node, float> neighbours;
	public Node(Vector2Int tile, Vector3 position, int area)
	{
		this.position = position;
		this.tile = tile;
		this.area = area;
		neighbours = new Dictionary<Node, float>();
	}

	public bool IsNeighbour(Node other, out float cost)
	{
		return neighbours.TryGetValue(other, out cost);
	}

	public float GetCost(Node other)
	{
		if (neighbours.TryGetValue(other, out float cost))
			return cost;
		return float.PositiveInfinity;
	}
}

public class Area : IGraph<Node>
{
	public Area(int id)
	{
		this.id = id;
		nodes = new Dictionary<Vector2Int, Node>();
		tiles = new HashSet<Vector2Int>();
	}

	public void AddNode(Vector2Int tile, Vector3 position)
	{
		nodes[tile] = new Node(tile, position, id);
	}

	public int id;
	public HashSet<Vector2Int> tiles;
	public Dictionary<Vector2Int, Node> nodes;                      // this is only the corners!
	//public Dictionary<Node, HashSet<Vector2Int>> neighbours;

	public int TileCount => tiles.Count;
	public int NodeCount => nodes.Count;
	public int ID => id;
	public bool Contains(int x, int y) => tiles.Contains(new Vector2Int(x, y));
	public bool IsNode(int x, int y) => nodes.ContainsKey(new Vector2Int(x, y));
	public IEnumerable<Node> Nodes => nodes.Values;
	public IEnumerable<Vector2Int> Tiles => tiles;

	public bool IsNeighbour(Node node, Node other)
	{
		return node.IsNeighbour(other, out _);
	}

	public IEnumerable<Node> Neighbours(Node node)
	{
		return node.neighbours.Keys;
	}

	public float EdgeCost(Node node, Node other)
	{
		return node.GetCost(other);
	}

	public float HeuristicCost(Node node, Node other)
	{
		return Vector3.Distance(node.position, other.position);
	}
}

public class PolygonTerrainGenerator : TerrainGenerator, IGraph<Node>
{
	[Header("Map Settings")]
	public Vector3 offset;
	public Vector2Int size;

	[Header("Perlin Settings")]
	public int seed = 0;
	[Range(0.1f, 100f)] public float amplitude = 1f;
	[Range(0.01f, 1.0f)] public float frequency = 0.05f;
	[Range(1, 10)] public int octaves = 1;
	[Range(1f, 3f)] public float lacunarity = 2f;
	[Range(0f, 1f)] public float persistence = 0.5f;
	[Range(0.1f, 100f)] public float scale = 1f;

	[Header("Graph Settings")]
	public float snapIncrement = 0f;

	private int[] tiles;
	private bool[] areaBoundaries;
	private bool[] areaCorners;
	[Range(0, 1)] public float cornerDistance;

	public int areaCount;

	public const int OUT_OF_BOUNDS = -2;
	public const int CLIFF = -1;
	public const int UNVISITED = 0;
	private List<Vector3> corners = new List<Vector3>();

	private List<HashSet<int>> areasList = new List<HashSet<int>>();

	public int focusedCorner = 0;

	private Dictionary<int, Area> areas = new Dictionary<int, Area>();

	public int GetAreaCode(int x, int y)
	{
		if (x < 0 || x >= size.x || y < 0 || y >= size.y)
			return OUT_OF_BOUNDS;

		return tiles[x + y * size.x];
	}

	public static float Perlin(int seed, float x, float y, float amplitude, float frequency, int octaves, float lacunarity, float persistence, float scale)
	{
		Random.InitState(seed);

		x += (Random.value - 0.5f) * 100000;
		y += (Random.value - 0.5f) * 100000;

		float sum = Mathf.PerlinNoise(x * frequency, y * frequency);
		float range = 1f;
		for (int o = 1; o < octaves; o++)
		{
			frequency *= lacunarity;
			amplitude *= persistence;
			range += amplitude;
			sum += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
		}
		return sum / range * scale;
	}

	public float SampleHeight(Vector3 position)
	{
		float noise = Perlin(seed, position.x, position.z, amplitude, frequency, octaves, lacunarity, persistence, scale);

		if (snapIncrement > 0)
			noise = Mathf.Round(noise / snapIncrement) * snapIncrement;

		return noise;
	}

	public Vector3 OnGround(Vector3 position)
	{
		return new Vector3(position.x, SampleHeight(position), position.z);
	}

	private static readonly Vector3[] CARDINAL_DIRECTIONS = new Vector3[] { Vector3.forward, Vector3.right, Vector3.back, Vector3.left };

	private static readonly Vector2Int[] DIAGONAL_DIRECTIONS = new Vector2Int[] 
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

	private void AddNode(int x, int y, Vector3 position, int area)
	{
		Vector2Int tile = new Vector2Int(x, y);
		nodes[tile] = new Node(tile, position, area);
	}


	public override void CreateArrays(out Vector3[] vertices, out int[] triangles, out Vector2[] uv)
	{
		vertices = new Vector3[(size.x + 1) * (size.y + 1)];
		triangles = new int[size.x * size.y * 6];
		uv = new Vector2[vertices.Length];

		for (int y = 0, i = 0; y <= size.y; y++)
		{
			for (int x = 0; x <= size.x; x++)
			{
				float height = SampleHeight(new Vector3(x + offset.x, 0, y + offset.z));

				vertices[i] = new Vector3(x, height, y) + offset;
				uv[i] = new Vector2((float)x / size.x, (float)y / size.y);
				i++;
			}
		}

		tiles = new int[size.x * size.y];
		areaBoundaries = new bool[size.x * size.y];
		areaCorners = new bool[size.x * size.y];

		for (int ti = 0, vi = 0, y = 0; y < size.y; y++, vi++)
		{
			for (int x = 0; x < size.x; x++, ti += 6, vi++)
			{

				float minHeight = Mathf.Min(vertices[vi].y, vertices[vi + 1].y, vertices[vi + size.x + 1].y, vertices[vi + size.x + 2].y);
				float maxHeight = Mathf.Max(vertices[vi].y, vertices[vi + 1].y, vertices[vi + size.x + 1].y, vertices[vi + size.x + 2].y);
				if (maxHeight -	minHeight > 0.01f)
					tiles[x + y * size.x] = CLIFF;

				// rotate the triangles so that the diagonal is shortest

				Vector3 v00 = vertices[vi];
				Vector3 v10 = vertices[vi + 1];
				Vector3 v01 = vertices[vi + size.x + 1];
				Vector3 v11 = vertices[vi + size.x + 2];

				float d1 = (v00 - v11).sqrMagnitude;
				float d2 = (v10 - v01).sqrMagnitude;

				if (d1 > d2)
				{
					triangles[ti] = vi;
					triangles[ti + 2] = triangles[ti + 3] = vi + 1;
					triangles[ti + 1] = triangles[ti + 4] = vi + size.x + 1;
					triangles[ti + 5] = vi + size.x + 2;
				}
				else
				{
					triangles[ti + 0] = triangles[ti + 3] = vi;
					triangles[ti + 2] = vi + 1;
					triangles[ti + 4] = vi + size.x + 1;
					triangles[ti + 1] = triangles[ti + 5] = vi + size.x + 2;
				}
			}
		}

		areaCount = 0;
		areasList.Clear();
		areas.Clear();

		for (int x = 0; x < size.x; x++)
		{
			for (int y = 0; y < size.y; y++)
			{
				if (tiles[x + y * size.x] != UNVISITED)
					continue;

				Queue<int> queue = new Queue<int>();
				queue.Enqueue(x + y * size.x);
				tiles[x + y * size.x] = ++areaCount;
				Vector2Int tile = new Vector2Int(x, y);

				Area area = new Area(areas.Count + 1);
				area.AddNode(tile, OnGround(tile.X0Z()));

				void AddNode(int x, int y) => area.AddNode(new Vector2Int(x, y), OnGround(new Vector3(x, 0, y)));

				while (queue.Count > 0)
				{
					int current = queue.Dequeue();
					int cx = current % size.x;
					int cy = current / size.x;

					if (cx < size.x - 1 && tiles[cx + 1 + cy * size.x] == UNVISITED)
					{
						queue.Enqueue(cx + 1 + cy * size.x);
						tiles[cx + 1 + cy * size.x] = tiles[current];
						AddNode(x + 1, y);
					}

					if (cx > 0 && tiles[cx - 1 + cy * size.x] == UNVISITED)
					{
						queue.Enqueue(cx - 1 + cy * size.x);
						tiles[cx - 1 + cy * size.x] = tiles[current];
						AddNode(x - 1, y);
					}

					if (cy < size.y - 1 && tiles[cx + (cy + 1) * size.x] == UNVISITED)
					{
						queue.Enqueue(cx + (cy + 1) * size.x);
						tiles[cx + (cy + 1) * size.x] = tiles[current];
						AddNode(x, y + 1);
					}

					if (cy > 0 && tiles[cx + (cy - 1) * size.x] == UNVISITED)
					{
						queue.Enqueue(cx + (cy - 1) * size.x);
						tiles[cx + (cy - 1) * size.x] = tiles[current];
						AddNode(x, y - 1);
					}
				}
			}
		}

		// Identify corners
		corners = new List<Vector3>();
		for (int x = 0; x < size.x; x++)
		{
			for (int y = 0; y < size.y; y++)
			{
				int area = tiles[x + y * size.x];
				
				if (area == CLIFF)
					continue;


				for (int i = 0; i < 8; i += 2)
				{
					Vector2Int v0 = DIAGONAL_DIRECTIONS[(i + 0) % 8] + new Vector2Int(x, y);
					Vector2Int v1 = DIAGONAL_DIRECTIONS[(i + 1) % 8] + new Vector2Int(x, y);
					Vector2Int v2 = DIAGONAL_DIRECTIONS[(i + 2) % 8] + new Vector2Int(x, y);

					int a0 = GetAreaCode(v0.x, v0.y);
					int a1 = GetAreaCode(v1.x, v1.y);
					int a2 = GetAreaCode(v2.x, v2.y);

					if (a0 != area)
						continue;

					if (a2 != area)
						continue;

					if (a1 != CLIFF)
						continue;

					Vector3 position = Vector3.Lerp(OnGround(new Vector3(x, 0, y)), OnGround(new Vector3(v1.x, 0, v1.y)), (0.5f - 0.5f * cornerDistance));

					corners.Add(position);
					areaCorners[x + y * size.x] = true;
					AddNode(x, y, position, area);
				}

				if (
					GetAreaCode(x + 1, y) != area || 
					GetAreaCode(x - 1, y) != area || 
					GetAreaCode(x, y + 1) != area || 
					GetAreaCode(x, y - 1) != area
				)
				{
					areaBoundaries[x + y * size.x] = true;
				}
			}
		}


		foreach (HashSet<int> areaSet in areasList)
		{
			foreach (int index in areaSet)
			{
				if (!areaCorners[index]) continue;


			}
		}
	}

	public void OnDrawGizmos()
	{
		for (int x = 0; x < size.x; x++)
		{
			for (int y = 0; y < size.y; y++)
			{
				int area = tiles[x + y * size.x];

				if (area <= 0)
					continue;

				Gizmos.color = Color.HSVToRGB((float)area / areaCount, 1f, 1f);
				Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);
				//Gizmos.DrawSphere(OnGround(new Vector3(x, 0, y)), 0.25f);
				Gizmos.DrawCube(OnGround(new Vector3(x, 0, y)), new Vector3(1f, 0.001f, 1f));

				if (areaCorners[x + y * size.x])
				{
					Gizmos.color = Color.black;
					Gizmos.DrawSphere(OnGround(new Vector3(x, 0, y)), 0.125f);
				}
			}
		}

		Gizmos.color = Color.black;
		for (int i = 0; i < corners.Count; i++)
			Gizmos.DrawSphere(corners[i], 0.125f);

		int c = 0;
		int fa = 0;
		Vector2Int fc = Vector2Int.zero;
		for (int i = 0; i < size.x * size.y; i++)
		{
			if (areaCorners[i])
			{
				if (c == focusedCorner)
				{
					fc = new Vector2Int(i % size.x, i / size.x);
					fa = tiles[i];
					break;
				}
				c++;
			}
		}

		if (false)
		{ 
			Gizmos.color = Color.black;
			for (int x = 0; x < size.x; x++)
				for (int y = 0; y < size.y; y++)
				{
					if (tiles[x + y * size.x] != fa)
						continue;

					if (!areaCorners[x + y * size.x])
						continue;

					if (!Voxel2D.Line(fc, new Vector2(x, y), Vector2.one, -0.5f * Vector2.one, (Vector2Int block, Vector2 intersection, Vector2 normal, float distance) =>
					{
						if (GetAreaCode(block.x, block.y) != fa)
							return true;
						return false;
					}))
					{
						Gizmos.DrawLine(OnGround(fc.X0Z()), OnGround(new Vector3(x, 0, y)));
					}
				}
		}

		Gizmos.color = Color.black;
		foreach (KeyValuePair<Vector2Int, Node> pair in nodes)
		{
			Vector2Int tile = pair.Key;
			Node node = pair.Value;

			foreach(Node neighbour in Neighbours(node))
				Gizmos.DrawLine(OnGround(node.position), OnGround(neighbour.position));
		}
	}

	private Dictionary<Vector2Int, Node> nodes = new Dictionary<Vector2Int, Node>();
	
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
}
