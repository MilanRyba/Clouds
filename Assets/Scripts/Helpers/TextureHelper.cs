using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace Helpers
{
	// Helper class for texture manipulation, i.e. creation, serialization
	public static class TextureHelper
	{
		#region Create

		// Create 2D texture with a given resolution
		public static void CreateTexture2D(ref RenderTexture outTexture, int inResolution, GraphicsFormat inFormat, string inName)
		{
			CreateTexture2D(ref outTexture, inResolution, inResolution, inFormat, inName);
		}

		public static void CreateTexture2D(ref RenderTexture outTexture, int inWidth, int inHeight, GraphicsFormat inFormat, string inName)
		{
			if (outTexture == null || !outTexture.IsCreated() ||
				outTexture.width != inWidth || outTexture.height != inHeight ||
				outTexture.graphicsFormat != inFormat)
			{
				if (outTexture != null)
					outTexture.Release();

				outTexture = new RenderTexture(inWidth, inHeight, 0);
				outTexture.graphicsFormat = inFormat;
				outTexture.enableRandomWrite = true;
				outTexture.Create();
			}

			// The filter and wrap modes are kind of useless to set here
			// since we determine this in the shader while sampling
			outTexture.wrapMode = TextureWrapMode.Repeat;
			outTexture.filterMode = FilterMode.Point;
		}

		public static void CreateTexture3D(ref RenderTexture outTexture, int inResolution, GraphicsFormat inFormat, string inName)
		{
			if (outTexture == null || !outTexture.IsCreated() || 
				outTexture.width != inResolution || outTexture.height != inResolution || outTexture.volumeDepth != inResolution || 
				outTexture.graphicsFormat != inFormat)
			{
				if (outTexture != null)
					outTexture.Release();

				outTexture = new RenderTexture(inResolution, inResolution, 0);
				outTexture.graphicsFormat = inFormat;
				outTexture.enableRandomWrite = true;
				outTexture.volumeDepth = inResolution;
				outTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
				outTexture.name = inName;

				outTexture.Create();
			}
			outTexture.wrapMode = TextureWrapMode.Repeat;
			outTexture.filterMode = FilterMode.Bilinear;
		}

		#endregion
	}
}
