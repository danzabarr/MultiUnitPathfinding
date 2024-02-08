using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChocTerrainGenerator : TerrainGenerator
{
	[Range(1, 64)] public int size;
	public int[] heights;
	[Range(0f, 0.5f)] public float bevelSize;
	[Range(1, 16)] public int subdivisions;
	public Function bevelSmoothing = t => t;
	public Function bevelSubdivisionDistribution = t => t;
	public delegate float Function(float t);

	public override void CreateArrays(out Vector3[] vertices, out int[] triangles, out Vector2[] uv)
	{
		size = Mathf.Max(1, size);

		if (heights == null)
			heights = new int[size * size];

		if (heights.Length != size * size)
		{
			int[] ints = new int[size * size];
			for (int i = 0; i < Mathf.Min(ints.Length, heights.Length); i++)
				ints[i] = heights[i];
			heights = new int[size * size];
			for (int i = 0; i < Mathf.Min(ints.Length, heights.Length); i++)
				heights[i] = ints[i];
		}

		float[] floats = new float[size * (3 + subdivisions * 2) + 1];

		{
			int i = 0;
			for (int t = 0; t < size; t++)
			{
				floats[i++] = t;

				for (int j = 0; j < subdivisions; j++)
				{
					floats[i++] = t + bevelSubdivisionDistribution(j / (float)subdivisions) * bevelSize;
				}

				floats[i++] = t + bevelSize;
				floats[i++] = t + 1 - bevelSize;

				for (int j = 0; j < subdivisions; j++)
				{
					floats[i++] = t + 1 - bevelSize + bevelSubdivisionDistribution(j / (float)subdivisions) * bevelSize;
				}
			}
			floats[i] = size;
		}

		vertices = new Vector3[floats.Length * floats.Length];
		uv = new Vector2[vertices.Length];
		triangles = new int[(floats.Length - 1) * (floats.Length - 1) * 6];

		for (int y = 0; y < floats.Length; y++)
		{
			for (int x = 0; x < floats.Length; x++)
			{
				float t = floats[x];
				float s = floats[y];
				float height = 0;// heights[(int)t * size + (int)s];
				vertices[y * floats.Length + x] = new Vector3(t, height, s);
				uv[y * floats.Length + x] = new Vector2(t / size, s / size);
			}
		}

		for (int y = 0; y < floats.Length - 1; y++)
		{
			for (int x = 0; x < floats.Length - 1; x++)
			{
				int i = (y * (floats.Length - 1) + x) * 6;
				triangles[i] = y * floats.Length + x;
				triangles[i + 1] = (y + 1) * floats.Length + x;
				triangles[i + 2] = y * floats.Length + x + 1;
				triangles[i + 3] = (y + 1) * floats.Length + x;
				triangles[i + 4] = (y + 1) * floats.Length + x + 1;
				triangles[i + 5] = y * floats.Length + x + 1;
			}
		}
	}
}
