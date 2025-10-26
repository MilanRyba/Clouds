using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NoiseGenerator))]
public class NoiseEditor : Editor
{
	private NoiseGenerator m_Generator;

	private void OnEnable()
	{
		m_Generator = (NoiseGenerator)target;
	}

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		if (GUILayout.Button("Generate"))
			m_Generator.UpdateNoise();

		if (GUILayout.Button("Save"))
			m_Generator.Save(m_Generator.ActiveTexture.name);

		GUILayout.Label(m_Generator.Slices[m_Generator.Slice]);
	}
}
