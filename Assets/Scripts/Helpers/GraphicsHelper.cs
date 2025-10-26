using UnityEngine;

namespace Helpers
{
	public static class GraphicsHelper
	{
		#region #Compute

		public static Vector3Int GetThreadGroupSizes(ComputeShader inShader, int inKernel = 0)
		{
			uint x, y, z;
			inShader.GetKernelThreadGroupSizes(inKernel, out x, out y, out z);
			return new Vector3Int((int)x, (int)y, (int)z);
		}

		public static void Dispatch(ComputeShader inShader, 
			int inNumInvocationsX = 1, int inNumInvocationsY = 1, int inNumInvocationsZ = 1, int inKernel = 0)
		{
			Vector3Int threadGroupSizes = GetThreadGroupSizes(inShader, inKernel);
			int numGroupsX = Mathf.CeilToInt(inNumInvocationsX / (float)threadGroupSizes.x);
			int numGroupsY = Mathf.CeilToInt(inNumInvocationsY / (float)threadGroupSizes.y);
			int numGroupsZ = Mathf.CeilToInt(inNumInvocationsZ / (float)threadGroupSizes.z);
			inShader.Dispatch(inKernel, numGroupsX, numGroupsY, numGroupsZ);
		}

		#endregion

		#region #Buffers

		public static int GetStride<T>()
		{
			return System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
		}

		public static void CreateStructuredBuffer<T>(ref ComputeBuffer outBuffer, int inCount)
		{
			int stride = GetStride<T>();
			bool createNewBuffer = outBuffer == null || !outBuffer.IsValid() || outBuffer.count != inCount || outBuffer.stride != stride;
			if (createNewBuffer)
			{
				Release(outBuffer);
				outBuffer = new ComputeBuffer(inCount, stride, ComputeBufferType.Structured);
			}
		}

		public static void CreateStructuredBuffer<T>(ref ComputeBuffer outBuffer, T[] inData)
		{
			CreateStructuredBuffer<T>(ref outBuffer, inData.Length);
			outBuffer.SetData(inData);
		}

		public static void Release(ComputeBuffer inBuffer)
		{
			inBuffer?.Release();
		}

		#endregion
	}
}
