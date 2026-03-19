using UnityEngine;

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
	Transform m_Sphere;

	int NumVoxelsX => (int)(m_WorldExtents.x / m_VoxelSize);
	int NumVoxelsY => (int)(m_WorldExtents.y / m_VoxelSize);
	int NumVoxelsZ => (int)(m_WorldExtents.z / m_VoxelSize);
	int Volume => NumVoxelsX * NumVoxelsY * NumVoxelsZ;

	private void Awake()
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

					int idx = IdxFromVoxelCoords(x, y, z);
					m_Points[idx] = point;
				}
			}
		}
	}

	private void Update()
	{
		int idx = Random.Range(0, Volume);
		GameObject pointGO = m_Points[idx].gameObject;
		pointGO.SetActive(!pointGO.activeSelf);
	}

	private int IdxFromVoxelCoords(int inX, int inY, int inZ)
	{
		return inX + inY * NumVoxelsX + inZ * NumVoxelsY * NumVoxelsX;
	}

	private int IdxFromVoxelCoords(Vector3Int inVoxelCoords)
	{
		return IdxFromVoxelCoords(inVoxelCoords.x, inVoxelCoords.y, inVoxelCoords.z);
	}

	private int IdxFromWorldPosition(Vector3 inPosition)
	{
		Vector3 voxelGridOrigin = -(m_WorldExtents / 2);
		Vector3 localPos = inPosition - voxelGridOrigin;

		Vector3Int voxelPos = Vector3Int.FloorToInt(new Vector3(
			localPos.x / m_VoxelSize,
			localPos.y / m_VoxelSize,
			localPos.z / m_VoxelSize
		));

		if (OutOfBoundsVoxelCoords(voxelPos))
			return -1;

		return IdxFromVoxelCoords(voxelPos);
	}

	bool OutOfBoundsVoxelCoords(int inX, int inY, int inZ)
	{
		return inX < 0 || inX >= NumVoxelsX || inY < 0 || inY >= NumVoxelsY || inZ < 0 || inZ >= NumVoxelsZ;
	}

	bool OutOfBoundsVoxelCoords(Vector3Int inVoxelCoords)
	{
		return OutOfBoundsVoxelCoords(inVoxelCoords.x, inVoxelCoords.y, inVoxelCoords.z);
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(Vector3.zero, m_WorldExtents);

		if (Application.isPlaying)
		{
			Gizmos.color = Color.cyan;
			for (int i = 0; i < Volume; i++)
			{
				if (m_Points[i].gameObject.activeSelf == false)
					continue;

				int idx = IdxFromWorldPosition(m_Sphere.localPosition);
				if (idx != i)
					continue;

				Transform t = m_Points[i];
				Gizmos.DrawWireCube(t.localPosition, t.localScale);
			}
		}
	}
}
