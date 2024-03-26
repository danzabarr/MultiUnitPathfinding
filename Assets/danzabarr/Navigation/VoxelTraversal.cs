using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Functions for voxel traversal in 2D space.
/// See Amanatides, J., & Woo, A. (1987). A fast voxel traversal algorithm for ray tracing. Eurographics, 87(3), 3-10.
/// Available here http://www.cse.yorku.ca/~amana/research/grid.pdf
/// Also see https://github.com/cgyurgyik/fast-voxel-traversal-algorithm/blob/master/overview/FastVoxelTraversalOverview.md
/// </summary>
public class Voxel2D 
{
	/// <summary>
	/// Visitor callback. Passes details about the visited node to the caller and breaks on return true.
	/// </summary>
	/// <param name="node"> Visited node, integer precision. </param>
	/// <param name="intersection">Visited intersection. This is the point that the ray intersects (enter/exits) the cell</param>
	/// <param name="normal">The normal of the intersection, this is always an normalised orthogonal direction.</param>
	/// <param name="steps">Number of steps (nodes visited) along the ray.</param>
	/// <param name="distance">Floating point precision distance from the start to the visited intersection.</param>
	/// <returns></returns>
	public delegate bool VisitIntersection(Vector2Int node, Vector2 intersection, Vector2 normal, int steps, float distance);

	/// <summary>
	/// Simpler visitor callback. Passes just the node and the number of steps.
	/// </summary>
	public delegate bool VisitNode(Vector2Int node, int steps);

	/// <summary>
	/// Do voxel traversal along the line between two points in R2.
	/// A line is a ray with a length...
	/// <param name="p0"/> Traversal start point.</param>
	/// <param name="p1"/> Traversal end point.</param>
	/// <param name="vSize"/> The size of a cel in the voxel grid.</param>
	/// <param name="vOffset"/> The offset of the voxel grid from the origin.</param>
	/// <param name="callback"/> The visitor function that will be called at each visited node.</param>
	/// </summary>
	public static bool Line(Vector2 p0, Vector2 p1, Vector2 vSize, Vector2 vOffset, VisitNode callback)
	{
		return Ray(new Ray(p0.X0Y(), (p1 - p0).X0Y()), (p1 - p0).magnitude, vSize, vOffset, callback);
	}

	/// <summary>
	/// Do voxel traversal along the line between two points in R2.
	/// A line is a ray with a length...
	/// <param name="p0"/> Traversal start point.</param>
	/// <param name="p1"/> Traversal end point.</param>
	/// <param name="vSize"/> The size of a cel in the voxel grid.</param>
	/// <param name="vOffset"/> The offset of the voxel grid from the origin.</param>
	/// <param name="callback"/> The visitor function that will be called at each visited node.</param>
	/// </summary>
	public static bool Line(Vector2 p0, Vector2 p1, Vector2 vSize, Vector2 vOffset, VisitIntersection callback)
	{
		return Ray(new Ray(p0.X0Y(), (p1 - p0).X0Y()), (p1 - p0).magnitude, vSize, vOffset, callback);
	}

	//
	public static bool Line(Vector2 p0, Vector2 p1, Vector2 vSize, Vector2 vOffset, IObstruction obstruction)
	{
		return Ray(new Ray(p0.X0Y(), (p1 - p0).X0Y()), (p1 - p0).magnitude, vSize, vOffset, obstruction);
	}

	public static bool Ray(Ray ray, float maxDistance, Vector2 vSize, Vector2 vOffset, VisitNode callback)
	{
		return Ray(ray, maxDistance, vSize, vOffset, (node, _, _, steps, _) => callback(node, steps));
	}


	/// <summary>
	/// Intersection between a ray and abstract obstacle in R2.
	/// Simple visitor callback.
	/// </summary>
	public static bool Ray(Ray ray, float maxDistance, Vector2 vSize, Vector2 vOffset, IObstruction obstruction)
	{
		// if the ray doesn't intersect the bounding rectangle of the obstruction we can skip the raytrace
		bool Intersects(Ray ray, RectInt bounds)
		{
			Vector2 p0 = ray.origin.XZ();
			Vector2 p1 = (ray.origin + ray.direction * maxDistance).XZ();

			//float t0 = 0;
			//float t1 = 1;

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
                        (t1i, t0i) = (t0i, t1i);

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

	/// <summary>
	/// Intersection between a ray and abstract obstacle in R2.
	/// More detailed visitor callback.
	/// </summary>
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

	/// <summary>
	/// Runs traversal and returns a list containing all the visited nodes. 
	/// </summary>
	public static List<Vector2Int> Line(Vector2 p0, Vector2 p1, Vector2 vSize, Vector2 vOffset)
	{
		List<Vector2Int> line = new List<Vector2Int>();
		static Vector2 Vector2Abs(Vector2 a) => new Vector2(Mathf.Abs(a.x), Mathf.Abs(a.y));

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


	/// <summary>
	/// Like spherecast. Cast a circle along a line and the passed visitor function is called with every grid cel that is intersected by the capsule formed.
	/// </summary>
	public static bool Capsule(Vector2 p0, Vector2 p1, float radius, Vector2 vSize, Vector2 vOffset, VisitNode callback)
	{
		return Capsule(p0, p1, radius, vSize, vOffset, (node, _, _, steps, _) => callback(node, steps));
	}

	/// <summary>
	/// Like spherecast. Cast a circle along a line and the passed visitor function is called with every grid cel that is intersected by the capsule formed.
	/// </summary>
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
