using Code.Gameplay.Character.Features;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Objects
{
    public class ObjectShield : NetworkBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        
        private Bomb _bomb;
        private Shield _shield;
        
        [SerializeField] Gradient _shieldColor;

        public void Init(Bomb bomb, Shield shield)
        { 
            _bomb = bomb;  
            _shield = shield;
        }

        private void Update()
        {
            if(!gameObject.activeSelf) return;
            _spriteRenderer.color = _shieldColor.Evaluate(_shield.TemperatureProgress);
        }

        public void OnBlock(int senderId, float heatDamage)
        {
            _bomb.RequestBlockReloadAccelerate(GunBelt.Weapon.Shield, senderId);
            _shield.HeatShield(heatDamage);
        }
    }
}