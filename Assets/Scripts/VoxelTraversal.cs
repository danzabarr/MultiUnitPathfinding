using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxel2D 
{
	public delegate bool VisitIntersection(Vector2Int node, Vector2 intersection, Vector2 normal, int steps, float distance);

	public delegate bool VisitNode(Vector2Int node, int steps);

	public static bool Line(Vector2 p0, Vector2 p1, Vector2 vSize, Vector2 vOffset, VisitNode callback)
	{
		return Ray(new Ray(p0.X0Y(), (p1 - p0).X0Y()), (p1 - p0).magnitude, vSize, vOffset, callback);
	}

	public static bool Line(Vector2 p0, Vector2 p1, Vector2 vSize, Vector2 vOffset, VisitIntersection callback)
	{
		return Ray(new Ray(p0.X0Y(), (p1 - p0).X0Y()), (p1 - p0).magnitude, vSize, vOffset, callback);
	}

	public static bool Line(Vector2 p0, Vector2 p1, Vector2 vSize, Vector2 vOffset, IObstruction obstruction)
	{
		return Ray(new Ray(p0.X0Y(), (p1 - p0).X0Y()), (p1 - p0).magnitude, vSize, vOffset, obstruction);
	}

	public static bool Ray(Ray ray, float maxDistance, Vector2 vSize, Vector2 vOffset, VisitNode callback)
	{
		return Ray(ray, maxDistance, vSize, vOffset, (node, _, _, steps, _) => callback(node, steps));
	}

	public static bool Ray(Ray ray, float maxDistance, Vector2 vSize, Vector2 vOffset, IObstruction obstruction)
	{
		// if the ray doesn't intersect the bounding rectangle of the obstruction we can skip the raytrace
		bool Intersects(Ray ray, RectInt bounds)
		{
			Vector2 p0 = ray.origin.XZ();
			Vector2 p1 = (ray.origin + ray.direction * maxDistance).XZ();

			float t0 = 0;
			float t1 = 1;

			float tNear = float.NegativeInfinity;
			float tFar = float.PositiveInfinity;

			// for each dimension (2)
			for (int i = 0; i < 2; i++)
			{
				float p0i = p0[i];
				float p1i = p1[i];
				float bmini = bounds.min[i] * vSize[i] + vOffset[i];
				float bmaxi = bounds.max[i] * vSize[i] + vOffset[i];

				if (Mathf.Abs(p1i - p0i) < float.Epsilon)
				{
					if (p0i < bmini || p0i > bmaxi)
						return false;
				}
				else
				{
					float t0i = (bmini - p0i) / (p1i - p0i);
					float t1i = (bmaxi - p0i) / (p1i - p0i);

					if (t0i > t1i)
					{
						float temp = t0i;
						t0i = t1i;
						t1i = temp;
					}

					tNear = Mathf.Max(tNear, t0i);
					tFar = Mathf.Min(tFar, t1i);

					if (tNear > tFar || tFar < 0)
						return false;
				}
			}

			return true;
		}

		if (!Intersects(ray, obstruction.GetBoundingRectangle()))
			return false;

		return Ray(ray, maxDistance, vSize, vOffset, (node, _, _, _, _) => obstruction.Contains(node));
	}

	public static bool Ray(Ray ray, float maxDistance, Vector2 vSize, Vector2 vOffset, VisitIntersection callback)
	{
		Vector2 p0 = ray.origin.XZ();
		Vector2 p1 = (ray.origin + ray.direction * maxDistance).XZ();

		static Vector2 Vector2Abs(Vector2 a) => new Vector2(Mathf.Abs(a.x), Mathf.Abs(a.y));

		p0.x /= vSize.x;
		p0.y /= vSize.y;

		p1.x /= vSize.x;
		p1.y /= vSize.y;

		p0 -= vOffset;
		p1 -= vOffset;

		Vector2 rd = p1 - p0;
		//float length = rd.magnitude;
		Vector2 p = new Vector2(Mathf.Floor(p0.x), Mathf.Floor(p0.y));
		Vector2 rdinv = new Vector2(1f / rd.x, 1f / rd.y);
		Vector2 stp = new Vector2(Mathf.Sign(rd.x), Mathf.Sign(rd.y));
		Vector2 delta = Vector2.Min(Vector2.Scale(rdinv, stp), Vector2.one);
		Vector2 t_max = Vector2Abs(Vector2.Scale((p + Vector2.Max(stp, Vector2.zero) - p0), rdinv));

		Vector2Int square;
		Vector2 intersection;
		Vector2 normalX = Vector2.right * Mathf.Sign(delta.x);
		Vector2 normalY = Vector2.up * Mathf.Sign(delta.y);
		Vector2 normal = t_max.x < t_max.y ? normalX : normalY;
		float next_t = Mathf.Min(t_max.x, t_max.y);

		int i = 0;
		while (i < 1000)
		{
			i++;

			square = Vector2Int.RoundToInt(p);
			intersection = p0 + rd * next_t + vOffset * vSize;

			if (callback(square, intersection, normal, i, next_t * maxDistance))
				return true;

			if (t_max.x < t_max.y)
			{
				next_t = t_max.x;
				t_max.x += delta.x;
				p.x += stp.x;
				normal = normalX;
			}
			else
			{
				next_t = t_max.y;
				t_max.y += delta.y;
				p.y += stp.y;
				normal = normalY;
			}
			if (next_t > 1f)
				return false;
		}

		return false;
	}

	public static List<Vector2Int> Line(Vector2 p0, Vector2 p1, Vector2 vSize, Vector2 vOffset)
	{
		List<Vector2Int> line = new List<Vector2Int>();
		static Vector2 Vector2Abs(Vector2 a) => new Vector2(Mathf.Abs(a.x), Mathf.Abs(a.y));

		// vOffset -= new Vector3(.5f, .5f, .5f);

		p0.x /= vSize.x;
		p0.y /= vSize.y;

		p1.x /= vSize.x;
		p1.y /= vSize.y;

		p0 -= vOffset;
		p1 -= vOffset;

		Vector2 rd = p1 - p0;
		Vector2 p = new Vector2(Mathf.Floor(p0.x), Mathf.Floor(p0.y));
		Vector2 rdinv = new Vector2(1f / rd.x, 1f / rd.y);
		Vector2 stp = new Vector2(Mathf.Sign(rd.x), Mathf.Sign(rd.y));
		Vector2 delta = Vector2.Min(Vector2.Scale(rdinv, stp), Vector2.one);
		Vector2 t_max = Vector2Abs(Vector2.Scale((p + Vector2.Max(stp, Vector2.zero) - p0), rdinv));
		int i = 0;
		while (i < 1000)
		{
			i++;
			Vector2Int square = Vector2Int.RoundToInt(p);
			line.Add(square);

			float next_t = Mathf.Min(t_max.x, t_max.y);
			if (next_t > 1.0) break;
			//Vector2 intersection = p0 + next_t * rd;  

			if (t_max.x < t_max.y)
			{
				t_max.x += delta.x;
				p.x += stp.x;
			}
			else
			{
				t_max.y += delta.y;
				p.y += stp.y;
			}
		}

		return line;
	}


	public static bool Capsule(Vector2 p0, Vector2 p1, float radius, Vector2 vSize, Vector2 vOffset, VisitNode callback)
	{
		return Capsule(p0, p1, radius, vSize, vOffset, (node, _, _, steps, _) => callback(node, steps));
	}

	public static bool Capsule(Vector2 p0, Vector2 p1, float radius, Vector2 vSize, Vector2 vOffset, VisitIntersection callback)
	{
		float maxDistance = (p1 - p0).magnitude;

		int steps = Mathf.CeilToInt(maxDistance / vSize.magnitude);
		List<Vector2Int> visited = new List<Vector2Int>();

		int i = 0;
		for (float t = 0; t < 1; t += 1f / steps)
		{

			Vector2 p = Vector2.Lerp(p0, p1, t);
			{
				for (float x = -radius; x <= radius; x += vSize.x)
				{
					for (float y = -radius; y <= radius; y += vSize.y)
					{
						Vector2 offset = new Vector2(x, y);
						Vector2 p2 = p + offset;
						if ((p2 - p).magnitude <= radius)
						{
							Vector2Int block = Vector2Int.RoundToInt(p2 / vSize - vOffset);
							if (visited.Contains(block))
								continue;
							visited.Add(block);
							Vector2 intersection = p2;
							Vector2 normal = (p2 - p).normalized;
							float distance = (p2 - p).magnitude;
							if (callback(block, intersection, normal, i++, distance))
								return true;
						}
					}
				}
			}
		}
		return false;
	}

}
