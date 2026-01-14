using Helpers;
using System.IO;
using UnityEditor;
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

	[Range(32, 256), Tooltip("The maximum number of steps the raymarcher will take")]
	public int NumSteps = 128;

	[Range(2.0f, 4.0f)]
	public float LargeStepSizeMultiplier = 3.0f;

	[Range(0.0f, 1.0f)]       public float GlobalDensity = 0.1f;
	[Range(0.0001f, 0.001f)]  public float GlobalScale = 0.001f;
	[Range(0.0f, 1.0f)]       public float Coverage = 0.9f;
	[Range(0.0f, 1.0f)]		  public float CloudType = 0.5f;

	[Range(0.0f, 360.0f)] public float WindAngle = 0.0f;
	[Range(0.01f, 10.0f)] public float CloudSpeed = 1.0f;

	[Range(0.0f, 250.0f), Tooltip("Pushes the tops of the clouds along the wind direction by this many units")]
	public float CloudTopOffset = 100.0f;

	public bool DualLobPhase = true;
	[Range(0.0f, 1.0f)] public float ForwardScattering = 0.8f;
	[Range(-1.0f, 0.0f)] public float BackwardScattering = -0.2f;
	[Range(0.0f, 1.0f)] public float ScatteringWeight = 0.5f;

	[Range(1000.0f, 1000000.0f)] public float PlanetRadius = 60000.0f; // Earth's radius in meters
	public Vector2 AtmosphereHeightRange = new Vector2(200.0f, 900.0f);

	[Header("Debug")]

	public bool EnableDebug = true;
	public bool UseJitter = true;

	[Range(0.0f, 1.0f)]
	public float TextureSlice = 0.0f;

	private Texture2D[] m_Slices; // #Saving

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
	[SerializeField] ComputeShader m_SlicerShader; // #Saving

	private RenderTexture m_IntermediateTexture;
	private RenderTexture m_ShapeNoise;
	private RenderTexture m_DetailNoise;
	private RenderTexture m_HeightDensityGradient;

	private Camera m_Camera;

	private void Start()
	{
		m_Camera = GetComponent<Camera>();

		OnValidate();

		// SaveNoiseAsPNGs("_CloudShapeA", m_ShapeNoise);
		// SaveNoiseAsPNGs("_CloudDetailB", m_DetailNoise);
	}

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		TextureHelper.Create2D(ref m_IntermediateTexture, source.width, source.height, source.graphicsFormat, "Clouds Intermediate Texture");

		m_Clouds.SetVector("ViewportDimensions", new Vector2(source.width, source.height));
		m_Clouds.SetVector("ViewportDimensionsInv", new Vector2(1.0f / source.width, 1.0f / source.height));
		m_Clouds.SetVector("CameraPosition", transform.position);

		m_Clouds.SetMatrix("CameraToWorld", m_Camera.cameraToWorldMatrix);
		m_Clouds.SetMatrix("CameraInverseProjection", m_Camera.projectionMatrix.inverse);

		m_Clouds.SetFloat("PlanetRadius", PlanetRadius);
		m_Clouds.SetVector("AtmosphereHeightRange", AtmosphereHeightRange);

		Light sun = FindObjectOfType<Light>();
		m_Clouds.SetVector("SunDirection", sun.transform.forward);
		m_Clouds.SetVector("SunColor", sun.color);

		m_Clouds.SetInt("NumSteps", NumSteps);
		m_Clouds.SetFloat("LargeStepSizeMultiplier", LargeStepSizeMultiplier);

		m_Clouds.SetFloat("GlobalDensity", GlobalDensity);
		m_Clouds.SetFloat("GlobalScale", GlobalScale);
		m_Clouds.SetFloat("ShapeNoiseScale", ShapeNoiseScale);
		m_Clouds.SetFloat("DetailNoiseScale", DetailNoiseScale);
		m_Clouds.SetFloat("Coverage", Coverage);
		m_Clouds.SetFloat("CloudType", CloudType);
		m_Clouds.SetBool("PhaseMethod", DualLobPhase);
		m_Clouds.SetFloat("ForwardScattering", ForwardScattering);
		m_Clouds.SetFloat("BackwardScattering", BackwardScattering);
		m_Clouds.SetFloat("ScatteringWeight", ScatteringWeight);

		m_Clouds.SetVector("WindDirection", new Vector3(Mathf.Cos(WindAngle * Mathf.Deg2Rad), 0, -Mathf.Sin(WindAngle * Mathf.Deg2Rad)));
		m_Clouds.SetFloat("CloudSpeed", CloudSpeed);
		m_Clouds.SetFloat("CloudTopOffset", CloudTopOffset);
		m_Clouds.SetInt("Time", Time.frameCount);
		
		m_Clouds.SetTexture(0, "ShapeTexture", m_ShapeNoise);
		m_Clouds.SetTexture(0, "DetailTexture", m_DetailNoise);
		m_Clouds.SetTexture(0, "HeightGradient", m_HeightDensityGradient);
		m_Clouds.SetTexture(0, "SceneTexture", source);
		m_Clouds.SetTexture(0, "DepthTexture", Shader.GetGlobalTexture("_CameraDepthTexture"));
		m_Clouds.SetTexture(0, "Output", m_IntermediateTexture);

		// Debug parameters
		m_Clouds.SetBool("Debug", EnableDebug);
		m_Clouds.SetBool("UseJitter", UseJitter);
		m_Clouds.SetFloat("TextureSlice", TextureSlice);
		m_Clouds.SetVector("ChannelMask", ChannelMask);

		GraphicsHelper.Dispatch(m_Clouds, source.width, source.height);

		Graphics.Blit(m_IntermediateTexture, destination);
		// Graphics.Blit(source, destination);

		if (Application.isPlaying)
			ScreenCapture.CaptureScreenshot("Screenshots/_CloudHeightGradient.png");
	}

	private void SaveNoiseAsPNGs(string inFileName, RenderTexture inTexture)
	{
#if UNITY_EDITOR
		int resolution = inTexture.width;
		m_Slices = new Texture2D[resolution];

		m_SlicerShader.SetTexture(0, "_Input", inTexture);

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

		// Save To Disk as PNG
		byte[] bytes = m_Slices[0].EncodeToPNG();
		var dirPath = Application.dataPath + "/../Screenshots/";
		File.WriteAllBytes(dirPath + inFileName + ".png", bytes);
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


	private void OnValidate()
	{
		// Recreate textures if needed
		TextureHelper.Create3D(ref m_ShapeNoise, ShapeNoiseResolution, GraphicsFormat.R8G8B8A8_UNorm, "Shape Noise");
		TextureHelper.Create3D(ref m_DetailNoise, DetailNoiseResolution, GraphicsFormat.R8G8B8A8_UNorm, "Detail Noise");
		TextureHelper.Create2D(ref m_HeightDensityGradient, 128, 128, GraphicsFormat.R8_UNorm, "Height Gradient");

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
