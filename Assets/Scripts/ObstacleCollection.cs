using System.Collections.Generic;
using UnityEngine;

public abstract class ObstacleCollection<Collection> : 
	MonoBehaviour, IObstruction where Collection : ICollection<Vector2Int>
{
	[SerializeField] protected Collection collection = default;
	[SerializeField] protected RectInt boundingRectangle = new RectInt();

	public ObstacleCollection()
	{
	}

	public ObstacleCollection(IEnumerable<Vector2Int> collection)
	{
		AddRange(collection);
	}

	public virtual void Add(Vector2Int position)
	{
		collection.Add(position);
		UpdateBoundingRectangle();
	}

	public virtual void Remove(Vector2Int position)
	{
		collection.Remove(position);
		UpdateBoundingRectangle();
	}

	public virtual void AddRange(IEnumerable<Vector2Int> list)
	{
		foreach (Vector2Int position in list)
			collection.Add(position);
		UpdateBoundingRectangle();
	}

	public virtual void RemoveRange(IEnumerable<Vector2Int> list)
	{
		foreach (Vector2Int position in list)
			collection.Remove(position);
		
		UpdateBoundingRectangle();
	}

	public virtual void UpdateBoundingRectangle()
	{
		int minX = int.MaxValue;
		int minY = int.MaxValue;
		int maxX = int.MinValue;
		int maxY = int.MinValue;

		foreach (Vector2Int position in collection)
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

	public virtual RectInt GetBoundingRectangle()
	{
		return boundingRectangle;
	}

	public virtual bool IsObstructed(Vector2Int position)
	{
		return collection.Contains(position);
	}

	public virtual void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		foreach (Vector2Int position in collection)
			Gizmos.DrawWireCube(position.X0Z(), Vector3.one);

		Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
		foreach (Vector2Int position in collection)
			Gizmos.DrawCube(position.X0Z(), Vector3.one);
	}
}
