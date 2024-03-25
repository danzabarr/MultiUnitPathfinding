using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstructionList : AbstractObstructionCollection<List<Vector2Int>>
{
	public ObstructionList()
	{
		collection = new List<Vector2Int>();
	}

	public ObstructionList(List<Vector2Int> list)
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
