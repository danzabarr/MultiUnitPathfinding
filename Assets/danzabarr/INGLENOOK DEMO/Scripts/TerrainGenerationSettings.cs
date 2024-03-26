using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Terrain Generation Settings", menuName = "Terrain/Generation Settings")]
public class TerrainGenerationSettings : ScriptableObject, IHeightMap
{
	[Header("Terrain Generation")]
	public Vector3 offset = new Vector3(-0.5f, 0, -0.5f);
	public float snapIncrement;
	public float incrementSize;
	public NoiseSettings noise;
	public NoiseSettings riverNoise;

	// lower the terrain in a circle around the center
	[Header("Edge Falloff")]
	public Vector3 center = new Vector3(32, 0, 32);
	public float radius = 32;
	public float falloff = 1;
	public float scale = 1;
	public float erosion = .005f;

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
		float erosionFactor = Mathf.Clamp01(1 / (1 + Mathf.Exp(-value * erosion)));
		float distance = Vector2.Distance(new Vector2(x, z), new Vector2(center.x, center.z));
		float falloffValue = Mathf.Clamp01((distance - radius) / falloff);

		value -= falloffValue * erosionFactor * incrementSize * scale;

		return value;
	}
}
