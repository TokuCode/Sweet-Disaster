using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSlotUI : MonoBehaviour
{
    public Image colorIcon;
    public TMP_Text nameText;

    public void Setup(string name, Color color)
    {
        nameText.text = name;
        colorIcon.color = color;
    }
}