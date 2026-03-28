using UnityEngine;
using UnityEngine.Assertions;

public class VoxelClouds : MonoBehaviour
{
	[SerializeField]
	Transform m_PointPrefab;

	[SerializeField]
	Vector3Int m_WorldExtents = new Vector3Int(20, 10, 20);

	[SerializeField, Min(0.2f)]
	float m_VoxelSize = 1.0f;

	Transform[] m_Points;

	[SerializeField]
	Transform m_Sphere1, m_Sphere2;

	int NumVoxelsX => (int)(m_WorldExtents.x / m_VoxelSize);
	int NumVoxelsY => (int)(m_WorldExtents.y / m_VoxelSize);
	int NumVoxelsZ => (int)(m_WorldExtents.z / m_VoxelSize);
	int Volume => NumVoxelsX * NumVoxelsY * NumVoxelsZ;

	Vector3 VoxelGridOrigin => -(m_WorldExtents / 2);

	int BitsPerByte => sizeof(byte) * 8;
	int NumBytes => (Volume + BitsPerByte - 1) / BitsPerByte;

	byte[] m_Activation;
	byte[] m_Clouds;
	byte[] m_Humidity;

	byte[] m_Activation2;
	byte[] m_Clouds2;
	byte[] m_Humidity2;

	byte[] m_WriteableActivation;
	byte[] m_WriteableClouds;
	byte[] m_WriteableHumidity;

	byte[] m_ReadableActivation;
	byte[] m_ReadableClouds;
	byte[] m_ReadableHumidity;

	float[] m_ProbabilityExtinction;
	float[] m_ProbabilityGeneration;

	private void Awake()
	{
		InitializeStateVariables();
		InitializeProbabilities();
		InitializeVisualization();
	}

	private void InitializeStateVariables()
	{
		m_Activation = new byte[NumBytes];
		m_Clouds = new byte[NumBytes];
		m_Humidity = new byte[NumBytes];

		m_Activation2 = new byte[NumBytes];
		m_Clouds2 = new byte[NumBytes];
		m_Humidity2 = new byte[NumBytes];

		for (int i = 0; i < NumBytes; i++)
		{
			m_Activation[i] = 0;
			m_Clouds[i] = 0;
			m_Humidity[i] = 255;

			m_Activation2[i] = 0;
			m_Clouds2[i] = 0;
			m_Humidity2[i] = 0;
		}

		m_WriteableActivation = m_Activation;
		m_WriteableClouds = m_Clouds;
		m_WriteableHumidity = m_Humidity;

		m_ReadableActivation = m_Activation2;
		m_ReadableClouds = m_Clouds2;
		m_ReadableHumidity = m_Humidity2;
	}

	private void InitializeProbabilities()
	{
		m_ProbabilityExtinction = new float[Volume];
		m_ProbabilityGeneration = new float[Volume];

		Vector3 elCenter = m_Sphere1.position;
		Vector3 elSize = m_Sphere2.localScale;

		for (int z = 0; z < NumVoxelsZ; z++)
		{
			for (int y = 0; y < NumVoxelsY; y++)
			{
				for (int x = 0; x < NumVoxelsX; x++)
				{
					int idx = IdxFromVoxelCoords(x, y, z);
					m_ProbabilityExtinction[idx] = 0.1f;
					m_ProbabilityGeneration[idx] = 0.0001f;
				}
			}
		}
	}

	private void SwapBuffers()
	{
		byte[] wAct = m_WriteableActivation;
		byte[] wCld = m_WriteableClouds;
		byte[] wHum = m_WriteableHumidity;

		m_WriteableActivation = m_ReadableActivation;
		m_WriteableClouds = m_ReadableClouds;
		m_WriteableHumidity = m_ReadableHumidity;

		m_ReadableActivation = wAct;
		m_ReadableClouds = wCld;
		m_ReadableHumidity = wHum;
	}

