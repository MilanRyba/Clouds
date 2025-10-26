using System;
using UnityEngine;

[Serializable]
public class NoiseSettings
{
	[Serializable]
	public struct WorleySettings
	{
		public int NumCells;
	}

	[Serializable]
	public struct PerlinSettings
	{
		[Range(1, 10)]
		public int NumOctaves;

		public float Frequency;
	}

	public WorleySettings Worley;
	public PerlinSettings Perlin;
}
