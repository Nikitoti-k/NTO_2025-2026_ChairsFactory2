using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System;

public class CassettePlayer : MonoBehaviour
{
    [Header("3D Components")]
    [SerializeField] private Transform cassetteSlot;
    //[SerializeField] private Collider insertTrigger;  

    [Header("Video Player")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Fullscreen UI")]
    [SerializeField] private GameObject fullscreenUI;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private RenderTexture videoRenderTexture;

    [Header("UI Controls")]
    [SerializeField] private Button playPauseButton;
    [SerializeField] private Button rewindButton;
    [SerializeField] private Button speedUpButton;
    [SerializeField] private Button speedDownButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Text timeText;
    [SerializeField] private Text speedText;

    [Header("Settings")]
    [SerializeField] private float rewindSeconds = 10f;

    private Cassette currentCassette;
    private float currentSpeed = 1.0f;
    private bool isPlaying = false;

    public static event Action OnCassete1Play;
    public static event Action OnCassete2Play;
    public static event Action OnCassete3Play;
    public static event Action OnCassete4Play;

    private void Start()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;
        videoPlayer.loopPointReached += OnVideoFinished;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;

        fullscreenUI.SetActive(false);
        videoDisplay.texture = videoRenderTexture;

        playPauseButton.onClick.AddListener(TogglePlayPause);
        rewindButton.onClick.AddListener(RewindBack);
        speedUpButton.onClick.AddListener(() => SetSpeed(2.0f));
        speedDownButton.onClick.AddListener(() => SetSpeed(0.5f));
        closeButton.onClick.AddListener(CloseUI);

        UpdateSpeedUI();
    }

    private void Update()
    {
        if (fullscreenUI.activeSelf && videoPlayer.isPlaying)
            UpdateTimeUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentCassette != null) return;

        Cassette cassette = other.GetComponent<Cassette>();
        if (cassette != null && !cassette.IsInserted())
        {
            InsertCassette(cassette);
        }
    }

    private void InsertCassette(Cassette cassette)
    {
        currentCassette = cassette;
        currentCassette.InsertIntoPlayer(cassetteSlot);

        fullscreenUI.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        videoPlayer.clip = cassette.Data.videoClip;
        videoPlayer.Prepare();
    }

    private void EjectCassette()
    {
        if (currentCassette == null) return;

        videoPlayer.Stop();
        currentCassette.EjectFromPlayer();
        
        isPlaying = false;
        int id = currentCassette.id;
        print(id);
        switch (id)
        {
            case 1:
                OnCassete1Play?.Invoke();
                break;
            case 2:
                OnCassete2Play?.Invoke();
                break;
            case 3:
                OnCassete3Play?.Invoke();
                break;
            case 4:
                OnCassete4Play?.Invoke();
                break;
        }

        currentCassette = null;
    }

    public void TogglePlayPause()
    {
        if (currentCassette == null) return;
        if (videoPlayer.isPlaying) Pause();
        else Play();
    }

    private void Play()
    {
        videoPlayer.playbackSpeed = currentSpeed;
        videoPlayer.Play();
        isPlaying = true;
        UpdatePlayPauseButtonText();
    }

    private void Pause()
    {
        videoPlayer.Pause();
        isPlaying = false;
        UpdatePlayPauseButtonText();
    }

    public void RewindBack()
    {
        if (currentCassette == null || !videoPlayer.canSetTime) return;
        double newTime = videoPlayer.time - rewindSeconds;
        videoPlayer.time = System.Math.Max(0, newTime);
    }

    public void SetSpeed(float speed)
    {
        currentSpeed = speed;
        if (videoPlayer.isPlaying) videoPlayer.playbackSpeed = currentSpeed;
        UpdateSpeedUI();
    }

    private void CloseUI()
    {
        fullscreenUI.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        EjectCassette();
    }

    private void UpdateTimeUI()
    {
        if (timeText != null)
            timeText.text = FormatTime(videoPlayer.time) + " / " + FormatTime(videoPlayer.length);
    }

    private void UpdateSpeedUI()
    {
        if (speedText != null)
            speedText.text = $"{currentSpeed:F1}x";
    }

    private void UpdatePlayPauseButtonText()
    {
        var txt = playPauseButton.GetComponentInChildren<Text>();
        if (txt != null) txt.text = isPlaying ? "Pause" : "Play";
    }

    private string FormatTime(double seconds)
    {
        int mins = (int)(seconds / 60);
        int secs = (int)(seconds % 60);
        return $"{mins:D2}:{secs:D2}";
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        Play();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        CloseUI();
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"Video error: {message}");
        CloseUI();
    }
}