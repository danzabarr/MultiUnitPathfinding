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
}
