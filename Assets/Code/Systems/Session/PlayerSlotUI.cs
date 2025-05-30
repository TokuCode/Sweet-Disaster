using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Systems.Session
{
    public class PlayerSlotUI : MonoBehaviour
    {
        public TMP_Text nameText;
        public Image colorIcon;
        
        public void Setup(string name, Color color)
        {
            nameText.text = name;
            colorIcon.color = color;
        }
    }
}