using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Systems.Session
{
    public class PopMessages : MonoBehaviour
    {
        [SerializeField] UIElements uiElements;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] Button okBtn;

        public void PopMessage(string message, bool withOk)
        {
            if (uiElements == null) return;
            okBtn.gameObject.SetActive(withOk);
            messageText.text = message;
            uiElements.PopUp(gameObject);
        }
    }
}