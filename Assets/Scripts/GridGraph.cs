using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GridGraph", menuName = "Graph/Grid Graph")]
public class GridGraph : ScriptableObject, IGraph<Vector2Int>
{
	public Vector2Int size;
	public float[] costs;

	[ContextMenu("Set Uniform Costs")]
	public void SetUniformCosts()
	{
		costs = new float[size.x * size.y];
		for (int i = 0; i < costs.Length; i++)
			costs[i] = 1;
	}

	public float this[int x, int y]
	{
		get
		{
			if (x < 0 || x >= size.x || y < 0 || y >= size.y)
				return float.PositiveInfinity;
			return costs[x + y * size.x];
		}
		set
		{
			if (x < 0 || x >= size.x || y < 0 || y >= size.y)
				return;
			costs[x + y * size.x] = value;
		}
	}

	public float this[Vector2Int p]
	{
		get => this[p.x, p.y];
		set => this[p.x, p.y] = value;
	}

	public IEnumerable<Vector2Int> Neighbours(Vector2Int current)

	{
		List<Vector2Int> neighbours = new List<Vector2Int>();
		
		if (current.x > 0 && 
			this[current + Vector2Int.left] != float.PositiveInfinity)
			neighbours.Add(current + Vector2Int.left);
		
		if (current.x < size.x - 1 && 
			this[current + Vector2Int.right] != float.PositiveInfinity)
			neighbours.Add(current + Vector2Int.right);

		if (current.y > 0 && 
			this[current + Vector2Int.down] != float.PositiveInfinity)
			neighbours.Add(current + Vector2Int.down);
		
		if (current.y < size.y - 1 && 
			this[current + Vector2Int.up] != float.PositiveInfinity)
			neighbours.Add(current + Vector2Int.up);
		
		return neighbours;
	}

	public float EdgeCost(Vector2Int current, Vector2Int next)
	{
		if (next.x < 0 || next.x >= size.x || next.y < 0 || next.y >= size.y)
			return float.PositiveInfinity;

		return this[next];
	}

	public float HeuristicCost(Vector2Int current, Vector2Int next)
	{
		return Vector2.Distance(current, next);
	}
}
