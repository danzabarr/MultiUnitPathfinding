using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

/// <summary>
/// A square grid plane terrain generator.
/// Snaps vertices to increments and generates cliffs.
/// Chunks store arrays of tile data, for example accessibility and altitude increments.
/// </summary>
public class Chunk : AbstractTerrainGenerator
{
	public TerrainGenerationSettings terrainSettings;
	public Vector2Int size;
	public Vector2Int chunkPosition;

	public BatchRenderer rocks;

	public const int FLAT = 0;
	public const int RAMP = -1;
	public const int CLIFF = -2;
	public const int WATER = -3;
	public const int OUT_OF_BOUNDS = -4;

	private int[] permanentObstructions;
	
	//TODO:
	public bool smoothShading = true;

	public Vector3 ChunkOffset => (chunkPosition * size).X0Y();
	public Vector3 OnGround(Vector3 position)
	{
		return new Vector3(position.x, terrainSettings.Sample(position.x, position.z), position.z);
	}

	public int GetPermanentObstructionType(int x, int z)
	{
		if (x < 0 || x >= size.x || z < 0 || z >= size.y)
			return OUT_OF_BOUNDS;

		return permanentObstructions[x + z * size.x];
	}

	public void SetRamp(int x, int z)
	{
		if (x < 0 || x >= size.x || z < 0 || z >= size.y)
			return;
			
		permanentObstructions[x + z * size.x] = RAMP;
	}

	public (Vector3, Vector3) OnMesh(Vector3 position)
	{
		if (collider.Raycast(new Ray(new Vector3(position.x, 100, position.z), Vector3.down), out RaycastHit hit, 200))
			return (hit.point, hit.normal);

		return (position, Vector3.up);
	}

	public IEnumerable<Vector2Int> Tiles
	{
		get
		{
			for (int x = 0; x < size.x; x++)
				for (int y = 0; y < size.y; y++)
					yield return new Vector2Int(x, y) + chunkPosition * size;

			yield break;
		}
	}

	public override void OnVertexUpdate()
	{

		// We just want to fix the quads here so that the shortest diagonal is used for triangulation

		Vector3[] vertices = mesh.vertices;
		int[] triangles = mesh.triangles;

		for (int ti = 0, vi = 0, y = 0; y < size.y; y++, vi++)
		{
			for (int x = 0; x < size.x; x++, ti += 6, vi++)
			{
				/* Don't need to do this here
				float minHeight = Mathf.Min(vertices[vi].y, vertices[vi + 1].y, vertices[vi + size.x + 1].y, vertices[vi + size.x + 2].y);
				float maxHeight = Mathf.Max(vertices[vi].y, vertices[vi + 1].y, vertices[vi + size.x + 1].y, vertices[vi + size.x + 2].y);
				if (maxHeight - minHeight > 0.01f)
				{
					cliffs[x + y * size.x] = true;
					increments[x + y * size.x] = -1;
				}

				if (terrainSettings.snapIncrement > 0)
				{
					int increment = Mathf.RoundToInt(minHeight / terrainSettings.snapIncrement);
					increments[x + y * size.x] = increment;
				}
				*/
				// rotate the triangles so that the diagonal is shortest

				Vector3 v00 = vertices[vi];
				Vector3 v10 = vertices[vi + 1];
				Vector3 v01 = vertices[vi + size.x + 1];
				Vector3 v11 = vertices[vi + size.x + 2];

				float d1 = (v00 - v11).sqrMagnitude;
				float d2 = (v10 - v01).sqrMagnitude;

				if (d1 > d2)
				{
					// |\
					triangles[ti + 0] = vi;
					triangles[ti + 1] = vi + size.x + 1;
					triangles[ti + 2] = vi + 1;

					// \|
					triangles[ti + 3] = vi + 1;
					triangles[ti + 4] = vi + size.x + 1;
					triangles[ti + 5] = vi + size.x + 2;
				}
				else
				{
					// |/
					triangles[ti + 0] = vi;
					triangles[ti + 1] = vi + size.x + 2;
					triangles[ti + 2] = vi + 1;

					// /|
					triangles[ti + 3] = vi;
					triangles[ti + 4] = vi + size.x + 1;
					triangles[ti + 5] = vi + size.x + 2;
				}
			}
		}

		mesh.triangles = triangles;

		mesh.RecalculateNormals();
		mesh.RecalculateBounds();
	}

	[ContextMenu("Regenerate Decorations")]
	public void RegenerateDecorations()
	{
		foreach (Decorations decorations in GetComponents<Decorations>())
			decorations.Regenerate();
	}

