using UnityEngine;

public class Obstacle : MonoBehaviour, IObstruction
{
	public Vector2Int position;

	public virtual RectInt GetBoundingRectangle()
	{
		return new RectInt(position.x, position.y, 1, 1);
	}

	public virtual bool IsObstructed(Vector2Int position)
	{
		return this.position == position;
	}

	public void OnDrawGizmosSelected()
	{
		RectInt rect = GetBoundingRectangle();
		Vector3 center = new Vector3(rect.x + rect.width / 2, 0, rect.y + rect.height / 2);
		Vector3 size = new Vector3(rect.width, 1, rect.height);
		
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(center, size);

		Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
		Gizmos.DrawCube(center, size);
	}
}
