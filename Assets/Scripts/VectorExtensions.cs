using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class VectorExtensions 
{
	public static Vector2 XZ(this Vector3 vector)
	{
		return new Vector2(vector.x, vector.z);
	}

	public static Vector3 X0Y(this Vector2 vector)
	{
		return new Vector3(vector.x, 0, vector.y);
	}

	public static Vector3 X0Z(this Vector2 vector)
	{
		return new Vector3(vector.x, 0, vector.y);
	}
}