	[ContextMenu("Generate Rocks")]
	public void GenerateRocks()
	{
		Random.InitState(chunkPosition.x * 1000 + chunkPosition.y);
		List<Matrix4x4> matrices = new List<Matrix4x4>();
		for (int x = 0; x < size.x; x++)
		{
			for (int y = 0; y < size.y; y++)
			{
				if (permanentObstructions[x + y * size.x] == CLIFF)
				{
					for (int i = 0; i < Random.Range(1, 4); i++)
					{
						Vector2 range = new Vector2(-0.125f, 0.125f);
						Vector2Int tile = new Vector2Int(x, y) + chunkPosition * size;
						(Vector3 position, Vector3 normal) = OnMesh(new Vector3(tile.x + Random.Range(range.x, range.y), 0, tile.y + Random.Range(range.x, range.y)));
						Quaternion rotation = // up is normal
							Quaternion.LookRotation(Vector3.Cross(normal, Vector3.forward), normal) *
							Quaternion.Euler(0, Random.Range(0, 360), 0);
						Vector3 scale = Random.Range(0.5f, 0.7f) * new Vector3(1, Random.Range(0.25f, 0.5f), 1);

						//Apply the scale and rotation locally, then translate to the world position

						Matrix4x4 matrix = Matrix4x4.TRS(position, rotation, scale);
						matrices.Add(matrix);
					}
				}
			}
		}
		rocks.SetMatrices(matrices.ToArray());
	}

