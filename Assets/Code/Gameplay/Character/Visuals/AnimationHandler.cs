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
            SetStunBool();
            SetMoveBool(InputReader.Instance.Move);
        }

        private void SetStunBool()
        {
            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out Health health)) return;
            animatorController.SetBool("isStunned", health.IsStunned);
        }

        private void SetMoveBool(float moveInput)
        {
            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out PhysicsCheck physicsCheck)) return;
            animatorController.SetBool("isMoving", moveInput != 0 && physicsCheck.IsGrounded);
        }
        
        public void SetVisuals(CharacterVisuals visuals)
        {
            animatorController.runtimeAnimatorController = visuals.runtimeAnimator;
        }
    }
}