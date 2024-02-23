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

	public static Vector3 X0Y(this Vector2Int vector)
	{
		return new Vector3(vector.x, 0, vector.y);
	}

	public static bool Codirectional(this Vector2Int a, Vector2Int b)
	{
		// Check if either vector is the zero vector
		if (a == Vector2Int.zero || b == Vector2Int.zero)
			return false;

		// Direct comparison for equality
		if (a == b)
			return true;

		// Handling cases where one of b's components is zero
		if (b.x == 0 || b.y == 0)
		{
			// If both components of b are zero, a must also be zero for them to be codirectional (already checked)
			// If only one component of b is zero, the corresponding component of a must also be zero, and check the other component for positive integer multiple
			if ((b.x == 0 && a.x != 0) || (b.y == 0 && a.y != 0))
				return false;

			// Check the non-zero component for being a positive integer multiple
			int nonZeroComponentRatio = (b.x == 0) ? a.y / b.y : a.x / b.x;
			return nonZeroComponentRatio > 0 && ((b.x == 0) ? a.y % b.y == 0 : a.x % b.x == 0);
		}

		// Ensuring both components of b are non-zero, check for integer multiple and same direction
		if (a.x % b.x != 0 || a.y % b.y != 0)
			return false;

		int xRatio = a.x / b.x;
		int yRatio = a.y / b.y;

		return xRatio == yRatio && xRatio > 0;
	}

	public static Vector2Int ToTileCoord(this Vector2 position)
	{
		return Vector2Int.RoundToInt(position);
	}

	public static Vector2Int ToTileCoord(this Vector3 position)
	{
		return Vector2Int.RoundToInt(position.XZ());
	}

	public static Vector2Int ToChunkCoord(this Vector2Int tile, int chunkSize)
	{
		int AdjustForNegative(int coordinate) =>
			(coordinate < 0) ? (coordinate - chunkSize + 1) : coordinate;

		return new Vector2Int
		(
			AdjustForNegative(tile.x) / chunkSize,
			AdjustForNegative(tile.y) / chunkSize
		);
	}
}
