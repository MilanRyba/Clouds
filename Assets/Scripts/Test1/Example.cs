using UnityEditor;
using UnityEngine;

public class Example
{
	[MenuItem("CreateExamples/3DTexture")]
	static void CreateTexture3D()
	{
		// Set the texture parameters
		int size = 128;
		TextureFormat format = TextureFormat.RGBA32;
		TextureWrapMode wrapMode = TextureWrapMode.Repeat;

		// Create the texture and apply the parameters
		Texture3D texture = new Texture3D(size, size, size, format, false);
		texture.wrapMode = wrapMode;

		// Create a 3-dimensional array to store color data
		Color[] colors = new Color[size * size * size];

		// Populate the array so that the x, y, and z values of the texture map to red, blue, and green colors
		float inverseResolution = 1.0f / (size - 1.0f);
		for (int z = 0; z < size; z++)
		{
			int zOffset = z * size * size;
			for (int y = 0; y < size; y++)
			{
				int yOffset = y * size;
				for (int x = 0; x < size; x++)
				{
					if (y < 64)
						colors[x + yOffset + zOffset] = new Color(0, 0, 0, 1.0f);
					else
						colors[x + yOffset + zOffset] = new Color(1, 1, 1, 1.0f);
				}
			}
		}

		// Copy the color values to the texture
		texture.SetPixels(colors);

		// Apply the changes to the texture and upload the updated texture to the GPU
		texture.Apply();

		// Save the texture to your Unity Project
		AssetDatabase.CreateAsset(texture, "Assets/Example3DTexture.asset");
	}
}