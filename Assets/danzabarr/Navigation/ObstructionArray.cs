using UnityEngine;

public class ObstructionArray : AbstractObstruction
{
	public bool[] array;
	public RectInt rect;

	public override RectInt GetBoundingRectangle()
	{
		return rect;
	}

	public override bool Contains(Vector2Int position)
	{
		position -= new Vector2Int(rect.x, rect.y);
		if (position.x < 0 || position.y < 0 || position.x >= rect.width || position.y >= rect.height)
		{
			return false;
		}
		return array[position.x + position.y * rect.width];
	}

	public void OnValidate()
	{
		if (array == null || array.Length != rect.width * rect.height)
			array = new bool[rect.width * rect.height];
	}

	public void OnDrawGizmosSelected()
	{
		RectInt rect = GetBoundingRectangle();
		for (int x = 0; x < rect.width; x++)
		{
			for (int y = 0; y < rect.height; y++)
			{
				if (array[x + y * rect.width])
				{
					Vector3 center = new Vector3(rect.x + x + 0.5f, 0, rect.y + y + 0.5f);
					Vector3 size = new Vector3(1, 1, 1);

					Gizmos.color = Color.red;
					Gizmos.DrawWireCube(center, size);
				
					Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
					Gizmos.DrawCube(center, size);
				}
			}
		}
	}

	public override float SignedDistance(Vector2Int position)
	{
		throw new System.NotImplementedException();
	}
}
