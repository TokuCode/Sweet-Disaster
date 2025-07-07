using Code.Gameplay.Character;
using UnityEngine;

namespace Code.UserInterface.HUD
{
    public class PlayerHUDBase : MonoBehaviour
    {
        private PlayerController _player;
        protected PlayerController Player => _player;
        private bool _assigned;
        protected bool Assigned => _assigned;
        
        protected virtual void TryCachePlayer()
        {
            if(PlayerController.Singleton == null) return;
            
            _player = PlayerController.Singleton;
            _assigned = true;
        }

        protected virtual void Update()
        {
            if(!_assigned) TryCachePlayer();
        }
    }
}