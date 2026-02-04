using UnityEngine;

public class TorchFlicker : MonoBehaviour
{
    public Light torchLight;
    public float minIntensity = 3f;
    public float maxIntensity = 5f;
    public float flickerSpeed = 5f;

    void Update()
    {
        if (torchLight == null) return;
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, 0f);
        torchLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}