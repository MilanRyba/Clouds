using UnityEngine;

[CreateAssetMenu(fileName = "Clouds ", menuName = "Clouds/Cloud Settings", order = 1)]
public class CloudSettings : ScriptableObject
{
	[Header("Weather")]

	[Range(0.0f, 1.0f)]
	public float GlobalDensity = 0.3f;

	[Range(0.0f, 1.0f)]
	public float GlobalCoverage = 0.5f;

	[Range(0.0f, 1.0f), Tooltip("Type of the cloud to render. 0 -> stratus, 0.5 -> stratocumulus, 1 -> cumulus")]
	public float CloudType = 0.65f;

	[Range(0.0f, 360.0f), Tooltip("Global wind direction")]
	public float WindDirection = 0.0f;

	[Range(0.01f, 5000.0f), Tooltip("Speed of the clouds")]
	public float CloudSpeed = 840.0f;

	[Range(0.0f, 250.0f), Tooltip("Pushes the tops of the clouds along the wind direction by this many units")]
	public float CloudTopOffset = 100.0f;


	[Header("Clouds")]

	[Range(0.0f, 120.0f), Tooltip("Scale of the base cloud shape")]
	public float BaseScale = 40.0f;

	[Range(0.0f, 120.0f), Tooltip("Scale of the cloud details")]
	public float DetailScale = 40.0f;

	[Range(0.0f, 0.01f), Tooltip("Controls how much clouds absorp light")]
	public float Absorption = 0.0042f;


	[Header("Phase")]

	[Range(-0.99f, 0.99f), Tooltip("Directional scattering bias. Values >1 make the light scatter forward and values <1 backward")]
	public float Eccentricity = 0.65f;

	[Range(0.0f, 5.0f)]
	public float Intensity = 0.95f;

	[Range(0.0f, 1.0f)]
	public float Spread = 1.0f;
}
