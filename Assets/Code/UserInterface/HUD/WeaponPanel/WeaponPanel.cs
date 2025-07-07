using System;
using Code.Gameplay.Character;
using Code.Gameplay.Character.Features;
using Code.Systems.Input;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.UserInterface.HUD
{
    public class WeaponPanel : PlayerHUDBase
    {
        [Header("Active Panel Selection")] 
        [SerializeField] private GameObject _bombPanel;
        [SerializeField] private GameObject _gunPanel;
        [SerializeField] private GameObject _shieldPanel;
        [SerializeField] private float _maxIdleActiveTime;
        private float _lastActiveTime;

        [Header("Gun Panel")] 
        [SerializeField] private Image _ammunitionBar;
        [SerializeField] private TextMeshProUGUI _ammoText;
        [SerializeField] private float _reloadIconAngleSpeed;
        [SerializeField] private Image _reloadIcon;

        [Header("Bomb Panel")] 
        [SerializeField] private TextMeshProUGUI _bombCount;
        [SerializeField] private float _cooldownIconAngleSpeed;
        [SerializeField] private GameObject _cooldownIcon;
        
        [Header("Shield Panel")]
        [SerializeField] private Image _temperatureBar;
        [SerializeField] private float _shieldCooldownIconAngleSpeed;
        [SerializeField] private GameObject _shieldCooldownIcon;
        [SerializeField] private Gradient _shieldColor;
        [SerializeField] private Color _cooldownZoneColor;
        
        protected override void Update()
        {
            base.Update();
            if (!Assigned) return;
            
            UpdateActivePanel(Time.time);
            UpdateGunPanel();
            UpdateBombPanel();
            UpdateShieldPanel();
        }

        protected override void TryCachePlayer()
        {
            base.TryCachePlayer();
            if (!Assigned) return;
            InputReader.Instance.OnThrowPressed += SetPanelBomb;
            InputReader.Instance.OnShieldPressed += SetPanelShield;
        }

        private void SetPanelBomb()
        {
            Player.Dependencies.TryGetFeature(out Shoot shoot);
            if(shoot.IsShooting || shoot.IsReloading) return;
            Player.Dependencies.TryGetFeature(out Shield shield);
            if(shield.IsShieldActive) return;
            
            _bombPanel.SetActive(true);
            _gunPanel.SetActive(false);
            _shieldPanel.SetActive(false);
            _lastActiveTime = Time.time;
        }

        private void SetPanelShield()
        { 
            Player.Dependencies.TryGetFeature(out Shoot shoot);
            if(shoot.IsShooting || shoot.IsReloading) return;
            Player.Dependencies.TryGetFeature(out Bomb bomb);
            if(bomb.IsThrowing) return;
            
            _bombPanel.SetActive(false);
            _gunPanel.SetActive(false);
            _shieldPanel.SetActive(true);
            _lastActiveTime = Time.time;
        }

        private void UpdateActivePanel(float time)
        {
            Player.Dependencies.TryGetFeature(out Bomb bomb); 
            Player.Dependencies.TryGetFeature(out Shoot shoot);
            Player.Dependencies.TryGetFeature(out Shield shield);

            if (bomb.IsThrowing)
            {
                _bombPanel.SetActive(true);
                _gunPanel.SetActive(false);
                _shieldPanel.SetActive(false);
                _lastActiveTime = time;
            }
            
            else if (shoot.IsShooting || shoot.IsReloading)
            {
                _bombPanel.SetActive(false);
                _gunPanel.SetActive(true);
                _shieldPanel.SetActive(false);
                _lastActiveTime = time;
            }
            
            else if (shield.IsShieldActive)
            {
                _bombPanel.SetActive(false);
                _gunPanel.SetActive(false);
                _shieldPanel.SetActive(true);
                _lastActiveTime = time;
            }

            if (time > _lastActiveTime + _maxIdleActiveTime)
            {
                _bombPanel.SetActive(false);
                _gunPanel.SetActive(false);
                _shieldPanel.SetActive(false);
            }
        }

        private void UpdateGunPanel()
        {
            if(!_gunPanel.activeSelf) return;

            Player.Dependencies.TryGetFeature(out Shoot shoot);
            
            _reloadIcon.gameObject.SetActive(shoot.IsReloading);

            if (!_reloadIcon.gameObject.activeSelf)
            {
                _reloadIcon.transform.rotation = Quaternion.identity;
            }
            else
            {
                _reloadIcon.transform.rotation *= Quaternion.Euler(0, 0, _reloadIconAngleSpeed * Time.deltaTime);
            }

            if (shoot.IsReloading)
            {
                _ammoText.text = string.Empty;
                float reloadProgress = 1 - Mathf.Clamp01(shoot.ReloadTimer / shoot.ReloadTime);
                _ammunitionBar.fillAmount = reloadProgress;
                return;
            }

            _ammoText.text = $"{shoot.CurrentAmmo}/{shoot.MagazineSize}";
            float ratio = Mathf.Clamp01((float)shoot.CurrentAmmo / shoot.MagazineSize);
            _ammunitionBar.fillAmount = ratio;
        }

        private void UpdateBombPanel()
        {
            if(!_bombPanel.activeSelf) return;

            Player.Dependencies.TryGetFeature(out Bomb bomb);

            _cooldownIcon.gameObject.SetActive(bomb.IsOnCooldown);
            _bombCount.text = $"#{bomb.BombCount}";

            if (!_cooldownIcon.activeSelf)
            {
                _cooldownIcon.transform.rotation = Quaternion.identity;
            }
            else
            {
                _cooldownIcon.transform.rotation *= Quaternion.Euler(0, 0, _cooldownIconAngleSpeed * Time.deltaTime);
            }
        }

        private void UpdateShieldPanel()
        {
            if(!_shieldPanel.activeSelf) return;
            
            Player.Dependencies.TryGetFeature(out Shield shield);
            
            _temperatureBar.fillAmount = shield.TemperatureProgress;
            
            if(shield.OnCooldown) _temperatureBar.color = _cooldownZoneColor;
            else _temperatureBar.color = _shieldColor.Evaluate(shield.TemperatureProgress);
            
            _shieldCooldownIcon.gameObject.SetActive(shield.OnCooldown);

            if (!_shieldCooldownIcon.gameObject.activeSelf)
            {
                _shieldCooldownIcon.transform.rotation = Quaternion.identity;
            }
            else
            {
                _shieldCooldownIcon.transform.rotation *= Quaternion.Euler(0, 0, _shieldCooldownIconAngleSpeed * Time.deltaTime);
            }
        }
    }
}