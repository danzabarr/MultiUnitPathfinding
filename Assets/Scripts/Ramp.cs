using System.Collections.Generic;
using UnityEngine;

public enum Orientation
{
	HORIZONTAL,
	VERTICAL
}

[System.Serializable]
public class Ramp
{
	public Vector2Int start;
	public Orientation orientation;
	public int length;
	public Node n00, n01, n10, n11;

	public IEnumerable<Node> Nodes
	{
		get
		{
			if (n00 != null) yield return n00;
			if (n01 != null) yield return n01;
			if (n10 != null) yield return n10;
			if (n11 != null) yield return n11;

			yield break;
		}
	}

	

	public bool Contains(Vector2Int tile) => Contains(tile.x, tile.y);

	public bool Contains(int x, int z)
	{
		if (orientation == Orientation.HORIZONTAL)
		{
			if (x < start.x || x >= start.x + length)
				return false;
			if (z != start.y)
				return false;
		}
		else
		{
			if (z < start.y || z >= start.y + length)
				return false;
			if (x != start.x)
				return false;
		}

		return true;
	}
}
