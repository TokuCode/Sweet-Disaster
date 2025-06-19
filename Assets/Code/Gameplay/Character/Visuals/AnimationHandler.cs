using Code.Gameplay.Character.Features;
using Code.Systems.Input;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Visuals
{
    public class AnimationHandler : NetworkBehaviour
    {
        [SerializeField] private Animator animatorController;
        
        void Update()
        {
            if (!IsOwner) return;

            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out PhysicsCheck physicsCheck)) return;
            
            animatorController.SetBool("isMoving", InputReader.Instance.Move != 0 && physicsCheck.IsGrounded);
        }
        
        public void SetVisuals(CharacterVisuals visuals)
        {
            animatorController.runtimeAnimatorController = visuals.runtimeAnimator;
        }
    }
}