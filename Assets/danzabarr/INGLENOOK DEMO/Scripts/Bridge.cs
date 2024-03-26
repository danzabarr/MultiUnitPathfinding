using System.Collections.Generic;
using UnityEngine;

public class Bridge
{
    public Vector2Int start;
    public int length;
    public Orientation orientation;
    public Node n0, n1;
    public List<GameObject> pieces = new List<GameObject>();
    public List<Node> waypoints = new List<Node>();	

    public Bridge() { }

    public void Delete()
    {
        #if UNITY_EDITOR
        foreach (var piece in pieces)
            Object.DestroyImmediate(piece);
        #else
        foreach (var piece in pieces)
            Object.Destroy(piece);
        #endif
    }

    public bool Contains(Vector2Int tile)
    {
        if (orientation == Orientation.HORIZONTAL)
            return tile.x >= start.x && tile.x < start.x + length && tile.y == start.y;
        else
            return tile.y >= start.y && tile.y < start.y + length && tile.x == start.x;
    }
}
