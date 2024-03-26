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
	public Vector2Int position;
	public int length;
	public Orientation orientation;
	public Node n00, n01, n10, n11;

	public List<Node> waypoints = new List<Node>();	// keep track of waypoints on this ramp

	public Ramp() { }

	public Ramp(Vector2Int position, int length, Orientation orientation, Node n00, Node n01, Node n10, Node n11)
	{
		this.position = position;
		this.length = length;
		this.orientation = orientation;
		this.n00 = n00;
		this.n01 = n01;
		this.n10 = n10;
		this.n11 = n11;
	}

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
			if (x < position.x || x >= position.x + length)
				return false;
			if (z != position.y)
				return false;
		}
		else
		{
			if (z < position.y || z >= position.y + length)
				return false;
			if (x != position.x)
				return false;
		}

		return true;
	}
}
