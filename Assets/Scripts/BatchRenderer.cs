using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class BatchRenderer : MonoBehaviour
{
	public Material material;
	public Mesh mesh;
	private Matrix4x4[] matrices;

	public Vector3 position;
	public Vector3 scale;
	public Vector3 rotation;

	public void SetMatrices(Matrix4x4[] matrices)
	{
		this.matrices = matrices;
	}

	public void SetMaterial(Material material)
	{
		this.material = material;
	}

	public void SetMesh(Mesh mesh)
	{
		this.mesh = mesh;
	}

	[ContextMenu("Add Matrix")]
	public void Add()
	{
		Matrix4x4 matrix = Matrix4x4.TRS(position, Quaternion.Euler(rotation), scale);
		List<Matrix4x4> newMatrices = new List<Matrix4x4>(matrices);
		newMatrices.Add(matrix);
		matrices = newMatrices.ToArray();
	}

	[ContextMenu("Remove Matrix")]
	public void Pop()
	{
		if (matrices.Length > 0)
		{
			List<Matrix4x4> newMatrices = new List<Matrix4x4>(matrices);
			newMatrices.RemoveAt(matrices.Length - 1);
			matrices = newMatrices.ToArray();
		}
	}

	private void Update()
	{
		if (matrices == null)
			return;

		if (matrices.Length == 0)
			return;

		if (material == null)
			return;

		if (mesh == null)
			return;

		Graphics.DrawMeshInstanced(mesh, 0, material, matrices);
	}

	private void OnDrawGizmosSelected()
	{
		//Gizmos.color = Color.red;
		//for (int i = 0; i < matrices.Length; i++)
		//	Handles.PositionHandle(matrices[i].GetColumn(3), matrices[i].rotation);
	}
}
