using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Terrain Generation Settings", menuName = "Terrain/Generation Settings")]
public class TerrainGenerationSettings : ScriptableObject, IHeightMap
{
	public Vector3 offset = new Vector3(-0.5f, 0, -0.5f);
	public float snapIncrement;
	public float incrementSize;
	public NoiseSettings noise;
	public NoiseSettings riverNoise;
	public float Sample(float x, float z)
	{
		float value = noise.Sample(x, z);

		float riverValue = riverNoise.Sample(x, z);
		value *= riverValue;
		value += (riverValue - 1) * incrementSize;

		if (snapIncrement > 0)
		{
			int increment = Mathf.RoundToInt(value / snapIncrement);
			value = increment * incrementSize;
		}

		return value;
	}
}
