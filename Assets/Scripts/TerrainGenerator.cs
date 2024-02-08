using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class TerrainGenerator : MonoBehaviour
{
	public UnityEngine.Rendering.IndexFormat indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
	private Mesh mesh;
	private MeshFilter meshFilter;

	private Vector3[] vertices;
	private int[] triangles;
	private Vector2[] uv;

	public bool autoUpdate;

	[ContextMenu("Regenerate Mesh")]
	public virtual void RegenerateMesh()
	{
		CreateMesh();

		CreateArrays(out vertices, out triangles, out uv);

		BuildMesh(vertices, triangles, uv);
	}

#if UNITY_EDITOR
	void OnValidate()
	{
		if (autoUpdate) ExecuteAfter(seconds: 0.01f, () => RegenerateMesh());
	}

	/// <summary>Executes a function after short period of time</summary>
	/// <param name="seconds">delay time from now</param>
	/// <param name="theDelegate">The function which will be called</param>
	public void ExecuteAfter(float seconds, Action theDelegate)
	{
		if (isActiveAndEnabled)
			StartCoroutine(ExecuteAfterPrivate(seconds, theDelegate));
	}

	private IEnumerator ExecuteAfterPrivate(float seconds, Action theDelegate)
	{
		yield return new WaitForSeconds(seconds);
		theDelegate();
	}
#endif

	public void CreateMesh()
	{
		if (meshFilter == null)
			meshFilter = GetComponent<MeshFilter>();

		if (mesh == null)
			mesh = new Mesh();
		else
			mesh.Clear();
	}

	public virtual void CreateArrays(out Vector3[] vertices, out int[] triangles, out Vector2[] uv)
	{
		vertices = new Vector3[0];
		triangles = new int[0];
		uv = new Vector2[0];
	}

	public void BuildMesh(Vector3[] vertices, int[] triangles, Vector2[] uv)
	{
		mesh.indexFormat = indexFormat;
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		mesh.RecalculateBounds();
		
		mesh.RecalculateNormals();
		meshFilter.mesh = mesh;
	}
}
