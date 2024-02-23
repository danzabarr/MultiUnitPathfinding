using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collision
{
	public Vector2 position;
	public float depth;
	public Vector2 normal;
}

public class CollisionTest : MonoBehaviour
{
	public float radius = 1;
	public List<Circle> circles = new List<Circle>();
	public List<Rectangle> rectangles = new List<Rectangle>();

		public Vector2 velocity;
	public void OnDrawGizmos()
	{
		Dictionary<Collision, IShape> collisions = new Dictionary<Collision, IShape>();

		Vector2 position = transform.position.XZ();
		transform.forward = velocity.X0Y().normalized;

		foreach (Circle circle in circles)
		{
			Collision collision = DynamicCollisionCircle(position, velocity, radius, circle.center, Vector2.zero, circle.radius);
			if (collision != null)
				collisions[collision] = circle;
		}

		foreach (Rectangle rectangle in rectangles)
		{
			Collision collision = DynamicCollisionRectangle(position, velocity, radius, rectangle.rect);
			if (collision != null)
				collisions[collision] = rectangle;
		}

		if (collisions.Count > 0)
			Gizmos.color = Color.red;
		else
			Gizmos.color = Color.green;

		Gizmos.DrawWireSphere(transform.position, radius);
		Gizmos.color = Color.blue;
		Gizmos.DrawWireSphere(transform.position + velocity.X0Y(), radius);

		Gizmos.color = Color.yellow;
		foreach (Circle circle in circles)
			circle.OnDrawGizmos();

		foreach (Rectangle rectangle in rectangles)
			rectangle.OnDrawGizmos();

		foreach (KeyValuePair<Collision, IShape> collision in collisions)
		{
			Collision c = collision.Key;
			IShape shape = collision.Value;

			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(c.position.X0Y(), 0.1f);
			Gizmos.DrawLine(c.position.X0Y(), (c.position + c.normal * c.depth).X0Y());
			Gizmos.color = Color.green;
			shape.OnDrawGizmos();
			Debug.Log("Collision!" + c.position + " " + c.normal + " " + c.depth);	
		}
	}

	public static float SDFCircle(Vector2 position, Vector2 center, float radius)
	{
		return (position - center).magnitude - radius;
	}

	public static float SDFRectangle(Vector2 position, Rect rect)
	{
		float dx = Mathf.Max(Mathf.Max(rect.x - position.x, 0), Mathf.Max(position.x - (rect.x + rect.width), 0));
		float dy = Mathf.Max(Mathf.Max(rect.y - position.y, 0), Mathf.Max(position.y - (rect.y + rect.height), 0));
		return Mathf.Sqrt(dx * dx + dy * dy);
	}

	public static Vector2 GetClosestPointOnRectangle(Vector2 point, Rect rect)
	{
		float width = rect.width;
		float height = rect.height;
		Vector2 rectangleCenter = new Vector2(rect.x + width / 2, rect.y + height / 2);

		// Calculate the half extents of the rectangle
		float halfWidth = width / 2;
		float halfHeight = height / 2;

		// Calculate the max and min x and y bounds of the rectangle
		float minX = rectangleCenter.x - halfWidth;
		float maxX = rectangleCenter.x + halfWidth;
		float minY = rectangleCenter.y - halfHeight;
		float maxY = rectangleCenter.y + halfHeight;

		// Clamp the point's x and y coordinates to be within the rectangle's perimeter
		float closestX = Mathf.Max(minX, Mathf.Min(maxX, point.x));
		float closestY = Mathf.Max(minY, Mathf.Min(maxY, point.y));

		// Determine if the point is closest to the horizontal or vertical edges
		bool closerToVerticalEdge = Mathf.Abs(closestX - point.x) < Mathf.Abs(closestY - point.y);

		if (closerToVerticalEdge)
		{
			// If closer to the vertical edge, set the y coordinate to the point's y, clamped within the rectangle
			closestY = Mathf.Clamp(point.y, minY, maxY);
		}
		else
		{
			// If closer to the horizontal edge, set the x coordinate to the point's x, clamped within the rectangle
			closestX = Mathf.Clamp(point.x, minX, maxX);
		}

		// Return the closest point on the rectangle perimeter
		return new Vector2(closestX, closestY);
	}

	public static Vector2 GetClosestPointOnCircle(Vector2 position, Vector2 center, float radius)
	{
		return center + (position - center).normalized * radius;
	}

	public static Collision StaticCollisionCircle(Vector2 p1, float r1, Vector2 p2, float r2)
	{
		Vector2 direction = p1 - p2;
		float radiiSum = r1 + r2;
		float dSquared = direction.sqrMagnitude;
		float rSquared = radiiSum * radiiSum;

		// Check if circles are already overlapping
		if (dSquared < rSquared)
		{
			float distance = Mathf.Sqrt(dSquared);
			float overlap = radiiSum - distance;

			// Handling overlap: The normal points from p1 to p2, ensuring separation
			Vector2 collisionNormal = distance == 0 ? new Vector2(1, 0) : direction / distance;
			// Return collision with overlap details
			return new Collision
			{
				position = p2 + collisionNormal * r2, // Midpoint based on p1's perspective
				normal = collisionNormal,
				depth = overlap
			};
		}

		return null;
	}

