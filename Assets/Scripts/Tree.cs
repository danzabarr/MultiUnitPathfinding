using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tree
{
	public Vector2Int position;
    public List<Tree> children = new List<Tree>();

	public Tree(Vector2Int position)
	{
		this.position = position;
	}

	public void DrawGizmos()
	{
		foreach (Tree child in children)
		{
			DrawArrow(new Vector3(position.x, 0, position.y), new Vector3(child.position.x, 0, child.position.y));
			child.DrawGizmos();
		}
	}

	public static void DrawArrow(Vector3 start, Vector3 end, float arrowHeadLength = 0.25f, float arrowHeadAngle = 20.0f)
	{
		Gizmos.DrawLine(start, end);
		Vector3 direction = end - start;
		Vector3 right = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 + arrowHeadAngle, 0) * new Vector3(0, 0, 1);
		Vector3 left = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 180 - arrowHeadAngle, 0) * new Vector3(0, 0, 1);
		Gizmos.DrawRay(end, right * arrowHeadLength);
		Gizmos.DrawRay(end, left * arrowHeadLength);
	}
}
