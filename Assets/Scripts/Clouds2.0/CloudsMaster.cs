using Helpers;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class CloudsMaster : MonoBehaviour
{
	[SerializeField] Texture3D m_ShapeTexture;

	[Header("Debug")]

	public bool EnableDebug = true;

	[Range(0.0f, 1.0f)]
	public float TextureSlice = 0.0f;

	public enum TextureChannel { All, R, G, B, A }
	public TextureChannel ActiveChannel = TextureChannel.R;

	private Vector4 ChannelMask
	{
		get
		{
			return new Vector4(
				(ActiveChannel == TextureChannel.R) ? 1 : 0,
				(ActiveChannel == TextureChannel.G) ? 1 : 0,
				(ActiveChannel == TextureChannel.B) ? 1 : 0,
				(ActiveChannel == TextureChannel.A) ? 1 : 0);
		}
	}

	[Header("Noise")]

	[Range(0, 1)] public int PerlinMethod = 0;
	[Range(0, 1)] public int WorleyMethod = 0;
	[Range(0, 100)]     public int Seed = 0;
	[Range(1, 10)]      public int ShapeNoiseFrequency = 3;
	[Range(32, 256)]    public int ShapeNoiseResolution = 128;
	// [Range(0.1f, 5.0f)] public float ShapeNoiseScale = 0.3f;

	[Header("Shaders")]

	[SerializeField] ComputeShader m_CloudShapeNoise;
	[SerializeField] ComputeShader m_CloudDebug;

	private RenderTexture m_ShapeNoise;

	private RenderTexture m_DebugTexture;

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (EnableDebug)
		{
			// Debug Pass
			TextureHelper.CreateTexture2D(ref m_DebugTexture, source.width, source.height, "DebugTexture");

			m_CloudDebug.SetFloat("TextureSlice", TextureSlice);
			m_CloudDebug.SetVector("ViewportDimensions", new Vector2(source.width, source.height));
			m_CloudDebug.SetVector("ViewportDimensionsInv", new Vector2(1.0f / source.width, 1.0f / source.height));
			m_CloudDebug.SetVector("ChannelMask", ChannelMask);

			m_CloudDebug.SetTexture(0, "ShapeTexture", m_ShapeNoise);
			m_CloudDebug.SetTexture(0, "SourceTexture", source);
			m_CloudDebug.SetTexture(0, "Output", m_DebugTexture);


			GraphicsHelper.Dispatch(m_CloudDebug, source.width, source.height);

			Graphics.Blit(m_DebugTexture, destination);
		}
		else
			Graphics.Blit(source, destination);
	}

	private void OnValidate()
	{
		// Shape Noise Pass
		TextureHelper.CreateTexture3D(ref m_ShapeNoise, ShapeNoiseResolution, "Shape Noise");

		m_CloudShapeNoise.SetInt("WorleyMethod", WorleyMethod);
		m_CloudShapeNoise.SetInt("PerlinMethod", PerlinMethod);
		m_CloudShapeNoise.SetInt("Seed", Seed);
		m_CloudShapeNoise.SetInt("Frequency", ShapeNoiseFrequency);
		m_CloudShapeNoise.SetFloat("ResolutionInv", 1.0f / ShapeNoiseResolution);
		m_CloudShapeNoise.SetTexture(0, "NoiseOutput", m_ShapeNoise);

		GraphicsHelper.Dispatch(m_CloudShapeNoise, ShapeNoiseResolution, ShapeNoiseResolution, ShapeNoiseResolution, 0);
	}
}
