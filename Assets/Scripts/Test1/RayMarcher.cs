using Helpers;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class RayMarcher : MonoBehaviour
{
	public Transform Container;
	public Shader Shader;
	public Texture3D Texture;
	private Material m_Material;

	[Range(0.0f, 3.0f)]
	public float Absorption;

	[Range(1, 128)]
	public int NumSteps = 8;

	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (m_Material == null)
			m_Material = new Material(Shader);

		m_Material.SetTexture("Cloud3DNoiseTextureShape", Texture);
		m_Material.SetVector("_BoundsMin", Container.position - Container.localScale / 2);
		m_Material.SetVector("_BoundsMax", Container.position + Container.localScale / 2);
		m_Material.SetFloat("_Absorption", Absorption);
		m_Material.SetFloat("_NumSteps", NumSteps);

		// Blit the result texture to the screen
		Graphics.Blit(source, destination, m_Material);
	}
}
