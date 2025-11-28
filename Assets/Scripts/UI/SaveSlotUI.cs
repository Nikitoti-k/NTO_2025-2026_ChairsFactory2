using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotUI : MonoBehaviour
{
   /* [SerializeField] RawImage previewImage;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI dateText;
    [SerializeField] Button loadButton;
    [SerializeField] Button deleteButton;

    public void Setup(SaveFile save, string slotName, Action onLoad, Action onDelete)
    {
        nameText.text = save.saveName;
        dateText.text = DateTime.Parse(save.saveTime).ToString("dd MMMM yyyy, HH:mm");

        if (!string.IsNullOrEmpty(save.previewImageBase64))
        {
            byte[] bytes = Convert.FromBase64String(save.previewImageBase64);
            Texture2D tex = new Texture2D(256, 144, TextureFormat.RGB24, false);
            tex.LoadImage(bytes);
            previewImage.texture = tex;
        }
        else
        {
            previewImage.texture = null;
            previewImage.color = Color.black;
        }

        loadButton.onClick.RemoveAllListeners();
        loadButton.onClick.AddListener(() => onLoad?.Invoke());

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() =>
        {
            onDelete?.Invoke();
            Destroy(gameObject);
        });
    }*/
}