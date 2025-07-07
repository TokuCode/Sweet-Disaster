using System;
using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using Code.Systems.MatchTime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface.HUD
{
    public class GameClock : PlayerHUDBase
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _timeField;
        [SerializeField] private Image _nextBomb;
        protected override void Update()
        {
            base.Update();
            if (!Assigned) return;
            
            UpdateTime();
            UpdateBombTime();
        }

        private void UpdateTime()
        {
            float secondsLeft = MatchTime.Instance.MatchTimer.Value;
            
            var span = TimeSpan.FromSeconds(secondsLeft);
            _timeField.text = span.ToString(@"mm\:ss");
        }

        private void UpdateBombTime()
        {
            float progress = Player.Dependencies.TryGetFeature(out Bomb bomb) ? bomb.BombReloadProgress : 0;
            _nextBomb.fillAmount = progress;
        }
    }
}