using Code.Networking.Session;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Code.UserInterface.LobbyUI
{
    public class CharacterButtonUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string characterName;

        public Color DefaultColor { get; private set; }
        public Image outlineColorImage;
        public Button SelectButton { get; private set; }
        
        private SessionManager _sessionManager;

        private void Awake()
        {
            _sessionManager = SessionManager.Instance;
            DefaultColor = outlineColorImage.color; 
            SelectButton = GetComponent<Button>();
        } 

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_sessionManager.ActiveSession == null) return;
            if (!SelectButton.interactable) return;
            
            outlineColorImage.color = _sessionManager.playerInfo.GetColor(_sessionManager.ActiveSession.CurrentPlayer);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_sessionManager.ActiveSession == null) return;
            if (!SelectButton.interactable) return;
            
            outlineColorImage.color = DefaultColor;
        }
    }
}