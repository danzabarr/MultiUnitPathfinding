using System.Collections.Generic;
using UnityEngine;

public class Bridge
{
    public Vector2Int start;
    public int length;
    public Orientation orientation;
    public Node n0, n1;
    public List<GameObject> pieces = new List<GameObject>();

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
}
