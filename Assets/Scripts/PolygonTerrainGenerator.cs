using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PolygonTerrainGenerator : TerrainGenerator, IGraph<Vector2Int>
{
	[Header("Perlin Settings")]
	public int seed = 0;
	[Range(0.1f, 100f)] public float amplitude = 1f;
	[Range(0.01f, 1.0f)] public float frequency = 0.05f;
	[Range(1, 10)] public int octaves = 1;
	[Range(1f, 3f)] public float lacunarity = 2f;
	[Range(0f, 1f)] public float persistence = 0.5f;
	[Range(0.1f, 100f)] public float scale = 1f;
	public Vector3 offset;

	public float snapIncrement = 0f;

	public Vector2Int size;

	public int[] areas;
	public bool[] areaBoundaries;

	public int areaCount;

	public const int OUT_OF_BOUNDS = -2;
	public const int CLIFF = -1;
	public const int UNVISITED = 0;

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

		areas = new int[size.x * size.y];
		areaBoundaries = new bool[size.x * size.y];

		for (int ti = 0, vi = 0, y = 0; y < size.y; y++, vi++)
		{
			for (int x = 0; x < size.x; x++, ti += 6, vi++)
			{

				float minHeight = Mathf.Min(vertices[vi].y, vertices[vi + 1].y, vertices[vi + size.x + 1].y, vertices[vi + size.x + 2].y);
				float maxHeight = Mathf.Max(vertices[vi].y, vertices[vi + 1].y, vertices[vi + size.x + 1].y, vertices[vi + size.x + 2].y);
				if (minHeight != maxHeight)
					areas[x + y * size.x] = CLIFF;

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
		for (int x = 0; x < size.x; x++)
		{
			for (int y = 0; y < size.y; y++)
			{
				if (areas[x + y * size.x] != UNVISITED)
					continue;

				Queue<Vector2Int> queue = new Queue<Vector2Int>();
				queue.Enqueue(new Vector2Int(x, y));
				areas[x + y * size.x] = ++areaCount;

				while (queue.Count > 0)
				{
					Vector2Int current = queue.Dequeue();
					foreach (Vector2Int neighbour in Neighbours(current))
					{
						if (areas[neighbour.x + neighbour.y * size.x] == UNVISITED)
						{
							queue.Enqueue(neighbour);
							areas[neighbour.x + neighbour.y * size.x] = areas[current.x + current.y * size.x];
						}
					}
				}
			}
		}

		for (int x = 0; x < size.x; x++)
		{
			for (int y = 0; y < size.y; y++)
			{
				if (areas[x + y * size.x] == CLIFF)
					continue;

				List<Vector2Int> neighbours = Neighbours(new Vector2Int(x, y));
				foreach (Vector2Int neighbour in neighbours)
				{
					if (areas[neighbour.x + neighbour.y * size.x] != areas[x + y * size.x])
					{
						areaBoundaries[x + y * size.x] = true;
						break;
					}
				}
			}
		}
	}

	public List<Vector2Int> Neighbours(Vector2Int current)
	{
		List<Vector2Int> neighbours = new List<Vector2Int>();
		if (current.x < size.x - 1)
			neighbours.Add(current + Vector2Int.right);
		if (current.x > 0)
			neighbours.Add(current + Vector2Int.left);
		if (current.y < size.y - 1)
			neighbours.Add(current + Vector2Int.up);
		if (current.y > 0)
			neighbours.Add(current + Vector2Int.down);
		return neighbours;


	}

	public float EdgeCost(Vector2Int current, Vector2Int next)
	{
		throw new System.NotImplementedException();
	}

	public float HeuristicCost(Vector2Int current, Vector2Int next)
	{
		throw new System.NotImplementedException();
	}

	
	public void OnDrawGizmos()
	{
		for (int x = 0; x < size.x; x++)
		{
			for (int y = 0; y < size.y; y++)
			{
				int area = areas[x + y * size.x];

				if (area <= 0)
					continue;

				if (!areaBoundaries[x + y * size.x])
					continue;

				Gizmos.color = Color.HSVToRGB((float)area / areaCount, 1f, 1f);
				Gizmos.DrawSphere(OnGround(new Vector3(x, 0, y)), 0.25f);
			}
		}
	}
}
