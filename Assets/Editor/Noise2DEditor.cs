using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NoiseGenerator2D))]
public class Noise2DEditor : Editor
{
	private NoiseGenerator2D m_Generator;

	private void OnEnable()
	{
		m_Generator = (NoiseGenerator2D)target;
	}

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		if (GUILayout.Button("Generate"))
		{
			m_Generator.UpdateNoise();
		}

		int res = m_Generator.NoiseTexture.width;

		GUILayout.BeginVertical();

		GUIStyle style = new GUIStyle(GUI.skin.box);
		style.margin = new RectOffset(0, 0, 0, 0);
		style.padding = new RectOffset(0, 0, 0, 0);

		GUILayout.BeginHorizontal(style);
		GUILayout.Label(m_Generator.NoiseTexture, GUILayout.Width(res), GUILayout.Height(res));
		GUILayout.Label(m_Generator.NoiseTexture, GUILayout.Width(res), GUILayout.Height(res));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUILayout.Label(m_Generator.NoiseTexture, GUILayout.Width(res), GUILayout.Height(res));
		GUILayout.Label(m_Generator.NoiseTexture, GUILayout.Width(res), GUILayout.Height(res));
		GUILayout.EndHorizontal();

		GUILayout.EndVertical();
	}
}
