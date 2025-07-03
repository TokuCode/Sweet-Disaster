using Code.Gameplay.Character.Features;
using Unity.Netcode;

namespace Code.Gameplay.Objects
{
    public class ObjectShield : NetworkBehaviour
    {
        private Bomb _bomb;

        public void Init(Bomb bomb) => _bomb = bomb;

        public void OnBlock(int senderId)
        {
            _bomb.RequestBlockReloadAccelerate(GunBelt.Weapon.Shield, senderId);
        }
    }
}