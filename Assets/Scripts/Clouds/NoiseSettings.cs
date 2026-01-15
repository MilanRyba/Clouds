using UnityEngine;

[CreateAssetMenu(fileName = "Noise ", menuName = "Clouds/Noise Settings", order = 2)]
public class NoiseSettings : ScriptableObject
{
	// Seed value for Worley noise
	[Range(0, 100)]
	public int Seed = 0;

	[Header("Shape Texture")]

	[Range(1, 10)]
	public int ShapeNoiseFrequency = 4; // Worley noise frequency

	[Range(32, 256)]
	public int ShapeNoiseResolution = 128;

	[Range(0.1f, 5.0f)]
	public float ShapeNoiseScale = 0.3f;


	[Header("Detail Texture")]

	[Range(1, 10)]
	public int DetailNoiseFrequency = 2;

	[Range(1, 64)]
	public int DetailNoiseResolution = 32;

	[Range(0.1f, 5.0f)]
	public float DetailNoiseScale = 0.3f;
}
