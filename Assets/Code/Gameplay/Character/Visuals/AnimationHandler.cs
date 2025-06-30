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
            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out Health health)) return;
            
            animatorController.SetFloat("horizontalVel", PlayerController.Singleton.rigidbody.linearVelocityX);
            //animatorController.SetFloat("horizontalInput", InputReader.Instance.Move);
            
            animatorController.SetFloat("verticalVel", PlayerController.Singleton.rigidbody.linearVelocityY);
            
            animatorController.SetBool("isGrounded", physicsCheck.IsGrounded);
            
            animatorController.SetBool("isStunned", health.IsStunned);
        }

        public void SetAnimator(CharacterVisuals visuals) => animatorController.runtimeAnimatorController = visuals.runtimeAnimator;
    }
}