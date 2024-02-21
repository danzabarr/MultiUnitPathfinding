using System.Collections.Generic;
using UnityEngine;

public class ObstructionSet : AbstractObstructionCollection<HashSet<Vector2Int>>
{
	public ObstructionSet()
	{
		collection = new HashSet<Vector2Int>();
	}

	public ObstructionSet(HashSet<Vector2Int> set)
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
