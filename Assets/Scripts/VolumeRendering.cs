using Helpers;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class VolumeRendering : MonoBehaviour
{
	public Color BackgroundColor = new Color(0.572f, 0.772f, 0.921f);
	public bool UseBackgroundColor = false;

	public float SigmaA = 0.1f;
	public Color Scatter = new Color(0.8f, 0.1f, 0.5f);

	public Vector3 SphereCenter = Vector3.zero;
	public float SphereRadius = 1;
	public bool ShowGizmos = false;

	public ComputeShader m_Shader;

	private Camera m_Camera;
	private RenderTexture m_IntermediateTexture;

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		TextureHelper.Create2D(ref m_IntermediateTexture, source.width, source.height, source.graphicsFormat, "Intermediate Texture");

		m_Shader.SetVector("ViewportDimensions", new Vector2(source.width, source.height));
		m_Shader.SetVector("ViewportDimensionsInv", new Vector2(1.0f / source.width, 1.0f / source.height));
		m_Shader.SetVector("CameraPosition", transform.position);

		m_Shader.SetMatrix("CameraToWorld", m_Camera.cameraToWorldMatrix);
		m_Shader.SetMatrix("CameraInverseProjection", m_Camera.projectionMatrix.inverse);

		m_Shader.SetVector("BackgroundColor", BackgroundColor);
		m_Shader.SetBool("UseBackgroundColor", UseBackgroundColor);

		m_Shader.SetFloat("SigmaA", SigmaA);
		m_Shader.SetVector("Scatter", Scatter);

		m_Shader.SetVector("SphereCenter", SphereCenter);
		m_Shader.SetFloat("SphereRadius", SphereRadius);

		m_Shader.SetTexture(0, "SceneTexture", source);
		m_Shader.SetTexture(0, "Result", m_IntermediateTexture);

		// Dispatch the compute shader
		GraphicsHelper.Dispatch(m_Shader, source.width, source.height);

		// Blit our result into the destination texture
		Graphics.Blit(m_IntermediateTexture, destination);
	}

	private void OnValidate()
	{
		m_Camera = GetComponent<Camera>();
	}

	private void OnDrawGizmos()
	{
		if (ShowGizmos)
		{
			Gizmos.color = Scatter;
			Gizmos.DrawWireSphere(SphereCenter, SphereRadius);
		}
	}
}
