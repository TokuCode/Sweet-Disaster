using System;
using Code.Gameplay.Character.Features;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Visuals 
{
    public class ArmSpriteChanger : NetworkBehaviour
    {
        private GunBelt _gunbelt;
        
        [SerializeField] private SpriteRenderer armRenderer;
        
        private Sprite _armWithGun;
        private Sprite _armWithBomb;
        private Sprite _armWithShield;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            
            if (!IsOwner) return;
            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out _gunbelt)) return;
            
            _gunbelt.WeaponChanged += ChangeArm;
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            
            if (!IsOwner) return;
            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out _gunbelt)) return;
            
            _gunbelt.WeaponChanged -= ChangeArm;
        }

        public void SetSprites(CharacterVisuals visuals)
        {
            _armWithGun = visuals.armWithGun;
            _armWithBomb = visuals.armWithBomb;
            _armWithShield = visuals.armWithShield;
        }
        
        private void ChangeArm(int index)
        {
            ChangeSprite(index);
            ChangeSpriteRpc(index);
        }
        
        private void ChangeSprite(int index)
        {
            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out _gunbelt)) return;

            switch ((GunBelt.Weapon)index)
            {
                case GunBelt.Weapon.Gun:
                    armRenderer.sprite = _armWithGun;
                    break;
                case GunBelt.Weapon.Bomb:
                    armRenderer.sprite = _armWithBomb;
                    break;
            }
        }
        
        [Rpc(SendTo.NotMe)]
        private void ChangeSpriteRpc(int index) => ChangeSprite(index);
    }
}