using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleList : ObstacleCollection<List<Vector2Int>>
{
	public ObstacleList()
	{
		collection = new List<Vector2Int>();
	}

	public ObstacleList(List<Vector2Int> list)
	{
		collection = list;
		UpdateBoundingRectangle();
	}

	public override void AddRange(IEnumerable<Vector2Int> list)
	{
		collection.AddRange(list);
		UpdateBoundingRectangle();
	}
}
