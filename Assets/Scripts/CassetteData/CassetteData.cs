using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "NewCassette", menuName = "Cassette System/Cassette Data")]
public class CassetteData : ScriptableObject
{
    public int id;
    public string displayName;
    public Sprite coverSprite;
    [TextArea] public string description;
    public VideoClip videoClip;
}