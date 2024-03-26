using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

/// <summary>
/// This data structure is used to fill a queue of vertices to update.
/// The terrain generator will then set all the updated vertices at once,
/// and then call OnVertexUpdate.
/// </summary>
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

[RequireComponent(typeof(MeshFilter), typeof(MeshCollider))]
public abstract class AbstractTerrainGenerator : MonoBehaviour
{
	public bool autoUpdate;

	[Header("Mesh Settings")]
	public UnityEngine.Rendering.IndexFormat indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

	protected Mesh mesh;
	protected MeshFilter filter;
	protected new MeshCollider collider;

	/// <summary>
	/// Implement this.
	/// </summary>
	/// <param name="vertices"></param>
	/// <param name="triangles"></param>
	/// <param name="uv"></param>
	public abstract void CreateArrays(out Vector3[] vertices, out int[] triangles, out Vector2[] uv, out Vector3[] normals);

	public void UpdateVertices(List<VertexUpdate> updates)
	{
		// Update the mesh
		Vector3[] vertices = mesh.vertices;
		
		foreach (var update in updates)
			vertices[update.index] = update.position;
		
		mesh.vertices = vertices;

		OnVertexUpdate();

		filter.mesh = mesh;
		collider.sharedMesh = mesh;
	}

	/// <summary>
	/// Overrider can fix triangles or normals if necessary after a vertex update.
	/// Or do whatever you like.
	/// /// </summary>
	public virtual void OnVertexUpdate()
	{
		mesh.RecalculateBounds();
		mesh.RecalculateNormals();
	}

	/// <summary>
	/// Raycast the terrain.
	/// </summary>
	/// <param name="ray"></param>
	/// <param name="hit"></param>
	/// <returns></returns>
	public bool Raycast(Ray ray, out RaycastHit hit)
	{
		return collider.Raycast(ray, out hit, float.MaxValue);
	}

	/// <summary>
	/// Regenerates the mesh.
	/// </summary>
	[ContextMenu("Regenerate Mesh")]
	public void Generate()
	{
		// Gets the mesh filter and mesh collider components.
		// Creates a new mesh if it doesn't exist.
		// Clears the mesh.
		// Then calls implementor's CreateArrays method.

		if (filter == null)
			filter = GetComponent<MeshFilter>();

		if (collider == null)
			collider = GetComponent<MeshCollider>();

		if (mesh == null)
			mesh = new Mesh();
		else
			mesh.Clear();

		CreateArrays(out Vector3[] vertices, out int[] triangles, out Vector2[] uv, out Vector3[] normals);

		// Assigns the vertices, uv, and triangles to the mesh.
		// If normals were provided, assigns them to the mesh.
		// Otherwise, recalculates the normals.

		// Recalculates the bounds.
		// Assigns the mesh to the mesh filter.
		// Assigns the mesh to the mesh collider.
		mesh.indexFormat = indexFormat;
		mesh.vertices = vertices;
		mesh.uv = uv;
		mesh.triangles = triangles;
		if (normals != null)
			mesh.normals = normals;
		else 
			mesh.RecalculateNormals();
		mesh.RecalculateBounds();
		filter.mesh = mesh;
		collider.sharedMesh = mesh;
	}

	// This is so the editor recreates the mesh in OnValidate.
	// Unity doesn't really encourage this.
	// Found this solution on the internet.
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
