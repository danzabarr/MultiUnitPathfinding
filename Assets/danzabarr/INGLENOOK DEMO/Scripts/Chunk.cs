using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
	public Material grassMaterial;

	private Camera overheadCamera;
	private GameObject grass;

	public const int FLAT = 0;
	public const int RAMP = -1;
	public const int BRIDGE = -2;
	public const int CLIFF = -3;
	public const int WATER = -4;
	public const int OUT_OF_BOUNDS = -5;

	[SerializeField] private int[] permanentObstructions;
	
	//TODO:
	public bool smoothShading = true;

	public Vector3 ChunkOffset => (chunkPosition * size).X0Y();
	
	public Vector3 OnGround(Vector3 position)
	{
		return new Vector3(position.x, terrainSettings.Sample(position.x, position.z), position.z);
	}

	[ContextMenu("Create Grass")]
	public void SetupGrass()
	{
		grass = new GameObject("Grass");
		grass.layer = LayerMask.NameToLayer("Grass");
		grass.transform.parent = transform;
		grass.transform.localPosition = Vector3.zero;
		grass.transform.localRotation = Quaternion.identity;
		grass.transform.localScale = Vector3.one;
		grass.AddComponent<MeshFilter>().sharedMesh = mesh;
		MeshRenderer meshRenderer = grass.AddComponent<MeshRenderer>();
		meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		Material material = meshRenderer.sharedMaterial = new Material(grassMaterial);
		material.SetTexture("_GrassMask", overheadCamera.targetTexture);
		material.SetTextureScale("_GrassMask", new Vector2(1.0f / size.x, 1.0f / size.y));
	}

	[ContextMenu("Create Overhead Camera")]
	public void SetupOverheadCamera() 
	{
		int textureScale = 2;
		overheadCamera = new GameObject("Overhead Camera").AddComponent<Camera>();
		overheadCamera.gameObject.AddComponent<CameraThrottle>();
		overheadCamera.transform.parent = transform;
		overheadCamera.transform.localPosition = new Vector3(size.x / 2, 100, size.y / 2);
		overheadCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
		overheadCamera.orthographic = true;
		overheadCamera.orthographicSize = size.x / 2;
		overheadCamera.nearClipPlane = 0.1f;
		overheadCamera.farClipPlane = 200;
		overheadCamera.targetTexture = new RenderTexture(size.x * textureScale, size.y * textureScale, 24);
		overheadCamera.cullingMask = 1 << LayerMask.NameToLayer("Overhead Camera");
		overheadCamera.clearFlags = CameraClearFlags.SolidColor;
		overheadCamera.backgroundColor = Color.white;
		overheadCamera.enabled = false;
		overheadCamera.Render();
		overheadCamera.clearFlags = CameraClearFlags.Nothing;
	}

	[ContextMenu("Regenerate Decorations")]
	public void RegenerateDecorations()
	{		
		Random.InitState(chunkPosition.x * 2000 + chunkPosition.y - 1000);
		foreach (Decorations decorations in GetComponents<Decorations>())
			decorations.Regenerate();
	}

	[ContextMenu("Regenerate Cliff Decorations")]
	public void GenerateRocks()
	{
		foreach (CliffDecorations cd in GetComponents<CliffDecorations>())
			cd.GenerateRocks();
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

	public void SetBridge(int x, int z)
	{
		if (x < 0 || x >= size.x || z < 0 || z >= size.y)
			return;

		permanentObstructions[x + z * size.x] = BRIDGE;
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

		if (permanentObstructions == null || permanentObstructions.Length != size.x * size.y)
			permanentObstructions = new int[size.x * size.y];

		for (int ti = 0, vi = 0, y = 0; y < size.y; y++, vi++)
		{
			for (int x = 0; x < size.x; x++, ti += 6, vi++)
			{
				if (permanentObstructions[x + y * size.x] == BRIDGE)
					continue;
					
				if (permanentObstructions[x + y * size.x] == RAMP)
					continue;

				float minHeight = Mathf.Min(vertices[vi].y, vertices[vi + 1].y, vertices[vi + size.x + 1].y, vertices[vi + size.x + 2].y);
				float maxHeight = Mathf.Max(vertices[vi].y, vertices[vi + 1].y, vertices[vi + size.x + 1].y, vertices[vi + size.x + 2].y);

				if (maxHeight - minHeight > 0.01f)
					permanentObstructions[x + y * size.x] = CLIFF;

				else if (minHeight < 0)
					permanentObstructions[x + y * size.x] = WATER;

				else
					permanentObstructions[x + y * size.x] = Mathf.RoundToInt(minHeight / terrainSettings.snapIncrement);

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
	}

	public bool drawGizmos = false; 

#if UNITY_EDITOR
	public void OnDrawGizmosSelected()
	{
		if (drawGizmos)
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
				else if (increment == BRIDGE)
				{
					Gizmos.color = Color.yellow;
					Gizmos.DrawSphere(position, 0.5f);
				}
				else
				{
					Handles.Label(position, increment.ToString());
				}
			}
		}
	}
#endif

}
