using Helpers;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class CloudsMaster : MonoBehaviour
{
	[Header("Noise")]

	// Temporary members for testing different methods of generation noise
	[Range(0, 1)] public int PerlinMethod = 0;
	[Range(0, 1)] public int WorleyMethod = 0;

	[Range(0, 100)]     public int Seed = 0; // Seed value for Worley noise

	[Range(1, 10)]      public int ShapeNoiseFrequency = 3;
	[Range(32, 256)]    public int ShapeNoiseResolution = 128;
	[Range(0.1f, 5.0f)] public float ShapeNoiseScale = 0.3f;

	[Range(1, 10)]      public int DetailNoiseFrequency = 2;
	[Range(1, 64)]      public int DetailNoiseResolution = 32;
	[Range(0.1f, 5.0f)] public float DetailNoiseScale = 0.3f;

	[Header("Clouds")]
	// public Transform Container;

	[Range(0.01f, 10.0f)]   public float RayStepSize = 0.15f;
	[Range(0.0f, 1.0f)]     public float GlobalDensity = 0.1f;
	[Range(0.01f, 0.0005f)] public float GlobalScale = 0.001f;
	[Range(0.0f, 1.0f)]     public float Coverage = 0.9f;
	[Range(0.0f, 1.0f)]		public float CloudType = 0.5f;

	[Range(1000.0f, 1000000.0f)] public float PlanetRadius = 60000.0f; // Earth's radius in meters
	public Vector2 AtmosphereHeightRange = new Vector2(200.0f, 900.0f);

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

	[Header("Shaders")]

	[SerializeField] ComputeShader m_Clouds;
	[SerializeField] ComputeShader m_CloudShapeNoise;

	private RenderTexture m_IntermediateTexture;
	private RenderTexture m_ShapeNoise;
	private RenderTexture m_DetailNoise;
	private RenderTexture m_HeightDensityGradient;

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		TextureHelper.CreateTexture2D(ref m_IntermediateTexture, source.width, source.height, source.graphicsFormat, "Clouds Intermediate Texture");

		m_Clouds.SetVector("ViewportDimensions", new Vector2(source.width, source.height));
		m_Clouds.SetVector("ViewportDimensionsInv", new Vector2(1.0f / source.width, 1.0f / source.height));
		m_Clouds.SetVector("CameraPosition", transform.position);

		Camera camera = GetComponent<Camera>();
		m_Clouds.SetMatrix("CameraToWorld", camera.cameraToWorldMatrix);
		m_Clouds.SetMatrix("CameraInverseProjection", camera.projectionMatrix.inverse);

		m_Clouds.SetFloat("NearZ", camera.nearClipPlane);
		m_Clouds.SetFloat("FarZ", camera.farClipPlane);

		m_Clouds.SetFloat("PlanetRadius", PlanetRadius);
		m_Clouds.SetVector("AtmosphereHeightRange", AtmosphereHeightRange);

		Light sun = FindObjectOfType<Light>();
		m_Clouds.SetVector("SunDirection", sun.transform.forward);
		m_Clouds.SetVector("SunColor", sun.color);

		m_Clouds.SetFloat("RayStepSize", RayStepSize);
		m_Clouds.SetFloat("GlobalDensity", GlobalDensity);
		m_Clouds.SetFloat("GlobalScale", GlobalScale);
		m_Clouds.SetFloat("ShapeNoiseScale", ShapeNoiseScale);
		m_Clouds.SetFloat("Coverage", Coverage);
		m_Clouds.SetFloat("CloudType", CloudType);
		
		m_Clouds.SetTexture(0, "ShapeTexture", m_ShapeNoise);
		m_Clouds.SetTexture(0, "DetailTexture", m_DetailNoise);
		m_Clouds.SetTexture(0, "HeightGradient", m_HeightDensityGradient);
		m_Clouds.SetTexture(0, "SceneTexture", source);
		m_Clouds.SetTexture(0, "DepthTexture", Shader.GetGlobalTexture("_CameraDepthTexture"));
		m_Clouds.SetTexture(0, "Output", m_IntermediateTexture);

		// Debug parameters
		m_Clouds.SetBool("Debug", EnableDebug);
		m_Clouds.SetFloat("TextureSlice", TextureSlice);
		m_Clouds.SetVector("ChannelMask", ChannelMask);

		GraphicsHelper.Dispatch(m_Clouds, source.width, source.height);

		Graphics.Blit(m_IntermediateTexture, destination);

		if (Application.isPlaying)
			ScreenCapture.CaptureScreenshot("Screenshots/OldIntersection.png");
	}

	private void OnValidate()
	{
		// Recreate textures if needed
		TextureHelper.CreateTexture3D(ref m_ShapeNoise, ShapeNoiseResolution, GraphicsFormat.R8G8B8A8_UNorm, "Shape Noise");
		TextureHelper.CreateTexture3D(ref m_DetailNoise, DetailNoiseResolution, GraphicsFormat.R8G8B8A8_UNorm, "Detail Noise");
		TextureHelper.CreateTexture2D(ref m_HeightDensityGradient, 128, 128, GraphicsFormat.R8_UNorm, "Height Gradient");

		m_CloudShapeNoise.SetInt("WorleyMethod", WorleyMethod);
		m_CloudShapeNoise.SetInt("PerlinMethod", PerlinMethod);
		m_CloudShapeNoise.SetInt("Seed", Seed);
		m_CloudShapeNoise.SetInt("Frequency", ShapeNoiseFrequency);
		m_CloudShapeNoise.SetFloat("ResolutionInv", 1.0f / ShapeNoiseResolution);
		m_CloudShapeNoise.SetTexture(0, "NoiseOutput", m_ShapeNoise);
		m_CloudShapeNoise.SetTexture(0, "HeightGradient", m_HeightDensityGradient);
		GraphicsHelper.Dispatch(m_CloudShapeNoise, ShapeNoiseResolution, ShapeNoiseResolution, ShapeNoiseResolution, 0);

		m_CloudShapeNoise.SetInt("WorleyMethod", WorleyMethod);
		m_CloudShapeNoise.SetInt("PerlinMethod", PerlinMethod);
		m_CloudShapeNoise.SetInt("Seed", Seed);
		m_CloudShapeNoise.SetInt("Frequency", DetailNoiseFrequency);
		m_CloudShapeNoise.SetFloat("ResolutionInv", 1.0f / DetailNoiseResolution);
		m_CloudShapeNoise.SetTexture(1, "NoiseOutput", m_DetailNoise);
		m_CloudShapeNoise.SetTexture(1, "HeightGradient", m_HeightDensityGradient);
		GraphicsHelper.Dispatch(m_CloudShapeNoise, DetailNoiseResolution, DetailNoiseResolution, DetailNoiseResolution, 1);

		m_CloudShapeNoise.SetInt("WorleyMethod", WorleyMethod);
		m_CloudShapeNoise.SetInt("PerlinMethod", PerlinMethod);
		m_CloudShapeNoise.SetInt("Seed", Seed);
		m_CloudShapeNoise.SetInt("Frequency", DetailNoiseFrequency);
		m_CloudShapeNoise.SetFloat("ResolutionInv", 1.0f / m_HeightDensityGradient.width);
		m_CloudShapeNoise.SetTexture(2, "NoiseOutput", m_DetailNoise);
		m_CloudShapeNoise.SetTexture(2, "HeightGradient", m_HeightDensityGradient);
		GraphicsHelper.Dispatch(m_CloudShapeNoise, 128, 128, 128, 2);
	}
}
