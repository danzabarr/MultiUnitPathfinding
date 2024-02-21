using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// The obstruction interface.
/// </summary>
public interface IObstruction
{
	RectInt GetBoundingRectangle();
	bool Contains(Vector2Int position);
}

/// <summary>
/// The abstract obstruction class that extends MonoBehaviour.
/// </summary>
public abstract class AbstractObstruction : MonoBehaviour, IObstruction
{
	public abstract RectInt GetBoundingRectangle();
	public abstract bool Contains(Vector2Int position);
}

/// <summary>
/// Version of previous type for collections.
/// </summary>
/// <typeparam name="Collection"></typeparam>
public abstract class AbstractObstructionCollection<Collection> : AbstractObstruction where Collection : ICollection<Vector2Int>
{
	[SerializeField] protected Collection collection = default;
	[SerializeField] protected RectInt boundingRectangle = new RectInt();

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

	public override RectInt GetBoundingRectangle()
	{
		return boundingRectangle;
	}

	public override bool Contains(Vector2Int position)
	{
		return boundingRectangle.Contains(position) && collection.Contains(position);
	}

	public void OnValidate()
	{
		UpdateBoundingRectangle();
	}

	public virtual void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		foreach (Vector2Int position in collection)
			Gizmos.DrawWireCube(position.X0Z(), Vector3.one);

		//Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
		//foreach (Vector2Int position in collection)
		//	Gizmos.DrawCube(position.X0Z(), Vector3.one);
	}
}

/// <summary>
/// An obstruction component.
/// This component obstructs a single tile position.
/// See other obstruction components
/// ObstructionList
/// ObstructionRect
/// ObstructionArray
/// 
/// </summary>
public class Obstruction : AbstractObstruction
{
	public Vector2Int position;

	public override RectInt GetBoundingRectangle()
	{
		return new RectInt(position.x, position.y, 1, 1);
	}

	public override bool Contains(Vector2Int position)
	{
		return this.position == position;
	}

	public void OnDrawGizmos()
	{
		RectInt rect = GetBoundingRectangle();
		Vector3 center = new Vector3(rect.x + rect.width / 2, 0, rect.y + rect.height / 2);
		Vector3 size = new Vector3(rect.width, 1, rect.height);

		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(center, size);

		//Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
		//Gizmos.DrawCube(center, size);
	}
}