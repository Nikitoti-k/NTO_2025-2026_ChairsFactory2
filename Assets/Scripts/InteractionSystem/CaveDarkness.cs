using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(Collider))]
public class CaveDarkness : MonoBehaviour
{
    [Header("=== ПЕЩЕРА: ТЬМА ===")]
    [ColorUsage(true, true)] public Color caveAmbientLight = new Color(0.015f, 0.015f, 0.03f, 1f);
    public float caveFogDensity = 0.22f;
    [ColorUsage(true, true)] public Color caveFogColor = new Color(0.02f, 0.02f, 0.03f, 1f);
    public bool enableFogInCave = true;

    [Header("=== ФАКЕЛЫ ===")]
    public List<Light> caveTorches = new List<Light>();
    public float normalTorchIntensity = 1f;
    public float caveTorchIntensity = 6f;

    [Header("=== ПЕРЕХОДЫ ===")]
    public float transitionTime = 1.2f;
    public bool useSmoothTransitions = true;

    // ГЛОБАЛЬНОЕ СОСТОЯНИЕ
    private static int playersInCave = 0;
    public static bool IsInsideAnyCave => playersInCave > 0; // ← ЭТО ЧИТАЕТ WeatherManager!

    private static Color origAmbient;
    private static float origFogDensity;
    private static Color origFogColor;
    private static bool origFogEnabled;
    private static FogMode origFogMode;
    private static bool settingsSaved = false;

    private Coroutine transition;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
        if (caveTorches.Count == 0) FindTorchesInChildren();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInCave++;
        if (playersInCave == 1)
        {
            Debug.Log("Вход в пещеру → тьма и яркие факелы");
            EnterCave();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInCave = Mathf.Max(0, playersInCave - 1);
        if (playersInCave == 0)
        {
            Debug.Log("Выход из пещеры → нормальное освещение");
            ExitCave();
        }
    }

    private void EnterCave()
    {
        SaveOriginalSettings();

        // Усиливаем факелы сразу (это нормально — игроку сразу видеть дорогу)
        foreach (var t in caveTorches)
            if (t) { t.intensity = caveTorchIntensity; t.enabled = true; }

        if (useSmoothTransitions && transitionTime > 0.1f)
        {
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(SmoothEnterCave()); // ← ПЛАВНЫЙ ВХОД В ТЬМУ
        }
        else
        {
            ApplyCaveMode(); // мгновенно, если выключены переходы
        }

        Debug.Log("Вход в пещеру → начинается плавный переход в тьму");
    }

    private System.Collections.IEnumerator SmoothEnterCave()
    {
        // Мы начинаем с текущих (светлых) настроек, сохранённых в origAmbient и т.д.
        float t = 0f;
        while (t < transitionTime)
        {
            t += Time.deltaTime;
            float p = t / transitionTime;

            RenderSettings.ambientLight = Color.Lerp(origAmbient, caveAmbientLight, p);

            if (enableFogInCave)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogDensity = Mathf.Lerp(origFogDensity, caveFogDensity, p);
                RenderSettings.fogColor = Color.Lerp(origFogColor, caveFogColor, p);
            }

            // Факелы можно оставить яркими сразу, либо тоже плавно усиливать:
            // foreach (var torch in caveTorches)
            //     if (torch) torch.intensity = Mathf.Lerp(normalTorchIntensity, caveTorchIntensity, p);

            yield return null;
        }

        // Финальное состояние
        ApplyCaveMode();
    }

    private void ExitCave()
    {
        // Если плавный переход — запускаем от текущего (тёмного) к светлому
        if (useSmoothTransitions && transitionTime > 0.1f)
        {
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(SmoothExitToLight());
        }
        else
        {
            RestoreNormalMode();
        }
    }
    private System.Collections.IEnumerator SmoothEnterFromDark()
    {
        // Ничего не делаем — тьма уже применена мгновенно
        // Можно добавить лёгкое "затухание" факелов или эффекты
        yield return null;
    }


    private void ApplyCaveMode()
    {
        RenderSettings.ambientLight = caveAmbientLight;
        if (enableFogInCave)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = caveFogDensity;
            RenderSettings.fogColor = caveFogColor;
        }

        foreach (var t in caveTorches)
            if (t) { t.intensity = caveTorchIntensity; t.enabled = true; }
    }

    private void RestoreNormalMode()
    {
        RenderSettings.ambientLight = origAmbient;
        RenderSettings.fog = origFogEnabled;
        RenderSettings.fogMode = origFogMode;
        RenderSettings.fogDensity = origFogDensity;
        RenderSettings.fogColor = origFogColor;

        FindObjectsOfType<CaveDarkness>().ToList().ForEach(c =>
            c.caveTorches.ForEach(t => { if (t) t.intensity = c.normalTorchIntensity; }));
    }

    private void SaveOriginalSettings()
    {
        if (settingsSaved) return;
        origAmbient = RenderSettings.ambientLight;
        origFogDensity = RenderSettings.fogDensity;
        origFogColor = RenderSettings.fogColor;
        origFogEnabled = RenderSettings.fog;
        origFogMode = RenderSettings.fogMode;
        settingsSaved = true;
    }

    // Плавные переходы
    private System.Collections.IEnumerator SmoothEnter()
    {
        SaveOriginalSettings();
        float t = 0;
        while (t < transitionTime)
        {
            t += Time.deltaTime;
            float p = t / transitionTime;
            RenderSettings.ambientLight = Color.Lerp(origAmbient, caveAmbientLight, p);
            if (enableFogInCave)
            {
                RenderSettings.fogDensity = Mathf.Lerp(origFogDensity, caveFogDensity, p);
                RenderSettings.fogColor = Color.Lerp(origFogColor, caveFogColor, p);
            }
            yield return null;
        }
        ApplyCaveMode();
    }
    private System.Collections.IEnumerator SmoothExitToLight()
    {
        float t = 0;
        while (t < transitionTime)
        {
            t += Time.deltaTime;
            float p = t / transitionTime;

            RenderSettings.ambientLight = Color.Lerp(caveAmbientLight, origAmbient, p);
            RenderSettings.fogDensity = Mathf.Lerp(caveFogDensity, origFogDensity, p);
            RenderSettings.fogColor = Color.Lerp(caveFogColor, origFogColor, p);

            // Факелы плавно тускнеют
            foreach (var torch in caveTorches)
                if (torch) torch.intensity = Mathf.Lerp(caveTorchIntensity, normalTorchIntensity, p);

            yield return null;
        }

        RestoreNormalMode();
    }
    private System.Collections.IEnumerator SmoothExit()
    {
        float t = 0;
        while (t < transitionTime)
        {
            t += Time.deltaTime;
            float p = t / transitionTime;
            RenderSettings.ambientLight = Color.Lerp(caveAmbientLight, origAmbient, p);
            RenderSettings.fogDensity = Mathf.Lerp(caveFogDensity, origFogDensity, p);
            RenderSettings.fogColor = Color.Lerp(caveFogColor, origFogColor, p);
            yield return null;
        }
        RestoreNormalMode();
    }

    private void FindTorchesInChildren()
    {
        caveTorches.Clear();
        foreach (var l in GetComponentsInChildren<Light>())
            if (l.type == LightType.Point || l.type == LightType.Spot)
                caveTorches.Add(l);
    }

    // Дебаг
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsInsideAnyCave ? Color.red : Color.cyan;
        var col = GetComponent<Collider>();
        if (col) Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}