using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

public class TextureConvertor : MonoBehaviour
{
    public bool ModifyTextureAssets = false;
	public bool CreateBaseNoise = false;
	public bool CreateDetailNoise = false;
	public bool CreateAlligatorNoise = false;

	public Texture2D[] BaseTextureSlices;
	public Texture2D[] DetailTextureSlices;
	public Texture2D[] AlligatorTextureSlices;

	private void OnValidate()
	{
		if (ModifyTextureAssets)
			PrepareTextures();

		if (CreateBaseNoise)
			Create3DBaseNoise();

		if (CreateDetailNoise)
			Create3DDetailNoise();

		if (CreateAlligatorNoise)
			Create3DAlligatorNoise();
	}

	private void CreateTexture3DAssetFromSlices(Texture2D[] inSlices, GraphicsFormat inFormat, string inFilePath)
	{
		// Set the texture parameters
		int size = inSlices.Length;

		// Create the texture and apply the parameters
		Texture3D texture = new Texture3D(size, size, size, inFormat, TextureCreationFlags.None);
		texture.wrapMode = TextureWrapMode.Repeat;

		// Create a 3-dimensional array to store color data
		Color[] colors = new Color[size * size * size];

		for (int z = 0; z < size; z++)
		{
			int zOffset = z * size * size;
			for (int y = 0; y < size; y++)
			{
				int yOffset = y * size;
				for (int x = 0; x < size; x++)
				{
					colors[x + yOffset + zOffset] = inSlices[z].GetPixel(x, y);
				}
			}
		}

		// Copy the color values to the texture
		texture.SetPixels(colors);

		// Apply the changes to the texture and upload the updated texture to the GPU
		texture.Apply();

		// Save the texture to your Unity Project
		AssetDatabase.CreateAsset(texture, inFilePath);
	}

	private void PrepareTexturesInFolder(string inDirectory)
	{
		var info = new DirectoryInfo(inDirectory);
		FileInfo[] fileInfo = info.GetFiles();
		foreach (FileInfo file in fileInfo)
		{
			string metadataPath = file.FullName + ".meta";
			if (File.Exists(metadataPath))
			{
				List<string> newfile = new List<string>();

				string[] lines = File.ReadAllLines(metadataPath);
				foreach (string line in lines)
				{
					string newline = line;
					if (newline.Contains("sRGBTexture: 1"))
					{
						newline = newline.Replace("sRGBTexture: 1", "sRGBTexture: 0");
					}

					if (newline.Contains("isReadable: 0"))
					{
						newline = newline.Replace("isReadable: 0", "isReadable: 1");
					}
					newfile.Add(newline);
				}

				File.WriteAllLines(metadataPath, newfile.ToArray());
			}
		}
	}

	private void Create3DBaseNoise()
	{
		CreateBaseNoise = false;
		CreateTexture3DAssetFromSlices(BaseTextureSlices, 
			GraphicsFormat.R16G16B16A16_SFloat, "Assets/Scripts/Experiment/BaseTexture.asset");
	}

	void Create3DDetailNoise()
	{
		CreateDetailNoise = false;
		CreateTexture3DAssetFromSlices(DetailTextureSlices, 
			GraphicsFormat.R16G16B16A16_SFloat, "Assets/Scripts/Experiment/DetailTexture.asset");
	}

	void Create3DAlligatorNoise()
	{
		CreateAlligatorNoise = false;
		CreateTexture3DAssetFromSlices(AlligatorTextureSlices, 
			GraphicsFormat.R16G16B16A16_SFloat, "Assets/Scripts/Experiment/AlligatorTexture.asset");
	}

	private void PrepareTextures()
	{
		ModifyTextureAssets = false;

		PrepareTexturesInFolder("Assets/Scripts/Experiment/base");
		PrepareTexturesInFolder("Assets/Scripts/Experiment/detail");
		PrepareTexturesInFolder("Assets/Scripts/Experiment/alligator");

		AssetDatabase.Refresh();
	}
};
