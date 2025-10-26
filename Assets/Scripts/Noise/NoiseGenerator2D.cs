using Helpers;
using UnityEngine;

[ExecuteInEditMode]
public class NoiseGenerator2D : MonoBehaviour
{
	[SerializeField]
	ComputeShader m_Noise2DShader;

	[Range(1, 512)]
	public int TextureResolution = 128;

	public NoiseSettings Settings;
	public RenderTexture NoiseTexture;

	public enum TextureChannel { R, G, B, A }
	public TextureChannel ActiveChannel;

	private Vector4 ChannelMask
	{
		get
		{
			Vector4 v = new Vector4(
				(ActiveChannel == TextureChannel.R) ? 1 : 0,
				(ActiveChannel == TextureChannel.G) ? 1 : 0,
				(ActiveChannel == TextureChannel.B) ? 1 : 0,
				(ActiveChannel == TextureChannel.A) ? 1 : 0
				);
			return v;
		}
	}

	public void UpdateNoise()
	{
		Debug.Log("Updating Noise");

		// Re-create the texture if needed
		TextureHelper.CreateTexture2D(ref NoiseTexture, TextureResolution, "WorleyNoise");

		SetNoiseSettings(Settings);

		m_Noise2DShader.SetTexture(0, "_Result", NoiseTexture);
		m_Noise2DShader.SetInt("_Resolution", NoiseTexture.width);
		m_Noise2DShader.SetVector("_ChannelMask", ChannelMask);

		GraphicsHelper.Dispatch(m_Noise2DShader, NoiseTexture.width, NoiseTexture.height);
	}

	private void SetNoiseSettings(NoiseSettings inSettings)
	{
		m_Noise2DShader.SetInt("_Worley_NumCells", inSettings.Worley.NumCells);
		m_Noise2DShader.SetInt("_Perlin_NumOctaves", inSettings.Perlin.NumOctaves);
		m_Noise2DShader.SetFloat("_Perlin_Frequency", inSettings.Perlin.Frequency);
	}
}
