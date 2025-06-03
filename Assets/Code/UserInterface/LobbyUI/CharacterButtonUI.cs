using System;
using Code.Networking.Session;
using Unity.Services.Multiplayer;
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

        private void Awake()
        {
            DefaultColor = outlineColorImage.color; 
            SelectButton = GetComponent<Button>();
        } 

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (SessionManager.Instance.ActiveSession == null) return;
            if (!SelectButton.interactable) return;
            
            string colorName = SessionManager.Instance.ActiveSession.CurrentPlayer.Properties.
                TryGetValue(SessionManager.Instance.PlayerKeys[PlayerPropertyKeys.PlayerColor], out var colorProp)
                ? colorProp.Value : String.Empty;

            if (colorName == String.Empty) return;
            var colorMap = SessionManager.Instance.PlayerColors;
            colorMap.TryGetValue(colorName, out var color);
            
            outlineColorImage.color = color;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (SessionManager.Instance.ActiveSession == null) return;
            if (!SelectButton.interactable) return;
            
            outlineColorImage.color = DefaultColor;
        }
    }
}