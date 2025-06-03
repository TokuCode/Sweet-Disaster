using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface.LobbyUI
{
    public class PlayerSlotUI : MonoBehaviour
    {
        public TMP_Text nameText;
        public Image outlineColor;
        
        public void Setup(string playerName, Color color)
        {
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.text = playerName;
            outlineColor.color = color;
        }

        public void SetDefault()
        {
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.text = "+";
            outlineColor.color = Color.white;
        }
    }
}