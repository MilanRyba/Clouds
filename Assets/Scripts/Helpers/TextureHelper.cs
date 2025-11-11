using UnityEngine;

namespace Helpers
{
	// Helper class for texture manipulation, i.e. creation, serialization
	public static class TextureHelper
	{
		#region Create

		// Create 2D texture with a given resolution
		public static void CreateTexture2D(ref RenderTexture outTexture, int inResolution, string inName)
		{
			CreateTexture2D(ref outTexture, inResolution, inResolution, inName);
		}

		public static void CreateTexture2D(ref RenderTexture outTexture, int inWidth, int inHeight, string inName)
		{
			var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm;

			if (outTexture == null || !outTexture.IsCreated() ||
				outTexture.width != inWidth || outTexture.height != inHeight ||
				outTexture.graphicsFormat != format)
			{
				if (outTexture != null)
					outTexture.Release();

				outTexture = new RenderTexture(inWidth, inHeight, 0);
				outTexture.graphicsFormat = format;
				outTexture.enableRandomWrite = true;
				outTexture.Create();
			}

			outTexture.wrapMode = TextureWrapMode.Repeat;
			outTexture.filterMode = FilterMode.Point;
		}

		public static void CreateTexture3D(ref RenderTexture texture, int resolution, string name)
		{
			var format = UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_UNorm;
			if (texture == null || !texture.IsCreated() || 
				texture.width != resolution || texture.height != resolution || texture.volumeDepth != resolution || 
				texture.graphicsFormat != format)
			{
				if (texture != null)
					texture.Release();

				texture = new RenderTexture(resolution, resolution, 0);
				texture.graphicsFormat = format;
				texture.enableRandomWrite = true;
				texture.volumeDepth = resolution;
				texture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
				texture.name = name;

				texture.Create();
			}
			texture.wrapMode = TextureWrapMode.Repeat;
			texture.filterMode = FilterMode.Bilinear;
		}

		#endregion

		#region #Buffers

		public static void Release(ComputeBuffer inBuffer)
		{
			inBuffer?.Release();
		}

		#endregion
	}
}
