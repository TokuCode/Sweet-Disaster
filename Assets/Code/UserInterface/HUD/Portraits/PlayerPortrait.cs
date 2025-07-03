using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface.HUD
{
    public class PlayerPortrait : MonoBehaviour
    {
        private PlayerPublicInfo _playerInfo;
        private bool _started;

        [Header("Main Portrait UI Elements")]
        [SerializeField] private Image[] _borders;
        [SerializeField] private TextMeshProUGUI _playerName;
        [SerializeField] private Image _playerImage;
        
        [Header("Main Portrait Overlay Elements")]
        [SerializeField] private Image _stunIcon;
        [SerializeField] private float _stunIconAngleSpeed;
        [SerializeField] private TextMeshProUGUI _defeatedText;
        [SerializeField] private TextMeshProUGUI _timeToRespawn;
        
        [Header("Stocks and Life")]
        [SerializeField] private TextMeshProUGUI _stocksCounter;
        [SerializeField] private TextMeshProUGUI _damageTaken;

        [Header("Colors Danger Zone")] 
        [SerializeField] private Color _safe;
        [SerializeField] private Color _warning;
        [SerializeField] private Color _danger;

        public void CachePlayerInfo(PlayerPublicInfo playerInfo)
        { 
            _playerInfo = playerInfo;  
            _started = true;
            SetMainPortrait();
        }

        private void Update()
        {
            if(!_started) return;
            
            UpdatePortraitOverlay();
            UpdateTimeToRespawn();
            UpdateStocksCounter();
            UpdateDamageTaken();
        }

        private void SetMainPortrait()
        {
            foreach (var border in _borders)
            {
                border.color = _playerInfo.playerColor;
            }
            _playerName.text = _playerInfo.playerName;
            _playerImage.sprite = _playerInfo.playerIcon;
        }

        private void UpdatePortraitOverlay()
        {
            bool stunIcon = false;
            bool defeatedIcon = false;
            bool timeToRespawn = false;
            bool isStunned = _playerInfo.player.Dependencies.TryGetFeature(out Health health) && health.IsStunned;

            if (_playerInfo.player.defeated.Value)
            {
                defeatedIcon = true;
            }
            
            else if (_playerInfo.player.outOfBattle.Value)
            {
                timeToRespawn = true;
            }
            
            else if (isStunned)
            {
                stunIcon = true;
            }
            
            if(_stunIcon.gameObject.activeSelf != stunIcon) _stunIcon.gameObject.SetActive(stunIcon);
            if(_defeatedText.gameObject.activeSelf != defeatedIcon) _defeatedText.gameObject.SetActive(defeatedIcon);
            if(_timeToRespawn.gameObject.activeSelf != timeToRespawn) _timeToRespawn.gameObject.SetActive(timeToRespawn);

            if (!_stunIcon.gameObject.activeSelf)
            {
                _stunIcon.transform.rotation = Quaternion.identity;
            }
            else
            {
                _stunIcon.transform.rotation *= Quaternion.Euler(0, 0, _stunIconAngleSpeed * Time.deltaTime);
            }
        }

        private void UpdateTimeToRespawn()
        {
            if(!_timeToRespawn.gameObject.activeSelf) return;
            
            float timeLeftToRespawn = _playerInfo.player.Dependencies.TryGetFeature(out LoseReporterPadded reporter) ? reporter.TimeToRespawn : 0;
            _timeToRespawn.text = $"{Mathf.Ceil(timeLeftToRespawn)}";
        }

        private void UpdateStocksCounter()
        {
            int stocks = _playerInfo.player.Dependencies.TryGetFeature(out LoseReporterPadded reporter)
                ? reporter.StockCount
                : 0;
            
            _stocksCounter.text = $"x{stocks}";
        }

        private void UpdateDamageTaken()
        {
            if(!_playerInfo.player.Dependencies.TryGetFeature(out Health health)) return;

            float damage = health.HealthAmount;
            float ratio = damage/health.BaseHealth;
            
            _damageTaken.text = $"{damage:N0}%";
            _damageTaken.color = ratio switch
            {
                <= 1 => _safe,
                > 1 and <= 2 => _warning,
                > 2 => _danger,
                _ => Color.white
            };
        }
    }
}