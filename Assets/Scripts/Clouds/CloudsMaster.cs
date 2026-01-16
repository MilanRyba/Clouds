using Helpers;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class CloudsMaster : MonoBehaviour
{
	[Header("Weather")]

	[Range(0.0f, 1.0f)]
	public float Coverage = 0.9f;

	[Range(0.0f, 1.0f), Tooltip("Type of the cloud to render. 0 -> stratus, 0.5 -> stratocumulus, 1 -> cumulus")]
	public float CloudType = 0.5f;

	[Range(0.0f, 360.0f), Tooltip("Angle of the global wind direction")]
	public float WindAngle = 0.0f;

	[Range(0.01f, 10.0f), Tooltip("Speed of the clouds")]
	public float CloudSpeed = 1.0f;

	[Range(0.0f, 250.0f), Tooltip("Pushes the tops of the clouds along the wind direction by this many units")]
	public float CloudTopOffset = 100.0f;

	[Range(0.0f, 1.0f)]
	public float GlobalDensity = 0.021f;

	[Range(0.0001f, 0.001f)]
	public float GlobalScale = 0.001f;


	[Header("Clouds")]

	[Range(0.1f, 5.0f), Tooltip("Scale of the base cloud shape")]
	public float ShapeNoiseScale = 0.3f;

	[Range(0.1f, 5.0f), Tooltip("Scale of the cloud details")]
	public float DetailNoiseScale = 0.3f;


	[Header("Phase")]

	[Range(-0.99f, 0.99f), Tooltip("Directional scattering bias. Values >1 make the light scatter forward and values <1 backward")]
	public float Eccentricity = 0.65f;

	[Range(0.0f, 5.0f)]
	public float Intensity = 0.95f;

	[Range(0.0f, 1.0f)]
	public float Spread = 1.0f;


	[Header("Rendering")]

	[Range(8, 256), Tooltip("The maximum number of steps the raymarcher will take")]
	public int NumSteps = 128;

	[Range(2.0f, 4.0f)]
	public float LargeStepSizeMultiplier = 3.0f;

	[Tooltip("Offsets the starting sample position during the ray march")]
	public bool UseJitter = true;

	[Range(1000.0f, 1000000.0f)]
	public float PlanetRadius = 60000.0f; // Earth's radius in meters

	public Vector2 AtmosphereHeightRange = new Vector2(200.0f, 900.0f);


	[Header("Noise")]

	[Range(1, 10)] public int ShapeNoiseFrequency = 3;
	[Range(32, 256)] public int ShapeNoiseResolution = 128;

	[Range(1, 10)] public int DetailNoiseFrequency = 3;
	[Range(1, 64)] public int DetailNoiseResolution = 32;


	[Header("Debug")]

	[Tooltip("Show pixels that ended the ray marching loop early due to low transmittance")]
	public bool EarlyTerminatedPixels = false;

	public bool ShowTextureSlices = false;	

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
	private Light m_Sun;

	private void Start()
	{
		m_Camera = GetComponent<Camera>();
		m_Sun = FindObjectOfType<Light>();

		OnValidate();
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

		m_Clouds.SetVector("SunDirection", m_Sun.transform.forward);
		m_Clouds.SetVector("SunColor", m_Sun.color);

		m_Clouds.SetInt("NumSteps", NumSteps);
		m_Clouds.SetFloat("LargeStepSizeMultiplier", LargeStepSizeMultiplier);
		m_Clouds.SetBool("UseJitter", UseJitter);

		m_Clouds.SetFloat("GlobalDensity", GlobalDensity);
		m_Clouds.SetFloat("GlobalScale", GlobalScale);
		m_Clouds.SetFloat("ShapeNoiseScale", ShapeNoiseScale);
		m_Clouds.SetFloat("DetailNoiseScale", DetailNoiseScale);
		m_Clouds.SetFloat("Coverage", Coverage);
		m_Clouds.SetFloat("CloudType", CloudType);

		m_Clouds.SetFloat("Eccentricity", Eccentricity);
		m_Clouds.SetFloat("Intensity", Intensity);
		m_Clouds.SetFloat("Spread", Spread);

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
		m_Clouds.SetBool("Debug", ShowTextureSlices);
		m_Clouds.SetFloat("TextureSlice", TextureSlice);
		m_Clouds.SetVector("ChannelMask", ChannelMask);

		GraphicsHelper.Dispatch(m_Clouds, source.width, source.height);

		Graphics.Blit(m_IntermediateTexture, destination);

		if (Application.isPlaying)
			ScreenCapture.CaptureScreenshot("Screenshots/Worley_NoTiling.png");
	}

	private void OnValidate()
	{
		// Recreate textures if needed
		TextureHelper.Create3D(ref m_ShapeNoise, ShapeNoiseResolution, GraphicsFormat.R8G8B8A8_UNorm, "Shape Noise");
		TextureHelper.Create3D(ref m_DetailNoise, DetailNoiseResolution, GraphicsFormat.R8G8B8A8_UNorm, "Detail Noise");
		TextureHelper.Create2D(ref m_HeightDensityGradient, 128, 128, GraphicsFormat.R8_UNorm, "Height Gradient");

		m_CloudShapeNoise.SetInt("WorleyNoiseFrequency", ShapeNoiseFrequency);
		m_CloudShapeNoise.SetFloat("ResolutionInv", 1.0f / ShapeNoiseResolution);
		m_CloudShapeNoise.SetTexture(0, "NoiseOutput", m_ShapeNoise);
		m_CloudShapeNoise.SetTexture(0, "HeightGradient", m_HeightDensityGradient);
		GraphicsHelper.DispatchXYZ(m_CloudShapeNoise, ShapeNoiseResolution, 0);

		m_CloudShapeNoise.SetInt("WorleyNoiseFrequency", DetailNoiseFrequency);
		m_CloudShapeNoise.SetFloat("ResolutionInv", 1.0f / DetailNoiseResolution);
		m_CloudShapeNoise.SetTexture(1, "NoiseOutput", m_DetailNoise);
		m_CloudShapeNoise.SetTexture(1, "HeightGradient", m_HeightDensityGradient);
		GraphicsHelper.DispatchXYZ(m_CloudShapeNoise, DetailNoiseResolution, 1);

		m_CloudShapeNoise.SetInt("WorleyNoiseFrequency", DetailNoiseFrequency);
		m_CloudShapeNoise.SetFloat("ResolutionInv", 1.0f / m_HeightDensityGradient.width);
		m_CloudShapeNoise.SetTexture(2, "NoiseOutput", m_DetailNoise);
		m_CloudShapeNoise.SetTexture(2, "HeightGradient", m_HeightDensityGradient);
		GraphicsHelper.DispatchXYZ(m_CloudShapeNoise, 128, 2);
	}
}