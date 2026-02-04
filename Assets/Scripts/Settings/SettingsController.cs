using UnityEngine;
using UnityEngine.Audio;

public class SettingsController : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer mixer;

    [SerializeField] private string masterVolParam = "MasterVol";
    [SerializeField] private string musicVolParam = "MusicVol";
    [SerializeField] private string sfxVolParam = "SFXVol";

    private float sensitivity = 1f;

    public void SetSensitivity(float value) => sensitivity = value;

    public void SetMasterVolume(float value) => SetMixerVol(masterVolParam, value);
    public void SetMusicVolume(float value) => SetMixerVol(musicVolParam, value);
    public void SetSFXVolume(float value) => SetMixerVol(sfxVolParam, value);

    private void SetMixerVol(string paramName, float linear)
    {
        float dB = linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
        mixer.SetFloat(paramName, dB);
    }
}