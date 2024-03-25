using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collision2
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
		Dictionary<Collision2, IShape> collisions = new Dictionary<Collision2, IShape>();

		Vector2 position = transform.position.XZ();
		transform.forward = velocity.X0Y().normalized;

		foreach (Circle circle in circles)
		{
			Collision2 collision = StaticCollisionCircle(position, radius, circle.center, circle.radius);
			if (collision != null)
				collisions[collision] = circle;
		}

		foreach (Rectangle rectangle in rectangles)
		{
			Collision2 collision = StaticCollisionRectangle(position, radius, rectangle.rect);
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

		foreach (KeyValuePair<Collision2, IShape> collision in collisions)
		{
			Collision2 c = collision.Key;
			IShape shape = collision.Value;

			Gizmos.color = Color.red;
			Gizmos.DrawWireSphere(c.position.X0Y(), 0.1f);
			Gizmos.DrawLine(c.position.X0Y(), (c.position + c.normal * c.depth).X0Y());
			Gizmos.color = Color.green;
			shape.OnDrawGizmos();
			Debug.Log("Collision2!" + c.position + " " + c.normal + " " + c.depth);	
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
		float l = rect.x,
			t = rect.y,
			w = rect.width,
			h = rect.height;
		float x = point.x;
		float y = point.y;

		float r = l + w;
			float b = t + h;

		x = Mathf.Clamp(x, l, r);
		y = Mathf.Clamp(y, t, b);

			float dl = Mathf.Abs(x - l),
				dr = Mathf.Abs(x - r),
				dt = Mathf.Abs(y - t),
				db = Mathf.Abs(y - b);

			var m = Mathf.Min(dl, dr, dt, db);

			return (m == dt) ? 
			new Vector2(x, t) : (m == db) ? 
			new Vector2(x, b) : (m == dl) ? 
			new Vector2(l, y) : 
			new Vector2(r, y);
	}

	public static Vector2 GetClosestPointOnCircle(Vector2 position, Vector2 center, float radius)
	{
		return center + (position - center).normalized * radius;
	}

	public static Collision2 StaticCollisionCircle(Vector2 p1, float r1, Vector2 p2, float r2)
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
			return new Collision2
			{
				position = p2 + collisionNormal * r2, // Midpoint based on p1's perspective
				normal = collisionNormal,
				depth = overlap
			};
		}

		return null;
	}

	public static Collision2 DynamicCollisionCircle(Vector2 p1, Vector2 v1, float r1, Vector2 p2, Vector2 v2, float r2)
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
			// Collision2 happened in the past
			return null;
		}

		// Calculate collision details for dynamic collision
		Vector2 collisionPoint = p1 + v1 * collisionTime;
		Vector2 collisionNormal = ((p2 + v2 * collisionTime) - collisionPoint).normalized;
		float collisionDepth = radiiSum - (collisionPoint - (p2 + v2 * collisionTime)).magnitude;

		return new Collision2 { position = collisionPoint, normal = collisionNormal, depth = collisionDepth };
	}

	public static Collision2 StaticCollisionRectangle(Vector2 circlePos, float circleRadius, Rect rect)
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

		return new Collision2 { position = collisionPoint, normal = collisionNormal, depth = collisionDepth };
	}

	public static Collision2 DynamicCollisionRectangle(Vector2 circlePos, Vector2 circleVel, float circleRadius, Rect rect)
	{
		Collision2 s = StaticCollisionRectangle(circlePos + circleVel, circleRadius, rect);
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

