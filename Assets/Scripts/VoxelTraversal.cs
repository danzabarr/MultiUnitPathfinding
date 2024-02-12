using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxel2D 
{
	public delegate bool VisitIntersection(Vector2Int node, Vector2 intersection, Vector2 normal, int steps, float distance);

	public delegate bool VisitNode(Vector2Int node, int steps);

	public static bool Line(Vector2 p0, Vector2 p1, Vector2 voxelSize, Vector2 voxelOffset, VisitNode callback)
	{
		return Ray(new Ray(p0, p1 - p0), (p1 - p0).magnitude, voxelSize, voxelOffset, callback);
	}

	public static bool Line(Vector2 p0, Vector2 p1, Vector2 voxelSize, Vector2 voxelOffset, VisitIntersection callback)
	{
		return Ray(new Ray(p0, p1 - p0), (p1 - p0).magnitude, voxelSize, voxelOffset, callback);
	}

	public static bool Ray(Ray ray, float maxDistance, Vector2 voxelSize, Vector2 voxelOffset, VisitNode callback)
	{
		return Ray(ray, maxDistance, voxelSize, voxelOffset, (node, _, _, steps, _) => callback(node, steps));
	}


	public static bool Ray(Ray ray, float maxDistance, Vector2 voxelSize, Vector2 voxelOffset, VisitIntersection callback)
	{
		Vector2 p0 = ray.origin;
		Vector2 p1 = ray.origin + ray.direction * maxDistance;

		static Vector2 Vector2Abs(Vector2 a) => new Vector2(Mathf.Abs(a.x), Mathf.Abs(a.y));

		p0.x /= voxelSize.x;
		p0.y /= voxelSize.y;

		p1.x /= voxelSize.x;
		p1.y /= voxelSize.y;

		p0 -= voxelOffset;
		p1 -= voxelOffset;

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
			intersection = p0 + rd * next_t + voxelOffset * voxelSize;

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

	public static List<Vector2Int> Line(Vector2 p0, Vector2 p1, Vector2 voxelSize, Vector2 voxelOffset)
	{
		List<Vector2Int> line = new List<Vector2Int>();
		static Vector2 Vector2Abs(Vector2 a) => new Vector2(Mathf.Abs(a.x), Mathf.Abs(a.y));

		// voxelOffset -= new Vector3(.5f, .5f, .5f);

		p0.x /= voxelSize.x;
		p0.y /= voxelSize.y;

		p1.x /= voxelSize.x;
		p1.y /= voxelSize.y;

		p0 -= voxelOffset;
		p1 -= voxelOffset;

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


	public static bool Capsule(Vector2 p0, Vector2 p1, float radius, Vector2 voxelSize, Vector2 voxelOffset, VisitNode callback)
	{
		return Capsule(p0, p1, radius, voxelSize, voxelOffset, (node, _, _, steps, _) => callback(node, steps));
	}

	public static bool Capsule(Vector2 p0, Vector2 p1, float radius, Vector2 voxelSize, Vector2 voxelOffset, VisitIntersection callback)
	{
		float maxDistance = (p1 - p0).magnitude;

		int steps = Mathf.CeilToInt(maxDistance / voxelSize.magnitude);
		List<Vector2Int> visited = new List<Vector2Int>();

		int i = 0;
		for (float t = 0; t < 1; t += 1f / steps)
		{

			Vector2 p = Vector2.Lerp(p0, p1, t);
			{
				for (float x = -radius; x <= radius; x += voxelSize.x)
				{
					for (float y = -radius; y <= radius; y += voxelSize.y)
					{
						Vector2 offset = new Vector2(x, y);
						Vector2 p2 = p + offset;
						if ((p2 - p).magnitude <= radius)
						{
							Vector2Int block = Vector2Int.RoundToInt(p2 / voxelSize - voxelOffset);
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
