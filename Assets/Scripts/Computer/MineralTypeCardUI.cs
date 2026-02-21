using UnityEngine;
using TMPro;
using System.Text;
using System.Collections.Generic;

public class MineralTypeCardUI : MonoBehaviour
{
    public List<MineralTypeData> cards;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bulletText;

    private int index;

    private void Start()
    {
        Refresh();
    }

    public void Next()
    {
        index = (index + 1) % cards.Count;
        Refresh();
    }

    public void Prev()
    {
        index--;
        if (index < 0) index = cards.Count - 1;
        Refresh();
    }

    private void Refresh()
    {
        var data = cards[index];
        titleText.text = data.title;

        StringBuilder sb = new StringBuilder();
        foreach (var line in data.description)
            sb.AppendLine("• " + line);

        bulletText.text = sb.ToString();
    }
}
