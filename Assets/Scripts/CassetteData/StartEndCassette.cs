using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class StartEndCassette : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private GameObject panelUI;
    [SerializeField] private RawImage videoDisplay;
    [SerializeField] private RenderTexture videoRenderTexture;
    [SerializeField] private VideoClip startVideo;
    [SerializeField] private VideoClip endVideo;
    [SerializeField] private string manuScene;
    //public string nextSceneName = "GameScene";

    void Start()
    {
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoRenderTexture;
        videoDisplay.texture = videoRenderTexture;
        videoPlayer.clip = startVideo;
        panelUI.SetActive(true);
        videoPlayer.Play();
        videoPlayer.loopPointReached += StartGame;
    }

    public void StartEndVideo()
    {
        videoPlayer.clip = endVideo;
        panelUI.SetActive(true);
        videoPlayer.Play();
        videoPlayer.loopPointReached += EndGame;
    }
    private void StartGame(VideoPlayer vp)
    {
        panelUI.SetActive(false);
        vp.Stop();
        //SceneManager.LoadScene(nextSceneName);
    }

    private void EndGame(VideoPlayer vp)
    {
        vp.Stop();
        SceneManager.LoadScene(manuScene);
    }
}