	private void InitializeVisualization()
	{
		m_Points = new Transform[Volume];

		Vector3 scale = Vector3.one * m_VoxelSize;
		Vector3 position = Vector3.zero;
		for (int z = 0; z < NumVoxelsZ; z++)
		{
			for (int y = 0; y < NumVoxelsY; y++)
			{
				for (int x = 0; x < NumVoxelsX; x++)
				{
					Transform point = Instantiate(m_PointPrefab);
					position.x = m_VoxelSize * x + (m_VoxelSize / 2.0f) - (m_WorldExtents.x / 2.0f);
					position.y = m_VoxelSize * y + (m_VoxelSize / 2.0f) - (m_WorldExtents.y / 2.0f);
					position.z = m_VoxelSize * z + (m_VoxelSize / 2.0f) - (m_WorldExtents.z / 2.0f);
					point.localPosition = position;
					point.localScale = scale;

					point.SetParent(transform, false);
					point.gameObject.SetActive(false);

					int idx = IdxFromVoxelCoords(x, y, z);
					m_Points[idx] = point;
				}
			}
		}
	}

	private void Update()
	{
		if (Time.frameCount % 100 == 0)
		{
			SwapBuffers();
		
			for (int z = 0; z < NumVoxelsZ; z++)
			{
				for (int y = 0; y < NumVoxelsY; y++)
				{
					for (int x = 0; x < NumVoxelsX; x++)
					{
						int idx = IdxFromVoxelCoords(x, y, z);
		
						bool hum = GetBit(idx, m_ReadableHumidity);
						bool cld = GetBit(idx, m_ReadableClouds);
						bool act = GetBit(idx, m_ReadableActivation);
		
						// TODO: Change parameters in the transition functions to an index
						TransitionHumidity(x, y, z);
						TransitionClouds(x, y, z);
						TransitionActivation(x, y, z);
		
						// ExtinctionClouds(x, y, z);
						GenerateActivation(x, y, z);
						GenerateHumidity(x, y, z);
		
						bool hum1 = GetBit(idx, m_WriteableHumidity);
						bool cld1 = GetBit(idx, m_WriteableClouds);
						bool act1 = GetBit(idx, m_WriteableActivation);
		
						m_Points[idx].gameObject.SetActive(cld1);
					}
				}
			}
		}
	}

	#region TransitionRules

	void TransitionHumidity(int inX, int inY, int inZ)
	{
		int idx = IdxFromVoxelCoords(inX, inY, inZ);

		bool hum = GetBit(idx, m_ReadableHumidity);
		bool act = GetBit(idx, m_ReadableActivation);

		AssignBit(idx, m_WriteableHumidity, hum && !act);
	}

	void TransitionClouds(int inX, int inY, int inZ)
	{
		int idx = IdxFromVoxelCoords(inX, inY, inZ);

		bool cld = GetBit(idx, m_ReadableClouds);
		bool act = GetBit(idx, m_ReadableActivation);

		AssignBit(idx, m_WriteableClouds, cld || act);
	}

	void TransitionActivation(int inX, int inY, int inZ)
	{
		int idx = IdxFromVoxelCoords(inX, inY, inZ);

		bool act = GetBit(idx, m_ReadableActivation);
		bool hum = GetBit(idx, m_ReadableHumidity);
		bool fAct = ActivationFunction(inX, inY, inZ);

		AssignBit(idx, m_WriteableActivation, !act && hum && fAct);
	}

