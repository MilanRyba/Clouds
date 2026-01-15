using Helpers;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class Clouds : MonoBehaviour
{
	public CloudSettings CloudSettings;
	public NoiseSettings NoiseSettings;


	[Header("Rendering")]

	[Range(8, 256), Tooltip("The maximum number of samples taken by the ray marcher")]
	public int MaxSamles = 128;

	[Range(1.0f, 5.0f)]
	public float LargeStepSizeMultiplier = 3.0f;


	[Header("Debug")]

	[Tooltip("Show pixels that ended the ray marching loop early due to low transmittance")]
	public bool EarlyTerminatedPixels = false;

	[Tooltip("Offset starting sample positions")]
	public bool EnableJitter = true;

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

		m_Clouds.SetFloat("GlobalDensity", CloudSettings.GlobalDensity);
		m_Clouds.SetFloat("GlobalCoverage", CloudSettings.GlobalCoverage);
		m_Clouds.SetFloat("CloudType", CloudSettings.CloudType);
		m_Clouds.SetVector("WindDirection", 
			new Vector3(Mathf.Cos(CloudSettings.WindDirection * Mathf.Deg2Rad), 0, -Mathf.Sin(CloudSettings.WindDirection * Mathf.Deg2Rad)));
		m_Clouds.SetFloat("CloudSpeed", CloudSettings.CloudSpeed);
		m_Clouds.SetFloat("CloudTopOffset", CloudSettings.CloudTopOffset);

		m_Clouds.SetFloat("BaseScale", CloudSettings.BaseScale);
		m_Clouds.SetFloat("DetailScale", CloudSettings.DetailScale);
		m_Clouds.SetFloat("Absorption", CloudSettings.Absorption);

		m_Clouds.SetFloat("Eccentricity", CloudSettings.Eccentricity);
		m_Clouds.SetFloat("Intensity", CloudSettings.Intensity);
		m_Clouds.SetFloat("Spread", CloudSettings.Spread);

		m_Clouds.SetVector("ViewportDimensions", new Vector2(source.width, source.height));
		m_Clouds.SetVector("ViewportDimensionsInv", new Vector2(1.0f / source.width, 1.0f / source.height));
		m_Clouds.SetVector("CameraPosition", transform.position);
		m_Clouds.SetMatrix("CameraToWorld", m_Camera.cameraToWorldMatrix);
		m_Clouds.SetMatrix("CameraInverseProjection", m_Camera.projectionMatrix.inverse);

		m_Clouds.SetInt("MaxSamples", MaxSamles);
		m_Clouds.SetFloat("LargeStepSizeMultiplier", LargeStepSizeMultiplier);

		m_Clouds.SetVector("SunDirection", m_Sun.transform.forward);
		m_Clouds.SetVector("SunColor", m_Sun.color);
		
		m_Clouds.SetInt("Time", Time.frameCount);
		
		m_Clouds.SetTexture(0, "SceneTexture", source);
		m_Clouds.SetTexture(0, "DepthTexture", Shader.GetGlobalTexture("_CameraDepthTexture"));
		m_Clouds.SetTexture(0, "ShapeTexture", m_ShapeNoise);
		m_Clouds.SetTexture(0, "DetailTexture", m_DetailNoise);
		m_Clouds.SetTexture(0, "HeightGradient", m_HeightDensityGradient);
		m_Clouds.SetTexture(0, "Output", m_IntermediateTexture);
		
		// Debug parameters
		m_Clouds.SetBool("EarlyTerminatedPixels", EarlyTerminatedPixels);
		m_Clouds.SetBool("EnableJitter", EnableJitter);
		m_Clouds.SetFloat("TextureSlice", TextureSlice);
		m_Clouds.SetVector("ChannelMask", ChannelMask);
		
		GraphicsHelper.Dispatch(m_Clouds, source.width, source.height);
		
		Graphics.Blit(m_IntermediateTexture, destination);
		
		// if (Application.isPlaying)
		// 	ScreenCapture.CaptureScreenshot("Screenshots/_CloudHeightGradient.png");
	}


	private void OnValidate()
	{
		int shapeResolution = NoiseSettings.ShapeNoiseResolution;
		int detailResolution = NoiseSettings.DetailNoiseResolution;

		// Recreate textures if needed
		TextureHelper.Create3D(ref m_ShapeNoise, shapeResolution, GraphicsFormat.R16G16B16A16_SFloat, "Shape Noise");
		TextureHelper.Create3D(ref m_DetailNoise, detailResolution, GraphicsFormat.R16G16B16A16_SFloat, "Detail Noise");
		TextureHelper.Create2D(ref m_HeightDensityGradient, 128, 128, GraphicsFormat.R8_UNorm, "Height Gradient");

		m_CloudShapeNoise.SetInt("Seed", NoiseSettings.Seed);
		m_CloudShapeNoise.SetInt("WorleyNoiseFrequency", NoiseSettings.ShapeNoiseFrequency);
		m_CloudShapeNoise.SetFloat("ResolutionInv", 1.0f / shapeResolution);
		m_CloudShapeNoise.SetTexture(0, "NoiseOutput", m_ShapeNoise);
		m_CloudShapeNoise.SetTexture(0, "HeightGradient", m_HeightDensityGradient);
		GraphicsHelper.DispatchXYZ(m_CloudShapeNoise, shapeResolution, 0);

		m_CloudShapeNoise.SetInt("Seed", NoiseSettings.Seed);
		m_CloudShapeNoise.SetInt("WorleyNoiseFrequency", NoiseSettings.DetailNoiseFrequency);
		m_CloudShapeNoise.SetFloat("ResolutionInv", 1.0f / detailResolution);
		m_CloudShapeNoise.SetTexture(1, "NoiseOutput", m_DetailNoise);
		m_CloudShapeNoise.SetTexture(1, "HeightGradient", m_HeightDensityGradient);
		GraphicsHelper.DispatchXYZ(m_CloudShapeNoise, detailResolution, 1);
		
		m_CloudShapeNoise.SetInt("Seed", NoiseSettings.Seed);
		m_CloudShapeNoise.SetInt("WorleyNoiseFrequency", NoiseSettings.DetailNoiseFrequency);
		m_CloudShapeNoise.SetFloat("ResolutionInv", 1.0f / m_HeightDensityGradient.width);
		m_CloudShapeNoise.SetTexture(2, "NoiseOutput", m_DetailNoise);
		m_CloudShapeNoise.SetTexture(2, "HeightGradient", m_HeightDensityGradient);
		GraphicsHelper.DispatchXYZ(m_CloudShapeNoise, 128, 2);
	}
}
