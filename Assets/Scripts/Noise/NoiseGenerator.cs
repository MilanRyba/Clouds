using Helpers;
using System;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class NoiseGenerator : MonoBehaviour
{
	public enum GeneratorMode { Shape, Detail }
	public enum TextureChannel { R, G, B, A }

	[Header("General")]

	[Tooltip("Dictates which texture will be generated after clicking Generate button")]
	public GeneratorMode Mode = GeneratorMode.Shape;
	public TextureChannel ActiveChannel = TextureChannel.R;

	[Header("Noise")]

	[Range(1, 128)]
	public int ShapeTextureResolution = 128;

	[Range(1, 128)]
	public int DetailTextureResolution = 32;

	public NoiseSettings ShapeSettings;
	public NoiseSettings DetailSettings;

	RenderTexture m_ShapeTexture;
	RenderTexture m_DetailTexture;

	// After generating a 3D texture we store each slice here.
	// This way we can preview individual slices in the inspector and also store the texture as an asset.
	Texture2D[] m_Slices;

	[Range(0, 127)]
	public int Slice = 0;

	[Header("Shaders")]
	[SerializeField] ComputeShader m_CloudShapeShader;
	[SerializeField] ComputeShader m_CloudDetailShader;
	[SerializeField] ComputeShader m_SlicerShader;

	public RenderTexture ShapeTexture { get {  return m_ShapeTexture; } }
	public RenderTexture DetailTexture { get { return m_DetailTexture; } }
	public Texture2D[] Slices { get { return m_Slices; } }

	public RenderTexture ActiveTexture  { get { return Mode == GeneratorMode.Shape ? m_ShapeTexture : m_DetailTexture; } }
	private NoiseSettings ActiveSettings { get { return Mode == GeneratorMode.Shape ? ShapeSettings : DetailSettings; } }
	private ComputeShader ActiveShader   { get { return Mode == GeneratorMode.Shape ? m_CloudShapeShader : m_CloudDetailShader; } }

	private Vector4 ChannelMask
	{
		get
		{
			return new Vector4(
				(ActiveChannel == TextureChannel.R) ? 1 : 0,
				(ActiveChannel == TextureChannel.G) ? 1 : 0,
				(ActiveChannel == TextureChannel.B) ? 1 : 0,
				(ActiveChannel == TextureChannel.A) ? 1 : 0
				);
		}
	}

	public void UpdateNoise()
	{
		Debug.Log("Updating Noise");

		// Re-create the textures if needed
		TextureHelper.CreateTexture3D(ref m_ShapeTexture, ShapeTextureResolution, "CloudShapeTexture");
		TextureHelper.CreateTexture3D(ref m_DetailTexture, DetailTextureResolution, "CloudDetailTexture");

		SetNoiseSettings(ActiveSettings);

		ActiveShader.SetTexture(0, "_Result", ActiveTexture);
		ActiveShader.SetInt("_Resolution", ActiveTexture.width);
		ActiveShader.SetVector("_ChannelMask", ChannelMask);

		GraphicsHelper.Dispatch(ActiveShader, ActiveTexture.width, ActiveTexture.height, ActiveTexture.volumeDepth);

		SliceTexture3D(ActiveTexture);
	}

	private void SetNoiseSettings(NoiseSettings inSettings)
	{
		ActiveShader.SetInt("_Worley_NumCells", inSettings.Worley.NumCells);
		ActiveShader.SetInt("_Perlin_NumOctaves", inSettings.Perlin.NumOctaves);
		ActiveShader.SetFloat("_Perlin_Frequency", inSettings.Perlin.Frequency);
	}

	private void SliceTexture3D(RenderTexture rt)
	{
#if UNITY_EDITOR
		int resolution = rt.width;
		m_Slices = new Texture2D[resolution];

		m_SlicerShader.SetTexture(0, "_Input", rt);

		for (int layer = 0; layer < resolution; layer++)
		{
			var slice = new RenderTexture(resolution, resolution, 0);
			slice.dimension = UnityEngine.Rendering.TextureDimension.Tex2D;
			slice.enableRandomWrite = true;
			slice.Create();

			m_SlicerShader.SetTexture(0, "_Output", slice);
			m_SlicerShader.SetInt("_Layer", layer);

			GraphicsHelper.Dispatch(m_SlicerShader, resolution, resolution);
			m_Slices[layer] = GetRTPixels(slice);
		}
#endif
	}

	public void Save(string textureName)
	{
#if UNITY_EDITOR
		 string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
		 string saveName = sceneName + "_" + textureName;
		 
		 var x = Tex3DFromTex2DArray(m_Slices, m_Slices[0].width);
		 AssetDatabase.CreateAsset(x, "Assets/" + saveName + ".asset");
#endif
	}

	// Taken from https://docs.unity3d.com/6000.2/Documentation/ScriptReference/RenderTexture-active.html
	// TODO: Move into TextureHelper
	private Texture2D GetRTPixels(RenderTexture rt)
	{
		// Remember currently active render texture
		RenderTexture currentActiveRT = RenderTexture.active;

		// Set the supplied RenderTexture as the active one
		RenderTexture.active = rt;

		// Create a new Texture2D and read the RenderTexture image into it
		Texture2D tex = new Texture2D(rt.width, rt.height);
		tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
		tex.Apply();

		// Restore previously active render texture
		RenderTexture.active = currentActiveRT;
		return tex;
	}

	private Texture3D Tex3DFromTex2DArray(Texture2D[] slices, int resolution)
	{
		// Set the texture parameters
		TextureFormat format = TextureFormat.RGBA32;

		// Create the texture and apply the parameters
		Texture3D texture = new Texture3D(resolution, resolution, resolution, format, false);
		texture.wrapMode = TextureWrapMode.Repeat;
		texture.filterMode = FilterMode.Point;

		// Create a 3-dimensional array to store color data
		Color[] colors = texture.GetPixels();

		// Populate the array so that the x, y, and z values of the texture map to red, blue, and green colors
		for (int z = 0; z < resolution; z++)
		{
			int zOffset = z * resolution * resolution;
			Color[] layerPixels = slices[z].GetPixels();
			for (int y = 0; y < resolution; y++)
			{
				int yOffset = y * resolution;
				for (int x = 0; x < resolution; x++)
					colors[x + yOffset + zOffset] = layerPixels[x + yOffset];
			}
		}

		// Copy the color values to the texture
		texture.SetPixels(colors);

		// Apply the changes to the texture and upload the updated texture to the GPU
		texture.Apply();

		return texture;
	}
}
