using UnityEngine;
using System.Collections;
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

    [Header("=== СТРАШНЫЕ ЗВУКИ В ПЕЩЕРЕ ===")]
    [SerializeField] private AudioClip[] caveScaryClips;
    [SerializeField] private float minScaryInterval = 18f;
    [SerializeField] private float maxScaryInterval = 45f;
    [SerializeField, Range(0.8f, 1.3f)] private float scaryPitchMin = 0.9f;
    [SerializeField, Range(0.8f, 1.3f)] private float scaryPitchMax = 1.1f;

    // Глобальное состояние
    private static int playersInCave = 0;
    public static bool IsInsideAnyCave => playersInCave > 0;

    private static Color origAmbient;
    private static float origFogDensity;
    private static Color origFogColor;
    private static bool origFogEnabled;
    private static FogMode origFogMode;
    private static bool settingsSaved = false;

    private Coroutine transition;
    private Coroutine scarySoundRoutine;

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
            EnterCave();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playersInCave = Mathf.Max(0, playersInCave - 1);
        if (playersInCave == 0)
            ExitCave();
    }

    private void EnterCave()
    {
        SaveOriginalSettings();

        // Факелы сразу яркие — чтобы игрок не ослеп
        foreach (var t in caveTorches)
            if (t) { t.intensity = caveTorchIntensity; t.enabled = true; }

        if (useSmoothTransitions && transitionTime > 0.1f)
        {
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(SmoothEnterCave());
        }
        else
        {
            ApplyCaveMode();
            StartScarySounds();
        }
    }

    private void ExitCave()
    {
        if (useSmoothTransitions && transitionTime > 0.1f)
        {
            if (transition != null) StopCoroutine(transition);
            transition = StartCoroutine(SmoothExitToLight());
        }
        else
        {
            RestoreNormalMode();
            StopScarySounds();
        }
    }

    private IEnumerator SmoothEnterCave()
    {
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

            yield return null;
        }

        ApplyCaveMode();
        StartScarySounds();
    }

    private IEnumerator SmoothExitToLight()
    {
        float t = 0f;
        while (t < transitionTime)
        {
            t += Time.deltaTime;
            float p = t / transitionTime;

            RenderSettings.ambientLight = Color.Lerp(caveAmbientLight, origAmbient, p);
            RenderSettings.fogDensity = Mathf.Lerp(caveFogDensity, origFogDensity, p);
            RenderSettings.fogColor = Color.Lerp(caveFogColor, origFogColor, p);

            foreach (var torch in caveTorches)
                if (torch) torch.intensity = Mathf.Lerp(caveTorchIntensity, normalTorchIntensity, p);

            yield return null;
        }

        RestoreNormalMode();
        StopScarySounds();
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
    }

    private void RestoreNormalMode()
    {
        RenderSettings.ambientLight = origAmbient;
        RenderSettings.fog = origFogEnabled;
        RenderSettings.fogMode = origFogMode;
        RenderSettings.fogDensity = origFogDensity;
        RenderSettings.fogColor = origFogColor;

        // Все факелы во всех пещерах возвращаем к нормальной яркости
        foreach (var cave in FindObjectsOfType<CaveDarkness>())
            foreach (var torch in cave.caveTorches)
                if (torch) torch.intensity = cave.normalTorchIntensity;
    }

    private void StartScarySounds()
    {
        if (scarySoundRoutine != null) StopCoroutine(scarySoundRoutine);
        if (caveScaryClips.Length > 0)
            scarySoundRoutine = StartCoroutine(PlayCaveScarySoundsRoutine());
    }

    private void StopScarySounds()
    {
        if (scarySoundRoutine != null)
        {
            StopCoroutine(scarySoundRoutine);
            scarySoundRoutine = null;
        }
    }

    private IEnumerator PlayCaveScarySoundsRoutine()
    {
        yield return new WaitForSeconds(Random.Range(8f, 15f));

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("CaveDarkness: игрок с тегом 'Player' не найден — страшные звуки отключены");
            yield break;
        }

        while (playersInCave > 0)
        {
            if (caveScaryClips.Length > 0 && AudioManager.Instance != null)
            {
                AudioClip clip = caveScaryClips[Random.Range(0, caveScaryClips.Length)];

                // ГРОМКОСТЬ = 1.0 → ПОЛНОСТЬЮ ОТ AUDIO MANAGER!
                AudioManager.Instance.PlaySFX(
                    clip: clip,
                    volumeMultiplier: 1f,                                            // ← ЧИСТО! Никаких 0.7f!
                    pitch: Random.Range(scaryPitchMin, scaryPitchMax),
                    position: player.transform.position
                );
            }

            yield return new WaitForSeconds(Random.Range(minScaryInterval, maxScaryInterval));
        }
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

    private void FindTorchesInChildren()
    {
        caveTorches.Clear();
        foreach (var l in GetComponentsInChildren<Light>())
            if (l.type == LightType.Point || l.type == LightType.Spot)
                caveTorches.Add(l);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsInsideAnyCave ? Color.red : Color.cyan;
        var col = GetComponent<Collider>();
        if (col) Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}