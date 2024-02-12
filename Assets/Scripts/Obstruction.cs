using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IObstruction
{
	RectInt GetBoundingRectangle();
	bool IsObstructed(Vector2Int position);
}

public class Obstruction : IObstruction 
{
	private Vector2Int position;

	public Obstruction(Vector2Int position)
	{
		this.position = position;
	}

	public RectInt GetBoundingRectangle()
	{
		return new RectInt(position.x, position.y, 1, 1);
	}

	public bool IsObstructed(Vector2Int position)
	{
		return this.position == position;
	}
}

public class ObstructionSet : IObstruction
{
	private HashSet<Vector2Int> set;
	private RectInt boundingRectangle;
	public ObstructionSet(HashSet<Vector2Int> set)
	{
		this.set = set;
		UpdateBoundingRectangle();
	}

	public void Add(Vector2Int position)
	{
		set.Add(position);
		UpdateBoundingRectangle();
	}

	public void Remove(Vector2Int position)
	{
		set.Remove(position);
		UpdateBoundingRectangle();
	}

	public void AddRange(IEnumerable<Vector2Int> list)
	{
		set.UnionWith(list);
		UpdateBoundingRectangle();
	}

	public void RemoveRange(IEnumerable<Vector2Int> list)
	{
		set.ExceptWith(list);
		UpdateBoundingRectangle();
	}

	public void UpdateBoundingRectangle()
	{
		int minX = int.MaxValue;
		int minY = int.MaxValue;
		int maxX = int.MinValue;
		int maxY = int.MinValue;

		foreach (Vector2Int position in set)
		{
			if (position.x < minX)
			{
				minX = position.x;
			}
			if (position.y < minY)
			{
				minY = position.y;
			}
			if (position.x > maxX)
			{
				maxX = position.x;
			}
			if (position.y > maxY)
			{
				maxY = position.y;
			}
		}

		boundingRectangle = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
	}

	public RectInt GetBoundingRectangle()
	{
		return boundingRectangle;
	}

	public bool IsObstructed(Vector2Int position)
	{
		return set.Contains(position);
	}
}

public class ObstructionArray : IObstruction
{
	private bool[,] array;

	public ObstructionArray(bool[,] map)
	{
		this.array = map;
	}

	public RectInt GetBoundingRectangle()
	{
		return new RectInt(0, 0, array.GetLength(0), array.GetLength(1));
	}

	public bool IsObstructed(Vector2Int position)
	{
		return array[position.x, position.y];
	}
}

public class ObstructionRect : IObstruction
{
	private RectInt boundingRectangle;

	public ObstructionRect(RectInt boundingRectangle)
	{
		this.boundingRectangle = boundingRectangle;
	}

	public RectInt GetBoundingRectangle()
	{
		return boundingRectangle;
	}

	public bool IsObstructed(Vector2Int position)
	{
		return boundingRectangle.Contains(position);
	}
}
