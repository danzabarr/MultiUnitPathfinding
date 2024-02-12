using System.Collections.Generic;
using UnityEngine;

public class ObstacleSet : ObstacleCollection<HashSet<Vector2Int>>
{
	public ObstacleSet()
	{
		collection = new HashSet<Vector2Int>();
	}

	public ObstacleSet(HashSet<Vector2Int> set)
	{
		collection = set;
		UpdateBoundingRectangle();
	}

	public override void AddRange(IEnumerable<Vector2Int> list)
	{
		collection.UnionWith(list);
		UpdateBoundingRectangle();
	}

	public override void RemoveRange(IEnumerable<Vector2Int> list)
	{
		collection.ExceptWith(list);
		UpdateBoundingRectangle();
	}
}