	private bool ActivationFunction(int inX, int inY, int inZ)
	{
		int idx = IdxFromVoxelCoords(inX + 1, inY, inZ);
		if (idx != -1 && GetBit(idx, m_ReadableActivation))
			return true;

		idx = IdxFromVoxelCoords(inX, inY + 1, inZ);
		if (idx != -1 && GetBit(idx, m_ReadableActivation))
			return true;

		idx = IdxFromVoxelCoords(inX, inY, inZ + 1);
		if (idx != -1 && GetBit(idx, m_ReadableActivation))
			return true;


		idx = IdxFromVoxelCoords(inX - 1, inY, inZ);
		if (idx != -1 && GetBit(idx, m_ReadableActivation))
			return true;

		idx = IdxFromVoxelCoords(inX, inY - 1, inZ);
		if (idx != -1 && GetBit(idx, m_ReadableActivation))
			return true;

		idx = IdxFromVoxelCoords(inX, inY, inZ - 1);
		if (idx != -1 && GetBit(idx, m_ReadableActivation))
			return true;


		idx = IdxFromVoxelCoords(inX - 2, inY, inZ);
		if (idx != -1 && GetBit(idx, m_ReadableActivation))
			return true;

		idx = IdxFromVoxelCoords(inX + 2, inY, inZ);
		if (idx != -1 && GetBit(idx, m_ReadableActivation))
			return true;


		idx = IdxFromVoxelCoords(inX, inY, inZ - 2);
		if (idx != -1 && GetBit(idx, m_ReadableActivation))
			return true;

		idx = IdxFromVoxelCoords(inX, inY, inZ + 2);
		if (idx != -1 && GetBit(idx, m_ReadableActivation))
			return true;


		idx = IdxFromVoxelCoords(inX, inY - 2, inZ);
		if (idx != -1 && GetBit(idx, m_ReadableActivation))
			return true;

		return false;
	}

	// Based on some probability, transition cld from 1 to 0
	private void ExtinctionClouds(int inX, int inY, int inZ)
	{
		int idx = IdxFromVoxelCoords(inX, inY, inZ);

		if (!GetBit(idx, m_ReadableClouds))
			return;

		if (Random.Range(0.0f, 1.0f) < m_ProbabilityExtinction[idx])
			ClearBit(idx, m_WriteableClouds);
	}

	private void GenerateActivation(int inX, int inY, int inZ)
	{
		int idx = IdxFromVoxelCoords(inX, inY, inZ);

		if (GetBit(idx, m_ReadableActivation))
			return;

		if (Random.Range(0.0f, 1.0f) < m_ProbabilityGeneration[idx])
			SetBit(idx, m_WriteableActivation);
	}

	private void GenerateHumidity(int inX, int inY, int inZ)
	{
		int idx = IdxFromVoxelCoords(inX, inY, inZ);

		if (GetBit(idx, m_ReadableHumidity))
			return;

		if (Random.Range(0.0f, 1.0f) < m_ProbabilityGeneration[idx])
			SetBit(idx, m_WriteableHumidity);
	}

	#endregion

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(Vector3.zero, m_WorldExtents);