	public static Collision DynamicCollisionCircle(Vector2 p1, Vector2 v1, float r1, Vector2 p2, Vector2 v2, float r2)
	{
		Vector2 relativeVelocity = v2 - v1;
		Vector2 direction = p2 - p1;
		float radiiSum = r1 + r2;
		float dSquared = direction.sqrMagnitude;
		float rSquared = radiiSum * radiiSum;

		// Proceed with dynamic collision detection if not already overlapping
		float a = relativeVelocity.sqrMagnitude;
		float b = 2 * Vector2.Dot(relativeVelocity, direction);
		float c = dSquared - rSquared;
		float discriminant = b * b - 4 * a * c;

		if (discriminant < 0)
		{
			// No real roots, circles do not collide
			return null;
		}

		float sqrtDiscriminant = Mathf.Sqrt(discriminant);
		float t1 = (-b - sqrtDiscriminant) / (2 * a);
		float t2 = (-b + sqrtDiscriminant) / (2 * a);

		float collisionTime = t1 < 0 ? t2 : (t1 < t2 ? t1 : t2);

		if (collisionTime < 0)
		{
			// Collision happened in the past
			return null;
		}

		// Calculate collision details for dynamic collision
		Vector2 collisionPoint = p1 + v1 * collisionTime;
		Vector2 collisionNormal = ((p2 + v2 * collisionTime) - collisionPoint).normalized;
		float collisionDepth = radiiSum - (collisionPoint - (p2 + v2 * collisionTime)).magnitude;

		return new Collision { position = collisionPoint, normal = collisionNormal, depth = collisionDepth };
	}

	public static Collision StaticCollisionRectangle(Vector2 circlePos, float circleRadius, Rect rect)
	{
		Vector2 closestPoint = GetClosestPointOnRectangle(circlePos, rect);
		Vector2 toClosest = circlePos - closestPoint;
		float distanceSquared = toClosest.sqrMagnitude;
		float radiusSquared = circleRadius * circleRadius;

		if (distanceSquared > radiusSquared)
		{
			return null; // No collision
		}
		float distance = Mathf.Sqrt(distanceSquared);

		Vector2 collisionPoint = closestPoint;
		Vector2 collisionNormal = toClosest / distance;
		float collisionDepth = circleRadius - distance;

		return new Collision { position = collisionPoint, normal = collisionNormal, depth = collisionDepth };
	}

	public static Collision DynamicCollisionRectangle(Vector2 circlePos, Vector2 circleVel, float circleRadius, Rect rect)
	{
		Collision s = StaticCollisionRectangle(circlePos + circleVel, circleRadius, rect);
		if (s != null)
			s.position -= circleVel;
		return s;
	}
}

public interface IShape
{
	public Rect GetBoundingBox();
	public bool Contains(Vector2Int position);
	public float SignedDistance(Vector2Int position);
	public float SignedDistanceSquared(Vector2Int position);
	public Vector2 GetClosestPoint(Vector2 position);

	public void OnDrawGizmos();
}

[System.Serializable]
public class Circle : IShape
{
	public Vector2 center;
	public float radius;

	public void OnDrawGizmos()
	{
		Gizmos.DrawWireSphere(center.X0Y(), radius);
	}

	public Rect GetBoundingBox()
	{
		return new Rect(center.x - radius, center.y - radius, radius * 2, radius * 2);
	}

	public bool Contains(Vector2Int position)
	{
		return (position - center).sqrMagnitude <= radius * radius;
	}

	public float SignedDistance(Vector2Int position)
	{
		return (position - center).magnitude - radius;
	}

	public float SignedDistanceSquared(Vector2Int position)
	{
		return (position - center).sqrMagnitude - radius * radius;
	}

	public Vector2 GetClosestPoint(Vector2 position)
	{
		return center + (position - center).normalized * radius;
	}
}


[System.Serializable]
public class Rectangle : IShape
{
	public Rect rect;

	public void OnDrawGizmos()
	{
		Gizmos.DrawWireCube(new Vector3(rect.x + rect.width / 2, 0, rect.y + rect.height / 2), new Vector3(rect.width, 1, rect.height));
	}

	public Rect GetBoundingBox()
	{
		return rect;
	}

	public bool Contains(Vector2Int position)
	{
		return rect.Contains(position);
	}

	public float SignedDistance(Vector2Int position)
	{
		float dx = Mathf.Max(Mathf.Max(rect.x - position.x, 0), Mathf.Max(position.x - (rect.x + rect.width), 0));
		float dy = Mathf.Max(Mathf.Max(rect.y - position.y, 0), Mathf.Max(position.y - (rect.y + rect.height), 0));
		return Mathf.Sqrt(dx * dx + dy * dy);
	}

	public float SignedDistanceSquared(Vector2Int position)
	{
		float dx = Mathf.Max(Mathf.Max(rect.x - position.x, 0), Mathf.Max(position.x - (rect.x + rect.width), 0));
		float dy = Mathf.Max(Mathf.Max(rect.y - position.y, 0), Mathf.Max(position.y - (rect.y + rect.height), 0));
		return dx * dx + dy * dy;
	}

	public Vector2 GetClosestPoint(Vector2 position)
	{
		Vector2 closest = new Vector2(Mathf.Clamp(position.x, rect.x, rect.x + rect.width), Mathf.Clamp(position.y, rect.y, rect.y + rect.height));
		return closest;
	}
}

