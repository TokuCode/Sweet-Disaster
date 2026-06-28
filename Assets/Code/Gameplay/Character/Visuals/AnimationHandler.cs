using Code.Gameplay.Character.Features;
using Code.Systems.Input;
using Unity.Netcode;
using UnityEngine;

namespace Code.Gameplay.Character.Visuals
{
    public class AnimationHandler : NetworkBehaviour
    {
        [SerializeField] private Animator animatorController;
        
        [SerializeField] private SpriteRenderer arms;
        private Color _spriteColor;
            
        void Update()
        {
            if (!IsOwner) return;
            
            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out PhysicsCheck physicsCheck)) return;
            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out Health health)) return;
            if (!PlayerController.Singleton.Dependencies.TryGetFeature(out Crouch crouch)) return;
            
            animatorController.SetFloat("horizontalVel", PlayerController.Singleton.rigidbody.linearVelocityX);
            //animatorController.SetFloat("horizontalInput", InputReader.Instance.Move);
            
            animatorController.SetFloat("verticalVel", PlayerController.Singleton.rigidbody.linearVelocityY);
            
            animatorController.SetBool("isGrounded", physicsCheck.IsGrounded);
            
            animatorController.SetBool("isStunned", health.IsStunned);
            
            animatorController.SetBool("isCrouching", crouch.IsCrouching);

            _spriteColor = arms.color;
            _spriteColor.a = crouch.IsCrouching ? 0f : 1f;
            arms.color = _spriteColor;
        }

        public void SetAnimator(CharacterScriptable character) => animatorController.runtimeAnimatorController = character.runtimeAnimator;
    }
}