using UnityEngine;

public interface IDayTime
{
    float TimeProgress { get; set; }
    void UpdateLighting(float progress);
}

[ExecuteInEditMode]
public class DayTime : MonoBehaviour, IDayTime
{
    [SerializeField] Gradient directLightGradient;
    [SerializeField] Gradient ambientLightGradient;
    [SerializeField] Light dirLight;
    [SerializeField, Range(0f, 1f)] float timeProgress;

    private Vector3 defaultAngles;

    public float TimeProgress
    {
        get => timeProgress;
        set
        {
            timeProgress = Mathf.Clamp01(value);
            UpdateLighting(timeProgress);
        }
    }

    private void Start()
    {
        if (dirLight != null)
            defaultAngles = dirLight.transform.localEulerAngles;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;
        UpdateLighting(timeProgress);
    }

    public void UpdateLighting(float progress)
    {
        try
        {
            if (dirLight == null) return;
            dirLight.color = directLightGradient.Evaluate(progress);
            RenderSettings.ambientLight = ambientLightGradient.Evaluate(progress);
            dirLight.transform.localEulerAngles = new Vector3(360f * progress - 90f, defaultAngles.y, defaultAngles.z);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[DayTime] Lighting update failed: {e.Message}");
        }
    }
}