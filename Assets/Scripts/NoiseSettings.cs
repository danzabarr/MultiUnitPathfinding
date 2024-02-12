using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Perlin Noise", menuName = "Noise/Perlin Noise")]
public class NoiseSettings : ScriptableObject
{
	public HashSet<IOnValidateListener<NoiseSettings>> listeners = new HashSet<IOnValidateListener<NoiseSettings>>();

	public void AddListener(IOnValidateListener<NoiseSettings> listener)
	{
		if (!listeners.Contains(listener))
			listeners.Add(listener);
	}

	public int seed = 0;
	[Range(0.1f, 100f)] public float amplitude = 1f;
	[Range(0.001f, 1.0f)] public float frequency = 0.05f;
	[Range(1, 10)] public int octaves = 1;
	[Range(1f, 3f)] public float lacunarity = 2f;
	[Range(0f, 1f)] public float persistence = 0.5f;
	[Range(0.1f, 100f)] public float scale = 1f;
	public float offset = 0f;
	public AnimationCurve remap = AnimationCurve.Linear(0, 0, 1, 1);

	public static float Sample(int seed, float x, float y, float amplitude, float frequency, int octaves, float lacunarity, float persistence, AnimationCurve remap, float scale, float offset)
	{
		Random.InitState(seed);

		x += (Random.value - 0.5f) * 100000;
		y += (Random.value - 0.5f) * 100000;

		float sum = Mathf.PerlinNoise(x * frequency, y * frequency);
		float range = 1f;
		for (int o = 1; o < octaves; o++)
		{
			frequency *= lacunarity;
			amplitude *= persistence;
			range += amplitude;
			sum += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
		}


		return remap.Evaluate(sum / range) * scale + offset;
	}

	public float Sample(float x, float y)
	{
		return Sample(seed, x, y, amplitude, frequency, octaves, lacunarity, persistence, remap, scale, offset);
	}

	public Vector3 Height(float x, float z)
	{
		return new Vector3(x, Sample(x, z), z);
	}

	public Vector3 ToHeight(Vector3 position)
	{
		return Height(position.x, position.z);
	}

	public void OnValidate()
	{
		foreach (var listener in listeners)
			listener.OnScriptValidated(this);
	}
}