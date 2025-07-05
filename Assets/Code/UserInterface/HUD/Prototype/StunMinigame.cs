using System.Collections;
using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface.HUD
{
    public class StunMinigame : MonoBehaviour
    {
        private const float BaseScreenWidth = 1920f;
        
        private WillToLive _will;

        [Header("UI Elements")] 
        [SerializeField] private RectTransform _minigameRect;
        [SerializeField] private Image _indicatorBar;
        [SerializeField] private RectTransform _handle;
        
        [Header("Color")]
        [SerializeField] private Image _background;
        [SerializeField] private Color _pressedColor;
        [SerializeField] private Color _unpressedColor;
        [SerializeField] private Image _cap;
        [SerializeField] private Color _winColor;
        [SerializeField] private Color _loseColor;
        [SerializeField] private float _flashDuration;
        private Color _initialColor;
        private bool _assignedEvents;
        private bool _onTransition;
        private float _reScale;

        private void Awake()
        {
            _initialColor = _cap.color;
        }

        private void Update()
        {
            if (PlayerController.Singleton == null) return;

            PlayerController.Singleton.Dependencies.TryGetFeature(out _will);
            
            if (_will == null) return;

            if (!_assignedEvents)
            {
                _will.OnMinigameSucces += OnMinigameSuccess;
                _will.OnMinigameFailed += OnMinigameFailed;
                _assignedEvents = true;
            }
            
            _minigameRect.gameObject.SetActive(_will.OnMinigame);
            _indicatorBar.fillAmount = 1 - _will.SweetSpotSpan;
            
            UpdateHandlePosition();
            UpdateIndicatorColor();
        }

        private void UpdateHandlePosition()
        {
            _reScale = Screen.width / BaseScreenWidth;
            float radius = (_minigameRect.rect.width - _handle.rect.width) * _reScale /2;
            float angle = Mathf.PI * (.5f - 2 * _will.MinigameProgress);
            Vector3 relativePos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            _handle.position = _minigameRect.position + relativePos;
        }

        private void UpdateIndicatorColor()
        {
            _background.color = _will.CachedMinigameInput ? _pressedColor : _unpressedColor;
        }

        private void OnMinigameSuccess()
        {
            if(_onTransition) return;
            
            StopAllCoroutines();
            StartCoroutine(FlashSequence(_winColor));
        }

        private void OnMinigameFailed()
        {
            if(_onTransition) return;
            
            StopAllCoroutines();
            StartCoroutine(FlashSequence(_loseColor));
        }

        private IEnumerator FlashSequence(Color color)
        {
            _onTransition = true;
            _cap.color = color;
            yield return new WaitForSeconds(_flashDuration);
            _cap.color = _initialColor;
            _onTransition = false;
        }
    }
}