	public override void CreateArrays(out Vector3[] vertices, out int[] triangles, out Vector2[] uv, out Vector3[] normals)
	{
		transform.position = ChunkOffset;

		vertices = new Vector3[(size.x + 1) * (size.y + 1)];
		triangles = new int[size.x * size.y * 6];
		uv = new Vector2[vertices.Length];

		for (int y = 0, i = 0; y <= size.y; y++)
		{
			for (int x = 0; x <= size.x; x++)
			{
				vertices[i] = OnGround(new Vector3(x + terrainSettings.offset.x, 0, y + terrainSettings.offset.z) + ChunkOffset) - ChunkOffset;
				uv[i] = new Vector2((float)x / size.x, (float)y / size.y);
				i++;
			}
		}

		//cliffs = new bool[size.x * size.y];
		permanentObstructions = new int[size.x * size.y];
		//increments = new int[size.x * size.y];

		for (int ti = 0, vi = 0, y = 0; y < size.y; y++, vi++)
		{
			for (int x = 0; x < size.x; x++, ti += 6, vi++)
			{

				float minHeight = Mathf.Min(vertices[vi].y, vertices[vi + 1].y, vertices[vi + size.x + 1].y, vertices[vi + size.x + 2].y);
				float maxHeight = Mathf.Max(vertices[vi].y, vertices[vi + 1].y, vertices[vi + size.x + 1].y, vertices[vi + size.x + 2].y);

				if (maxHeight - minHeight > 0.01f)
					permanentObstructions[x + y * size.x] = CLIFF;
				//cliffs[x + y * size.x] = true;
				//increments[x + y * size.x] = -1;
				else if (minHeight < 0)
					permanentObstructions[x + y * size.x] = WATER;

				else
					permanentObstructions[x + y * size.x] = Mathf.RoundToInt(minHeight / terrainSettings.snapIncrement);

				if (terrainSettings.snapIncrement > 0)
				{
					int increment = Mathf.RoundToInt(minHeight / terrainSettings.snapIncrement);
					//increments[x + y * size.x] = increment;
				}

				// rotate the triangles so that the diagonal is shortest

				Vector3 v00 = vertices[vi];
				Vector3 v10 = vertices[vi + 1];
				Vector3 v01 = vertices[vi + size.x + 1];
				Vector3 v11 = vertices[vi + size.x + 2];

				float d1 = (v00 - v11).sqrMagnitude;
				float d2 = (v10 - v01).sqrMagnitude;

				if (d1 > d2)
				{
					// |\
					triangles[ti + 0] = vi;
					triangles[ti + 1] = vi + size.x + 1;
					triangles[ti + 2] = vi + 1;

					// \|
					triangles[ti + 3] = vi + 1;
					triangles[ti + 4] = vi + size.x + 1;
					triangles[ti + 5] = vi + size.x + 2;
				}
				else
				{
					// |/
					triangles[ti + 0] = vi;
					triangles[ti + 1] = vi + size.x + 2;
					triangles[ti + 2] = vi + 1;

					// /|
					triangles[ti + 3] = vi;
					triangles[ti + 4] = vi + size.x + 1;
					triangles[ti + 5] = vi + size.x + 2;
				}
			}
		}


		// Initialize normals array
		normals = new Vector3[vertices.Length];
		List<Vector3>[] tempNormals = new List<Vector3>[vertices.Length];
		for (int i = 0; i < tempNormals.Length; i++)
		{
			tempNormals[i] = new List<Vector3>();
		}

		// Compute normals for each triangle
		for (int i = 0; i < triangles.Length; i += 3)
		{
			Vector3 v1 = vertices[triangles[i]];
			Vector3 v2 = vertices[triangles[i + 1]];
			Vector3 v3 = vertices[triangles[i + 2]];

			Vector3 edge1 = v2 - v1;
			Vector3 edge2 = v3 - v1;
			Vector3 normal = Vector3.Cross(edge1, edge2).normalized;

			tempNormals[triangles[i]].Add(normal);
			tempNormals[triangles[i + 1]].Add(normal);
			tempNormals[triangles[i + 2]].Add(normal);
		}

		if (smoothShading)
		{
			// Average normals for smooth shading
			for (int i = 0; i < normals.Length; i++)
			{
				Vector3 sum = Vector3.zero;
				foreach (Vector3 n in tempNormals[i])
				{
					sum += n;
				}
				normals[i] = (sum / tempNormals[i].Count).normalized;
			}
		}

		/*

		// TODO this needs moving to a separate system for finding areas across chunks
		areaCount = 0;
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

				//Area mouseArea = new Area(areas.Count + 1);
				//mouseArea.AddNode(tile, OnGround(tile.X0Y()));
				//void AddNode(int x, int y) => mouseArea.AddNode(new Vector2Int(x, y), OnGround(new Vector3(x, 0, y)));

				while (queue.Count > 0)
				{
					int current = queue.Dequeue();
					int cx = current % size.x;
					int cy = current / size.x;

					if (cx < size.x - 1 && tiles[cx + 1 + cy * size.x] == UNVISITED)
					{
						queue.Enqueue(cx + 1 + cy * size.x);
						tiles[cx + 1 + cy * size.x] = tiles[current];
						//AddNode(x + 1, y);
					}

					if (cx > 0 && tiles[cx - 1 + cy * size.x] == UNVISITED)
					{
						queue.Enqueue(cx - 1 + cy * size.x);
						tiles[cx - 1 + cy * size.x] = tiles[current];
						//AddNode(x - 1, y);
					}

					if (cy < size.y - 1 && tiles[cx + (cy + 1) * size.x] == UNVISITED)
					{
						queue.Enqueue(cx + (cy + 1) * size.x);
						tiles[cx + (cy + 1) * size.x] = tiles[current];
						//AddNode(x, y + 1);
					}

					if (cy > 0 && tiles[cx + (cy - 1) * size.x] == UNVISITED)
					{
						queue.Enqueue(cx + (cy - 1) * size.x);
						tiles[cx + (cy - 1) * size.x] = tiles[current];
						//AddNode(x, y - 1);
					}
				}
			}
		}


		// Identify corners
		// TODO this needs moving to a separate system for finding areas across chunks
		
		//corners = new List<Vector3>();

		for (int x = 0; x < size.x; x++)
		{
			for (int y = 0; y < size.y; y++)
			{
				int mouseArea = tiles[x + y * size.x];
				
				if (mouseArea == CLIFF)
					continue;


				for (int i = 0; i < 8; i += 2)
				{
					Vector2Int v0 = DIAGONAL_DIRECTIONS[(i + 0) % 8] + new Vector2Int(x, y);
					Vector2Int v1 = DIAGONAL_DIRECTIONS[(i + 1) % 8] + new Vector2Int(x, y);
					Vector2Int v2 = DIAGONAL_DIRECTIONS[(i + 2) % 8] + new Vector2Int(x, y);

					int a0 = GetAreaCode(v0.x, v0.y);
					int a1 = GetAreaCode(v1.x, v1.y);
					int a2 = GetAreaCode(v2.x, v2.y);

					if (a0 != mouseArea)
						continue;

					if (a2 != mouseArea)
						continue;

					if (a1 != CLIFF)
						continue;

					//Vector3 position = Vector3.Lerp(OnGround(new Vector3(x, 0, y) + ChunkOffset), OnGround(new Vector3(v1.x, 0, v1.y) + ChunkOffset), (0.5f - 0.5f * cornerDistance));
					//corners.Add(position);
					//areaCorners[x + y * size.x] = true;
					AddNode(x, y, OnGround(new Vector3(x, 0, y) + ChunkOffset), mouseArea);
				}

				//if (
				//	GetAreaCode(x + 1, y) != mouseArea || 
				//	GetAreaCode(x - 1, y) != mouseArea || 
				//	GetAreaCode(x, y + 1) != mouseArea || 
				//	GetAreaCode(x, y - 1) != mouseArea
				//)
				//{
				//	areaBoundaries[x + y * size.x] = true;
				//}
			}
		}
		*/
	}

	public void OnDrawGizmosSelected()
	{
		for (int x = 0; x < size.x; x++)
		{
			for (int y = 0; y < size.y; y++)
			{
				Vector3 position = OnGround(new Vector3(x, 0, y) + ChunkOffset);

				int increment = permanentObstructions[x + y * size.x];

				if (increment == CLIFF)
				{
					Gizmos.color = Color.red;
					Gizmos.DrawSphere(position, 0.1f);
				}
				else if (increment == WATER)
				{
					Gizmos.color = Color.blue;
					Gizmos.DrawSphere(position, 0.1f);
				}
				else if (increment == RAMP)
				{
					Gizmos.color = Color.green;
					Gizmos.DrawSphere(position, 0.1f);
				}
				else
				{
					Handles.Label(position, increment.ToString());
				}
			}
		}
	}

}
