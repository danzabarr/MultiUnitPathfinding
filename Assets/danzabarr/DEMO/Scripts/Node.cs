using UnityEngine;

[System.Serializable]
public class Node
{
	public Vector2Int tile;
	public Vector3 position;
	public int area;

	public Node(Vector2Int tile, Vector3 position, int area)
	{
		this.position = position;
		this.tile = tile;
		this.area = area;
	}
}
