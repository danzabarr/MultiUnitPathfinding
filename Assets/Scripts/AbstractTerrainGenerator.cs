using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public abstract class AbstractTerrainGenerator : MonoBehaviour
{
	public bool autoUpdate;

	[Header("Mesh Settings")]
	public UnityEngine.Rendering.IndexFormat indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

	private Mesh mesh;
	private MeshFilter meshFilter;
	private MeshCollider meshCollider;

	/// <summary>
	/// Implement this.
	/// </summary>
	/// <param name="vertices"></param>
	/// <param name="triangles"></param>
	/// <param name="uv"></param>
	public abstract void CreateArrays(out Vector3[] vertices, out int[] triangles, out Vector2[] uv, out Vector3[] normals);

	public bool Raycast(Ray ray, out RaycastHit hit)
	{
		return meshCollider.Raycast(ray, out hit, float.MaxValue);
	}

	[ContextMenu("Regenerate Mesh")]
	public void Generate()
	{
		if (meshFilter == null)
			meshFilter = GetComponent<MeshFilter>();

		if (meshCollider == null)
			meshCollider = GetComponent<MeshCollider>();

		if (mesh == null)
			mesh = new Mesh();
		else
			mesh.Clear();

		CreateArrays(out Vector3[] vertices, out int[] triangles, out Vector2[] uv, out Vector3[] normals);

		mesh.indexFormat = indexFormat;
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		if (normals == null)
			mesh.RecalculateBounds();
		else 
			mesh.normals = normals;
		mesh.RecalculateNormals();
		meshFilter.mesh = mesh;
		meshCollider.sharedMesh = mesh;
	}

#if UNITY_EDITOR
	public void OnValidate()
	{
		if (autoUpdate) ExecuteAfter(seconds: 0.01f, () => Generate());
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
}