		// if (Application.isPlaying)
		// {
		// 	Gizmos.color = Color.cyan;
		// 	for (int i = 0; i < Volume; i++)
		// 	{
		// 		if (m_Points[i].gameObject.activeSelf == false)
		// 			continue;
		// 
		// 		Transform t = m_Points[i];
		// 		Gizmos.DrawWireCube(t.localPosition, t.localScale);
		// 	}
		// }
	}

	#region CoordinateConversions

	// +----------------------+    +-------------------+    +-------+
	// | World Space Position | -> | Voxel Coordinates | -> | Index |
	// +----------------------+    +-------------------+    +-------+

	// Get voxel index from voxel coordinates. Returns -1 if coords are out of bounds.
	private int IdxFromVoxelCoords(int inX, int inY, int inZ)
	{
		if (OutOfBoundsVoxelCoords(inX, inY, inZ))
			return -1;

		return inX + inY * NumVoxelsX + inZ * NumVoxelsY * NumVoxelsX;
	}

	// Get voxel index from voxel coordinates. Returns -1 if coords are out of bounds.
	private int IdxFromVoxelCoords(Vector3Int inVoxelCoords)
	{
		return IdxFromVoxelCoords(inVoxelCoords.x, inVoxelCoords.y, inVoxelCoords.z);
	}

	// Get voxel coordinates from world space position.
	private Vector3Int VoxelCoordsFromWorldPosition(Vector3 inPosition)
	{
		Vector3 localPos = inPosition - VoxelGridOrigin;

		return Vector3Int.FloorToInt(new Vector3(localPos.x / m_VoxelSize, localPos.y / m_VoxelSize, localPos.z / m_VoxelSize));
	}

	// Get voxel index from world space position. Returns -1 if inPosition lies outside the voxel space.
	private int IdxFromWorldPosition(Vector3 inPosition)
	{
		Vector3Int voxelCoords = VoxelCoordsFromWorldPosition(inPosition);
		return IdxFromVoxelCoords(voxelCoords);
	}

	// +----------------------+    +-------------------+    +-------+
	// | World Space Position | <- | Voxel Coordinates | <- | Index |
	// +----------------------+    +-------------------+    +-------+

	// Get world space position of voxel center
	private Vector3 WorldPositionFromVoxelCoords(int inX, int inY, int inZ)
	{
		// Scale voxel coordinates
		Vector3 position = new Vector3(inX, inY, inZ) * m_VoxelSize;

		// Apply offset so that the position is in voxel center
		float offset = 0.5f * m_VoxelSize;
		position += Vector3.one * offset;

		// Translate the position relative to the grid origin
		position += VoxelGridOrigin;

		return position;
	}

	// Get world space position of voxel center
	private Vector3 WorldPositionFromVoxelCoords(Vector3Int inVoxelCoords)
	{
		return WorldPositionFromVoxelCoords(inVoxelCoords.x, inVoxelCoords.y, inVoxelCoords.z);
	}

	private Vector3Int VoxelCoordsFromIdx(int inIdx)
	{
		Vector3Int voxelCoords = new Vector3Int();
		voxelCoords.z = inIdx / (NumVoxelsX * NumVoxelsY);
		inIdx -= (voxelCoords.z * NumVoxelsX * NumVoxelsY);
		voxelCoords.y = inIdx / NumVoxelsX;
		voxelCoords.x = inIdx % NumVoxelsX;
		return voxelCoords;
	}

	// Get world space position of voxel center
	private Vector3 WorldPositionFromIdx(int inIdx)
	{
		Vector3Int voxelCoords = VoxelCoordsFromIdx(inIdx);
		return WorldPositionFromVoxelCoords(voxelCoords);
	}

	bool OutOfBoundsVoxelCoords(int inX, int inY, int inZ)
	{
		return inX < 0 || inX >= NumVoxelsX || inY < 0 || inY >= NumVoxelsY || inZ < 0 || inZ >= NumVoxelsZ;
	}

	bool OutOfBoundsVoxelCoords(Vector3Int inVoxelCoords)
	{
		return OutOfBoundsVoxelCoords(inVoxelCoords.x, inVoxelCoords.y, inVoxelCoords.z);
	}

	#endregion

	#region Bits

	// Get index of a byte in bit field
	private int ByteIndexOfBit(int inBit) => inBit / BitsPerByte;

	// Get bit offset in byte
	private int IndexOfBitInByte(int inBit) => inBit % BitsPerByte;

	// Get byte where bit at inBit'th location is set to 1. All other bits are 0.
	private byte MakeBitmaskForByte(int inBit) => (byte)(1 << IndexOfBitInByte(inBit));

	// Set bit at inBit'th location to 0 or 1 based on inValue
	private void AssignBit(int inBit, byte[] inArray, bool inValue)
	{
		if (inValue)
			SetBit(inBit, inArray);
		else
			ClearBit(inBit, inArray);
	}

	// Set bit at inBit'th location to 1
	private void SetBit(int inBit, byte[] inArray)
	{
		Assert.IsTrue(inBit >= 0 && inBit < Volume);
		inArray[ByteIndexOfBit(inBit)] |= MakeBitmaskForByte(inBit);
	}

	// Set bit at inBit'th location to 0
	private void ClearBit(int inBit, byte[] inArray)
	{
		Assert.IsTrue(inBit >= 0 && inBit < Volume);
		inArray[ByteIndexOfBit(inBit)] &= (byte)~MakeBitmaskForByte(inBit);
	}

	// Get bit at inBit'th location
	private bool GetBit(int inBit, byte[] inArray)
	{
		Assert.IsTrue(inBit >= 0 && inBit < Volume);
		return (inArray[ByteIndexOfBit(inBit)] & MakeBitmaskForByte(inBit)) != 0;
	}

	#endregion
}
