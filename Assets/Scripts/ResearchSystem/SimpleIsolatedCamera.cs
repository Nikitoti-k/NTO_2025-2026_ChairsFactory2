using UnityEngine;
using UnityEngine.Rendering;

public class SimpleIsolatedCamera : MonoBehaviour
{
    [SerializeField] private Camera isolatedCamera;
    [SerializeField] private RenderTexture renderTexture;

    // Настройки изолированного освещения
    [SerializeField] private Color ambientColor = Color.gray;
    [SerializeField] private float ambientIntensity = 1f;

    // Ссылка на WeatherManager для получения оригинальных настроек
    private WeatherManager weatherManager;

    void Start()
    {
        if (isolatedCamera == null)
            isolatedCamera = GetComponent<Camera>();

        if (renderTexture != null)
        {
            isolatedCamera.targetTexture = renderTexture;
        }

        weatherManager = FindObjectOfType<WeatherManager>();
    }

    void OnPreCull()
    {
        // Сохраняем текущие настройки освещения
        SaveLightingSettings();

        // Устанавливаем фиксированное освещение
        SetIsolatedLighting();
    }

    void OnPostRender()
    {
        // Восстанавливаем оригинальные настройки сцены
        RestoreLightingSettings();
    }

    private void SaveLightingSettings()
    {
        PlayerPrefs.SetFloat("SavedAmbientIntensity", RenderSettings.ambientIntensity);
        PlayerPrefs.SetString("SavedAmbientColor",
            ColorUtility.ToHtmlStringRGBA(RenderSettings.ambientLight));
    }

    private void SetIsolatedLighting()
    {
        // Устанавливаем фиксированный ambient
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColor * ambientIntensity;

        // Отключаем влияние глобальных настроек времени суток
        if (weatherManager != null)
        {
            // Сохраняем оригинальный gradient
            var originalGradient = RenderSettings.ambientLight;
            // Временно заменяем на наш фиксированный
            RenderSettings.ambientLight = ambientColor * ambientIntensity;
        }
    }

    private void RestoreLightingSettings()
    {
        // Восстанавливаем ambient из сохраненных значений
        if (PlayerPrefs.HasKey("SavedAmbientColor"))
        {
            Color savedColor;
            if (ColorUtility.TryParseHtmlString("#" + PlayerPrefs.GetString("SavedAmbientColor"), out savedColor))
            {
                RenderSettings.ambientLight = savedColor;
            }
        }

        RenderSettings.ambientIntensity = PlayerPrefs.GetFloat("SavedAmbientIntensity", 1f);
    }

    void OnDestroy()
    {
        if (isolatedCamera != null && isolatedCamera.targetTexture != null)
        {
            isolatedCamera.targetTexture.Release();
        }
    }
}