using Helpers;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class CloudsPlayground : MonoBehaviour
{
	[Header("Resources")]

	[SerializeField] ComputeShader m_Clouds;

	private RenderTexture m_IntermediateTexture;
	public Texture3D CloudNoise;
	private Texture3D DetailNoise;
	public Texture2D WeatherMap;
	private Texture2D CurlNoise;

	private Camera m_Camera;
	private Light m_Sun;
	public Transform Container;

	// These values map to heights of 256 and 2048 meters
	[Range(0.0f, 1.0f)]
	public float CloudMinHeight = 0.0f;
	[Range(0.0f, 1.0f)]
	public float CloudMaxHeight = 1.0f;

	[Range(0.0f, 1.0f), Tooltip("Allows manual control over coverage. Lesser values remove coverage.")]
	public float CloudCoverage = 1.0f;

	[Range(0.0f, 1.0f), Tooltip("Contorls the height of clouds.")]
	public float CloudType = 0.6f;

	[Range(0.0f, 2.0f)]
	public float Absorption = 1.0f;

	[Range(1, 256)]
	public int NumSamples = 64;

	[Range(5, 15)]
	public int NumLightSamples = 6;

	[Range(0.0f, 1.0f)]
	public float Eccentricity = 0.6f;

	[Range(0.0f, 4.0f)]
	public float SilverIntensity = 1.0f;

	[Range(0.0f, 4.0f)]
	public float SilverSpread = 1.0f;

	[Range(0.0f, 4.0f)]
	public float LightIntensity = 1.0f;

	public bool DEBUG_EarlyExit = false;

	// Shader Property IDs
	int ID_ViewportDimensions;
	int ID_ViewportDimensionsInv;

	int ID_CameraPosition;
	int ID_ProjInv;
	int ID_ViewInv;

	int ID_Absorption;
	int ID_NumSamples;
	int ID_NumLightSamples;
	int ID_SunDirection;
	int ID_SunColor;

	int ID_CloudType;

	int ID_Eccentricity;
	int ID_SilverIntensity;
	int ID_SilverSpread;
	int ID_LightIntensity;

	int ID_DEBUG_EarlyExit;

	int ID_Result;
	int ID_SceneTexture;
	int ID_DepthTexture;
	int ID_WeatherMap;
	int ID_CloudNoise;

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		TextureHelper.Create2D(ref m_IntermediateTexture, source.width, source.height, GraphicsFormat.R16G16B16A16_SFloat, "Clouds Intermediate Texture");

		m_Clouds.SetVector(ID_ViewportDimensions, new Vector2(source.width, source.height));
		m_Clouds.SetVector(ID_ViewportDimensionsInv, new Vector2(1.0f / source.width, 1.0f / source.height));

		m_Clouds.SetVector(ID_CameraPosition, m_Camera.transform.position);
		m_Clouds.SetMatrix(ID_ProjInv, m_Camera.projectionMatrix.inverse);
		m_Clouds.SetMatrix(ID_ViewInv, m_Camera.cameraToWorldMatrix);

		m_Clouds.SetVector("BoundsMin", Container.position - Container.localScale / 2);
		m_Clouds.SetVector("BoundsMax", Container.position + Container.localScale / 2);

		m_Clouds.SetVector("BoxCenter", Container.position);
		m_Clouds.SetVector("BoxBounds", Container.localScale);

		m_Clouds.SetFloat(ID_Absorption, Absorption);
		m_Clouds.SetInt(ID_NumSamples, NumSamples);
		m_Clouds.SetInt(ID_NumLightSamples, NumLightSamples);
		m_Clouds.SetVector(ID_SunDirection, m_Sun.transform.forward);
		m_Clouds.SetVector(ID_SunColor, m_Sun.color);

		// m_Clouds.SetFloat(ID_CloudType, CloudType);

		m_Clouds.SetFloat(ID_Eccentricity, Eccentricity);
		m_Clouds.SetFloat(ID_SilverIntensity, SilverIntensity);
		m_Clouds.SetFloat(ID_SilverSpread, SilverSpread);
		m_Clouds.SetFloat(ID_LightIntensity, LightIntensity);

		m_Clouds.SetBool(ID_DEBUG_EarlyExit, DEBUG_EarlyExit);

		m_Clouds.SetTexture(0, ID_Result, m_IntermediateTexture);
		m_Clouds.SetTexture(0, ID_SceneTexture, source);
		m_Clouds.SetTexture(0, ID_DepthTexture, Shader.GetGlobalTexture("_CameraDepthTexture"));
		m_Clouds.SetTexture(0, ID_WeatherMap, WeatherMap);
		m_Clouds.SetTexture(0, ID_CloudNoise, CloudNoise);

		GraphicsHelper.Dispatch(m_Clouds, source.width, source.height);

		Graphics.Blit(m_IntermediateTexture, destination);
	}

	private void OnValidate()
	{
		m_Camera = GetComponent<Camera>();
		m_Sun = FindObjectOfType<Light>();

		ID_ViewportDimensions = Shader.PropertyToID("ViewportDimensions");
		ID_ViewportDimensionsInv = Shader.PropertyToID("ViewportDimensionsInv");

		ID_CameraPosition = Shader.PropertyToID("CameraPosition");
		ID_ProjInv = Shader.PropertyToID("ProjInv");
		ID_ViewInv = Shader.PropertyToID("ViewInv");

		ID_Absorption = Shader.PropertyToID("Absorption");
		ID_NumSamples = Shader.PropertyToID("NumSamples");
		ID_NumLightSamples = Shader.PropertyToID("NumLightSamples");
		ID_SunDirection = Shader.PropertyToID("SunDirection");
		ID_SunColor = Shader.PropertyToID("SunColor");

		ID_CloudType = Shader.PropertyToID("CloudType");

		ID_Eccentricity = Shader.PropertyToID("Eccentricity");
		ID_SilverIntensity = Shader.PropertyToID("SilverIntensity");
		ID_SilverSpread = Shader.PropertyToID("SilverSpread");
		ID_LightIntensity = Shader.PropertyToID("LightIntensity");

		ID_DEBUG_EarlyExit = Shader.PropertyToID("DEBUG_EarlyExit");

		ID_Result = Shader.PropertyToID("Result");
		ID_SceneTexture = Shader.PropertyToID("SceneTexture");
		ID_DepthTexture = Shader.PropertyToID("DepthTexture");
		ID_WeatherMap = Shader.PropertyToID("WeatherMap");
		ID_CloudNoise = Shader.PropertyToID("CloudNoise");
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		Gizmos.DrawWireCube(Container.position, Container.localScale);
	}
}
