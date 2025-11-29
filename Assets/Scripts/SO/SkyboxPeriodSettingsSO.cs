using UnityEngine;

[CreateAssetMenu(fileName = "SkyboxPeriodSettings", menuName = "Weather/Skybox Period Settings", order = 1)]
public class SkyboxPeriodSettingsSO : ScriptableObject
{
    public Material skyboxMaterial;
    public Color skyTint = Color.white;
    public float exposure = 1f;
    public Color ambientLight = Color.white;
    public Color fogColor = Color.white;
    public float fogDensity = 0f;
}